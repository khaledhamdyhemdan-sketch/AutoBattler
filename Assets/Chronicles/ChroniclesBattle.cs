using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    // Data-driven auto-battle runtime used by the campaign. Supports the entire catalog.
    // Procedural bodies and effects are temporary until approved character rigs/VFX arrive.
    public sealed class ChroniclesBattle : MonoBehaviour
    {
        private sealed class Combatant
        {
            public bool entered;
            public HeroDefinition hero;
            public GameObject model;
            public HeroVisualDriver visual;
            public int side, level;
            public float hp, maxHp, attack, armor, energy, shield, attackCd, skillCd, stunned, flash;
            public float burn, burnTick, cursed, frozen, rallied, tauntSeconds, passiveTimer;
            public float skillFactor = 1f, ultimateFactor = 1f, passiveFactor = 1f;
            public bool savedOnce;
            public Combatant forcedTarget;
            public bool boss;
            public float bossSpecialTimer = 9f;
            public Vector3 formationPosition, arrivalPosition, displayScale;
            public Vector3 panelAnchor;
            public GameObject panelQuad;
            public float panelCardSize = 2.2f;
            public float lungeT, dealt, hitFlashT;
            public Combatant lungeTarget;
            public bool Alive { get { return hp > 0f; } }
        }
        private sealed class FloatingText
        {
            public string text;
            public Vector3 world;
            public float remaining;
            public Color color;
            public bool big;
        }

        private readonly List<Combatant> units = new List<Combatant>();
        private readonly List<FloatingText> popups = new List<FloatingText>();
        private readonly Dictionary<Origin, Material> materials = new Dictionary<Origin, Material>();
        private Material goldMaterial, groundMaterial, redMaterial,
            sandstoneMaterial, sandstoneDarkMaterial;
        private Camera cam;
        private StageDefinition stage;
        private Origin arenaOrigin;
        private Origin? playerPantheon;
        private Action<bool> finished;
        private bool ended, raidMode;
        private float raidDamage;
        private float elapsed, introTime = 2.8f;
        public int RaidDamageScore { get { return Mathf.FloorToInt(
            Mathf.Clamp(raidDamage, 0f, 2000000f)); } }
        public bool Paused { get; set; }
        public bool PanelLayout { get; set; }
        public string BannerOverride { set { bannerText = value ?? string.Empty; } }
        public bool Complete { get { return ended; } }
        public float SecondsLeft { get { return Mathf.Max(0f, 90f - elapsed); } }
        public float Speed { get; set; } = 1f;
        private float shakeT, shakeAmp, hitStopT, slowMoT;
        private Vector3 camBase = new Vector3(0f, 11.5f, -13f);
        private string cutInHero;
        private string cutInSkill;
        private float cutInTime = 99f;
        private bool cutInUltimate;
        private string bannerText = string.Empty;
        private float bannerT = 99f;
        private Texture2D speechBubbleTex;
        private readonly Dictionary<string, Texture2D> cutInPortraits = new Dictionary<string, Texture2D>();
        private GUIStyle cutInNameStyle, cutInSkillStyle, popupBigStyle, bannerStyle, bubbleStyle;
        private Texture2D panelAllyTex, panelEnemyTex;
        private readonly Dictionary<string, Texture2D[]> cutInAnimFrames =
            new Dictionary<string, Texture2D[]>();

        public void Initialize(StageDefinition chosen, IList<OwnedHero> squad,
            PlayerSave savedProgress, Action<bool> onFinished,
            TrialMode mode = TrialMode.Normal, Origin? towerOrigin = null,
            int towerFloor = 0, bool raidBattle = false,
            LabyrinthOption labyrinth = null, int labyrinthBlessings = 0,
            int arenaTrainingRating = -1)
        {
            raidMode = raidBattle;
            raidDamage = 0f;
            if (chosen == null || squad == null || squad.Count != PlayerProgressService.TeamSize) throw new ArgumentException("Battle requires stage and six heroes.");
            stage = chosen; finished = onFinished;
            // Battle-start banner: stage name; tournament rounds say ROUND N.
            bannerText = mode == TrialMode.Normal ?
                (stage.name ?? string.Empty).ToUpperInvariant() :
                mode.ToString().ToUpperInvariant() + " \u2022 " +
                    (stage.name ?? string.Empty).ToUpperInvariant();
            bannerT = 99f;
            arenaOrigin = towerOrigin.HasValue ?
                towerOrigin.Value : WorldsCatalog.Chapters[stage.chapter - 1].origin;
            // Mono-pantheon team: the battle moves to their mythology's stage and
            // the opening banner announces the unity bonus.
            playerPantheon = null;
            if (squad != null && squad.Count >= 5)
            {
                var squadHeroes = new List<HeroDefinition>();
                foreach (OwnedHero owned in squad)
                {
                    HeroDefinition def = WorldsCatalog.FindHero(owned.id);
                    if (def != null) squadHeroes.Add(def);
                }
                if (squadHeroes.Count == squad.Count)
                {
                    TeamSynergyBonus probe = TeamSynergy.Calculate(squadHeroes);
                    if (probe.monoPantheon)
                    {
                        playerPantheon = probe.pantheonOrigin;
                        if (!towerOrigin.HasValue) arenaOrigin = probe.pantheonOrigin;
                        bannerText = "PANTHEON UNITY • " +
                            probe.pantheonOrigin.ToString().ToUpperInvariant();
                    }
                }
            }
            BuildArena();
            for (int i = 0; i < squad.Count; i++)
            {
                HeroDefinition hero = WorldsCatalog.FindHero(squad[i].id);
                if (hero == null) throw new ArgumentException("Unknown hero: " + squad[i].id);
                int actualLevel = ChroniclesGrowth.EffectiveLevel(savedProgress, squad[i]);
                Spawn(hero, 0, i, actualLevel, false, squad[i].weaponLevel,
                    squad[i].armorLevel, squad[i].stars, squad[i].skin);
                Combatant fighter = units[units.Count - 1];
                HeroGrowthStats growth = ChroniclesGrowth.Calculate(savedProgress, squad[i]);
                EquipmentBonuses gear = ChroniclesEquipment.Calculate(
                    savedProgress == null ? null : savedProgress.equipment, hero.id);
                float mercenary = savedProgress != null &&
                    savedProgress.friends != null &&
                    savedProgress.friends.mercenaryPending ?
                    1f + ChroniclesFriends.MercenaryPowerBonus : 1f;
                fighter.attack = (fighter.attack + gear.attack) *
                    growth.attackMultiplier * gear.attackMultiplier * mercenary;
                fighter.maxHp = (fighter.maxHp + gear.health) *
                    growth.hpMultiplier * gear.healthMultiplier * mercenary;
                fighter.armor = (fighter.armor + gear.defense) * growth.defenseMultiplier;
                fighter.hp = fighter.maxHp;
                fighter.skillFactor = growth.skillMultiplier;
                fighter.ultimateFactor = growth.ultimateMultiplier;
                fighter.passiveFactor = growth.passiveMultiplier;
                fighter.attack *= 1f + squad[i].signatureWeaponRank * .03f;
                fighter.skillFactor *= 1f + squad[i].signatureWeaponRank * .035f;
                fighter.ultimateFactor *= 1f + squad[i].signatureWeaponRank * .055f;
            }
            var opponents = new List<HeroDefinition>();
            Origin origin = towerOrigin.HasValue ?
                towerOrigin.Value : WorldsCatalog.Chapters[stage.chapter - 1].origin;
            foreach (var hero in WorldsCatalog.Heroes)
                if (hero.rarity != Rarity.UR &&
                    (hero.origin == origin || (!towerOrigin.HasValue &&
                    origin == Origin.EastAsia &&
                    (hero.origin == Origin.Japan || hero.origin == Origin.China))))
                    opponents.Add(hero);
            if (opponents.Count == 0) throw new InvalidOperationException("Chapter needs opponents.");
            for (int i = 0; i < squad.Count; i++)
            {
                HeroDefinition enemy;
                if (stage.boss && i == 0)
                {
                    ChapterDefinition chapter = WorldsCatalog.Chapters[stage.chapter - 1];
                    enemy = new HeroDefinition("boss_chapter_" + chapter.number,
                        chapter.boss, chapter.origin, CombatRole.Warrior, Rarity.Legendary,
                        "A corrupted guardian of the fractured Loom.");
                }
                else enemy = opponents[(stage.id * 3 + i) % opponents.Count];
                Spawn(enemy, 1, i, 1 + stage.id / 3, stage.boss && i == 0);
            }
            if (mode != TrialMode.Normal)
            {
                // Difficulties reuse the story enemies while keeping battle victory
                // and first-clear reward records independent.
                foreach (Combatant opponent in units)
                {
                    if (opponent.side != 1) continue;
                    opponent.maxHp *= ChroniclesTrials.EnemyHealthMultiplier(mode);
                    opponent.hp = opponent.maxHp;
                    opponent.attack *= ChroniclesTrials.EnemyAttackMultiplier(mode);
                    opponent.armor *= mode == TrialMode.Dark ? 1.5f : 1.25f;
                    opponent.skillCd *= mode == TrialMode.Dark ? .80f : .90f;
                }
            }
            if (towerFloor > 0)
            {
                float hpScale = 1f + (towerFloor - 1) * .09f;
                float attackScale = 1f + (towerFloor - 1) * .07f;
                foreach (Combatant opponent in units)
                {
                    if (opponent.side != 1) continue;
                    opponent.maxHp *= hpScale;
                    opponent.hp = opponent.maxHp;
                    opponent.attack *= attackScale;
                    opponent.armor *= 1f + (towerFloor - 1) * .025f;
                }
            }
            if (raidMode && stage.boss)
            {
                foreach (Combatant opponent in units)
                {
                    if (opponent.side != 1 || !opponent.boss) continue;
                    opponent.maxHp = Mathf.Max(220000f,
                        opponent.maxHp * 18f);
                    opponent.hp = opponent.maxHp;
                    opponent.armor = Mathf.Max(opponent.armor, 92f);
                }
            }
            if (arenaTrainingRating >= 0)
            {
                float arenaScale = ChroniclesArenaTraining.EnemyMultiplier(arenaTrainingRating);
                foreach (Combatant unit in units)
                {
                    if (unit.side != 1) continue;
                    unit.maxHp *= arenaScale;
                    unit.hp = unit.maxHp;
                    unit.attack *= arenaScale;
                    unit.armor *= arenaScale;
                }
            }
            if (labyrinth != null)
            {
                float enemyScale = 1f + (labyrinth.room - 1) * .075f;
                if (labyrinth.kind == LabyrinthRoomKind.Elite) enemyScale *= 1.28f;
                if (labyrinth.kind == LabyrinthRoomKind.Guardian) enemyScale *= 1.55f;
                foreach (Combatant unit in units)
                {
                    if (unit.side == 1)
                    {
                        unit.maxHp *= enemyScale;
                        unit.hp = unit.maxHp;
                        unit.attack *= enemyScale;
                    }
                }
            }
            ApplySynergy();
            CreatePantheonAuras();
            ApplyRelic(savedProgress);
            if (labyrinth != null && labyrinthBlessings > 0)
                foreach (Combatant unit in units)
                    if (unit.side == 0)
                        unit.attack *= 1f + Mathf.Clamp(labyrinthBlessings, 0,
                            ChroniclesLabyrinth.MaxBlessings) *
                            ChroniclesLabyrinth.AttackPerBlessing;
        }

        private void ApplyRelic(PlayerSave saved)
        {
            if (saved == null || saved.relics == null) return;
            WorldRelic relic = ChroniclesRelics.Find(saved.equippedRelicId);
            RelicRank owned = saved.relics.Find(x => x.id == saved.equippedRelicId);
            if (relic == null || owned == null) return;
            float power = 1f + (Math.Max(1, owned.level) - 1) * .22f;
            foreach (Combatant combatant in units)
            {
                if (combatant.side != 0) continue;
                combatant.attack *= 1f + relic.attackBonus * power;
                combatant.maxHp *= 1f + relic.hpBonus * power;
                combatant.hp = combatant.maxHp;
                combatant.energy = Mathf.Min(100f,
                    combatant.energy + relic.startingEnergy * power);
            }
        }

        private void CreatePantheonAuras()
        {
            if (!playerPantheon.HasValue) return;
            Color auraColor;
            switch (playerPantheon.Value)
            {
                case Origin.Egypt: auraColor = new Color(1f, .78f, .25f); break;
                case Origin.Greece: auraColor = new Color(.55f, .75f, 1f); break;
                case Origin.Norse: auraColor = new Color(.55f, .9f, 1f); break;
                case Origin.Japan: auraColor = new Color(1f, .35f, .4f); break;
                case Origin.China: auraColor = new Color(.4f, .95f, .6f); break;
                case Origin.EastAsia: auraColor = new Color(.45f, .95f, .85f); break;
                case Origin.Folklore: auraColor = new Color(.8f, .5f, 1f); break;
                default: auraColor = new Color(1f, .85f, .5f); break;
            }
            Shader shader = ChroniclesShaders.FindAny(
                "Legacy Shaders/Particles/Alpha Blended", "Particles/Alpha Blended",
                "Legacy Shaders/Particles/Additive", "Particles/Additive",
                "Unlit/Transparent", "Unlit/Color");
            if (shader == null) return;
            foreach (Combatant fighter in units)
            {
                if (fighter.side != 0 || fighter.visual == null) continue;
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
                ring.name = "pantheon unity aura";
                Collider ringCollider = ring.GetComponent<Collider>();
                if (ringCollider != null) Destroy(ringCollider);
                ring.transform.SetParent(fighter.visual.transform, false);
                ring.transform.localPosition = new Vector3(0f, .06f, 0f);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                ring.transform.localScale = Vector3.one * 1.6f;
                var material = new Material(shader);
                material.color = new Color(auraColor.r, auraColor.g, auraColor.b, .45f);
                if (material.HasProperty("_TintColor"))
                    material.SetColor("_TintColor",
                        new Color(auraColor.r, auraColor.g, auraColor.b, .4f));
                Renderer ringRenderer = ring.GetComponent<Renderer>();
                if (ringRenderer != null)
                {
                    ringRenderer.sharedMaterial = material;
                    ringRenderer.shadowCastingMode =
                        UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        private void ApplySynergy()
        {
            for (int team = 0; team <= 1; team++)
            {
                var heroes = new List<HeroDefinition>(5);
                foreach (Combatant fighter in units)
                    if (fighter.side == team) heroes.Add(fighter.hero);
                TeamSynergyBonus bonus = TeamSynergy.Calculate(heroes);
                ElementalTeamBonus element = HeroElements.Calculate(heroes);
                bool diplomat = heroes.Exists(h => h.id == "cleopatra");
                foreach (Combatant fighter in units)
                {
                    if (fighter.side != team) continue;
                    // Combined pantheon + element buffs are capped to protect early R/SR heroes.
                    fighter.attack *= Mathf.Min(1.36f,
                        bonus.attackMultiplier * element.attackMultiplier);
                    fighter.maxHp *= Mathf.Min(1.36f,
                        bonus.hpMultiplier * element.healthMultiplier);
                    fighter.hp = fighter.maxHp;
                    fighter.energy = Mathf.Min(100f,
                        bonus.startingEnergy + element.startingEnergy + (diplomat ? 14f : 0f));
                }
                Debug.Log("[Chronicles] Team " + team + " synergy: " + bonus.description +
                    "; elemental: " + element.description);
            }
        }

        private Material CreateMaterial(Color color)
        {
            Shader shader = ChroniclesShaders.FindAny(
                "Universal Render Pipeline/Lit", "Standard", "Sprites/Default");
            if (shader == null) return null; // renderer keeps its default material
            Material material = new Material(shader);
            material.color = color;
            return material;
        }

        private Material OriginMaterial(Origin origin)
        {
            Material mat;
            if (materials.TryGetValue(origin, out mat)) return mat;
            Color c;
            switch (origin)
            {
                case Origin.Egypt: c = new Color(.96f, .70f, .23f); break;
                case Origin.Greece: c = new Color(.50f, .71f, 1f); break;
                case Origin.Norse: c = new Color(.58f, .87f, 1f); break;
                case Origin.EastAsia: c = new Color(1f, .32f, .49f); break;
                case Origin.Japan: c = new Color(.94f, .48f, .68f); break;
                case Origin.China: c = new Color(.35f, .85f, .70f); break;
                case Origin.Folklore: c = new Color(.67f, .47f, .95f); break;
                default: c = new Color(.62f, .92f, .59f); break;
            }
            mat = CreateMaterial(c);
            materials.Add(origin, mat);
            return mat;
        }

        private void MakePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.SetParent(parent);
            go.transform.position = position; go.transform.localScale = scale;
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
            Collider hit = go.GetComponent<Collider>();
            if (hit != null) Destroy(hit);
        }

        private void BuildArena()
        {
            goldMaterial = CreateMaterial(new Color(.92f, .66f, .20f));
            // Warm dusk floor that blends into the battlefield backdrop instead
            // of a flat navy slab.
            groundMaterial = CreateMaterial(new Color(.30f, .23f, .27f));
            if (groundMaterial != null)
                groundMaterial.mainTexture = ArenaFloorTexture();
            redMaterial = CreateMaterial(new Color(.69f, .17f, .30f));
            // Carved sandstone columns: shaft + darker base and capital, so the
            // arena frame reads as ruins, not bare capsules.
            sandstoneMaterial = CreateMaterial(new Color(.58f, .44f, .30f));
            sandstoneDarkMaterial = CreateMaterial(new Color(.40f, .30f, .21f));
            if (!PanelLayout)
            {
            MakePrimitive("Arena Floor", PrimitiveType.Cube, new Vector3(0f, -.40f, 0f), new Vector3(9f, .5f, 12f), groundMaterial, transform);
            MakePrimitive("Alliance Boundary", PrimitiveType.Cube, new Vector3(0f, -.10f, 0f), new Vector3(8f, .02f, .06f), goldMaterial, transform);
            }
            for (int side = -1; !PanelLayout && side <= 1; side += 2)
            {
                for (int end = -1; end <= 1; end += 2)
                {
                    float px = side * 4.0f, pz = end * 3.9f;
                    MakePrimitive("Ruin Pillar Base", PrimitiveType.Cylinder, new Vector3(px, -.02f, pz), new Vector3(.9f, .28f, .9f), sandstoneDarkMaterial, transform);
                    MakePrimitive("Ruin Pillar", PrimitiveType.Cylinder, new Vector3(px, .8f, pz), new Vector3(.65f, 1.1f, .65f), sandstoneMaterial, transform);
                    MakePrimitive("Ruin Pillar Capital", PrimitiveType.Cylinder, new Vector3(px, 1.88f, pz), new Vector3(.85f, .22f, .85f), sandstoneDarkMaterial, transform);
                }
            }

            GameObject cameraObject = new GameObject("Chronicles Portrait Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform);
            cam = cameraObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.055f, .05f, .10f);
            if (PanelLayout)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5.4f;
                cameraObject.transform.position = new Vector3(0f, 0f, -13.8f);
                camBase = cameraObject.transform.position;
                cameraObject.transform.LookAt(new Vector3(0f, 0f, 0f));
            }
            else
            {
            cam.fieldOfView = 50f;
            // Lower, more side-on view: heroes read upright with visible depth
            // between front and back rows (AFK Arena/Omniheroes battle framing).
            cameraObject.transform.position = new Vector3(0f, 9.2f, -13.8f);
            camBase = cameraObject.transform.position;
            cameraObject.transform.LookAt(new Vector3(0f, .8f, .2f));
            }
            AudioListener activeListener = FindFirstObjectByType<AudioListener>();
            if (activeListener == null || !activeListener.isActiveAndEnabled)
                cameraObject.AddComponent<AudioListener>();
            // Authored battlefield backdrop behind the 3D arena (falls back to
            // the solid camera clear color when the art is missing).
            Texture2D battlefield = ChroniclesStageArt.Background(arenaOrigin);
            if (battlefield != null)
            {
                GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
                backdrop.name = "Battlefield Backdrop";
                Collider backdropCollider = backdrop.GetComponent<Collider>();
                if (backdropCollider != null) Destroy(backdropCollider);
                backdrop.transform.SetParent(transform);
                if (PanelLayout)
                {
                    // Panel mode: cover the full orthographic frame edge-to-edge
                    // behind the panels (no empty border), preserving the art's
                    // aspect like a background crop.
                    float halfH = cam.orthographicSize;
                    float halfW = halfH * cam.aspect;
                    float texAspect = (float)battlefield.width / battlefield.height;
                    float coverH = halfH * 2f;
                    float coverW = halfW * 2f;
                    float scaleW = Mathf.Max(coverW, coverH * texAspect) * 1.02f;
                    float scaleH = Mathf.Max(coverH, coverW / texAspect) * 1.02f;
                    backdrop.transform.position = new Vector3(0f, 0f, 18f);
                    backdrop.transform.localScale = new Vector3(scaleW, scaleH, 1f);
                }
                else
                {
                    backdrop.transform.position = new Vector3(0f, -13f, 18f);
                    backdrop.transform.localScale = new Vector3(18f, 33f, 1f);
                }
                Shader backdropShader = ChroniclesShaders.FindAny(
                    "Unlit/Texture", "Sprites/Default",
                    "Universal Render Pipeline/Unlit");
                MeshRenderer backdropRenderer = backdrop.GetComponent<MeshRenderer>();
                if (backdropShader != null && backdropRenderer != null)
                {
                    var backdropMaterial = new Material(backdropShader);
                    backdropMaterial.mainTexture = battlefield;
                    backdropRenderer.material = backdropMaterial;
                }
                // When every lookup fails the quad keeps its default material,
                // which is still a dark backdrop; battle must never fail on this.
            }

            GameObject lightObject = new GameObject("Chronicles Arena Light");
            lightObject.transform.SetParent(transform);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.15f;
            lightObject.transform.rotation = Quaternion.Euler(60f, -35f, 0f);
        }

        private void Spawn(HeroDefinition hero, int side, int slot, int level, bool boss,
            int weaponLevel = 0, int armorLevel = 0, int stars = 1, string skin = "default")
        {
            bool front = slot < 3;
            Vector3 destination;
            float panelCardSizeLocal = 2.2f;
            if (PanelLayout)
            {
                // Card-battle wall per Khalid's mock: a tight 3x2 grid (six
                // per side) on the left ~78% of the frame, sized to the camera
                // aspect so nothing clips on tall phones; the right strip
                // stays free for the HUD rail. Rows slightly overlap.
                int col = slot % 3, row = slot / 3;
                // Clamp to the design aspect: the layout must fit the tallest
                // phones, and the headless capture rig can spawn units at a
                // different aspect than it renders at. 540x960 is the canvas.
                float spawnAspect = cam != null ? cam.aspect : .5625f;
                float effAspect = Mathf.Min(spawnAspect, .5625f);
                float halfW = (cam != null ? cam.orthographicSize : 5.4f) * effAspect;
                float gridW = halfW * 2f * .78f;
                float cardSize = Mathf.Min(2.2f, gridW / 3.1f);
                float x0 = -halfW + cardSize * .55f;
                float px = x0 + col * cardSize * 1.02f;
                float py = side == 1 ? 4.35f - row * cardSize * .92f
                    : -2.55f - row * cardSize * .92f;
                destination = new Vector3(px, py, -0.4f);
                panelCardSizeLocal = cardSize;
            }
            else
            {
            // 3v3 arena rows: three front, three back.
            float x = front ? (slot - 1) * 2.2f : (slot - 4) * 2.45f;
            float z = (front ? 1.6f : 3.9f) * (side == 0 ? -1f : 1f);
            destination = new Vector3(x, 1.0f, z);
            }
            float tier = hero.rarity == Rarity.UR ? 1.30f :
                hero.rarity == Rarity.SSR ? 1.20f :
                hero.rarity == Rarity.SR ? 1.08f : 1f;
            float roleHp = hero.role == CombatRole.Tank ? 1.8f : hero.role == CombatRole.Warrior ? 1.35f : 1f;
            float roleAttack = hero.role == CombatRole.Assassin ? 1.4f : hero.role == CombatRole.Mage ? 1.35f : 1f;
            float chapterScale = side == 1 ? 1f + stage.id * .042f : 1f;
            if (side == 1 && stage.id <= 6)
                // Opening stages must be feel-good wins; the curve rejoins at stage 7.
                chapterScale *= .55f + stage.id * .06f;
            float ascensionScale = 1f + (stars - 1) * .075f;
            Combatant unit = new Combatant
            {
                hero = hero, side = side, level = level, boss = boss,
                maxHp = (230f + level * 28f + armorLevel * 14f) * roleHp * tier * chapterScale * ascensionScale * (boss ? 2.3f : 1f),
                attack = (22f + level * 4.4f + weaponLevel * 2.8f) * roleAttack * tier * chapterScale * ascensionScale * (boss ? 1.3f : 1f),
                armor = 9f + level * 1.3f + armorLevel * 2f + (hero.role == CombatRole.Tank ? 18f : 0f),
                attackCd = .5f + slot * .09f, skillCd = 2.6f + slot * .4f,
                formationPosition = destination,
                panelCardSize = panelCardSizeLocal,
                arrivalPosition = destination + (PanelLayout ?
                    new Vector3(0f, side == 0 ? -4f : 4f, 0f) :
                    new Vector3(0f, 0f, side == 0 ? -5f : 5f)),
                panelAnchor = destination
            };
            unit.hp = unit.maxHp;
            unit.visual = HeroVisualDriver.Spawn(hero, skin, transform,
                side == 0 ? OriginMaterial(hero.origin) : redMaterial);
            unit.model = unit.visual.gameObject;
            unit.model.transform.position = unit.arrivalPosition;
            unit.displayScale = unit.model.transform.localScale;
            // In panel mode the hero art insets inside the card frame.
            if (PanelLayout) unit.displayScale *= .80f;
            unit.model.transform.localScale = unit.displayScale * .001f;
            if (unit.visual.IsPlaceholder)
            {
                GameObject emblem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                emblem.name = "Placeholder Emblem";
                emblem.transform.SetParent(unit.model.transform, false);
                emblem.transform.localPosition = new Vector3(0f, .90f, 0f);
                emblem.transform.localScale = new Vector3(.45f, .20f, .45f);
                emblem.GetComponent<Renderer>().sharedMaterial = goldMaterial;
                Collider emblemCollider = emblem.GetComponent<Collider>();
                if (emblemCollider != null) Destroy(emblemCollider);
            }
            unit.visual.Play(HeroAnimation.Entrance);
            unit.visual.EntranceFX(hero.id);
            if (PanelLayout)
            {
                if (panelAllyTex == null)
                    panelAllyTex = Resources.Load<Texture2D>("Chronicles/UI/panel_battle_ally");
                if (panelEnemyTex == null)
                    panelEnemyTex = Resources.Load<Texture2D>("Chronicles/UI/panel_battle_enemy");
                // Card face is the hero's full splash art (mock look: art
                // fills the card edge-to-edge); the dark ornate panel texture
                // is only a fallback when a hero has no portrait yet.
                Texture2D cardArt = ChroniclesPortraitAssets.Static(hero.id);
                Texture2D panelTex = cardArt != null ? cardArt
                    : (side == 0 ? panelAllyTex : panelEnemyTex);
                if (panelTex != null)
                {
                    int panelRow = slot / 3;
                    float cardScale = unit.panelCardSize;
                    GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = "Panel - " + hero.name;
                    Collider quadCollider = quad.GetComponent<Collider>();
                    if (quadCollider != null) Destroy(quadCollider);
                    quad.transform.SetParent(transform);
                    quad.transform.position = new Vector3(
                        unit.panelAnchor.x, unit.panelAnchor.y, 0.6f - panelRow * .02f);
                    quad.transform.localScale = new Vector3(cardScale, cardScale, 1f);
                    Shader panelShader = ChroniclesShaders.FindAny(
                        "Unlit/Texture", "Sprites/Default",
                        "Universal Render Pipeline/Unlit");
                    MeshRenderer panelRenderer = quad.GetComponent<MeshRenderer>();
                    if (panelShader != null && panelRenderer != null)
                    {
                        var panelMaterial = new Material(panelShader);
                        panelMaterial.mainTexture = panelTex;
                        panelRenderer.material = panelMaterial;
                    }
                    quad.transform.rotation = Quaternion.Euler(-6f, 0f, 0f);
                    unit.panelQuad = quad;
                    // Rarity-colored metallic frame behind the card, parented
                    // to it so attack lunges move card and frame together.
                    GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    frame.name = "Panel Frame - " + hero.name;
                    Collider frameCollider = frame.GetComponent<Collider>();
                    if (frameCollider != null) Destroy(frameCollider);
                    frame.transform.SetParent(quad.transform, false);
                    frame.transform.localPosition = new Vector3(0f, 0f, .035f);
                    frame.transform.localScale = new Vector3(1.07f, 1.07f, 1f);
                    frame.transform.localRotation = Quaternion.identity;
                    Shader frameShader = ChroniclesShaders.FindAny(
                        "Sprites/Default", "Unlit/Transparent",
                        "Universal Render Pipeline/Unlit");
                    MeshRenderer frameRenderer = frame.GetComponent<MeshRenderer>();
                    if (frameShader != null && frameRenderer != null)
                    {
                        var frameMaterial = new Material(frameShader);
                        frameMaterial.mainTexture = PanelFrameTexture(hero.rarity);
                        frameRenderer.material = frameMaterial;
                    }
                }
                // The card face carries the art in panel mode; the 2D battle
                // rig (cutout) would double the portrait, so it stays hidden.
                if (unit.model != null) unit.model.SetActive(false);
            }
            units.Add(unit);
        }

        private static readonly System.Collections.Generic.Dictionary<Rarity, Texture2D>
            panelFrameCache = new System.Collections.Generic.Dictionary<Rarity, Texture2D>();

        private static Texture2D PanelFrameTexture(Rarity rarity)
        {
            Texture2D cached;
            if (panelFrameCache.TryGetValue(rarity, out cached))
                return cached;
            Color col = PanelRarityColor(rarity);
            col.a = 1f;
            const int size = 96, border = 3, innerLine = 1;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            // Solid dark card center, no alpha dependence: renders as a card
            // with a rarity border no matter which unlit shader the runtime has.
            // Slim metallic frame: vertical sheen (bright top, darker bottom)
            // with a darker inner hairline so the frame reads as metal edging,
            // not a flat block of color.
            Color center = new Color(.09f, .07f, .13f, 1f);
            Color hairline = new Color(.03f, .025f, .05f, 1f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < border || x >= size - border ||
                        y < border || y >= size - border;
                    if (edge)
                    {
                        float t = (float)y / (size - 1);
                        float sheen = 1.12f - .5f * t;
                        Color c = col * sheen;
                        c.a = 1f;
                        tex.SetPixel(x, y, c);
                    }
                    else if (x < border + innerLine || x >= size - border - innerLine ||
                        y < border + innerLine || y >= size - border - innerLine)
                    {
                        tex.SetPixel(x, y, hairline);
                    }
                    else
                    {
                        tex.SetPixel(x, y, center);
                    }
                }
            tex.Apply();
            panelFrameCache[rarity] = tex;
            return tex;
        }

        private static Color PanelRarityColor(Rarity rarity)
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

        private static Texture2D arenaFloorTexture;

        // Procedural sandstone tile texture for the arena floor: warm value
        // noise plus darker seams so the ground reads as stone tiling, not a
        // flat colored slab. Deterministic hash noise keeps every run identical.
        private static Texture2D ArenaFloorTexture()
        {
            if (arenaFloorTexture != null) return arenaFloorTexture;
            const int size = 128, tile = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    uint h = (uint)(x * 374761393 + y * 668265263);
                    h = (h ^ (h >> 13)) * 1274126177;
                    float n = ((h ^ (h >> 16)) & 0xffff) / 65535f;
                    float v = .92f + .18f * n; // subtle mottling, avg ~1
                    bool seamX = (x % tile) < 2, seamY = (y % tile) < 2;
                    bool bevelX = (x % tile) == 2, bevelY = (y % tile) == 2;
                    if (seamX || seamY) v = .45f + .1f * n;
                    else if (bevelX || bevelY) v = 1.06f + .06f * n;
                    tex.SetPixel(x, y, new Color(v, v * .96f, v * .92f, 1f));
                }
            tex.Apply();
            arenaFloorTexture = tex;
            return tex;
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.unscaledDeltaTime);
        }

        // Advances the battle by explicit frame times. Update() feeds real Unity
        // time; the editor screenshot rig feeds fixed steps for deterministic
        // headless captures.
        public void Tick(float frameDt, float unscaledDt)
        {
            if (ended || Paused) return;
            float dt = Mathf.Min(.05f, frameDt * Speed);
            if (hitStopT > 0f)
            {
                hitStopT -= unscaledDt;
                dt *= .15f;
            }
            if (cutInTime < 3f) cutInTime += unscaledDt;
            if (slowMoT > 0f)
            {
                slowMoT -= unscaledDt;
                if (slowMoT <= 0f) Time.timeScale = 1f;
            }
            if (PanelLayout && cam != null && cam.orthographic)
            {
                // Subtle pulse under an ultimate cut-in, ease back after.
                // Keep it shallow: the card grid must never clip off-frame
                // (a hard 4.35 zoom cut the enemy top row in half).
                float targetSize = cutInUltimate && cutInTime < 1.75f &&
                    !string.IsNullOrEmpty(cutInHero) ? 5.15f : 5.4f;
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize,
                    Mathf.Clamp01(unscaledDt * 5f));
            }
            if (shakeT > 0f && cam != null)
            {
                shakeT -= unscaledDt;
                Vector2 shakeOffset = UnityEngine.Random.insideUnitCircle * shakeAmp * Mathf.Max(0f, shakeT);
                cam.transform.position = camBase + new Vector3(shakeOffset.x, shakeOffset.y, 0f);
                if (shakeT <= 0f) cam.transform.position = camBase;
            }
            if (introTime > 0f)
            {
                introTime -= dt;
                float t = Mathf.Clamp01((2.8f - introTime) / 1.9f);
                for (int i = 0; i < units.Count; i++)
                {
                    Combatant f = units[i];
                    float stagger = Mathf.Clamp01(t * 1.6f - (i % 5) * .12f);
                    f.model.transform.position = Vector3.Lerp(f.arrivalPosition, f.formationPosition, stagger);
                    f.model.transform.localScale = f.displayScale * Mathf.Max(.001f, stagger) * (f.boss ? 1.4f : 1f);
                    if (PanelLayout && f.panelQuad != null)
                    {
                        // Card flip-in: smoothstep scale with a small overshoot
                        // pop, staggered per card - the grid assembles like a
                        // dealt hand instead of popping in at full size.
                        float ease = stagger * stagger * (3f - 2f * stagger);
                        float pop = 1f + Mathf.Sin(stagger * Mathf.PI) * .07f;
                        float s = Mathf.Max(.001f, ease) * pop;
                        f.panelQuad.transform.localScale = new Vector3(
                            f.panelCardSize * s, f.panelCardSize * s, 1f);
                    }
                    if (stagger >= 1f && !f.entered)
                    {
                        f.entered = true;
                        if (f.hero.rarity == Rarity.UR || f.hero.rarity == Rarity.EX)
                            HeroSkillEffects.Play(f.hero, false, f.model.transform, null);
                    }
                }
                return;
            }

            if (bannerT >= 99f && !string.IsNullOrEmpty(bannerText)) bannerT = 0f;
            if (bannerT < 2.2f) bannerT += dt;

            elapsed += dt;
            foreach (Combatant f in units)
            {
                if (!f.Alive) continue;
                TickStatuses(f, dt);
                if (!f.Alive) continue;
                if (f.boss) TickBossMechanics(f, dt);
                f.attackCd -= dt;
                f.skillCd -= dt;
                if (f.stunned > 0f) continue;
                Combatant target = FindTarget(f);
                if (target == null) continue;

                HeroKit kit = HeroAbilityCatalog.For(f.hero.id);
                if (f.energy >= 100f)
                {
                    f.energy = 0f;
                    CastUltimate(f, target);
                    continue;
                }
                if (f.skillCd <= 0f)
                {
                    f.skillCd = kit == null ? 6f : kit.cooldown;
                    if (kit != null && (kit.passive == HeroPassive.Sage || kit.passive == HeroPassive.Oracle))
                        f.skillCd *= 1f - .15f * PassiveScale(f.hero.rarity);
                    CastSkill(f, target);
                    continue;
                }

                float range = f.hero.role == CombatRole.Mage || f.hero.role == CombatRole.Healer ||
                    f.hero.role == CombatRole.Support || f.hero.role == CombatRole.Controller ? 4.5f : 1.6f;
                if (PanelLayout) range = 999f;
                Vector3 direction = target.model.transform.position - f.model.transform.position;
                direction.y = PanelLayout ? direction.y : 0f;
                if (!PanelLayout && direction.magnitude > range)
                {
                    float moveSpeed = f.frozen > 0f ? 1.0f : 2.1f;
                    f.visual.Play(HeroAnimation.Run);
                    f.model.transform.position += direction.normalized *
                        Mathf.Min(moveSpeed * dt, direction.magnitude - range);
                    f.model.transform.LookAt(new Vector3(target.model.transform.position.x,
                        f.model.transform.position.y, target.model.transform.position.z));
                }
                else if (f.attackCd <= 0f)
                {
                    f.visual.Play(HeroAnimation.Attack);
                    if (PanelLayout) { f.lungeT = .24f; f.lungeTarget = target; }
                    Hit(f, target, f.attack);
                    float baseCooldown = f.hero.role == CombatRole.Assassin ? .85f : 1.25f;
                    if (f.frozen > 0f) baseCooldown *= 1.45f;
                    if (kit != null && kit.passive == HeroPassive.Stormborn && f.rallied > 0f)
                        baseCooldown *= 1f - .2f * PassiveScale(f.hero.rarity);
                    f.attackCd = baseCooldown;
                    f.energy = Mathf.Min(100f, f.energy + (kit != null &&
                        kit.passive == HeroPassive.Enchantress ? 12f + 6f * PassiveScale(f.hero.rarity) : 12f));
                }
                else f.visual.Play(HeroAnimation.Idle);
            }

            if (PanelLayout)
            {
                foreach (Combatant f in units)
                {
                    if (f.panelQuad == null) continue;
                    Vector3 home = f.panelAnchor;
                    // Dead cards gray out and stay put; live cards lunge at
                    // their target (card and frame move together).
                    MeshRenderer cardRenderer = f.panelQuad.GetComponent<MeshRenderer>();
                    if (cardRenderer != null && cardRenderer.material.HasProperty("_Color"))
                        cardRenderer.material.color = f.Alive ? Color.white
                            : new Color(.32f, .32f, .36f, 1f);
                    if (!f.Alive)
                    {
                        f.panelQuad.transform.position = new Vector3(
                            home.x, home.y, f.panelQuad.transform.position.z);
                        if (f.model != null) f.model.transform.position = home;
                        continue;
                    }
                    Vector3 pos = new Vector3(
                        home.x, home.y, f.panelQuad.transform.position.z);
                    if (f.lungeT > 0f && f.lungeTarget != null)
                    {
                        f.lungeT -= dt;
                        float phase = 1f - Mathf.Max(0f, f.lungeT) / .24f;
                        float swing = Mathf.Sin(phase * Mathf.PI);
                        Vector3 dir = f.lungeTarget.panelAnchor - f.panelAnchor;
                        // Reference motion: a short bounce toward the target,
                        // not a long travel across the field.
                        pos += dir.normalized * swing * .28f;
                    }
                    // Hit reaction: brief red flash and jitter on the card.
                    float flash = 0f;
                    if (f.hitFlashT > 0f)
                    {
                        f.hitFlashT = Mathf.Max(0f, f.hitFlashT - dt);
                        flash = f.hitFlashT / .22f;
                        pos.x += Mathf.Sin(f.hitFlashT * 60f) * .07f * flash;
                    }
                    // Idle life: a slow breathing bob, phased per card, so the
                    // lineup sways gently instead of sitting frozen.
                    if (f.lungeT <= 0f && f.hitFlashT <= 0f)
                        pos.y += Mathf.Sin(elapsed * 1.35f +
                            f.panelAnchor.x * 1.1f + f.side * 1.7f) * .035f;
                    if (cardRenderer != null && cardRenderer.material.HasProperty("_Color"))
                        cardRenderer.material.color = Color.Lerp(
                            Color.white, new Color(1f, .5f, .45f, 1f), flash * .85f);
                    f.panelQuad.transform.position = pos;
                    // The hidden rig stays the logical anchor for projectiles
                    // and damage popups.
                    if (f.model != null)
                        f.model.transform.position = pos;
                }
            }

            for (int i = popups.Count - 1; i >= 0; i--)
            {
                popups[i].remaining -= dt;
                popups[i].world += Vector3.up * dt * .5f;
                if (popups[i].remaining <= 0f) popups.RemoveAt(i);
            }
            bool playerAlive = units.Exists(x => x.side == 0 && x.Alive);
            bool enemyAlive = units.Exists(x => x.side == 1 && x.Alive);
            if (!playerAlive || !enemyAlive || elapsed >= 90f)
            {
                ended = true;
                float left = 0f, right = 0f;
                foreach (var unit in units)
                {
                    if (unit.side == 0) left += unit.hp / unit.maxHp;
                    else right += unit.hp / unit.maxHp;
                }
                bool won = playerAlive && (!enemyAlive || left > right);
                foreach (Combatant unit in units)
                    if (unit.Alive && unit.side == (won ? 0 : 1))
                        unit.visual.Play(HeroAnimation.Victory);
                if (finished != null) finished(won);
            }
        }

        private void TickStatuses(Combatant hero, float dt)
        {
            hero.stunned = Mathf.Max(0f, hero.stunned - dt);
            hero.cursed = Mathf.Max(0f, hero.cursed - dt);
            hero.frozen = Mathf.Max(0f, hero.frozen - dt);
            hero.rallied = Mathf.Max(0f, hero.rallied - dt);
            hero.tauntSeconds = Mathf.Max(0f, hero.tauntSeconds - dt);
            if (hero.tauntSeconds <= 0f) hero.forcedTarget = null;
            hero.passiveTimer += dt;
            if (hero.burn > 0f)
            {
                hero.burn = Mathf.Max(0f, hero.burn - dt);
                hero.burnTick += dt;
                if (hero.burnTick >= 1f)
                {
                    hero.burnTick -= 1f;
                    Hit(null, hero, Mathf.Max(6f, hero.maxHp * .018f));
                }
            }

            HeroKit kit = HeroAbilityCatalog.For(hero.hero.id);
            if (kit == null || !hero.Alive) return;
            if (kit.passive == HeroPassive.Rebirth && hero.passiveTimer >= 4f)
            {
                hero.passiveTimer = 0f;
                Heal(hero, hero.maxHp * .03f * PassiveScale(hero.hero.rarity));
            }
            else if (kit.passive == HeroPassive.Equilibrium && hero.passiveTimer >= 8f)
            {
                hero.passiveTimer = 0f;
                Shield(LowestFriend(hero), hero.attack * 2f * PassiveScale(hero.hero.rarity));
            }
        }

        private void TickBossMechanics(Combatant boss, float dt)
        {
            boss.bossSpecialTimer -= dt;
            if (boss.bossSpecialTimer > 0f) return;
            boss.bossSpecialTimer = 9f + stage.chapter * .75f;
            Combatant target = FindTarget(boss);
            if (target == null) return;
            // Each chapter guardian introduces a different, visibly labeled tactical challenge.
            switch (stage.chapter)
            {
                case 1:
                    CastEffect(boss, target, AbilityEffect.WeakeningCurse, 2.2f, true);
                    Floating("ERASED NAMES", boss, Color.magenta);
                    break;
                case 2:
                    foreach (Combatant enemy in units)
                    {
                        if (enemy.side == boss.side || !enemy.Alive) continue;
                        enemy.energy = Mathf.Max(0f, enemy.energy - 32f);
                    }
                    CastEffect(boss, target, AbilityEffect.StunStrike, 2f, true);
                    Floating("BROKEN PROPHECY", boss, Color.magenta);
                    break;
                case 3:
                    CastEffect(boss, target, AbilityEffect.ShieldTeam, 3f, true);
                    CastEffect(boss, target, AbilityEffect.FrostStrike, 1.8f, true);
                    Floating("ROOTS OF WINTER", boss, Color.cyan);
                    break;
                case 4:
                    CastEffect(boss, target, AbilityEffect.BurningWave, 2.5f, true);
                    Floating("MIRROR FLAME", boss, Color.yellow);
                    break;
                case 5:
                    CastEffect(boss, target, AbilityEffect.TauntGuard, 2.5f, true);
                    CastEffect(boss, target, AbilityEffect.TripleStrike, 1.6f, true);
                    Floating("HOLLOW OATH", boss, Color.magenta);
                    break;
                default:
                    CastEffect(boss, target, AbilityEffect.Eclipse, 3.0f, true);
                    CastEffect(boss, target, AbilityEffect.EnergizeTeam, 1.5f, true);
                    Floating("UNWRITTEN FATE", boss, Color.magenta);
                    break;
            }
            boss.visual.Play(HeroAnimation.Ultimate);
            CutIn(boss, string.Empty);
            HeroSkillEffects.Play(boss.hero, true, boss.model.transform,
                target == null ? null : target.model.transform);
        }

        private Combatant FindTarget(Combatant actor)
        {
            if (actor.tauntSeconds > 0f && actor.forcedTarget != null && actor.forcedTarget.Alive)
                return actor.forcedTarget;
            Combatant nearest = null;
            float best = float.MaxValue;
            foreach (Combatant unit in units)
            {
                if (unit.side == actor.side || !unit.Alive) continue;
                float dist = (actor.model.transform.position - unit.model.transform.position).sqrMagnitude;
                if (actor.hero.role == CombatRole.Assassin)
                    dist += 20f * unit.hp / unit.maxHp;
                if (dist < best) { best = dist; nearest = unit; }
            }
            return nearest;
        }

        private Combatant WeakestEnemy(Combatant actor)
        {
            Combatant weakest = null;
            foreach (Combatant unit in units)
                if (unit.side != actor.side && unit.Alive &&
                    (weakest == null || unit.hp / unit.maxHp < weakest.hp / weakest.maxHp))
                    weakest = unit;
            return weakest;
        }

        private Combatant LowestFriend(Combatant actor)
        {
            Combatant found = actor;
            foreach (var unit in units)
                if (unit.side == actor.side && unit.Alive && unit.hp / unit.maxHp < found.hp / found.maxHp)
                    found = unit;
            return found;
        }

        private void Floating(string message, Combatant target, Color color, bool big = false)
        {
            if (popups.Count >= 22) popups.RemoveAt(0);
            popups.Add(new FloatingText { text = message, color = color, big = big, remaining = .9f,
                world = target.model.transform.position + Vector3.up * 1.6f });
        }

        // Passive strength scales with rarity: SR is baseline, R weaker,
        // SSR and UR stronger, so higher-tier heroes feel better per tier.
        private static float PassiveScale(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.UR: case Rarity.EX: case Rarity.SSRP: return 1.45f;
                case Rarity.SSR: return 1.2f;
                case Rarity.SR: return 1f;
                default: return .8f;
            }
        }

        private void Hit(Combatant attacker, Combatant defender, float raw)
        {
            if (defender == null || !defender.Alive) return;
            HeroKit attackingKit = attacker == null ? null : HeroAbilityCatalog.For(attacker.hero.id);
            HeroKit defendingKit = HeroAbilityCatalog.For(defender.hero.id);
            if (defendingKit != null &&
                (defendingKit.passive == HeroPassive.Evasion || defendingKit.passive == HeroPassive.Trickster) &&
                UnityEngine.Random.value < .15f)
            {
                Floating("DODGE", defender, Color.cyan);
                return;
            }

            if (attacker != null && attackingKit != null)
            {
                switch (attackingKit.passive)
                {
                    case HeroPassive.Executioner:
                        if (defender.hp < defender.maxHp * .4f) raw *= 1f + .4f * PassiveScale(attacker.hero.rarity);
                        break;
                    case HeroPassive.SolarFlame:
                        defender.burn = Mathf.Max(defender.burn, 3.5f * PassiveScale(attacker.hero.rarity));
                        break;
                    case HeroPassive.Fury:
                        raw *= 1f + .45f * PassiveScale(attacker.hero.rarity) * (1f - attacker.hp / attacker.maxHp);
                        break;
                    case HeroPassive.NightHunter:
                    case HeroPassive.DeathChill:
                        if (defender.cursed > 0f) raw *= 1f + .28f * PassiveScale(attacker.hero.rarity);
                        break;
                    case HeroPassive.Winterborn:
                        if (defender.frozen > 0f) raw *= 1f + .3f * PassiveScale(attacker.hero.rarity);
                        break;
                    case HeroPassive.Hunter:
                        if (UnityEngine.Random.value < .22f) raw *= 1f + .65f * PassiveScale(attacker.hero.rarity);
                        break;
                    case HeroPassive.Thunderlord:
                        if (UnityEngine.Random.value < .12f)
                            defender.stunned = Mathf.Max(defender.stunned, .65f * PassiveScale(attacker.hero.rarity));
                        break;
                }

                foreach (Combatant ally in units)
                {
                    if (ally.side != attacker.side || !ally.Alive) continue;
                    HeroKit allyKit = HeroAbilityCatalog.For(ally.hero.id);
                    if (allyKit != null && (allyKit.passive == HeroPassive.Banner ||
                        allyKit.passive == HeroPassive.Tactician))
                        { raw *= 1f + .06f * PassiveScale(ally.hero.rarity); break; }
                }
                if (attacker.rallied > 0f) raw *= 1.22f;
            }

            if (attacker != null)
                raw *= HeroElements.DamageMultiplier(attacker.hero.element,
                    defender.hero.element);
            defender.visual.Play(HeroAnimation.Hit);
            float damage = raw * 100f / (100f + defender.armor);
            if (defender.hero.role == CombatRole.Tank) damage *= .84f;
            if (defender.cursed > 0f) damage *= 1.2f;
            if (defendingKit != null && (defendingKit.passive == HeroPassive.Guardian ||
                defendingKit.passive == HeroPassive.Ironhide || defendingKit.passive == HeroPassive.Knight))
                damage *= 1f - .13f * PassiveScale(defender.hero.rarity);
            // Critical strikes: 12% chance, 1.6x damage, big gold popup (genre standard).
            bool crit = UnityEngine.Random.value < .12f;
            if (crit)
            {
                damage *= 1.6f;
                shakeT = Mathf.Max(shakeT, .14f);
                shakeAmp = Mathf.Max(shakeAmp, .16f);
            }
            float absorbed = Mathf.Min(defender.shield, damage);
            defender.shield -= absorbed;
            damage = Mathf.Max(0f, damage - absorbed);
            float actualDamage = Mathf.Min(defender.hp, damage);
            if (raidMode && defender.side == 1 && defender.boss &&
                attacker != null && attacker.side == 0)
                raidDamage += actualDamage;
            defender.hp = Mathf.Max(0f, defender.hp - actualDamage);
            if (actualDamage > 0f) defender.hitFlashT = .22f;
            if (attacker != null) attacker.dealt += actualDamage;
            defender.energy = Mathf.Min(100f, defender.energy + actualDamage * .09f);
            if (crit)
                Floating("CRIT -" + Mathf.RoundToInt(damage), defender,
                    new Color(1f, .84f, .2f), true);
            else
                Floating("-" + Mathf.RoundToInt(damage), defender, new Color(1f, .72f, .54f));
            if (ChroniclesAudioDirector.Instance != null)
                ChroniclesAudioDirector.Instance.PlayEffect(
                    crit || actualDamage >= defender.maxHp * .22f ? "hit_heavy" : "hit");

            if (attacker != null && attackingKit != null &&
                attackingKit.passive == HeroPassive.Bloodthirst && attacker.Alive)
                Heal(attacker, damage * .12f * PassiveScale(attacker.hero.rarity));

            if (defender.Alive) return;
            if (defendingKit != null &&
                (defendingKit.passive == HeroPassive.Colossus || defendingKit.passive == HeroPassive.Phoenix) &&
                !defender.savedOnce)
            {
                defender.savedOnce = true;
                defender.hp = Mathf.Max(1f, defender.maxHp * .16f * PassiveScale(defender.hero.rarity));
                Shield(defender, defender.maxHp * .13f * PassiveScale(defender.hero.rarity));
                Floating("LAST STAND", defender, Color.yellow);
                return;
            }
            foreach (Combatant ally in units)
            {
                if (ally == defender || ally.side != defender.side || !ally.Alive || ally.savedOnce) continue;
                HeroKit allyKit = HeroAbilityCatalog.For(ally.hero.id);
                if (allyKit != null && allyKit.passive == HeroPassive.GuardianAngel)
                {
                    ally.savedOnce = true;
                    defender.hp = defender.maxHp * .23f * PassiveScale(ally.hero.rarity);
                    Shield(defender, defender.maxHp * .15f * PassiveScale(ally.hero.rarity));
                    Floating("RESCUED", defender, Color.green);
                    return;
                }
            }
            defender.visual.Play(HeroAnimation.Death);
            hitStopT = Mathf.Max(hitStopT, .32f);
            if (defender.visual.IsPlaceholder) defender.model.SetActive(false);
            foreach (Combatant unit in units)
            {
                if (!unit.Alive || unit.side == defender.side) continue;
                HeroKit kit = HeroAbilityCatalog.For(unit.hero.id);
                if (kit != null && kit.passive == HeroPassive.Gravekeeper)
                    Shield(unit, unit.attack * .8f * PassiveScale(unit.hero.rarity));
            }
        }

        private void Heal(Combatant target, float value, Combatant healer = null)
        {
            if (target == null || !target.Alive) return;
            foreach (Combatant teammate in units)
            {
                if (teammate.side != target.side || !teammate.Alive) continue;
                HeroKit kit = HeroAbilityCatalog.For(teammate.hero.id);
                if (kit != null && kit.passive == HeroPassive.Dawnborn) { value *= 1f + .10f * PassiveScale(teammate.hero.rarity); break; }
            }
            float actual = Mathf.Min(value, target.maxHp - target.hp);
            if (actual <= 0f) return;
            target.hp += actual;
            Floating("+" + Mathf.RoundToInt(actual), target, Color.green);
            if (ChroniclesAudioDirector.Instance != null)
                ChroniclesAudioDirector.Instance.PlayEffect("heal");
            HeroKit healerKit = healer == null ? null : HeroAbilityCatalog.For(healer.hero.id);
            if (healerKit != null && healerKit.passive == HeroPassive.Compassion)
                Shield(target, actual * .16f * PassiveScale(healer.hero.rarity));
        }

        private void Shield(Combatant target, float value)
        {
            if (target == null || !target.Alive) return;
            target.shield = Mathf.Min(target.maxHp * .75f, target.shield + value);
            Floating("SHIELD", target, Color.cyan);
            if (ChroniclesAudioDirector.Instance != null)
                ChroniclesAudioDirector.Instance.PlayEffect("shield");
        }

        private void CutIn(Combatant actor, string skillName)
        {
            CutIn(actor, skillName, false);
        }

        private void CutIn(Combatant actor, string skillName, bool ultimate)
        {
            cutInHero = actor.hero.id;
            cutInSkill = skillName;
            cutInUltimate = ultimate;
            cutInTime = 0f;
            hitStopT = .16f;
            if (ultimate)
            {
                // Cinematic beat: the world drops into slow motion under the takeover.
                slowMoT = .95f;
                Time.timeScale = .42f;
            }
            shakeT = .28f;
            shakeAmp = .35f;
        }

        private void CastSkill(Combatant actor, Combatant target)
        {
            HeroKit kit = HeroAbilityCatalog.For(actor.hero.id);
            if (kit == null) { Hit(actor, target, actor.attack * 1.4f); return; }
            actor.energy = Mathf.Min(100f, actor.energy + 9f);
            if (kit.passive == HeroPassive.Stormborn)
                actor.rallied = Mathf.Max(actor.rallied, 3.2f);
            actor.visual.Play(HeroAnimation.Skill);
            HeroSkillEffects.Play(actor.hero, false,
                actor.model.transform, target == null ? null : target.model.transform);
            CastEffect(actor, target, kit.skill, kit.skillPower * actor.skillFactor, false);
            Floating(kit.skillName, actor, Color.yellow);
            if (ChroniclesAudioDirector.Instance != null)
                ChroniclesAudioDirector.Instance.PlayEffect("skill");
        }

        private void CastUltimate(Combatant actor, Combatant target)
        {
            HeroKit kit = HeroAbilityCatalog.For(actor.hero.id);
            if (kit == null) { Hit(actor, target, actor.attack * 3f); return; }
            actor.visual.Play(HeroAnimation.Ultimate);
            CutIn(actor, kit != null ? kit.ultimateName : string.Empty, true);
            HeroSkillEffects.Play(actor.hero, true,
                actor.model.transform, target == null ? null : target.model.transform);
            if (ChroniclesAudioDirector.Instance != null)
                ChroniclesAudioDirector.Instance.HeroUltimate(actor.hero.id);
            CastEffect(actor, target, kit.ultimate, kit.ultimatePower * actor.ultimateFactor, true);
            Floating(kit.ultimateName, actor, new Color(1f, .75f, .22f), true);
        }

        private void CastEffect(Combatant actor, Combatant target, AbilityEffect effect, float power, bool ultimate)
        {
            switch (effect)
            {
                case AbilityEffect.Strike:
                    Hit(actor, target, actor.attack * power);
                    break;
                case AbilityEffect.HuntWeakest:
                    Hit(actor, WeakestEnemy(actor), actor.attack * power);
                    break;
                case AbilityEffect.Cleave:
                    int hitCount = 0;
                    foreach (Combatant enemy in units)
                    {
                        if (enemy.side == actor.side || !enemy.Alive) continue;
                        Hit(actor, enemy, actor.attack * power * .74f);
                        if (++hitCount >= 3) break;
                    }
                    break;
                case AbilityEffect.AreaBlast:
                    foreach (Combatant enemy in units)
                        if (enemy.side != actor.side && enemy.Alive)
                            Hit(actor, enemy, actor.attack * power * .72f);
                    break;
                case AbilityEffect.StunStrike:
                    Hit(actor, target, actor.attack * power);
                    if (target.Alive) target.stunned = Mathf.Max(target.stunned, 1.1f);
                    break;
                case AbilityEffect.FrostStrike:
                    Hit(actor, target, actor.attack * power);
                    if (target.Alive)
                    {
                        target.frozen = Mathf.Max(target.frozen, 3.6f);
                        target.stunned = Mathf.Max(target.stunned, .65f);
                    }
                    break;
                case AbilityEffect.BurningWave:
                    foreach (Combatant enemy in units)
                    {
                        if (enemy.side == actor.side || !enemy.Alive) continue;
                        Hit(actor, enemy, actor.attack * power * .60f);
                        if (enemy.Alive) enemy.burn = Mathf.Max(enemy.burn, 4f);
                    }
                    break;
                case AbilityEffect.WeakeningCurse:
                    foreach (Combatant enemy in units)
                    {
                        if (enemy.side == actor.side || !enemy.Alive) continue;
                        if (!ultimate && enemy != target) continue;
                        Hit(actor, enemy, actor.attack * power * (ultimate ? .6f : 1f));
                        if (enemy.Alive)
                        {
                            HeroKit curseKit = HeroAbilityCatalog.For(actor.hero.id);
                            float curseDuration = ultimate ? 6f : 4.5f;
                            if (curseKit != null && curseKit.passive == HeroPassive.Sorcery)
                                curseDuration *= 1f + .3f * PassiveScale(actor.hero.rarity);
                            enemy.cursed = Mathf.Max(enemy.cursed, curseDuration);
                        }
                    }
                    break;
                case AbilityEffect.LifeDrain:
                    Hit(actor, target, actor.attack * power);
                    Heal(actor, actor.attack * power * .52f);
                    break;
                case AbilityEffect.HealLowest:
                    Heal(LowestFriend(actor), actor.attack * power, actor);
                    break;
                case AbilityEffect.HealTeam:
                    foreach (Combatant ally in units)
                        if (ally.side == actor.side && ally.Alive)
                            Heal(ally, actor.attack * power * .85f, actor);
                    break;
                case AbilityEffect.ShieldSelf:
                    Shield(actor, actor.maxHp * .12f * power);
                    break;
                case AbilityEffect.ShieldTeam:
                    foreach (Combatant ally in units)
                        if (ally.side == actor.side && ally.Alive)
                            Shield(ally, actor.attack * power * 1.25f);
                    break;
                case AbilityEffect.EnergizeTeam:
                    foreach (Combatant ally in units)
                        if (ally.side == actor.side && ally.Alive)
                            ally.energy = Mathf.Min(100f, ally.energy + power * 11f);
                    break;
                case AbilityEffect.TauntGuard:
                    Shield(actor, actor.maxHp * .14f * power);
                    foreach (Combatant enemy in units)
                        if (enemy.side != actor.side && enemy.Alive)
                        {
                            enemy.forcedTarget = actor;
                            enemy.tauntSeconds = Mathf.Max(enemy.tauntSeconds, 2.8f);
                        }
                    break;
                case AbilityEffect.TripleStrike:
                    for (int i = 0; i < 3; i++)
                    {
                        Combatant hitTarget = target.Alive ? target : WeakestEnemy(actor);
                        if (hitTarget != null) Hit(actor, hitTarget, actor.attack * power * .77f);
                    }
                    break;
                case AbilityEffect.Execution:
                    Combatant weakest = WeakestEnemy(actor);
                    if (weakest != null)
                        Hit(actor, weakest, actor.attack * power *
                            (weakest.hp <= weakest.maxHp * .5f ? 1.3f : 1f));
                    break;
                case AbilityEffect.Rally:
                    foreach (Combatant ally in units)
                        if (ally.side == actor.side && ally.Alive)
                        {
                            ally.rallied = Mathf.Max(ally.rallied, 3.5f + power);
                            ally.energy = Mathf.Min(100f, ally.energy + power * 5f);
                        }
                    break;
                case AbilityEffect.Eclipse:
                    foreach (Combatant enemy in units)
                        if (enemy.side != actor.side && enemy.Alive)
                        {
                            Hit(actor, enemy, actor.attack * power * .62f);
                            if (enemy.Alive)
                            {
                                enemy.stunned = Mathf.Max(enemy.stunned, ultimate ? 1.1f : .4f);
                                if (actor.hero.role == CombatRole.Controller)
                                    enemy.cursed = Mathf.Max(enemy.cursed, 3.8f);
                            }
                        }
                    break;
            }
        }

        public struct DamageEntry { public string heroId; public int amount; public bool ally; }

        public void FillDamageMeter(List<DamageEntry> output)
        {
            output.Clear();
            foreach (Combatant u in units)
            {
                if (u.dealt <= 0f) continue;
                output.Add(new DamageEntry {
                    heroId = u.hero.id, amount = Mathf.RoundToInt(u.dealt),
                    ally = u.side == 0 });
            }
            output.Sort((a, b) => b.amount.CompareTo(a.amount));
        }

        public void DrawWorldLabels(float uiScale)
        {
            if (cam == null || introTime > 0) return;
            foreach (var unit in units)
            {
                if (!unit.Alive) continue;
                if (PanelLayout)
                {
                    // Bars overlay the card's bottom edge like the approved
                    // mock: LV badge bottom-left, HP + energy bars across the
                    // card width.
                    Vector3 anchor = cam.WorldToScreenPoint(
                        unit.panelAnchor + new Vector3(0f, -(unit.panelCardSize * .5f - .04f), 0f));
                    if (anchor.z <= 0f) continue;
                    float px = anchor.x / uiScale, py = (Screen.height - anchor.y) / uiScale;
                    float halfW = cam.orthographicSize * cam.aspect;
                    float cardPx = unit.panelCardSize / (2f * halfW) * (Screen.width / uiScale);
                    float barW = cardPx * .66f;
                    float barX = px - cardPx * .5f + cardPx * .22f;
                    // Illustrated plate behind the bars (Khalid: art, not flat plates)
                    Texture2D plate = OrnateGuiSkin.Art("bar_plate");
                    if (plate != null)
                        GUI.DrawTexture(new Rect(px - cardPx * .5f - 2f, py - 17f, cardPx + 4f, 32f), plate);
                    // level badge: rarity-colored ring circle + number
                    Color rarity = ChroniclesGame.RarityFrameColor(unit.hero.rarity);
                    Texture2D badgeRing = OrnateGuiSkin.Frame(
                        "panel-badge-" + unit.side + "-" + (int)unit.hero.rarity,
                        rarity, new Color(.08f, .06f, .12f, .95f), 28, 3);
                    if (badgeRing != null)
                        GUI.DrawTexture(new Rect(px - cardPx * .5f - 1f, py - 22f, 28f, 28f), badgeRing);
                    GUI.color = new Color(1f, .95f, .8f);
                    GUI.Label(new Rect(px - cardPx * .5f - 1f, py - 18f, 28f, 20f), unit.level.ToString());
                    // Inset dark channels, then enamel gradient fills with
                    // rounded caps - art bars, not flat color strips.
                    Texture2D channel = OrnateGuiSkin.BarChannel("panel-bar-channel");
                    if (channel != null)
                    {
                        GUI.color = Color.white;
                        GUI.DrawTexture(new Rect(barX - 1f, py - 10f, barW + 2f, 8f), channel);
                        GUI.DrawTexture(new Rect(barX - 1f, py - 2f, barW + 2f, 6f), channel);
                    }
                    Color hpColor = unit.side == 0
                        ? new Color(.28f, .82f, .34f)
                        : new Color(.92f, .26f, .20f);
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(barX, py - 9f,
                        barW * Mathf.Clamp01(unit.hp / unit.maxHp), 6f),
                        OrnateGuiSkin.StatBar(unit.side == 0 ? "hp-ally" : "hp-enemy", hpColor));
                    // Energy: gold enamel; a pulsing ready-glow when the
                    // ultimate is charged (professional gacha "ult ready" cue).
                    if (unit.energy >= 100f)
                        OrnateGuiSkin.PulseGlow(new Rect(barX - 2f, py - 4f, barW + 4f, 9f),
                            new Color(1f, .85f, .3f), 3.2f);
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(barX, py - 1f,
                        barW * Mathf.Clamp01(unit.energy / 100f), 4f),
                        OrnateGuiSkin.StatBar("energy-gold", new Color(1f, .76f, .22f)));
                    GUI.color = Color.white;
                    continue;
                }
                Vector3 point = cam.WorldToScreenPoint(unit.model.transform.position + Vector3.up * 2f);
                if (point.z <= 0f) continue;
                float x = point.x / uiScale - 38f;
                float y = (Screen.height - point.y) / uiScale - 15f;
                // Illustrated name plate instead of a flat code box.
                Texture2D unitPlate = OrnateGuiSkin.Art("bar_plate");
                Color oldC = GUI.color;
                if (unitPlate != null)
                {
                    GUI.color = new Color(.14f, .12f, .26f, .95f);
                    GUI.DrawTexture(new Rect(x - 4f, y - 3f, 84f, 36f),
                        unitPlate, ScaleMode.StretchToFill);
                }
                else
                {
                    GUI.color = new Color(.05f, .045f, .10f, .92f);
                    GUI.DrawTexture(new Rect(x, y, 76f, 30f),
                        Texture2D.whiteTexture);
                }
                GUI.color = ChroniclesGame.RarityFrameColor(unit.hero.rarity);
                GUI.Label(new Rect(x + 3f, y + 2f, 73f, 16f),
                    unit.hero.name.Length > 11 ? unit.hero.name.Substring(0, 11) : unit.hero.name);
                // Bars sit in a dark inset channel, not floating strips.
                GUI.color = new Color(0f, 0f, 0f, .65f);
                GUI.DrawTexture(new Rect(x + 1f, y + 18f, 74f, 6f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x + 1f, y + 24f, 74f, 5f), Texture2D.whiteTexture);
                GUI.color = Color.green;
                GUI.DrawTexture(new Rect(x + 2f, y + 19f, 72f * unit.hp / unit.maxHp, 4f), Texture2D.whiteTexture);
                GUI.color = Color.cyan;
                GUI.DrawTexture(new Rect(x + 2f, y + 25f, 72f * unit.energy / 100f, 3f), Texture2D.whiteTexture);
                GUI.color = oldC;
            }
            foreach (var popup in popups)
            {
                Vector3 point = cam.WorldToScreenPoint(popup.world);
                if (point.z <= 0f) continue;
                GUI.color = popup.color;
                if (popupBigStyle == null)
                {
                    popupBigStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                }
                if (popup.big)
                    GUI.Label(new Rect(point.x / uiScale - 140f, (Screen.height - point.y) / uiScale - 24f, 280f, 48f), popup.text, popupBigStyle);
                else
                    GUI.Label(new Rect(point.x / uiScale - 30f, (Screen.height - point.y) / uiScale - 18f, 100f, 30f), popup.text);
                GUI.color = Color.white;
            }
            DrawCutIn();
            DrawBanner();
        }

        private void DrawBanner()
        {
            if (bannerT >= 2.2f || string.IsNullOrEmpty(bannerText)) return;
            float t = bannerT / 2.2f;
            float inT = Mathf.Clamp01(t / .22f); inT = inT * inT * (3f - 2f * inT);
            float outT = Mathf.Clamp01((t - .78f) / .22f); outT = outT * outT * (3f - 2f * outT);
            float a = inT * (1f - outT);
            if (bannerStyle == null)
            {
                bannerStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 46, fontStyle = FontStyle.BoldAndItalic,
                    alignment = TextAnchor.MiddleCenter };
                bannerStyle.normal.textColor = new Color(1f, .92f, .6f);
            }
            float x = Mathf.Lerp(-560f, 0f, inT) + 560f * outT;
            // dark sweep plate behind the text
            GUI.color = new Color(0f, 0f, 0f, .45f * a);
            GUI.DrawTexture(new Rect(0f, 415f, 540f, 90f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, .85f, .4f, .9f * a);
            GUI.DrawTexture(new Rect(0f, 415f, 540f, 3f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, 502f, 540f, 3f), Texture2D.whiteTexture);
            GUI.color = new Color(0f, 0f, 0f, a);
            GUI.Label(new Rect(x + 3f, 423f, 540f, 80f), bannerText, bannerStyle);
            GUI.color = new Color(1f, .92f, .6f, a);
            GUI.Label(new Rect(x, 420f, 540f, 80f), bannerText, bannerStyle);
            GUI.color = Color.white;
        }

        private void DrawCutIn()
        {
            if (cutInNameStyle == null)
            {
                cutInNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
                cutInNameStyle.normal.textColor = new Color(1f, .87f, .55f);
                cutInSkillStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
            }
            bool takeoverCheck = false;
            {
                HeroDefinition checkHero = WorldsCatalog.FindHero(cutInHero);
                takeoverCheck = checkHero != null && (checkHero.rarity == Rarity.UR ||
                    checkHero.rarity == Rarity.EX || checkHero.rarity == Rarity.SSRP);
            }
            float cutInDuration = (cutInUltimate && takeoverCheck) ? 1.75f : 1.15f;
            if (cutInTime >= cutInDuration || string.IsNullOrEmpty(cutInHero)) return;
            float t = cutInTime / cutInDuration;
            float inT = Mathf.Clamp01(t / .18f); inT = inT * inT * (3f - 2f * inT);
            float outT = Mathf.Clamp01((t - .8f) / .2f); outT = outT * outT * (3f - 2f * outT);
            float a = inT * (1f - outT);
            HeroDefinition cutHero = WorldsCatalog.FindHero(cutInHero);
            Color ec = cutHero != null ? HeroSkillEffects.ColorFor(cutHero.element) : new Color(1f, .75f, .3f);
            bool premiumCut = cutHero != null &&
                (cutHero.rarity == Rarity.UR || cutHero.rarity == Rarity.EX ||
                cutHero.rarity == Rarity.SSRP);
            // SSR+ signature: violet-silver; UR: gold; EX: prismatic white-gold
            Color edgeColor = cutHero != null && cutHero.rarity == Rarity.SSRP ?
                new Color(.78f, .65f, 1f) :
                premiumCut ? new Color(1f, .85f, .5f) : ec;
            bool takeover = cutInUltimate && premiumCut;
            // subtle full-screen flash: gold for premium rarities, element otherwise
            GUI.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b,
                (takeover ? .30f : premiumCut ? .22f : .16f) * (1f - t));
            GUI.DrawTexture(new Rect(0f, 0f, 540f, 960f), Texture2D.whiteTexture);
            if (takeover)
            {
                // Full-screen ultimate takeover: dark veil + expanding tier ring
                Matrix4x4 ringKeep = GUI.matrix;
                GUI.color = new Color(0f, 0f, 0f, .55f * a);
                GUI.DrawTexture(new Rect(0f, 0f, 540f, 960f), Texture2D.whiteTexture);
                float ringR = 90f + 620f * t;
                int ringSegs = 28;
                for (int i = 0; i < ringSegs; i++)
                {
                    float ang = i * 6.28318f / ringSegs;
                    float segFade = .5f + .5f * Mathf.Sin(ang * 3f + t * 9f);
                    GUI.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b,
                        .5f * segFade * a);
                    GUIUtility.RotateAroundPivot(ang * 57.2958f, new Vector2(270f, 480f));
                    GUI.DrawTexture(new Rect(270f - 2f, 480f - ringR, 4f, 26f + 30f * segFade),
                        Texture2D.whiteTexture);
                }
                GUI.matrix = ringKeep;
                // Short animated ultimate clip for heroes with frame art
                // (charge -> strike): plays full-screen over the veil.
                Texture2D[] frames;
                if (!cutInAnimFrames.TryGetValue(cutInHero, out frames))
                {
                    frames = ChroniclesPortraitAssets.AnimatedFrames(cutInHero);
                    // End the clip on the hero's ultimate splash art when it exists
                    Texture2D ultArt = Resources.Load<Texture2D>(
                        "Chronicles/Poses/" + cutInHero + "_ultimate");
                    if (ultArt != null)
                    {
                        int n = frames != null ? frames.Length : 0;
                        Texture2D[] withUlt = new Texture2D[n + 3];
                        if (n > 0) frames.CopyTo(withUlt, 0);
                        withUlt[n] = ultArt; withUlt[n + 1] = ultArt; withUlt[n + 2] = ultArt;
                        frames = withUlt;
                    }
                    cutInAnimFrames[cutInHero] = frames;
                }
                if (frames != null && frames.Length > 1)
                {
                    int fi = Mathf.Clamp((int)(t * frames.Length * .92f),
                        0, frames.Length - 1);
                    Texture2D frame = frames[fi];
                    if (frame != null)
                    {
                        // cover-crop to the full screen, preserving aspect
                        float scrAr = 540f / 960f;
                        float texAr = (float)frame.width / frame.height;
                        Rect uv = new Rect(0f, 0f, 1f, 1f);
                        if (texAr > scrAr)
                        {
                            float w = scrAr / texAr;
                            uv = new Rect((1f - w) * .5f, 0f, w, 1f);
                        }
                        else
                        {
                            float h = texAr / scrAr;
                            uv = new Rect(0f, (1f - h) * .5f, 1f, h);
                        }
                        GUI.color = new Color(1f, 1f, 1f, a);
                        GUI.DrawTextureWithTexCoords(
                            new Rect(0f, 0f, 540f, 960f), frame, uv);
                    }
                }
            }
            // diagonal slash band with element edge lines and speed lines
            Matrix4x4 keepMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(-9f, new Vector2(270f, 470f));
            GUI.color = new Color(0f, 0f, 0f, .8f * a);
            GUI.DrawTexture(new Rect(-60f, 392f, 660f, 196f), Texture2D.whiteTexture);
            GUI.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, .85f * a);
            GUI.DrawTexture(new Rect(-60f, 392f, 660f, 4f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(-60f, 584f, 660f, 4f), Texture2D.whiteTexture);
            if (premiumCut)
            {
                GUI.color = new Color(1f, .85f, .5f, .5f * a);
                GUI.DrawTexture(new Rect(-60f, 399f, 660f, 2f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(-60f, 579f, 660f, 2f), Texture2D.whiteTexture);
            }
            for (int i = 0; i < 4; i++)
            {
                float lineY = 412f + i * 42f + 14f * Mathf.Sin(i * 2.1f);
                float lineW = 210f + 80f * i;
                GUI.color = new Color(ec.r, ec.g, ec.b, (.38f - .07f * i) * a);
                GUI.DrawTexture(new Rect(560f - lineW + 26f * i, lineY, lineW, 3f), Texture2D.whiteTexture);
            }
            GUI.matrix = keepMatrix;
            // hero splash: full-body cutout when one exists, else static portrait
            // comic speech bubble with the skill name near the splash
            if (!string.IsNullOrEmpty(cutInSkill))
            {
                if (speechBubbleTex == null)
                    speechBubbleTex = Resources.Load<Texture2D>("Chronicles/UI/speech_bubble");
                if (bubbleStyle == null)
                {
                    bubbleStyle = new GUIStyle(GUI.skin.label) {
                        fontSize = 17, fontStyle = FontStyle.BoldAndItalic,
                        alignment = TextAnchor.MiddleCenter };
                    bubbleStyle.normal.textColor = new Color(.1f, .08f, .14f);
                }
                float bubA = Mathf.Clamp01((t - .1f) / .12f) * (1f - outT);
                if (bubA > 0f)
                {
                    GUI.color = new Color(1f, 1f, 1f, bubA);
                    if (speechBubbleTex != null)
                        GUI.DrawTexture(new Rect(250f, 128f, 250f, 140f), speechBubbleTex);
                    GUI.color = new Color(.1f, .08f, .14f, bubA);
                    GUI.Label(new Rect(258f, 150f, 234f, 84f), cutInSkill, bubbleStyle);
                    GUI.color = Color.white;
                }
            }
            Texture2D portrait;
            string portraitKey = cutInHero + (cutInUltimate ? "_u" : "_s");
            if (!cutInPortraits.TryGetValue(portraitKey, out portrait))
            {
                // Prefer the hero's generated skill/ultimate splash pose
                portrait = Resources.Load<Texture2D>("Chronicles/Poses/" + cutInHero +
                    (cutInUltimate ? "_ultimate" : "_skill"));
                if (portrait == null)
                    portrait = Resources.Load<Texture2D>("Chronicles/HomeCutouts/" + cutInHero);
                if (portrait == null)
                    portrait = Resources.Load<Texture2D>("Chronicles/Portraits/Static/" + cutInHero);
                cutInPortraits[portraitKey] = portrait;
            }
            if (portrait != null)
            {
                float slide = Mathf.Lerp(-430f, 0f, inT) - 390f * outT;
                float maxW = takeover ? 540f : 470f, maxH = takeover ? 800f : 700f;
                float ar = (float)portrait.width / portrait.height;
                float ph = maxH, pw = ph * ar;
                if (pw > maxW) { pw = maxW; ph = pw / ar; }
                Rect fit = new Rect(-70f + slide, 170f + (maxH - ph), pw, ph);
                float fadeW = Mathf.Min(110f, fit.width * .35f);
                float fadeH = Mathf.Min(100f, fit.height * .2f);
                float bodyW = fit.width - fadeW, bodyH = fit.height - fadeH;
                GUI.color = new Color(1f, 1f, 1f, a);
                // opaque body (left + bottom chunk)
                GUI.DrawTextureWithTexCoords(
                    new Rect(fit.x, fit.y + fadeH, bodyW, bodyH), portrait,
                    new Rect(0f, 0f, bodyW / fit.width, bodyH / fit.height));
                // right-edge fade: vertical strips with falling alpha
                for (int i = 0; i < 5; i++)
                {
                    float sx = bodyW + i * fadeW / 5f;
                    GUI.color = new Color(1f, 1f, 1f, a * (1f - (i + 1f) / 5f));
                    GUI.DrawTextureWithTexCoords(
                        new Rect(fit.x + sx, fit.y, fadeW / 5f, fit.height), portrait,
                        new Rect(sx / fit.width, 0f, fadeW / 5f / fit.width, 1f));
                }
                // top-edge fade over the body region
                for (int i = 0; i < 4; i++)
                {
                    float sy = fadeH - (i + 1f) * fadeH / 4f;
                    GUI.color = new Color(1f, 1f, 1f, a * (1f - (i + 1f) / 4f));
                    GUI.DrawTextureWithTexCoords(
                        new Rect(fit.x, fit.y + sy, bodyW, fadeH / 4f), portrait,
                        new Rect(0f, (fit.height - sy - fadeH / 4f) / fit.height,
                            bodyW / fit.width, fadeH / 4f / fit.height));
                }
            }
            if (cutHero != null)
            {
                if (premiumCut)
                {
                    cutInSkillStyle.normal.textColor = Color.white;
                    GUI.color = new Color(0f, 0f, 0f, .9f * a);
                    GUI.Label(new Rect(298f, 396f, 240f, 26f),
                        cutHero.rarity == Rarity.EX ? "* EXCLUSIVE *" :
                        cutHero.rarity == Rarity.SSRP ? "* SSR+ *" : "* ULTRA RARE *", cutInSkillStyle);
                    GUI.color = new Color(1f, .85f, .5f, a);
                    GUI.Label(new Rect(296f, 394f, 240f, 26f),
                        cutHero.rarity == Rarity.EX ? "* EXCLUSIVE *" :
                        cutHero.rarity == Rarity.SSRP ? "* SSR+ *" : "* ULTRA RARE *", cutInSkillStyle);
                }
                string cutName = cutHero.name.ToUpperInvariant();
                GUI.color = new Color(0f, 0f, 0f, .9f * a);
                GUI.Label(new Rect(298f, 426f, 240f, 52f), cutName, cutInNameStyle);
                GUI.Label(new Rect(298f, 478f, 240f, 36f), string.IsNullOrEmpty(cutInSkill) ? "" : cutInSkill.ToUpperInvariant(), cutInSkillStyle);
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(296f, 424f, 240f, 52f), cutName, cutInNameStyle);
                if (!string.IsNullOrEmpty(cutInSkill))
                {
                    cutInSkillStyle.normal.textColor = ec;
                    GUI.Label(new Rect(296f, 476f, 240f, 36f), cutInSkill.ToUpperInvariant(), cutInSkillStyle);
                }
            }
            GUI.color = Color.white;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            foreach (var item in materials) if (item.Value != null) Destroy(item.Value);
            if (goldMaterial != null) Destroy(goldMaterial);
            if (groundMaterial != null) Destroy(groundMaterial);
            if (redMaterial != null) Destroy(redMaterial);
            if (sandstoneMaterial != null) Destroy(sandstoneMaterial);
            if (sandstoneDarkMaterial != null) Destroy(sandstoneDarkMaterial);
        }
    }
}
