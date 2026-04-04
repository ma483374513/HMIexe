using System.Data;
using Microsoft.Data.Sqlite;

namespace HMIexe.Runtime.Services;

/// <summary>
/// Local SQLite database for storing variable history, alarm records, and system logs.
/// Database is created at %AppData%/HMIexe/hmidata.db (or current directory if unavailable).
/// </summary>
public class DataPersistenceService : IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public DataPersistenceService(string? dbPath = null)
    {
        dbPath ??= GetDefaultDbPath();
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeSchema();
    }

    private static string GetDefaultDbPath()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "HMIexe", "hmidata.db");
        }
        catch
        {
            return "hmidata.db";
        }
    }

    private void InitializeSchema()
    {
        ExecuteNonQuery(@"
            CREATE TABLE IF NOT EXISTS VariableHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                VariableName TEXT NOT NULL,
                Value TEXT,
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_var_history_name ON VariableHistory(VariableName);
            CREATE INDEX IF NOT EXISTS idx_var_history_ts ON VariableHistory(Timestamp);

            CREATE TABLE IF NOT EXISTS AlarmLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AlarmName TEXT NOT NULL,
                Severity TEXT,
                Message TEXT,
                RaisedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                AcknowledgedAt DATETIME,
                AcknowledgedBy TEXT
            );

            CREATE TABLE IF NOT EXISTS SystemLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Level TEXT NOT NULL,
                Category TEXT,
                Message TEXT NOT NULL,
                Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS idx_syslog_ts ON SystemLog(Timestamp);
        ");
    }

    // ── Variable History ─────────────────────────────────────────────

    public async Task RecordVariableValueAsync(string variableName, object? value)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO VariableHistory(VariableName, Value, Timestamp) VALUES($name, $value, $ts)";
        cmd.Parameters.AddWithValue("$name", variableName);
        cmd.Parameters.AddWithValue("$value", value?.ToString() ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<(DateTime Timestamp, string? Value)>> GetVariableHistoryAsync(
        string variableName, DateTime? from = null, DateTime? to = null, int limit = 1000)
    {
        await using var cmd = _connection.CreateCommand();
        // WHERE clause is assembled from hardcoded string literals only;
        // all user-supplied values use parameterized placeholders ($name, $from, $to, $limit).
        var where = "VariableName = $name";
        if (from.HasValue) where += " AND Timestamp >= $from";
        if (to.HasValue) where += " AND Timestamp <= $to";
        cmd.CommandText = $"SELECT Timestamp, Value FROM VariableHistory WHERE {where} ORDER BY Timestamp DESC LIMIT $limit";
        cmd.Parameters.AddWithValue("$name", variableName);
        cmd.Parameters.AddWithValue("$limit", limit);
        if (from.HasValue) cmd.Parameters.AddWithValue("$from", from.Value.ToString("o"));
        if (to.HasValue) cmd.Parameters.AddWithValue("$to", to.Value.ToString("o"));

        var result = new List<(DateTime, string?)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (DateTime.TryParse(reader.GetString(0), out var ts))
                result.Add((ts, reader.IsDBNull(1) ? null : reader.GetString(1)));
            // Skip records with unparseable timestamps (data integrity issue in DB)
        }
        return result;
    }

    // ── Alarm Log ────────────────────────────────────────────────────

    public async Task<long> RecordAlarmRaisedAsync(string alarmName, string severity, string message)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO AlarmLog(AlarmName, Severity, Message, RaisedAt)
            VALUES($name, $sev, $msg, $ts);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$name", alarmName);
        cmd.Parameters.AddWithValue("$sev", severity);
        cmd.Parameters.AddWithValue("$msg", message);
        cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
        return (long)(await cmd.ExecuteScalarAsync() ?? 0L);
    }

    public async Task AcknowledgeAlarmAsync(long alarmId, string acknowledgedBy)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE AlarmLog SET AcknowledgedAt=$ts, AcknowledgedBy=$by WHERE Id=$id";
        cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$by", acknowledgedBy);
        cmd.Parameters.AddWithValue("$id", alarmId);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── System Log ───────────────────────────────────────────────────

    public async Task LogAsync(string level, string category, string message)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO SystemLog(Level, Category, Message, Timestamp) VALUES($level,$cat,$msg,$ts)";
        cmd.Parameters.AddWithValue("$level", level);
        cmd.Parameters.AddWithValue("$cat", category);
        cmd.Parameters.AddWithValue("$msg", message);
        cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<(DateTime Timestamp, string Level, string Category, string Message)>> GetLogsAsync(
        string? level = null, string? category = null, int limit = 500)
    {
        await using var cmd = _connection.CreateCommand();
        // Conditions list contains only hardcoded column = $param strings; values are parameterized.
        var conditions = new List<string>();
        if (level != null) conditions.Add("Level = $level");
        if (category != null) conditions.Add("Category = $cat");
        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        cmd.CommandText = $"SELECT Timestamp, Level, Category, Message FROM SystemLog {where} ORDER BY Timestamp DESC LIMIT $limit";
        if (level != null) cmd.Parameters.AddWithValue("$level", level);
        if (category != null) cmd.Parameters.AddWithValue("$cat", category);
        cmd.Parameters.AddWithValue("$limit", limit);

        var result = new List<(DateTime, string, string, string)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (DateTime.TryParse(reader.GetString(0), out var ts))
                result.Add((ts, reader.GetString(1), reader.GetString(2), reader.GetString(3)));
            // Skip records with unparseable timestamps
        }
        return result;
    }

    // ── Export to CSV ────────────────────────────────────────────────

    public async Task ExportVariableHistoryToCsvAsync(string variableName, string filePath, int maxRecords = 100_000)
    {
        var data = await GetVariableHistoryAsync(variableName, limit: maxRecords);
        var lines = new List<string> { "Timestamp,Value" };
        lines.AddRange(data.Select(r => $"{r.Timestamp:o},{EscapeCsv(r.Value)}"));
        await File.WriteAllLinesAsync(filePath, lines);
    }

    public async Task ExportAlarmLogToCsvAsync(string filePath)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT AlarmName, Severity, Message, RaisedAt, AcknowledgedAt, AcknowledgedBy FROM AlarmLog ORDER BY RaisedAt DESC";
        var lines = new List<string> { "AlarmName,Severity,Message,RaisedAt,AcknowledgedAt,AcknowledgedBy" };
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            lines.Add(string.Join(",", Enumerable.Range(0, reader.FieldCount)
                .Select(i => reader.IsDBNull(i) ? string.Empty : EscapeCsv(reader.GetValue(i)?.ToString()))));
        await File.WriteAllLinesAsync(filePath, lines);
    }

    private static string EscapeCsv(string? value)
    {
        if (value == null) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    // ── Cleanup ──────────────────────────────────────────────────────

    public async Task PurgeOldRecordsAsync(int keepDays = 90)
    {
        var cutoff = DateTime.UtcNow.AddDays(-keepDays).ToString("o");
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM VariableHistory WHERE Timestamp < $cutoff;
            DELETE FROM AlarmLog WHERE RaisedAt < $cutoff;
            DELETE FROM SystemLog WHERE Timestamp < $cutoff;";
        cmd.Parameters.AddWithValue("$cutoff", cutoff);
        await cmd.ExecuteNonQueryAsync();
    }

    private void ExecuteNonQuery(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Close();
        _connection.Dispose();
    }
}
