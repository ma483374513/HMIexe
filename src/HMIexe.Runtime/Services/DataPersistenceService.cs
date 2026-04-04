using System.Data;
using Microsoft.Data.Sqlite;

namespace HMIexe.Runtime.Services;

/// <summary>
/// 本地数据持久化服务，使用 SQLite 数据库存储变量历史值、报警日志和系统日志。
/// <para>
/// 数据库默认位置：<c>%AppData%/HMIexe/hmidata.db</c>；
/// 若无法获取系统目录，则退回至当前工作目录下的 <c>hmidata.db</c>。
/// </para>
/// <para>
/// 所有 SQL 语句均使用参数化查询，防止 SQL 注入。
/// 实现 <see cref="IDisposable"/>，使用完毕后应调用 <see cref="Dispose"/> 关闭连接。
/// </para>
/// </summary>
public class DataPersistenceService : IDisposable
{
    /// <summary>底层 SQLite 数据库连接，在构造函数中打开并保持整个服务生命周期内有效。</summary>
    private readonly SqliteConnection _connection;

    /// <summary>标记资源是否已释放，防止重复 Dispose。</summary>
    private bool _disposed;

    /// <summary>
    /// 初始化 <see cref="DataPersistenceService"/>：创建或打开数据库文件，并初始化表结构。
    /// </summary>
    /// <param name="dbPath">数据库文件路径；为 <c>null</c> 时使用默认路径。</param>
    public DataPersistenceService(string? dbPath = null)
    {
        dbPath ??= GetDefaultDbPath();
        var dir = Path.GetDirectoryName(dbPath);
        // 确保目录存在，不存在则递归创建
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        InitializeSchema();
    }

    /// <summary>
    /// 获取默认数据库文件路径：<c>%AppData%/HMIexe/hmidata.db</c>。
    /// 若获取系统目录失败，则返回当前目录下的 <c>hmidata.db</c>。
    /// </summary>
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

    /// <summary>
    /// 初始化数据库表结构（使用 IF NOT EXISTS，安全幂等）：
    /// <list type="bullet">
    ///   <item><c>VariableHistory</c>：变量历史值表，含变量名和时间戳索引。</item>
    ///   <item><c>AlarmLog</c>：报警日志表，记录触发和确认信息。</item>
    ///   <item><c>SystemLog</c>：系统日志表，含时间戳索引。</item>
    /// </list>
    /// </summary>
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

    // ── 变量历史 ─────────────────────────────────────────────

    /// <summary>
    /// 异步将变量的当前值记录到 <c>VariableHistory</c> 表，时间戳使用 UTC。
    /// </summary>
    /// <param name="variableName">变量名称。</param>
    /// <param name="value">要记录的值；为 <c>null</c> 时存储 SQL NULL。</param>
    public async Task RecordVariableValueAsync(string variableName, object? value)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO VariableHistory(VariableName, Value, Timestamp) VALUES($name, $value, $ts)";
        cmd.Parameters.AddWithValue("$name", variableName);
        cmd.Parameters.AddWithValue("$value", value?.ToString() ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 异步查询指定变量的历史记录，支持时间范围过滤和条数限制。
    /// 结果按时间戳降序排列（最新记录在前）。
    /// </summary>
    /// <param name="variableName">变量名称。</param>
    /// <param name="from">起始时间（含），为 <c>null</c> 时不限制。</param>
    /// <param name="to">结束时间（含），为 <c>null</c> 时不限制。</param>
    /// <param name="limit">最大返回条数，默认 1000。</param>
    /// <returns>时间戳和值的元组列表；时间戳解析失败的记录会被跳过。</returns>
    public async Task<List<(DateTime Timestamp, string? Value)>> GetVariableHistoryAsync(
        string variableName, DateTime? from = null, DateTime? to = null, int limit = 1000)
    {
        await using var cmd = _connection.CreateCommand();
        // WHERE 子句由硬编码的列名和参数占位符拼接，用户值全部使用参数化传递
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
            // 时间戳格式异常的记录跳过，属于数据完整性问题
        }
        return result;
    }

    // ── 报警日志 ────────────────────────────────────────────────────

    /// <summary>
    /// 异步将一条报警触发记录写入 <c>AlarmLog</c> 表。
    /// </summary>
    /// <param name="alarmName">报警名称。</param>
    /// <param name="severity">严重级别字符串。</param>
    /// <param name="message">报警描述消息。</param>
    /// <returns>新插入记录的自增主键 ID，可用于后续确认操作。</returns>
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

