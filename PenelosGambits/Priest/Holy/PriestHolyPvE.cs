namespace InfernoWow.Modules
{
    public class HolyPriestPvE : Rotation
    {
        private PriestHolyActionBook ActionBook;
        private GambitSetPicker GambitSetPicker;
        private PeneloRotation PeneloRotation;

        public HolyPriestPvE()
        {
            ActionBook = new PriestHolyActionBook();
            GambitSetPicker = new PriestHolyGambitPicker();
            PeneloRotation = new PeneloRotation(GambitSetPicker);
        }
        
        private Environment _environment;

        public override void LoadSettings()
        {
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("Penelos Gambits Loader");

            ActionBook.GetDefaultActions().ForEach(Spellbook.Add);
            ActionBook.GetDebuffActions().ForEach(Debuffs.Add);
            ActionBook.GetBuffActions().ForEach(Buffs.Add);
            ActionBook.GetCommands().ForEach(CustomCommands.Add);


            foreach (var macro in TargetingMacros.macros)
            {
                Macros.Add(macro.Key, macro.Value);
            }

            foreach (var macro in ActionBook.GetMacroActions())
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
            return PeneloRotation.Tick(_environment);
        }

        public override bool OutOfCombatTick()
        {
            RefreshEnvironment();
            return PeneloRotation.Tick(_environment);
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