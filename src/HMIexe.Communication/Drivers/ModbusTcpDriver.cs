using System.Net;
using System.Net.Sockets;

namespace HMIexe.Communication.Drivers;

public class ModbusTcpDriver : IProtocolDriver
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private int _transactionId;

    public string ProtocolName => "Modbus TCP";
    public bool IsConnected => _client?.Connected ?? false;

#pragma warning disable CS0067 // Event required by IProtocolDriver interface; raised in polling scenarios
    public event EventHandler<DataReceivedEventArgs>? DataReceived;
#pragma warning restore CS0067
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public async Task<bool> ConnectAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var host = parameters.GetValueOrDefault("Host", "127.0.0.1")!;
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

    public async Task DisconnectAsync()
    {
        _stream?.Close();
        _client?.Close();
        _client = null;
        _stream = null;
        Disconnected?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    public async Task<object?> ReadAsync(string address, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _stream == null) return null;
        try
        {
            var parts = address.Split(':');
            if (parts.Length < 2) return null;
            if (!ushort.TryParse(parts[1], out var register)) return null;

            var tid = (ushort)System.Threading.Interlocked.Increment(ref _transactionId);
            var request = BuildReadHoldingRegistersRequest(tid, register, 1);
            await _stream.WriteAsync(request, cancellationToken);

            // Read MBAP header (6 bytes) + PDU
            var header = new byte[9];
            var headerRead = await ReadExactAsync(_stream, header, 9, cancellationToken);
            if (!headerRead) return null;

            // Validate transaction ID
            var responseTid = (ushort)((header[0] << 8) | header[1]);
            if (responseTid != tid) return null;

            // Check function code (should be 0x03), not an exception (0x83)
            if (header[7] == 0x83) return null; // Modbus exception
            if (header[7] != 0x03) return null;

            // Byte count is header[8]
            var byteCount = header[8];
            if (byteCount < 2) return null;

            var data = new byte[byteCount];
            if (!await ReadExactAsync(_stream, data, byteCount, cancellationToken)) return null;

            return (ushort)((data[0] << 8) | data[1]);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> WriteAsync(string address, object value, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _stream == null) return false;
        try
        {
            var parts = address.Split(':');
            if (parts.Length < 2) return false;
            if (!ushort.TryParse(parts[1], out var register)) return false;
            var writeValue = Convert.ToUInt16(value);

            var tid = (ushort)System.Threading.Interlocked.Increment(ref _transactionId);
            var request = BuildWriteSingleRegisterRequest(tid, register, writeValue);
            await _stream.WriteAsync(request, cancellationToken);

            // Read response header
            var response = new byte[12];
            if (!await ReadExactAsync(_stream, response, 12, cancellationToken)) return false;

            // Validate transaction ID and function code
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

    private static async Task<bool> ReadExactAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct)
    {
        int offset = 0;
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), ct);
            if (read == 0) return false;
            offset += read;
        }
        return true;
    }

    private static byte[] BuildReadHoldingRegistersRequest(ushort tid, ushort startRegister, ushort count)
    {
        return new byte[]
        {
            (byte)(tid >> 8), (byte)(tid & 0xFF),
            0x00, 0x00,
            0x00, 0x06,
            0x01,
            0x03,
            (byte)(startRegister >> 8), (byte)(startRegister & 0xFF),
            (byte)(count >> 8), (byte)(count & 0xFF)
        };
    }

    private static byte[] BuildWriteSingleRegisterRequest(ushort tid, ushort register, ushort value)
    {
        return new byte[]
        {
            (byte)(tid >> 8), (byte)(tid & 0xFF),
            0x00, 0x00,
            0x00, 0x06,
            0x01,
            0x06,
            (byte)(register >> 8), (byte)(register & 0xFF),
            (byte)(value >> 8), (byte)(value & 0xFF)
        };
    }
}
