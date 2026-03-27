using System.Text;

public class ConnectMessage : MessageBase
{
    public string Character { get; private set; }
    public string Spec { get; private set; }

    public ConnectMessage(string character, string spec) : base(MessageType.Connect)
    {
        Character = character;
        Spec = spec;
    }

    public override string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"type\":" + EscapeJson(Type) + ",");
        sb.Append("\"character\":" + EscapeJson(Character) + ",");
        sb.Append("\"spec\":" + EscapeJson(Spec));
        sb.Append("}");
        return sb.ToString();
    }
}
