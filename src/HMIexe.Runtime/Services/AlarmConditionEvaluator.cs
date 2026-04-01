using HMIexe.Core.Models.Alarm;
using HMIexe.Core.Models.Variables;
using HMIexe.Core.Services;

namespace HMIexe.Runtime.Services;

/// <summary>
/// Evaluates alarm conditions against variable values and triggers/clears alarms.
/// Condition syntax: "[VarName] > 50", "[VarName] == true", "[VarName] != 0", etc.
/// Supported operators: >, less-than, >=, less-than-or-equal, ==, !=
/// </summary>
public class AlarmConditionEvaluator
{
    private readonly IVariableService _variableService;
    private readonly IAlarmService _alarmService;
    private IReadOnlyList<HmiAlarmDefinition> _definitions = [];
    // Track which definitions are currently active so we don't re-raise them
    private readonly HashSet<string> _activeDefinitionIds = new();

    public AlarmConditionEvaluator(IVariableService variableService, IAlarmService alarmService)
    {
        _variableService = variableService;
        _alarmService = alarmService;
        _variableService.VariableValueChanged += OnVariableValueChanged;
    }

    /// <summary>Set the current list of alarm definitions to evaluate against.</summary>
    public void SetDefinitions(IEnumerable<HmiAlarmDefinition> definitions)
    {
        _definitions = definitions.ToList();
        _activeDefinitionIds.Clear();
        EvaluateAll();
    }

    private void OnVariableValueChanged(object? sender, VariableValueChangedEventArgs e)
    {
        EvaluateAll();
    }

    /// <summary>Evaluate all alarm definitions and trigger/clear as appropriate.</summary>
    public void EvaluateAll()
    {
        foreach (var def in _definitions)
        {
            if (!def.IsEnabled || string.IsNullOrWhiteSpace(def.Condition)) continue;

            bool conditionMet = EvaluateCondition(def.Condition);
            bool isActive = _activeDefinitionIds.Contains(def.Id);

            if (conditionMet && !isActive)
            {
                _activeDefinitionIds.Add(def.Id);
                _alarmService.RaiseAlarm(def);
            }
            else if (!conditionMet && isActive)
            {
                _activeDefinitionIds.Remove(def.Id);
                var toClose = _alarmService.ActiveAlarms
                    .Where(a => a.AlarmDefinitionId == def.Id)
                    .ToList();
                foreach (var a in toClose)
                    _alarmService.ClearAlarm(a.Id);
            }
        }
    }

    /// <summary>
    /// Evaluates a single condition expression.
    /// Supported format: "[VariableName] operator value"
    /// e.g. "[Temperature] > 80", "[Active] == true"
    /// </summary>
    private bool EvaluateCondition(string condition)
    {
        try
        {
            condition = condition.Trim();

            var bracketEnd = condition.IndexOf(']');
            if (condition[0] != '[' || bracketEnd < 0) return false;

            var varName = condition.Substring(1, bracketEnd - 1).Trim();
            var rest = condition.Substring(bracketEnd + 1).Trim();

            var variable = _variableService.GetVariableByName(varName);
            if (variable == null) return false;

            string op;
            string rhs;
            if (rest.StartsWith(">=")) { op = ">="; rhs = rest.Substring(2).Trim(); }
            else if (rest.StartsWith("<=")) { op = "<="; rhs = rest.Substring(2).Trim(); }
            else if (rest.StartsWith("!=")) { op = "!="; rhs = rest.Substring(2).Trim(); }
            else if (rest.StartsWith("==")) { op = "=="; rhs = rest.Substring(2).Trim(); }
            else if (rest.StartsWith(">")) { op = ">"; rhs = rest.Substring(1).Trim(); }
            else if (rest.StartsWith("<")) { op = "<"; rhs = rest.Substring(1).Trim(); }
            else return false;

            var varValue = variable.Value;

            if (bool.TryParse(rhs, out bool rhsBool))
            {
                bool lhsBool = varValue is bool b ? b : Convert.ToBoolean(varValue);
                return op switch { "==" => lhsBool == rhsBool, "!=" => lhsBool != rhsBool, _ => false };
            }

            if (double.TryParse(rhs, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double rhsNum))
            {
                double lhsNum;
                try { lhsNum = Convert.ToDouble(varValue); }
                catch { return false; }

                return op switch
                {
                    ">" => lhsNum > rhsNum,
                    "<" => lhsNum < rhsNum,
                    ">=" => lhsNum >= rhsNum,
                    "<=" => lhsNum <= rhsNum,
                    "==" => Math.Abs(lhsNum - rhsNum) < 1e-9,
                    "!=" => Math.Abs(lhsNum - rhsNum) >= 1e-9,
                    _ => false
                };
            }

            var lhsStr = varValue?.ToString() ?? string.Empty;
            return op switch
            {
                "==" => string.Equals(lhsStr, rhs, StringComparison.OrdinalIgnoreCase),
                "!=" => !string.Equals(lhsStr, rhs, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }
}
