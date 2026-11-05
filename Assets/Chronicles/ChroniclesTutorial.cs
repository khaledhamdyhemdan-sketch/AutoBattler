namespace MythicChronicles
{
    /// <summary>
    /// English first-time-player guidance. Transitions are controlled by the
    /// game UI; choices are saved locally and never change wallet balances.
    /// </summary>
    public static class ChroniclesTutorial
    {
        public const int StepCount = 5;
        public static readonly string[] Prompts =
        {
            "WELCOME! Explore a world of 41 mythic heroes.",
            "HEROES: inspect rarity, element and signature skills.",
            "FORMATION: assemble five heroes before entering combat.",
            "ADVENTURE: clear story chapters, Elite and Dark trials.",
            "SUMMON: spend earned tickets or gems to expand your roster."
        };

        public static string Description(int step)
        {
            return step >= 0 && step < Prompts.Length ?
                Prompts[step] : "TUTORIAL COMPLETE";
        }
    }
}
