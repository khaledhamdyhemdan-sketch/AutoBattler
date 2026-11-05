#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesSummonVerification
    {
        [MenuItem("Tools/Chronicles/Test Standard & UR Banners")]
        public static void Run()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_banner_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                PlayerProgressService player = new PlayerProgressService(folder);
                player.Data.gems = 100000;
                SummonBatchResult bad = player.DrawHeroes(SummonBannerType.Standard, 11);
                Verify(!bad.success, "Only single and ten-pulls should be allowed.");
                SummonBatchResult ten = player.DrawHeroes(SummonBannerType.Standard, 10);
                Verify(ten.success && ten.heroIds.Count == 10,
                    "Standard 10-pull failed.");
                Verify(player.Data.summonHistory.Count == 10,
                    "Ten-pull history must record all ten outcomes.");

                int standardCount = 10;
                int ssr = 0;
                foreach (string id in ten.heroIds)
                {
                    HeroDefinition hero = WorldsCatalog.FindHero(id);
                    Verify(hero != null && hero.rarity != Rarity.UR &&
                        hero.rarity != Rarity.EX,
                        "Standard banner incorrectly awarded a UR or EX hero.");
                    if (hero.rarity == Rarity.SSR) ssr++;
                }
                while (standardCount < ChroniclesSummonRules.StandardSsrPity)
                {
                    SummonBatchResult draw = player.DrawHeroes(SummonBannerType.Standard, 1);
                    Verify(draw.success, "Standard single pull failed.");
                    HeroDefinition hero = WorldsCatalog.FindHero(draw.heroIds[0]);
                    Verify(hero.rarity != Rarity.UR, "Standard banner awarded a UR.");
                    if (hero.rarity == Rarity.SSR) ssr++;
                    standardCount++;
                }
                Verify(ssr >= 1, "Standard pity did not produce an SSR within 40 draws.");

                int urCount = 0;
                for (int i = 0; i < ChroniclesSummonRules.AstralUrPity; i++)
                {
                    SummonBatchResult draw = player.DrawHeroes(
                        SummonBannerType.AstralCovenant, 1);
                    Verify(draw.success, "Astral draw failed.");
                    HeroDefinition hero = WorldsCatalog.FindHero(draw.heroIds[0]);
                    Verify(hero.rarity == Rarity.SR ||
                        hero.rarity == Rarity.SSR || hero.rarity == Rarity.UR,
                        "Astral pool includes an invalid tier.");
                    if (hero.rarity == Rarity.UR)
                    {
                        urCount++;
                        Verify(HeroElements.IsSpecial(hero),
                            "Astral UR must have Light or Darkness.");
                    }
                }
                Verify(urCount >= 1,
                    "UR pity must award a Light/Dark champion within 70 draws.");

                // Deterministic hard-pity edge: pity 69 guarantees next UR.
                player.Data.astralPity = ChroniclesSummonRules.AstralUrPity - 1;
                player.Data.astralSeals = 1;
                int priorGems = player.Data.gems;
                SummonBatchResult guaranteed = player.DrawHeroes(
                    SummonBannerType.AstralCovenant, 1);
                Verify(guaranteed.success &&
                    WorldsCatalog.FindHero(guaranteed.heroIds[0]).rarity == Rarity.UR,
                    "Guaranteed UR edge case failed.");
                Verify(player.Data.astralPity == 0 &&
                    player.Data.gems == priorGems && player.Data.astralSeals == 0,
                    "Astral seal or pity was not consumed/reset correctly.");

                Verify(player.Save(), "Save failed.");
                var restored = new PlayerProgressService(folder);
                Verify(restored.Data.astralPity == 0 &&
                    restored.Data.summonHistory.Count == ChroniclesSummonRules.HistoryLimit,
                    "History, pity or currency did not survive reload.");
                int historyBefore = restored.Data.summonHistory.Count;
                restored.Data.gems = 0;
                restored.Data.summonTickets = 0;
                restored.Data.astralSeals = 0;
                Verify(!restored.DrawHeroes(SummonBannerType.AstralCovenant, 10).success &&
                    restored.Data.summonHistory.Count == historyBefore,
                    "Insufficient currency must not generate rewards.");

                Debug.Log("[Chronicles] Offline banner checks passed. " +
                    "These tests do not certify server security, fairness or Android playability.");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(folder)) Directory.Delete(folder, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Chronicles] Test cleanup: " + ex.Message);
                }
            }
        }

        private static void Verify(bool ok, string error)
        {
            if (!ok) throw new InvalidOperationException(error);
        }
    }
}
#endif
