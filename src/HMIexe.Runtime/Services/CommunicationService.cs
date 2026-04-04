using HMIexe.Communication.Drivers;
using HMIexe.Core.Models.Communication;
using HMIexe.Core.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HMIexe.Runtime.Services;

/// <summary>
/// 通信服务，实现 <see cref="ICommunicationService"/> 接口。
/// 负责管理多个通信通道（<see cref="CommunicationChannel"/>）的连接生命周期，
/// 通过对应的协议驱动（<see cref="IProtocolDriver"/>）进行数据读写，
/// 并将底层驱动接收到的数据统一转发给上层订阅者。
/// </summary>
public class CommunicationService : ICommunicationService
{
    /// <summary>日志记录器，用于输出连接状态、异常等诊断信息。</summary>
    private readonly ILogger<CommunicationService> _logger;

    /// <summary>已注册的通信通道列表。</summary>
    private readonly List<CommunicationChannel> _channels = new();

    /// <summary>通道 ID 到协议驱动实例的映射，支持多线程并发访问。</summary>
    private readonly ConcurrentDictionary<string, IProtocolDriver> _drivers = new();

    /// <summary>通道 ID 到当前连接状态的映射。</summary>
    private readonly ConcurrentDictionary<string, ConnectionStatus> _statuses = new();

    /// <summary>
    /// 通道 ID 到已注册 DataReceived 事件处理器的映射，
    /// 用于在重新连接时先注销旧处理器，防止重复订阅。
    /// </summary>
    private readonly ConcurrentDictionary<string, EventHandler<DataReceivedEventArgs>> _driverHandlers = new();

    /// <summary>获取已注册的通信通道只读列表。</summary>
    public IReadOnlyList<CommunicationChannel> Channels => _channels;

    /// <summary>当任意通道接收到新数据时引发，携带通道 ID、地址、值和时间戳。</summary>
    public event EventHandler<CommunicationDataEventArgs>? DataReceived;

    /// <summary>
    /// 初始化 <see cref="CommunicationService"/> 实例。
    /// </summary>
    /// <param name="logger">依赖注入提供的日志记录器。</param>
    public CommunicationService(ILogger<CommunicationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 向服务中注册一个新的通信通道，并将其初始状态设置为 <see cref="ConnectionStatus.Disconnected"/>。
    /// </summary>
    /// <param name="channel">要注册的通信通道配置。</param>
    public void AddChannel(CommunicationChannel channel)
    {
        _channels.Add(channel);
        _statuses[channel.Id] = ConnectionStatus.Disconnected;
    }

    /// <summary>
    /// 异步连接指定通道：查找或创建对应的协议驱动，构建连接参数并发起连接。
    /// 连接成功后注册数据接收事件处理器，将底层事件转发至 <see cref="DataReceived"/>。
    /// </summary>
    /// <param name="channelId">要连接的通道 ID。</param>
    /// <returns>连接成功返回 <c>true</c>，否则返回 <c>false</c>。</returns>
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
                // 先注销旧处理器，防止重连时重复订阅同一事件导致数据重复触发
                if (_driverHandlers.TryRemove(channelId, out var oldHandler))
                    driver.DataReceived -= oldHandler;

                // 创建新处理器：将驱动层事件包装为通信服务层事件并转发
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

    /// <summary>
    /// 异步断开指定通道的连接，并将状态更新为 <see cref="ConnectionStatus.Disconnected"/>。
    /// 若通道对应的驱动不存在，则忽略此调用。
    /// </summary>
    /// <param name="channelId">要断开的通道 ID。</param>
    public async Task DisconnectAsync(string channelId)
    {
        if (_drivers.TryGetValue(channelId, out var driver))
        {
            await driver.DisconnectAsync();
            _statuses[channelId] = ConnectionStatus.Disconnected;
        }
    }

    /// <summary>
    /// 从指定通道的指定地址异步读取一个值。
    /// 若通道未连接或驱动不存在，则返回 <c>null</c>。
    /// </summary>
    /// <param name="channelId">通道 ID。</param>
    /// <param name="address">设备地址（格式由具体协议驱动定义）。</param>
    /// <returns>读取到的值；读取失败时返回 <c>null</c>。</returns>
    public async Task<object?> ReadValueAsync(string channelId, string address)
    {
        if (!_drivers.TryGetValue(channelId, out var driver) || !driver.IsConnected)
            return null;
        return await driver.ReadAsync(address);
    }

    /// <summary>
    /// 向指定通道的指定地址异步写入一个值。
    /// 若通道未连接或驱动不存在，则返回 <c>false</c>。
    /// </summary>
    /// <param name="channelId">通道 ID。</param>
    /// <param name="address">设备地址。</param>
    /// <param name="value">要写入的值。</param>
    /// <returns>写入成功返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public async Task<bool> WriteValueAsync(string channelId, string address, object value)
    {
        if (!_drivers.TryGetValue(channelId, out var driver) || !driver.IsConnected)
            return false;
        return await driver.WriteAsync(address, value);
    }

    /// <summary>
    /// 获取指定通道的当前连接状态。
    /// 若通道 ID 不存在，则返回 <see cref="ConnectionStatus.Disconnected"/>。
    /// </summary>
    /// <param name="channelId">通道 ID。</param>
    /// <returns>该通道的 <see cref="ConnectionStatus"/>。</returns>
    public ConnectionStatus GetChannelStatus(string channelId) =>
        _statuses.TryGetValue(channelId, out var status) ? status : ConnectionStatus.Disconnected;

    /// <summary>
    /// 根据通道配置获取已有驱动，或根据协议类型创建新驱动实例。
    /// 当前支持 <see cref="ProtocolType.ModbusTcp"/>；其他协议类型抛出 <see cref="NotSupportedException"/>。
    /// </summary>
    /// <param name="channel">通信通道配置。</param>
    /// <returns>对应的协议驱动实例。</returns>
    private IProtocolDriver GetOrCreateDriver(CommunicationChannel channel)
    {
        return _drivers.GetOrAdd(channel.Id, _ => channel.Protocol switch
        {
            ProtocolType.ModbusTcp => new ModbusTcpDriver(),
            _ => throw new NotSupportedException($"Protocol {channel.Protocol} is not yet supported.")
        });
    }

    /// <summary>
    /// 将通道的参数字典复制为一个新的 <see cref="Dictionary{String, String}"/>，
    /// 传递给驱动的连接方法。
    /// </summary>
    /// <param name="channel">通信通道配置。</param>
    /// <returns>包含连接参数的字典副本。</returns>
    private static Dictionary<string, string> BuildParameters(CommunicationChannel channel)
    {
        return new Dictionary<string, string>(channel.Parameters);
    }
}
