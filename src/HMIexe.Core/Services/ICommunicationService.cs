using HMIexe.Core.Models.Communication;

namespace HMIexe.Core.Services;

public interface ICommunicationService
{
    IReadOnlyList<CommunicationChannel> Channels { get; }
    Task<bool> ConnectAsync(string channelId);
    Task DisconnectAsync(string channelId);
    Task<object?> ReadValueAsync(string channelId, string address);
    Task<bool> WriteValueAsync(string channelId, string address, object value);
    ConnectionStatus GetChannelStatus(string channelId);
    event EventHandler<CommunicationDataEventArgs> DataReceived;
}

public enum ConnectionStatus { Disconnected, Connecting, Connected, Error }

public class CommunicationDataEventArgs : EventArgs
{
    public string ChannelId { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public object? Value { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
