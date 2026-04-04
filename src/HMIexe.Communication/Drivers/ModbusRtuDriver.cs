using System.IO.Ports;

/// <summary>
/// 提供基于串口的 Modbus RTU 协议驱动实现。
/// 地址格式为 "功能码:寄存器号"（例如 "3:0" 表示使用 FC03 读取保持寄存器 0）。
/// 支持 FC01（线圈）、FC02（离散输入）、FC03（保持寄存器）、FC04（输入寄存器）读取，
/// 以及 FC05（写单个线圈）、FC06（写单个寄存器）写入操作。
/// </summary>
namespace HMIexe.Communication.Drivers;

/// <summary>
/// Modbus RTU driver over serial port.
/// Address format: FC:register (e.g. "3:0" reads holding register 0 with FC03).
/// Supports FC01 (coils), FC02 (discrete inputs), FC03 (holding registers), FC04 (input registers).
/// </summary>
public class ModbusRtuDriver : IProtocolDriver
{
    /// <summary>
    /// 串口通信对象，负责底层字节的发送与接收。
    /// </summary>
    private SerialPort? _port;

    /// <summary>
    /// Modbus 从站单元标识符（Unit ID），用于区分总线上的不同从设备，默认值为 1。
    /// </summary>
    private byte _unitId = 1;

    /// <summary>
    /// 发送请求后等待从站响应的超时时间（毫秒），默认 50 ms。
    /// </summary>
    private int _readTimeoutMs = 50;

    /// <summary>
    /// 获取协议名称。
    /// </summary>
    public string ProtocolName => "Modbus RTU";

    /// <summary>
    /// 获取当前串口连接状态；<c>true</c> 表示串口已打开。
    /// </summary>
    public bool IsConnected => _port?.IsOpen ?? false;

    // DataReceived is required by IProtocolDriver but not used in RTU polling mode;
    // callers should use Read/WriteAsync directly for Modbus RTU.
    // DataReceived 事件由接口契约要求声明，但在 RTU 轮询模式下不使用；
    // 调用方应直接使用 ReadAsync/WriteAsync 进行数据交互。
#pragma warning disable CS0067
    /// <summary>
    /// 数据接收事件（RTU 轮询模式下未使用，保留以满足接口约定）。
    /// </summary>
    public event EventHandler<DataReceivedEventArgs>? DataReceived;
#pragma warning restore CS0067

    /// <summary>
    /// 串口成功打开后触发的连接事件。
    /// </summary>
    public event EventHandler? Connected;

    /// <summary>
    /// 串口关闭后触发的断开事件。
    /// </summary>
    public event EventHandler? Disconnected;

    /// <summary>
    /// 异步建立串口连接，从参数字典中解析串口配置并打开端口。
    /// </summary>
    /// <param name="parameters">
    /// 连接参数字典，支持以下键：
    /// <list type="bullet">
    ///   <item><description><b>PortName</b>：串口名称，默认 "COM1"</description></item>
    ///   <item><description><b>BaudRate</b>：波特率，默认 9600</description></item>
    ///   <item><description><b>DataBits</b>：数据位，默认 8</description></item>
    ///   <item><description><b>StopBits</b>：停止位，默认 "One"</description></item>
    ///   <item><description><b>Parity</b>：校验方式，默认 "None"</description></item>
    ///   <item><description><b>UnitId</b>：从站 ID，默认 1</description></item>
    ///   <item><description><b>ReadTimeoutMs</b>：读超时（毫秒），默认 50</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
    /// <returns>连接成功返回 <c>true</c>，发生异常时返回 <c>false</c>。</returns>
    public async Task<bool> ConnectAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var portName = parameters.GetValueOrDefault("PortName", "COM1");
            if (!int.TryParse(parameters.GetValueOrDefault("BaudRate", "9600"), out var baud))
                baud = 9600;
            if (!int.TryParse(parameters.GetValueOrDefault("DataBits", "8"), out var dataBits))
                dataBits = 8;
            if (!Enum.TryParse<StopBits>(parameters.GetValueOrDefault("StopBits", "One"), out var stopBits))
                stopBits = StopBits.One;
            if (!Enum.TryParse<Parity>(parameters.GetValueOrDefault("Parity", "None"), out var parity))
                parity = Parity.None;
            if (!byte.TryParse(parameters.GetValueOrDefault("UnitId", "1"), out _unitId))
                _unitId = 1;
            if (!int.TryParse(parameters.GetValueOrDefault("ReadTimeoutMs", "50"), out _readTimeoutMs))
                _readTimeoutMs = 50;

