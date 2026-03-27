using System.Text;

public abstract class MessageBase
{
    public string Type { get; private set; }

    protected MessageBase(string type)
    {
        Type = type;
    }

    public abstract string ToJson();

    public static string EscapeJson(string value)
    {
        if (value == null) return "null";

        var sb = new StringBuilder();
        sb.Append("\"");
        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        sb.Append("\"");
        return sb.ToString();
    }

    public static string BoolToJson(bool value)
    {
        return value ? "true" : "false";
    }

    public static string ParseType(string json)
    {
        return JsonParser.GetString(json, "type");
    }
}
