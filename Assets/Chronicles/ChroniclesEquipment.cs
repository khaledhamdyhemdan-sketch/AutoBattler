using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public enum GearSlot { Weapon, Helmet, Armor, Boots, Accessory, Charm }
    public enum GearSet { Sun, Moon, Storm }
    public enum GearRarity { Common, Rare, Epic, Legendary }

    [Serializable]
    public sealed class GearInstance
    {
        public string instanceId;
        public string templateId;
        public int level;
        public string equippedHeroId; // empty = in bag
    }

    public sealed class GearTemplate
    {
        public readonly string id, name;
        public readonly GearSlot slot;
        public readonly GearSet set;
        public readonly GearRarity rarity;
        public readonly int attack, health, defense;
        public GearTemplate(string id, string name, GearSlot slot, GearSet set,
            GearRarity rarity, int attack, int health, int defense)
        {
            this.id = id; this.name = name; this.slot = slot; this.set = set;
            this.rarity = rarity; this.attack = attack; this.health = health;
            this.defense = defense;
        }
    }

    public struct EquipmentBonuses
    {
        public float attack, health, defense;
        public float attackMultiplier, healthMultiplier;
        public int equippedCount;
        public string summary;
    }

    // An original set-driven six-slot equipment system. Templates are game design
    // data, not proof of item art or completed content tuning.
    public static class ChroniclesEquipment
    {
        public static readonly GearTemplate[] Templates = BuildTemplates();
        private static readonly Dictionary<string, GearTemplate> Index = BuildIndex();

        private static GearTemplate[] BuildTemplates()
        {
            var list = new List<GearTemplate>();
            foreach (GearSet set in Enum.GetValues(typeof(GearSet)))
            {
                foreach (GearSlot slot in Enum.GetValues(typeof(GearSlot)))
                {
                    string key = set.ToString().ToLowerInvariant() + "_" +
                        slot.ToString().ToLowerInvariant();
                    int tier = (int)set + 1;
                    int atk = slot == GearSlot.Weapon || slot == GearSlot.Accessory ? 14 * tier : 3 * tier;
                    int hp = slot == GearSlot.Armor || slot == GearSlot.Charm ? 65 * tier : 12 * tier;
                    int def = slot == GearSlot.Helmet || slot == GearSlot.Boots ? 9 * tier : 2 * tier;
                    GearRarity rarity = set == GearSet.Storm ? GearRarity.Legendary :
                        set == GearSet.Moon ? GearRarity.Epic : GearRarity.Rare;
                    list.Add(new GearTemplate(key, set + " " + slot, slot, set,
                        rarity, atk, hp, def));
                }
            }
            return list.ToArray();
        }

        private static Dictionary<string, GearTemplate> BuildIndex()
        {
            var result = new Dictionary<string, GearTemplate>(StringComparer.Ordinal);
            foreach (GearTemplate item in Templates) result.Add(item.id, item);
            return result;
        }

        public static GearTemplate Find(string id)
        {
            GearTemplate result;
            return id != null && Index.TryGetValue(id, out result) ? result : null;
        }

        public static GearTemplate RewardForStage(int stageId)
        {
            if (stageId < 1 || stageId > 60) return null;
            // Deterministic first-clear drops, no reroll exploit on repeated clears.
            int setTier = stageId <= 20 ? 0 : stageId <= 40 ? 1 : 2;
            int slot = (stageId - 1) % 6;
            return Find(((GearSet)setTier).ToString().ToLowerInvariant() + "_" +
                ((GearSlot)slot).ToString().ToLowerInvariant());
        }

        public static int UpgradeCost(GearInstance item)
        {
            if (item == null) return 0;
            GearTemplate template = Find(item.templateId);
            return template == null ? 0 : 50 + 55 * item.level + 45 * (int)template.rarity;
        }

        public static EquipmentBonuses Calculate(IList<GearInstance> equipment, string heroId)
        {
            EquipmentBonuses bonus = new EquipmentBonuses
            {
                attackMultiplier = 1f,
                healthMultiplier = 1f,
                summary = "No equipment set"
            };
            if (equipment == null || string.IsNullOrEmpty(heroId)) return bonus;
            int[] counts = new int[3];
            var slots = new HashSet<GearSlot>();
            foreach (GearInstance item in equipment)
            {
                if (item == null || item.equippedHeroId != heroId) continue;
                GearTemplate template = Find(item.templateId);
                if (template == null || !slots.Add(template.slot)) continue;
                float scale = 1f + Math.Max(0, Math.Min(30, item.level)) * .12f;
                bonus.attack += template.attack * scale;
                bonus.health += template.health * scale;
                bonus.defense += template.defense * scale;
                counts[(int)template.set]++;
                bonus.equippedCount++;
            }
            var descriptions = new List<string>();
            for (int index = 0; index < counts.Length; index++)
            {
                int count = counts[index];
                if (count < 2) continue;
                GearSet set = (GearSet)index;
                descriptions.Add(set + " " + count + "/6");
                if (set == GearSet.Sun)
                {
                    bonus.attackMultiplier += count >= 4 ? .12f : .05f;
                }
                else if (set == GearSet.Moon)
                {
                    bonus.healthMultiplier += count >= 4 ? .13f : .06f;
                }
                else
                {
                    bonus.attackMultiplier += count >= 4 ? .08f : .035f;
                    bonus.healthMultiplier += count >= 4 ? .08f : .035f;
                }
            }
            if (descriptions.Count > 0) bonus.summary = string.Join(", ", descriptions.ToArray());
            return bonus;
        }
    }
}