            _port = new SerialPort(portName, baud, parity, dataBits, stopBits)
            {
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };
            _port.Open();
            Connected?.Invoke(this, EventArgs.Empty);
            return await Task.FromResult(true);
        }
        catch
        {
            return await Task.FromResult(false);
        }
    }

    /// <summary>
    /// 异步关闭串口并释放相关资源，触发断开事件。
    /// </summary>
    public async Task DisconnectAsync()
    {
        _port?.Close();
        _port?.Dispose();
        _port = null;
        Disconnected?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    /// <summary>
    /// 异步从指定地址读取数据，根据功能码分派到对应的读取方法。
    /// </summary>
    /// <param name="address">
    /// 地址字符串，格式为 "功能码:寄存器号"（例如 "3:100" 表示 FC03 读保持寄存器 100）。
    /// </param>
    /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
    /// <returns>读取成功时返回数据值（<see cref="bool"/> 或 <see cref="ushort"/>）；失败返回 <c>null</c>。</returns>
    public async Task<object?> ReadAsync(string address, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _port == null) return null;
        try
        {
            var parts = address.Split(':');
            if (parts.Length < 2) return null;
            if (!byte.TryParse(parts[0], out var fc)) return null;
            if (!ushort.TryParse(parts[1], out var reg)) return null;

            // 根据功能码将读取请求分派到对应的私有方法
            return fc switch
            {
                1 => await ReadCoilsAsync(reg, 1, cancellationToken),
                2 => await ReadDiscreteInputsAsync(reg, 1, cancellationToken),
                3 => await ReadHoldingRegistersAsync(reg, 1, cancellationToken),
                4 => await ReadInputRegistersAsync(reg, 1, cancellationToken),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 异步向指定地址写入数据，支持写单个线圈（FC05）和写单个寄存器（FC06）。
    /// </summary>
    /// <param name="address">
    /// 地址字符串，格式为 "功能码:寄存器号"（例如 "5:0" 表示 FC05 写线圈 0，"6:10" 表示 FC06 写寄存器 10）。
    /// </param>
    /// <param name="value">要写入的值；FC05 时应为 <see cref="bool"/>，FC06 时应可转换为 <see cref="ushort"/>。</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
    /// <returns>写入成功返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    public async Task<bool> WriteAsync(string address, object value, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _port == null) return false;
        try
        {
            var parts = address.Split(':');
            if (parts.Length < 2) return false;
            if (!byte.TryParse(parts[0], out var fc)) return false;
            if (!ushort.TryParse(parts[1], out var reg)) return false;

            if (fc == 5)
            {
                // Write single coil (FC05)
                // 写单个线圈（FC05）：将值转换为 bool 后发送 FC05 帧
                bool coilValue = Convert.ToBoolean(value);
                return await WriteSingleCoilAsync(reg, coilValue, cancellationToken);
            }
            else if (fc == 6)
            {
                // Write single register (FC06)
                // 写单个寄存器（FC06）：将值转换为 ushort 后发送 FC06 帧
                ushort regValue = Convert.ToUInt16(value);
                return await WriteSingleRegisterAsync(reg, regValue, cancellationToken);
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 释放串口资源（关闭并销毁串口对象）。
    /// </summary>
    public void Dispose()
    {
        _port?.Close();
        _port?.Dispose();
        _port = null;
    }

    // -- helpers --
    // -- 私有辅助方法 --

    /// <summary>
    /// 使用 FC01 读取线圈状态，返回第一个线圈的布尔值。
    /// </summary>
    /// <param name="startReg">起始线圈地址。</param>
    /// <param name="count">读取线圈数量。</param>
    /// <param name="ct">取消令牌。</param>
    private Task<object?> ReadCoilsAsync(ushort startReg, ushort count, CancellationToken ct)
        => ExecuteReadAsync(0x01, startReg, count, response =>
        {
            if (response.Length < 1) return null;
            // 取响应数据字节的最低位作为线圈状态
            return (object?)(bool)((response[0] & 0x01) != 0);
        }, ct);

    /// <summary>
    /// 使用 FC02 读取离散输入状态，返回第一个输入的布尔值。
    /// </summary>
    /// <param name="startReg">起始离散输入地址。</param>
    /// <param name="count">读取数量。</param>
    /// <param name="ct">取消令牌。</param>
    private Task<object?> ReadDiscreteInputsAsync(ushort startReg, ushort count, CancellationToken ct)
        => ExecuteReadAsync(0x02, startReg, count, response =>
        {
            if (response.Length < 1) return null;
            // 取响应数据字节的最低位作为离散输入状态
            return (object?)(bool)((response[0] & 0x01) != 0);
        }, ct);

    /// <summary>
    /// 使用 FC03 读取保持寄存器，返回第一个寄存器的 16 位无符号整数值。
    /// </summary>
    /// <param name="startReg">起始寄存器地址。</param>
    /// <param name="count">读取寄存器数量。</param>
    /// <param name="ct">取消令牌。</param>
    private Task<object?> ReadHoldingRegistersAsync(ushort startReg, ushort count, CancellationToken ct)
        => ExecuteReadAsync(0x03, startReg, count, response =>
        {
            if (response.Length < 2) return null;
            // Modbus 采用大端字节序：高字节在前，低字节在后
            return (object?)(ushort)((response[0] << 8) | response[1]);
        }, ct);

    /// <summary>
    /// 使用 FC04 读取输入寄存器，返回第一个寄存器的 16 位无符号整数值。
    /// </summary>
    /// <param name="startReg">起始寄存器地址。</param>
    /// <param name="count">读取寄存器数量。</param>
    /// <param name="ct">取消令牌。</param>
    private Task<object?> ReadInputRegistersAsync(ushort startReg, ushort count, CancellationToken ct)
        => ExecuteReadAsync(0x04, startReg, count, response =>
        {
            if (response.Length < 2) return null;
            // Modbus 大端字节序拼合 16 位值
            return (object?)(ushort)((response[0] << 8) | response[1]);
        }, ct);

    /// <summary>
    /// 构造并发送 Modbus RTU 读取请求，等待响应后校验帧完整性与 CRC，
    /// 再将原始数据字节交由 <paramref name="decode"/> 委托解码为目标类型。
    /// </summary>
    /// <param name="fc">Modbus 功能码（0x01~0x04）。</param>
    /// <param name="startReg">起始寄存器/线圈地址。</param>
    /// <param name="count">读取数量。</param>
    /// <param name="decode">将原始响应数据字节转换为业务值的委托。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>解码后的值；任何校验失败均返回 <c>null</c>。</returns>
    private async Task<object?> ExecuteReadAsync(byte fc, ushort startReg, ushort count,
        Func<byte[], object?> decode, CancellationToken ct)
    {
        var request = BuildReadRequest(fc, startReg, count);
        _port!.Write(request, 0, request.Length);

        // Wait for response with configurable timeout
        // 等待从站响应，延迟时间由 _readTimeoutMs 配置
        await Task.Delay(_readTimeoutMs, ct);
        var buf = new byte[256];
        var len = _port.Read(buf, 0, buf.Length);

        // Minimum valid response: unitId(1) + fc(1) + byteCount(1) + data + crc(2) = at least 5
        // 最小有效响应帧：单元ID(1) + 功能码(1) + 字节计数(1) + 数据 + CRC(2) = 至少 5 字节
        if (len < 5) return null;

        // Validate unit ID and function code
        // 校验响应帧的从站 ID 与功能码是否与请求一致
        if (buf[0] != _unitId || buf[1] != fc) return null;
        var byteCount = buf[2];
        var expectedLen = 3 + byteCount + 2; // header + data + CRC
        if (len < expectedLen) return null;

        // Validate CRC over entire response (excluding the CRC bytes themselves)
        // 对响应帧头+数据部分重新计算 CRC，与帧尾的 CRC 字节比对
        var responseCrc = ComputeCrc(buf, 3 + byteCount);
        if (buf[3 + byteCount] != responseCrc[0] || buf[3 + byteCount + 1] != responseCrc[1])
            return null;

        // 提取数据区字节并交由解码委托处理
        var data = buf[3..(3 + byteCount)];
        return decode(data);
    }

    /// <summary>
    /// 发送 FC05 写单个线圈请求，并验证从站的回显响应。
    /// </summary>
    /// <param name="reg">目标线圈地址。</param>
    /// <param name="value"><c>true</c> 置位（0xFF00），<c>false</c> 清位（0x0000）。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>写入成功（收到正确回显）返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    private async Task<bool> WriteSingleCoilAsync(ushort reg, bool value, CancellationToken ct)
    {
        var request = new byte[8];
        request[0] = _unitId;
        request[1] = 0x05;
        request[2] = (byte)(reg >> 8);
        request[3] = (byte)(reg & 0xFF);
        // Modbus FC05 规范：ON = 0xFF00，OFF = 0x0000（高字节）
        request[4] = value ? (byte)0xFF : (byte)0x00;
        request[5] = 0x00;
        var crc = ComputeCrc(request, 6);
        request[6] = crc[0];
        request[7] = crc[1];

        _port!.Write(request, 0, 8);
        await Task.Delay(_readTimeoutMs, ct);

        // Validate echo response (FC05 reply mirrors the request)
        // FC05 的响应帧与请求帧完全相同（回显），校验单元 ID 和功能码即可
        var buf = new byte[8];
        var len = _port.BytesToRead >= 8 ? _port.Read(buf, 0, 8) : 0;
        return len == 8 && buf[0] == _unitId && buf[1] == 0x05;
    }

    /// <summary>
    /// 发送 FC06 写单个保持寄存器请求，并验证从站的回显响应。
    /// </summary>
    /// <param name="reg">目标寄存器地址。</param>
    /// <param name="value">要写入的 16 位无符号整数值。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>写入成功（收到正确回显）返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    private async Task<bool> WriteSingleRegisterAsync(ushort reg, ushort value, CancellationToken ct)
    {
        var request = new byte[8];
        request[0] = _unitId;
        request[1] = 0x06;
        request[2] = (byte)(reg >> 8);
        request[3] = (byte)(reg & 0xFF);
        // 寄存器值按大端字节序填充
        request[4] = (byte)(value >> 8);
        request[5] = (byte)(value & 0xFF);
        var crc = ComputeCrc(request, 6);
        request[6] = crc[0];
        request[7] = crc[1];

        _port!.Write(request, 0, 8);
        await Task.Delay(_readTimeoutMs, ct);

        // Validate echo response (FC06 reply mirrors the request)
        // FC06 的响应帧与请求帧完全相同（回显），校验单元 ID 和功能码即可
        var buf = new byte[8];
        var len = _port.BytesToRead >= 8 ? _port.Read(buf, 0, 8) : 0;
        return len == 8 && buf[0] == _unitId && buf[1] == 0x06;
    }

    /// <summary>
    /// 构造标准的 Modbus RTU 读取请求帧（8 字节）。
    /// 帧结构：单元ID + 功能码 + 起始地址高字节 + 起始地址低字节 + 数量高字节 + 数量低字节 + CRC低字节 + CRC高字节。
    /// </summary>
    /// <param name="fc">功能码。</param>
    /// <param name="startReg">起始寄存器/线圈地址。</param>
    /// <param name="count">读取数量。</param>
    /// <returns>完整的 8 字节请求帧（含 CRC）。</returns>
    private byte[] BuildReadRequest(byte fc, ushort startReg, ushort count)
    {
        var request = new byte[8];
        request[0] = _unitId;
        request[1] = fc;
        // 起始地址高低字节（大端字节序）
        request[2] = (byte)(startReg >> 8);
        request[3] = (byte)(startReg & 0xFF);
        // 读取数量高低字节（大端字节序）
        request[4] = (byte)(count >> 8);
        request[5] = (byte)(count & 0xFF);
        var crc = ComputeCrc(request, 6);
        request[6] = crc[0];
        request[7] = crc[1];
        return request;
    }

    /// <summary>
    /// 计算 Modbus RTU CRC-16 校验值。
    /// 算法：初始值 0xFFFF，多项式 0xA001（反向多项式），逐位处理。
    /// </summary>
    /// <param name="data">待计算的字节数组。</param>
    /// <param name="length">参与计算的字节数（从索引 0 开始）。</param>
    /// <returns>2 字节 CRC 数组；索引 0 为低字节，索引 1 为高字节（符合 RTU 帧尾顺序）。</returns>
    private static byte[] ComputeCrc(byte[] data, int length)
    {
        ushort crc = 0xFFFF;
        for (var i = 0; i < length; i++)
        {
            crc ^= data[i];
            for (var j = 0; j < 8; j++)
            {
                // 若最低位为 1，则右移后与反向多项式 0xA001 异或
                if ((crc & 0x0001) != 0)
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                else
                    crc >>= 1;
            }
        }
        // RTU 帧 CRC 字节序：低字节在前，高字节在后
        return [(byte)(crc & 0xFF), (byte)(crc >> 8)];
    }
}
