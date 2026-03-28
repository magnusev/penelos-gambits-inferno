using System;
using System.Text;

public class QueryHandler
{
    private readonly MessageRouter _router;

    public QueryHandler(MessageRouter router)
    {
        _router = router;
    }

    public void HandleQuery(QueryMessage query)
    {
        try
        {
            string method = query.Method;
            if (method == null)
            {
                SendError(query, "Missing method");
                return;
            }

            switch (method)
            {
                // ── Buff / Debuff ──────────────────────────
                case "HasBuff":
                    HandleHasBuff(query);
                    break;
                case "HasDebuff":
                    HandleHasDebuff(query);
                    break;
                case "BuffRemaining":
                    HandleBuffRemaining(query);
                    break;
                case "DebuffRemaining":
                    HandleDebuffRemaining(query);
                    break;
                case "BuffStacks":
                    HandleBuffStacks(query);
                    break;
                case "DebuffStacks":
                    HandleDebuffStacks(query);
                    break;

                // ── Spell info ─────────────────────────────
                case "CanCast":
                    HandleCanCast(query);
                    break;
                case "SpellCooldown":
                    HandleSpellCooldown(query);
                    break;
                case "SpellCharges":
                    HandleSpellCharges(query);
                    break;
                case "SpellUsable":
                    HandleSpellUsable(query);
                    break;
                case "SpellInRange":
                    HandleSpellInRange(query);
                    break;
                case "IsSpellKnown":
                    HandleIsSpellKnown(query);
                    break;
                case "GCD":
                    HandleGCD(query);
                    break;

                // ── Unit state ─────────────────────────────
                case "Health":
                    HandleHealth(query);
                    break;
                case "MaxHealth":
                    HandleMaxHealth(query);
                    break;
                case "Power":
                    HandlePower(query);
                    break;
                case "MaxPower":
                    HandleMaxPower(query);
                    break;
                case "InCombat":
                    HandleInCombat(query);
                    break;
                case "IsDead":
                    HandleIsDead(query);
                    break;
                case "IsMoving":
                    HandleIsMoving(query);
                    break;
                case "IsVisible":
                    HandleIsVisible(query);
                    break;
                case "UnitName":
                    HandleUnitName(query);
                    break;

                // ── Casting detection ──────────────────────
                case "IsInterruptable":
                    HandleIsInterruptable(query);
                    break;
                case "IsChanneling":
                    HandleIsChanneling(query);
                    break;
                case "CastingID":
                    HandleCastingID(query);
                    break;
                case "CastingName":
                    HandleCastingName(query);
                    break;
                case "CastingRemaining":
                    HandleCastingRemaining(query);
                    break;

                // ── Distance / AoE ─────────────────────────
                case "DistanceBetween":
                    HandleDistanceBetween(query);
                    break;
                case "EnemiesNearUnit":
                    HandleEnemiesNearUnit(query);
                    break;
                case "FriendsNearUnit":
                    HandleFriendsNearUnit(query);
                    break;

                // ── Items ──────────────────────────────────
                case "CanUseEquippedItem":
                    HandleCanUseEquippedItem(query);
                    break;
                case "InventoryItemID":
                    HandleInventoryItemID(query);
                    break;
                case "ItemCooldown":
                    HandleItemCooldown(query);
                    break;

                // ── Misc ───────────────────────────────────
                case "PlayerIsMounted":
                    HandlePlayerIsMounted(query);
                    break;
                case "CombatTime":
                    HandleCombatTime(query);
                    break;
                case "GroupSize":
                    HandleGroupSize(query);
                    break;
                case "InParty":
                    HandleInParty(query);
                    break;
                case "InRaid":
                    HandleInRaid(query);
                    break;
                case "IsCustomCodeOn":
                    HandleIsCustomCodeOn(query);
                    break;

                default:
                    SendError(query, "Unknown method: " + method);
                    break;
            }
        }
        catch (Exception ex)
        {
            Inferno.PrintMessage("[QueryHandler] Error: " + ex.Message);
            SendError(query, "Exception: " + ex.Message);
        }
    }

    // ── Buff / Debuff ──────────────────────────────────────

    private void HandleHasBuff(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        string buff = q.GetParam("buff");
        bool byPlayer = q.GetParamBool("byPlayer", true);

        bool has = Inferno.HasBuff(buff, unit, byPlayer);
        int remaining = Inferno.BuffRemaining(buff, unit, byPlayer);
        int stacks = Inferno.BuffStacks(buff, unit, byPlayer);

        SendResult(q, has, "{\"remaining\":" + remaining + ",\"stacks\":" + stacks + "}");
    }

