namespace InfernoWow.Modules
{
    public class HolyPaladinPvE : Rotation
    {
        private PeneloRotation PeneloRotation;

        public HolyPaladinPvE()
        {
            PeneloRotation = new PeneloRotation();
        }

        private List<string> Spells = new List<string>
        {
            "Holy Shock",
            "Flash of Light",
            "Devotion Aura"
        };

        private Environment _environment;

        public override void LoadSettings()
        {
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

            SpellMacroRegistry.Register("Holy Shock", "cast_holy_shock", "/cast [@focus] Holy Shock");
            SpellMacroRegistry.Register("Flash of Light", "cast_flash_of_light", "/cast [@focus] Flash of Light");
            SpellMacroRegistry.Register("Word of Glory", "cast_word_of_glory", "/cast [@focus] Word of Glory");
            SpellMacroRegistry.Register("Holy Light", "cast_holy_light", "/cast [@focus] Holy Light");

            foreach (var macro in SpellMacroRegistry.GetAllMacros())
            {
                Macros.Add(macro.Key, macro.Value);
            }
        }

        public override void OnStop()
        {
        }

        public override bool CombatTick()
        {
            RefreshEnvironment();
            return PeneloRotation.Tick();
        }

        public override bool OutOfCombatTick()
        {
            RefreshEnvironment();
            return PeneloRotation.Tick();
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