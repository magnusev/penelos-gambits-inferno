using System.Text;

public class QueryMessage : MessageBase
{
    public string QueryId { get; private set; }
    public string Method { get; private set; }
    public string Params { get; private set; }

    private QueryMessage(string queryId, string method, string queryParams)
        : base(MessageType.Query)
    {
        QueryId = queryId;
        Method = method;
        Params = queryParams;
    }

    public static QueryMessage FromJson(string json)
    {

        string queryId = JsonParser.GetString(json, "queryId");
        string method = JsonParser.GetString(json, "method");
        string queryParams = JsonParser.GetObject(json, "params");

        return new QueryMessage(queryId, method, queryParams);
    }

    public string GetParam(string key)
    {
        if (Params == null) return null;
        return JsonParser.GetString(Params, key);
    }

    public int GetParamInt(string key, int defaultValue)
    {
        if (Params == null) return defaultValue;
        return JsonParser.GetInt(Params, key, defaultValue);
    }

    public bool GetParamBool(string key, bool defaultValue)
    {
        if (Params == null) return defaultValue;
        return JsonParser.GetBool(Params, key, defaultValue);
    }

    public override string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"type\":" + EscapeJson(Type) + ",");
        sb.Append("\"queryId\":" + EscapeJson(QueryId) + ",");
        sb.Append("\"method\":" + EscapeJson(Method));

        if (Params != null)
        {
            sb.Append(",\"params\":" + Params);
        }

        sb.Append("}");
        return sb.ToString();
    }
}
