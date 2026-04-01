namespace HMIexe.Core.Models.Communication;

public enum ProtocolType
{
    ModbusRtu,
    ModbusTcp,
    OpcUa,
    OpcDa,
    Mqtt,
    WebApi,
    Udp,
    Tcp,
    SerialPort
}

public class CommunicationChannel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public ProtocolType Protocol { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool AutoReconnect { get; set; } = true;
    public int ReconnectIntervalMs { get; set; } = 5000;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public List<DeviceMapping> DeviceMappings { get; set; } = new();
}

public class DeviceMapping
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DeviceName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string VariableId { get; set; } = string.Empty;
    public int PollingIntervalMs { get; set; } = 1000;
}
