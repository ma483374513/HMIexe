using HMIexe.Communication.Drivers;
using HMIexe.Core.Models.Communication;
using HMIexe.Core.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HMIexe.Runtime.Services;

public class CommunicationService : ICommunicationService
{
    private readonly ILogger<CommunicationService> _logger;
    private readonly List<CommunicationChannel> _channels = new();
    private readonly ConcurrentDictionary<string, IProtocolDriver> _drivers = new();
    private readonly ConcurrentDictionary<string, ConnectionStatus> _statuses = new();
    private readonly ConcurrentDictionary<string, EventHandler<DataReceivedEventArgs>> _driverHandlers = new();

    public IReadOnlyList<CommunicationChannel> Channels => _channels;
    public event EventHandler<CommunicationDataEventArgs>? DataReceived;

    public CommunicationService(ILogger<CommunicationService> logger)
    {
        _logger = logger;
    }

    public void AddChannel(CommunicationChannel channel)
    {
        _channels.Add(channel);
        _statuses[channel.Id] = ConnectionStatus.Disconnected;
    }

    public async Task<bool> ConnectAsync(string channelId)
    {
        var channel = _channels.FirstOrDefault(c => c.Id == channelId);
        if (channel == null)
        {
            _logger.LogWarning("Channel {Id} not found", channelId);
            return false;
        }

        _statuses[channelId] = ConnectionStatus.Connecting;
        try
        {
            var driver = GetOrCreateDriver(channel);
            var parameters = BuildParameters(channel);
            var result = await driver.ConnectAsync(parameters);
            _statuses[channelId] = result ? ConnectionStatus.Connected : ConnectionStatus.Error;
            if (result)
            {
                // Unregister previous handler before registering a new one to prevent duplicate events on reconnect
                if (_driverHandlers.TryRemove(channelId, out var oldHandler))
                    driver.DataReceived -= oldHandler;

                EventHandler<DataReceivedEventArgs> handler = (s, e) => DataReceived?.Invoke(this, new CommunicationDataEventArgs
                {
                    ChannelId = channelId,
                    Address = e.Address,
                    Value = e.Value,
                    Timestamp = e.Timestamp
                });
                driver.DataReceived += handler;
                _driverHandlers[channelId] = handler;
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect channel {Id}", channelId);
            _statuses[channelId] = ConnectionStatus.Error;
            return false;
        }
    }

    public async Task DisconnectAsync(string channelId)
    {
        if (_drivers.TryGetValue(channelId, out var driver))
        {
            await driver.DisconnectAsync();
            _statuses[channelId] = ConnectionStatus.Disconnected;
        }
    }

    public async Task<object?> ReadValueAsync(string channelId, string address)
    {
        if (!_drivers.TryGetValue(channelId, out var driver) || !driver.IsConnected)
            return null;
        return await driver.ReadAsync(address);
    }

    public async Task<bool> WriteValueAsync(string channelId, string address, object value)
    {
        if (!_drivers.TryGetValue(channelId, out var driver) || !driver.IsConnected)
            return false;
        return await driver.WriteAsync(address, value);
    }

    public ConnectionStatus GetChannelStatus(string channelId) =>
        _statuses.TryGetValue(channelId, out var status) ? status : ConnectionStatus.Disconnected;

    private IProtocolDriver GetOrCreateDriver(CommunicationChannel channel)
    {
        return _drivers.GetOrAdd(channel.Id, _ => channel.Protocol switch
        {
            ProtocolType.ModbusTcp => new ModbusTcpDriver(),
            _ => throw new NotSupportedException($"Protocol {channel.Protocol} is not yet supported.")
        });
    }

    private static Dictionary<string, string> BuildParameters(CommunicationChannel channel)
    {
        return new Dictionary<string, string>(channel.Parameters);
    }
}