    /// <summary>
    /// 异步更新指定报警记录的确认信息（确认时间和操作人）。
    /// </summary>
    /// <param name="alarmId">报警记录的数据库自增 ID。</param>
    /// <param name="acknowledgedBy">执行确认的用户名。</param>
    public async Task AcknowledgeAlarmAsync(long alarmId, string acknowledgedBy)
    {
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE AlarmLog SET AcknowledgedAt=$ts, AcknowledgedBy=$by WHERE Id=$id";
        cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$by", acknowledgedBy);
        cmd.Parameters.AddWithValue("$id", alarmId);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── 系统日志 ───────────────────────────────────────────────────

    /// <summary>
    /// 异步向 <c>SystemLog</c> 表写入一条系统日志。
    /// </summary>
    /// <param name="level">日志级别（如 "Info"、"Warning"、"Error"）。</param>
    /// <param name="category">日志类别/来源模块名称。</param>
    /// <param name="message">日志消息内容。</param>
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

    /// <summary>
    /// 异步查询系统日志，支持按级别和类别过滤，并限制返回条数。
    /// 结果按时间戳降序排列（最新在前）。
    /// </summary>
    /// <param name="level">日志级别过滤；为 <c>null</c> 时不过滤。</param>
    /// <param name="category">类别过滤；为 <c>null</c> 时不过滤。</param>
    /// <param name="limit">最大返回条数，默认 500。</param>
    /// <returns>日志条目的元组列表（时间戳、级别、类别、消息）。</returns>
    public async Task<List<(DateTime Timestamp, string Level, string Category, string Message)>> GetLogsAsync(
        string? level = null, string? category = null, int limit = 500)
    {
        await using var cmd = _connection.CreateCommand();
        // 动态构建 WHERE 子句，条件列表只含硬编码的"列名 = $参数"字符串
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
            // 时间戳解析失败的记录跳过
        }
        return result;
    }

    // ── 导出 CSV ────────────────────────────────────────────────────

    /// <summary>
    /// 将指定变量的历史数据异步导出为 CSV 文件，列为 Timestamp 和 Value。
    /// </summary>
    /// <param name="variableName">要导出的变量名称。</param>
    /// <param name="filePath">目标 CSV 文件路径。</param>
    /// <param name="maxRecords">最大导出条数，默认 100,000。</param>
    public async Task ExportVariableHistoryToCsvAsync(string variableName, string filePath, int maxRecords = 100_000)
    {
        var data = await GetVariableHistoryAsync(variableName, limit: maxRecords);
        var lines = new List<string> { "Timestamp,Value" };
        lines.AddRange(data.Select(r => $"{r.Timestamp:o},{EscapeCsv(r.Value)}"));
        await File.WriteAllLinesAsync(filePath, lines);
    }

    /// <summary>
    /// 将完整报警日志异步导出为 CSV 文件，按触发时间降序排列。
    /// </summary>
    /// <param name="filePath">目标 CSV 文件路径。</param>
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

    /// <summary>
    /// 对 CSV 字段值进行转义：若值包含逗号、引号或换行符，则用双引号包裹，
    /// 并将内部引号替换为两个引号（RFC 4180 标准）。
    /// </summary>
    /// <param name="value">原始字段值。</param>
    /// <returns>转义后的 CSV 字段字符串。</returns>
    private static string EscapeCsv(string? value)
    {
        if (value == null) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    // ── 数据清理 ──────────────────────────────────────────────────────

    /// <summary>
    /// 异步清理超过指定保留天数的历史数据，涵盖变量历史、报警日志和系统日志三张表。
    /// </summary>
    /// <param name="keepDays">数据保留天数，默认 90 天。</param>
    public async Task PurgeOldRecordsAsync(int keepDays = 90)
    {
        // 计算截止时间点，早于此时间的记录将被删除
        var cutoff = DateTime.UtcNow.AddDays(-keepDays).ToString("o");
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM VariableHistory WHERE Timestamp < $cutoff;
            DELETE FROM AlarmLog WHERE RaisedAt < $cutoff;
            DELETE FROM SystemLog WHERE Timestamp < $cutoff;";
        cmd.Parameters.AddWithValue("$cutoff", cutoff);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// 同步执行不返回结果集的 SQL 语句，主要用于 DDL（建表、建索引）。
    /// </summary>
    /// <param name="sql">要执行的 SQL 语句。</param>
    private void ExecuteNonQuery(string sql)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// 释放数据库连接资源。通过 <see cref="_disposed"/> 标志保证幂等性。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Close();
        _connection.Dispose();
    }
}
