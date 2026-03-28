using System;
using System.Collections.Generic;
using System.Text;

public class StateUpdateMessage : MessageBase
{
    public long Timestamp { get; private set; }
    public PlayerState Player { get; private set; }
    public TargetState TargetInfo { get; private set; }
    public GroupState GroupInfo { get; private set; }
    public List<BossState> BossStates { get; private set; }
    public int GlobalCooldown { get; private set; }
    public int CombatTime { get; private set; }
    public int MapId { get; private set; }

    public StateUpdateMessage(Environment env) : base(MessageType.StateUpdate)
    {
        Timestamp = DateTime.UtcNow.Ticks;
        GlobalCooldown = Inferno.GCD();
        CombatTime = Inferno.CombatTime();
        MapId = env.MapId;

        if (env.Player != null)
        {
            Player = new PlayerState(env.Player);
        }

        if (env.Target != null)
        {
            TargetInfo = new TargetState(env.Target);
        }

        if (env.Group != null)
        {
            GroupInfo = new GroupState(env.Group);
        }

        BossStates = new List<BossState>();
        if (env.Bosses != null)
        {
            foreach (var boss in env.Bosses)
            {
                BossStates.Add(new BossState(boss));
            }
        }
    }

    public override string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"type\":" + EscapeJson(Type) + ",");
        sb.Append("\"timestamp\":" + Timestamp + ",");
        sb.Append("\"mapId\":" + MapId + ",");
        sb.Append("\"globalCooldown\":" + GlobalCooldown + ",");
        sb.Append("\"combatTime\":" + CombatTime + ",");

        if (Player != null)
        {
            sb.Append("\"player\":" + Player.ToJson() + ",");
        }
        else
        {
            sb.Append("\"player\":null,");
        }

        if (TargetInfo != null)
        {
            sb.Append("\"target\":" + TargetInfo.ToJson() + ",");
        }
        else
        {
            sb.Append("\"target\":null,");
        }

        if (GroupInfo != null)
        {
            sb.Append("\"group\":" + GroupInfo.ToJson() + ",");
        }
        else
        {
            sb.Append("\"group\":null,");
        }

        sb.Append("\"bosses\":[");
        for (int i = 0; i < BossStates.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append(BossStates[i].ToJson());
        }
        sb.Append("]");

        sb.Append("}");
        return sb.ToString();
    }
}

public class PlayerState
{
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public int HealthPct { get; private set; }
    public string Spec { get; private set; }
    public int CastingSpellId { get; private set; }
    public bool InCombat { get; private set; }
    public bool IsMoving { get; private set; }

    public PlayerState(PlayerUnit player)
    {
        Health = player.Health;
        MaxHealth = player.MaxHealth;
        HealthPct = player.HealthPercentage;
        Spec = player.Role;
        CastingSpellId = player.CastingSpell;
        InCombat = Inferno.InCombat("player");
        IsMoving = Inferno.IsMoving("player");
    }

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"health\":" + Health + ",");
        sb.Append("\"maxHealth\":" + MaxHealth + ",");
        sb.Append("\"healthPct\":" + HealthPct + ",");
        sb.Append("\"spec\":" + MessageBase.EscapeJson(Spec) + ",");
        sb.Append("\"castingSpellId\":" + CastingSpellId + ",");
        sb.Append("\"inCombat\":" + MessageBase.BoolToJson(InCombat) + ",");
        sb.Append("\"isMoving\":" + MessageBase.BoolToJson(IsMoving));
        sb.Append("}");
        return sb.ToString();
    }
}

public class TargetState
{
    public bool Exists { get; private set; }
    public string Name { get; private set; }
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public int HealthPct { get; private set; }
    public int CastingSpellId { get; private set; }

    public TargetState(Target target)
    {
        Exists = target != null;
        if (target != null)
        {
            Name = target.name;
            Health = target.health;
            MaxHealth = target.maxHealth;
            HealthPct = target.healthPercentage;
            CastingSpellId = target.castingSpell;
        }
    }

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"exists\":" + MessageBase.BoolToJson(Exists) + ",");
        sb.Append("\"name\":" + MessageBase.EscapeJson(Name) + ",");
        sb.Append("\"health\":" + Health + ",");
        sb.Append("\"maxHealth\":" + MaxHealth + ",");
        sb.Append("\"healthPct\":" + HealthPct + ",");
        sb.Append("\"castingSpellId\":" + CastingSpellId);
        sb.Append("}");
        return sb.ToString();
    }
}

public class GroupState
{
    public string GroupType { get; private set; }
    public int Size { get; private set; }
    public List<MemberState> Members { get; private set; }

    public GroupState(Group group)
    {
        if (group is RaidGroup)
        {
            GroupType = "raid";
        }
        else if (group is PartyGroup)
        {
            GroupType = "party";
        }
        else
        {
            GroupType = "solo";
        }
        Size = Inferno.GroupSize();

        Members = new List<MemberState>();
        foreach (var member in group.GetMembers())
        {
            Members.Add(new MemberState(member));
        }
    }

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"type\":" + MessageBase.EscapeJson(GroupType) + ",");
        sb.Append("\"size\":" + Size + ",");

        sb.Append("\"members\":[");
        for (int i = 0; i < Members.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append(Members[i].ToJson());
        }
        sb.Append("]");

        sb.Append("}");
        return sb.ToString();
    }
}

public class MemberState
{
    public string UnitId { get; private set; }
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public int HealthPct { get; private set; }
    public bool IsDead { get; private set; }

    public MemberState(Unit member)
    {
        UnitId = member.Id;
        Health = member.Health;
        MaxHealth = member.MaxHealth;
        HealthPct = member.HealthPercentage;
        IsDead = member.Health == 0;
    }

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"unitId\":" + MessageBase.EscapeJson(UnitId) + ",");
        sb.Append("\"health\":" + Health + ",");
        sb.Append("\"maxHealth\":" + MaxHealth + ",");
        sb.Append("\"healthPct\":" + HealthPct + ",");
        sb.Append("\"isDead\":" + MessageBase.BoolToJson(IsDead));
        sb.Append("}");
        return sb.ToString();
    }
}

public class BossState
{
    public string UnitId { get; private set; }
    public string Name { get; private set; }
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public int HealthPct { get; private set; }
    public int CastingSpellId { get; private set; }

    public BossState(Boss boss)
    {
        UnitId = boss.Name;
        Name = boss.UnitName;
        Health = boss.Health;
        MaxHealth = boss.MaxHealth;
        HealthPct = boss.HealthPercentage;
        CastingSpellId = boss.CurrentlyCastingSpellId;
    }

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"unitId\":" + MessageBase.EscapeJson(UnitId) + ",");
        sb.Append("\"name\":" + MessageBase.EscapeJson(Name) + ",");
        sb.Append("\"health\":" + Health + ",");
        sb.Append("\"maxHealth\":" + MaxHealth + ",");
        sb.Append("\"healthPct\":" + HealthPct + ",");
        sb.Append("\"castingSpellId\":" + CastingSpellId);
        sb.Append("}");
        return sb.ToString();
    }
}
