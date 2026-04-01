using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HMIexe.App.Services;
using HMIexe.Core.Models.Communication;
using HMIexe.Core.Services;

namespace HMIexe.App.ViewModels;

public partial class CommunicationManagerViewModel : ObservableObject
{
    private readonly ICommunicationService _communicationService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<CommunicationChannel> Channels { get; } = new();
    public ObservableCollection<DeviceMapping> CurrentMappings { get; } = new();

    [ObservableProperty]
    private CommunicationChannel? _selectedChannel;

    [ObservableProperty]
    private DeviceMapping? _selectedMapping;

    [ObservableProperty]
    private string _channelStatusText = "未连接";

    [ObservableProperty]
    private string _testReadAddress = string.Empty;

    [ObservableProperty]
    private string _testReadResult = string.Empty;

    [ObservableProperty]
    private string _testWriteAddress = string.Empty;

    [ObservableProperty]
    private string _testWriteValue = string.Empty;

    public IReadOnlyList<string> ProtocolTypes { get; } = Enum.GetNames<ProtocolType>();

    public CommunicationManagerViewModel(ICommunicationService communicationService, IDialogService dialogService)
    {
        _communicationService = communicationService;
        _dialogService = dialogService;

        _communicationService.DataReceived += OnDataReceived;

        foreach (var ch in _communicationService.Channels)
            Channels.Add(ch);
    }

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

        var status = _communicationService.GetChannelStatus(value.Id);
        ChannelStatusText = status switch
        {
            ConnectionStatus.Connected => "已连接",
            ConnectionStatus.Connecting => "连接中...",
            ConnectionStatus.Error => "错误",
            _ => "未连接"
        };
    }

    private void OnDataReceived(object? sender, CommunicationDataEventArgs e)
    {
        if (SelectedChannel?.Id == e.ChannelId)
            TestReadResult = $"[{e.Timestamp:HH:mm:ss}] {e.Address} = {e.Value}";
    }

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

    [RelayCommand]
    private void RemoveChannel()
    {
        if (SelectedChannel == null) return;
        Channels.Remove(SelectedChannel);
        SelectedChannel = Channels.FirstOrDefault();
    }

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

    [RelayCommand]
    private async Task DisconnectChannel()
    {
        if (SelectedChannel == null) return;
        await _communicationService.DisconnectAsync(SelectedChannel.Id);
        ChannelStatusText = "未连接";
    }

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

    [RelayCommand]
    private void RemoveMapping()
    {
        if (SelectedMapping == null || SelectedChannel == null) return;
        SelectedChannel.DeviceMappings.Remove(SelectedMapping);
        CurrentMappings.Remove(SelectedMapping);
        SelectedMapping = CurrentMappings.FirstOrDefault();
    }

    public void LoadFromProject(IEnumerable<CommunicationChannel> channels)
    {
        Channels.Clear();
        foreach (var ch in channels)
            Channels.Add(ch);
        SelectedChannel = Channels.FirstOrDefault();
    }
}
