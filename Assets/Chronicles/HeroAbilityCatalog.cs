using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public enum AbilityEffect
    {
        Strike, HuntWeakest, Cleave, AreaBlast, StunStrike, FrostStrike, BurningWave,
        WeakeningCurse, LifeDrain, HealLowest, HealTeam, ShieldSelf, ShieldTeam,
        EnergizeTeam, TauntGuard, TripleStrike, Execution, Rally, Eclipse
    }

    public enum HeroPassive
    {
        Guardian, Executioner, SolarFlame, Ironhide, Fury, GuardianAngel, Bloodthirst,
        NightHunter, Evasion, Equilibrium, Tactician, Hunter, Gravekeeper, Rebirth,
        Colossus, Enchantress, Thunderlord, Sage, DeathChill, Winterborn,
        Dawnborn, Stormborn, Trickster, Compassion, Phoenix, Sorcery, Oracle,
        Knight, Diplomat, Banner
    }

    public sealed class HeroKit
    {
        public readonly string heroId, basicName, skillName, ultimateName, passiveName;
        public readonly string skillDescription, ultimateDescription, passiveDescription;
        public readonly AbilityEffect skill, ultimate;
        public readonly HeroPassive passive;
        public readonly float skillPower, ultimatePower, cooldown;

        public HeroKit(string heroId, string skillName, AbilityEffect skill, float skillPower,
            string ultimateName, AbilityEffect ultimate, float ultimatePower,
            string passiveName, HeroPassive passive, string skillDescription,
            string ultimateDescription, string passiveDescription, float cooldown = 6f)
        {
            this.heroId = heroId; this.basicName = "Basic attack"; this.skillName = skillName;
            this.skill = skill; this.skillPower = skillPower; this.ultimateName = ultimateName;
            this.ultimate = ultimate; this.ultimatePower = ultimatePower;
            this.passiveName = passiveName; this.passive = passive;
            this.skillDescription = skillDescription; this.ultimateDescription = ultimateDescription;
            this.passiveDescription = passiveDescription; this.cooldown = cooldown;
        }
    }

    public static class HeroAbilityCatalog
    {
        // Generated from shared/heroes/hero-roster.json (see tools/generate-hero-data.mjs).
        public static readonly HeroKit[] All = HeroRosterData.Kits;
        private static readonly Dictionary<string, HeroKit> Index = Build();

        private static Dictionary<string, HeroKit> Build()
        {
            var byId = new Dictionary<string, HeroKit>(StringComparer.Ordinal);
            foreach (HeroKit kit in All) byId.Add(kit.heroId, kit);
            return byId;
        }
        public static HeroKit For(string heroId)
        {
            HeroKit kit;
            return heroId != null && Index.TryGetValue(heroId, out kit) ? kit : null;
        }
    }
}
