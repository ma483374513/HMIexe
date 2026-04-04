using HMIexe.Core.Models.Communication;

/// <summary>
/// 通信服务接口定义。
/// 提供通信通道的连接/断开、数据读写、连接状态查询及数据接收事件通知等能力，
/// 是 HMI 与外部设备（PLC、传感器等）数据交换的核心契约。
/// </summary>
namespace HMIexe.Core.Services;

/// <summary>
/// HMI 通信服务接口，统一管理多个通信通道的连接状态和数据收发。
/// 消费者可订阅 <see cref="DataReceived"/> 事件以异步处理设备上报的数据。
/// </summary>
public interface ICommunicationService
{
    /// <summary>当前配置的所有通信通道的只读列表。</summary>
    IReadOnlyList<CommunicationChannel> Channels { get; }

    /// <summary>
    /// 异步连接指定通信通道。
    /// </summary>
    /// <param name="channelId">要连接的通道 ID。</param>
    /// <returns>连接成功返回 true，失败返回 false。</returns>
    Task<bool> ConnectAsync(string channelId);

    /// <summary>
    /// 异步断开指定通信通道的连接。
    /// </summary>
    /// <param name="channelId">要断开的通道 ID。</param>
    Task DisconnectAsync(string channelId);

    /// <summary>
    /// 异步从指定通道读取指定地址的当前值。
    /// </summary>
    /// <param name="channelId">目标通信通道 ID。</param>
    /// <param name="address">设备地址（格式由协议决定）。</param>
    /// <returns>读取到的值；读取失败时返回 null。</returns>
    Task<object?> ReadValueAsync(string channelId, string address);

    /// <summary>
    /// 异步向指定通道的指定地址写入数据。
    /// </summary>
    /// <param name="channelId">目标通信通道 ID。</param>
    /// <param name="address">设备地址（格式由协议决定）。</param>
    /// <param name="value">要写入的值。</param>
    /// <returns>写入成功返回 true，失败返回 false。</returns>
    Task<bool> WriteValueAsync(string channelId, string address, object value);

    /// <summary>
    /// 获取指定通道的当前连接状态。
    /// </summary>
    /// <param name="channelId">目标通信通道 ID。</param>
    /// <returns>返回 <see cref="ConnectionStatus"/> 枚举值。</returns>
    ConnectionStatus GetChannelStatus(string channelId);

    /// <summary>当通道收到设备上报的数据时引发的事件，参数包含通道 ID、地址、值和时间戳。</summary>
    event EventHandler<CommunicationDataEventArgs> DataReceived;
}

/// <summary>
/// 通信通道连接状态枚举。
/// </summary>
public enum ConnectionStatus
{
    /// <summary>未连接状态。</summary>
    Disconnected,
    /// <summary>正在建立连接中。</summary>
    Connecting,
    /// <summary>已成功连接并处于正常通信状态。</summary>
    Connected,
    /// <summary>连接发生错误（如超时、认证失败等）。</summary>
    Error
}

/// <summary>
/// 通信数据接收事件参数。
/// 在 <see cref="ICommunicationService.DataReceived"/> 事件触发时传递，
/// 包含来源通道、设备地址、接收到的值及接收时间。
/// </summary>
public class CommunicationDataEventArgs : EventArgs
{
    /// <summary>数据来源的通信通道 ID。</summary>
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>数据来源的设备地址。</summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>接收到的数据值；类型由协议和地址定义决定。</summary>
    public object? Value { get; init; }

    /// <summary>数据接收的时间戳，默认为接收时的当前时间。</summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
