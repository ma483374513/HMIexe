/// <summary>
/// 定义通信协议驱动的统一抽象接口及相关数据传输对象。
/// 所有具体协议驱动（如 Modbus RTU、Modbus TCP 等）均须实现此接口，
/// 以便上层业务代码以统一方式进行连接、读取、写入和事件订阅。
/// </summary>
namespace HMIexe.Communication.Drivers;

/// <summary>
/// 协议驱动接口，定义与底层通信设备交互所需的全部操作契约。
/// 实现类负责具体的连接管理、数据收发以及事件通知。
/// </summary>
public interface IProtocolDriver
{
    /// <summary>
    /// 获取该驱动所实现的协议名称（例如 "Modbus RTU"、"Modbus TCP"）。
    /// </summary>
    string ProtocolName { get; }

    /// <summary>
    /// 获取当前与设备的连接状态；<c>true</c> 表示已连接，<c>false</c> 表示未连接。
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 异步建立与设备的连接。
    /// </summary>
    /// <param name="parameters">连接参数字典，键值对内容因协议而异（如端口号、波特率、IP 地址等）。</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
    /// <returns>连接成功返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    Task<bool> ConnectAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步断开与设备的连接并释放相关资源。
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// 异步从指定地址读取数据。
    /// </summary>
    /// <param name="address">设备地址字符串，格式由具体协议驱动定义（例如 Modbus RTU 使用 "FC:寄存器号"）。</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
    /// <returns>读取到的值；若读取失败则返回 <c>null</c>。</returns>
    Task<object?> ReadAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步向指定地址写入数据。
    /// </summary>
    /// <param name="address">设备地址字符串，格式由具体协议驱动定义。</param>
    /// <param name="value">要写入的值。</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
    /// <returns>写入成功返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    Task<bool> WriteAsync(string address, object value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 当驱动接收到设备主动上报的数据时触发此事件（适用于订阅/推送模式）。
    /// </summary>
    event EventHandler<DataReceivedEventArgs>? DataReceived;

    /// <summary>
    /// 当驱动成功建立与设备的连接后触发此事件。
    /// </summary>
    event EventHandler? Connected;

    /// <summary>
    /// 当驱动与设备断开连接后触发此事件。
    /// </summary>
    event EventHandler? Disconnected;
}

/// <summary>
/// 设备数据接收事件参数，携带接收到的地址、值及时间戳信息。
/// </summary>
public class DataReceivedEventArgs : EventArgs
{
    /// <summary>
    /// 数据来源的设备地址。
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// 接收到的数据值；类型由具体协议驱动决定。
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// 数据接收的本地时间戳，默认为当前时间。
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
