using System.IO.Ports;

namespace HMIexe.Communication.Drivers;

/// <summary>
/// Modbus RTU driver over serial port.
/// Address format: FC:register (e.g. "3:0" reads holding register 0 with FC03).
/// Supports FC01 (coils), FC02 (discrete inputs), FC03 (holding registers), FC04 (input registers).
/// </summary>
public class ModbusRtuDriver : IProtocolDriver
{
    private SerialPort? _port;
    private byte _unitId = 1;

    public string ProtocolName => "Modbus RTU";
    public bool IsConnected => _port?.IsOpen ?? false;

#pragma warning disable CS0067
    public event EventHandler<DataReceivedEventArgs>? DataReceived;
#pragma warning restore CS0067
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public async Task<bool> ConnectAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var portName = parameters.GetValueOrDefault("PortName", "COM1")!;
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

    public async Task DisconnectAsync()
    {
        _port?.Close();
        _port?.Dispose();
        _port = null;
        Disconnected?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    public async Task<object?> ReadAsync(string address, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _port == null) return null;
        try
        {
            var parts = address.Split(':');
            if (parts.Length < 2) return null;
            if (!byte.TryParse(parts[0], out var fc)) return null;
            if (!ushort.TryParse(parts[1], out var reg)) return null;

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
                bool coilValue = Convert.ToBoolean(value);
                return await WriteSingleCoilAsync(reg, coilValue, cancellationToken);
            }
            else if (fc == 6)
            {
                // Write single register (FC06)
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

    public void Dispose()
    {
        _port?.Close();
        _port?.Dispose();
        _port = null;
    }

    // -- helpers --

    private Task<object?> ReadCoilsAsync(ushort startReg, ushort count, CancellationToken ct)
        => ExecuteReadAsync(0x01, startReg, count, response =>
        {
            if (response.Length < 1) return null;
            return (object?)(bool)((response[0] & 0x01) != 0);
        }, ct);

    private Task<object?> ReadDiscreteInputsAsync(ushort startReg, ushort count, CancellationToken ct)
        => ExecuteReadAsync(0x02, startReg, count, response =>
        {
            if (response.Length < 1) return null;
            return (object?)(bool)((response[0] & 0x01) != 0);
        }, ct);

    private Task<object?> ReadHoldingRegistersAsync(ushort startReg, ushort count, CancellationToken ct)
        => ExecuteReadAsync(0x03, startReg, count, response =>
        {
            if (response.Length < 2) return null;
            return (object?)(ushort)((response[0] << 8) | response[1]);
        }, ct);

    private Task<object?> ReadInputRegistersAsync(ushort startReg, ushort count, CancellationToken ct)
        => ExecuteReadAsync(0x04, startReg, count, response =>
        {
            if (response.Length < 2) return null;
            return (object?)(ushort)((response[0] << 8) | response[1]);
        }, ct);

    private async Task<object?> ExecuteReadAsync(byte fc, ushort startReg, ushort count,
        Func<byte[], object?> decode, CancellationToken ct)
    {
        var request = BuildReadRequest(fc, startReg, count);
        _port!.Write(request, 0, request.Length);

        // Read response
        await Task.Delay(50, ct);
        var buf = new byte[256];
        var len = _port.Read(buf, 0, buf.Length);
        if (len < 5) return null;

        // Validate address and FC
        if (buf[0] != _unitId || buf[1] != fc) return null;
        var byteCount = buf[2];
        if (len < 3 + byteCount) return null;

        var data = buf[3..(3 + byteCount)];
        return decode(data);
    }

    private async Task<bool> WriteSingleCoilAsync(ushort reg, bool value, CancellationToken ct)
    {
        var request = new byte[8];
        request[0] = _unitId;
        request[1] = 0x05;
        request[2] = (byte)(reg >> 8);
        request[3] = (byte)(reg & 0xFF);
        request[4] = value ? (byte)0xFF : (byte)0x00;
        request[5] = 0x00;
        var crc = ComputeCrc(request, 6);
        request[6] = crc[0];
        request[7] = crc[1];

        _port!.Write(request, 0, 8);
        await Task.Delay(50, ct);
        return true;
    }

    private async Task<bool> WriteSingleRegisterAsync(ushort reg, ushort value, CancellationToken ct)
    {
        var request = new byte[8];
        request[0] = _unitId;
        request[1] = 0x06;
        request[2] = (byte)(reg >> 8);
        request[3] = (byte)(reg & 0xFF);
        request[4] = (byte)(value >> 8);
        request[5] = (byte)(value & 0xFF);
        var crc = ComputeCrc(request, 6);
        request[6] = crc[0];
        request[7] = crc[1];

        _port!.Write(request, 0, 8);
        await Task.Delay(50, ct);
        return true;
    }

    private byte[] BuildReadRequest(byte fc, ushort startReg, ushort count)
    {
        var request = new byte[8];
        request[0] = _unitId;
        request[1] = fc;
        request[2] = (byte)(startReg >> 8);
        request[3] = (byte)(startReg & 0xFF);
        request[4] = (byte)(count >> 8);
        request[5] = (byte)(count & 0xFF);
        var crc = ComputeCrc(request, 6);
        request[6] = crc[0];
        request[7] = crc[1];
        return request;
    }

    private static byte[] ComputeCrc(byte[] data, int length)
    {
        ushort crc = 0xFFFF;
        for (var i = 0; i < length; i++)
        {
            crc ^= data[i];
            for (var j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) != 0)
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                else
                    crc >>= 1;
            }
        }
        return [(byte)(crc & 0xFF), (byte)(crc >> 8)];
    }
}
