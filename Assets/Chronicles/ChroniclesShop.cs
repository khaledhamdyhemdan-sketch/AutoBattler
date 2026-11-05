using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public enum ShopCurrency { Free, Gold, Gems }
    public enum ShopReward { Gold, Gems, SummonTicket, AstralSeal, ForgeDust, RelicDust }

    public sealed class ShopOffer
    {
        public readonly string id, name, description;
        public readonly ShopCurrency currency;
        public readonly int cost, dailyLimit;
        public readonly ShopReward reward;
        public readonly int amount;

        public ShopOffer(string id, string name, string description,
            ShopCurrency currency, int cost, ShopReward reward, int amount, int dailyLimit)
        {
            this.id = id; this.name = name; this.description = description;
            this.currency = currency; this.cost = cost; this.reward = reward;
            this.amount = amount; this.dailyLimit = dailyLimit;
        }

        public string CostLabel
        {
            get
            {
                return currency == ShopCurrency.Free ? "FREE" :
                    cost + (currency == ShopCurrency.Gold ? " GOLD" : " GEMS");
            }
        }

        public string RewardLabel
        {
            get
            {
                switch (reward)
                {
                    case ShopReward.Gold: return amount + " Gold";
                    case ShopReward.Gems: return amount + " Gems";
                    case ShopReward.SummonTicket: return amount + " Summon Ticket";
                    case ShopReward.AstralSeal: return amount + " Astral Seal";
                    case ShopReward.ForgeDust: return amount + " Forge Dust";
                    case ShopReward.RelicDust: return amount + " Relic Dust";
                    default: return "Reward";
                }
            }
        }
    }

    [Serializable]
    public sealed class ShopPurchaseCounter
    {
        public string offerId;
        public int quantity;
    }

    [Serializable]
    public sealed class RewardMail
    {
        public string id, subject, body, createdUtc;
        public int gold, gems, tickets, astralSeals, forgeDust, relicDust;
        public bool claimed;

        public string RewardSummary
        {
            get
            {
                var items = new List<string>();
                if (gold > 0) items.Add(gold + " Gold");
                if (gems > 0) items.Add(gems + " Gems");
                if (tickets > 0) items.Add(tickets + " Tickets");
                if (astralSeals > 0) items.Add(astralSeals + " Astral Seals");
                if (forgeDust > 0) items.Add(forgeDust + " Forge Dust");
                if (relicDust > 0) items.Add(relicDust + " Relic Dust");
                return string.Join(" • ", items.ToArray());
            }
        }
    }

    /// <summary>
    /// Purely OFFLINE in-game resource exchange, no cash checkout or
    /// remote entitlements. Prices require balancing before public release.
    /// </summary>
    public static class ChroniclesShop
    {
        public static readonly ShopOffer[] Offers =
        {
            new ShopOffer("daily_free", "Daily Supply Chest", "A free daily resource cache.",
                ShopCurrency.Free, 0, ShopReward.Gold, 500, 1),
            new ShopOffer("ticket_gold", "Summon Ticket", "Exchange saved gold for one draw.",
                ShopCurrency.Gold, 2000, ShopReward.SummonTicket, 1, 2),
            new ShopOffer("forge_gold", "Forge Materials", "Strengthen your equipment.",
                ShopCurrency.Gold, 450, ShopReward.ForgeDust, 35, 5),
            new ShopOffer("relic_gold", "Relic Fragments", "Gather ancient artifact dust.",
                ShopCurrency.Gold, 2300, ShopReward.RelicDust, 15, 2),
            new ShopOffer("ticket_gems", "Standard Ticket", "Trade gems for a summon ticket.",
                ShopCurrency.Gems, 100, ShopReward.SummonTicket, 1, 3),
            new ShopOffer("astral_gems", "Astral Seal", "A rare Light / Dark banner token.",
                ShopCurrency.Gems, 260, ShopReward.AstralSeal, 1, 1),
            new ShopOffer("gold_gems", "Gold Supply", "Receive extra gold for training.",
                ShopCurrency.Gems, 60, ShopReward.Gold, 1100, 5)
        };

        public static ShopOffer Find(string id)
        {
            foreach (ShopOffer offer in Offers)
                if (offer.id == id) return offer;
            return null;
        }
    }
}
