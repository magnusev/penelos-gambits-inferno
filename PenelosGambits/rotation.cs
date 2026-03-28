namespace InfernoWow.Modules
{
    public class ProtectionPaladinRotation : Rotation
    {
        private List<string> Spells = new List<string>
        {
            "Devotion Aura",
            "Holy Shock"
        };

        private Environment _environment;
        private RemoteEngineClient _engine;

        public override void LoadSettings()
        {
            _engine = new RemoteEngineClient(8082);
            _engine.Start();
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("Penelos Gambits Loader");

            foreach (string s in Spells)
            {
                Spellbook.Add(s);
            }

            foreach (var macro in TargetingMacros.macros)
            {
                Macros.Add(macro.Key, macro.Value);
            }
        }

        public override void OnStop()
        {
            ActionQueuer.Clear();
            _engine.Stop();
        }

        public override bool CombatTick()
        {
            if (ActionQueuer.CastQueuedActionIfExists()) return true;

            RefreshEnvironment();
            _engine.ProcessPendingQueries();
            _engine.SendStateUpdate(_environment);

            return _engine.ExecuteNextCommand();
        }

        public override bool OutOfCombatTick()
        {
            if (ActionQueuer.CastQueuedActionIfExists()) return true;

            RefreshEnvironment();
            _engine.ProcessPendingQueries();

            if (!Inferno.HasBuff("Devotion Aura") && Inferno.CanCast("Devotion Aura"))
            {
                Inferno.Cast("Devotion Aura");
                return true;
            }

            _engine.SendStateUpdate(_environment);
            return false;
        }

        private void RefreshEnvironment()
        {
            _environment = new Environment(GetOldBosses());
        }

        private List<Boss> GetOldBosses()
        {
            if (_environment == null) return new List<Boss>();
            return _environment.Bosses;
        }
    }
}