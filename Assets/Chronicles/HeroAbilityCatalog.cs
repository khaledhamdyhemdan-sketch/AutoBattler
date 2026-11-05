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
        public static readonly HeroKit[] All =
        {
            new HeroKit("horus", "Falcon Talon", AbilityEffect.HuntWeakest, 1.85f,
                "Skyfall", AbilityEffect.Execution, 4.2f, "Royal Precision", HeroPassive.Executioner,
                "Dive toward the frailest enemy.", "Strike the weakest enemy with an execution bonus.", "More damage against foes under 40% HP.", 5.4f),
            new HeroKit("anubis", "Soul Barrier", AbilityEffect.ShieldTeam, 1.7f,
                "Judgement Gate", AbilityEffect.Eclipse, 1.8f, "Keeper's Oath", HeroPassive.Guardian,
                "Protect allied heroes with spectral shields.", "Strike the enemy team and briefly stun them.", "Resist damage while protecting the front line.", 7.2f),
            new HeroKit("ra", "Solar Flare", AbilityEffect.BurningWave, 1.35f,
                "Suncrown", AbilityEffect.AreaBlast, 2.7f, "Eternal Day", HeroPassive.SolarFlame,
                "Burn enemies with sunlight.", "Unleash a solar shockwave.", "Basic attacks inflict a brief solar burn.", 6.1f),
            new HeroKit("sobek", "River Guard", AbilityEffect.TauntGuard, 1.8f,
                "Flood of Teeth", AbilityEffect.Cleave, 3.3f, "Crocodile Hide", HeroPassive.Ironhide,
                "Shield yourself and force enemies to target you.", "Strike several foes at once.", "Take less incoming damage.", 6.8f),
            new HeroKit("set", "Sand Rend", AbilityEffect.Cleave, 1.8f,
                "Desert Tempest", AbilityEffect.BurningWave, 2.2f, "Chaos Pulse", HeroPassive.Fury,
                "Rend nearby enemies.", "Sweep the enemy team with storm damage.", "Damage grows as HP falls.", 5.8f),
            new HeroKit("isis", "Healing Light", AbilityEffect.HealLowest, 3f,
                "Wings of Life", AbilityEffect.HealTeam, 3.2f, "Life Thread", HeroPassive.GuardianAngel,
                "Restore the lowest-health ally.", "Heal the allied team.", "Once per fight, save an ally from lethal damage.", 5.3f),
            new HeroKit("sekhmet", "Lioness Claw", AbilityEffect.LifeDrain, 2.2f,
                "Red Hunt", AbilityEffect.TripleStrike, 1.75f, "Bloodlust", HeroPassive.Bloodthirst,
                "Strike an enemy and recover health.", "Land a rapid three-hit burst.", "Restore some HP when damaging enemies.", 5.6f),
            new HeroKit("nephthys", "Veil of Night", AbilityEffect.WeakeningCurse, 1.1f,
                "Silent Night", AbilityEffect.Eclipse, 2f, "Night Hunt", HeroPassive.NightHunter,
                "Weaken an enemy and amplify subsequent hits.", "Curse the entire enemy formation.", "Deal more damage to weakened targets.", 6.4f),
            new HeroKit("bastet", "Moon Claws", AbilityEffect.TripleStrike, .75f,
                "Shadow Dance", AbilityEffect.HuntWeakest, 3.5f, "Nine Lives", HeroPassive.Evasion,
                "Perform a fast triple swipe.", "Dive behind the enemy front line.", "Occasionally dodge incoming attacks.", 5.3f),
            new HeroKit("maat", "Balanced Scales", AbilityEffect.ShieldTeam, 1.35f,
                "Judgement of Order", AbilityEffect.EnergizeTeam, 2f, "Feather's Grace", HeroPassive.Equilibrium,
                "Protect allied heroes.", "Restore team energy and bolster defenses.", "Periodically shield the most injured ally.", 6.5f),
            new HeroKit("athena", "Tactical Spear", AbilityEffect.StunStrike, 1.7f,
                "Aegis Formation", AbilityEffect.ShieldTeam, 3.1f, "War Strategist", HeroPassive.Tactician,
                "Disrupt an enemy attack.", "Raise a powerful shield over all allies.", "Boost team damage at the opening of battle.", 6.1f),
            new HeroKit("artemis", "Silver Arrow", AbilityEffect.HuntWeakest, 2.3f,
                "Moonfall Volley", AbilityEffect.AreaBlast, 2.2f, "Perfect Aim", HeroPassive.Hunter,
                "Track down the weakest enemy.", "Rain arrows over the entire enemy party.", "Basic attacks sometimes deal critical damage.", 5.4f),
            new HeroKit("hades", "Stygian Tax", AbilityEffect.LifeDrain, 1.85f,
                "Underworld Gate", AbilityEffect.Eclipse, 2.4f, "Grave Keeper", HeroPassive.Gravekeeper,
                "Drain vitality from the front line.", "Seal the battlefield in shadow.", "Gain a shield when an enemy falls.", 6.5f),
            new HeroKit("persephone", "Spring's Return", AbilityEffect.HealLowest, 2.65f,
                "Winter Rebirth", AbilityEffect.HealTeam, 3.3f, "Bloom", HeroPassive.Rebirth,
                "Restore an injured ally.", "Restore the allied team with seasonal magic.", "Regen health periodically.", 5.2f),
            new HeroKit("heracles", "Twelve Labors", AbilityEffect.Strike, 2.5f,
                "Lion's Might", AbilityEffect.TripleStrike, 2f, "Unbroken", HeroPassive.Colossus,
                "Deliver a crushing strike.", "Land three heroic blows.", "Survive fatal damage once with 1 HP.", 6.0f),
            new HeroKit("freyja", "Seidr Blessing", AbilityEffect.EnergizeTeam, 1.5f,
                "Valkyrie Dawn", AbilityEffect.ShieldTeam, 2.9f, "Battle Magic", HeroPassive.Enchantress,
                "Feed energy into the team.", "Wrap allies in ancient magic.", "Earn more energy with each attack.", 5.9f),
            new HeroKit("thor", "Hammerbolt", AbilityEffect.StunStrike, 2.1f,
                "Stormbreaker", AbilityEffect.Eclipse, 2.9f, "Lightning Heart", HeroPassive.Thunderlord,
                "Stun the enemy struck by thunder.", "Electrify the whole field.", "Occasionally stun on normal attacks.", 5.8f),
            new HeroKit("odin", "Raven's Counsel", AbilityEffect.EnergizeTeam, 1.8f,
                "Allfather's Decree", AbilityEffect.Rally, 2.5f, "Wisdom", HeroPassive.Sage,
                "Charge allied ultimates.", "Empower the formation for a decisive push.", "Gain faster skill recharge.", 6.1f),
            new HeroKit("hel", "Chill of Niflheim", AbilityEffect.FrostStrike, 1.8f,
                "The Unremembered", AbilityEffect.WeakeningCurse, 3f, "Death Chill", HeroPassive.DeathChill,
                "Slow and hurt an enemy.", "Brand the opponent with a lingering curse.", "Deal extra damage to weakened enemies.", 6.0f),
            new HeroKit("skadi", "Glacial Shot", AbilityEffect.FrostStrike, 2.2f,
                "Winter's Silence", AbilityEffect.Eclipse, 2.2f, "White Hunt", HeroPassive.Winterborn,
                "Freeze an enemy in place.", "Cover the enemies in winter.", "Attack frozen targets more effectively.", 5.7f),
            new HeroKit("amaterasu", "Dawn Ribbon", AbilityEffect.BurningWave, 1.6f,
                "Heavenly Radiance", AbilityEffect.AreaBlast, 2.75f, "Morning Light", HeroPassive.Dawnborn,
                "Ignite the enemy formation.", "Send a wave of radiant fire.", "Increase healing received by the team.", 6.1f),
            new HeroKit("susanoo", "Wind Cutter", AbilityEffect.Cleave, 1.9f,
                "Tempest Blade", AbilityEffect.TripleStrike, 2.15f, "Unbound Storm", HeroPassive.Stormborn,
                "Slash across the front line.", "Slice the enemy three times.", "Attacks accelerate after skill use.", 5.2f),
            new HeroKit("sunwukong", "Cloud Dash", AbilityEffect.TripleStrike, 1.1f,
                "Seventy-Two Shapes", AbilityEffect.HuntWeakest, 4f, "Immortal Mischief", HeroPassive.Trickster,
                "Pummel a foe with playful feints.", "Strike the vulnerable back line.", "Avoid a portion of attacks.", 4.9f),
            new HeroKit("guanyin", "Compassion's Touch", AbilityEffect.HealLowest, 3.1f,
                "Mercy Beyond Worlds", AbilityEffect.HealTeam, 3.8f, "Compassion", HeroPassive.Compassion,
                "Mend the most wounded ally.", "Restore every surviving ally.", "Healing applies a small shield.", 5.4f),
            new HeroKit("nezha", "Fire Wheels", AbilityEffect.BurningWave, 1.6f,
                "Lotus Inferno", AbilityEffect.AreaBlast, 2.6f, "Lotus Child", HeroPassive.Phoenix,
                "Burn enemies with spinning wheels.", "Ignite the whole battlefield.", "Once per battle, avoid defeat.", 5.3f),
            new HeroKit("morgana", "Binding Fate", AbilityEffect.WeakeningCurse, 1.65f,
                "Queen of Veils", AbilityEffect.Eclipse, 2.45f, "Hexweaver", HeroPassive.Sorcery,
                "Curse a priority target.", "Silence the opponents in a wave of magic.", "Curses last longer.", 6f),
            new HeroKit("merlin", "Runic Burst", AbilityEffect.AreaBlast, 1.45f,
                "Timekeeper", AbilityEffect.EnergizeTeam, 3.5f, "Prophet", HeroPassive.Oracle,
                "Weave a burst of spell damage.", "Fill allied energy with arcane power.", "Skills recharge sooner.", 5.9f),
            new HeroKit("arthur", "Knight's Shield", AbilityEffect.TauntGuard, 2f,
                "Oath of the Round", AbilityEffect.ShieldTeam, 3.2f, "King's Resolve", HeroPassive.Knight,
                "Protect allies by drawing aggression.", "Raise the shields of the Round Table.", "Gain defense while fighting on the front line.", 6.8f),
            new HeroKit("cleopatra", "Royal Decree", AbilityEffect.Rally, 1.8f,
                "Queen's Gambit", AbilityEffect.WeakeningCurse, 3.1f, "Diplomacy", HeroPassive.Diplomat,
                "Increase the next allied attacks.", "Leave foes vulnerable to your team.", "At battle start, charge allied energy.", 6.4f),
            new HeroKit("joan", "Banner of Courage", AbilityEffect.ShieldTeam, 1.65f,
                "Light of Resolve", AbilityEffect.Rally, 3.2f, "Inspiration", HeroPassive.Banner,
                "Protect the front line.", "Inspire the whole formation.", "Allies deal slightly more damage.", 5.9f),
            new HeroKit("amun", "Hidden Radiance", AbilityEffect.ShieldTeam, 2.8f,
                "The First Dawn", AbilityEffect.AreaBlast, 3.3f, "The Unseen", HeroPassive.Dawnborn,
                "Shield comrades with unseen light.", "Flood every enemy with the beginning of dawn.", "Strengthen allies receiving healing.", 6.2f),
            new HeroKit("apep", "Horizon Devourer", AbilityEffect.WeakeningCurse, 2.4f,
                "Midnight Without End", AbilityEffect.Eclipse, 3.1f, "Primordial Hunger", HeroPassive.NightHunter,
                "Brand an opponent with unraveling shadow.", "Consume energy and strike the opposing team.", "Deal more damage to cursed targets.", 6.4f),
            new HeroKit("zeus", "King's Thunder", AbilityEffect.StunStrike, 2.7f,
                "Olympian Judgement", AbilityEffect.Eclipse, 3.4f, "Sky Sovereign", HeroPassive.Thunderlord,
                "Smite and interrupt a priority enemy.", "Call a kingdom-wide storm of thunder.", "Attacks sometimes disrupt opponents.", 5.8f),
            new HeroKit("nyx", "Veil of First Night", AbilityEffect.WeakeningCurse, 2.5f,
                "Before the Stars", AbilityEffect.Eclipse, 3.2f, "Ancient Darkness", HeroPassive.Sorcery,
                "Weaken a chosen enemy.", "Darken and stagger the enemy formation.", "Curses last longer.", 6.0f),
            new HeroKit("baldr", "Light of Return", AbilityEffect.HealTeam, 2.4f,
                "Tomorrow's Oath", AbilityEffect.ShieldTeam, 3.8f, "Unfading Hope", HeroPassive.Rebirth,
                "Mend the allied formation.", "Protect allies against a fatal onslaught.", "Regenerate health periodically.", 6.4f),
            new HeroKit("fenrir", "Fatebreaker Fangs", AbilityEffect.TripleStrike, 1.8f,
                "Ragnarok Bite", AbilityEffect.Execution, 4.7f, "The Unbound", HeroPassive.Executioner,
                "Tear through a single opponent three times.", "Pursue the weakest target with an execution blow.", "Finish weakened targets more effectively.", 5.8f),
            new HeroKit("izanagi", "Worldward", AbilityEffect.ShieldTeam, 2.6f,
                "Primordial Sanctuary", AbilityEffect.ShieldTeam, 4f, "Creator's Protection", HeroPassive.Guardian,
                "Protect every ally with ancient creation magic.", "Forge a formidable celestial sanctuary.", "Reduce incoming front-line damage.", 6.6f),
            new HeroKit("izanami", "Yomi's Embrace", AbilityEffect.LifeDrain, 2.35f,
                "Realm of No Return", AbilityEffect.Eclipse, 3.25f, "The Last Threshold", HeroPassive.Gravekeeper,
                "Drain a target's vitality.", "Conjure a dark gate across the battlefield.", "Gain a shield when an enemy falls.", 6.1f),
            new HeroKit("nuwa", "Mend the Sky", AbilityEffect.HealTeam, 2.85f,
                "Stones of Heaven", AbilityEffect.HealTeam, 4.0f, "Divine Compassion", HeroPassive.Compassion,
                "Restore allied vitality.", "Repair the entire formation with divine light.", "Healing grants protective shields.", 5.8f),
            new HeroKit("hundun", "Formless Maw", AbilityEffect.TauntGuard, 2.8f,
                "Untamed Beginning", AbilityEffect.Eclipse, 3.1f, "Chaos Before Form", HeroPassive.Ironhide,
                "Absorb attacks and disrupt enemy targeting.", "Distort the battlefield in chaotic darkness.", "Resist incoming damage.", 6.7f),
            new HeroKit("apollo", "Sunlit Arrow", AbilityEffect.HuntWeakest, 2.45f,
                "Delphic Radiance", AbilityEffect.AreaBlast, 3.65f, "Prophetic Dawn", HeroPassive.Dawnborn,
                "Pierce the enemy with a precise shaft of light.", "Illuminate and strike every enemy with prophetic radiance.",
                "The oracle's light increases healing received by allies.", 5.8f)
        };
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
