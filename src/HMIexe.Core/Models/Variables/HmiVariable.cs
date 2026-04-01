using System.ComponentModel;
using System.Text.Json.Serialization;

namespace HMIexe.Core.Models.Variables;

public class HmiVariable : INotifyPropertyChanged
{
    private object? _value;

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public VariableType Type { get; set; } = VariableType.Int;
    public string Group { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object? DefaultValue { get; set; }
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
    public DateTime LastModified { get; set; } = DateTime.Now;

    [JsonIgnore]
    public object? Value
    {
        get => _value;
        set
        {
            if (!Equals(_value, value))
            {
                var oldValue = _value;
                _value = value;
                LastModified = DateTime.Now;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                ValueChanged?.Invoke(this, new VariableValueChangedEventArgs(oldValue, value));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<VariableValueChangedEventArgs>? ValueChanged;
}

public class VariableValueChangedEventArgs : EventArgs
{
    public object? OldValue { get; }
    public object? NewValue { get; }
    public DateTime Timestamp { get; } = DateTime.Now;

    public VariableValueChangedEventArgs(object? oldValue, object? newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}
