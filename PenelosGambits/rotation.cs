using System.Collections.Generic;

namespace InfernoWow.Modules
{
    public class ProtectionPaladinRotation : Rotation
    {
        private List<string> Spells = new List<string>
        {
            "Devotion Aura",
            "Holy Shock",
            "Word of Glory",
            "Holy Light",
            "Beacon of Virtue",
            "Divine Toll",
            "Crusader Strike",
            "Holy Prism",
            "Judgment",
            "Shield of the Righteous",
            "Hammer of Wrath",
            "Consecration"
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

            // Register targeting macros (focus_party1, focus_raid5, etc.)
            foreach (var macro in TargetingMacros.macros)
            {
                Macros.Add(macro.Key, macro.Value);
            }

            // Register @focus cast macros for friendly-targeted spells
            SpellMacroRegistry.Register("Holy Shock", "cast_holy_shock", "/cast [@focus] Holy Shock");
            SpellMacroRegistry.Register("Word of Glory", "cast_word_of_glory", "/cast [@focus] Word of Glory");
            SpellMacroRegistry.Register("Holy Light", "cast_holy_light", "/cast [@focus] Holy Light");

            foreach (var macro in SpellMacroRegistry.GetAllMacros())
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