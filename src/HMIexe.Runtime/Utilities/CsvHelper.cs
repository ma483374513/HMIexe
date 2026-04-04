namespace HMIexe.Runtime.Utilities;

/// <summary>
/// CSV 工具类，提供 CSV 字段的引号转义和行解析功能。
/// 遵循 RFC 4180 规范：字段内含逗号、双引号或换行符时用双引号包裹，内部双引号转义为两个双引号。
/// </summary>
internal static class CsvHelper
{
    /// <summary>
    /// 对单个 CSV 字段进行引号转义处理。
    /// 若字段包含逗号、双引号或换行符，则用双引号包裹，并将内部双引号替换为 <c>""</c>。
    /// 否则原样返回。
    /// </summary>
    /// <param name="field">原始字段字符串。</param>
    /// <returns>转义后可安全写入 CSV 的字段字符串。</returns>
    public static string QuoteField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

    /// <summary>
    /// 将单行 CSV 文本解析为字段数组，正确处理带引号的字段（含内部逗号、换行和转义引号）。
    /// <para>
    /// 解析规则：
    /// <list type="bullet">
    ///   <item>遇到 <c>"</c> 进入引号模式，此模式下逗号和换行不作为分隔符。</item>
    ///   <item>引号模式中 <c>""</c> 表示一个字面引号字符。</item>
    ///   <item>单个 <c>"</c> 退出引号模式。</item>
    ///   <item>非引号模式下 <c>,</c> 作为字段分隔符。</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="line">单行 CSV 文本。</param>
    /// <returns>解析出的字段字符串数组。</returns>
    public static string[] SplitLine(string line)
    {
        var fields = new List<string>();
        var fieldBuilder = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 连续两个引号：转义，输出一个字面引号并跳过下一个字符
                    fieldBuilder.Append('"');
                    i++;
                }
                else if (c == '"')
                    // 单个引号：退出引号模式
                    inQuotes = false;
                else
                    fieldBuilder.Append(c);
            }
            else if (c == '"')
                // 非引号模式遇到引号：进入引号模式
                inQuotes = true;
            else if (c == ',')
            {
                // 字段分隔符：保存当前字段并重置缓冲区
                fields.Add(fieldBuilder.ToString());
                fieldBuilder.Clear();
            }
            else
                fieldBuilder.Append(c);
        }
        // 将最后一个字段（即使为空）也加入结果
        fields.Add(fieldBuilder.ToString());
        return fields.ToArray();
    }
}
