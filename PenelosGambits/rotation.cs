
namespace InfernoWow.Modules
{
    public class ProtectionPaladinRotation : Rotation
    {
        List<string> UtilitySpells = new List<string> { "Devotion Aura" };
        
        public override void LoadSettings()
        {
            WebSocket.Port = 8082;
            WebSocket.OnMessageReceived += OnWebSocketMessage;
            WebSocket.Start();
            Inferno.PrintMessage("WebSocket server started on ws://localhost:8082/");
        }

        private void OnWebSocketMessage(string message)
        {
            Inferno.PrintMessage("[WS] " + message);
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("Penelos Gambits Loader");
            foreach (string s in UtilitySpells) Spellbook.Add(s);
        }

        public override void OnStop()
        {
            WebSocket.Stop();
        }

        public override bool CombatTick()
        {
            return false;
        }

        public override bool OutOfCombatTick()
        {
            if (!Inferno.HasBuff("Devotion Aura") && Inferno.CanCast("Devotion Aura"))
            { Inferno.Cast("Devotion Aura"); return true; }
            return false;
        }
    }
}