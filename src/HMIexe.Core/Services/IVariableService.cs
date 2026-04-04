using HMIexe.Core.Models.Variables;

/// <summary>
/// 变量服务接口定义。
/// 提供 HMI 变量的增删改查、批量导入/导出及值变更事件通知等能力，
/// 是运行时变量管理子系统的核心契约。
/// </summary>
namespace HMIexe.Core.Services;

/// <summary>
/// HMI 变量服务接口，统一管理项目中所有 HMI 变量的运行时状态。
/// 消费者可订阅 <see cref="VariableValueChanged"/> 事件以响应任意变量的值变化。
/// </summary>
public interface IVariableService
{
    /// <summary>当前已注册的所有 HMI 变量的只读列表。</summary>
    IReadOnlyList<HmiVariable> Variables { get; }

    /// <summary>
    /// 根据变量 ID 查找变量。
    /// </summary>
    /// <param name="id">目标变量的唯一标识符。</param>
    /// <returns>找到返回对应的 <see cref="HmiVariable"/>；未找到返回 null。</returns>
    HmiVariable? GetVariable(string id);

    /// <summary>
    /// 根据变量名称查找变量。
    /// </summary>
    /// <param name="name">目标变量的名称（区分大小写）。</param>
    /// <returns>找到返回对应的 <see cref="HmiVariable"/>；未找到返回 null。</returns>
    HmiVariable? GetVariableByName(string name);

    /// <summary>
    /// 向服务中注册一个新的 HMI 变量。
    /// </summary>
    /// <param name="variable">要注册的变量对象。</param>
    void AddVariable(HmiVariable variable);

    /// <summary>
    /// 从服务中移除指定 ID 的变量。
    /// </summary>
    /// <param name="id">要移除的变量 ID。</param>
    void RemoveVariable(string id);

    /// <summary>
    /// 更新指定变量的运行时值。
    /// </summary>
    /// <param name="id">目标变量的唯一标识符。</param>
    /// <param name="value">要设置的新值。</param>
    void UpdateVariable(string id, object? value);

    /// <summary>
    /// 异步从 CSV 文件批量导入变量定义。
    /// </summary>
    /// <param name="filePath">CSV 文件的路径。</param>
    Task ImportFromCsvAsync(string filePath);

    /// <summary>
    /// 异步将当前所有变量定义导出为 CSV 文件。
    /// </summary>
    /// <param name="filePath">导出文件的目标路径。</param>
    Task ExportToCsvAsync(string filePath);

    /// <summary>当任意变量的值发生变更时引发的事件，参数包含变量名、旧值和新值。</summary>
    event EventHandler<VariableValueChangedEventArgs> VariableValueChanged;
}
