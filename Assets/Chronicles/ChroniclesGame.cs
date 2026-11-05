using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    // Integrated campaign navigation. Current IMGUI layout is a functional scaffold
    // to be replaced by production UI Toolkit art without changing progression logic.
    public sealed class ChroniclesGame : MonoBehaviour
    {
        private enum Page { Home, Chapters, Trials, Towers, StageStory, Heroes, HeroDetails, Formation, Skins, Gear, Forge, Training, Runes, SignatureWeapon, RelicVault, Summon, Quests, WeeklyQuests, Idle, Login, Settings, Battle, Results }
        private Page page = Page.Home;
        private PlayerProgressService progress;
        private ChroniclesAudioDirector audio;
        private ChroniclesBattle battle;
        private float draftMusic = .8f, draftEffects = .9f;
        private Camera menuCamera;
        private AudioListener menuAudio;
        private int stageId = 1;
        private TrialMode activeTrialMode = TrialMode.Normal;
        private bool inTower;
        private Origin selectedTowerOrigin = Origin.Egypt;
        private int selectedTowerFloor = 1;
        private string selectedHero = "horus";
        private bool showHeroSkills;
        private bool showHeroMotion = true;
        private int galleryTierFilter = -1;
        private int galleryElementFilter = -1;
        private float storyStartedAt;
        private string notice = "";
        private string lastSummoned = "";
        private SummonBannerType activeSummonBanner = SummonBannerType.Standard;
        private readonly List<string> lastSummonBatch = new List<string>();
        private bool lastVictory;
        private Vector2 chapterScroll, rosterScroll, formationScroll, gearScroll, runeScroll;
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
            GameObject cameraObject = new GameObject("Chronicles Menu Camera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.tag = "MainCamera";
            menuCamera = cameraObject.AddComponent<Camera>();
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = new Color(.06f, .045f, .11f);
            menuCamera.orthographic = true;
            menuCamera.orthographicSize = 6f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            menuAudio = cameraObject.AddComponent<AudioListener>();
            progress = new PlayerProgressService();
            audio = gameObject.AddComponent<ChroniclesAudioDirector>();
            draftMusic = progress.Data.musicVolume;
            draftEffects = progress.Data.effectsVolume;
            audio.SetVolumes(draftMusic, draftEffects);
            audio.SetState(AudioScene.Menu);
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
            ChroniclesHeroPortrait.Clear();
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
                case Page.Trials: Trials(); break;
                case Page.Towers: Towers(); break;
                case Page.StageStory: StageStory(); break;
                case Page.Heroes: Heroes(); break;
                case Page.HeroDetails: HeroDetails(); break;
                case Page.Formation: Formation(); break;
                case Page.Skins: Skins(); break;
                case Page.Gear: Gear(); break;
                case Page.Forge: Forge(); break;
                case Page.Training: Training(); break;
                case Page.Runes: Runes(); break;
                case Page.SignatureWeapon: SignatureWeapon(); break;
                case Page.RelicVault: RelicVault(); break;
                case Page.Summon: Summon(); break;
                case Page.Quests: Quests(false); break;
                case Page.WeeklyQuests: Quests(true); break;
                case Page.Idle: Idle(); break;
                case Page.Login: Login(); break;
                case Page.Settings: Settings(); break;
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
            if (GUI.Button(new Rect(40, 421, 221, 64), "CAMPAIGN", bigButton))
            { inTower = false; activeTrialMode = TrialMode.Normal; chapterScroll = Vector2.zero; page = Page.Chapters; }
            if (GUI.Button(new Rect(275, 421, 225, 64), "ELITE / DARK", bigButton))
            { inTower = false; chapterScroll = Vector2.zero; page = Page.Trials; }
            if (GUI.Button(new Rect(40, 497, 222, 62), "HEROES (" + progress.Data.heroes.Count + ")", bigButton)) page = Page.Heroes;
            if (GUI.Button(new Rect(274, 497, 226, 62), "FORMATION", bigButton)) page = Page.Formation;
            if (GUI.Button(new Rect(40, 570, 222, 62), "SUMMON", bigButton)) page = Page.Summon;
            if (GUI.Button(new Rect(274, 570, 226, 62), "DAILY QUESTS", bigButton)) page = Page.Quests;
            if (GUI.Button(new Rect(40, 645, 222, 64), "WEEKLY QUESTS", bigButton))
                page = Page.WeeklyQuests;
            if (GUI.Button(new Rect(274, 645, 226, 64), "AFK REWARDS", bigButton))
                page = Page.Idle;
            if (GUI.Button(new Rect(40, 722, 222, 56), "FACTION TOWERS", bigButton))
            { inTower = false; chapterScroll = Vector2.zero; page = Page.Towers; }
            if (GUI.Button(new Rect(274, 722, 226, 56),
                progress.CanClaimDailyLogin() ? "DAILY GIFT!" : "LOGIN REWARDS", bigButton))
                page = Page.Login;
            if (GUI.Button(new Rect(40, 796, 222, 49), "RELIC VAULT"))
                page = Page.RelicVault;
            if (GUI.Button(new Rect(274, 796, 226, 49), "SETTINGS"))
                page = Page.Settings;
            GUI.Label(new Rect(40, 854, 460, 36),
                "Six chapters • 41 heroes • One original world.", subtitle);
        }

        private void Trials()
        {
            GUI.Label(new Rect(32, 191, 475, 58), "ELITE & DARK TRIALS", heading);
            GUI.Label(new Rect(22, 242, 498, 67),
                "CAMPAIGN RANK: " + ChroniclesTrials.CampaignRank(progress.Data) +
                "\nClear Normal chapters to unlock Elite; defeat Elite bosses to unlock Dark.",
                subtitle);
            chapterScroll = GUI.BeginScrollView(
                new Rect(20, 310, 500, 580), chapterScroll,
                new Rect(0, 0, 475, WorldsCatalog.Chapters.Length * 192));
            for (int i = 0; i < WorldsCatalog.Chapters.Length; i++)
            {
                ChapterDefinition chapter = WorldsCatalog.Chapters[i];
                int elite = ChroniclesTrials.Cleared(progress.Data, TrialMode.Elite,
                    chapter.number);
                int dark = ChroniclesTrials.Cleared(progress.Data, TrialMode.Dark,
                    chapter.number);
                int eliteNext = ChroniclesTrials.NextAvailable(
                    progress.Data, TrialMode.Elite, chapter.number);
                int darkNext = ChroniclesTrials.NextAvailable(
                    progress.Data, TrialMode.Dark, chapter.number);
                float y = i * 192f;
                GUI.Box(new Rect(4, y, 462, 181), "");
                GUI.Label(new Rect(16, y + 9, 442, 47),
                    chapter.number + ". " + chapter.title, subtitle);
                GUI.Label(new Rect(16, y + 54, 442, 34),
                    "ELITE " + elite + "/10  |  DARK " + dark + "/10", tiny);
                GUI.enabled = eliteNext > 0;
                if (GUI.Button(new Rect(16, y + 104, 215, 59),
                    elite == 10 ? "ELITE COMPLETE" :
                    eliteNext > 0 ? "ELITE STAGE " + eliteNext :
                        "ELITE LOCKED"))
                    SelectTrial(eliteNext, TrialMode.Elite, chapter.number);
                GUI.enabled = darkNext > 0;
                if (GUI.Button(new Rect(241, y + 104, 209, 59),
                    dark == 10 ? "DARK COMPLETE" :
                    darkNext > 0 ? "DARK STAGE " + darkNext :
                        "DARK LOCKED"))
                    SelectTrial(darkNext, TrialMode.Dark, chapter.number);
                GUI.enabled = true;
            }
            GUI.EndScrollView();
        }

        private void SelectTrial(int id, TrialMode mode, int chapter)
        {
            if (id < 1 || !progress.CanPlayTrial(id, mode)) return;
            stageId = id;
            inTower = false;
            activeTrialMode = mode;
            storyStartedAt = Time.unscaledTime;
            page = Page.StageStory;
            notice = "";
            if (audio != null) audio.SetState(AudioScene.Story, chapter);
        }

        private void Towers()
        {
            GUI.Label(new Rect(28, 189, 488, 62), "FACTION TOWERS", heading);
            GUI.Label(new Rect(24, 248, 495, 55),
                "Seven mythological towers • 100 floors each\n" +
                "A team needs two heroes from the tower's own origin.", subtitle);
            chapterScroll = GUI.BeginScrollView(
                new Rect(19, 314, 503, 573), chapterScroll,
                new Rect(0, 0, 474, ChroniclesTowers.TowerOrigins.Length * 143));
            for (int i = 0; i < ChroniclesTowers.TowerOrigins.Length; i++)
            {
                Origin tower = ChroniclesTowers.TowerOrigins[i];
                int highest = ChroniclesTowers.Highest(progress.Data, tower);
                int next = highest + 1;
                float y = i * 143f;
                GUI.Box(new Rect(5, y, 465, 132), "");
                GUI.Label(new Rect(15, y + 6, 445, 37),
                    ChroniclesTowers.DisplayName(tower), subtitle);
                GUI.Label(new Rect(20, y + 46, 440, 34),
                    "Floor " + highest + "/100 • " + ChroniclesTowers.Rank(highest), tiny);
                GUI.enabled = progress.Data.lastClearedStage >= 10 &&
                    next <= ChroniclesTowers.MaxFloor;
                if (GUI.Button(new Rect(28, y + 82, 420, 41),
                    highest >= ChroniclesTowers.MaxFloor ? "TOWER COMPLETED" :
                    progress.Data.lastClearedStage < 10 ? "CLEAR STORY CHAPTER 1 TO UNLOCK" :
                    "CHALLENGE FLOOR " + next))
                    SelectTower(tower, next);
                GUI.enabled = true;
            }
            GUI.EndScrollView();
        }

        private void SelectTower(Origin origin, int floor)
        {
            StageDefinition stage = ChroniclesTowers.StageFor(origin, floor);
            if (stage == null || progress.Data.lastClearedStage < 10) return;
            selectedTowerOrigin = origin;
            selectedTowerFloor = floor;
            inTower = true;
            activeTrialMode = TrialMode.Normal;
            stageId = stage.id;
            notice = "Select five heroes, including two from " + origin + ".";
            page = Page.Formation;
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
                        activeTrialMode = TrialMode.Normal;
                        storyStartedAt = Time.unscaledTime;
                        page = Page.StageStory;
                        if (audio != null) audio.SetState(AudioScene.Story, chapter.number);
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
                activeTrialMode == TrialMode.Dark ? "DARK ENCOUNTER" :
                activeTrialMode == TrialMode.Elite ? "ELITE ENCOUNTER" :
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
                "Recommended power: " +
                Mathf.RoundToInt(stage.recommendedPower *
                    ChroniclesTrials.EnemyHealthMultiplier(activeTrialMode)) +
                "\nFirst clear: " + (activeTrialMode == TrialMode.Normal ?
                    stage.gold + " Gold, " + stage.gems + " Gems" :
                    ChroniclesTrials.FirstClearReward(stageId, activeTrialMode).Description),
                subtitle);
            if (GUI.Button(new Rect(42, 753, 458, 70), "PREPARE TEAM", bigButton))
            {
                notice = "";
                page = Page.Formation;
            }
            if (GUI.Button(new Rect(42, 831, 458, 51),
                activeTrialMode == TrialMode.Normal ?
                    "BACK TO CHAPTERS" : "BACK TO TRIALS"))
                page = activeTrialMode == TrialMode.Normal ?
                    Page.Chapters : Page.Trials;
        }

        private void Heroes()
        {
            GUI.Label(new Rect(110, 181, 320, 48), "HERO COLLECTION", heading);
            GUI.Label(new Rect(30, 226, 480, 36),
                progress.Data.heroes.Count + " / " + WorldsCatalog.Heroes.Length +
                " owned  •  STATIC PORTRAITS", subtitle);
            string[] tabs = { "ALL", "R", "SR", "SSR", "UR", "EX" };
            for (int i = 0; i < tabs.Length; i++)
            {
                float x = 22f + i * 83f;
                if (i == 5)
                {
                    GUI.DrawTexture(new Rect(x, 268, 77, 45), Texture2D.blackTexture);
                    GUI.Label(new Rect(x, 270, 77, 39), "EX LOCKED", tiny);
                    continue;
                }
                Color old = GUI.backgroundColor;
                if (galleryTierFilter == i - 1)
                    GUI.backgroundColor = new Color(.86f, .73f, .46f);
                if (GUI.Button(new Rect(x, 268, 77, 45), tabs[i]))
                {
                    galleryTierFilter = i - 1;
                    rosterScroll = Vector2.zero;
                }
                GUI.backgroundColor = old;
            }

            string[] elementTabs = { "ALL", "FIRE", "WATER", "NATURE", "WIND", "LIGHT", "DARK" };
            for (int i = 0; i < elementTabs.Length; i++)
            {
                Color beforeTab = GUI.backgroundColor;
                if (galleryElementFilter == i - 1)
                    GUI.backgroundColor = new Color(.78f, .82f, .91f);
                if (GUI.Button(new Rect(22f + i * 71f, 319f, 67f, 40f), elementTabs[i]))
                {
                    galleryElementFilter = i - 1;
                    rosterScroll = Vector2.zero;
                }
                GUI.backgroundColor = beforeTab;
            }

            var visible = new List<HeroDefinition>();
            foreach (HeroDefinition hero in WorldsCatalog.Heroes)
                if ((galleryTierFilter < 0 || (int)hero.rarity == galleryTierFilter) &&
                    (galleryElementFilter < 0 ||
                     (int)hero.element == galleryElementFilter))
                    visible.Add(hero);
            visible.Sort((first, second) =>
            {
                int byRarity = second.rarity.CompareTo(first.rarity);
                return byRarity != 0 ? byRarity :
                    string.CompareOrdinal(first.name, second.name);
            });

            int rows = Mathf.CeilToInt(visible.Count / 3f);
            rosterScroll = GUI.BeginScrollView(
                new Rect(17, 365, 509, 510), rosterScroll,
                new Rect(0, 0, 477, Mathf.Max(510, rows * 222 + 12)));
            for (int i = 0; i < visible.Count; i++)
            {
                HeroDefinition hero = visible[i];
                int column = i % 3, row = i / 3;
                float x = 4f + column * 157f;
                float y = 4f + row * 222f;
                // Generate only thumbnails inside or near the viewport.
                if (y + 218f < rosterScroll.y - 30f ||
                    y > rosterScroll.y + 550f) continue;
                GUI.Box(new Rect(x, y, 150, 212), "");
                ChroniclesHeroPortrait.Draw(hero.id,
                    new Rect(x + 4, y + 4, 142, 164), false);
                Color oldColor = GUI.color;
                GUI.color = new Color(.035f, .032f, .074f, .96f);
                GUI.DrawTexture(new Rect(x + 4, y + 156, 142, 53),
                    Texture2D.whiteTexture);
                GUI.color = oldColor;
                GUI.Label(new Rect(x + 7, y + 162, 136, 22),
                    hero.name + "  " + hero.rarity, tiny);
                GUI.Label(new Rect(x + 7, y + 185, 137, 24),
                    hero.element.ToString() +
                    (progress.OwnsHero(hero.id) ? " • OWNED" : " • LOCKED"), tiny);
                if (GUI.Button(new Rect(x, y, 150, 212), "", GUIStyle.none))
                {
                    selectedHero = hero.id;
                    showHeroMotion = true;
                    notice = "";
                    page = Page.HeroDetails;
                }
            }
            GUI.EndScrollView();
            if (visible.Count == 0)
                GUI.Label(new Rect(40, 535, 456, 80),
                    "No heroes match the selected filters.", subtitle);
        }

        private void HeroDetails()
        {
            HeroDefinition hero = WorldsCatalog.FindHero(selectedHero);
            if (hero == null) { page = Page.Heroes; return; }
            OwnedHero owned = progress.FindOwnedHero(hero.id);
            HeroKit kit = HeroAbilityCatalog.For(hero.id);
            URCardIdentity premium = hero.rarity == Rarity.UR ?
                URCardCatalog.For(hero.id) : null;
            GUI.Label(new Rect(28, 235, 485, 54),
                hero.name.ToUpperInvariant(), heading);
            GUI.Label(new Rect(26, 292, 487, 39),
                hero.rarity + " • " + hero.element + " • " +
                hero.origin + " • " + hero.role, subtitle);

            // Both static and animated portraits are available for EVERY hero.
            ChroniclesHeroPortrait.Draw(hero.id,
                new Rect(30, 350, 228, 280),
                showHeroMotion && !progress.Data.reducePortraitMotion);
            GUI.enabled = !progress.Data.reducePortraitMotion;
            if (GUI.Button(new Rect(30, 636, 109, 37),
                showHeroMotion ? "● MOTION" : "MOTION"))
                showHeroMotion = true;
            GUI.enabled = true;
            if (GUI.Button(new Rect(146, 636, 112, 37),
                !showHeroMotion ? "● STATIC" : "STATIC"))
                showHeroMotion = false;

            GUI.Box(new Rect(268, 350, 243, 323), "");
            if (GUI.Button(new Rect(276, 360, 109, 36), "STORY"))
                showHeroSkills = false;
            if (GUI.Button(new Rect(392, 360, 110, 36), "SKILLS"))
                showHeroSkills = true;
            if (showHeroSkills && kit != null)
            {
                GUI.Label(new Rect(280, 407, 221, 260),
                    "SKILL: " + kit.skillName + "\n" +
                    kit.skillDescription + "\n\nULTIMATE: " +
                    kit.ultimateName + "\n" + kit.ultimateDescription +
                    "\n\nPASSIVE: " + kit.passiveName + "\n" +
                    kit.passiveDescription, tiny);
            }
            else
            {
                PortraitIdentity illustration = HeroPortraitIdentityCatalog.For(hero.id);
                string text = premium != null ?
                    premium.title + "\n" + premium.scene +
                    "\n\nENTRANCE: " + premium.introCue :
                    hero.story + "\n\nLOOK: " +
                    (illustration == null ? "" : illustration.visualNote);
                GUI.Label(new Rect(279, 407, 221, 253), text, tiny);
            }

            if (owned == null)
            {
                GUI.Label(new Rect(30, 680, 478, 61),
                    hero.rarity == Rarity.UR ?
                        "UR limited acquisition is coming later." :
                        "Not owned. Find this hero via summoning.", subtitle);
            }
            else
            {
                GUI.Label(new Rect(27, 674, 488, 53),
                    "LV " + owned.level + " • STARS " + owned.stars +
                    " • GEAR +" + owned.weaponLevel + "/+" + owned.armorLevel +
                    " • FRAGMENTS " + owned.fragments, subtitle);
            }
            GUI.enabled = owned != null;
            if (GUI.Button(new Rect(31, 735, 478, 49), "UPGRADE HERO", bigButton))
                notice = progress.UpgradeHero(hero.id) ?
                    "Hero upgraded and saved." : "Gold or level cap reached.";
            if (GUI.Button(new Rect(31, 789, 232, 49), "EQUIPMENT"))
                page = Page.Gear;
            if (GUI.Button(new Rect(275, 789, 234, 49), "SKILL TRAINING"))
                page = Page.Training;
            if (GUI.Button(new Rect(31, 843, 232, 46), "ASCEND"))
                notice = progress.AscendHero(hero.id) ?
                    "Star rank increased!" : "Not enough fragments.";
            GUI.enabled = true;
            if (GUI.Button(new Rect(275, 843, 234, 46), "SKINS"))
                page = Page.Skins;
            if (GUI.Button(new Rect(31, 897, 478, 39), "BACK TO COLLECTION"))
                page = Page.Heroes;
        }

        private void Skins()
        {
            HeroDefinition hero = WorldsCatalog.FindHero(selectedHero);
            if (hero == null) { page = Page.Heroes; return; }
            OwnedHero owned = progress.FindOwnedHero(hero.id);
            GUI.Label(new Rect(30, 245, 480, 72), hero.name.ToUpperInvariant() + " SKINS", heading);
            GUI.Label(new Rect(30, 315, 480, 53),
                "Costumes must be unlocked before equipping. Real artwork is loaded from Unity Resources.", subtitle);
            IList<HeroSkin> skins = HeroSkinCatalog.For(hero.id);
            for (int i = 0; i < skins.Count; i++)
            {
                HeroSkin skin = skins[i];
                bool unlocked = progress.OwnsSkin(hero.id, skin.id);
                bool equipped = owned != null && owned.skin == skin.id;
                float y = 391 + i * 205;
                GUI.Box(new Rect(30, y, 480, 194), "");
                GUI.Label(new Rect(41, y + 12, 458, 48), skin.displayName, heading);
                GUI.Label(new Rect(42, y + 62, 456, 52), skin.visualTheme, tiny);
                GUI.Label(new Rect(42, y + 111, 455, 27),
                    equipped ? "EQUIPPED" : unlocked ? "OWNED" :
                        skin.gemCost + " earned gems", subtitle);
                GUI.enabled = owned != null && !equipped &&
                    (unlocked || (skin.gemCost > 0 && progress.Data.gems >= skin.gemCost));
                if (GUI.Button(new Rect(43, y + 147, 450, 38),
                    equipped ? "EQUIPPED" : unlocked ? "EQUIP SKIN" : "UNLOCK SKIN"))
                {
                    bool success = unlocked ? progress.EquipSkin(hero.id, skin.id) :
                        progress.UnlockSkin(hero.id, skin.id);
                    notice = success ? (unlocked ? "Skin equipped." : "Skin unlocked. Tap EQUIP to wear it.") :
                        "Skin action unavailable.";
                }
                GUI.enabled = true;
            }
            if (GUI.Button(new Rect(40, 817, 460, 58), "BACK TO HERO", bigButton))
                page = Page.HeroDetails;
        }

        private void Gear()
        {
            HeroDefinition hero = WorldsCatalog.FindHero(selectedHero);
            if (hero == null) { page = Page.Heroes; return; }
            EquipmentBonuses bonus = progress.GearBonus(hero.id);
            GUI.Label(new Rect(30, 236, 480, 55), hero.name.ToUpperInvariant() + " EQUIPMENT", heading);
            GUI.Label(new Rect(30, 283, 480, 87),
                "SLOTS " + bonus.equippedCount + "/6  •  DUST " + progress.Data.forgeDust +
                "\nATK +" + Mathf.RoundToInt(bonus.attack) +
                " HP +" + Mathf.RoundToInt(bonus.health) +
                " DEF +" + Mathf.RoundToInt(bonus.defense) +
                "\nSET: " + bonus.summary, subtitle);
            if (progress.Data.equipment.Count == 0)
                GUI.Label(new Rect(36, 390, 465, 110),
                    "Win stages to earn equipment, or craft gear from forge materials.", subtitle);
            gearScroll = GUI.BeginScrollView(new Rect(18, 380, 504, 426), gearScroll,
                new Rect(0, 0, 478, Mathf.Max(430, progress.Data.equipment.Count * 111)));
            for (int i = 0; i < progress.Data.equipment.Count; i++)
            {
                GearInstance item = progress.Data.equipment[i];
                GearTemplate gear = ChroniclesEquipment.Find(item.templateId);
                if (gear == null) continue;
                float y = i * 111f;
                GUI.Box(new Rect(4, y, 469, 104), "");
                string wearing = item.equippedHeroId == hero.id ? "EQUIPPED" :
                    string.IsNullOrEmpty(item.equippedHeroId) ? "IN BAG" : "ON " + item.equippedHeroId;
                GUI.Label(new Rect(11, y + 5, 456, 34),
                    gear.name + " +" + item.level + " [" + gear.rarity + "]", tiny);
                GUI.Label(new Rect(11, y + 35, 451, 24), wearing, tiny);
                if (item.equippedHeroId == hero.id)
                {
                    if (GUI.Button(new Rect(13, y + 64, 141, 34), "UNEQUIP"))
                        notice = progress.UnequipGear(item.instanceId) ? "Unequipped." : "Failed.";
                }
                else
                {
                    if (GUI.Button(new Rect(13, y + 64, 141, 34), "EQUIP"))
                        notice = progress.EquipGear(item.instanceId, hero.id) ? "Equipped." : "Failed.";
                }
                GUI.enabled = item.level < 30 && progress.Data.gold >= ChroniclesEquipment.UpgradeCost(item);
                if (GUI.Button(new Rect(162, y + 64, 146, 34), "LEVEL UP"))
                    notice = progress.UpgradeGear(item.instanceId) ? "Gear leveled." : "Failed.";
                GUI.enabled = string.IsNullOrEmpty(item.equippedHeroId);
                if (GUI.Button(new Rect(316, y + 64, 142, 34), "DISMANTLE"))
                    notice = progress.DismantleGear(item.instanceId) ? "Materials received." : "Failed.";
                GUI.enabled = true;
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(37, 816, 223, 47), "AUTO EQUIP"))
                notice = progress.AutoEquip(hero.id) ? "Best available items equipped." : "Auto equip failed.";
            if (GUI.Button(new Rect(270, 816, 229, 47), "FORGE"))
                { page = Page.Forge; notice = ""; }
            if (GUI.Button(new Rect(40, 871, 460, 42), "BACK TO HERO"))
                page = Page.HeroDetails;
        }

        private void Forge()
        {
            GUI.Label(new Rect(25, 238, 490, 57), "FORGE WORKSHOP", heading);
            GUI.Label(new Rect(35, 291, 470, 55),
                "Dust: " + progress.Data.forgeDust +
                " • Dismantle unwanted unequipped gear to make new equipment.", subtitle);
            gearScroll = GUI.BeginScrollView(new Rect(17, 359, 505, 474), gearScroll,
                new Rect(0, 0, 479, ChroniclesEquipment.Templates.Length * 77));
            for (int i = 0; i < ChroniclesEquipment.Templates.Length; i++)
            {
                GearTemplate item = ChroniclesEquipment.Templates[i];
                float y = i * 77f;
                int dust = 20 + (int)item.rarity * 35;
                int gold = 200 + (int)item.rarity * 300;
                GUI.Box(new Rect(3, y, 469, 71), "");
                GUI.Label(new Rect(11, y + 5, 300, 62),
                    item.name + " [" + item.rarity + "]\n" + dust + " dust + " + gold + " gold", tiny);
                GUI.enabled = progress.Data.forgeDust >= dust && progress.Data.gold >= gold;
                if (GUI.Button(new Rect(324, y + 13, 136, 45), "CRAFT"))
                    notice = progress.CraftGear(item.id) ? "New gear forged." : "Crafting unavailable.";
                GUI.enabled = true;
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(40, 843, 460, 55), "BACK TO EQUIPMENT", bigButton))
                page = Page.Gear;
        }

        private void Training()
        {
            OwnedHero hero = progress.FindOwnedHero(selectedHero);
            if (hero == null) { page = Page.Heroes; return; }
            GUI.Label(new Rect(24, 244, 490, 60), "HERO TRAINING", heading);
            GUI.Label(new Rect(22, 303, 495, 56),
                "Resonance LV " + progress.ResonanceLevel() +
                " • Effective LV " + ChroniclesGrowth.EffectiveLevel(progress.Data, hero), subtitle);
            string[] skillTypes = { "active", "ultimate", "passive" };
            int[] levels = { hero.activeSkillLevel, hero.ultimateSkillLevel, hero.passiveSkillLevel };
            for (int i = 0; i < skillTypes.Length; i++)
            {
                float y = 367 + i * 89;
                GUI.Box(new Rect(30, y, 480, 78), "");
                GUI.Label(new Rect(45, y + 10, 248, 55),
                    skillTypes[i].ToUpperInvariant() + " Lv." + levels[i] +
                    "\nNext: " + ChroniclesGrowth.SkillCost(levels[i]) + " Gold", tiny);
                GUI.enabled = levels[i] < ChroniclesGrowth.SkillCap && levels[i] < hero.stars + 4;
                if (GUI.Button(new Rect(310, y + 15, 179, 48), "LEVEL UP"))
                    notice = progress.UpgradeSkill(hero.id, skillTypes[i]) ? "Skill upgraded." : "Insufficient gold.";
                GUI.enabled = true;
            }
            GUI.Label(new Rect(35, 650, 465, 35),
                "Talent Paths: Attack " + hero.attackTalents + " • Defense " +
                hero.defenseTalents + " / " + ChroniclesGrowth.MaxTalents, tiny);
            if (GUI.Button(new Rect(40, 691, 221, 49), "+ ATTACK TALENT"))
                notice = progress.BuyTalent(hero.id, true) ? "Attack talent purchased." : "Talent locked or low gold.";
            if (GUI.Button(new Rect(275, 691, 224, 49), "+ DEFENSE TALENT"))
                notice = progress.BuyTalent(hero.id, false) ? "Defense talent purchased." : "Talent locked or low gold.";
            if (GUI.Button(new Rect(40, 756, 220, 49), "RESET TALENTS"))
                notice = progress.ResetTalents(hero.id) ? "Talent costs refunded." : "No talents to reset.";
            if (GUI.Button(new Rect(274, 756, 225, 49), "RESET LEVEL"))
                notice = progress.ResetHeroLevel(hero.id) ? "Level resources refunded." : "Already at Lv.1.";
            if (GUI.Button(new Rect(34, 825, 147, 59), "RUNES"))
                page = Page.Runes;
            if (GUI.Button(new Rect(193, 825, 148, 59), "SIGNATURE"))
                page = Page.SignatureWeapon;
            if (GUI.Button(new Rect(350, 825, 158, 59), "BACK TO HERO"))
                page = Page.HeroDetails;
        }

        private void SignatureWeapon()
        {
            OwnedHero hero = progress.FindOwnedHero(selectedHero);
            HeroDefinition definition = WorldsCatalog.FindHero(selectedHero);
            if (hero == null || definition == null) { page = Page.Heroes; return; }
            GUI.Label(new Rect(30, 245, 480, 68), "SIGNATURE WEAPON", heading);
            GUI.Label(new Rect(35, 331, 470, 74),
                definition.name + " • Ancient Soulbound Weapon", subtitle);
            GUI.Box(new Rect(45, 423, 450, 192), "");
            GUI.Label(new Rect(62, 451, 421, 147),
                "Rank: " + hero.signatureWeaponRank + " / 5" +
                "\nUnlock at hero level 10." +
                "\nEach rank improves ATK, skill power and Ultimate." +
                "\nEnhancement requires hero fragments and gold.", subtitle);
            GUI.enabled = hero.level >= 10 && hero.signatureWeaponRank < 5;
            if (GUI.Button(new Rect(45, 675, 450, 75), "ENHANCE SIGNATURE", bigButton))
                notice = progress.UpgradeSignatureWeapon(hero.id) ?
                    "Signature weapon upgraded!" : "Need more gold or fragments.";
            GUI.enabled = true;
            if (GUI.Button(new Rect(45, 800, 450, 65), "BACK TO TRAINING", bigButton))
                page = Page.Training;
        }

        private void RelicVault()
        {
            GUI.Label(new Rect(30, 244, 480, 58), "RELIC VAULT", heading);
            GUI.Label(new Rect(35, 305, 471, 59),
                "Earn an original artifact by defeating each chapter guardian." +
                "\nRelic dust: " + progress.Data.relicDust, subtitle);
            gearScroll = GUI.BeginScrollView(new Rect(18, 376, 502, 448), gearScroll,
                new Rect(0, 0, 477, ChroniclesRelics.All.Length * 102));
            for (int i = 0; i < ChroniclesRelics.All.Length; i++)
            {
                WorldRelic relic = ChroniclesRelics.All[i];
                RelicRank owned = progress.Data.relics.Find(x => x.id == relic.id);
                bool equipped = progress.Data.equippedRelicId == relic.id;
                float y = 102f * i;
                GUI.Box(new Rect(5, y, 468, 97), "");
                GUI.Label(new Rect(16, y + 5, 450, 34),
                    relic.name + "  •  " +
                    (owned == null ? "LOCKED" : "LV " + owned.level), tiny);
                GUI.Label(new Rect(13, y + 33, 448, 25), relic.description, tiny);
                GUI.enabled = owned != null && !equipped;
                if (GUI.Button(new Rect(19, y + 63, 199, 28), equipped ? "EQUIPPED" : "EQUIP"))
                    notice = progress.EquipRelic(relic.id) ? "Relic equipped." : "Equip unavailable.";
                GUI.enabled = owned != null && owned.level < 5;
                if (GUI.Button(new Rect(243, y + 63, 218, 28), "UPGRADE"))
                    notice = progress.UpgradeRelic(relic.id) ? "Relic upgraded." :
                        "Need more relic dust or gold.";
                GUI.enabled = true;
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(39, 838, 460, 59), "RETURN HOME", bigButton))
                GoHome();
        }

        private void Runes()
        {
            HeroDefinition hero = WorldsCatalog.FindHero(selectedHero);
            if (hero == null) { page = Page.Heroes; return; }
            GUI.Label(new Rect(30, 244, 480, 59), hero.name.ToUpperInvariant() + " RUNES", heading);
            GUI.Label(new Rect(35, 304, 465, 66),
                "One rune of each type per hero. Earn a rune every five first-clear campaign stages.", subtitle);
            runeScroll = GUI.BeginScrollView(new Rect(18, 376, 504, 442), runeScroll,
                new Rect(0, 0, 478, Mathf.Max(445, progress.Data.runes.Count * 101)));
            for (int i = 0; i < progress.Data.runes.Count; i++)
            {
                RuneInstance rune = progress.Data.runes[i];
                float y = i * 101f;
                GUI.Box(new Rect(5, y, 469, 95), "");
                GUI.Label(new Rect(14, y + 7, 450, 35),
                    rune.type + " RUNE — LV " + rune.level, tiny);
                GUI.Label(new Rect(14, y + 36, 180, 45),
                    rune.assignedHeroId == hero.id ? "EQUIPPED" :
                    string.IsNullOrEmpty(rune.assignedHeroId) ? "AVAILABLE" : rune.assignedHeroId, tiny);
                if (rune.assignedHeroId == hero.id)
                {
                    if (GUI.Button(new Rect(199, y + 43, 126, 40), "REMOVE"))
                        notice = progress.UnequipRune(rune.instanceId) ? "Rune removed." : "Failed.";
                }
                else if (GUI.Button(new Rect(199, y + 43, 126, 40), "EQUIP"))
                    notice = progress.EquipRune(rune.instanceId, hero.id) ? "Rune equipped." : "Failed.";
                GUI.enabled = rune.level < 20 &&
                    progress.Data.gold >= ChroniclesGrowth.RuneUpgradeCost(rune.level);
                if (GUI.Button(new Rect(334, y + 43, 129, 40), "UPGRADE"))
                    notice = progress.UpgradeRune(rune.instanceId) ? "Rune upgraded." : "Failed.";
                GUI.enabled = true;
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(41, 835, 458, 59), "BACK TO TRAINING", bigButton))
                page = Page.Training;
        }

        private void Formation()
        {
            GUI.Label(new Rect(125, 182, 360, 50), "TEAM FORMATION", heading);
            GUI.Label(new Rect(35, 235, 466, 66),
                "Select five owned heroes. Order is used to place your team in front and back rows.", subtitle);
            formationScroll = GUI.BeginScrollView(new Rect(20, 317, 500, 413), formationScroll,
                new Rect(0, 0, 473, progress.Data.heroes.Count * 66));
            for (int i = 0; i < progress.Data.heroes.Count; i++)
            {
                OwnedHero owned = progress.Data.heroes[i];
                HeroDefinition hero = WorldsCatalog.FindHero(owned.id);
                if (hero == null) continue;
                int picked = progress.Data.selectedTeam.IndexOf(owned.id);
                string label = (picked >= 0 ? "[SLOT " + (picked + 1) + "] " : "[ + ] ") +
                    hero.name + "   Level " + owned.level + "   " + hero.role;
                float y = i * 66f;
                ChroniclesHeroPortrait.Draw(hero.id, new Rect(5, y, 48, 61), false);
                GUI.Box(new Rect(54, y, 404, 61), "");
                GUI.Label(new Rect(64, y + 7, 377, 46), label, tiny);
                if (GUI.Button(new Rect(5, y, 457, 61), "", GUIStyle.none))
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
            ElementalTeamBonus elemental = HeroElements.Calculate(synergyHeroes);
            GUI.Label(new Rect(30, 728, 480, 76),
                "Selected " + progress.Data.selectedTeam.Count + "/5  |  " +
                elemental.description + "\nOrigin: " + bonus.description +
                "\nElement ATK/HP +" +
                Mathf.RoundToInt((elemental.attackMultiplier - 1f) * 100f) +
                "%  •  START ENERGY +" + elemental.startingEnergy, subtitle);
            GUI.enabled = progress.Data.selectedTeam.Count == 5;
            if (GUI.Button(new Rect(40, 803, 460, 70),
                "START " + (activeTrialMode == TrialMode.Normal ?
                    "" : activeTrialMode.ToString().ToUpperInvariant() + " ") +
                "STAGE " + stageId, bigButton))
                BeginBattle();
            GUI.enabled = true;
        }

        private void Summon()
        {
            GUI.Label(new Rect(25, 223, 490, 55), "SUMMONING CHAMBER", heading);
            if (GUI.Button(new Rect(28, 282, 230, 52), "STANDARD"))
                SelectBanner(SummonBannerType.Standard);
            if (GUI.Button(new Rect(274, 282, 236, 52), "LIGHT / DARK UR"))
                SelectBanner(SummonBannerType.AstralCovenant);
            bool astral = activeSummonBanner == SummonBannerType.AstralCovenant;
            GUI.Label(new Rect(22, 337, 494, 75),
                ChroniclesSummonRules.DisplayName(activeSummonBanner) + "\n" +
                ChroniclesSummonRules.Rates(activeSummonBanner) + "\n" +
                (astral ?
                    "UR PITY " + progress.Data.astralPity + "/" +
                    ChroniclesSummonRules.AstralUrPity + "  •  ASTRAL SEALS " +
                    progress.Data.astralSeals :
                    "SSR PITY " + progress.Data.standardPity + "/" +
                    ChroniclesSummonRules.StandardSsrPity + "  •  TICKETS " +
                    progress.Data.summonTickets), subtitle);

            if (lastSummonBatch.Count == 1)
            {
                string id = lastSummonBatch[0];
                HeroDefinition hero = WorldsCatalog.FindHero(id);
                if (hero != null)
                {
                    ChroniclesHeroPortrait.Draw(id,
                        new Rect(165, 424, 210, 258),
                        !progress.Data.reducePortraitMotion);
                    GUI.Label(new Rect(30, 680, 480, 38),
                        hero.name.ToUpperInvariant() + "  •  " + hero.rarity +
                        "  •  " + hero.element, subtitle);
                }
            }
            else if (lastSummonBatch.Count > 1)
            {
                GUI.Box(new Rect(36, 415, 468, 278), "");
                for (int i = 0; i < lastSummonBatch.Count; i++)
                {
                    HeroDefinition hero = WorldsCatalog.FindHero(lastSummonBatch[i]);
                    if (hero == null) continue;
                    int column = i % 5, row = i / 5;
                    float x = 45 + column * 92, y = 424 + row * 133;
                    ChroniclesHeroPortrait.Draw(hero.id,
                        new Rect(x, y, 83, 98), false);
                    GUI.Label(new Rect(x, y + 99, 89, 34),
                        hero.name + "\n" + hero.rarity, tiny);
                }
            }
            else
            {
                GUI.Box(new Rect(68, 427, 404, 263),
                    astral ? "THE ASTRAL COVENANT\n16 UR heroes: 9 Light / 7 Dark\n" +
                        "Each UR has an equal share of the 1.5% rate." :
                        "STANDARD SUMMON\nCollect heroes from R, SR and SSR." );
            }
            int cost = ChroniclesSummonRules.GemCost(activeSummonBanner);
            string type = astral ? "ASTRAL SEAL" : "TICKET";
            if (GUI.Button(new Rect(34, 714, 230, 58),
                "SUMMON ×1\n1 " + type + " / " + cost + " GEMS"))
                DoSummon(1);
            if (GUI.Button(new Rect(274, 714, 229, 58),
                "SUMMON ×10\n10 " + type + " / " + (cost * 10) + " GEMS"))
                DoSummon(10);

            GUI.Box(new Rect(30, 784, 482, 107), "");
            GUI.Label(new Rect(36, 788, 462, 30), "RECENT SUMMON HISTORY", subtitle);
            int first = Math.Max(0, progress.Data.summonHistory.Count - 3);
            for (int i = first; i < progress.Data.summonHistory.Count; i++)
            {
                SummonHistoryRecord record = progress.Data.summonHistory[i];
                HeroDefinition hero = WorldsCatalog.FindHero(record.heroId);
                if (hero == null) continue;
                int index = i - first;
                GUI.Label(new Rect(48, 817 + index * 22, 451, 24),
                    hero.name + "  •  " + hero.rarity + "  •  " +
                    record.banner + (record.guaranteed ? "  [PITY]" : ""), tiny);
            }
        }

        private void SelectBanner(SummonBannerType banner)
        {
            activeSummonBanner = banner;
            lastSummonBatch.Clear();
            notice = "";
        }

        private void DoSummon(int count)
        {
            SummonBatchResult result = progress.DrawHeroes(activeSummonBanner, count);
            lastSummonBatch.Clear();
            if (!result.success)
            {
                lastSummoned = "";
                notice = result.error;
                return;
            }
            lastSummonBatch.AddRange(result.heroIds);
            lastSummoned = result.heroIds.Count > 0 ?
                result.heroIds[result.heroIds.Count - 1] : "";
            notice = result.heroIds.Count + (result.heroIds.Count == 1 ?
                " hero summoned." : " heroes summoned.");
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

        private void Login()
        {
            GUI.Label(new Rect(25, 228, 490, 62), "28-DAY LOGIN REWARDS", heading);
            GUI.Label(new Rect(25, 283, 490, 56),
                "Claim one reward per UTC day. Missing a day never resets your progress.",
                subtitle);
            int next = progress.NextLoginRewardDay();
            for (int i = 0; i < ChroniclesLoginRewards.Days; i++)
            {
                LoginReward gift = ChroniclesLoginRewards.ForDay(i + 1);
                int column = i % 7, row = i / 7;
                float x = 22 + column * 71f, y = 357 + row * 97f;
                bool received = i + 1 <= progress.Data.loginDaysClaimed;
                Color previous = GUI.backgroundColor;
                if (i + 1 == next && progress.CanClaimDailyLogin())
                    GUI.backgroundColor = new Color(.92f, .77f, .44f);
                else if (received)
                    GUI.backgroundColor = new Color(.49f, .61f, .60f);
                GUI.Box(new Rect(x, y, 67, 89), "");
                GUI.Label(new Rect(x + 2, y + 7, 62, 26),
                    "DAY " + (i + 1), tiny);
                GUI.Label(new Rect(x + 4, y + 29, 62, 56),
                    received ? "CLAIMED" : gift.Label, tiny);
                GUI.backgroundColor = previous;
            }

            LoginReward current = ChroniclesLoginRewards.ForDay(next);
            GUI.Label(new Rect(30, 754, 480, 55),
                "NEXT: DAY " + next + " • " + current.Label, subtitle);
            GUI.enabled = progress.CanClaimDailyLogin();
            if (GUI.Button(new Rect(39, 830, 462, 62),
                progress.CanClaimDailyLogin() ? "CLAIM TODAY'S REWARD" :
                    "ALREADY CLAIMED TODAY", bigButton))
            {
                LoginReward claimed = progress.ClaimDailyLogin();
                notice = claimed == null ? "Could not save login reward." :
                    "Claimed Day " + claimed.day + ": " + claimed.Label;
            }
            GUI.enabled = true;
        }

        private void Settings()
        {
            GUI.Label(new Rect(25, 254, 490, 67), "GAME SETTINGS", heading);
            GUI.Box(new Rect(35, 350, 470, 428), "");
            GUI.Label(new Rect(53, 375, 440, 48),
                "MUSIC: " + Mathf.RoundToInt(draftMusic * 100f) + "%", subtitle);
            draftMusic = GUI.HorizontalSlider(new Rect(70, 436, 398, 25),
                draftMusic, 0f, 1f);
            GUI.Label(new Rect(53, 508, 440, 47),
                "SOUND EFFECTS: " + Mathf.RoundToInt(draftEffects * 100f) + "%", subtitle);
            draftEffects = GUI.HorizontalSlider(new Rect(70, 569, 398, 26),
                draftEffects, 0f, 1f);
            bool oldReduce = progress.Data.reducePortraitMotion;
            bool newReduce = GUI.Toggle(new Rect(66, 616, 413, 48),
                oldReduce, "REDUCE PORTRAIT MOTION");
            if (newReduce != oldReduce)
                notice = progress.SetReducedPortraitMotion(newReduce) ?
                    "Motion preference saved." : "Could not save motion preference.";
            if (GUI.Button(new Rect(58, 682, 425, 64), "SAVE AUDIO SETTINGS", bigButton))
            {
                bool saved = progress.SetAudioLevels(draftMusic, draftEffects);
                if (audio != null) audio.SetVolumes(draftMusic, draftEffects);
                notice = saved ? "Audio preferences saved." : "Could not save settings.";
            }
            GUI.Label(new Rect(37, 794, 466, 48),
                "Portrait animations and sounds use original character assets when installed.", subtitle);
            if (GUI.Button(new Rect(40, 845, 460, 54), "RETURN HOME", bigButton))
                GoHome();
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
            bool unlocked = stage != null &&
                (activeTrialMode == TrialMode.Normal ?
                    stageId <= progress.Data.lastClearedStage + 1 :
                    progress.CanPlayTrial(stageId, activeTrialMode));
            if (!unlocked)
            { notice = "Stage is locked."; return; }
            if (battle != null) Destroy(battle.gameObject);
            battle = new GameObject("Chronicles - Campaign Battle").AddComponent<ChroniclesBattle>();
            battle.Initialize(stage, chosen, progress.Data, OnBattleFinished, activeTrialMode);
            if (menuCamera != null) menuCamera.enabled = false;
            if (menuAudio != null) menuAudio.enabled = false;
            page = Page.Battle; notice = "";
            if (audio != null) audio.SetState(AudioScene.Battle, stage.chapter);
        }

        private void OnBattleFinished(bool victory)
        {
            lastVictory = victory;
            if (victory)
            {
                bool saved = activeTrialMode == TrialMode.Normal ?
                    progress.CompleteStage(stageId) :
                    progress.CompleteTrial(stageId, activeTrialMode);
                if (!saved) notice = "Battle won, but progress could not be saved.";
            }
            if (audio != null) audio.SetState(victory ? AudioScene.Victory : AudioScene.Defeat);
            page = Page.Results;
        }

        private void BattleHud(float scale)
        {
            if (battle == null) return;
            battle.DrawWorldLabels(scale);
            GUI.Box(new Rect(12, 22, 516, 106), "");
            GUI.Label(new Rect(20, 29, 500, 45),
                activeTrialMode.ToString().ToUpperInvariant() + " • " +
                WorldsCatalog.GetStage(stageId).name, subtitle);
            GUI.Label(new Rect(29, 73, 460, 36), "TIME " + Mathf.CeilToInt(battle.SecondsLeft) + " s", subtitle);
            if (GUI.Button(new Rect(20, 837, 153, 65), battle.Paused ? "RESUME" : "PAUSE"))
                battle.Paused = !battle.Paused;
            if (GUI.Button(new Rect(184, 837, 159, 65), "SPEED x" + battle.Speed))
                battle.Speed = battle.Speed >= 2f ? 1f : 2f;
            if (GUI.Button(new Rect(354, 837, 160, 65), "RETREAT"))
            {
                CleanupBattle();
                page = Page.Formation;
            }
        }

        private void Results()
        {
            GUI.DrawTexture(new Rect(0, 0, 540, 960), panelTex);
            GUI.Label(new Rect(25, 248, 490, 95), lastVictory ? "VICTORY!" : "DEFEAT", heading);
            StageDefinition stage = WorldsCatalog.GetStage(stageId);
            GUI.Label(new Rect(35, 362, 470, 100),
                activeTrialMode + " • " + stage.name +
                "\nCampaign rank: " + ChroniclesTrials.CampaignRank(progress.Data), subtitle);
            GUI.Box(new Rect(38, 480, 464, 142),
                lastVictory ? "First-clear rewards are added once. Your progress is saved." :
                    "Upgrade your heroes or change formation to try again.");
            bool finalTrialStage = activeTrialMode != TrialMode.Normal &&
                stageId % ChroniclesTrials.StagesPerChapter == 0;
            if (GUI.Button(new Rect(42, 689, 456, 70),
                !lastVictory ? "RETRY" :
                finalTrialStage ? "RETURN TO TRIALS" : "NEXT STAGE", bigButton))
            {
                if (lastVictory && activeTrialMode != TrialMode.Normal &&
                    stageId % ChroniclesTrials.StagesPerChapter == 0)
                {
                    page = Page.Trials;
                }
                else
                {
                    if (lastVictory) stageId = Mathf.Min(60, stageId + 1);
                    page = Page.Formation;
                }
                CleanupBattle();
            }
            if (GUI.Button(new Rect(42, 781, 456, 65), "RETURN HOME", bigButton)) GoHome();
        }

        private void CleanupBattle()
        {
            if (battle != null) Destroy(battle.gameObject);
            battle = null;
            if (menuCamera != null) menuCamera.enabled = true;
            if (menuAudio != null) menuAudio.enabled = true;
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
            activeTrialMode = TrialMode.Normal;
            page = Page.Home;
            if (audio != null) audio.SetState(AudioScene.Menu);
        }
    }
}