    private void HandleHasDebuff(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "target";
        string debuff = q.GetParam("debuff");
        bool byPlayer = q.GetParamBool("byPlayer", true);

        bool has = Inferno.HasDebuff(debuff, unit, byPlayer);
        int remaining = Inferno.DebuffRemaining(debuff, unit, byPlayer);
        int stacks = Inferno.DebuffStacks(debuff, unit, byPlayer);

        SendResult(q, has, "{\"remaining\":" + remaining + ",\"stacks\":" + stacks + "}");
    }

    private void HandleBuffRemaining(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        string buff = q.GetParam("buff");
        bool byPlayer = q.GetParamBool("byPlayer", true);

        int remaining = Inferno.BuffRemaining(buff, unit, byPlayer);
        SendResult(q, remaining > 0, "{\"remaining\":" + remaining + "}");
    }

    private void HandleDebuffRemaining(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "target";
        string debuff = q.GetParam("debuff");
        bool byPlayer = q.GetParamBool("byPlayer", true);

        int remaining = Inferno.DebuffRemaining(debuff, unit, byPlayer);
        SendResult(q, remaining > 0, "{\"remaining\":" + remaining + "}");
    }

    private void HandleBuffStacks(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        string buff = q.GetParam("buff");
        bool byPlayer = q.GetParamBool("byPlayer", true);

        int stacks = Inferno.BuffStacks(buff, unit, byPlayer);
        SendResult(q, stacks > 0, "{\"stacks\":" + stacks + "}");
    }

    private void HandleDebuffStacks(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "target";
        string debuff = q.GetParam("debuff");
        bool byPlayer = q.GetParamBool("byPlayer", true);

        int stacks = Inferno.DebuffStacks(debuff, unit, byPlayer);
        SendResult(q, stacks > 0, "{\"stacks\":" + stacks + "}");
    }

    // ── Spell info ─────────────────────────────────────────

    private void HandleCanCast(QueryMessage q)
    {
        string spell = q.GetParam("spell");
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";

        bool can = Inferno.CanCast(spell, unit);
        SendResult(q, can, null);
    }

    private void HandleSpellCooldown(QueryMessage q)
    {
        string spell = q.GetParam("spell");
        int cd = Inferno.SpellCooldown(spell);
        SendResult(q, cd == 0, "{\"cooldown\":" + cd + "}");
    }

    private void HandleSpellCharges(QueryMessage q)
    {
        string spell = q.GetParam("spell");
        int charges = Inferno.SpellCharges(spell);
        SendResult(q, charges > 0, "{\"charges\":" + charges + "}");
    }

    private void HandleSpellUsable(QueryMessage q)
    {
        string spell = q.GetParam("spell");
        bool usable = Inferno.SpellUsable(spell);
        SendResult(q, usable, null);
    }

    private void HandleSpellInRange(QueryMessage q)
    {
        string spell = q.GetParam("spell");
        string unit = q.GetParam("unit");
        if (unit == null) unit = "target";

        bool inRange = Inferno.SpellInRange(spell, unit);
        SendResult(q, inRange, null);
    }

    private void HandleIsSpellKnown(QueryMessage q)
    {
        string spell = q.GetParam("spell");
        bool known = Inferno.IsSpellKnown(spell);
        SendResult(q, known, null);
    }

    private void HandleGCD(QueryMessage q)
    {
        int gcd = Inferno.GCD();
        SendResult(q, gcd == 0, "{\"remaining\":" + gcd + "}");
    }

    // ── Unit state ─────────────────────────────────────────

    private void HandleHealth(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        int health = Inferno.Health(unit);
        SendResult(q, health > 0, "{\"health\":" + health + "}");
    }

    private void HandleMaxHealth(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        int maxHealth = Inferno.MaxHealth(unit);
        SendResult(q, true, "{\"maxHealth\":" + maxHealth + "}");
    }

    private void HandlePower(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        int powerType = q.GetParamInt("powerType", 0);
        int power = Inferno.Power(unit, powerType);
        SendResult(q, true, "{\"power\":" + power + "}");
    }

    private void HandleMaxPower(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        int powerType = q.GetParamInt("powerType", 0);
        int maxPower = Inferno.MaxPower(unit, powerType);
        SendResult(q, true, "{\"maxPower\":" + maxPower + "}");
    }

    private void HandleInCombat(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        bool combat = Inferno.InCombat(unit);
        SendResult(q, combat, null);
    }

    private void HandleIsDead(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        bool dead = Inferno.IsDead(unit);
        SendResult(q, dead, null);
    }

    private void HandleIsMoving(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        bool moving = Inferno.IsMoving(unit);
        SendResult(q, moving, null);
    }

    private void HandleIsVisible(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        bool visible = Inferno.IsVisible(unit);
        SendResult(q, visible, null);
    }

