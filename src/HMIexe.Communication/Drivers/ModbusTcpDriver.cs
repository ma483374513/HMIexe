using System.Net.Sockets;
using System.Net;

namespace HMIexe.Communication.Drivers;

public class ModbusTcpDriver : IProtocolDriver
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private ushort _transactionId;

    public string ProtocolName => "Modbus TCP";
    public bool IsConnected => _client?.Connected ?? false;

    public event EventHandler<DataReceivedEventArgs>? DataReceived;
    public event EventHandler? Connected;
    public event EventHandler? Disconnected;

    public async Task<bool> ConnectAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var host = parameters.GetValueOrDefault("Host", "127.0.0.1");
            var port = int.Parse(parameters.GetValueOrDefault("Port", "502")!);

            _client = new TcpClient();
            await _client.ConnectAsync(IPAddress.Parse(host!), port, cancellationToken);
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
            var register = ushort.Parse(parts[1]);

            var request = BuildReadHoldingRegistersRequest(register, 1);
            await _stream.WriteAsync(request, cancellationToken);

            var response = new byte[256];
            var bytesRead = await _stream.ReadAsync(response, cancellationToken);
            if (bytesRead >= 11)
            {
                var value = (ushort)((response[9] << 8) | response[10]);
                return value;
            }
            return null;
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
            var register = ushort.Parse(parts[1]);
            var writeValue = Convert.ToUInt16(value);

            var request = BuildWriteSingleRegisterRequest(register, writeValue);
            await _stream.WriteAsync(request, cancellationToken);

            var response = new byte[256];
            await _stream.ReadAsync(response, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private byte[] BuildReadHoldingRegistersRequest(ushort startRegister, ushort count)
    {
        var tid = ++_transactionId;
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

    private byte[] BuildWriteSingleRegisterRequest(ushort register, ushort value)
    {
        var tid = ++_transactionId;
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
