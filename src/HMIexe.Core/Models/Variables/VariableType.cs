/// <summary>
/// HMI 变量数据类型枚举。
/// 定义变量所支持的所有基础和复合数据类型，用于指导序列化、类型转换和界面输入验证。
/// </summary>
namespace HMIexe.Core.Models.Variables;

/// <summary>
/// HMI 变量类型，标识变量值的数据格式和存储方式。
/// </summary>
public enum VariableType
{
    /// <summary>无符号 8 位整型（0～255），常用于单字节寄存器值。</summary>
    Byte,
    /// <summary>有符号 16 位整型（-32768～32767），常用于 Modbus 寄存器。</summary>
    Short,
    /// <summary>有符号 32 位整型，用于一般整数数值。</summary>
    Int,
    /// <summary>32 位单精度浮点数，用于一般模拟量（精度较低）。</summary>
    Float,
    /// <summary>64 位双精度浮点数，用于需要高精度的模拟量。</summary>
    Double,
    /// <summary>布尔型（true/false），用于开关量、状态位等二值信号。</summary>
    Bool,
    /// <summary>字符串类型，用于文本消息、配方名称等字符数据。</summary>
    String,
    /// <summary>日期时间类型，用于记录时间戳或设定时间值。</summary>
    DateTime,
    /// <summary>枚举类型，值为有限离散集合，需配合枚举定义使用。</summary>
    Enum,
    /// <summary>结构体类型，由多个子字段组成的复合数据类型。</summary>
    Struct
}