    private void HandleUnitName(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "target";
        string name = Inferno.UnitName(unit);
        bool exists = !string.IsNullOrEmpty(name);
        SendResult(q, exists, "{\"name\":" + MessageBase.EscapeJson(name) + "}");
    }

    // ── Casting detection ──────────────────────────────────

    private void HandleIsInterruptable(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "target";
        bool interruptable = Inferno.IsInterruptable(unit);
        SendResult(q, interruptable, null);
    }

    private void HandleIsChanneling(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "target";
        bool channeling = Inferno.IsChanneling(unit);
        SendResult(q, channeling, null);
    }

    private void HandleCastingID(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "target";
        int spellId = Inferno.CastingID(unit);
        SendResult(q, spellId != 0, "{\"spellId\":" + spellId + "}");
    }

    private void HandleCastingName(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "target";
        string name = Inferno.CastingName(unit);
        bool casting = !string.IsNullOrEmpty(name);
        SendResult(q, casting, "{\"name\":" + MessageBase.EscapeJson(name) + "}");
    }

    private void HandleCastingRemaining(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "target";
        int remaining = Inferno.CastingRemaining(unit);
        SendResult(q, remaining > 0, "{\"remaining\":" + remaining + "}");
    }

    // ── Distance / AoE ─────────────────────────────────────

    private void HandleDistanceBetween(QueryMessage q)
    {
        string unit1 = q.GetParam("unit1");
        if (unit1 == null) unit1 = "player";
        string unit2 = q.GetParam("unit2");
        if (unit2 == null) unit2 = "target";

        float dist = Inferno.DistanceBetween(unit1, unit2);
        SendResult(q, true, "{\"distance\":" + dist + "}");
    }

    private void HandleEnemiesNearUnit(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        int distance = q.GetParamInt("distance", 8);

        int count = Inferno.EnemiesNearUnit(distance, unit);
        SendResult(q, count > 0, "{\"count\":" + count + "}");
    }

    private void HandleFriendsNearUnit(QueryMessage q)
    {
        string unit = q.GetParam("unit");
        if (unit == null) unit = "player";
        int distance = q.GetParamInt("distance", 8);

        int count = Inferno.FriendsNearUnit(distance, unit);
        SendResult(q, count > 0, "{\"count\":" + count + "}");
    }

    // ── Items ──────────────────────────────────────────────

    private void HandleCanUseEquippedItem(QueryMessage q)
    {
        int slot = q.GetParamInt("slot", 0);
        bool can = Inferno.CanUseEquippedItem(slot);
        SendResult(q, can, null);
    }

    private void HandleInventoryItemID(QueryMessage q)
    {
        int slot = q.GetParamInt("slot", 0);
        int itemId = Inferno.InventoryItemID(slot);
        SendResult(q, itemId != 0, "{\"itemId\":" + itemId + "}");
    }

    private void HandleItemCooldown(QueryMessage q)
    {
        int itemId = q.GetParamInt("itemId", 0);
        int cd = Inferno.ItemCooldown(itemId);
        SendResult(q, cd == 0, "{\"cooldown\":" + cd + "}");
    }

    // ── Misc ───────────────────────────────────────────────

    private void HandlePlayerIsMounted(QueryMessage q)
    {
        bool mounted = Inferno.PlayerIsMounted();
        SendResult(q, mounted, null);
    }

    private void HandleCombatTime(QueryMessage q)
    {
        int time = Inferno.CombatTime();
        SendResult(q, time > 0, "{\"combatTime\":" + time + "}");
    }

    private void HandleGroupSize(QueryMessage q)
    {
        int size = Inferno.GroupSize();
        SendResult(q, size > 0, "{\"size\":" + size + "}");
    }

    private void HandleInParty(QueryMessage q)
    {
        bool party = Inferno.InParty();
        SendResult(q, party, null);
    }

    private void HandleInRaid(QueryMessage q)
    {
        bool raid = Inferno.InRaid();
        SendResult(q, raid, null);
    }

    private void HandleIsCustomCodeOn(QueryMessage q)
    {
        string code = q.GetParam("code");
        if (code == null) code = "";
        bool on = Inferno.IsCustomCodeOn(code);
        SendResult(q, on, null);
    }

    // ── Helpers ────────────────────────────────────────────

    private void SendResult(QueryMessage q, bool result, string data)
    {
        var response = new QueryResponseMessage(q.QueryId, result, data);
        _router.SendQueryResponse(response);
    }

    private void SendError(QueryMessage q, string error)
    {
        var response = new QueryResponseMessage(q.QueryId, false, "{\"error\":" + MessageBase.EscapeJson(error) + "}");
        _router.SendQueryResponse(response);
    }
}
