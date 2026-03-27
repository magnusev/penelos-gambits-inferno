using System.Text;

public class ExecutionResultMessage : MessageBase
{
    public string CommandId { get; private set; }
    public bool Success { get; private set; }
    public string Error { get; private set; }

    public ExecutionResultMessage(string commandId, bool success, string error)
        : base(MessageType.ExecutionResult)
    {
        CommandId = commandId;
        Success = success;
        Error = error;
    }

    public override string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"type\":" + EscapeJson(Type) + ",");
        sb.Append("\"commandId\":" + EscapeJson(CommandId) + ",");
        sb.Append("\"success\":" + BoolToJson(Success));

        if (Error != null)
        {
            sb.Append(",\"error\":" + EscapeJson(Error));
        }
        else
        {
            sb.Append(",\"error\":null");
        }

        sb.Append("}");
        return sb.ToString();
    }
}
