using HMIexe.Core.Models.Alarm;
using HMIexe.Core.Models.Variables;
using HMIexe.Core.Services;

namespace HMIexe.Runtime.Services;

/// <summary>
/// 报警条件求值器，负责将报警定义中的条件表达式与变量当前值进行比对，
/// 并在条件满足时触发报警、条件不满足时清除报警。
/// <para>
/// 条件语法示例：<c>[变量名] &gt; 50</c>、<c>[变量名] == true</c>、<c>[变量名] != 0</c>
/// </para>
/// <para>
/// 支持的比较运算符：<c>&gt;</c>、<c>&lt;</c>、<c>&gt;=</c>、<c>&lt;=</c>、<c>==</c>、<c>!=</c>
/// </para>
/// </summary>
public class AlarmConditionEvaluator
{
    /// <summary>变量服务，用于按名称查询变量及监听变量值变化事件。</summary>
    private readonly IVariableService _variableService;

    /// <summary>报警服务，用于触发和清除报警记录。</summary>
    private readonly IAlarmService _alarmService;

    /// <summary>当前加载的报警定义列表，默认为空集合。</summary>
    private IReadOnlyList<HmiAlarmDefinition> _definitions = [];

    /// <summary>
    /// 记录当前处于激活状态的报警定义 ID 集合，
    /// 避免对同一条件重复触发报警。
    /// </summary>
    private readonly HashSet<string> _activeDefinitionIds = new();

    /// <summary>
    /// 初始化 <see cref="AlarmConditionEvaluator"/> 实例，并订阅变量值变化事件。
    /// </summary>
    /// <param name="variableService">变量服务实例。</param>
    /// <param name="alarmService">报警服务实例。</param>
    public AlarmConditionEvaluator(IVariableService variableService, IAlarmService alarmService)
    {
        _variableService = variableService;
        _alarmService = alarmService;
        // 订阅变量值变化事件，变量更新后立即重新求值所有报警条件
        _variableService.VariableValueChanged += OnVariableValueChanged;
    }

    /// <summary>
    /// 设置需要求值的报警定义列表，并立即执行一次全量求值。
    /// 调用此方法会清空当前所有激活状态，以便重新判断。
    /// </summary>
    /// <param name="definitions">报警定义集合。</param>
    public void SetDefinitions(IEnumerable<HmiAlarmDefinition> definitions)
    {
        _definitions = definitions.ToList();
        _activeDefinitionIds.Clear();
        EvaluateAll();
    }

    /// <summary>
    /// 当任意变量值发生变化时触发，驱动全量报警条件重新求值。
    /// </summary>
    private void OnVariableValueChanged(object? sender, VariableValueChangedEventArgs e)
    {
        EvaluateAll();
    }

    /// <summary>
    /// 遍历所有启用的报警定义，对每条定义的条件进行求值：
    /// <list type="bullet">
    ///   <item>条件成立且当前未激活 → 触发报警并记录为激活。</item>
    ///   <item>条件不成立且当前已激活 → 清除对应的活动报警并移出激活集合。</item>
    /// </list>
    /// </summary>
    public void EvaluateAll()
    {
        foreach (var def in _definitions)
        {
            // 跳过未启用或条件为空的报警定义
            if (!def.IsEnabled || string.IsNullOrWhiteSpace(def.Condition)) continue;

            bool conditionMet = EvaluateCondition(def.Condition);
            bool isActive = _activeDefinitionIds.Contains(def.Id);

            if (conditionMet && !isActive)
            {
                // 条件首次满足：标记为激活并触发报警
                _activeDefinitionIds.Add(def.Id);
                _alarmService.RaiseAlarm(def);
            }
            else if (!conditionMet && isActive)
            {
                // 条件已不满足：移除激活标记并清除所有关联的活动报警
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
    /// 对单条条件表达式进行求值。
    /// <para>
    /// 表达式格式：<c>[变量名] 运算符 值</c>，例如：
    /// <c>[Temperature] &gt; 80</c>、<c>[Active] == true</c>
    /// </para>
    /// <para>
    /// 求值顺序：优先尝试布尔比较，其次数值比较，最后字符串比较（不区分大小写）。
    /// </para>
    /// </summary>
    /// <param name="condition">待求值的条件字符串。</param>
    /// <returns>条件满足返回 <c>true</c>，否则返回 <c>false</c>；解析失败也返回 <c>false</c>。</returns>
    private bool EvaluateCondition(string condition)
    {
        try
        {
            condition = condition.Trim();

            // 解析方括号内的变量名，格式必须以 '[' 开头
            var bracketEnd = condition.IndexOf(']');
            if (condition[0] != '[' || bracketEnd < 0) return false;

            var varName = condition.Substring(1, bracketEnd - 1).Trim();
            var rest = condition.Substring(bracketEnd + 1).Trim();

            // 通过变量服务查找变量，若不存在则条件不成立
            var variable = _variableService.GetVariableByName(varName);
            if (variable == null) return false;

            // 按优先级从高到低识别运算符（双字符运算符必须先于单字符判断）
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

            // ── 布尔比较 ──────────────────────────────────────────
            if (bool.TryParse(rhs, out bool rhsBool))
            {
                bool lhsBool = varValue is bool b ? b : Convert.ToBoolean(varValue);
                return op switch { "==" => lhsBool == rhsBool, "!=" => lhsBool != rhsBool, _ => false };
            }

            // ── 数值比较（使用不变区域文化解析，支持小数点） ──────
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
                    // 浮点相等比较使用极小误差阈值，避免精度问题
                    "==" => Math.Abs(lhsNum - rhsNum) < 1e-9,
                    "!=" => Math.Abs(lhsNum - rhsNum) >= 1e-9,
                    _ => false
                };
            }

            // ── 字符串比较（不区分大小写） ────────────────────────
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
            // 任何解析或转换异常均视为条件不满足
            return false;
        }
    }
}
