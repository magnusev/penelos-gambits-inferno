using System.Text;

public class CommandMessage : MessageBase
{
    public string CommandId { get; private set; }
    public string Action { get; private set; }
    public string Spell { get; private set; }
    public string Target { get; private set; }
    public string Macro { get; private set; }

    private CommandMessage(string commandId, string action, string spell, string target, string macro)
        : base(MessageType.Command)
    {
        CommandId = commandId;
        Action = action;
        Spell = spell;
        Target = target;
        Macro = macro;
    }

    public static CommandMessage FromJson(string json)
    {
        string msgType = JsonParser.GetString(json, "type");
        if (msgType != MessageType.Command) return null;

        string commandId = JsonParser.GetString(json, "commandId");
        string action = JsonParser.GetString(json, "action");
        string spell = JsonParser.GetString(json, "spell");
        string target = JsonParser.GetString(json, "target");
        string macro = JsonParser.GetString(json, "macro");

        return new CommandMessage(commandId, action, spell, target, macro);
    }

    public override string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"type\":" + EscapeJson(Type) + ",");
        sb.Append("\"commandId\":" + EscapeJson(CommandId) + ",");
        sb.Append("\"action\":" + EscapeJson(Action));

        if (Spell != null)
        {
            sb.Append(",\"spell\":" + EscapeJson(Spell));
        }

        if (Target != null)
        {
            sb.Append(",\"target\":" + EscapeJson(Target));
        }

        if (Macro != null)
        {
            sb.Append(",\"macro\":" + EscapeJson(Macro));
        }

        sb.Append("}");
        return sb.ToString();
    }

    public bool IsCast()
    {
        return Action == "CAST";
    }

    public bool IsMacro()
    {
        return Action == "MACRO";
    }

    public bool IsNone()
    {
        return Action == "NONE";
    }
}
