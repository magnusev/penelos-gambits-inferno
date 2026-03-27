using System.Text;

public class QueryResponseMessage : MessageBase
{
    public string QueryId { get; private set; }
    public bool Result { get; private set; }
    public string Data { get; private set; }

    public QueryResponseMessage(string queryId, bool result, string data)
        : base(MessageType.QueryResponse)
    {
        QueryId = queryId;
        Result = result;
        Data = data;
    }

    public override string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"type\":" + EscapeJson(Type) + ",");
        sb.Append("\"queryId\":" + EscapeJson(QueryId) + ",");
        sb.Append("\"result\":" + BoolToJson(Result));

        if (Data != null)
        {
            sb.Append(",\"data\":" + Data);
        }

        sb.Append("}");
        return sb.ToString();
    }
}
