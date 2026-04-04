/// <summary>
/// 通信通道模型定义。
/// 包含工业协议类型枚举、通信通道配置（协议参数与重连策略）以及设备地址到变量的映射关系。
/// </summary>
namespace HMIexe.Core.Models.Communication;

/// <summary>
/// 工业通信协议类型枚举。
/// 枚举 HMI 系统所支持的各类工业现场总线和网络协议。
/// </summary>
public enum ProtocolType
{
    /// <summary>Modbus RTU 串行通信协议。</summary>
    ModbusRtu,
    /// <summary>Modbus TCP/IP 以太网协议。</summary>
    ModbusTcp,
    /// <summary>OPC 统一架构（OPC-UA）协议，支持安全加密通信。</summary>
    OpcUa,
    /// <summary>OPC 经典数据访问（OPC-DA）协议。</summary>
    OpcDa,
    /// <summary>MQTT 消息队列遥测传输协议，常用于物联网场景。</summary>
    Mqtt,
    /// <summary>Web API / HTTP RESTful 接口通信。</summary>
    WebApi,
    /// <summary>UDP 用户数据报协议，适用于低延迟广播场景。</summary>
    Udp,
    /// <summary>TCP 传输控制协议，自定义帧格式通信。</summary>
    Tcp,
    /// <summary>串行端口（COM 口）通信。</summary>
    SerialPort
}

/// <summary>
/// HMI 通信通道配置。
/// 描述一条与设备通信的通道，包括所使用的协议、连接参数、自动重连策略及设备地址映射列表。
/// </summary>
public class CommunicationChannel
{
    /// <summary>通道的唯一标识符（GUID）。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>通道名称，用于在界面中标识该通信链路。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>该通道使用的工业通信协议类型。</summary>
    public ProtocolType Protocol { get; set; }

    /// <summary>是否启用该通信通道；禁用后运行时将不尝试连接。</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>连接断开后是否自动重连。</summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>自动重连的间隔时间（毫秒），默认为 5000ms（5 秒）。</summary>
    public int ReconnectIntervalMs { get; set; } = 5000;

    /// <summary>
    /// 协议特定的参数字典（键值对形式）。
    /// 例如：Modbus TCP 通道可包含 "Host"、"Port"；串口通道可包含 "BaudRate"、"DataBits" 等。
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = new();

    /// <summary>该通道关联的设备地址映射列表，每项将设备寄存器/地址映射到一个 HMI 变量。</summary>
    public List<DeviceMapping> DeviceMappings { get; set; } = new();
}

/// <summary>
/// 设备地址与 HMI 变量的映射关系。
/// 定义如何将设备上的某个寄存器地址与 HMI 变量绑定，并指定轮询频率。
/// </summary>
public class DeviceMapping
{
    /// <summary>映射项的唯一标识符（GUID）。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>目标设备的名称或标识，便于区分同一通道中的多台设备。</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>设备寄存器或数据点的地址字符串（格式由协议决定，如 "40001"、"ns=2;i=3"）。</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>与该地址绑定的 HMI 变量 ID；读取到的数据将写入对应变量。</summary>
    public string VariableId { get; set; } = string.Empty;

    /// <summary>轮询该地址的时间间隔（毫秒），默认为 1000ms（1 秒）。</summary>
    public int PollingIntervalMs { get; set; } = 1000;
}
