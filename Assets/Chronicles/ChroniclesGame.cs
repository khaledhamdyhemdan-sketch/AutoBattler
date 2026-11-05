using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    // Integrated campaign navigation. Current IMGUI layout is a functional scaffold
    // to be replaced by production UI Toolkit art without changing progression logic.
    public sealed class ChroniclesGame : MonoBehaviour
    {
        private enum Page { Home, Chapters, StageStory, Heroes, HeroDetails, Formation, Summon, Quests, WeeklyQuests, Idle, Battle, Results }
        private Page page = Page.Home;
        private PlayerProgressService progress;
        private ChroniclesBattle battle;
        private int stageId = 1;
        private string selectedHero = "horus";
        private bool showHeroSkills;
        private float storyStartedAt;
        private string notice = "";
        private string lastSummoned = "";
        private bool lastVictory;
        private Vector2 chapterScroll, rosterScroll, formationScroll;
        private GUIStyle heading, subtitle, tiny;
        private GUIStyle bigButton;
        private Texture2D panelTex;
        private float panelWidth = 540f;

        private void Awake()
        {
            #if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.Portrait;
            #endif
        }

        private void Start()
        {
            progress = new PlayerProgressService();
            stageId = Mathf.Clamp(progress.Data.lastClearedStage + 1, 1, 60);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && progress != null) progress.Save();
        }
        private void OnApplicationQuit()
        {
            if (progress != null) progress.Save();
        }

        private void InitializeStyles()
        {
            if (heading != null) return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 27, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, wordWrap = true };
            heading.normal.textColor = new Color(1f, .85f, .43f);
            subtitle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true,
                alignment = TextAnchor.MiddleCenter };
            subtitle.normal.textColor = new Color(.87f, .86f, .99f);
            tiny = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            tiny.normal.textColor = new Color(.84f, .83f, .94f);
            bigButton = new GUIStyle(GUI.skin.button) { fontSize = 19, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, wordWrap = true };
            panelTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            panelTex.SetPixel(0, 0, new Color(.09f, .08f, .18f, .96f));
            panelTex.Apply();
        }

        private void OnDestroy()
        {
            if (panelTex != null) Destroy(panelTex);
        }

        private void OnGUI()
        {
            if (progress == null) return;
            InitializeStyles();
            float scale = Mathf.Min(Screen.width / panelWidth, Screen.height / 960f);
            Matrix4x4 matrix = GUI.matrix;
            Color oldColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            GUI.color = Color.white;

            if (page != Page.Battle && page != Page.Results)
            {
                GUI.DrawTexture(new Rect(0, 0, 540, 960), panelTex);
                Header();
            }
            switch (page)
            {
                case Page.Home: Home(); break;
                case Page.Chapters: Chapters(); break;
                case Page.StageStory: StageStory(); break;
                case Page.Heroes: Heroes(); break;
                case Page.HeroDetails: HeroDetails(); break;
                case Page.Formation: Formation(); break;
                case Page.Summon: Summon(); break;
                case Page.Quests: Quests(false); break;
                case Page.WeeklyQuests: Quests(true); break;
                case Page.Idle: Idle(); break;
                case Page.Battle: BattleHud(scale); break;
                case Page.Results: Results(); break;
            }
            GUI.color = oldColor;
            GUI.matrix = matrix;
        }

        private void Header()
        {
            GUI.Label(new Rect(20, 20, 500, 54), "CHRONICLES OF THE LOOM", heading);
            GUI.Label(new Rect(25, 66, 490, 34), "An original crossover of myths, legends and histories", subtitle);
            GUI.Box(new Rect(15, 112, 510, 55), "");
            GUI.Label(new Rect(28, 124, 485, 30), "GOLD " + progress.Data.gold + "   |   GEMS " +
                progress.Data.gems + "   |   TICKETS " + progress.Data.summonTickets, tiny);
            if (page != Page.Home && GUI.Button(new Rect(20, 183, 105, 42), "HOME")) GoHome();
            if (!string.IsNullOrEmpty(notice))
                GUI.Label(new Rect(20, 903, 500, 43), notice, subtitle);
        }

        private void Home()
        {
            GUI.Label(new Rect(30, 200, 480, 85), "The Loom of Ages is fractured.\nReunite heroes from every story.", subtitle);
            GUI.Box(new Rect(35, 290, 470, 107),
                "CAMPAIGN PROGRESS\n" + progress.Data.lastClearedStage + " / 60 stages cleared\n" +
                "Current chapter: " + WorldsCatalog.Chapters[Mathf.Min(5, progress.Data.lastClearedStage / 10)].title);
            if (GUI.Button(new Rect(40, 421, 460, 64), "CAMPAIGN", bigButton)) page = Page.Chapters;
            if (GUI.Button(new Rect(40, 497, 222, 62), "HEROES (" + progress.Data.heroes.Count + ")", bigButton)) page = Page.Heroes;
            if (GUI.Button(new Rect(274, 497, 226, 62), "FORMATION", bigButton)) page = Page.Formation;
            if (GUI.Button(new Rect(40, 570, 222, 62), "SUMMON", bigButton)) page = Page.Summon;
            if (GUI.Button(new Rect(274, 570, 226, 62), "DAILY QUESTS", bigButton)) page = Page.Quests;
            if (GUI.Button(new Rect(40, 645, 222, 64), "WEEKLY QUESTS", bigButton))
                page = Page.WeeklyQuests;
            if (GUI.Button(new Rect(274, 645, 226, 64), "AFK REWARDS", bigButton))
                page = Page.Idle;
            GUI.Label(new Rect(40, 748, 460, 125),
                "Six connected chapters • 30 collectible heroes\n" +
                "Different myths, one original storyline.", subtitle);
        }

        private void Chapters()
        {
            GUI.Label(new Rect(130, 177, 380, 54), "STORY CAMPAIGN", heading);
            chapterScroll = GUI.BeginScrollView(new Rect(20, 242, 505, 635), chapterScroll,
                new Rect(0, 0, 475, 6 * 498));
            for (int c = 0; c < WorldsCatalog.Chapters.Length; c++)
            {
                ChapterDefinition chapter = WorldsCatalog.Chapters[c];
                float y = c * 498;
                GUI.Box(new Rect(5, y, 465, 482), "");
                GUI.Label(new Rect(15, y + 10, 430, 44), chapter.number + ". " + chapter.title, subtitle);
                GUI.Label(new Rect(20, y + 52, 425, 60), chapter.summary, tiny);
                for (int j = 0; j < 10; j++)
                {
                    int id = c * 10 + j + 1;
                    bool unlocked = id <= progress.Data.lastClearedStage + 1;
                    int col = j % 5, row = j / 5;
                    GUI.enabled = unlocked;
                    if (GUI.Button(new Rect(20 + col * 88, y + 135 + row * 106, 82, 84),
                        (j == 9 ? "BOSS\n" : "STAGE\n") + id +
                        (id <= progress.Data.lastClearedStage ? "\nCLEARED" : "")))
                    {
                        stageId = id;
                        storyStartedAt = Time.unscaledTime;
                        page = Page.StageStory;
                        notice = "";
                    }
                    GUI.enabled = true;
                }
                GUI.Label(new Rect(20, y + 367, 428, 70), "Chapter boss: " + chapter.boss, tiny);
            }
            GUI.EndScrollView();
        }

        private void StageStory()
        {
            StageDefinition stage = WorldsCatalog.GetStage(stageId);
            if (stage == null) { page = Page.Chapters; return; }
            StoryMoment moment = WorldsStory.Get(stageId);
            Texture2D background = ChroniclesStageArt.Background(
                WorldsCatalog.Chapters[stage.chapter - 1].origin);
            if (background != null)
                GUI.DrawTexture(new Rect(27, 241, 486, 216), background, ScaleMode.ScaleAndCrop);
            GUI.Label(new Rect(22, 257, 496, 78),
                stage.boss ? "BOSS ENCOUNTER" : "STORY ENCOUNTER", heading);
            GUI.Label(new Rect(25, 349, 490, 72), stage.name, heading);

            GUI.Box(new Rect(32, 458, 476, 196), "");
            string speaker = moment == null ? "Narrator" : moment.speaker;
            string line = moment == null ? stage.narrative : moment.line;
            Texture2D portrait = ChroniclesStageArt.Portrait(speaker);
            float textWidth = portrait == null ? 439f : 288f;
            GUI.Label(new Rect(45, 466, textWidth, 37), speaker, subtitle);
            int visibleCharacters = Mathf.Clamp(
                Mathf.FloorToInt((Time.unscaledTime - storyStartedAt) * 32f), 0, line.Length);
            GUI.Label(new Rect(45, 506, textWidth, 140),
                line.Substring(0, visibleCharacters), subtitle);

            if (portrait != null)
            {
                float slide = 1f - Mathf.Clamp01((Time.unscaledTime - storyStartedAt) * 2f);
                GUI.DrawTexture(new Rect(350f + slide * 70f, 465f, 141f, 180f),
                    portrait, ScaleMode.ScaleToFit);
            }
            if (GUI.Button(new Rect(180, 658, 180, 35), "SHOW ALL DIALOGUE"))
                storyStartedAt = Time.unscaledTime - 30f;
            GUI.Label(new Rect(35, 690, 475, 50),
                "Recommended power: " + stage.recommendedPower +
                "  |  First-clear: " + stage.gold + " Gold, " +
                stage.gems + " Gems", subtitle);
            if (GUI.Button(new Rect(42, 753, 458, 70), "PREPARE TEAM", bigButton))
            {
                notice = "";
                page = Page.Formation;
            }
            if (GUI.Button(new Rect(42, 831, 458, 51), "BACK TO CHAPTERS"))
                page = Page.Chapters;
        }

        private void Heroes()
        {
            GUI.Label(new Rect(130, 181, 300, 50), "HERO COLLECTION", heading);
            GUI.Label(new Rect(30, 222, 475, 48), "Collected " + progress.Data.heroes.Count + " of " + WorldsCatalog.Heroes.Length + " characters", subtitle);
            rosterScroll = GUI.BeginScrollView(new Rect(20, 283, 505, 586), rosterScroll,
                new Rect(0, 0, 474, WorldsCatalog.Heroes.Length * 76));
            for (int i = 0; i < WorldsCatalog.Heroes.Length; i++)
            {
                HeroDefinition hero = WorldsCatalog.Heroes[i];
                OwnedHero owned = progress.FindOwnedHero(hero.id);
                string text = hero.name + "   [" + hero.origin + "]  " + hero.role + "\n" +
                    (owned == null ? "LOCKED • " + hero.rarity : "LV " + owned.level + "   " + hero.rarity);
                if (GUI.Button(new Rect(4, i * 76, 461, 69), text))
                { selectedHero = hero.id; notice = ""; page = Page.HeroDetails; }
            }
            GUI.EndScrollView();
        }

        private void HeroDetails()
        {
            HeroDefinition hero = WorldsCatalog.FindHero(selectedHero);
            if (hero == null) { page = Page.Heroes; return; }
            OwnedHero owned = progress.FindOwnedHero(hero.id);
            HeroKit kit = HeroAbilityCatalog.For(hero.id);
            GUI.Label(new Rect(25, 243, 490, 62), hero.name.ToUpperInvariant(), heading);
            GUI.Label(new Rect(28, 309, 484, 39), hero.origin + "  •  " + hero.role + "  •  " + hero.rarity, subtitle);
            GUI.Box(new Rect(36, 358, 465, 197), "");
            if (GUI.Button(new Rect(49, 367, 216, 37), "STORY")) showHeroSkills = false;
            if (GUI.Button(new Rect(275, 367, 216, 37), "ABILITIES")) showHeroSkills = true;
            if (showHeroSkills && kit != null)
            {
                GUI.Label(new Rect(52, 410, 438, 138),
                    "Skill: " + kit.skillName + " — " + kit.skillDescription +
                    "\nUltimate: " + kit.ultimateName + " — " + kit.ultimateDescription +
                    "\nPassive: " + kit.passiveName + " — " + kit.passiveDescription, tiny);
            }
            else GUI.Label(new Rect(52, 421, 438, 120), hero.story, subtitle);

            if (owned == null)
                GUI.Label(new Rect(32, 569, 476, 67), "Not yet summoned. Use earned tickets or gems.", subtitle);
            else
            {
                GUI.Label(new Rect(32, 566, 472, 74),
                    "Level " + owned.level + "  •  Stars " + owned.stars +
                    "\nWeapon +" + owned.weaponLevel + "  •  Armor +" + owned.armorLevel +
                    "  •  Fragments " + owned.fragments, subtitle);
                if (GUI.Button(new Rect(40, 650, 460, 59), "UPGRADE HERO", bigButton))
                    notice = progress.UpgradeHero(hero.id) ? "Level increased and saved." :
                        "Upgrade unavailable: gold or level cap.";
                if (GUI.Button(new Rect(40, 718, 219, 60), "UPGRADE WEAPON"))
                    notice = progress.UpgradeEquipment(hero.id, true) ? "Weapon improved!" :
                        "Weapon upgrade unavailable.";
                if (GUI.Button(new Rect(278, 718, 222, 60), "UPGRADE ARMOR"))
                    notice = progress.UpgradeEquipment(hero.id, false) ? "Armor improved!" :
                        "Armor upgrade unavailable.";
                if (GUI.Button(new Rect(40, 787, 460, 51),
                    "ASCEND (" + (owned.stars * 20) + " FRAGMENTS)"))
                    notice = progress.AscendHero(hero.id) ? "Star rank increased!" :
                        "Not enough fragments/gold or max stars.";
            }
            if (GUI.Button(new Rect(40, 849, 460, 46), "HERO COLLECTION")) page = Page.Heroes;
        }

        private void Formation()
        {
            GUI.Label(new Rect(125, 182, 360, 50), "TEAM FORMATION", heading);
            GUI.Label(new Rect(35, 235, 466, 66),
                "Select five owned heroes. Order is used to place your team in front and back rows.", subtitle);
            formationScroll = GUI.BeginScrollView(new Rect(20, 317, 500, 413), formationScroll,
                new Rect(0, 0, 473, progress.Data.heroes.Count * 57));
            for (int i = 0; i < progress.Data.heroes.Count; i++)
            {
                OwnedHero owned = progress.Data.heroes[i];
                HeroDefinition hero = WorldsCatalog.FindHero(owned.id);
                if (hero == null) continue;
                int picked = progress.Data.selectedTeam.IndexOf(owned.id);
                string label = (picked >= 0 ? "[SLOT " + (picked + 1) + "] " : "[ + ] ") +
                    hero.name + "   Level " + owned.level + "   " + hero.role;
                if (GUI.Button(new Rect(5, i * 57, 457, 52), label))
                {
                    List<string> temp = new List<string>(progress.Data.selectedTeam);
                    if (picked >= 0) temp.Remove(owned.id);
                    else if (temp.Count < 5) temp.Add(owned.id);
                    if (temp.Count == 5)
                        notice = progress.SetTeam(temp) ? "Team saved." : "Could not save team.";
                    else
                    {
                        // Temporarily allow incomplete in the formation editor.
                        progress.Data.selectedTeam = temp;
                        notice = temp.Count + "/5 heroes selected. Complete your team to save.";
                    }
                }
            }
            GUI.EndScrollView();
            var synergyHeroes = new List<HeroDefinition>();
            foreach (string id in progress.Data.selectedTeam)
            {
                HeroDefinition hero = WorldsCatalog.FindHero(id);
                if (hero != null) synergyHeroes.Add(hero);
            }
            TeamSynergyBonus bonus = TeamSynergy.Calculate(synergyHeroes);
            GUI.Label(new Rect(30, 737, 480, 63),
                "Selected: " + progress.Data.selectedTeam.Count + "/5  |  " + bonus.description +
                "\nHP +" + Mathf.RoundToInt((bonus.hpMultiplier - 1f) * 100f) +
                "% • ATK +" + Mathf.RoundToInt((bonus.attackMultiplier - 1f) * 100f) +
                "% • Start energy +" + bonus.startingEnergy, subtitle);
            GUI.enabled = progress.Data.selectedTeam.Count == 5;
            if (GUI.Button(new Rect(40, 803, 460, 70), "START STAGE " + stageId, bigButton))
                BeginBattle();
            GUI.enabled = true;
        }

        private void Summon()
        {
            GUI.Label(new Rect(25, 250, 490, 65), "SUMMONING CHAMBER", heading);
            GUI.Box(new Rect(40, 347, 460, 135), "Bring a hero from another age into your collection.");
            GUI.Label(new Rect(28, 503, 488, 86),
                "Cost: 1 earned ticket or 120 earned gems\n" +
                "Epic pool 75% • Legendary pool 25%\nDuplicates become 10 hero fragments.", subtitle);
            if (GUI.Button(new Rect(40, 621, 460, 80), "SUMMON ONE HERO", bigButton))
            {
                string chosen = progress.SummonHero();
                lastSummoned = chosen ?? "";
                notice = chosen == null ? "Not enough tickets or gems." :
                    "Summoned: " + WorldsCatalog.FindHero(chosen).name;
            }
            if (!string.IsNullOrEmpty(lastSummoned))
                GUI.Label(new Rect(40, 730, 460, 66), "LATEST: " + WorldsCatalog.FindHero(lastSummoned).name, heading);
        }

        private void Quests(bool weekly)
        {
            GUI.Label(new Rect(22, 249, 490, 60), weekly ? "WEEKLY MISSIONS" : "DAILY MISSIONS", heading);
            if (GUI.Button(new Rect(25, 306, 244, 40), "DAILY")) page = Page.Quests;
            if (GUI.Button(new Rect(276, 306, 244, 40), "WEEKLY")) page = Page.WeeklyQuests;
            QuestInfo[] quests = weekly ? PlayerProgressService.WeeklyQuests : PlayerProgressService.DailyQuests;
            for (int i = 0; i < quests.Length; i++)
            {
                QuestInfo quest = quests[i];
                int amount = weekly ? progress.WeeklyQuestProgress(quest.id) : progress.QuestProgress(quest.id);
                bool claimed = weekly ? progress.Data.claimedWeeklyQuests.Contains(quest.id)
                    : progress.Data.claimedDailyQuests.Contains(quest.id);
                float y = 361 + i * 99;
                GUI.Box(new Rect(21, y, 500, 87), "");
                GUI.Label(new Rect(37, y + 8, 276, 67),
                    quest.title + "\n" + Math.Min(quest.target, amount) + "/" + quest.target +
                    "  •  " + quest.goldReward + " gold / " + quest.gemReward + " gems", tiny);
                GUI.enabled = !claimed && amount >= quest.target;
                if (GUI.Button(new Rect(340, y + 17, 157, 54), claimed ? "CLAIMED" : "CLAIM"))
                    notice = (weekly ? progress.ClaimWeeklyQuest(quest.id) : progress.ClaimDailyQuest(quest.id))
                        ? "Mission reward claimed!" : "Mission not ready.";
                GUI.enabled = true;
            }
        }

                private void Idle()
        {
            GUI.Label(new Rect(30, 275, 480, 76), "AFK TREASURY", heading);
            GUI.Box(new Rect(41, 388, 459, 177), "Your adventurers gather resources while you are away.");
            GUI.Label(new Rect(26, 582, 492, 98),
                "Elapsed: " + progress.IdleAvailableMinutes() + " / 720 minutes\n" +
                "Gold waiting: " + progress.IdleProjectedGold(), subtitle);
            if (GUI.Button(new Rect(40, 714, 460, 76), "CLAIM IDLE GOLD", bigButton))
            {
                int gold = progress.ClaimIdleGold();
                notice = gold > 0 ? "Collected " + gold + " gold." : "Not enough time elapsed yet.";
            }
        }

        private void BeginBattle()
        {
            if (progress.Data.selectedTeam.Count != 5)
            { notice = "Select exactly five heroes first."; return; }
            if (!progress.SetTeam(progress.Data.selectedTeam))
            { notice = "Invalid team."; return; }
            var chosen = new List<OwnedHero>();
            foreach (var id in progress.Data.selectedTeam)
            {
                OwnedHero unit = progress.FindOwnedHero(id);
                if (unit == null) { notice = "A selected hero is not owned."; return; }
                chosen.Add(unit);
            }
            StageDefinition stage = WorldsCatalog.GetStage(stageId);
            if (stage == null || stageId > progress.Data.lastClearedStage + 1)
            { notice = "Stage is locked."; return; }
            if (battle != null) Destroy(battle.gameObject);
            battle = new GameObject("Chronicles - Campaign Battle").AddComponent<ChroniclesBattle>();
            battle.Initialize(stage, chosen, OnBattleFinished);
            page = Page.Battle; notice = "";
        }

        private void OnBattleFinished(bool victory)
        {
            lastVictory = victory;
            if (victory) progress.CompleteStage(stageId);
            page = Page.Results;
        }

        private void BattleHud(float scale)
        {
            if (battle == null) return;
            battle.DrawWorldLabels(scale);
            GUI.Box(new Rect(12, 22, 516, 106), "");
            GUI.Label(new Rect(20, 29, 500, 45), WorldsCatalog.GetStage(stageId).name, subtitle);
            GUI.Label(new Rect(29, 73, 460, 36), "TIME " + Mathf.CeilToInt(battle.SecondsLeft) + " s", subtitle);
            if (GUI.Button(new Rect(20, 837, 153, 65), battle.Paused ? "RESUME" : "PAUSE"))
                battle.Paused = !battle.Paused;
            if (GUI.Button(new Rect(184, 837, 159, 65), "SPEED x" + battle.Speed))
                battle.Speed = battle.Speed >= 2f ? 1f : 2f;
            if (GUI.Button(new Rect(354, 837, 160, 65), "RETREAT"))
            {
                Destroy(battle.gameObject); battle = null;
                page = Page.Formation;
            }
        }

        private void Results()
        {
            GUI.DrawTexture(new Rect(0, 0, 540, 960), panelTex);
            GUI.Label(new Rect(25, 248, 490, 95), lastVictory ? "VICTORY!" : "DEFEAT", heading);
            StageDefinition stage = WorldsCatalog.GetStage(stageId);
            GUI.Label(new Rect(35, 362, 470, 100), stage.name, subtitle);
            GUI.Box(new Rect(38, 480, 464, 142),
                lastVictory ? "First-clear rewards are added once. Your progress is saved." :
                    "Upgrade your heroes or change formation to try again.");
            if (GUI.Button(new Rect(42, 689, 456, 70), lastVictory ? "NEXT STAGE" : "RETRY", bigButton))
            {
                if (lastVictory) stageId = Mathf.Min(60, stageId + 1);
                page = Page.Formation;
                CleanupBattle();
            }
            if (GUI.Button(new Rect(42, 781, 456, 65), "RETURN HOME", bigButton)) GoHome();
        }

        private void CleanupBattle()
        {
            if (battle != null) Destroy(battle.gameObject);
            battle = null;
        }

        private void GoHome()
        {
            if (page == Page.Formation && progress.Data.selectedTeam.Count != 5)
            {
                progress.Data.selectedTeam.Clear();
                foreach (var id in WorldsCatalog.StartingHeroes)
                {
                    if (progress.Data.selectedTeam.Count >= 5) break;
                    if (progress.OwnsHero(id)) progress.Data.selectedTeam.Add(id);
                }
                progress.Save();
            }
            CleanupBattle();
            notice = "";
            page = Page.Home;
        }
    }
}
