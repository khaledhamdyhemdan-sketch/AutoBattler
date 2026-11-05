#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesLoginVerification
    {
        [MenuItem("Tools/Chronicles/Test 28-Day Login Rewards")]
        public static void Run()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_login_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var player = new PlayerProgressService(folder);
                Verify(player.NextLoginRewardDay() == 1 &&
                    player.CanClaimDailyLogin(), "New player login day must be 1.");
                int priorGold = player.Data.gold;
                LoginReward first = player.ClaimDailyLogin();
                Verify(first != null && first.day == 1 && player.Data.gold > priorGold,
                    "First login reward was not granted.");
                Verify(player.ClaimDailyLogin() == null,
                    "Claiming twice in the same UTC day must be forbidden.");
                Verify(player.Data.loginDaysClaimed == 1,
                    "Duplicate claim must not advance progress.");

                // The production method uses real UTC; this simulates later dates
                // only inside an isolated disposable test save.
                for (int day = 2; day <= ChroniclesLoginRewards.Days; day++)
                {
                    player.Data.lastLoginGiftUtc =
                        DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
                    LoginReward reward = player.ClaimDailyLogin();
                    Verify(reward != null && reward.day == day,
                        "Consecutive reward progression failed on day " + day);
                }
                Verify(player.Data.loginDaysClaimed == 28 &&
                    player.NextLoginRewardDay() == 1,
                    "28-day cycle should roll over to day 1 next time.");
                Verify(player.Data.astralSeals >= 5,
                    "Day 14 and Day 28 must award at least five Astral Seals.");
                player.Data.lastLoginGiftUtc =
                    DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
                Verify(player.ClaimDailyLogin().day == 1 &&
                    player.Data.loginDaysClaimed == 1,
                    "New 28-day reward cycle failed.");

                Verify(player.Save(), "Could not save login progress.");
                var restored = new PlayerProgressService(folder);
                Verify(restored.Data.loginDaysClaimed == 1 &&
                    !restored.CanClaimDailyLogin(),
                    "Login claim or UTC date did not persist.");

                Debug.Log("[Chronicles] Offline 28-day login checks passed. " +
                    "Local time can be manipulated; production live rewards require server UTC.");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(folder)) Directory.Delete(folder, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Chronicles] Login test cleanup: " + e.Message);
                }
            }
        }

        private static void Verify(bool pass, string message)
        {
            if (!pass) throw new InvalidOperationException(message);
        }
    }
}
#endif
