using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    // Integrated campaign navigation. Current IMGUI layout is a functional scaffold
    // to be replaced by production UI Toolkit art without changing progression logic.
    public sealed class ChroniclesGame : MonoBehaviour
    {
        private enum Page { Home, More, Events, Shop, Mailbox, Chapters, Trials, Towers, Raid, Labyrinth, ArenaTraining, Bounty, Legacy, ElementalGates, EventAdventure, SeasonPass, Leaderboards, Guild, Friends, Tournament, StageStory, Heroes, HeroDetails, Formation, Skins, Gear, Forge, Training, Runes, SignatureWeapon, RelicVault, Summon, Quests, WeeklyQuests, Achievements, Idle, Login, Settings, Battle, Results }
        private Page page = Page.Home;
        private Page lastDrawnPage = Page.Home;
        private float pageFadeT;
        private PlayerProgressService progress;
        private ChroniclesAudioDirector audio;
        private ChroniclesBattle battle;
        private ChroniclesCatalogClient catalogClient;
        [SerializeField] private string publicCatalogServerUrl = ""; // HTTPS; opt-in only
        private float draftMusic = .8f, draftEffects = .9f;
        private Camera menuCamera;
        private AudioListener menuAudio;
        private int stageId = 1;
        private TrialMode activeTrialMode = TrialMode.Normal;
        private bool inTower, inRaid, inLabyrinth, inArenaTraining, inElementalGate, inEventAdventure, inGuildBoss, inTournament;
        private string raidRunId = "", labyrinthRunId = "", arenaRunId = "", gateRunId = "", adventureRunId = "", guildBossRunId = "", tournamentRunId = "";
        private HeroElement selectedGateElement = HeroElement.Fire;
        private int selectedAdventureStage = 1;
        private int selectedLabyrinthChoice;
        private int lastRaidDamage;
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
        private int summonFeatureIdx;
        private bool eventSplashDone;
        private SummonBannerType activeSummonBanner = SummonBannerType.Standard;
        private readonly List<string> lastSummonBatch = new List<string>();
        private float summonRevealStart = -99f;
        private bool lastVictory;
        private int clearedStageBeforeBattle = -1;
        private Vector2 chapterScroll, rosterScroll, formationScroll, gearScroll, runeScroll;
        private GUIStyle heading, subtitle, tiny, pageDesc;
        private GUIStyle rowText, tinyCenter, cellText;
        private GUIStyle bigButton;
        private Texture2D panelTex;
        private Texture2D buttonNormalTex, buttonHoverTex, buttonPressedTex, boxTex;
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
            // Portraits show the equipped skin wherever a hero is drawn.
            ChroniclesPortraitAssets.EquippedSkinResolver = heroId =>
            {
                OwnedHero ownedHero = progress != null ? progress.FindOwnedHero(heroId) : null;
                return ownedHero != null ? ownedHero.skin : null;
            };
            catalogClient = gameObject.AddComponent<ChroniclesCatalogClient>();
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
            tiny.padding = new RectOffset(12, 8, 6, 4); // device: text hugged frame edges
            rowText = new GUIStyle(tiny) { alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(16, 8, 0, 0) };
            tinyCenter = new GUIStyle(tiny) { alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 4, 4) };
            pageDesc = new GUIStyle(tiny) { fontSize = 13, wordWrap = true,
                alignment = TextAnchor.UpperCenter,
                padding = new RectOffset(16, 16, 4, 4) };
            pageDesc.normal.textColor = new Color(.78f, .76f, .88f);
            cellText = new GUIStyle(tiny) { fontSize = 11, wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(1, 1, 1, 1) };
            // Original dark-violet / antique-gold Alpha theme. Only lightweight
            // runtime textures are used, so no third-party game art is copied.
            buttonNormalTex = MakePlate(new Color(.19f, .16f, .35f),
                new Color(.105f, .09f, .215f), new Color(.60f, .46f, .29f), 9);
            buttonHoverTex = MakePlate(new Color(.30f, .22f, .46f),
                new Color(.18f, .14f, .30f), new Color(.90f, .71f, .38f), 9);
            buttonPressedTex = MakePlate(new Color(.13f, .10f, .26f),
                new Color(.10f, .07f, .20f), new Color(.93f, .76f, .44f), 9);
            boxTex = MakePlate(new Color(.12f, .095f, .22f, .91f),
                new Color(.075f, .065f, .16f, .88f),
                new Color(.37f, .29f, .45f, .87f), 8);

            GUI.skin.button.normal.background = buttonNormalTex;
            GUI.skin.button.hover.background = buttonHoverTex;
            GUI.skin.button.active.background = buttonPressedTex;
            GUI.skin.button.focused.background = buttonHoverTex;
            GUI.skin.button.border = new RectOffset(10, 10, 10, 10);
            GUI.skin.button.fontSize = 15;
            GUI.skin.button.wordWrap = true;
            GUI.skin.button.normal.textColor = new Color(.96f, .93f, .84f);
            GUI.skin.button.hover.textColor = Color.white;
            GUI.skin.button.active.textColor = new Color(1f, .87f, .49f);
            GUI.skin.box.normal.background = boxTex;
            GUI.skin.box.border = new RectOffset(10, 10, 10, 10);
            // Illustrated ornate panel art replaces the procedural plate for
            // every GUI.Box panel in the game (9-sliced at the corners).
            Texture2D ornatePanelArt = OrnateGuiSkin.Art("panel_frame");
            if (ornatePanelArt != null)
            {
                GUI.skin.box.normal.background = ornatePanelArt;
                GUI.skin.box.border = new RectOffset(44, 44, 44, 44);
            }
            GUI.skin.box.fontSize = 14;
            GUI.skin.box.wordWrap = true;
            GUI.skin.box.normal.textColor = new Color(.90f, .87f, .98f);
            // Authored ornate art overrides the procedural plates globally, so
            // every page gets framed buttons and panels without per-page edits.
            Texture2D btnArt = OrnateGuiSkin.Art("btn_primary");
            if (btnArt != null)
            {
                GUI.skin.button.normal.background = btnArt;
                GUI.skin.button.hover.background = btnArt;
                GUI.skin.button.active.background = btnArt;
                GUI.skin.button.focused.background = btnArt;
                GUI.skin.button.border = new RectOffset(0, 0, 0, 0);
                GUI.skin.button.normal.textColor = OrnateGuiSkin.GoldBright;
            }
            bigButton = new GUIStyle(GUI.skin.button) {
                fontSize = 18, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, wordWrap = true };
            panelTex = new Texture2D(2, 128, TextureFormat.RGBA32, false);
            panelTex.wrapMode = TextureWrapMode.Clamp;
            panelTex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 128; y++)
            {
                float p = y / 127f;
                Color shade = Color.Lerp(new Color(.060f, .044f, .13f),
                    new Color(.135f, .105f, .24f), p);
                shade = Color.Lerp(shade,
                    new Color(.08f, .06f, .17f), .18f + .19f * Mathf.Sin(p * 3f));
                panelTex.SetPixel(0, y, shade);
                panelTex.SetPixel(1, y, shade);
            }
            panelTex.Apply(false);
        }

        private static Texture2D MakePlate(Color top, Color bottom,
            Color outline, int radius)
        {
            const int width = 96, height = 56;
            Texture2D texture = new Texture2D(width, height,
                TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float nearestX = Mathf.Clamp(x, radius, width - radius - 1);
                    float nearestY = Mathf.Clamp(y, radius, height - radius - 1);
                    float dx = x - nearestX, dy = y - nearestY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > radius + .2f)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }
                    Color fill = Color.Lerp(bottom, top, y / (float)(height - 1));
                    if (distance > radius - 1.8f ||
                        x < 2 || y < 2 || x >= width - 2 || y >= height - 2)
                        fill = Color.Lerp(fill, outline, .87f);
                    texture.SetPixel(x, y, fill);
                }
            texture.Apply(false);
            return texture;
        }

        private void OnDestroy()
        {
            if (panelTex != null) Destroy(panelTex);
            if (boxTex != null) Destroy(boxTex);
            if (buttonNormalTex != null) Destroy(buttonNormalTex);
            if (buttonHoverTex != null) Destroy(buttonHoverTex);
            if (buttonPressedTex != null) Destroy(buttonPressedTex);
            ChroniclesHeroPortrait.Clear();
            ChroniclesPortraitAssets.EquippedSkinResolver = null;
        }

        private void OnGUI()
        {
            if (progress == null) return;
            InitializeStyles();
            Matrix4x4 matrix = GUI.matrix;
            Color oldColor = GUI.color;
            float scale;
            GUI.matrix = ChroniclesViewport.Matrix(
                Screen.safeArea, Screen.width, Screen.height, out scale);
            GUI.color = Color.white;

            bool splashUp = page == Page.Home && !eventSplashDone &&
                PlayerPrefs.GetString("chronicles_splash_day", "") != DateTime.UtcNow.ToString("yyyyMMdd");
            if (page != Page.Battle && page != Page.Results)
            {
                GUI.DrawTexture(new Rect(0, 0, 540, 960), panelTex);
                OrnateGuiSkin.ScreenBackground(
                    page == Page.Home ? "bg_home" :
                    page == Page.Summon ? "bg_summon" : "bg_menu");
                GUI.enabled = !splashUp;
                Header();
                GUI.enabled = true;
            }
            switch (page)
            {
                case Page.Home: GUI.enabled = !splashUp; Home(); GUI.enabled = true; break;
                case Page.More: More(); break;
                case Page.Bounty: Bounty(); break;
                case Page.Legacy: Legacy(); break;
                case Page.ElementalGates: ElementalGates(); break;
                case Page.EventAdventure: EventAdventure(); break;
                case Page.SeasonPass: SeasonPass(); break;
                case Page.Leaderboards: Leaderboards(); break;
                case Page.Guild: Guild(); break;
                case Page.Friends: Friends(); break;
                case Page.Tournament: Tournament(); break;
                case Page.Events: Events(); break;
                case Page.Shop: Shop(); break;
                case Page.Mailbox: Mailbox(); break;
                case Page.Chapters: Chapters(); break;
                case Page.Trials: Trials(); break;
                case Page.Towers: Towers(); break;
                case Page.Raid: Raid(); break;
                case Page.Labyrinth: Labyrinth(); break;
                case Page.ArenaTraining: ArenaTraining(); break;
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
                case Page.Achievements: Achievements(); break;
                case Page.Idle: Idle(); break;
                case Page.Login: Login(); break;
                case Page.Settings: Settings(); break;
                case Page.Battle: BattleHud(scale); break;
                case Page.Results: Results(); break;
            }
            if (page == Page.Home && !eventSplashDone)
                EventSplash();
            if (page != Page.Battle && page != Page.Results)
                TutorialOverlay();
            // Smooth page transitions: short black fade on every page switch
            // instead of a hard cut.
            if (page != lastDrawnPage) { pageFadeT = 1f; lastDrawnPage = page; }
            if (pageFadeT > 0f)
            {
                pageFadeT = Mathf.Max(0f, pageFadeT - Time.deltaTime * 3.2f);
                GUI.color = new Color(0f, 0f, 0f, pageFadeT * .9f);
                GUI.DrawTexture(new Rect(0f, 0f, 540f, 960f),
                    Texture2D.whiteTexture);
            }
            GUI.color = oldColor;
            GUI.matrix = matrix;
        }

        private void TutorialOverlay()
        {
            if (progress == null || progress.Data.tutorialCompleted) return;
            int step = Mathf.Clamp(progress.Data.tutorialStep, 0,
                ChroniclesTutorial.StepCount - 1);
            GUI.Box(new Rect(125, 170, 395, 103), "", OrnateGuiSkin.RowBox());
            GUI.Label(new Rect(132, 175, 379, 50),
                "GUIDE " + (step + 1) + "/" + ChroniclesTutorial.StepCount +
                "  •  " + ChroniclesTutorial.Description(step), tiny);
            if (GUI.Button(new Rect(137, 231, 171, 33), "SKIP"))
            {
                if (progress.SkipTutorial()) { page = Page.Home; notice = ""; }
                else notice = "Could not save tutorial choice.";
            }
            if (GUI.Button(new Rect(315, 231, 195, 33),
                step == ChroniclesTutorial.StepCount - 1 ? "FINISH" : "NEXT"))
            {
                if (!progress.AdvanceTutorial())
                {
                    notice = "Could not save tutorial progress.";
                    return;
                }
                switch (step)
                {
                    case 0: page = Page.Heroes; break;
                    case 1: page = Page.Formation; break;
                    case 2: page = Page.Chapters; break;
                    case 3: page = Page.Summon; break;
                    default: page = Page.Home; break;
                }
                notice = "";
            }
        }

        private void Header()
        {
            OrnateGuiSkin.Title(new Rect(20, 12, 500, 48), "CHRONICLES OF THE LOOM", 26);
            GUIStyle subtitleShadow = new GUIStyle(subtitle);
            subtitleShadow.normal.textColor = new Color(0f, 0f, 0f, .8f);
            GUI.Label(new Rect(26, 87, 490, 34), "An original crossover of myths, legends and histories", subtitleShadow);
            GUI.Label(new Rect(25, 85, 490, 34), "An original crossover of myths, legends and histories", subtitle);
            OrnateGuiSkin.Bar(new Rect(15, 112, 510, 55));
            GUIStyle currency = new GUIStyle(tiny) {
                alignment = TextAnchor.MiddleLeft, fontSize = 17 };
            currency.normal.textColor = OrnateGuiSkin.GoldBright;
            DrawCurrency(30, 118, "icon_gold_gems", progress.Data.gold, currency);
            DrawCurrency(158, 118, "icon_gems", progress.Data.gems, currency);
            DrawCurrency(288, 118, "icon_ticket_gold", progress.Data.summonTickets, currency);
            DrawCurrency(425, 118, "icon_stamina", progress.CurrentStamina(), currency);
            if (page != Page.Home && GUI.Button(new Rect(20, 64, 105, 42), "HOME")) GoHome();
            if (!string.IsNullOrEmpty(notice))
            {
                // Toast with a dark backing so it never bleeds into panels below.
                GUI.Box(new Rect(20, 901, 500, 47), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(28, 903, 484, 43), notice, subtitle);
            }
        }

        private void Home()
        {
            GUIStyle plateStyle = new GUIStyle(subtitle) {
                alignment = TextAnchor.MiddleCenter };
            plateStyle.normal.textColor = OrnateGuiSkin.GoldBright;

            // Khalid's own boss artwork comes alive: a breathing red aura
            // around the boss, glowing eyes, drifting red embers in the sky.
            OrnateGuiSkin.PulseGlow(new Rect(140, 0, 260, 270),
                new Color(1f, .18f, .12f, .5f), 1.3f);
            OrnateGuiSkin.PulseGlow(new Rect(190, 40, 160, 190),
                new Color(1f, .1f, .08f, .45f), 2.1f);
            OrnateGuiSkin.PulseGlow(new Rect(244, 130, 22, 14),
                new Color(1f, .08f, .08f, .9f), 3.2f);
            OrnateGuiSkin.PulseGlow(new Rect(270, 130, 22, 14),
                new Color(1f, .08f, .08f, .9f), 3.2f);
            OrnateGuiSkin.Embers(new Rect(0, 0, 540, 430), 14, .03f,
                new Color(1f, .3f, .18f, .55f));

            // The doors are the buildings inside the scene itself: invisible
            // hotspots over each named building, with a glowing label plate.
            if (SceneDoor(new Rect(100, 170, 110, 135),
                new Rect(103, 288, 130, 28), "ARENA",
                progress.Data.lastClearedStage < 10, "CLEAR STORY 10"))
            {
                inArenaTraining = false; page = Page.ArenaTraining;
            }
            if (SceneDoor(new Rect(90, 370, 180, 180),
                new Rect(120, 516, 130, 28), "ELITE DARK"))
            {
                inTower = false; activeTrialMode = TrialMode.Normal;
                chapterScroll = Vector2.zero; page = Page.Trials;
            }
            bool ch1Open = progress.Data.lastClearedStage >= 10;
            if (SceneDoor(new Rect(415, 190, 120, 150),
                new Rect(408, 316, 130, 28), "LABYRINTH",
                !ch1Open, "CLEAR STORY 10"))
            {
                inLabyrinth = false; page = Page.Labyrinth;
            }
            if (SceneDoor(new Rect(415, 395, 120, 170),
                new Rect(408, 548, 130, 28), "TOWERS",
                !ch1Open, "CLEAR STORY 10"))
            {
                inTower = false; chapterScroll = Vector2.zero;
                page = Page.Towers;
            }
            if (SceneDoor(new Rect(210, 70, 130, 170),
                new Rect(207, 238, 130, 28), "WORLD BOSS",
                !ch1Open, "CLEAR STORY 10"))
            {
                inRaid = false; page = Page.Raid;
            }
            if (SceneDoor(new Rect(190, 604, 190, 44),
                new Rect(220, 610, 130, 28),
                progress.CanClaimDailyLogin() ? "FREE GIFT!" : "DAILY GIFT"))
                page = Page.Login;

            ChapterDefinition chapter = WorldsCatalog.Chapters[
                Mathf.Clamp(progress.Data.lastClearedStage / 10, 0,
                    WorldsCatalog.Chapters.Length - 1)];
            OrnateGuiSkin.Embers(new Rect(0, 180, 540, 300), 10, .04f,
                new Color(1f, .85f, .5f, .8f));
            // Story progress as clean shadowed text, no frame.
            GUI.DrawTexture(new Rect(120, 652, 300, 36),
                OrnateGuiSkin.Glow("story_shade", new Color(0f, 0f, 0f, .5f)));
            GUIStyle storyStyle = new GUIStyle(subtitle) {
                alignment = TextAnchor.MiddleCenter };
            storyStyle.normal.textColor = OrnateGuiSkin.GoldBright;
            GUI.Label(new Rect(42, 656, 456, 30),
                "STORY " + progress.Data.lastClearedStage +
                "/60  •  " + chapter.title, storyStyle);
            OrnateGuiSkin.PulseGlow(new Rect(35, 696, 471, 96),
                new Color(1f, .82f, .42f, .8f));
            if (GUI.Button(new Rect(49, 708, 443, 72),
                "CONTINUE ADVENTURE", OrnateGuiSkin.ButtonStyle()))
            {
                inTower = false;
                inRaid = false;
                inLabyrinth = false;
                inArenaTraining = false;
                activeTrialMode = TrialMode.Normal;
                chapterScroll = Vector2.zero;
                page = Page.Chapters;
            }

            // Bottom navigation follows common mobile hero-RPG conventions.
            GUIStyle navStyle = new GUIStyle(OrnateGuiSkin.ButtonStyle());
            navStyle.wordWrap = false;
            navStyle.fontSize = 12;
            navStyle.clipping = TextClipping.Overflow;
            OrnateGuiSkin.Bar(new Rect(12, 802, 516, 85));
            if (GUI.Button(new Rect(18, 813, 80, 61), "HOME", navStyle))
                page = Page.Home;
            if (GUI.Button(new Rect(102, 813, 80, 61), "HEROES", navStyle))
                page = Page.Heroes;
            if (GUI.Button(new Rect(186, 813, 80, 61), "STORY", navStyle))
            {
                inTower = false; activeTrialMode = TrialMode.Normal;
                page = Page.Chapters;
            }
            if (GUI.Button(new Rect(270, 813, 80, 61), "SUMMON", navStyle))
                page = Page.Summon;
            if (GUI.Button(new Rect(354, 813, 80, 61), "EVENTS", navStyle))
                page = Page.Events;
            if (GUI.Button(new Rect(438, 813, 80, 61), "MORE", navStyle))
                page = Page.More;
        }

        // Draws a door over a building inside the Home scene: a soft idle
        // glow plus a small label plate, and an invisible tap hotspot on the
        // building itself. Returns true when tapped.
        private bool SceneDoor(Rect hotspot, Rect labelRect, string label)
        {
            return SceneDoor(hotspot, labelRect, label, false, "");
        }

        private bool SceneDoor(Rect hotspot, Rect labelRect, string label,
            bool locked, string lockHint)
        {
            // Slim dark text backing instead of an ornate frame, so the
            // scene stays clean and the label stays readable.
            GUI.DrawTexture(new Rect(labelRect.x - 8f, labelRect.y - 2f,
                labelRect.width + 16f, labelRect.height + 4f),
                OrnateGuiSkin.Glow("door_label_bg",
                    new Color(0f, 0f, 0f, .6f)));
            if (locked)
            {
                // Darken the building and pin a lock plate on it, like the
                // reference home-city screens do for gated buildings.
                Color pre = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, .45f);
                GUI.DrawTexture(hotspot, Texture2D.whiteTexture);
                GUI.color = pre;
                Rect plate = new Rect(hotspot.center.x - 55f,
                    hotspot.center.y - 22f, 110f, 44f);
                GUI.DrawTexture(new Rect(plate.x - 6f, plate.y - 4f,
                    plate.width + 12f, plate.height + 8f),
                    OrnateGuiSkin.Glow("door_lock_bg",
                        new Color(0f, 0f, 0f, .75f)));
                OrnateGuiSkin.PulseGlow(plate,
                    new Color(.8f, .8f, .8f, .8f), 2.2f);
                GUIStyle lockStyle = new GUIStyle(tiny) {
                    alignment = TextAnchor.MiddleCenter, fontSize = 11 };
                lockStyle.normal.textColor = new Color(.92f, .92f, .95f);
                GUI.Label(plate, "LOCKED\n" + lockHint, lockStyle);
                return false;
            }
            OrnateGuiSkin.PulseGlow(labelRect,
                OrnateGuiSkin.GoldBright, 1.6f);
            GUIStyle labelStyle = new GUIStyle(subtitle) {
                alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            labelStyle.normal.textColor = OrnateGuiSkin.GoldBright;
            GUI.Label(labelRect, label, labelStyle);
            return GUI.Button(hotspot, "", GUIStyle.none);
        }

        private void More()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 215, 480, 54), "MORE ACTIVITIES", 24);
            GUI.Label(new Rect(34, 278, 468, 58),
                "Training, rewards and progression in one place.", pageDesc);
            string[] names =
            {
                "EVENTS", "FORMATION",
                "DAILY MISSIONS", "WEEKLY MISSIONS",
                "ACHIEVEMENTS", "AFK REWARDS",
                "LOGIN REWARDS", "RELIC VAULT",
                "FACTION TOWERS", "WORLD BOSS",
                "LABYRINTH", "ARENA TRAINING",
                "SHOP", "MAILBOX",
                "BOUNTY BOARD", "CHRONICLE LEGACY",
                "ELEMENTAL GATES", "EVENT ADVENTURE",
                "SEASON PASS", "RANKINGS",
                "GUILD", "FRIENDS",
                "TOURNAMENT", "SETTINGS"
            };
            int rows = (names.Length + 1) / 2;
            chapterScroll = GUI.BeginScrollView(new Rect(19, 340, 505, 545),
                chapterScroll, new Rect(0, 0, 471, rows * 80f));
            for (int i = 0; i < names.Length; i++)
            {
                int row = i / 2, col = i % 2;
                float x = 15 + col * 236f, y = 4 + row * 80f;
                if (!GUI.Button(new Rect(x, y, 224, 70), names[i], bigButton))
                    continue;
                switch (i)
                {
                    case 0: page = Page.Events; break;
                    case 1: page = Page.Formation; break;
                    case 2: page = Page.Quests; break;
                    case 3: page = Page.WeeklyQuests; break;
                    case 4: page = Page.Achievements; break;
                    case 5: page = Page.Idle; break;
                    case 6: page = Page.Login; break;
                    case 7: page = Page.RelicVault; break;
                    case 8: page = Page.Towers; break;
                    case 9: inRaid = false; page = Page.Raid; break;
                    case 10: inLabyrinth = false; page = Page.Labyrinth; break;
                    case 11: inArenaTraining = false; page = Page.ArenaTraining; break;
                    case 12: page = Page.Shop; break;
                    case 13: page = Page.Mailbox; break;
                    case 14: page = Page.Bounty; break;
                    case 15: page = Page.Legacy; break;
                    case 16: page = Page.ElementalGates; break;
                    case 17: page = Page.EventAdventure; break;
                    case 18: page = Page.SeasonPass; break;
                    case 19: page = Page.Leaderboards; break;
                    case 20: page = Page.Guild; break;
                    case 21: page = Page.Friends; break;
                    case 22: page = Page.Tournament; break;
                    case 23: page = Page.Settings; break;
                }
            }
            GUI.EndScrollView();
        }

        private void Legacy()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 214, 480, 54), "CHRONICLE LEGACY", 24);
            int earned = ChroniclesGrowth.LegacyPointsEarned(progress.Data);
            int spent = ChroniclesGrowth.LegacyPointsSpent(progress.Data);
            GUI.Label(new Rect(29, 272, 479, 42),
                "Account-wide blessings. Points: " + (earned - spent) +
                " available  (" + earned + " earned from story + achievements)",
                pageDesc);
            string[] names = { "WARPATH", "AEGIS", "FORTUNE" };
            int[] levels =
            {
                progress.Data.legacyWar, progress.Data.legacyGuard,
                progress.Data.legacyFortune
            };
            string[] effects =
            {
                "+" + (progress.Data.legacyWar * 1) + "% attack for every hero",
                "+" + (progress.Data.legacyGuard * 1) + "% health and +" +
                    (progress.Data.legacyGuard / 2f).ToString("0.#") + "% defense",
                "+" + (progress.Data.legacyFortune * 2) + "% gold from idle and fast battle"
            };
            for (int i = 0; i < 3; i++)
            {
                float y = 336 + i * 120f;
                GUI.Box(new Rect(26, y, 488, 108), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(44, y + 12, 300, 30),
                    names[i] + "  LV " + levels[i] + "/" +
                    ChroniclesGrowth.LegacyCap, subtitle);
                GUI.Label(new Rect(44, y + 44, 300, 54), effects[i], tiny);
                GUI.enabled = earned - spent > 0 &&
                    levels[i] < ChroniclesGrowth.LegacyCap;
                if (GUI.Button(new Rect(360, y + 26, 136, 56), "BLESS +1"))
                    notice = progress.SpendLegacyPoint(i) ?
                        names[i] + " grows stronger." : "No legacy points.";
                GUI.enabled = true;
            }
            GUI.Box(new Rect(26, 712, 488, 96),
                "Earn a point every 3 story stages and for each achievement " +
                "claimed. Blessings are permanent and apply to the whole roster.");
        }

        private void ElementalGates()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 214, 480, 54), "ELEMENTAL GATES", 24);
            GUI.Label(new Rect(29, 270, 479, 44),
                "Element-locked dungeons. Your whole team must share the gate's element." +
                "\nAttempts left today: " + progress.GateAttemptsRemaining() + "/" +
                ChroniclesElementalGates.DailyAttempts + "  •  All gates open Sunday",
                subtitle);
            HeroElement[] order =
            {
                HeroElement.Fire, HeroElement.Water, HeroElement.Nature,
                HeroElement.Wind, HeroElement.Light, HeroElement.Darkness
            };
            for (int i = 0; i < order.Length; i++)
            {
                HeroElement element = order[i];
                float y = 352 + i * 90f;
                bool open = ChroniclesElementalGates.IsOpen(element, System.DateTime.UtcNow);
                ElementalGateProgress gate = progress.GateState(element);
                int level = Mathf.Clamp(gate.unlockedLevel, 1, ChroniclesElementalGates.Levels);
                GUI.Box(new Rect(26, y, 488, 80), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(44, y + 8, 320, 28),
                    ChroniclesElementalGates.GateName(element) + "  •  LV " + level,
                    subtitle);
                GUI.Label(new Rect(44, y + 40, 330, 34),
                    open ? "Rewards: " + ChroniclesElementalGates.RewardLabel(level,
                        !gate.firstClears.Contains(level)) :
                        ChroniclesElementalGates.OpensLabel(element), tiny);
                bool teamOk = progress.GateTeamMatches(element);
                GUI.enabled = open && progress.GateAttemptsRemaining() > 0 && teamOk;
                if (GUI.Button(new Rect(382, y + 14, 116, 52),
                    !open ? "CLOSED" : !teamOk ? "TEAM" : "FIGHT"))
                {
                    inElementalGate = true;
                    inTower = false; inRaid = false; inLabyrinth = false;
                    inArenaTraining = false; inEventAdventure = false; inGuildBoss = false; inTournament = false;
                    activeTrialMode = TrialMode.Normal;
                    selectedGateElement = element;
                    stageId = ChroniclesElementalGates.StageTemplateFor(level);
                    BeginBattle();
                }
                GUI.enabled = true;
            }
        }

        private void EventAdventure()
        {
            var def = ChroniclesEventAdventures.Active(System.DateTime.UtcNow);
            var next = ChroniclesEventAdventures.Next(System.DateTime.UtcNow);
            var state = progress.AdventureState(def.id);
            TimeSpan left = ChroniclesEventAdventures.TimeLeft(System.DateTime.UtcNow);
            OrnateGuiSkin.TitlePlate(new Rect(30, 214, 480, 54), "EVENT ADVENTURE", 24);
            GUI.Label(new Rect(29, 270, 479, 66),
                def.title + "\n" + def.tagline, pageDesc);
            GUI.Label(new Rect(29, 336, 479, 40),
                "Ends in " + left.Days + "d " + left.Hours + "h  •  Next: " +
                next.title + "\n" + def.currencyName + ": " + state.currencyEarned,
                tiny);
            float contentHeight = ChroniclesEventAdventures.Stages * 62f + 44f +
                ChroniclesEventAdventures.MilestoneThresholds.Length * 58f;
            gearScroll = GUI.BeginScrollView(new Rect(19, 392, 505, 493),
                gearScroll, new Rect(0, 0, 471, contentHeight));
            float y = 8f;
            for (int stage = 1; stage <= ChroniclesEventAdventures.Stages; stage++)
            {
                bool cleared = stage <= state.highestCleared;
                bool playable = progress.CanEnterAdventureStage(def.id, stage);
                GUI.Box(new Rect(26, y, 488, 56), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(44, y + 6, 250, 24),
                    "STAGE " + stage + (cleared ? "  •  CLEARED" : ""), subtitle);
                GUI.Label(new Rect(44, y + 30, 320, 22),
                    ChroniclesEventAdventures.CurrencyReward(stage, !cleared) + " " +
                    def.currencyName + "  •  " +
                    ChroniclesEventAdventures.GoldReward(stage) + " gold", tiny);
                GUI.enabled = playable;
                if (GUI.Button(new Rect(402, y + 8, 96, 40), cleared ? "REPLAY" : "FIGHT"))
                {
                    inEventAdventure = true;
                    inTower = false; inRaid = false; inLabyrinth = false;
                    inArenaTraining = false; inElementalGate = false;
                    activeTrialMode = TrialMode.Normal;
                    selectedAdventureStage = stage;
                    stageId = ChroniclesEventAdventures.StageTemplateFor(stage);
                    BeginBattle();
                }
                GUI.enabled = true;
                y += 62f;
            }
            y += 8f;
            GUI.Label(new Rect(44, y, 400, 30), "MILESTONE REWARDS", subtitle);
            y += 36f;
            foreach (int threshold in ChroniclesEventAdventures.MilestoneThresholds)
            {
                bool claimed = state.milestonesClaimed.Contains(threshold);
                bool ready = state.currencyEarned >= threshold && !claimed;
                GUI.Box(new Rect(26, y, 488, 52), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(44, y + 6, 300, 40),
                    threshold + " " + def.currencyName + "\n" +
                    ChroniclesEventAdventures.MilestoneRewardLabel(threshold), tiny);
                GUI.enabled = ready;
                if (GUI.Button(new Rect(402, y + 8, 96, 36),
                    claimed ? "TAKEN" : "CLAIM"))
                    notice = progress.ClaimAdventureMilestone(threshold) ?
                        "Milestone claimed: " +
                        ChroniclesEventAdventures.MilestoneRewardLabel(threshold) :
                        "Not enough " + def.currencyName + " yet.";
                GUI.enabled = true;
                y += 58f;
            }
            GUI.EndScrollView();
        }

        private void SeasonPass()
        {
            int level = progress.PassLevel();
            int xpInto = ChroniclesBattlePass.XpIntoLevel(progress.Data.passSeasonXp);
            DateTime end = ChroniclesBattlePass.SeasonEnd(System.DateTime.UtcNow);
            TimeSpan left = end - System.DateTime.UtcNow;
            OrnateGuiSkin.TitlePlate(new Rect(30, 214, 480, 54), "SEASON PASS", 24);
            GUI.Label(new Rect(29, 270, 479, 66),
                "LEVEL " + level + "/" + ChroniclesBattlePass.Levels +
                "  •  " + xpInto + "/" + ChroniclesBattlePass.XpPerLevel + " XP" +
                "\nSeason resets in " + left.Days + "d " + left.Hours +
                "h. Earn XP from battles and quest claims.", pageDesc);
            float contentHeight = ChroniclesBattlePass.Levels * 58f;
            gearScroll = GUI.BeginScrollView(new Rect(19, 348, 505, 537),
                gearScroll, new Rect(0, 0, 471, contentHeight));
            for (int i = 1; i <= ChroniclesBattlePass.Levels; i++)
            {
                float y = (i - 1) * 58f;
                bool claimed = progress.Data.passLevelsClaimed.Contains(i);
                bool ready = i <= level && !claimed;
                GUI.Box(new Rect(6, y, 460, 52), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(16, y + 6, 60, 40), "LV " + i, subtitle);
                GUI.Label(new Rect(86, y + 6, 260, 40),
                    ChroniclesBattlePass.RewardLabel(i), tiny);
                GUI.enabled = ready;
                if (GUI.Button(new Rect(356, y + 8, 102, 36),
                    claimed ? "TAKEN" : "CLAIM"))
                    notice = progress.ClaimPassLevel(i) ?
                        "Pass level " + i + " claimed: " +
                        ChroniclesBattlePass.RewardLabel(i) :
                        "Reach level " + i + " first.";
                GUI.enabled = true;
            }
            GUI.EndScrollView();
        }

        private int leaderboardTab;

        private void Leaderboards()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 214, 480, 54), "RANKINGS", 24);
            GUI.Label(new Rect(29, 270, 479, 40),
                "Local season rivals - online rankings arrive with the server.",
                tiny);
            string[] tabs = { "ARENA", "STORY", "TOWER" };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (GUI.Button(new Rect(26 + i * 168, 316, 160, 52), tabs[i],
                    OrnateGuiSkin.ButtonStyle(i == leaderboardTab)))
                    leaderboardTab = i;
            }
            string boardId;
            int playerScore;
            if (leaderboardTab == 0)
            { boardId = "arena"; playerScore = progress.ArenaTrainingState().rating; }
            else if (leaderboardTab == 1)
            { boardId = "story"; playerScore = progress.Data.lastClearedStage; }
            else
            { boardId = "tower"; playerScore = progress.TowerHighestTotal(); }
            var board = ChroniclesLeaderboards.Build(boardId, "YOU", playerScore,
                System.DateTime.UtcNow);
            int rank = ChroniclesLeaderboards.PlayerRank(board);
            GUI.Label(new Rect(29, 376, 479, 30),
                "YOUR RANK: #" + rank + " / " + board.Count +
                "  •  Weekly reward: " + ChroniclesLeaderboards.RankRewardLabel(rank),
                subtitle);
            GUI.enabled = true;
            if (GUI.Button(new Rect(330, 372, 180, 40), "CLAIM WEEKLY"))
            {
                string lbLabel;
                notice = progress.ClaimLeaderboardReward(rank, out lbLabel) ?
                    "Weekly ranking reward: " + lbLabel :
                    "Weekly reward already claimed.";
            }
            gearScroll = GUI.BeginScrollView(new Rect(19, 420, 505, 465),
                gearScroll, new Rect(0, 0, 471, board.Count * 44f));
            for (int i = 0; i < board.Count; i++)
            {
                var entry = board[i];
                float y = i * 44f;
                if (entry.isPlayer)
                    GUI.Box(new Rect(6, y, 460, 40), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(16, y + 8, 60, 26), "#" + (i + 1),
                    entry.isPlayer ? subtitle : tiny);
                GUI.Label(new Rect(80, y + 8, 240, 26), entry.name,
                    entry.isPlayer ? subtitle : tiny);
                GUI.Label(new Rect(330, y + 8, 130, 26),
                    entry.score.ToString(), entry.isPlayer ? subtitle : tiny);
            }
            GUI.EndScrollView();
        }

        private void Guild()
        {
            var guild = progress.GuildState();
            int weekly = ChroniclesGuild.MemberContributionWeek(System.DateTime.UtcNow) +
                guild.contributionTotal;
            int bossTotal = guild.guildBossDamageToday +
                ChroniclesGuild.MemberBossDamageToday(System.DateTime.UtcNow);
            OrnateGuiSkin.TitlePlate(new Rect(30, 214, 480, 54), "GUILD", 24);
            GUI.Label(new Rect(29, 270, 479, 62),
                ChroniclesGuild.GuildName + "  •  LEVEL " +
                ChroniclesGuild.GuildLevel(guild.contributionTotal) +
                "  •  " + (ChroniclesGuild.MemberCount) + " MEMBERS" +
                "\nOffline prototype - live guilds arrive with the server.", pageDesc);
            GUI.Box(new Rect(26, 348, 488, 108), "", OrnateGuiSkin.RowBox());
            GUI.Label(new Rect(44, 358, 300, 88),
                "WEEKLY ACTIVITY: " + weekly +
                "\nYour contribution today: " + guild.contributionToday + "/" +
                ChroniclesGuild.DailyContributionCap +
                "\n" + ChroniclesGuild.WeeklyChestLabel(
                    ChroniclesGuild.WeeklyChestTier(weekly)), tiny);
            string chestLabel;
            GUI.enabled = ChroniclesGuild.WeeklyChestTier(weekly) > 0;
            if (GUI.Button(new Rect(360, 378, 136, 56), "CHEST"))
                notice = progress.ClaimGuildWeeklyChest(out chestLabel) ?
                    "Guild chest: " + chestLabel :
                    "Chest already claimed this week.";
            GUI.enabled = true;
            GUI.Box(new Rect(26, 470, 488, 108), "", OrnateGuiSkin.RowBox());
            GUI.Label(new Rect(44, 480, 300, 88),
                "GUILD BOSS - DAILY" +
                "\nGuild damage today: " + bossTotal.ToString("N0") +
                " (yours " + guild.guildBossDamageToday.ToString("N0") + ")" +
                "\nAttacks left: " + progress.GuildBossAttemptsRemaining() + "/" +
                ChroniclesGuild.BossDailyAttacks, tiny);
            GUI.enabled = progress.GuildBossAttemptsRemaining() > 0 &&
                progress.Data.lastClearedStage >= 10 &&
                string.IsNullOrEmpty(guild.guildBossPendingRunId);
            if (GUI.Button(new Rect(360, 500, 136, 56), "ATTACK"))
            {
                inGuildBoss = true;
                inRaid = false; inTower = false; inLabyrinth = false;
                inArenaTraining = false; inElementalGate = false;
                inEventAdventure = false;
                activeTrialMode = TrialMode.Normal;
                stageId = ChroniclesRaids.StageForDay(System.DateTime.UtcNow).id;
                notice = "Deal as much damage as you can.";
                page = Page.Formation;
            }
            GUI.enabled = true;
            float my = 592f;
            foreach (int threshold in ChroniclesGuild.BossMilestones)
            {
                bool claimed = guild.guildBossMilestonesClaimed.Contains(threshold);
                bool ready = bossTotal >= threshold && !claimed;
                GUI.Box(new Rect(26, my, 488, 50), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(44, my + 5, 310, 40),
                    threshold.ToString("N0") + " damage\n" +
                    ChroniclesGuild.BossMilestoneLabel(threshold), tiny);
                GUI.enabled = ready;
                if (GUI.Button(new Rect(402, my + 8, 96, 34),
                    claimed ? "TAKEN" : "CLAIM"))
                    notice = progress.ClaimGuildBossMilestone(threshold) ?
                        "Guild boss milestone: " +
                        ChroniclesGuild.BossMilestoneLabel(threshold) :
                        "Guild has not reached that damage yet.";
                GUI.enabled = true;
                my += 56f;
            }
        }

        private void Friends()
        {
            string today = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            OrnateGuiSkin.TitlePlate(new Rect(30, 214, 480, 54), "FRIENDS", 24);
            GUI.Label(new Rect(29, 270, 479, 60),
                "Offline prototype - real friends arrive with the server." +
                "\nFriend points: " + progress.Data.friends.friendPoints +
                "  •  Mercenary hire: " + ChroniclesFriends.MercenaryCost +
                " pts (one +8% team blessing)", subtitle);
            float y = 352f;
            foreach (var friend in ChroniclesFriends.Friends)
            {
                bool gifted = progress.Data.friends.giftUtcDay == today &&
                    progress.Data.friends.giftsClaimedToday.Contains(friend.name);
                GUI.Box(new Rect(26, y, 488, 84), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(44, y + 8, 250, 30), friend.name, rowText);
                GUI.Label(new Rect(44, y + 40, 260, 40),
                    "Champion: " + friend.championLabel +
                    "\nDaily gift: " + friend.giftGold + " gold + " +
                    friend.giftPoints + " pts", tiny);
                GUI.enabled = !gifted;
                if (GUI.Button(new Rect(300, y + 18, 100, 48),
                    gifted ? "TAKEN" : "GIFT"))
                    notice = progress.ClaimFriendGift(friend.name) ?
                        friend.name + "'s gift claimed." : "Already claimed today.";
                GUI.enabled = progress.Data.friends.mercenaryUtcDay != today &&
                    progress.Data.friends.friendPoints >=
                    ChroniclesFriends.MercenaryCost;
                if (GUI.Button(new Rect(408, y + 18, 100, 48), "HIRE"))
                    notice = progress.HireMercenary(friend.championId) ?
                        friend.championLabel + " joins your next battle (+8%)." :
                        "Hire unavailable today.";
                GUI.enabled = true;
                y += 92f;
            }
            if (progress.Data.friends.mercenaryPending)
                GUI.Label(new Rect(29, y + 8, 479, 30),
                    "A mercenary blessing is ready for your next battle.", subtitle);
        }

        private void Tournament()
        {
            string week = ChroniclesTournament.WeekKey(System.DateTime.UtcNow);
            TimeSpan left = ChroniclesTournament.WeekEnd(System.DateTime.UtcNow) -
                System.DateTime.UtcNow;
            int won = progress.Data.tournamentRoundsWon;
            bool out_ = progress.Data.tournamentEliminated;
            OrnateGuiSkin.TitlePlate(new Rect(30, 214, 480, 54), "TOURNAMENT", 24);
            GUI.Label(new Rect(29, 270, 479, 62),
                "WEEKLY 8-SEAT BRACKET  •  ENDS IN " + left.Days + "D " +
                left.Hours + "H\nOffline prototype - live brackets arrive " +
                "with the server.", pageDesc);
            GUI.Box(new Rect(26, 350, 488, 130), "");
            GUI.Label(new Rect(44, 362, 440, 106),
                "STATUS: " + ChroniclesTournament.PlacementLabel(won, out_) +
                "\nROUNDS WON: " + won + "/" + ChroniclesTournament.Rounds +
                "\nREWARD: " + ChroniclesTournament.RewardLabel(won, out_) +
                (won < ChroniclesTournament.Rounds && !out_ ?
                    "\nNEXT OPPONENT: " +
                    ChroniclesTournament.OpponentFor(week, won) : ""), subtitle);
            string tLabel;
            bool canClaim = (won >= ChroniclesTournament.Rounds || out_) &&
                progress.Data.tournamentRewardClaimed != week;
            GUI.enabled = canClaim;
            if (GUI.Button(new Rect(26, 494, 240, 64), "CLAIM REWARD"))
                notice = progress.ClaimTournamentReward(out tLabel) ?
                    "Tournament reward: " + tLabel : "Nothing to claim yet.";
            GUI.enabled = progress.CanEnterTournament();
            if (GUI.Button(new Rect(278, 494, 240, 64),
                won >= ChroniclesTournament.Rounds ? "CHAMPION" :
                out_ ? "ELIMINATED" : "FIGHT " +
                ChroniclesTournament.RoundNames[won]))
            {
                inTournament = true;
                inRaid = false; inTower = false; inLabyrinth = false;
                inArenaTraining = false; inElementalGate = false;
                inEventAdventure = false; inGuildBoss = false;
                activeTrialMode = TrialMode.Normal;
                stageId = ChroniclesTournament.StageFor(won);
                notice = "Win to advance. Lose and the bracket ends.";
                page = Page.Formation;
            }
            GUI.enabled = true;
            GUI.Box(new Rect(26, 578, 488, 90), "", OrnateGuiSkin.RowBox());
            GUI.Label(new Rect(44, 590, 440, 68),
                "THE FIELD: YOU + " +
                string.Join(", ", ChroniclesTournament.RivalNames) +
                "\nNPC results resolve as your bracket advances.", tiny);
        }

        private void Bounty()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 214, 480, 54), "BOUNTY BOARD", 24);
            GUI.Label(new Rect(29, 269, 479, 40),
                "Dispatch heroes on timed missions. New bounties daily (UTC).",
                pageDesc);
            var daily = ChroniclesBounty.DailyBounties(System.DateTime.UtcNow);
            gearScroll = GUI.BeginScrollView(new Rect(19, 330, 505, 550),
                gearScroll, new Rect(0, 0, 471, daily.Count * 105));
            for (int i = 0; i < daily.Count; i++)
            {
                BountyDefinition def = daily[i];
                float y = i * 105f;
                GUI.Box(new Rect(6, y, 460, 99), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(16, y + 7, 300, 27), def.name, rowText);
                GUI.Label(new Rect(16, y + 36, 295, 57),
                    def.hours + "h  •  " + def.minHeroes + "+ heroes  •  power " +
                    def.requiredPower + "\n" + def.RewardLabel, tiny);
                ActiveBounty running = progress.FindActiveBounty(def.id);
                bool claimedToday = progress.Data.claimedBountyIds.Contains(def.id);
                if (running != null)
                {
                    long left = running.finishTicks - System.DateTime.UtcNow.Ticks;
                    if (left <= 0)
                    {
                        if (GUI.Button(new Rect(314, y + 21, 142, 59), "CLAIM"))
                            notice = progress.ClaimBounty(def.id) ?
                                "Bounty complete: " + def.RewardLabel :
                                "Bounty is still running.";
                    }
                    else
                    {
                        var span = System.TimeSpan.FromTicks(left);
                        GUI.Label(new Rect(314, y + 30, 142, 40),
                            "RETURN IN\n" + (int)span.TotalHours + "H " +
                            span.Minutes + "M", tiny);
                    }
                }
                else if (claimedToday)
                {
                    GUI.Label(new Rect(314, y + 30, 142, 40), "DONE", tiny);
                }
                else
                {
                    int power = progress.BestDispatchPower(def.minHeroes);
                    GUI.enabled = power >= def.requiredPower;
                    if (GUI.Button(new Rect(314, y + 21, 142, 59),
                        power >= def.requiredPower ? "DISPATCH" : "WEAK"))
                        notice = progress.StartBounty(def.id) ?
                            def.name + " dispatched." : "Not enough hero power.";
                    GUI.enabled = true;
                }
            }
            GUI.EndScrollView();
        }

        private void Shop()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 214, 480, 54), "DAILY RESOURCE SHOP", 24);
            GUI.Label(new Rect(29, 269, 479, 67),
                "OFFLINE SHOP • NO REAL-MONEY PURCHASES" +
                "\nGOLD " + progress.Data.gold + "  •  GEMS " + progress.Data.gems,
                pageDesc);
            gearScroll = GUI.BeginScrollView(new Rect(19, 343, 505, 537),
                gearScroll, new Rect(0, 0, 471, ChroniclesShop.Offers.Length * 105));
            for (int i = 0; i < ChroniclesShop.Offers.Length; i++)
            {
                ShopOffer offer = ChroniclesShop.Offers[i];
                int left = progress.ShopRemaining(offer.id);
                float y = i * 105f;
                GUI.Box(new Rect(6, y, 460, 99), "", OrnateGuiSkin.RowBox());
                // Illustrated item icon per offer when the art exists.
                Texture2D offerIcon = OrnateGuiSkin.Art("icon_" + offer.id);
                if (offerIcon != null)
                    GUI.DrawTexture(new Rect(14, y + 17, 66, 66), offerIcon,
                        ScaleMode.ScaleToFit);
                float textX = offerIcon != null ? 92f : 16f;
                float textW = 460f - textX - 160f;
                GUI.Label(new Rect(textX, y + 7, textW, 27), offer.name, rowText);
                GUI.Label(new Rect(textX, y + 36, textW, 57),
                    offer.RewardLabel + "\n" + offer.CostLabel +
                    "  •  LEFT " + left + "/" + offer.dailyLimit, tiny);
                GUI.enabled = left > 0;
                if (GUI.Button(new Rect(314, y + 21, 142, 59),
                    left > 0 ? "EXCHANGE" : "SOLD OUT"))
                    notice = progress.BuyShopOffer(offer.id) ?
                        "Purchased: " + offer.RewardLabel :
                        "Insufficient funds or daily limit reached.";
                GUI.enabled = true;
            }
            GUI.EndScrollView();
        }

        private void Mailbox()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 215, 480, 54), "MAILBOX", 24);
            GUI.Label(new Rect(31, 279, 477, 58),
                progress.UnclaimedMailCount() + " UNCLAIMED REWARD MESSAGES" +
                "\nOffline inbox. Future server notices are not active.", pageDesc);
            gearScroll = GUI.BeginScrollView(new Rect(19, 353, 505, 529),
                gearScroll, new Rect(0, 0, 471, Mathf.Max(529,
                    progress.Data.mailbox.Count * 145 + 8)));
            for (int i = 0; i < progress.Data.mailbox.Count; i++)
            {
                RewardMail mail = progress.Data.mailbox[i];
                float y = i * 145f;
                GUI.Box(new Rect(5, y, 463, 137), "");
                GUI.Label(new Rect(18, y + 8, 435, 32), mail.subject, rowText);
                GUI.Label(new Rect(17, y + 45, 435, 49),
                    mail.body + "\n" + mail.RewardSummary, tiny);
                GUI.enabled = !mail.claimed;
                if (GUI.Button(new Rect(290, y + 97, 166, 32),
                    mail.claimed ? "CLAIMED" : "CLAIM REWARD"))
                    notice = progress.ClaimMailReward(mail.id) ?
                        "Mailbox reward collected." : "Already claimed.";
                GUI.enabled = true;
            }
            GUI.EndScrollView();
        }

        private void ArenaTraining()
        {
            ArenaTrainingProgress arena = progress.ArenaTrainingState();
            OrnateGuiSkin.TitlePlate(new Rect(30, 220, 480, 54), "ARENA TRAINING", 24);
            string arenaTier = ChroniclesArenaTraining.Tier(arena.seasonBest);
            TimeSpan arenaLeft = ChroniclesArenaTraining.SeasonEnd(System.DateTime.UtcNow) -
                System.DateTime.UtcNow;
            GUI.Label(new Rect(32, 284, 473, 85),
                "OFFLINE NPC LADDER • SEASON " + arena.seasonKey +
                " • ENDS IN " + arenaLeft.Days + "D" +
                "\n5 matches per UTC day. Online PvP is not available yet.", pageDesc);
            GUI.Box(new Rect(39, 384, 462, 220), "");
            GUI.Label(new Rect(50, 407, 438, 180),
                "CURRENT TIER: " + ChroniclesArenaTraining.Tier(arena.rating) +
                "\nRATING: " + arena.rating +
                "  •  PERSONAL BEST: " + arena.bestRating +
                "\nSEASON BEST: " + arena.seasonBest + " (" + arenaTier + ")" +
                "\nMATCH RECORD: " + arena.wins + " W / " + arena.losses + " L" +
                "\nATTEMPTS LEFT: " + progress.ArenaAttemptsRemaining() +
                "/" + ChroniclesArenaTraining.DailyAttempts, subtitle);
            GUI.Box(new Rect(39, 619, 463, 126), "");
            GUI.Label(new Rect(49, 630, 300, 101),
                "SEASON REWARD: " +
                ChroniclesArenaTraining.SeasonRewardLabel(arenaTier) +
                (string.IsNullOrEmpty(arena.prevSeasonSummary) ? "" :
                    "\nLast season: " + arena.prevSeasonSummary) +
                "\nWIN: +22 RATING, 260 GOLD, 12 GEMS" +
                "\nLOSS: -12 RATING, 70 GOLD", tiny);
            GUI.enabled = arena.seasonTierClaimed != arenaTier;
            if (GUI.Button(new Rect(362, 658, 130, 50),
                arena.seasonTierClaimed == arenaTier ? "CLAIMED" : "CLAIM"))
                notice = progress.ClaimArenaSeasonReward() ?
                    "Season reward claimed: " +
                    ChroniclesArenaTraining.SeasonRewardLabel(arenaTier) :
                    "Season reward already claimed at this tier.";
            GUI.enabled = true;
            GUI.enabled = progress.Data.lastClearedStage >= 10 &&
                progress.ArenaAttemptsRemaining() > 0 &&
                string.IsNullOrEmpty(arena.pendingMatchToken);
            if (GUI.Button(new Rect(42, 782, 455, 78),
                "FIND NPC TRAINING MATCH", bigButton))
            {
                stageId = ChroniclesArenaTraining.StageFor(
                    arena.rating, arena.attemptsToday + 1);
                inArenaTraining = true;
                inLabyrinth = false;
                inRaid = false;
                inTower = false;
                inElementalGate = false; inEventAdventure = false; inGuildBoss = false; inTournament = false;
                activeTrialMode = TrialMode.Normal;
                notice = "Choose six heroes for the NPC duel.";
                page = Page.Formation;
            }
            GUI.enabled = true;
            if (progress.Data.lastClearedStage < 10)
                GUI.Label(new Rect(41, 865, 455, 26),
                    "Clear Chapter 1 to unlock training.", tiny);
        }

        private void Labyrinth()
        {
            LabyrinthProgress run = progress.LabyrinthState();
            OrnateGuiSkin.TitlePlate(new Rect(30, 213, 480, 54), "WEEKLY LABYRINTH", 24);
            GUI.Label(new Rect(29, 270, 475, 75),
                "12 CHAMBERS • THREE GUARDIANS • BRANCHING PATHS" +
                "\nA new layout begins each Monday (UTC). Local PvE only.", pageDesc);
            GUI.Box(new Rect(30, 358, 478, 125), "");
            GUI.Label(new Rect(43, 367, 451, 108),
                "WEEK OF " + run.weekKey +
                "\nROOM " + Mathf.Min(run.nextRoom, ChroniclesLabyrinth.Rooms) +
                "/" + ChroniclesLabyrinth.Rooms +
                "  •  LIVES " + run.lives + "/3" +
                "\nBLESSINGS " + run.blessingStacks +
                "  •  SCORE " + run.score, subtitle);
            if (progress.Data.lastClearedStage < 10)
            {
                GUI.Box(new Rect(40, 515, 461, 199),
                    "LOCKED\nClear Chapter 1 to enter the Labyrinth.");
                return;
            }
            if (run.completed || run.lives == 0)
            {
                GUI.Box(new Rect(40, 515, 460, 200),
                    run.completed ?
                        "LABYRINTH COMPLETED\nWeekly completion bonus received!" :
                        "YOUR EXPEDITION HAS ENDED\nTry again after the weekly reset.");
                return;
            }
            if (!string.IsNullOrEmpty(run.pendingBattleToken))
            {
                GUI.Box(new Rect(42, 526, 459, 171),
                    "An encounter is already in progress.");
                return;
            }
            List<LabyrinthOption> options = ChroniclesLabyrinth.Options(
                run.weekKey, run.nextRoom);
            GUI.Label(new Rect(42, 500, 451, 45),
                options.Count == 1 ? "GUARDIAN ENCOUNTER" : "CHOOSE YOUR PATH", subtitle);
            for (int i = 0; i < options.Count; i++)
            {
                LabyrinthOption option = options[i];
                float y = 558 + i * 129;
                GUI.Box(new Rect(32, y, 478, 114), "");
                GUI.Label(new Rect(43, y + 8, 454, 51), option.title, rowText);
                GUI.Label(new Rect(44, y + 58, 459, 47),
                    option.description, tiny);
                if (GUI.Button(new Rect(371, y + 75, 122, 34),
                    option.RequiresBattle ? "FIGHT" : "CHOOSE"))
                {
                    if (option.RequiresBattle)
                    {
                        selectedLabyrinthChoice = option.choice;
                        inLabyrinth = true;
                        inRaid = false;
                        inTower = false;
                        inElementalGate = false; inEventAdventure = false; inGuildBoss = false; inTournament = false;
                        stageId = option.stageId;
                        activeTrialMode = TrialMode.Normal;
                        page = Page.Formation;
                        notice = "Select six heroes for this chamber.";
                    }
                    else
                    {
                        notice = progress.ChooseLabyrinthEvent(option.choice) ?
                            "Room complete. Your reward was saved." :
                            "Could not complete this room.";
                    }
                }
            }
            GUI.Label(new Rect(32, 842, 477, 42),
                "Guardian rooms: 4, 8, 12 • Finish all rooms for bonus rewards.",
                tiny);
        }

        private void Raid()
        {
            int attemptsLeft = progress.RaidAttemptsRemaining();
            StageDefinition today = ChroniclesRaids.StageForDay(DateTime.UtcNow);
            OrnateGuiSkin.TitlePlate(new Rect(30, 219, 480, 54), "DAILY WORLD BOSS", 24);
            GUI.Label(new Rect(28, 280, 480, 83),
                "90-second solo damage challenge. Boss rotates daily (UTC).",
                pageDesc);
            GUI.Box(new Rect(34, 388, 472, 188), "");
            GUI.Label(new Rect(44, 400, 450, 162),
                "TODAY'S GUARDIAN\n" + today.name +
                "\nATTEMPTS: " + attemptsLeft + "/" + ChroniclesRaids.AttemptsPerDay +
                "\nBEST DAMAGE: " + progress.Data.raidBestDamage +
                "\nRANK: " + ChroniclesRaids.Rank(progress.Data.raidBestDamage),
                subtitle);
            GUI.Box(new Rect(34, 597, 471, 143), "");
            GUI.Label(new Rect(43, 606, 452, 124),
                "MILESTONE RANKS\n3000: BOSS HUNTER  •  15000: MASTER\n" +
                "40000: LEGENDARY  •  80000: WORLD BREAKER\n" +
                "150000: MYTHIC SLAYER", tinyCenter);
            GUI.enabled = progress.Data.lastClearedStage >= 10 &&
                attemptsLeft > 0 && string.IsNullOrEmpty(progress.Data.raidPendingRunId);
            if (GUI.Button(new Rect(40, 787, 460, 73), "CHALLENGE TODAY'S BOSS", bigButton))
            {
                inRaid = true;
                inTower = false;
                inElementalGate = false; inEventAdventure = false; inGuildBoss = false; inTournament = false;
                activeTrialMode = TrialMode.Normal;
                stageId = today.id;
                notice = "Attempt is consumed when the battle starts.";
                page = Page.Formation;
            }
            GUI.enabled = true;
            if (progress.Data.lastClearedStage < 10)
                GUI.Label(new Rect(38, 864, 464, 37),
                    "Clear the first story chapter to unlock World Boss.", tiny);
        }

        private void Trials()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 191, 480, 54), "ELITE & DARK TRIALS", 24);
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
            OrnateGuiSkin.TitlePlate(new Rect(30, 189, 480, 54), "FACTION TOWERS", 24);
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
            inElementalGate = false; inEventAdventure = false; inGuildBoss = false; inTournament = false;
            activeTrialMode = TrialMode.Normal;
            stageId = stage.id;
            notice = "Select six heroes, including two from " + origin + ".";
            page = Page.Formation;
        }

        private void Chapters()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 177, 480, 54), "STORY CAMPAIGN", 24);
            GUI.Label(new Rect(27, 236, 481, 32), "CAMPAIGN MILESTONE CHESTS", tiny);
            for (int i = 0; i < ChroniclesMilestones.All.Length; i++)
            {
                CampaignMilestoneReward reward = ChroniclesMilestones.All[i];
                bool claimed = progress.Data.campaignMilestonesClaimed.Contains(reward.stage);
                bool unlocked = progress.CanClaimCampaignMilestone(reward.stage);
                Color previous = GUI.backgroundColor;
                if (unlocked) GUI.backgroundColor = new Color(.91f, .77f, .45f);
                GUI.enabled = unlocked;
                if (GUI.Button(new Rect(23 + i * 166, 275, 158, 57),
                    "STAGE " + reward.stage + " CHEST\n" +
                    (claimed ? "CLAIMED" : unlocked ? "CLAIM NOW" : "LOCKED")))
                    notice = progress.ClaimCampaignMilestone(reward.stage) ?
                        "Milestone collected: " + reward.Summary : "Claim unavailable.";
                GUI.enabled = true;
                GUI.backgroundColor = previous;
            }
            chapterScroll = GUI.BeginScrollView(new Rect(20, 348, 505, 538), chapterScroll,
                new Rect(0, 0, 475, 6 * 498));
            for (int c = 0; c < WorldsCatalog.Chapters.Length; c++)
            {
                ChapterDefinition chapter = WorldsCatalog.Chapters[c];
                float y = c * 498;
                GUI.Box(new Rect(5, y, 465, 482), "");
                Texture2D chapterArt = ChroniclesStageArt.Background(chapter.origin);
                if (chapterArt != null)
                {
                    GUI.DrawTexture(new Rect(10, y + 6, 455, 118), chapterArt,
                        ScaleMode.ScaleAndCrop);
                    Texture2D chapterVignette = OrnateGuiSkin.Vignette();
                    if (chapterVignette != null)
                        GUI.DrawTexture(new Rect(10, y + 6, 455, 118), chapterVignette);
                }
                GUI.Label(new Rect(15, y + 10, 430, 44), chapter.number + ". " + chapter.title, rowText);
                GUI.Label(new Rect(20, y + 52, 425, 60), chapter.summary, tiny);
                float prevNodeX = -1f, prevNodeY = -1f;
                for (int j = 0; j < 10; j++)
                {
                    int id = c * 10 + j + 1;
                    bool unlocked = id <= progress.Data.lastClearedStage + 1;
                    bool cleared = id <= progress.Data.lastClearedStage;
                    int col = j % 5, row = j / 5;
                    int pathCol = row == 0 ? col : 4 - col; // serpentine path
                    bool bossNode = j == 9;
                    float nodeSize = bossNode ? 88f : 74f;
                    float nx = 28 + pathCol * 86 + (row == 1 ? 20f : 0f);
                    float ny = y + 138 + row * 118 + (pathCol % 2 == 0 ? 0f : 12f);
                    if (prevNodeX >= 0f)
                    {
                        // gold connector along the path
                        float cx = Mathf.Min(prevNodeX, nx) + 30f;
                        float cy = Mathf.Min(prevNodeY, ny) + 34f;
                        float cw = Mathf.Abs(nx - prevNodeX) + 14f;
                        float ch = Mathf.Abs(ny - prevNodeY) + 8f;
                        Color oldGui = GUI.color;
                        GUI.color = new Color(.78f, .62f, .28f,
                            id <= progress.Data.lastClearedStage + 1 ? .8f : .25f);
                        GUI.DrawTexture(new Rect(cx, cy + ch * .5f - 2f, cw, 4f),
                            Texture2D.whiteTexture);
                        if (ch > 10f)
                            GUI.DrawTexture(new Rect(cx + cw * .5f - 2f, cy, 4f, ch),
                                Texture2D.whiteTexture);
                        GUI.color = oldGui;
                    }
                    prevNodeX = nx; prevNodeY = ny;
                    if (id == progress.Data.lastClearedStage + 1)
                    {
                        // Current stage: pulsing gold halo so the next fight is obvious.
                        float pulse = .65f + .35f * Mathf.Sin(Time.realtimeSinceStartup * 3.2f);
                        Color haloOld = GUI.color;
                        GUI.color = new Color(1f, 1f, 1f, pulse);
                        GUI.DrawTexture(new Rect(nx - 20f, ny - 20f, nodeSize + 40f, nodeSize + 40f),
                            OrnateGuiSkin.Glow("stage_halo", new Color(1f, .82f, .35f, .75f)));
                        GUI.color = haloOld;
                    }
                    GUI.enabled = unlocked;
                    Color prevBg = GUI.backgroundColor;
                    if (bossNode) GUI.backgroundColor = new Color(.75f, .25f, .2f);
                    else if (cleared) GUI.backgroundColor = new Color(.45f, .65f, .35f);
                    if (GUI.Button(new Rect(nx, ny, nodeSize, nodeSize),
                        (bossNode ? "BOSS\n" : "") + id + (cleared ? "\n\u2605" : "")))
                    {
                        stageId = id;
                        activeTrialMode = TrialMode.Normal;
                        storyStartedAt = Time.unscaledTime;
                        page = Page.StageStory;
                        if (audio != null) audio.SetState(AudioScene.Story, chapter.number);
                        notice = "";
                    }
                    GUI.backgroundColor = prevBg;
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
            {
                GUI.DrawTexture(new Rect(0, 96, 540, 560), background, ScaleMode.ScaleAndCrop);
                Texture2D vignette = OrnateGuiSkin.Vignette();
                if (vignette != null)
                    GUI.DrawTexture(new Rect(0, 96, 540, 560), vignette);
            }
            GUI.Label(new Rect(22, 120, 496, 40),
                activeTrialMode == TrialMode.Dark ? "DARK ENCOUNTER" :
                activeTrialMode == TrialMode.Elite ? "ELITE ENCOUNTER" :
                stage.boss ? "BOSS ENCOUNTER" : "STORY ENCOUNTER", heading);
            GUI.Label(new Rect(25, 158, 490, 52), stage.name, heading);

            GUI.Box(new Rect(20, 560, 500, 208), "", OrnateGuiSkin.RowBox());
            string speaker = moment == null ? "Narrator" : moment.speaker;
            string line = moment == null ? stage.narrative : moment.line;
            Texture2D portrait = ChroniclesStageArt.Portrait(speaker);
            float textWidth = portrait == null ? 445f : 288f;
            GUI.Label(new Rect(45, 572, textWidth, 37), speaker, subtitle);
            int visibleCharacters = Mathf.Clamp(
                Mathf.FloorToInt((Time.unscaledTime - storyStartedAt) * 32f), 0, line.Length);
            GUI.Label(new Rect(45, 612, textWidth, 148),
                line.Substring(0, visibleCharacters), subtitle);

            if (portrait != null)
            {
                float slide = 1f - Mathf.Clamp01((Time.unscaledTime - storyStartedAt) * 2f);
                GUI.DrawTexture(new Rect(355f + slide * 70f, 572f, 141f, 180f),
                    portrait, ScaleMode.ScaleToFit);
            }
            if (GUI.Button(new Rect(180, 774, 180, 32), "SHOW ALL DIALOGUE"))
                storyStartedAt = Time.unscaledTime - 30f;
            GUI.Label(new Rect(35, 808, 475, 46),
                "Recommended power: " +
                Mathf.RoundToInt(stage.recommendedPower *
                    ChroniclesTrials.EnemyHealthMultiplier(activeTrialMode)) +
                "\nFirst clear: " + (activeTrialMode == TrialMode.Normal ?
                    stage.gold + " Gold, " + stage.gems + " Gems" :
                    ChroniclesTrials.FirstClearReward(stageId, activeTrialMode).Description),
                subtitle);
            if (GUI.Button(new Rect(42, 858, 458, 56), "PREPARE TEAM", bigButton))
            {
                notice = "";
                page = Page.Formation;
            }
            if (GUI.Button(new Rect(42, 920, 458, 36),
                activeTrialMode == TrialMode.Normal ?
                    "BACK TO CHAPTERS" : "BACK TO TRIALS"))
                page = activeTrialMode == TrialMode.Normal ?
                    Page.Chapters : Page.Trials;
        }

        private static GUIStyle ElementChipStyle(bool off)
        {
            GUIStyle st = new GUIStyle(OrnateGuiSkin.ButtonStyle(off));
            st.wordWrap = false;
            st.fontSize = 11;
            st.clipping = TextClipping.Overflow;
            return st;
        }

        private void Heroes()
        {
            OrnateGuiSkin.Title(new Rect(110, 179, 320, 44),
                "HERO COLLECTION", 22);
            GUI.Label(new Rect(30, 226, 480, 36),
                progress.Data.heroes.Count + " / " + WorldsCatalog.Heroes.Length +
                " owned  •  COLLECTION +" +
                (ChroniclesGrowth.CollectionBonus(progress.Data) * 100f).ToString("0.#") +
                "% ATK/HP", subtitle);
            string[] tabs = { "ALL", "R", "SR", "SSR", "UR", "EX" };
            GUIStyle chipStyleFlat = new GUIStyle(OrnateGuiSkin.ButtonStyle());
            chipStyleFlat.wordWrap = false; chipStyleFlat.fontSize = 12;
            for (int i = 0; i < tabs.Length; i++)
            {
                float x = 22f + i * 83f;
                if (i == 5)
                {
                    GUI.Box(new Rect(x, 268, 77, 45), "", OrnateGuiSkin.CellBox());
                    GUI.Label(new Rect(x, 270, 77, 39), "EX\nLOCKED", tiny);
                    continue;
                }
                if (GUI.Button(new Rect(x, 268, 77, 45), tabs[i],
                    OrnateGuiSkin.ButtonStyle(galleryTierFilter != i - 1)))
                {
                    galleryTierFilter = i - 1;
                    rosterScroll = Vector2.zero;
                }
            }

            string[] elementTabs = { "ALL", "FIRE", "WATER", "NATURE", "WIND", "LIGHT", "DARK" };
            for (int i = 0; i < elementTabs.Length; i++)
                if (GUI.Button(new Rect(22f + i * 71f, 319f, 67f, 40f),
                    elementTabs[i],
                    ElementChipStyle(galleryElementFilter != i - 1)))
                {
                    galleryElementFilter = i - 1;
                    rosterScroll = Vector2.zero;
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
                // Rarity aura behind the card, like the reference card
                // walls: stronger glow for higher tiers.
                float aura = hero.rarity == Rarity.UR ? .55f :
                    hero.rarity == Rarity.EX ? .5f :
                    hero.rarity == Rarity.SSR ? .42f :
                    hero.rarity == Rarity.SR ? .3f : .22f;
                Color frameCol = RarityFrameColor(hero.rarity);
                GUI.DrawTexture(new Rect(x - 14f, y - 14f, 178f, 240f),
                    OrnateGuiSkin.Glow("card_aura_" + hero.rarity,
                        new Color(frameCol.r, frameCol.g, frameCol.b, aura)));
                OrnateGuiSkin.Panel(new Rect(x, y, 150, 212),
                    "card_" + hero.rarity, frameCol,
                    new Color(.08f, .06f, .12f, .94f));
                ChroniclesHeroPortrait.Draw(hero.id,
                    new Rect(x + 4, y + 4, 142, 164), false);
                OwnedHero ownedCard = progress.FindOwnedHero(hero.id);
                if (ownedCard != null)
                {
                    GUIStyle lvStyle = new GUIStyle(tiny) {
                        alignment = TextAnchor.MiddleCenter, fontSize = 11 };
                    lvStyle.normal.textColor = OrnateGuiSkin.GoldBright;
                    GUI.DrawTexture(new Rect(x + 98, y + 6, 48, 21),
                        OrnateGuiSkin.Glow("card_lv_bg",
                            new Color(0f, 0f, 0f, .8f)));
                    GUI.Label(new Rect(x + 98, y + 7, 48, 19),
                        "LV " + ownedCard.level, lvStyle);
                    for (int s = 0; s < Mathf.Min(ownedCard.stars, 6); s++)
                        GUI.DrawTexture(new Rect(x + 9 + s * 17, y + 146, 14, 14),
                            OrnateGuiSkin.Glow("card_star",
                                new Color(1f, .85f, .4f, .95f)));
                }
                Texture2D namePlate = OrnateGuiSkin.Art("bar_plate");
                Color oldColor = GUI.color;
                if (namePlate != null)
                {
                    GUI.color = new Color(.16f, .14f, .30f, .96f);
                    GUI.DrawTexture(new Rect(x + 4, y + 156, 142, 53),
                        namePlate, ScaleMode.StretchToFill);
                }
                else
                {
                    GUI.color = new Color(.035f, .032f, .074f, .96f);
                    GUI.DrawTexture(new Rect(x + 4, y + 156, 142, 53),
                        Texture2D.whiteTexture);
                }
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
            OrnateGuiSkin.Title(new Rect(28, 232, 485, 52),
                hero.name.ToUpperInvariant(), 27);
            GUI.Label(new Rect(26, 292, 487, 39),
                hero.rarity + " • " + hero.element + " • " +
                hero.origin + " • " + hero.role, subtitle);

            // Both static and animated portraits are available for EVERY hero.
            GUI.DrawTexture(new Rect(0, 320, 300, 340),
                OrnateGuiSkin.Glow("detail_glow_" + hero.rarity,
                    new Color(RarityFrameColor(hero.rarity).r,
                        RarityFrameColor(hero.rarity).g,
                        RarityFrameColor(hero.rarity).b, .42f)));
            ChroniclesHeroPortrait.Draw(hero.id,
                new Rect(30, 350, 228, 280),
                showHeroMotion && !progress.Data.reducePortraitMotion);
            GUI.enabled = !progress.Data.reducePortraitMotion;
            if (GUI.Button(new Rect(30, 636, 109, 37),
                showHeroMotion ? "● MOTION" : "MOTION",
                OrnateGuiSkin.ButtonStyle(!showHeroMotion)))
                showHeroMotion = true;
            GUI.enabled = true;
            if (GUI.Button(new Rect(146, 636, 112, 37),
                !showHeroMotion ? "● STATIC" : "STATIC",
                OrnateGuiSkin.ButtonStyle(showHeroMotion)))
                showHeroMotion = false;

            OrnateGuiSkin.Panel(new Rect(268, 350, 243, 323), "detail_info",
                RarityFrameColor(hero.rarity), new Color(.08f, .06f, .12f, .94f));
            if (GUI.Button(new Rect(276, 360, 109, 36), "STORY",
                OrnateGuiSkin.ButtonStyle()))
                showHeroSkills = false;
            if (GUI.Button(new Rect(392, 360, 110, 36), "SKILLS",
                OrnateGuiSkin.ButtonStyle()))
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
                        "UR heroes can be summoned in the Astral Covenant banner." :
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
            if (GUI.Button(new Rect(31, 735, 478, 49), "UPGRADE HERO",
                OrnateGuiSkin.ButtonStyle()))
                notice = progress.UpgradeHero(hero.id) ?
                    "Hero upgraded and saved." : "Gold or level cap reached.";
            if (GUI.Button(new Rect(31, 789, 232, 49), "EQUIPMENT",
                OrnateGuiSkin.ButtonStyle(false)))
                page = Page.Gear;
            if (GUI.Button(new Rect(275, 789, 234, 49), "SKILL TRAINING",
                OrnateGuiSkin.ButtonStyle(false)))
                page = Page.Training;
            if (GUI.Button(new Rect(31, 843, 232, 46), "ASCEND",
                OrnateGuiSkin.ButtonStyle()))
                notice = progress.AscendHero(hero.id) ?
                    "Star rank increased!" : "Not enough fragments.";
            GUI.enabled = true;
            if (GUI.Button(new Rect(275, 843, 234, 46), "SKINS",
                OrnateGuiSkin.ButtonStyle(false)))
                page = Page.Skins;
            if (GUI.Button(new Rect(31, 897, 478, 39), "BACK TO COLLECTION",
                OrnateGuiSkin.ButtonStyle(false)))
                page = Page.Heroes;
        }

        private void Skins()
        {
            HeroDefinition hero = WorldsCatalog.FindHero(selectedHero);
            if (hero == null) { page = Page.Heroes; return; }
            OwnedHero owned = progress.FindOwnedHero(hero.id);
            OrnateGuiSkin.TitlePlate(new Rect(30, 245, 480, 54), hero.name.ToUpperInvariant() + " SKINS", 24);
            GUI.Label(new Rect(30, 315, 480, 53),
                "Unlock a costume before equipping it.", pageDesc);
            IList<HeroSkin> skins = HeroSkinCatalog.For(hero.id);
            for (int i = 0; i < skins.Count; i++)
            {
                HeroSkin skin = skins[i];
                bool unlocked = progress.OwnsSkin(hero.id, skin.id);
                bool equipped = owned != null && owned.skin == skin.id;
                float y = 391 + i * 205;
                GUI.Box(new Rect(30, y, 480, 194), "");
                // Preview: the skin's own art when authored, otherwise the base portrait.
                ChroniclesHeroPortrait.DrawSkin(hero.id, skin.id, new Rect(38, y + 8, 88, 132), false);
                if (skin.id != "default" && !ChroniclesPortraitAssets.HasSkinArt(hero.id, skin.id))
                    GUI.Label(new Rect(38, y + 112, 88, 26), "ART PENDING", tiny);
                GUI.Label(new Rect(136, y + 12, 363, 48), skin.displayName, heading);
                GUI.Label(new Rect(137, y + 62, 361, 52), skin.visualTheme, tiny);
                GUI.Label(new Rect(137, y + 111, 360, 27),
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
            OrnateGuiSkin.TitlePlate(new Rect(30, 236, 480, 54), hero.name.ToUpperInvariant() + " EQUIPMENT", 24);
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
                GUI.Box(new Rect(4, y, 469, 104), "", OrnateGuiSkin.RowBox());
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
            OrnateGuiSkin.TitlePlate(new Rect(30, 238, 480, 54), "FORGE WORKSHOP", 24);
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
                GUI.Box(new Rect(3, y, 469, 71), "", OrnateGuiSkin.RowBox());
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
            OrnateGuiSkin.TitlePlate(new Rect(30, 244, 480, 54), "HERO TRAINING", 24);
            GUI.Label(new Rect(22, 303, 495, 56),
                "Resonance LV " + progress.ResonanceLevel() +
                " • Effective LV " + ChroniclesGrowth.EffectiveLevel(progress.Data, hero), subtitle);
            string[] skillTypes = { "active", "ultimate", "passive" };
            int[] levels = { hero.activeSkillLevel, hero.ultimateSkillLevel, hero.passiveSkillLevel };
            for (int i = 0; i < skillTypes.Length; i++)
            {
                float y = 367 + i * 89;
                GUI.Box(new Rect(30, y, 480, 78), "", OrnateGuiSkin.RowBox());
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
            OrnateGuiSkin.TitlePlate(new Rect(30, 245, 480, 54), "SIGNATURE WEAPON", 24);
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
            OrnateGuiSkin.TitlePlate(new Rect(30, 244, 480, 54), "RELIC VAULT", 24);
            GUI.Label(new Rect(35, 305, 471, 59),
                "Earn an original artifact by defeating each chapter guardian." +
                "\nRelic dust: " + progress.Data.relicDust, pageDesc);
            gearScroll = GUI.BeginScrollView(new Rect(18, 376, 502, 448), gearScroll,
                new Rect(0, 0, 477, ChroniclesRelics.All.Length * 102));
            for (int i = 0; i < ChroniclesRelics.All.Length; i++)
            {
                WorldRelic relic = ChroniclesRelics.All[i];
                RelicRank owned = progress.Data.relics.Find(x => x.id == relic.id);
                bool equipped = progress.Data.equippedRelicId == relic.id;
                float y = 102f * i;
                GUI.Box(new Rect(5, y, 468, 97), "", OrnateGuiSkin.RowBox());
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
            OrnateGuiSkin.TitlePlate(new Rect(30, 244, 480, 54), hero.name.ToUpperInvariant() + " RUNES", 24);
            GUI.Label(new Rect(35, 304, 465, 66),
                "One rune of each type per hero. Earn one every five first-clear stages.", pageDesc);
            runeScroll = GUI.BeginScrollView(new Rect(18, 376, 504, 442), runeScroll,
                new Rect(0, 0, 478, Mathf.Max(445, progress.Data.runes.Count * 101)));
            for (int i = 0; i < progress.Data.runes.Count; i++)
            {
                RuneInstance rune = progress.Data.runes[i];
                float y = i * 101f;
                GUI.Box(new Rect(5, y, 469, 95), "", OrnateGuiSkin.RowBox());
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

        // Home event splash: shown once per day, big event art, GO button,
        // tap anywhere else to close, "don't display again today" toggle.
        private void EventSplash()
        {
            string dayKey = "chronicles_splash_day";
            string today = DateTime.UtcNow.ToString("yyyyMMdd");
            if (PlayerPrefs.GetString(dayKey, "") == today) { eventSplashDone = true; return; }
            Texture2D art = OrnateGuiSkin.Art("event_launch_gold_rush");
            GUI.color = new Color(0f, 0f, 0f, .72f);
            GUI.DrawTexture(new Rect(0, 0, 540, 960), Texture2D.whiteTexture);
            GUI.color = Color.white;
            Rect card = new Rect(50, 170, 440, 560);
            OrnateGuiSkin.Panel(card, "event_splash_panel", OrnateGuiSkin.GoldBright,
                new Color(.07f, .05f, .11f, .97f));
            if (art != null)
                GUI.DrawTexture(new Rect(card.x + 12, card.y + 12, card.width - 24, 380), art, ScaleMode.ScaleAndCrop);
            OrnateGuiSkin.TitlePlate(new Rect(card.x + 30, card.y + 330, card.width - 60, 48), "LAUNCH GOLD RUSH", 20);
            GUI.Label(new Rect(card.x + 20, card.y + 392, card.width - 40, 24),
                "Limited event rewards, ends soon", new GUIStyle(tiny) { alignment = TextAnchor.MiddleCenter });
            if (GUI.Button(new Rect(card.x + 100, card.y + 424, 240, 56), "GO", OrnateGuiSkin.ButtonStyle()))
            { eventSplashDone = true; page = Page.Events; }
            if (GUI.Button(new Rect(card.x + 100, card.y + 492, 240, 36), "DON'T SHOW AGAIN TODAY",
                OrnateGuiSkin.ButtonStyle(false)))
            { PlayerPrefs.SetString(dayKey, today); PlayerPrefs.Save(); eventSplashDone = true; }
            GUI.Label(new Rect(0, 740, 540, 24), "Tap any blank area to close",
                new GUIStyle(tiny) { alignment = TextAnchor.MiddleCenter });
            // Last so GO / don't-show buttons (drawn first) win their own clicks.
            if (GUI.Button(new Rect(0, 0, 540, 960), "", GUIStyle.none))
                eventSplashDone = true;
        }

        private void Formation()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 182, 480, 54), "TEAM FORMATION", 24);
            GUI.Label(new Rect(30, 242, 480, 62),
                inArenaTraining ? "ARENA TRAINING - NPC MATCH" +
                    "\nThe ladder here is local practice, not online PvP." :
                inLabyrinth ? "WEEKLY LABYRINTH - ROOM " +
                    progress.LabyrinthState().nextRoom +
                    "\nChoose six heroes and carry your blessings into battle." :
                inTower ? "TOWER: " + ChroniclesTowers.DisplayName(selectedTowerOrigin) +
                    "\nPick 6 heroes, including at least 2 from this mythology." :
                    "Pick six heroes. Order sets your front and back rows.", pageDesc);
            // Selected team as six big cards - the way the genre presents a squad.
            for (int slot = 0; slot < PlayerProgressService.TeamSize; slot++)
            {
                float sx = 26f + slot * 81f;
                Rect slotRect = new Rect(sx, 310f, 76f, 176f);
                if (slot < progress.Data.selectedTeam.Count)
                {
                    string pickedId = progress.Data.selectedTeam[slot];
                    HeroDefinition pickedHero = WorldsCatalog.FindHero(pickedId);
                    OwnedHero pickedOwned = progress.FindOwnedHero(pickedId);
                    if (pickedHero != null)
                    {
                        OrnateGuiSkin.Panel(slotRect, "card_" + pickedHero.rarity,
                            RarityFrameColor(pickedHero.rarity),
                            new Color(.08f, .06f, .12f, .94f));
                        ChroniclesHeroPortrait.Draw(pickedHero.id,
                            new Rect(slotRect.x + 4, slotRect.y + 4, 68, 118), false);
                        OrnateGuiSkin.PulseGlow(slotRect,
                            new Color(1f, .82f, .42f, .18f), 1.6f + slot * .1f);
                        GUIStyle slotTxt = new GUIStyle(tiny) { alignment = TextAnchor.MiddleCenter, fontSize = 10, wordWrap = false, clipping = TextClipping.Overflow };
                        GUI.Label(new Rect(slotRect.x + 2, slotRect.y + 124, 72, 16),
                            "SLOT " + (slot + 1), slotTxt);
                        GUI.Label(new Rect(slotRect.x + 2, slotRect.y + 140, 72, 16),
                            "Lv " + (pickedOwned != null ? pickedOwned.level : 1) +
                            " " + pickedHero.role, slotTxt);
                        if (GUI.Button(slotRect, "", GUIStyle.none))
                        {
                            List<string> temp = new List<string>(progress.Data.selectedTeam);
                            temp.Remove(pickedId);
                            progress.Data.selectedTeam = temp;
                            notice = temp.Count + "/6 heroes selected. Complete your team to save.";
                        }
                        continue;
                    }
                }
                // Empty slot: quiet framed dash with a plus.
                OrnateGuiSkin.Panel(slotRect, "card_empty",
                    new Color(.45f, .42f, .55f), new Color(.05f, .045f, .09f, .9f));
                GUIStyle plus = new GUIStyle(heading) {
                    alignment = TextAnchor.MiddleCenter, fontSize = 34 };
                plus.normal.textColor = new Color(.6f, .56f, .7f, .8f);
                GUI.Label(slotRect, "+", plus);
            }
            // Synergy chips under the squad: one per mythology on the team with
            // its live attack chipBonus (same thresholds TeamSynergy applies in battle).
            {
                var originCount = new Dictionary<Origin, int>();
                int teamN = progress.Data.selectedTeam.Count;
                for (int i = 0; i < teamN; i++)
                {
                    HeroDefinition th = WorldsCatalog.FindHero(progress.Data.selectedTeam[i]);
                    if (th == null) continue;
                    int cc; originCount.TryGetValue(th.origin, out cc);
                    originCount[th.origin] = cc + 1;
                }
                float chipX = 26f;
                GUIStyle chip = new GUIStyle(tiny) { alignment = TextAnchor.MiddleCenter, fontSize = 11, fontStyle = FontStyle.Bold, wordWrap = false, clipping = TextClipping.Overflow };
                foreach (var pair in originCount)
                {
                    int n = pair.Value;
                    string chipBonus = n >= teamN && teamN >= 5 ? "+25%" : n >= 5 ? "+20%" :
                        n >= 4 ? "+15%" : n >= 3 ? "+10%" : n >= 2 ? "+6%" : "";
                    Color chipCol = chipBonus == "" ? new Color(.5f, .46f, .6f) : OrnateGuiSkin.GoldBright;
                    Texture2D chipBg = OrnateGuiSkin.Frame("synergy_chip_" + (chipBonus == ""), chipCol,
                        new Color(.07f, .055f, .11f, .95f), 32, 3);
                    Rect cr = new Rect(chipX, 489f, 96f, 24f);
                    GUI.DrawTexture(cr, chipBg);
                    chip.normal.textColor = chipCol;
                    GUI.Label(cr, pair.Key.ToString().ToUpperInvariant() + " x" + n +
                        (chipBonus == "" ? "" : " " + chipBonus), chip);
                    chipX += 100f;
                    if (chipX > 440f) break;
                }
            }
            int rosterRows = Mathf.CeilToInt(progress.Data.heroes.Count / 3f);
            formationScroll = GUI.BeginScrollView(new Rect(20, 518, 500, 224), formationScroll,
                new Rect(0, 0, 473, rosterRows * 178 + 8));
            for (int i = 0; i < progress.Data.heroes.Count; i++)
            {
                OwnedHero owned = progress.Data.heroes[i];
                HeroDefinition hero = WorldsCatalog.FindHero(owned.id);
                if (hero == null) continue;
                int picked = progress.Data.selectedTeam.IndexOf(owned.id);
                int col = i % 3, row = i / 3;
                float x = 4f + col * 158f, y = 4f + row * 178f;
                if (y + 174f < formationScroll.y - 30f ||
                    y > formationScroll.y + 280f) continue;
                OrnateGuiSkin.Panel(new Rect(x, y, 150, 170),
                    "card_" + hero.rarity, RarityFrameColor(hero.rarity),
                    new Color(.08f, .06f, .12f, .94f));
                ChroniclesHeroPortrait.Draw(hero.id, new Rect(x + 4, y + 4, 142, 116), false);
                GUI.Label(new Rect(x + 7, y + 124, 136, 20),
                    hero.name + "  " + hero.rarity, tiny);
                GUI.Label(new Rect(x + 7, y + 144, 136, 20),
                    "Lv " + owned.level + " " + hero.role, tiny);
                if (picked >= 0)
                {
                    OrnateGuiSkin.PulseGlow(new Rect(x, y, 150, 170),
                        new Color(1f, .82f, .42f, .25f), 1.7f);
                    GUIStyle slotBadge = new GUIStyle(tiny) {
                        alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                    slotBadge.normal.textColor = new Color(1f, .9f, .6f);
                    GUI.color = new Color(0f, 0f, 0f, .6f);
                    GUI.DrawTexture(new Rect(x + 4, y + 4, 60, 22), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(x + 4, y + 4, 60, 22), "SLOT " + (picked + 1), slotBadge);
                }
                if (GUI.Button(new Rect(x, y, 150, 170), "", GUIStyle.none))
                {
                    List<string> temp = new List<string>(progress.Data.selectedTeam);
                    if (picked >= 0) temp.Remove(owned.id);
                    else if (temp.Count < PlayerProgressService.TeamSize) temp.Add(owned.id);
                    if (temp.Count == PlayerProgressService.TeamSize)
                        notice = progress.SetTeam(temp) ? "Team saved." : "Could not save team.";
                    else
                    {
                        // Temporarily allow incomplete in the formation editor.
                        progress.Data.selectedTeam = temp;
                        notice = temp.Count + "/6 heroes selected. Complete your team to save.";
                    }
                }
            }
            GUI.EndScrollView();
            GUI.Box(new Rect(26, 748, 488, 62), "", OrnateGuiSkin.RowBox());
            var synergyHeroes = new List<HeroDefinition>();
            foreach (string id in progress.Data.selectedTeam)
            {
                HeroDefinition hero = WorldsCatalog.FindHero(id);
                if (hero != null) synergyHeroes.Add(hero);
            }
            TeamSynergyBonus bonus = TeamSynergy.Calculate(synergyHeroes);
            ElementalTeamBonus elemental = HeroElements.Calculate(synergyHeroes);
            GUI.Label(new Rect(34, 752, 472, 56),
                "Selected " + progress.Data.selectedTeam.Count + "/6  |  " +
                elemental.description + "\nOrigin: " + bonus.description +
                "  •  Element ATK/HP +" +
                Mathf.RoundToInt((elemental.attackMultiplier - 1f) * 100f) +
                "%  •  START ENERGY +" + elemental.startingEnergy, subtitle);
            GUI.enabled = progress.Data.selectedTeam.Count == PlayerProgressService.TeamSize;
            if (GUI.Button(new Rect(40, 818, 460, 64),
                inTower ? "START " + selectedTowerOrigin.ToString().ToUpperInvariant() +
                    " TOWER FLOOR " + selectedTowerFloor :
                "START " + (activeTrialMode == TrialMode.Normal ?
                    "" : activeTrialMode.ToString().ToUpperInvariant() + " ") +
                "STAGE " + stageId, bigButton))
                BeginBattle();
            GUI.enabled = true;
        }

        // Rarity-colored frame accents, in the genre's gold/red conventions.
        public static Color RarityFrameColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.UR: return new Color(1f, .85f, .5f);
                case Rarity.EX: return new Color(.9f, .55f, .9f);
                case Rarity.SSR: return new Color(.86f, .66f, .28f);
                case Rarity.SR: return new Color(.72f, .78f, .9f);
                default: return new Color(.68f, .54f, .36f);
            }
        }

        private void Summon()
        {
            bool astral = activeSummonBanner == SummonBannerType.AstralCovenant;
            GUIStyle goldCenter = new GUIStyle(tiny) {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15 };
            goldCenter.normal.textColor = OrnateGuiSkin.GoldBright;
            GUIStyle plainCenter = new GUIStyle(tiny) {
                alignment = TextAnchor.MiddleCenter };

            // The temple altar scene is drawn as the page background (OnGUI);
            // keep only a soft bottom shade so text stays readable over it.
            if (lastSummonBatch.Count == 0)
                GUI.DrawTexture(new Rect(20, 520, 500, 190),
                    OrnateGuiSkin.Backdrop("summon_shade",
                        new Color(0f, 0f, 0f, 0f), new Color(0f, 0f, 0f, .85f)));

            OrnateGuiSkin.TitlePlate(new Rect(30, 226, 480, 48),
                ChroniclesSummonRules.DisplayName(activeSummonBanner)
                    .ToUpperInvariant(), 24);

            if (GUI.Button(new Rect(58, 276, 200, 44), "STANDARD",
                OrnateGuiSkin.ButtonStyle()))
                SelectBanner(SummonBannerType.Standard);
            if (GUI.Button(new Rect(282, 276, 200, 44), "LIGHT / DARK UR",
                OrnateGuiSkin.ButtonStyle()))
                SelectBanner(SummonBannerType.AstralCovenant);

            // Astral wishlist: tap to cycle the featured UR, hold-none via NONE.
            if (astral && lastSummonBatch.Count == 0)
            {
                string wish = progress.Data.wishlistHeroId;
                if (GUI.Button(new Rect(58, 330, 312, 40),
                    ChroniclesSummonRules.WishlistLabel(wish),
                    OrnateGuiSkin.ButtonStyle()))
                {
                    progress.Data.wishlistHeroId =
                        ChroniclesSummonRules.NextWishlistPick(wish);
                    progress.Save();
                }
                GUI.enabled = !string.IsNullOrEmpty(wish);
                if (GUI.Button(new Rect(382, 330, 100, 40), "CLEAR",
                    OrnateGuiSkin.ButtonStyle(false)))
                {
                    progress.Data.wishlistHeroId = "";
                    progress.Save();
                }
                GUI.enabled = true;
            }
            GUI.Label(new Rect(40, 662, 460, 26),
                ChroniclesSummonRules.Rates(activeSummonBanner), plainCenter);
            if (astral)
                GUI.Label(new Rect(40, 688, 460, 22),
                    string.IsNullOrEmpty(progress.Data.wishlistHeroId) ?
                        "SET A WISHLIST PICK FOR 50% FEATURED RATE-UP" :
                        "WISHLIST ACTIVE  -  HALF OF ALL UR HITS",
                    plainCenter);

            if (lastSummonBatch.Count == 0)
            {
                // Featured splash: the wishlist pick (or a cycled UR) shown big
                // with a rate-up plate and arrows, like a banner screen.
                var featured = new List<HeroDefinition>();
                for (int i = 0; i < WorldsCatalog.Heroes.Length; i++)
                    if (WorldsCatalog.Heroes[i].rarity == Rarity.UR)
                        featured.Add(WorldsCatalog.Heroes[i]);
                if (featured.Count > 0)
                {
                    HeroDefinition feat = featured[((summonFeatureIdx % featured.Count) + featured.Count) % featured.Count];
                    if (astral && !string.IsNullOrEmpty(progress.Data.wishlistHeroId))
                    {
                        HeroDefinition wished = WorldsCatalog.FindHero(progress.Data.wishlistHeroId);
                        if (wished != null) feat = wished;
                    }
                    Color fcol = RarityFrameColor(feat.rarity);
                    GUI.DrawTexture(new Rect(70, 360, 400, 290),
                        OrnateGuiSkin.Glow("feat_glow_" + feat.rarity,
                            new Color(fcol.r, fcol.g, fcol.b, .45f)));
                    ChroniclesHeroPortrait.Draw(feat.id, new Rect(165, 378, 210, 238),
                        !progress.Data.reducePortraitMotion);
                    OrnateGuiSkin.TitlePlate(new Rect(110, 612, 320, 36),
                        "RATE UP  -  " + feat.name.ToUpperInvariant(), 15);
                    if (GUI.Button(new Rect(30, 470, 54, 60), "<", OrnateGuiSkin.ButtonStyle(false)))
                        summonFeatureIdx--;
                    if (GUI.Button(new Rect(456, 470, 54, 60), ">", OrnateGuiSkin.ButtonStyle(false)))
                        summonFeatureIdx++;
                }
            }
            if (lastSummonBatch.Count == 1)
            {
                string id = lastSummonBatch[0];
                HeroDefinition hero = WorldsCatalog.FindHero(id);
                if (hero != null)
                {
                    float rt = progress.Data.reducePortraitMotion ? 99f :
                        Time.realtimeSinceStartup - summonRevealStart;
                    float pop = EaseOutBack(Mathf.Clamp01(rt / .45f));
                    float fade = Mathf.Clamp01(rt / .3f);
                    Color rarityCol = RarityFrameColor(hero.rarity);
                    if (rt < .8f)
                    {
                        float flash = 1f - Mathf.Clamp01(rt / .8f);
                        Color preFlash = GUI.color;
                        GUI.color = new Color(1f, 1f, 1f, flash);
                        GUI.DrawTexture(new Rect(40, 240, 460, 460),
                            OrnateGuiSkin.Glow("summon_flash_" + hero.rarity,
                                new Color(rarityCol.r, rarityCol.g, rarityCol.b, .55f)));
                        GUI.color = preFlash;
                    }
                    Color saved = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, fade);
                    GUI.DrawTexture(ScaledAroundCenter(new Rect(120, 310, 300, 330), pop),
                        OrnateGuiSkin.Glow("summon_result_glow",
                            new Color(.95f, .8f, .4f, .5f)));
                    OrnateGuiSkin.Panel(ScaledAroundCenter(new Rect(150, 320, 240, 300), pop),
                        "summon_result", OrnateGuiSkin.GoldBright,
                        new Color(.08f, .06f, .12f, .95f));
                    ChroniclesHeroPortrait.Draw(id,
                        ScaledAroundCenter(new Rect(162, 332, 216, 246), pop),
                        !progress.Data.reducePortraitMotion);
                    OrnateGuiSkin.Title(new Rect(30, 574, 480, 36),
                        hero.name.ToUpperInvariant(), 24);
                    GUI.Label(new Rect(30, 608, 480, 28),
                        hero.rarity + "  -  " + hero.element, goldCenter);
                    GUI.color = saved;
                }
            }
            else if (lastSummonBatch.Count > 1)
            {
                OrnateGuiSkin.Panel(new Rect(30, 320, 480, 330),
                    "summon_batch", OrnateGuiSkin.Gold,
                    new Color(.08f, .06f, .12f, .95f));
                for (int i = 0; i < lastSummonBatch.Count; i++)
                {
                    HeroDefinition hero = WorldsCatalog.FindHero(lastSummonBatch[i]);
                    if (hero == null) continue;
                    int column = i % 5, row = i / 5;
                    float x = 45 + column * 92, y = 334 + row * 156;
                    float rt = progress.Data.reducePortraitMotion ? 99f :
                        Time.realtimeSinceStartup - summonRevealStart - i * .16f;
                    if (rt <= 0f) continue;
                    float pop = EaseOutBack(Mathf.Clamp01(rt / .35f));
                    float fade = Mathf.Clamp01(rt / .25f);
                    if (rt < .6f)
                    {
                        float flash = 1f - Mathf.Clamp01(rt / .6f);
                        Color fc = RarityFrameColor(hero.rarity);
                        Color pre = GUI.color;
                        GUI.color = new Color(1f, 1f, 1f, flash);
                        GUI.DrawTexture(new Rect(x - 26, y - 26, 135, 150),
                            OrnateGuiSkin.Glow("batch_flash_" + hero.rarity,
                                new Color(fc.r, fc.g, fc.b, .6f)));
                        GUI.color = pre;
                    }
                    Color saved = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, fade);
                    Color fc2 = RarityFrameColor(hero.rarity);
                    GUI.DrawTexture(ScaledAroundCenter(
                        new Rect(x - 8, y - 8, 99, 114), pop),
                        OrnateGuiSkin.Glow("batch_aura_" + hero.rarity,
                            new Color(fc2.r, fc2.g, fc2.b, .38f)));
                    ChroniclesHeroPortrait.Draw(hero.id,
                        ScaledAroundCenter(new Rect(x, y, 83, 98), pop), false);
                    GUI.Label(new Rect(x - 4, y + 100, 91, 48),
                        hero.name + "\n" + hero.rarity, plainCenter);
                    GUI.color = saved;
                }
            }

            GUI.DrawTexture(new Rect(120, 700, 300, 40),
                OrnateGuiSkin.Glow("pity_shade", new Color(0f, 0f, 0f, .5f)));
            GUI.Label(new Rect(66, 708, 408, 26),
                astral ?
                    "UR PITY " + progress.Data.astralPity + "/" +
                        ChroniclesSummonRules.AstralUrPity +
                        "  -  ASTRAL SEALS " + progress.Data.astralSeals :
                    "SSR PITY " + progress.Data.standardPity + "/" +
                        ChroniclesSummonRules.StandardSsrPity +
                        "  -  TICKETS " + progress.Data.summonTickets,
                goldCenter);

            OrnateGuiSkin.Embers(new Rect(30, 300, 480, 380), 14, .05f,
                new Color(1f, .8f, .45f, .9f));
            int cost = ChroniclesSummonRules.GemCost(activeSummonBanner);
            string type = astral ? "SEAL" : "TICKET";
            bool canOne = astral ? progress.Data.astralSeals >= 1 ||
                progress.Data.gems >= cost :
                progress.Data.summonTickets >= 1 || progress.Data.gems >= cost;
            bool canTen = astral ? progress.Data.astralSeals >= 10 ||
                progress.Data.gems >= cost * 10 :
                progress.Data.summonTickets >= 10 || progress.Data.gems >= cost * 10;
            if (canOne)
                OrnateGuiSkin.PulseGlow(new Rect(20, 742, 258, 90),
                    new Color(1f, .8f, .4f, .8f));
            if (canTen)
                OrnateGuiSkin.PulseGlow(new Rect(262, 742, 258, 90),
                    new Color(1f, .8f, .4f, .8f));
            GUIStyle summonBtn = new GUIStyle(OrnateGuiSkin.ButtonStyle()) {
                fontSize = 13, padding = new RectOffset(16, 16, 4, 4) };
            if (GUI.Button(new Rect(34, 756, 230, 62),
                "SUMMON x1\n1 " + type + " / " + cost + " GEMS", summonBtn))
                DoSummon(1);
            if (GUI.Button(new Rect(276, 756, 230, 62),
                "SUMMON x10\n10 " + type + " / " + (cost * 10) + " GEMS", summonBtn))
                DoSummon(10);

            OrnateGuiSkin.Panel(new Rect(30, 832, 480, 92),
                "summon_history", OrnateGuiSkin.Gold,
                new Color(.09f, .07f, .12f, .94f));
            GUI.Label(new Rect(40, 838, 460, 24), "RECENT SUMMON HISTORY",
                goldCenter);
            int first = Math.Max(0, progress.Data.summonHistory.Count - 3);
            for (int i = first; i < progress.Data.summonHistory.Count; i++)
            {
                SummonHistoryRecord record = progress.Data.summonHistory[i];
                HeroDefinition hero = WorldsCatalog.FindHero(record.heroId);
                if (hero == null) continue;
                int index = i - first;
                GUI.Label(new Rect(72, 862 + index * 20, 420, 22),
                    hero.name + "  -  " + hero.rarity + "  -  " +
                    record.banner + (record.guaranteed ? "  [PITY]" : ""), tiny);
            }
        }

        // Deterministic daily showcase hero for the banner art. The Astral
        // Covenant pool is UR-only; the Standard pool shows SSR heroes.
        private static string FeaturedBannerHero(bool astral)
        {
            var pool = new List<string>();
            foreach (HeroDefinition hero in WorldsCatalog.Heroes)
                if (astral ? hero.rarity == Rarity.UR : hero.rarity == Rarity.SSR)
                    pool.Add(hero.id);
            if (pool.Count == 0) return "ra";
            return pool[DateTime.UtcNow.DayOfYear % pool.Count];
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
            summonRevealStart = Time.realtimeSinceStartup;
            lastSummoned = result.heroIds.Count > 0 ?
                result.heroIds[result.heroIds.Count - 1] : "";
            notice = result.heroIds.Count + (result.heroIds.Count == 1 ?
                " hero summoned." : " heroes summoned.");
        }

        private void Quests(bool weekly)
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 249, 480, 54), weekly ? "WEEKLY MISSIONS" : "DAILY MISSIONS", 24);
            if (GUI.Button(new Rect(25, 306, 153, 40), "DAILY")) page = Page.Quests;
            if (GUI.Button(new Rect(193, 306, 154, 40), "WEEKLY")) page = Page.WeeklyQuests;
            if (GUI.Button(new Rect(360, 306, 160, 40), "ACHIEVEMENTS"))
                page = Page.Achievements;
            QuestInfo[] quests = weekly ? PlayerProgressService.WeeklyQuests : PlayerProgressService.DailyQuests;
            for (int i = 0; i < quests.Length; i++)
            {
                QuestInfo quest = quests[i];
                int amount = weekly ? progress.WeeklyQuestProgress(quest.id) : progress.QuestProgress(quest.id);
                bool claimed = weekly ? progress.Data.claimedWeeklyQuests.Contains(quest.id)
                    : progress.Data.claimedDailyQuests.Contains(quest.id);
                float y = 361 + i * 99;
                GUI.Box(new Rect(21, y, 500, 87), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(37, y + 8, 276, 67),
                    quest.title + "\n" + Math.Min(quest.target, amount) + "/" + quest.target +
                    "  •  " + quest.goldReward + " gold / " + quest.gemReward + " gems", tiny);
                {
                    float qf = quest.target > 0 ? Mathf.Clamp01(amount / (float)quest.target) : 1f;
                    Color qOld = GUI.color;
                    GUI.color = new Color(.05f, .04f, .08f, .85f);
                    GUI.DrawTexture(new Rect(37, y + 72, 290, 8), Texture2D.whiteTexture);
                    GUI.color = qf >= 1f ? new Color(.95f, .78f, .3f, 1f) : new Color(.45f, .75f, .4f, 1f);
                    GUI.DrawTexture(new Rect(38, y + 73, 288f * qf, 6), Texture2D.whiteTexture);
                    GUI.color = qOld;
                }
                GUI.enabled = !claimed && amount >= quest.target;
                if (GUI.Button(new Rect(340, y + 17, 157, 54), claimed ? "CLAIMED" : "CLAIM"))
                    notice = (weekly ? progress.ClaimWeeklyQuest(quest.id) : progress.ClaimDailyQuest(quest.id))
                        ? "Mission reward claimed!" : "Mission not ready.";
                GUI.enabled = true;
            }
        }

                private void Achievements()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 244, 480, 54), "ACHIEVEMENTS", 24);
            if (GUI.Button(new Rect(25, 306, 153, 40), "DAILY")) page = Page.Quests;
            if (GUI.Button(new Rect(193, 306, 154, 40), "WEEKLY")) page = Page.WeeklyQuests;
            if (GUI.Button(new Rect(360, 306, 160, 40), "ACHIEVEMENTS"))
                page = Page.Achievements;
            chapterScroll = GUI.BeginScrollView(
                new Rect(20, 360, 503, 522), chapterScroll,
                new Rect(0, 0, 473, ChroniclesAchievements.All.Length * 101));
            for (int i = 0; i < ChroniclesAchievements.All.Length; i++)
            {
                AchievementDefinition entry = ChroniclesAchievements.All[i];
                int completed = ChroniclesAchievements.Progress(progress.Data, entry.id);
                bool claimed = progress.Data.claimedAchievements.Contains(entry.id);
                bool available = progress.CanClaimAchievement(entry.id);
                float y = i * 101f;
                GUI.Box(new Rect(5, y, 461, 94), "", OrnateGuiSkin.RowBox());
                GUI.Label(new Rect(14, y + 8, 288, 77),
                    entry.title + "\n" + Math.Min(entry.target, completed) + "/" +
                    entry.target + "  •  " + entry.gems + " Gems" +
                    (entry.astralSeals > 0 ? "\n+" + entry.astralSeals + " Astral Seals" : ""),
                    tiny);
                Color beforeColor = GUI.backgroundColor;
                if (available) GUI.backgroundColor = new Color(.91f, .76f, .45f);
                GUI.enabled = available;
                if (GUI.Button(new Rect(308, y + 23, 145, 53),
                    claimed ? "CLAIMED" : available ? "CLAIM" : "LOCKED"))
                    notice = progress.ClaimAchievement(entry.id) ?
                        "Achievement reward claimed!" : "Achievement not complete.";
                GUI.enabled = true;
                GUI.backgroundColor = beforeColor;
            }
            GUI.EndScrollView();
        }

        private void Idle()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 275, 480, 54), "AFK TREASURY", 24);
            GUI.Box(new Rect(41, 388, 459, 177), "Your adventurers gather resources while you are away.");
            GUI.Label(new Rect(26, 582, 492, 98),
                "Elapsed: " + progress.IdleAvailableMinutes() + " / 720 minutes\n" +
                "Gold waiting: " + progress.IdleProjectedGold(), subtitle);
            {
                // Fill bar: how much of the 12h AFK cap has built up.
                float fill = Mathf.Clamp01(progress.IdleAvailableMinutes() / 720f);
                Color barOld = GUI.color;
                GUI.color = new Color(.05f, .04f, .08f, .9f);
                GUI.DrawTexture(new Rect(40, 678, 460, 16), Texture2D.whiteTexture);
                GUI.color = fill >= 1f ? new Color(.95f, .78f, .3f, 1f) : new Color(.45f, .75f, .4f, 1f);
                GUI.DrawTexture(new Rect(42, 680, 456f * fill, 12), Texture2D.whiteTexture);
                GUI.color = barOld;
            }
            if (GUI.Button(new Rect(40, 700, 460, 70), "CLAIM IDLE GOLD", bigButton))
            {
                int gold = progress.ClaimIdleGold();
                notice = gold > 0 ? "Collected " + gold + " gold." : "Not enough time elapsed yet.";
            }
            int fastLeft = progress.FastBattlesRemaining();
            GUI.enabled = fastLeft > 0;
            if (GUI.Button(new Rect(40, 782, 460, 62),
                "FAST BATTLE  -  2H INSTANT  (" + fastLeft + "/" +
                PlayerProgressService.FastBattleDailyLimit + " LEFT)"))
            {
                int fastGold = progress.FastBattle();
                notice = fastGold > 0 ?
                    "Fast battle: +" + fastGold + " gold instantly." :
                    "No fast battles left today.";
            }
            GUI.enabled = true;
        }

        private void DrawCurrency(float x, float y, string icon, int amount, GUIStyle style)
        {
            Texture2D art = OrnateGuiSkin.Art(icon);
            if (art != null)
                GUI.DrawTexture(new Rect(x, y, 40, 40), art, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(x + 46, y + 5, 110, 30), amount.ToString(), style);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private static Rect ScaledAroundCenter(Rect r, float scale)
        {
            float w = r.width * scale, h = r.height * scale;
            return new Rect(r.center.x - w / 2f, r.center.y - h / 2f, w, h);
        }

        private void Login()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 228, 480, 54), "28-DAY LOGIN REWARDS", 24);
            GUI.Label(new Rect(25, 283, 490, 56),
                "Claim one reward daily (UTC). Missed days never reset progress.",
                pageDesc);
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
                GUI.Box(new Rect(x, y, 67, 89), "", OrnateGuiSkin.CellBox());
                GUI.Label(new Rect(x + 2, y + 4, 63, 16), "DAY " + (i + 1), cellText);
                Texture2D rewardIcon = OrnateGuiSkin.Art(gift.IconName);
                if (rewardIcon != null)
                {
                    Rect iconRect = new Rect(x + 11.5f, y + 22, 44, 44);
                    if (received)
                    {
                        Color tint = GUI.color;
                        GUI.color = new Color(1f, 1f, 1f, .45f);
                        GUI.DrawTexture(iconRect, rewardIcon, ScaleMode.ScaleToFit, true);
                        GUI.color = tint;
                    }
                    else GUI.DrawTexture(iconRect, rewardIcon, ScaleMode.ScaleToFit, true);
                }
                GUI.Label(new Rect(x + 2, y + 67, 63, 18),
                    received ? "CLAIMED" : gift.ShortLabel, cellText);
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

        private void Events()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 224, 480, 54), "LIMITED-TIME EVENTS", 24);
            System.DateTime now = System.DateTime.UtcNow;
            float y = 306f;
            foreach (ChroniclesEventInfo info in ChroniclesEvents.All)
            {
                bool tapped = EventCard(new Rect(24, y, 492, 160),
                    "Chronicles/UI/event_" + info.id,
                    info.title, info.description,
                    ChroniclesEvents.IsActive(info, now),
                    ChroniclesEvents.IsUpcoming(info, now),
                    ChroniclesEvents.Countdown(info, now),
                    info.kind == ChroniclesEventKind.AstralRateUp ?
                        "GO TO SUMMON" : "FIGHT NOW");
                if (tapped) EventGo(info);
                y += 174f;
            }
            if (EventCard(new Rect(24, y, 492, 160),
                "Chronicles/UI/event_login_rewards",
                "28-DAY LOGIN REWARDS",
                "Claim one gift every UTC day. Missing a day never resets progress.",
                true, false, "", "OPEN"))
                page = Page.Login;
        }

        // One illustrated event banner: full-bleed art, a soft scrim for the
        // text, status and countdown over the art, and a clear tap button.
        // Returns true when its action button was tapped.
        private bool EventCard(Rect r, string artPath, string title,
            string description, bool active, bool upcoming, string countdown,
            string buttonLabel)
        {
            Texture2D art = Resources.Load<Texture2D>(artPath);
            if (art != null)
                GUI.DrawTexture(r, art, ScaleMode.ScaleAndCrop);
            else
                OrnateGuiSkin.Bar(r);
            // dim the whole card a little, then a stronger scrim at the
            // bottom, so text stays readable over bright artwork
            GUI.color = new Color(0f, 0f, 0f, .28f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(r.x, r.y + r.height - 74f, r.width, 74f),
                OrnateGuiSkin.Glow("event_scrim", new Color(0f, 0f, 0f, .82f)));
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 46f),
                OrnateGuiSkin.Glow("event_scrim_top", new Color(0f, 0f, 0f, .6f)));
            if (active)
                OrnateGuiSkin.PulseGlow(r, new Color(1f, .82f, .42f, .22f), 1.8f);
            GUIStyle cardTitle = new GUIStyle(subtitle) {
                fontStyle = FontStyle.Bold, fontSize = 22 };
            cardTitle.normal.textColor = Color.black;
            GUI.Label(new Rect(r.x + 22, r.y + 13, r.width - 160, 32), title, cardTitle);
            cardTitle.normal.textColor = OrnateGuiSkin.GoldBright;
            GUI.Label(new Rect(r.x + 20, r.y + 11, r.width - 160, 32), title, cardTitle);
            // status chip on ornate bar art, not a plain code plate
            Texture2D chipArt = OrnateGuiSkin.Art("bar_plate");
            Rect chip = new Rect(r.x + r.width - 118, r.y + 12, 100, 28);
            if (chipArt != null)
            {
                GUI.color = active ? new Color(.55f, 1f, .6f) :
                    upcoming ? new Color(1f, .9f, .5f) : Color.gray;
                GUI.DrawTexture(chip, chipArt, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }
            GUIStyle status = new GUIStyle(tiny) {
                fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            status.normal.textColor = new Color(.08f, .06f, .12f);
            GUI.Label(chip, active ? "ACTIVE" : upcoming ? "SOON" : "ENDED", status);
            GUIStyle desc = new GUIStyle(tiny);
            desc.normal.textColor = new Color(1f, .96f, .88f);
            GUI.Label(new Rect(r.x + 20, r.y + 44, r.width - 190, 44),
                description, desc);
            if (!string.IsNullOrEmpty(countdown))
            {
                GUIStyle timer = new GUIStyle(tiny) { fontStyle = FontStyle.Bold };
                timer.normal.textColor = new Color(1f, .9f, .6f);
                GUI.Label(new Rect(r.x + 20, r.y + r.height - 34, 260, 26),
                    countdown, timer);
            }
            GUI.enabled = active;
            bool tapped = GUI.Button(
                new Rect(r.x + r.width - 174, r.y + r.height - 62, 158, 46),
                buttonLabel, OrnateGuiSkin.ButtonStyle());
            GUI.enabled = true;
            return tapped;
        }

        private void EventGo(ChroniclesEventInfo info)
        {
            if (info.kind == ChroniclesEventKind.AstralRateUp)
                page = Page.Summon;
            else
            {
                inTower = false; inRaid = false; inLabyrinth = false;
                inArenaTraining = false;
                inElementalGate = false; inEventAdventure = false; inGuildBoss = false; inTournament = false;
                activeTrialMode = TrialMode.Normal;
                page = Page.Chapters;
            }
        }

        private void Settings()
        {
            OrnateGuiSkin.TitlePlate(new Rect(30, 254, 480, 54), "GAME SETTINGS", 24);
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
            bool oldPanel = PanelBattleEnabled;
            bool newPanel = GUI.Toggle(new Rect(66, 660, 413, 40),
                oldPanel, "COMIC-PANEL BATTLE VIEW (BETA)");
            if (newPanel != oldPanel)
            { PanelBattleEnabled = newPanel; notice = "Battle view preference saved."; }
            if (GUI.Button(new Rect(58, 706, 425, 64), "SAVE AUDIO SETTINGS", bigButton))
            {
                bool saved = progress.SetAudioLevels(draftMusic, draftEffects);
                if (audio != null) audio.SetVolumes(draftMusic, draftEffects);
                notice = saved ? "Audio preferences saved." : "Could not save settings.";
            }
            if (GUI.Button(new Rect(58, 743, 425, 40), "RESTART FIRST-TIME GUIDE"))
            {
                notice = progress.RestartTutorial() ?
                    "First-time guide restarted." : "Could not restart guide.";
                if (progress.Data.tutorialCompleted == false) page = Page.Home;
            }
            GUI.Label(new Rect(37, 791, 466, 27),
                "SERVER: " + (catalogClient == null ?
                    "OFFLINE MODE" : catalogClient.Status), tiny);
            GUI.enabled = catalogClient != null && !catalogClient.Busy;
            if (GUI.Button(new Rect(58, 815, 425, 27), "CHECK PUBLIC GAME API"))
                catalogClient.CheckReadOnlyCatalog(publicCatalogServerUrl);
            GUI.enabled = true;
            if (GUI.Button(new Rect(40, 845, 460, 54), "RETURN HOME", bigButton))
                GoHome();
        }

        // Comic-panel battle presentation (Khalid-approved layout, now the
        // default battle view per his 7:01 message: the game switches to the
        // card style from the reference videos since we are not going 3D).
        // The 3D-arena view stays available as the opt-in alternative. Toggle
        // persists in PlayerPrefs.
        private bool PanelBattleEnabled
        {
            get { return PlayerPrefs.GetInt("panel_battle", 1) == 1; }
            set { PlayerPrefs.SetInt("panel_battle", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        private void BeginBattle()
        {
            if (progress.Data.selectedTeam.Count != PlayerProgressService.TeamSize)
            { notice = "Select exactly six heroes first."; return; }
            // Campaign stages cost stamina; every other mode keeps its own
            // attempt system (#12: free regen only, no purchases).
            bool isCampaign = !inArenaTraining && !inLabyrinth && !inRaid &&
                !inGuildBoss && !inTournament && !inEventAdventure &&
                !inElementalGate && !inTower;
            if (isCampaign && !progress.TrySpendStamina(PlayerProgressService.StaminaCostCampaign))
            {
                notice = "Not enough stamina. It regenerates free over time.";
                return;
            }
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
            LabyrinthOption labyrinthOption =
                inLabyrinth ? progress.LabyrinthChoice(selectedLabyrinthChoice) : null;
            bool unlocked = stage != null &&
                (inArenaTraining ? progress.Data.lastClearedStage >= 10 &&
                    progress.ArenaAttemptsRemaining() > 0 &&
                    stageId == ChroniclesArenaTraining.StageFor(
                        progress.ArenaTrainingState().rating,
                        progress.ArenaTrainingState().attemptsToday + 1) :
                inLabyrinth ? labyrinthOption != null &&
                    labyrinthOption.RequiresBattle &&
                    labyrinthOption.stageId == stageId :
                inRaid ? progress.Data.lastClearedStage >= 10 &&
                    progress.RaidAttemptsRemaining() > 0 &&
                    stageId == ChroniclesRaids.StageForDay(DateTime.UtcNow).id :
                inElementalGate ? progress.CanEnterGate(selectedGateElement) &&
                    stageId == ChroniclesElementalGates.StageTemplateFor(
                        progress.GateState(selectedGateElement).unlockedLevel) :
                inEventAdventure ? progress.CanEnterAdventureStage(
                    ChroniclesEventAdventures.Active(DateTime.UtcNow).id,
                    selectedAdventureStage) &&
                    stageId == ChroniclesEventAdventures.StageTemplateFor(
                        selectedAdventureStage) :
                inGuildBoss ? progress.Data.lastClearedStage >= 10 &&
                    progress.GuildBossAttemptsRemaining() > 0 &&
                    stageId == ChroniclesRaids.StageForDay(DateTime.UtcNow).id :
                inTournament ? progress.CanEnterTournament() &&
                    stageId == ChroniclesTournament.StageFor(
                        progress.Data.tournamentRoundsWon) :
                inTower ?
                    progress.CanEnterTower(selectedTowerOrigin, selectedTowerFloor,
                        progress.Data.selectedTeam) :
                    activeTrialMode == TrialMode.Normal ?
                        stageId <= progress.Data.lastClearedStage + 1 :
                        progress.CanPlayTrial(stageId, activeTrialMode));
            if (!unlocked)
            { notice = "Stage is locked or no attempts remain."; return; }
            if (inRaid)
            {
                raidRunId = progress.BeginRaid();
                if (string.IsNullOrEmpty(raidRunId))
                { notice = "Unable to start a World Boss attempt."; return; }
            }
            if (inLabyrinth)
            {
                labyrinthRunId = progress.BeginLabyrinthBattle(selectedLabyrinthChoice);
                if (string.IsNullOrEmpty(labyrinthRunId))
                { notice = "Unable to begin this Labyrinth encounter."; return; }
            }
            if (inArenaTraining)
            {
                arenaRunId = progress.BeginArenaTraining();
                if (string.IsNullOrEmpty(arenaRunId))
                { notice = "No Arena Training attempts remain."; return; }
            }
            if (inElementalGate)
            {
                gateRunId = progress.BeginGate(selectedGateElement);
                if (string.IsNullOrEmpty(gateRunId))
                { notice = "No gate attempts remain, or the team does not match."; return; }
            }
            if (inEventAdventure)
            {
                adventureRunId = progress.BeginAdventureStage(
                    ChroniclesEventAdventures.Active(DateTime.UtcNow).id,
                    selectedAdventureStage);
                if (string.IsNullOrEmpty(adventureRunId))
                { notice = "That adventure stage is not available."; return; }
            }
            if (inGuildBoss)
            {
                guildBossRunId = progress.BeginGuildBoss();
                if (string.IsNullOrEmpty(guildBossRunId))
                { notice = "No guild boss attacks remain."; return; }
            }
            if (inTournament)
            {
                tournamentRunId = progress.BeginTournamentRound();
                if (string.IsNullOrEmpty(tournamentRunId))
                { notice = "The bracket is not available."; return; }
            }
            if (battle != null) Destroy(battle.gameObject);
            battle = new GameObject("Chronicles - Battle").AddComponent<ChroniclesBattle>();
            battle.PanelLayout = PanelBattleEnabled;
            if (inTournament)
                battle.BannerOverride = "ROUND " + (progress.Data.tournamentRoundsWon + 1);
            try
            {
                battle.Initialize(stage, chosen, progress.Data, OnBattleFinished,
                    activeTrialMode, inTower ? (Origin?)selectedTowerOrigin : null,
                    inTower ? selectedTowerFloor : 0, inRaid || inGuildBoss,
                    labyrinthOption, inLabyrinth ?
                        progress.LabyrinthState().blessingStacks : 0,
                    inArenaTraining ? progress.ArenaTrainingState().rating :
                    inTournament ? ChroniclesTournament.EnemyRatingFor(
                        progress.Data.tournamentRoundsWon) : -1);
            }
            catch (Exception ex)
            {
                if (inRaid && !string.IsNullOrEmpty(raidRunId))
                    progress.AbandonRaid(raidRunId);
                raidRunId = "";
                if (inLabyrinth && !string.IsNullOrEmpty(labyrinthRunId))
                    progress.CancelLabyrinthStart(labyrinthRunId);
                labyrinthRunId = "";
                if (inArenaTraining && !string.IsNullOrEmpty(arenaRunId))
                    progress.CancelArenaTrainingStart(arenaRunId);
                arenaRunId = "";
                if (inElementalGate && !string.IsNullOrEmpty(gateRunId))
                    progress.AbandonGate(selectedGateElement);
                gateRunId = "";
                if (inEventAdventure && !string.IsNullOrEmpty(adventureRunId))
                    progress.AbandonAdventure();
                adventureRunId = "";
                if (inGuildBoss && !string.IsNullOrEmpty(guildBossRunId))
                    progress.AbandonGuildBoss();
                guildBossRunId = "";
                if (inTournament && !string.IsNullOrEmpty(tournamentRunId))
                    progress.AbandonTournament();
                tournamentRunId = "";
                CleanupBattle();
                notice = "Failed to create battle: " + ex.Message;
                return;
            }
            if (menuCamera != null) menuCamera.enabled = false;
            if (menuAudio != null) menuAudio.enabled = false;
            clearedStageBeforeBattle = progress.Data.lastClearedStage;
            page = Page.Battle; notice = "";
            if (audio != null) audio.SetState(AudioScene.Battle, stage.chapter);
        }

        private void OnBattleFinished(bool victory)
        {
            lastVictory = victory;
            progress.ConsumeMercenary();
            if (inArenaTraining)
            {
                bool savedArena = progress.CompleteArenaTraining(arenaRunId, victory);
                arenaRunId = "";
                if (!savedArena) notice = "Arena Training result could not be saved.";
                else notice = victory ? "Training victory recorded!" :
                    "Training defeat recorded.";
            }
            else if (inLabyrinth)
            {
                bool saved = progress.CompleteLabyrinthBattle(labyrinthRunId, victory);
                labyrinthRunId = "";
                if (!saved) notice = "Labyrinth result could not be saved.";
                else notice = victory ? "Labyrinth room cleared!" :
                    "Defeat! One Labyrinth life lost.";
            }
            else if (inElementalGate)
            {
                string gateReward;
                bool savedGate = progress.CompleteGate(gateRunId, victory, out gateReward);
                gateRunId = "";
                if (!savedGate) notice = "Gate result could not be saved.";
                else notice = victory ? "Gate cleared: " + gateReward :
                    "The gate holds. Try a stronger " +
                    selectedGateElement.ToString().ToUpperInvariant() + " team.";
            }
            else if (inEventAdventure)
            {
                string adventureReward;
                bool savedAdventure = progress.CompleteAdventureStage(
                    adventureRunId, victory, out adventureReward);
                adventureRunId = "";
                if (!savedAdventure) notice = "Adventure result could not be saved.";
                else notice = victory ? "Stage cleared: " + adventureReward :
                    "The adventure party falls. Power up and retry.";
            }
            else if (inTournament)
            {
                bool savedRound = progress.CompleteTournamentRound(
                    tournamentRunId, victory);
                tournamentRunId = "";
                if (!savedRound) notice = "Tournament result could not be saved.";
                else notice = victory ?
                    (progress.Data.tournamentRoundsWon >= ChroniclesTournament.Rounds ?
                        "CHAMPION! Claim your reward." : "Advance! Next round awaits.") :
                    "Eliminated. Claim your placement reward.";
            }
            else if (inGuildBoss)
            {
                int bossDamage = battle == null ? 0 : battle.RaidDamageScore;
                bool savedBoss = progress.CompleteGuildBoss(guildBossRunId, bossDamage);
                guildBossRunId = "";
                lastVictory = bossDamage > 0;
                if (!savedBoss) notice = "Guild boss result could not be saved.";
                else notice = "Guild boss hit for " +
                    bossDamage.ToString("N0") + " damage.";
            }
            else if (inRaid)
            {
                lastRaidDamage = battle == null ? 0 : battle.RaidDamageScore;
                bool savedRaid = progress.CompleteRaid(raidRunId, lastRaidDamage);
                raidRunId = "";
                lastVictory = lastRaidDamage > 0;
                if (!savedRaid) notice = "World Boss result could not be saved.";
                else notice = "World Boss attempt recorded.";
            }
            else if (victory)
            {
                bool saved = inTower ?
                    progress.CompleteTower(selectedTowerOrigin, selectedTowerFloor,
                        progress.Data.selectedTeam) :
                    activeTrialMode == TrialMode.Normal ?
                        progress.CompleteStage(stageId) :
                        progress.CompleteTrial(stageId, activeTrialMode);
                if (!saved) notice = "Battle won, but progress could not be saved.";
            }
            if (audio != null) audio.SetState(victory ? AudioScene.Victory : AudioScene.Defeat);
            page = Page.Results;
        }

        private readonly System.Collections.Generic.List<ChroniclesBattle.DamageEntry>
            damageMeterScratch = new System.Collections.Generic.List<ChroniclesBattle.DamageEntry>();

        private void BattleHud(float scale)
        {
            if (battle == null) return;
            battle.DrawWorldLabels(scale);
            if (battle.PanelLayout)
            {
                // Panel mode keeps the stage title off the panel wall.
            }
            else
            {
            OrnateGuiSkin.Bar(new Rect(12, 22, 516, 106));
            GUI.Label(new Rect(20, 29, 500, 45),
                inArenaTraining ? "ARENA TRAINING • NPC OPPONENT" :
                inLabyrinth ? "WEEKLY LABYRINTH • ROOM " +
                    progress.LabyrinthState().nextRoom :
                inRaid ? "DAILY WORLD BOSS" :
                inGuildBoss ? "GUILD BOSS • DAILY ASSAULT" :
                inTournament ? "TOURNAMENT • " +
                    ChroniclesTournament.RoundNames[Mathf.Min(
                        progress.Data.tournamentRoundsWon,
                        ChroniclesTournament.Rounds - 1)] :
                inEventAdventure ? "EVENT ADVENTURE • STAGE " + selectedAdventureStage :
                inElementalGate ? ChroniclesElementalGates.GateName(selectedGateElement).ToUpperInvariant() +
                    " • LEVEL " + progress.GateState(selectedGateElement).unlockedLevel :
                inTower ? ChroniclesTowers.DisplayName(selectedTowerOrigin) +
                    " • FLOOR " + selectedTowerFloor :
                activeTrialMode.ToString().ToUpperInvariant() + " • " +
                WorldsCatalog.GetStage(stageId).name, subtitle);
            GUI.Label(new Rect(29, 73, 460, 36),
                "TIME " + Mathf.CeilToInt(battle.SecondsLeft) + " s" +
                (inRaid ? "   •   BOSS DAMAGE " + battle.RaidDamageScore : ""),
                subtitle);
            }
            if (battle.PanelLayout)
            {
                // Right rail: speed, fast-forward, timer, damage meter (panel layout).
                if (GUI.Button(new Rect(486, 66, 52, 46), "x" + battle.Speed,
                    OrnateGuiSkin.ButtonStyle(false)))
                    battle.Speed = battle.Speed >= 2f ? 1f : 2f;
                if (GUI.Button(new Rect(486, 150, 52, 46), ">>",
                    OrnateGuiSkin.ButtonStyle(false)))
                    battle.Speed = 8f;
                Texture2D railPlate = OrnateGuiSkin.Art("bar_plate");
                if (railPlate != null)
                {
                    Color railC = battle.SecondsLeft < 10f ?
                        new Color(.42f, .1f, .1f, .95f) : new Color(.14f, .12f, .26f, .95f);
                    GUI.color = railC;
                    GUI.DrawTexture(new Rect(478, 236, 62, 44),
                        railPlate, ScaleMode.StretchToFill);
                    GUI.DrawTexture(new Rect(478, 326, 62, 34),
                        railPlate, ScaleMode.StretchToFill);
                    GUI.color = Color.white;
                }
                GUI.Label(new Rect(482, 240, 58, 40),
                    Mathf.CeilToInt(battle.SecondsLeft) + "s", subtitle);
                GUI.Label(new Rect(482, 330, 58, 30), "DMG", subtitle);
                battle.FillDamageMeter(damageMeterScratch);
                int shown = 0;
                foreach (var entry in damageMeterScratch)
                {
                    if (shown >= 4) break;
                    GUI.Label(new Rect(482, 356 + shown * 30, 58, 28),
                        (entry.amount >= 1000 ?
                            (entry.amount / 1000).ToString() + "K" :
                            entry.amount.ToString()), subtitle);
                    shown++;
                }
                if (GUI.Button(new Rect(486, 560, 52, 46),
                    battle.Paused ? ">" : "||", OrnateGuiSkin.ButtonStyle(false)))
                    battle.Paused = !battle.Paused;
                if (GUI.Button(new Rect(486, 614, 52, 46), "X",
                    OrnateGuiSkin.ButtonStyle(false)))
                {
                    CleanupBattle();
                    page = Page.Formation;
                }
                return;
            }
            if (GUI.Button(new Rect(20, 837, 153, 65), battle.Paused ? "RESUME" : "PAUSE",
                OrnateGuiSkin.ButtonStyle(false)))
                battle.Paused = !battle.Paused;
            if (GUI.Button(new Rect(184, 837, 159, 65), "SPEED x" + battle.Speed,
                OrnateGuiSkin.ButtonStyle(false)))
                battle.Speed = battle.Speed >= 2f ? 1f : 2f;
            if (GUI.Button(new Rect(354, 837, 160, 65), "RETREAT",
                OrnateGuiSkin.ButtonStyle(false)))
            {
                CleanupBattle();
                page = Page.Formation;
            }
        }

        private void Results()
        {
            GUI.DrawTexture(new Rect(0, 0, 540, 960), panelTex);
            OrnateGuiSkin.ScreenBackground("bg_menu");
            if (inArenaTraining)
            {
                ArenaTrainingProgress arena = progress.ArenaTrainingState();
                OrnateGuiSkin.Title(new Rect(24, 245, 490, 70),
                    lastVictory ? "ARENA TRAINING WIN" : "ARENA TRAINING LOSS", 34);
                GUI.Label(new Rect(38, 394, 465, 135),
                    "LOCAL RATING: " + arena.rating +
                    "\nTIER: " + ChroniclesArenaTraining.Tier(arena.rating) +
                    "\nTRAINING WINS: " + arena.wins + "  •  LOSSES: " + arena.losses +
                    "\nMATCHES LEFT TODAY: " + progress.ArenaAttemptsRemaining(), subtitle);
                GUI.Box(new Rect(44, 558, 451, 110),
                    "Practice against NPC teams.\nOnline PvP and global rankings are not active.");
                if (GUI.Button(new Rect(42, 728, 455, 74), "RETURN TO ARENA", bigButton))
                {
                    CleanupBattle();
                    inArenaTraining = false;
                    page = Page.ArenaTraining;
                }
                if (GUI.Button(new Rect(42, 819, 455, 64), "RETURN HOME"))
                    GoHome();
                return;
            }
            if (inLabyrinth)
            {
                LabyrinthProgress run = progress.LabyrinthState();
                OrnateGuiSkin.Title(new Rect(20, 245, 500, 70),
                    lastVictory ? "LABYRINTH CLEARED" : "LABYRINTH DEFEAT", 34);
                GUI.Label(new Rect(40, 376, 460, 150),
                    "NEXT ROOM: " + Mathf.Min(ChroniclesLabyrinth.Rooms, run.nextRoom) +
                    "\nLIVES: " + run.lives + "/" + ChroniclesLabyrinth.StartingLives +
                    "\nBLESSINGS: " + run.blessingStacks +
                    "\nRUN SCORE: " + run.score, subtitle);
                GUI.Box(new Rect(40, 548, 460, 119),
                    run.completed ? "WEEKLY LABYRINTH FINISHED!\nFinal rewards claimed." :
                    run.lives == 0 ? "NO LIVES REMAINING THIS WEEK." :
                    lastVictory ? "Choose your next path to keep exploring." :
                    "You may try this room again if you have lives left.");
                if (GUI.Button(new Rect(40, 728, 460, 76), "RETURN TO LABYRINTH", bigButton))
                {
                    CleanupBattle();
                    inLabyrinth = false;
                    page = Page.Labyrinth;
                }
                if (GUI.Button(new Rect(40, 823, 460, 60), "RETURN HOME"))
                    GoHome();
                return;
            }
            if (inRaid)
            {
                OrnateGuiSkin.Title(new Rect(25, 245, 490, 70),
                    "WORLD BOSS COMPLETE", 34);
                GUI.Label(new Rect(34, 390, 472, 135),
                    "DAMAGE DEALT: " + lastRaidDamage +
                    "\nBEST TODAY: " + progress.Data.raidBestDamage +
                    "\nPERSONAL RANK: " + ChroniclesRaids.Rank(progress.Data.raidBestDamage),
                    subtitle);
                GUI.Box(new Rect(39, 542, 462, 115),
                    "Attempts remaining: " + progress.RaidAttemptsRemaining() +
                    "\nRewards depend on your damage and highest new tier.");
                if (GUI.Button(new Rect(40, 718, 462, 77), "RETURN TO WORLD BOSS", bigButton))
                {
                    CleanupBattle();
                    inRaid = false;
                    page = Page.Raid;
                }
                if (GUI.Button(new Rect(40, 817, 462, 62), "RETURN HOME"))
                    GoHome();
                return;
            }
            // Illustrated result emblem with the word on its ribbon; text-only
            // fallback when the art is missing.
            Texture2D resultEmblem = OrnateGuiSkin.Art(
                lastVictory ? "victory_emblem" : "defeat_emblem");
            if (resultEmblem != null)
            {
                float ew = 330f, eh = ew * resultEmblem.height /
                    (float)resultEmblem.width;
                Rect er = new Rect((540f - ew) / 2f, 118, ew, eh);
                if (lastVictory)
                {
                    // Rotating golden rays behind the emblem: a celebration,
                    // not a flat plate.
                    Vector2 emblemCenter = new Vector2(er.x + er.width / 2f,
                        er.y + er.height / 2f);
                    Matrix4x4 raysKeep = GUI.matrix;
                    Color raysColor = GUI.color;
                    for (int ray = 0; ray < 12; ray++)
                    {
                        GUIUtility.RotateAroundPivot(ray * 30f + Time.time * 16f,
                            emblemCenter);
                        GUI.color = new Color(1f, .82f, .38f, .10f);
                        GUI.DrawTexture(new Rect(emblemCenter.x - 8f,
                            emblemCenter.y - 260f, 16f, 260f),
                            Texture2D.whiteTexture);
                    }
                    GUI.matrix = raysKeep;
                    GUI.color = raysColor;
                }
                else
                {
                    // Defeat mood: crimson wash and a dark floor under the panel.
                    Color washKeep = GUI.color;
                    GUI.color = new Color(.45f, .05f, .08f, .20f);
                    GUI.DrawTexture(new Rect(0f, 0f, 540f, 960f),
                        Texture2D.whiteTexture);
                    GUI.color = new Color(0f, 0f, 0f, .30f);
                    GUI.DrawTexture(new Rect(0f, 780f, 540f, 180f),
                        Texture2D.whiteTexture);
                    GUI.color = washKeep;
                }
                GUI.DrawTexture(er, resultEmblem, ScaleMode.ScaleToFit);
                GUIStyle vt = new GUIStyle(heading) {
                    alignment = TextAnchor.MiddleCenter, fontSize = 40,
                    fontStyle = FontStyle.Bold };
                // Cream word with a dark outline reads on the busy ribbon art.
                vt.normal.textColor = lastVictory ?
                    new Color(1f, .95f, .82f) : new Color(.93f, .91f, .99f);
                Rect ribbon = new Rect(20, er.y + er.height * .45f, 500,
                    er.height * .24f);
                string word = lastVictory ? "VICTORY!" : "DEFEAT";
                GUIStyle vtOutline = new GUIStyle(vt);
                vtOutline.normal.textColor = new Color(.08f, .05f, .02f, .9f);
                for (int ox = -2; ox <= 2; ox++)
                    for (int oy = -2; oy <= 2; oy++)
                    {
                        if (ox == 0 && oy == 0) continue;
                        if (Mathf.Abs(ox) + Mathf.Abs(oy) > 3) continue;
                        GUI.Label(new Rect(ribbon.x + ox, ribbon.y + oy,
                            ribbon.width, ribbon.height), word, vtOutline);
                    }
                GUI.Label(ribbon, word, vt);
            }
            else
            {
                OrnateGuiSkin.Title(new Rect(25, 235, 490, 80),
                    lastVictory ? "VICTORY!" : "DEFEAT", 44);
            }
            StageDefinition stage = WorldsCatalog.GetStage(stageId);
            GUI.Label(new Rect(35, 362, 470, 100),
                inTournament ? "TOURNAMENT\n" +
                    ChroniclesTournament.PlacementLabel(
                        progress.Data.tournamentRoundsWon,
                        progress.Data.tournamentEliminated) :
                inGuildBoss ? "GUILD BOSS\nDamage is added to the guild total" :
                inEventAdventure ?
                    ChroniclesEventAdventures.Active(DateTime.UtcNow).title +
                    "\nEvent adventure - stage " + selectedAdventureStage :
                inElementalGate ? ChroniclesElementalGates.GateName(selectedGateElement) +
                    "\nElemental gate challenge" :
                inTower ? ChroniclesTowers.DisplayName(selectedTowerOrigin) +
                    " • FLOOR " + selectedTowerFloor +
                    "\nTower rank: " + ChroniclesTowers.Rank(selectedTowerFloor) :
                activeTrialMode + " • " + stage.name +
                    "\nCampaign rank: " + ChroniclesTrials.CampaignRank(progress.Data),
                subtitle);
            DrawVsResults(new Rect(38, 452, 464, 196), 452f);
            if (lastVictory && activeTrialMode == TrialMode.Normal &&
                !inTower && !inGuildBoss && !inEventAdventure && !inElementalGate &&
                !inRaid && !inLabyrinth && !inArenaTraining && !inTournament &&
                stageId == clearedStageBeforeBattle + 1)
            {
                // Icon-first first-clear rewards: icon + amount, no written names.
                GUIStyle rewardStyle = new GUIStyle(subtitle) { fontSize = 20 };
                rewardStyle.normal.textColor = OrnateGuiSkin.GoldBright;
                DrawCurrency(130, 648, "icon_gold_gems", stage.gold, rewardStyle);
                DrawCurrency(290, 648, "icon_gems", stage.gems, rewardStyle);
            }
            else
            {
                GUI.Label(new Rect(38, 654, 464, 30),
                    lastVictory ? "First-clear rewards are added once. Your progress is saved." :
                        "Upgrade your heroes or change formation to try again.", tiny);
            }
            bool finalTrialStage = activeTrialMode != TrialMode.Normal &&
                stageId % ChroniclesTrials.StagesPerChapter == 0;
            bool finishedTower = inTower &&
                selectedTowerFloor >= ChroniclesTowers.MaxFloor;
            if (GUI.Button(new Rect(42, 689, 456, 70),
                !lastVictory ? "RETRY" :
                inTower ? (finishedTower ? "RETURN TO TOWERS" : "NEXT FLOOR") :
                finalTrialStage ? "RETURN TO TRIALS" : "NEXT STAGE", bigButton))
            {
                if (lastVictory && inTower)
                {
                    if (finishedTower)
                    {
                        inTower = false;
                        chapterScroll = Vector2.zero;
                        page = Page.Towers;
                    }
                    else
                    {
                        selectedTowerFloor++;
                        StageDefinition nextFloor = ChroniclesTowers.StageFor(
                            selectedTowerOrigin, selectedTowerFloor);
                        if (nextFloor != null) stageId = nextFloor.id;
                        page = Page.Formation;
                    }
                }
                else if (lastVictory && finalTrialStage)
                {
                    page = Page.Trials;
                }
                else
                {
                    if (lastVictory && !inTower) stageId = Mathf.Min(60, stageId + 1);
                    page = Page.Formation;
                }
                CleanupBattle();
            }
            if (GUI.Button(new Rect(42, 781, 456, 65), "RETURN HOME", bigButton)) GoHome();
        }


        private readonly System.Collections.Generic.Dictionary<string, Texture2D>
            resultIconCache = new System.Collections.Generic.Dictionary<string, Texture2D>();

        private Texture2D ResultIcon(string heroId)
        {
            Texture2D icon;
            if (resultIconCache.TryGetValue(heroId, out icon)) return icon;
            icon = Resources.Load<Texture2D>("Chronicles/Portraits/Static/" + heroId);
            if (icon == null)
                icon = Resources.Load<Texture2D>("Chronicles/HomeCutouts/" + heroId);
            resultIconCache[heroId] = icon;
            return icon;
        }

        // Reference-style VS block: winner's hero cards colored with per-hero
        // damage bars, loser's side grayscale. Icons only, no written names.
        private void DrawVsResults(Rect area, float top)
        {
            if (battle == null) return;
            battle.FillDamageMeter(damageMeterScratch);
            if (damageMeterScratch.Count == 0) return;
            int maxDmg = 1;
            foreach (var entry in damageMeterScratch)
                if (entry.amount > maxDmg) maxDmg = entry.amount;
            Texture2D vsFrame = OrnateGuiSkin.Art("panel_frame");
            if (vsFrame != null) GUI.DrawTexture(area, vsFrame);
            else GUI.Box(area, "");
            GUIStyle vsStyle = new GUIStyle(heading) {
                alignment = TextAnchor.MiddleCenter, fontSize = 34,
                fontStyle = FontStyle.BoldAndItalic };
            vsStyle.normal.textColor = new Color(1f, .85f, .4f);
            GUI.Label(new Rect(area.x + area.width / 2f - 30f, area.y + 62f, 60f, 60f),
                "VS", vsStyle);
            GUIStyle amtStyle = new GUIStyle(tiny) {
                alignment = TextAnchor.MiddleRight, fontSize = 11 };
            amtStyle.normal.textColor = new Color(1f, .96f, .88f);
            var allies = damageMeterScratch.FindAll(e => e.ally);
            var foes = damageMeterScratch.FindAll(e => !e.ally);
            for (int side = 0; side <= 1; side++)
            {
                var list = side == 0 ? allies : foes;
                bool winnerSide = side == 0 ? lastVictory : !lastVictory;
                float x = side == 0 ? area.x + 12f : area.x + area.width / 2f + 12f;
                for (int i = 0; i < list.Count && i < PlayerProgressService.TeamSize; i++)
                {
                    var entry = list[i];
                    float y = area.y + 14f + i * 34f;
                    Texture2D icon = ResultIcon(entry.heroId);
                    if (icon != null)
                    {
                        GUI.color = winnerSide ? Color.white :
                            new Color(.5f, .5f, .55f, 1f);
                        GUI.DrawTexture(new Rect(x, y, 30f, 30f), icon,
                            ScaleMode.ScaleAndCrop);
                        GUI.color = Color.white;
                    }
                    float frac = Mathf.Clamp01((float)entry.amount / maxDmg);
                    GUI.color = new Color(0f, 0f, 0f, .55f);
                    GUI.DrawTexture(new Rect(x + 36f, y + 9f, 148f, 14f),
                        Texture2D.whiteTexture);
                    GUI.color = winnerSide ? new Color(1f, .75f, .25f) :
                        new Color(.45f, .45f, .5f);
                    GUI.DrawTexture(new Rect(x + 36f, y + 9f, 148f * frac, 14f),
                        Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(x + 36f, y + 8f, 144f, 16f),
                        entry.amount >= 1000 ?
                            (entry.amount / 1000).ToString() + "K" :
                            entry.amount.ToString(), amtStyle);
                }
            }
        }

        private void CleanupBattle()
        {
            if (battle != null) Destroy(battle.gameObject);
            battle = null;
            if (inRaid && !string.IsNullOrEmpty(raidRunId))
            {
                progress.AbandonRaid(raidRunId);
                raidRunId = "";
            }
            if (inLabyrinth && !string.IsNullOrEmpty(labyrinthRunId))
            {
                progress.AbandonLabyrinthBattle(labyrinthRunId);
                labyrinthRunId = "";
            }
            if (inArenaTraining && !string.IsNullOrEmpty(arenaRunId))
            {
                progress.AbandonArenaTraining(arenaRunId);
                arenaRunId = "";
            }
            if (inElementalGate && !string.IsNullOrEmpty(gateRunId))
            {
                progress.AbandonGate(selectedGateElement);
                gateRunId = "";
            }
            if (inEventAdventure && !string.IsNullOrEmpty(adventureRunId))
            {
                progress.AbandonAdventure();
                adventureRunId = "";
            }
            if (inGuildBoss && !string.IsNullOrEmpty(guildBossRunId))
            {
                progress.AbandonGuildBoss();
                guildBossRunId = "";
            }
            if (inTournament && !string.IsNullOrEmpty(tournamentRunId))
            {
                progress.AbandonTournament();
                tournamentRunId = "";
            }
            if (menuCamera != null) menuCamera.enabled = true;
            if (menuAudio != null) menuAudio.enabled = true;
        }

        private void GoHome()
        {
            if (page == Page.Formation && progress.Data.selectedTeam.Count != PlayerProgressService.TeamSize)
            {
                progress.Data.selectedTeam.Clear();
                foreach (var id in WorldsCatalog.StartingHeroes)
                {
                    if (progress.Data.selectedTeam.Count >= PlayerProgressService.TeamSize) break;
                    if (progress.OwnsHero(id)) progress.Data.selectedTeam.Add(id);
                }
                progress.Save();
            }
            CleanupBattle();
            notice = "";
            activeTrialMode = TrialMode.Normal;
            inTower = false;
            inRaid = false;
            inElementalGate = false;
            inEventAdventure = false;
            inGuildBoss = false;
            inTournament = false;
            inLabyrinth = false;
            inArenaTraining = false;
            raidRunId = "";
            labyrinthRunId = "";
            arenaRunId = "";
            adventureRunId = "";
            guildBossRunId = "";
            tournamentRunId = "";
            page = Page.Home;
            if (audio != null) audio.SetState(AudioScene.Menu);
        }
    }
}
