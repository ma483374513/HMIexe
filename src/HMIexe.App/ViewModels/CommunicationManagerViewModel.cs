/// <summary>
/// 通信管理器视图模型文件。
/// 负责通信通道的配置与管理、设备地址映射的维护，以及通道连接/断开和读写测试操作。
/// </summary>
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Communication;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

/// <summary>
/// 通信管理器视图模型。
/// 提供通信通道的增删、连接状态管理、设备地址映射配置，
/// 以及通过指定通道进行寄存器读写测试的功能。
/// </summary>
public partial class CommunicationManagerViewModel : ObservableObject
{
    /// <summary>通信业务服务，提供通道管理、连接控制和数据读写能力。</summary>
    private readonly ICommunicationService _communicationService;

    /// <summary>对话框服务，用于显示操作结果提示和错误信息。</summary>
    private readonly IDialogService _dialogService;

    /// <summary>当前已配置的所有通信通道集合，绑定到通道列表视图。</summary>
    public ObservableCollection<CommunicationChannel> Channels { get; } = new();

    /// <summary>当前选中通道的设备地址映射集合，绑定到映射列表视图。</summary>
    public ObservableCollection<DeviceMapping> CurrentMappings { get; } = new();

    /// <summary>当前在通道列表中选中的通信通道。</summary>
    [ObservableProperty]
    private CommunicationChannel? _selectedChannel;

    /// <summary>当前在映射列表中选中的设备地址映射。</summary>
    [ObservableProperty]
    private DeviceMapping? _selectedMapping;

    /// <summary>当前选中通道的连接状态文本，绑定到状态标签。</summary>
    [ObservableProperty]
    private string _channelStatusText = "未连接";

    /// <summary>读取测试的目标寄存器地址。</summary>
    [ObservableProperty]
    private string _testReadAddress = string.Empty;

    /// <summary>读取测试的结果显示文本。</summary>
    [ObservableProperty]
    private string _testReadResult = string.Empty;

    /// <summary>写入测试的目标寄存器地址。</summary>
    [ObservableProperty]
    private string _testWriteAddress = string.Empty;

    /// <summary>写入测试的目标值。</summary>
    [ObservableProperty]
    private string _testWriteValue = string.Empty;

    /// <summary>所有支持的通信协议类型枚举值列表，绑定到协议下拉框。</summary>
    public IReadOnlyList<ProtocolType> ProtocolTypes { get; } = Enum.GetValues<ProtocolType>();

    /// <summary>
    /// 初始化通信管理器视图模型。
    /// 订阅数据接收事件，并从服务加载已有通道列表。
    /// </summary>
    /// <param name="communicationService">通信业务服务。</param>
    /// <param name="dialogService">UI 对话框服务。</param>
    public CommunicationManagerViewModel(ICommunicationService communicationService, IDialogService dialogService)
    {
        _communicationService = communicationService;
        _dialogService = dialogService;

        _communicationService.DataReceived += OnDataReceived;

        foreach (var ch in _communicationService.Channels)
            Channels.Add(ch);
    }

    /// <summary>
    /// 选中通道变更时的响应方法。
    /// 更新当前映射列表，并查询通道实时连接状态以刷新状态文本。
    /// </summary>
    /// <param name="value">新选中的通信通道。</param>
    partial void OnSelectedChannelChanged(CommunicationChannel? value)
    {
        CurrentMappings.Clear();
        if (value == null)
        {
            ChannelStatusText = "未连接";
            return;
        }
        foreach (var m in value.DeviceMappings)
            CurrentMappings.Add(m);

        // 将枚举状态转换为中文显示文本
        var status = _communicationService.GetChannelStatus(value.Id);
        ChannelStatusText = status switch
        {
            ConnectionStatus.Connected => "已连接",
            ConnectionStatus.Connecting => "连接中...",
            ConnectionStatus.Error => "错误",
            _ => "未连接"
        };
    }

    /// <summary>
    /// 通信数据接收事件处理器。
    /// 若接收到的数据属于当前选中通道，则更新读取结果文本。
    /// </summary>
    /// <param name="sender">事件发送者。</param>
    /// <param name="e">包含通道 ID、地址、值和时间戳的事件参数。</param>
    private void OnDataReceived(object? sender, CommunicationDataEventArgs e)
    {
        if (SelectedChannel?.Id == e.ChannelId)
            TestReadResult = $"[{e.Timestamp:HH:mm:ss}] {e.Address} = {e.Value}";
    }

