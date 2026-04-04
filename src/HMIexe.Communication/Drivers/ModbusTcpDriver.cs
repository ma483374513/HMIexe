using System.Net;
using System.Net.Sockets;

/// <summary>
/// 提供基于 TCP/IP 的 Modbus TCP 协议驱动实现。
/// 通过标准 Modbus 应用协议头（MBAP）封装 PDU，经由 TCP 套接字与从站设备通信。
/// 默认端口为 502，支持 FC03（读保持寄存器）和 FC06（写单个寄存器）操作。
/// </summary>
namespace HMIexe.Communication.Drivers;

/// <summary>
/// Modbus TCP 协议驱动，基于 <see cref="System.Net.Sockets.TcpClient"/> 实现 <see cref="IProtocolDriver"/> 接口。
/// 每次读写操作均使用原子递增的事务 ID（Transaction Identifier）保证请求与响应的匹配。
/// </summary>
public class ModbusTcpDriver : IProtocolDriver
{
    /// <summary>
    /// TCP 客户端，负责与 Modbus TCP 从站建立和维护 TCP 连接。
    /// </summary>
    private TcpClient? _client;

    /// <summary>
    /// TCP 网络数据流，用于向从站发送请求帧和接收响应帧。
    /// </summary>
    private NetworkStream? _stream;

    /// <summary>
    /// Modbus MBAP 事务标识符，每次请求后原子递增，用于匹配异步响应。
    /// </summary>
    private int _transactionId;

    /// <summary>
    /// 获取协议名称。
    /// </summary>
    public string ProtocolName => "Modbus TCP";

    /// <summary>
    /// 获取当前 TCP 连接状态；<c>true</c> 表示已连接到从站。
    /// </summary>
    public bool IsConnected => _client?.Connected ?? false;

#pragma warning disable CS0067 // Event required by IProtocolDriver interface; raised in polling scenarios
    // DataReceived 事件由接口约定要求声明，在轮询场景下由外部调用触发
    /// <summary>
    /// 数据接收事件（轮询场景下由外部调用触发，保留以满足接口约定）。
    /// </summary>
    public event EventHandler<DataReceivedEventArgs>? DataReceived;
#pragma warning restore CS0067

    /// <summary>
    /// TCP 连接成功建立后触发的事件。
    /// </summary>
    public event EventHandler? Connected;

    /// <summary>
    /// TCP 连接断开后触发的事件。
    /// </summary>
    public event EventHandler? Disconnected;

    /// <summary>
    /// 异步建立与 Modbus TCP 从站的 TCP 连接。
    /// </summary>
    /// <param name="parameters">
    /// 连接参数字典，支持以下键：
    /// <list type="bullet">
    ///   <item><description><b>Host</b>：从站 IP 地址，默认 "127.0.0.1"</description></item>
    ///   <item><description><b>Port</b>：TCP 端口，默认 502</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
    /// <returns>连接成功返回 <c>true</c>；IP 解析失败或发生异常时返回 <c>false</c>。</returns>
    public async Task<bool> ConnectAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var host = parameters.GetValueOrDefault("Host", "127.0.0.1");
            if (!int.TryParse(parameters.GetValueOrDefault("Port", "502"), out var port))
                port = 502;
            if (!IPAddress.TryParse(host, out var ipAddress))
                return false;

