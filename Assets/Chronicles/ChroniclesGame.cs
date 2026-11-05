using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    // Integrated campaign navigation. Current IMGUI layout is a functional scaffold
    // to be replaced by production UI Toolkit art without changing progression logic.
    public sealed class ChroniclesGame : MonoBehaviour
    {
        private enum Page { Home, Chapters, StageStory, Heroes, HeroDetails, Formation, Skins, Gear, Forge, Training, Runes, SignatureWeapon, RelicVault, Summon, Quests, WeeklyQuests, Idle, Settings, Battle, Results }
        private Page page = Page.Home;
        private PlayerProgressService progress;
        private ChroniclesAudioDirector audio;
        private ChroniclesBattle battle;
        private float draftMusic = .8f, draftEffects = .9f;
        private Camera menuCamera;
        private AudioListener menuAudio;
        private int stageId = 1;
        private string selectedHero = "horus";
        private bool showHeroSkills;
        private bool showHeroMotion = true;
        private int galleryTierFilter = -1;
        private float storyStartedAt;
        private string notice = "";
        private string lastSummoned = "";
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
            if (GUI.Button(new Rect(40, 421, 460, 64), "CAMPAIGN", bigButton)) page = Page.Chapters;
            if (GUI.Button(new Rect(40, 497, 222, 62), "HEROES (" + progress.Data.heroes.Count + ")", bigButton)) page = Page.Heroes;
            if (GUI.Button(new Rect(274, 497, 226, 62), "FORMATION", bigButton)) page = Page.Formation;
            if (GUI.Button(new Rect(40, 570, 222, 62), "SUMMON", bigButton)) page = Page.Summon;
            if (GUI.Button(new Rect(274, 570, 226, 62), "DAILY QUESTS", bigButton)) page = Page.Quests;
            if (GUI.Button(new Rect(40, 645, 222, 64), "WEEKLY QUESTS", bigButton))
                page = Page.WeeklyQuests;
            if (GUI.Button(new Rect(274, 645, 226, 64), "AFK REWARDS", bigButton))
                page = Page.Idle;
            if (GUI.Button(new Rect(40, 722, 222, 56), "RELIC VAULT", bigButton))
                page = Page.RelicVault;
            if (GUI.Button(new Rect(274, 722, 226, 56), "SETTINGS", bigButton))
                page = Page.Settings;
            GUI.Label(new Rect(40, 798, 460, 80),
                "Six connected chapters • 41 collectible heroes\n" +
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
                    GUI.Label(new Rect(x, 270, 77, 39), "EX 🔒", subtitle);
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

            var visible = new List<HeroDefinition>();
            foreach (HeroDefinition hero in WorldsCatalog.Heroes)
                if (galleryTierFilter < 0 || (int)hero.rarity == galleryTierFilter)
                    visible.Add(hero);
            visible.Sort((first, second) =>
            {
                int byRarity = second.rarity.CompareTo(first.rarity);
                return byRarity != 0 ? byRarity :
                    string.CompareOrdinal(first.name, second.name);
            });

            int rows = Mathf.CeilToInt(visible.Count / 3f);
            rosterScroll = GUI.BeginScrollView(
                new Rect(17, 323, 509, 560), rosterScroll,
                new Rect(0, 0, 477, Mathf.Max(560, rows * 222 + 12)));
            for (int i = 0; i < visible.Count; i++)
            {
                HeroDefinition hero = visible[i];
                int column = i % 3, row = i / 3;
                float x = 4f + column * 157f;
                float y = 4f + row * 222f;
                // Generate only thumbnails inside or near the viewport.
                if (y + 218f < rosterScroll.y - 30f ||
                    y > rosterScroll.y + 600f) continue;
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
                    "No heroes belong to this tier.", subtitle);
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
            if (GUI.Button(new Rect(40, 803, 460, 70), "START STAGE " + stageId, bigButton))
                BeginBattle();
            GUI.enabled = true;
        }

        private void Summon()
        {
            GUI.Label(new Rect(25, 242, 490, 60), "SUMMONING CHAMBER", heading);
            GUI.Label(new Rect(27, 310, 482, 78),
                "Standard banner: R 20% • SR 65% • SSR 15%" +
                "\nUR light/dark banner is not active yet.", subtitle);
            if (!string.IsNullOrEmpty(lastSummoned))
            {
                HeroDefinition summoned = WorldsCatalog.FindHero(lastSummoned);
                if (summoned != null)
                {
                    ChroniclesHeroPortrait.Draw(summoned.id,
                        new Rect(141, 390, 258, 343),
                        !progress.Data.reducePortraitMotion);
                    GUI.Label(new Rect(70, 735, 400, 50),
                        summoned.name + " • " + summoned.rarity +
                        " • " + summoned.element, heading);
                }
            }
            else GUI.Box(new Rect(118, 394, 304, 332),
                "Summon a hero to reveal\nan animated portrait.");
            if (GUI.Button(new Rect(40, 803, 460, 70),
                "SUMMON ONE HERO • 1 TICKET / 120 GEMS", bigButton))
            {
                string chosen = progress.SummonHero();
                lastSummoned = chosen ?? "";
                notice = chosen == null ? "Not enough earned tickets or gems." :
                    "Summoned: " + WorldsCatalog.FindHero(chosen).name;
            }
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
            if (stage == null || stageId > progress.Data.lastClearedStage + 1)
            { notice = "Stage is locked."; return; }
            if (battle != null) Destroy(battle.gameObject);
            battle = new GameObject("Chronicles - Campaign Battle").AddComponent<ChroniclesBattle>();
            battle.Initialize(stage, chosen, progress.Data, OnBattleFinished);
            if (menuCamera != null) menuCamera.enabled = false;
            if (menuAudio != null) menuAudio.enabled = false;
            page = Page.Battle; notice = "";
            if (audio != null) audio.SetState(AudioScene.Battle, stage.chapter);
        }

        private void OnBattleFinished(bool victory)
        {
            lastVictory = victory;
            if (victory) progress.CompleteStage(stageId);
            if (audio != null) audio.SetState(victory ? AudioScene.Victory : AudioScene.Defeat);
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
                CleanupBattle();
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
            page = Page.Home;
            if (audio != null) audio.SetState(AudioScene.Menu);
        }
    }
}
