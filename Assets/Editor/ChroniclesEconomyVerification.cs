#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesEconomyVerification
    {
        [MenuItem("Tools/Chronicles/Test Offline Shop And Mailbox")]
        public static void Run()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_shop_mail_" + Guid.NewGuid().ToString("N"));
            try
            {
                var player = new PlayerProgressService(folder);
                Check(ChroniclesShop.Offers.Length == 7,
                    "Seven resource-only shop offers are expected.");
                Check(player.UnclaimedMailCount() == 1,
                    "A new player should have exactly one welcome reward.");
                int gems = player.Data.gems;
                int tickets = player.Data.summonTickets;
                Check(player.ClaimMailReward("welcome_v1"),
                    "Welcome mail could not be claimed.");
                Check(player.Data.gems == gems + 60 &&
                    player.Data.summonTickets == tickets + 1,
                    "Welcome reward was not credited exactly once.");
                Check(!player.ClaimMailReward("welcome_v1") &&
                    player.UnclaimedMailCount() == 0,
                    "Claimed mail must not generate a second reward.");

                int initialGold = player.Data.gold;
                Check(player.ShopRemaining("daily_free") == 1,
                    "A free daily chest must be available once.");
                Check(player.BuyShopOffer("daily_free") &&
                    player.Data.gold == initialGold + 500,
                    "Free chest should grant 500 gold.");
                Check(!player.BuyShopOffer("daily_free") &&
                    player.ShopRemaining("daily_free") == 0,
                    "A sold-out chest must not pay twice.");

                int astral = player.Data.astralSeals;
                int paidGems = player.Data.gems;
                Check(player.BuyShopOffer("astral_gems"),
                    "Gem-to-Astral-Seal offer should be purchasable.");
                Check(player.Data.astralSeals == astral + 1 &&
                    player.Data.gems == paidGems - 260,
                    "Astral purchase must debit the exact cost once.");
                Check(!player.BuyShopOffer("astral_gems"),
                    "Astral Seal offer must have a daily limit of one.");

                int gold = player.Data.gold;
                player.Data.gold = 0;
                Check(!player.BuyShopOffer("ticket_gold") &&
                    player.Data.gold == 0,
                    "Insufficient gold must not issue a summon ticket.");
                player.Data.gold = gold;

                Check(player.Save(), "Could not save the shop/mailbox transaction.");
                var restored = new PlayerProgressService(folder);
                Check(restored.UnclaimedMailCount() == 0 &&
                    !restored.ClaimMailReward("welcome_v1"),
                    "Welcome mail claim must survive restart.");
                Check(restored.ShopRemaining("daily_free") == 0 &&
                    restored.ShopRemaining("astral_gems") == 0,
                    "Daily shop counters must survive restart.");

                restored.Data.shopUtcDay = "2020-01-01";
                Check(restored.Save(), "Could not save reset marker.");
                Check(restored.ShopRemaining("daily_free") == 1 &&
                    restored.BuyShopOffer("daily_free"),
                    "UTC day rollover should restore only the next day's stock.");
                Check(restored.UnclaimedMailCount() == 0,
                    "Shop reset must not recreate welcome mail.");
                Debug.Log("[Chronicles] Offline shop and mailbox checks passed. " +
                    "All balances are local; online economy requires the trusted server.");
            }
            finally
            {
                try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
                catch (Exception ex) { Debug.LogWarning(ex.Message); }
            }
        }

        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
#endif
