using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    [Serializable]
    public sealed class FriendsState
    {
        public string giftUtcDay = "";
        public List<string> giftsClaimedToday = new List<string>();
        public int friendPoints;
        public string mercenaryUtcDay = "";
        public string mercenaryUsedHeroId = "";
        public bool mercenaryPending;
    }

    /// <summary>
    /// LOCAL-FIRST FRIENDS + MERCENARY PROTOTYPE. Five deterministic simulated
    /// friends send a daily gift (friend points and gold), and each shows a
    /// champion hero the player can hire as a mercenary: hiring grants a
    /// one-battle team power blessing, once per UTC day. Real friends, real
    /// hero lending and chat arrive with the backend; the gift/mercenary
    /// economy and page stay.
    /// </summary>
    public static class ChroniclesFriends
    {
        public sealed class FriendDef
        {
            public string name, championId, championLabel;
            public int giftGold, giftPoints;
        }

        public static readonly FriendDef[] Friends =
        {
            new FriendDef { name = "NileFang", championId = "ra",
                championLabel = "Ra (UR)", giftGold = 150, giftPoints = 10 },
            new FriendDef { name = "SakuraRonin", championId = "amaterasu",
                championLabel = "Amaterasu (UR)", giftGold = 120, giftPoints = 10 },
            new FriendDef { name = "ValkyrieSif", championId = "odin",
                championLabel = "Odin (UR)", giftGold = 130, giftPoints = 10 },
            new FriendDef { name = "OlympianFox", championId = "hades",
                championLabel = "Hades (UR)", giftGold = 110, giftPoints = 10 },
            new FriendDef { name = "ThebanGuard", championId = "horus",
                championLabel = "Horus (SSR)", giftGold = 100, giftPoints = 10 },
        };

        // Friend points buy small daily shop items; mercenary hire costs points.
        public const int MercenaryCost = 30;

        // A hired mercenary blesses the team for one battle (+8% attack/health),
        // standing in for the friend's champion until real hero lending exists.
        public const float MercenaryPowerBonus = .08f;
    }
}
