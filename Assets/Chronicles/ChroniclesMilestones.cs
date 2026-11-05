namespace MythicChronicles
{
    public sealed class CampaignMilestoneReward
    {
        public readonly int stage, gold, gems, tickets, astralSeals, forgeDust;
        public CampaignMilestoneReward(int stage, int gold, int gems,
            int tickets, int astralSeals, int forgeDust)
        {
            this.stage = stage; this.gold = gold; this.gems = gems;
            this.tickets = tickets; this.astralSeals = astralSeals;
            this.forgeDust = forgeDust;
        }

        public string Summary
        {
            get
            {
                return gold + " Gold • " + gems + " Gems • " +
                    tickets + " Tickets • " + astralSeals + " Astral Seals";
            }
        }
    }

    public static class ChroniclesMilestones
    {
        public static readonly CampaignMilestoneReward[] All =
        {
            new CampaignMilestoneReward(20, 3000, 100, 5, 1, 100),
            new CampaignMilestoneReward(40, 6000, 200, 8, 2, 200),
            new CampaignMilestoneReward(60, 10000, 350, 12, 3, 300)
        };

        public static CampaignMilestoneReward At(int stage)
        {
            foreach (CampaignMilestoneReward reward in All)
                if (reward.stage == stage) return reward;
            return null;
        }
    }
}