    /// <summary>
    /// 添加一个新的通信通道，默认使用 Modbus TCP 协议，并自动选中它。
    /// </summary>
    [RelayCommand]
    private void AddChannel()
    {
        var ch = new CommunicationChannel
        {
            Name = $"Channel{Channels.Count + 1}",
            Protocol = ProtocolType.ModbusTcp,
            IsEnabled = true,
            AutoReconnect = true,
            ReconnectIntervalMs = 5000,
            Parameters = new() { ["Host"] = "127.0.0.1", ["Port"] = "502" }
        };
        Channels.Add(ch);
        SelectedChannel = ch;
    }

    /// <summary>
    /// 删除当前选中的通信通道。
    /// </summary>
    [RelayCommand]
    private void RemoveChannel()
    {
        if (SelectedChannel == null) return;
        Channels.Remove(SelectedChannel);
        SelectedChannel = Channels.FirstOrDefault();
    }

    /// <summary>
    /// 异步连接当前选中的通信通道，并更新状态文本。
    /// </summary>
    [RelayCommand]
    private async Task ConnectChannel()
    {
        if (SelectedChannel == null) return;
        ChannelStatusText = "连接中...";
        try
        {
            var ok = await _communicationService.ConnectAsync(SelectedChannel.Id);
            ChannelStatusText = ok ? "已连接" : "连接失败";
        }
        catch (Exception ex)
        {
            ChannelStatusText = $"错误: {ex.Message}";
        }
    }

    /// <summary>
    /// 断开当前选中通信通道的连接。
    /// </summary>
    [RelayCommand]
    private async Task DisconnectChannel()
    {
        if (SelectedChannel == null) return;
        await _communicationService.DisconnectAsync(SelectedChannel.Id);
        ChannelStatusText = "未连接";
    }

    /// <summary>
    /// 从当前选中通道读取指定地址的寄存器值，结果显示在 <see cref="TestReadResult"/> 中。
    /// </summary>
    [RelayCommand]
    private async Task TestRead()
    {
        if (SelectedChannel == null || string.IsNullOrWhiteSpace(TestReadAddress)) return;
        TestReadResult = "读取中...";
        try
        {
            var value = await _communicationService.ReadValueAsync(SelectedChannel.Id, TestReadAddress);
            TestReadResult = value != null ? $"{TestReadAddress} = {value}" : "读取失败(返回null)";
        }
        catch (Exception ex)
        {
            TestReadResult = $"错误: {ex.Message}";
        }
    }

    /// <summary>
    /// 向当前选中通道的指定地址写入测试值，并通过对话框显示写入结果。
    /// </summary>
    [RelayCommand]
    private async Task TestWrite()
    {
        if (SelectedChannel == null || string.IsNullOrWhiteSpace(TestWriteAddress)) return;
        try
        {
            var ok = await _communicationService.WriteValueAsync(SelectedChannel.Id, TestWriteAddress, TestWriteValue);
            await _dialogService.ShowMessageAsync("写入", ok ? "写入成功" : "写入失败");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync("错误", $"写入错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 在当前选中通道中添加一个新的设备地址映射项。
    /// </summary>
    [RelayCommand]
    private void AddMapping()
    {
        if (SelectedChannel == null) return;
        var mapping = new DeviceMapping
        {
            DeviceName = $"Device{CurrentMappings.Count + 1}",
            Address = "1:0",
            PollingIntervalMs = 1000
        };
        SelectedChannel.DeviceMappings.Add(mapping);
        CurrentMappings.Add(mapping);
        SelectedMapping = mapping;
    }

    /// <summary>
    /// 从当前选中通道中移除选中的设备地址映射项。
    /// </summary>
    [RelayCommand]
    private void RemoveMapping()
    {
        if (SelectedMapping == null || SelectedChannel == null) return;
        SelectedChannel.DeviceMappings.Remove(SelectedMapping);
        CurrentMappings.Remove(SelectedMapping);
        SelectedMapping = CurrentMappings.FirstOrDefault();
    }

    /// <summary>
    /// 从工程数据加载通信通道列表，替换当前内容。
    /// </summary>
    /// <param name="channels">工程中保存的通信通道集合。</param>
    public void LoadFromProject(IEnumerable<CommunicationChannel> channels)
    {
        Channels.Clear();
        foreach (var ch in channels)
            Channels.Add(ch);
        SelectedChannel = Channels.FirstOrDefault();
    }
}
