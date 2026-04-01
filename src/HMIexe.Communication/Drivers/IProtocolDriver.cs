namespace HMIexe.Communication.Drivers;

public interface IProtocolDriver
{
    string ProtocolName { get; }
    bool IsConnected { get; }
    Task<bool> ConnectAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<object?> ReadAsync(string address, CancellationToken cancellationToken = default);
    Task<bool> WriteAsync(string address, object value, CancellationToken cancellationToken = default);
    event EventHandler<DataReceivedEventArgs>? DataReceived;
    event EventHandler? Connected;
    event EventHandler? Disconnected;
}

public class DataReceivedEventArgs : EventArgs
{
    public string Address { get; init; } = string.Empty;
    public object? Value { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
