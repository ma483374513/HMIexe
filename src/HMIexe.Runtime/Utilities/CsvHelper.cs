namespace HMIexe.Runtime.Utilities;

internal static class CsvHelper
{
    public static string QuoteField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

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
                    fieldBuilder.Append('"');
                    i++;
                }
                else if (c == '"')
                    inQuotes = false;
                else
                    fieldBuilder.Append(c);
            }
            else if (c == '"')
                inQuotes = true;
            else if (c == ',')
            {
                fields.Add(fieldBuilder.ToString());
                fieldBuilder.Clear();
            }
            else
                fieldBuilder.Append(c);
        }
        fields.Add(fieldBuilder.ToString());
        return fields.ToArray();
    }
}