            _client = new TcpClient();
            await _client.ConnectAsync(ipAddress, port, cancellationToken);
            _stream = _client.GetStream();
            Connected?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 异步关闭 TCP 连接并释放相关资源，触发断开事件。
    /// </summary>
    public async Task DisconnectAsync()
    {
        _stream?.Close();
        _client?.Close();
        _client = null;
        _stream = null;
        Disconnected?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 异步读取指定保持寄存器的值（FC03）。
    /// 地址格式为 "任意前缀:寄存器号"，实际只使用冒号后的寄存器号部分。
    /// </summary>
    /// <param name="address">地址字符串，格式为 "前缀:寄存器号"（例如 "3:100"）。</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
    /// <returns>读取成功时返回 <see cref="ushort"/> 类型的寄存器值；失败返回 <c>null</c>。</returns>
    public async Task<object?> ReadAsync(string address, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _stream == null) return null;
        try
        {
            var parts = address.Split(':');
            if (parts.Length < 2) return null;
            if (!ushort.TryParse(parts[1], out var register)) return null;

            // 使用原子递增生成唯一事务 ID，保证并发场景下的请求响应匹配
            var tid = (ushort)System.Threading.Interlocked.Increment(ref _transactionId);
            var request = BuildReadHoldingRegistersRequest(tid, register, 1);
            await _stream.WriteAsync(request, cancellationToken);

            // Read MBAP header (6 bytes) + PDU
            // 读取 MBAP 头（6 字节）+ PDU 开头（功能码 + 字节计数）= 共 9 字节
            var header = new byte[9];
            var headerRead = await ReadExactAsync(_stream, header, 9, cancellationToken);
            if (!headerRead) return null;

            // Validate transaction ID
            // 校验响应帧的事务 ID 是否与请求一致，防止错误匹配
            var responseTid = (ushort)((header[0] << 8) | header[1]);
            if (responseTid != tid) return null;

            // Check function code (should be 0x03), not an exception (0x83)
            // 检测功能码：0x83 为 FC03 的异常响应码，0x03 为正常响应
            if (header[7] == 0x83) return null; // Modbus exception
            if (header[7] != 0x03) return null;

            // Byte count is header[8]
            // header[8] 为后续数据的字节数，读取保持寄存器时每个寄存器占 2 字节
            var byteCount = header[8];
            if (byteCount < 2) return null;

            var data = new byte[byteCount];
            if (!await ReadExactAsync(_stream, data, byteCount, cancellationToken)) return null;

            // 按大端字节序拼合 16 位寄存器值
            return (ushort)((data[0] << 8) | data[1]);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 异步向指定保持寄存器写入值（FC06）。
    /// 地址格式为 "任意前缀:寄存器号"，实际只使用冒号后的寄存器号部分。
    /// </summary>
    /// <param name="address">地址字符串，格式为 "前缀:寄存器号"（例如 "6:10"）。</param>
    /// <param name="value">要写入的值，将被转换为 <see cref="ushort"/>。</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
    /// <returns>写入成功返回 <c>true</c>，发生异常或从站返回错误码时返回 <c>false</c>。</returns>
    public async Task<bool> WriteAsync(string address, object value, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _stream == null) return false;
        try
        {
            var parts = address.Split(':');
            if (parts.Length < 2) return false;
            if (!ushort.TryParse(parts[1], out var register)) return false;
            var writeValue = Convert.ToUInt16(value);

            // 使用原子递增生成唯一事务 ID
            var tid = (ushort)System.Threading.Interlocked.Increment(ref _transactionId);
            var request = BuildWriteSingleRegisterRequest(tid, register, writeValue);
            await _stream.WriteAsync(request, cancellationToken);

            // Read response header
            // FC06 的响应帧与请求帧结构相同，共 12 字节
            var response = new byte[12];
            if (!await ReadExactAsync(_stream, response, 12, cancellationToken)) return false;

            // Validate transaction ID and function code
            // 校验事务 ID 匹配，并检查功能码是否为正常响应（0x06）而非异常（0x86）
            var responseTid = (ushort)((response[0] << 8) | response[1]);
            if (responseTid != tid) return false;
            if (response[7] == 0x86) return false; // Write single register exception
            return response[7] == 0x06;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 从 <see cref="NetworkStream"/> 中精确读取指定数量的字节。
    /// 由于 TCP 是流式协议，单次 <c>ReadAsync</c> 可能不返回全部字节，此方法循环读取直到满足数量要求。
    /// </summary>
    /// <param name="stream">目标网络流。</param>
    /// <param name="buffer">接收缓冲区。</param>
    /// <param name="count">需要读取的字节数。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>成功读取到 <paramref name="count"/> 字节返回 <c>true</c>；流关闭（读到 0 字节）返回 <c>false</c>。</returns>
    private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct)
    {
        int offset = 0;
        while (offset < count)
        {
            // 每次只读取剩余未填充的部分，避免过读
            var read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct);
            if (read == 0) return false; // 连接已关闭
            offset += read;
        }
        return true;
    }

    /// <summary>
    /// 构造 Modbus TCP FC03（读保持寄存器）请求帧（12 字节）。
    /// 帧结构（MBAP + PDU）：
    /// 事务ID(2) + 协议ID(2, 固定0x0000) + 长度(2, 固定0x0006) + 单元ID(1, 固定0x01) + FC(1, 0x03) + 起始地址(2) + 数量(2)。
    /// </summary>
    /// <param name="tid">事务标识符（Transaction Identifier）。</param>
    /// <param name="startRegister">起始保持寄存器地址。</param>
    /// <param name="count">读取寄存器数量。</param>
    /// <returns>完整的 12 字节 Modbus TCP 请求帧。</returns>
    private static byte[] BuildReadHoldingRegistersRequest(ushort tid, ushort startRegister, ushort count)
    {
        return new byte[]
        {
            (byte)(tid >> 8), (byte)(tid & 0xFF),  // 事务 ID（大端）
            0x00, 0x00,                             // 协议标识符（Modbus = 0x0000）
            0x00, 0x06,                             // 后续字节长度（PDU = 6 字节）
            0x01,                                   // 单元 ID（Unit Identifier）
            0x03,                                   // 功能码 FC03：读保持寄存器
            (byte)(startRegister >> 8), (byte)(startRegister & 0xFF),  // 起始地址（大端）
            (byte)(count >> 8), (byte)(count & 0xFF)                   // 读取数量（大端）
        };
    }

    /// <summary>
    /// 构造 Modbus TCP FC06（写单个寄存器）请求帧（12 字节）。
    /// 帧结构（MBAP + PDU）：
    /// 事务ID(2) + 协议ID(2) + 长度(2) + 单元ID(1) + FC(1, 0x06) + 寄存器地址(2) + 写入值(2)。
    /// </summary>
    /// <param name="tid">事务标识符。</param>
    /// <param name="register">目标保持寄存器地址。</param>
    /// <param name="value">要写入的 16 位无符号整数值。</param>
    /// <returns>完整的 12 字节 Modbus TCP 请求帧。</returns>
    private static byte[] BuildWriteSingleRegisterRequest(ushort tid, ushort register, ushort value)
    {
        return new byte[]
        {
            (byte)(tid >> 8), (byte)(tid & 0xFF),  // 事务 ID（大端）
            0x00, 0x00,                             // 协议标识符
            0x00, 0x06,                             // 后续字节长度
            0x01,                                   // 单元 ID
            0x06,                                   // 功能码 FC06：写单个寄存器
            (byte)(register >> 8), (byte)(register & 0xFF),  // 寄存器地址（大端）
            (byte)(value >> 8), (byte)(value & 0xFF)          // 写入值（大端）
        };
    }
}
