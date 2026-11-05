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
            public HeroDefinition hero;
            public GameObject model;
            public int side, level;
            public float hp, maxHp, attack, armor, energy, shield, attackCd, skillCd, stunned, flash;
            public bool boss;
            public Vector3 formationPosition, arrivalPosition;
            public bool Alive { get { return hp > 0f; } }
        }
        private sealed class FloatingText
        {
            public string text;
            public Vector3 world;
            public float remaining;
            public Color color;
        }

        private readonly List<Combatant> units = new List<Combatant>();
        private readonly List<FloatingText> popups = new List<FloatingText>();
        private readonly Dictionary<Origin, Material> materials = new Dictionary<Origin, Material>();
        private Material goldMaterial, groundMaterial, redMaterial;
        private Camera cam;
        private StageDefinition stage;
        private Action<bool> finished;
        private bool ended;
        private float elapsed, introTime = 2.8f;
        public bool Paused { get; set; }
        public bool Complete { get { return ended; } }
        public float SecondsLeft { get { return Mathf.Max(0f, 90f - elapsed); } }
        public float Speed { get; set; } = 1f;

        public void Initialize(StageDefinition chosen, IList<OwnedHero> squad, Action<bool> onFinished)
        {
            if (chosen == null || squad == null || squad.Count != 5) throw new ArgumentException("Battle requires stage and five heroes.");
            stage = chosen; finished = onFinished;
            BuildArena();
            for (int i = 0; i < 5; i++)
            {
                HeroDefinition hero = WorldsCatalog.FindHero(squad[i].id);
                if (hero == null) throw new ArgumentException("Unknown hero: " + squad[i].id);
                Spawn(hero, 0, i, squad[i].level, false);
            }
            var opponents = new List<HeroDefinition>();
            Origin origin = WorldsCatalog.Chapters[stage.chapter - 1].origin;
            foreach (var hero in WorldsCatalog.Heroes) if (hero.origin == origin) opponents.Add(hero);
            if (opponents.Count == 0) throw new InvalidOperationException("Chapter needs opponents.");
            for (int i = 0; i < 5; i++)
                Spawn(opponents[(stage.id * 3 + i) % opponents.Count], 1, i, 1 + stage.id / 3, stage.boss && i == 0);
        }

        private Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
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
            if (renderer != null) renderer.sharedMaterial = material;
            Collider hit = go.GetComponent<Collider>();
            if (hit != null) Destroy(hit);
        }

        private void BuildArena()
        {
            goldMaterial = CreateMaterial(new Color(.92f, .66f, .20f));
            groundMaterial = CreateMaterial(new Color(.11f, .10f, .21f));
            redMaterial = CreateMaterial(new Color(.69f, .17f, .30f));
            MakePrimitive("Arena Floor", PrimitiveType.Cube, new Vector3(0f, -.40f, 0f), new Vector3(9f, .5f, 12f), groundMaterial, transform);
            MakePrimitive("Alliance Boundary", PrimitiveType.Cube, new Vector3(0f, -.10f, 0f), new Vector3(8f, .02f, .06f), goldMaterial, transform);
            for (int side = -1; side <= 1; side += 2)
            {
                MakePrimitive("Ruin Pillar", PrimitiveType.Cylinder, new Vector3(side * 4.0f, .8f, 3.9f), new Vector3(.65f, 1.1f, .65f), goldMaterial, transform);
                MakePrimitive("Ruin Pillar", PrimitiveType.Cylinder, new Vector3(side * 4.0f, .8f, -3.9f), new Vector3(.65f, 1.1f, .65f), goldMaterial, transform);
            }

            GameObject cameraObject = new GameObject("Chronicles Portrait Camera");
            cameraObject.transform.SetParent(transform);
            cam = cameraObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.055f, .05f, .10f);
            cam.fieldOfView = 52f;
            cameraObject.transform.position = new Vector3(0f, 11.5f, -13f);
            cameraObject.transform.LookAt(new Vector3(0f, 0f, 0f));
            if (FindFirstObjectByType<AudioListener>() == null) cameraObject.AddComponent<AudioListener>();
            GameObject lightObject = new GameObject("Chronicles Arena Light");
            lightObject.transform.SetParent(transform);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.7f;
            lightObject.transform.rotation = Quaternion.Euler(60f, -35f, 0f);
        }

        private void Spawn(HeroDefinition hero, int side, int slot, int level, bool boss)
        {
            bool front = slot < 2;
            float x = front ? (slot == 0 ? -1.55f : 1.55f) : (slot - 3) * 2.45f;
            float z = (front ? 1.6f : 3.9f) * (side == 0 ? -1f : 1f);
            Vector3 destination = new Vector3(x, 1.0f, z);
            float tier = hero.rarity == Rarity.Legendary ? 1.20f : 1f;
            float roleHp = hero.role == CombatRole.Tank ? 1.8f : hero.role == CombatRole.Warrior ? 1.35f : 1f;
            float roleAttack = hero.role == CombatRole.Assassin ? 1.4f : hero.role == CombatRole.Mage ? 1.35f : 1f;
            float chapterScale = side == 1 ? 1f + stage.id * .042f : 1f;
            Combatant unit = new Combatant
            {
                hero = hero, side = side, level = level, boss = boss,
                maxHp = (230f + level * 28f) * roleHp * tier * chapterScale * (boss ? 2.3f : 1f),
                attack = (22f + level * 4.4f) * roleAttack * tier * chapterScale * (boss ? 1.3f : 1f),
                armor = 9f + level * 1.3f + (hero.role == CombatRole.Tank ? 18f : 0f),
                attackCd = .5f + slot * .09f, skillCd = 2.6f + slot * .4f,
                formationPosition = destination,
                arrivalPosition = destination + new Vector3(0f, 0f, side == 0 ? -5f : 5f)
            };
            unit.hp = unit.maxHp;
            unit.model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unit.model.name = hero.name + (boss ? " - Chapter Boss" : "");
            unit.model.transform.SetParent(transform);
            unit.model.transform.position = unit.arrivalPosition;
            unit.model.transform.localScale = Vector3.zero;
            unit.model.GetComponent<Renderer>().sharedMaterial = side == 0 ? OriginMaterial(hero.origin) : redMaterial;
            Collider collider = unit.model.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            MakePrimitive("Emblem", PrimitiveType.Sphere, destination + Vector3.up, new Vector3(.45f, .20f, .45f),
                goldMaterial, unit.model.transform);
            units.Add(unit);
        }

        private void Update()
        {
            if (ended || Paused) return;
            float dt = Mathf.Min(.05f, Time.deltaTime * Speed);
            if (introTime > 0)
            {
                introTime -= dt;
                float t = Mathf.Clamp01((2.8f - introTime) / 1.9f);
                for (int i = 0; i < units.Count; i++)
                {
                    Combatant f = units[i];
                    float stagger = Mathf.Clamp01(t * 1.6f - (i % 5) * .12f);
                    f.model.transform.position = Vector3.Lerp(f.arrivalPosition, f.formationPosition, stagger);
                    f.model.transform.localScale = Vector3.one * Mathf.Max(.001f, stagger) * (f.boss ? 1.4f : 1f);
                }
                return;
            }
            elapsed += dt;
            foreach (Combatant f in units)
            {
                if (!f.Alive) continue;
                f.attackCd -= dt; f.skillCd -= dt;
                f.stunned = Mathf.Max(0f, f.stunned - dt);
                f.flash = Mathf.Max(0f, f.flash - dt);
                if (f.stunned > 0f) continue;
                Combatant target = FindTarget(f);
                if (target == null) continue;
                if (f.energy >= 100f)
                {
                    f.energy = 0f;
                    CastUltimate(f, target);
                    continue;
                }
                if (f.skillCd <= 0f)
                {
                    f.skillCd = 5f + (f.hero.role == CombatRole.Support ? -1f : 0f);
                    CastSkill(f, target);
                    continue;
                }
                float range = f.hero.role == CombatRole.Mage || f.hero.role == CombatRole.Healer ||
                    f.hero.role == CombatRole.Support || f.hero.role == CombatRole.Controller ? 4.5f : 1.6f;
                Vector3 direction = target.model.transform.position - f.model.transform.position;
                direction.y = 0f;
                if (direction.magnitude > range)
                {
                    f.model.transform.position += direction.normalized * Mathf.Min(2.1f * dt, direction.magnitude - range);
                    f.model.transform.LookAt(new Vector3(target.model.transform.position.x, f.model.transform.position.y,
                        target.model.transform.position.z));
                }
                else if (f.attackCd <= 0f)
                {
                    float multiplier = f.hero.role == CombatRole.Assassin && target.hp < target.maxHp * .35f ? 1.6f : 1f;
                    Hit(f, target, f.attack * multiplier);
                    f.attackCd = f.hero.role == CombatRole.Assassin ? .85f : 1.25f;
                    f.energy = Mathf.Min(100f, f.energy + 17f);
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
                    if (unit.side == 0) left += unit.hp;
                    else right += unit.hp;
                }
                if (finished != null) finished(playerAlive && (!enemyAlive || left > right));
            }
        }

        private Combatant FindTarget(Combatant actor)
        {
            Combatant nearest = null;
            float best = float.MaxValue;
            foreach (var unit in units)
            {
                if (unit.side == actor.side || !unit.Alive) continue;
                float dist = (actor.model.transform.position - unit.model.transform.position).sqrMagnitude;
                if (actor.hero.role == CombatRole.Assassin) dist += 20f * unit.hp / unit.maxHp;
                if (dist < best) { best = dist; nearest = unit; }
            }
            return nearest;
        }

        private Combatant LowestFriend(Combatant actor)
        {
            Combatant found = actor;
            foreach (var unit in units)
                if (unit.side == actor.side && unit.Alive && unit.hp / unit.maxHp < found.hp / found.maxHp)
                    found = unit;
            return found;
        }

        private void Floating(string message, Combatant target, Color color)
        {
            if (popups.Count >= 22) popups.RemoveAt(0);
            popups.Add(new FloatingText { text = message, color = color, remaining = .9f,
                world = target.model.transform.position + Vector3.up * 1.6f });
        }

        private void Hit(Combatant attacker, Combatant defender, float raw)
        {
            if (defender == null || !defender.Alive) return;
            float damage = raw * 100f / (100f + defender.armor);
            if (defender.hero.role == CombatRole.Tank) damage *= .84f;
            float consumed = Mathf.Min(defender.shield, damage);
            defender.shield -= consumed;
            damage = Mathf.Max(0f, damage - consumed);
            defender.hp = Mathf.Max(0f, defender.hp - damage);
            defender.energy = Mathf.Min(100f, defender.energy + damage * .12f);
            Floating("-" + Mathf.RoundToInt(damage), defender, new Color(1f, .72f, .54f));
            if (!defender.Alive) defender.model.SetActive(false);
        }

        private void Heal(Combatant target, float value)
        {
            if (target == null || !target.Alive) return;
            target.hp = Mathf.Min(target.maxHp, target.hp + value);
            Floating("+" + Mathf.RoundToInt(value), target, Color.green);
        }

        private void Shield(Combatant target, float value)
        {
            if (target == null || !target.Alive) return;
            target.shield = Mathf.Min(target.maxHp * .75f, target.shield + value);
            Floating("SHIELD", target, Color.cyan);
        }

        private void CastSkill(Combatant actor, Combatant target)
        {
            actor.energy = Mathf.Min(100f, actor.energy + 12f);
            switch (actor.hero.role)
            {
                case CombatRole.Healer: Heal(LowestFriend(actor), actor.attack * 2.5f); break;
                case CombatRole.Support:
                    foreach (var unit in units) if (unit.side == actor.side && unit.Alive)
                        unit.energy = Mathf.Min(100f, unit.energy + 16f);
                    Shield(LowestFriend(actor), actor.attack * 1.3f); break;
                case CombatRole.Tank: Shield(actor, actor.maxHp * .16f); break;
                case CombatRole.Controller: Hit(actor, target, actor.attack * 1.25f);
                    if (target.Alive) target.stunned = Mathf.Max(target.stunned, 1f); break;
                case CombatRole.Assassin: Hit(actor, target, actor.attack * 1.9f); break;
                case CombatRole.Mage:
                    foreach (var unit in units) if (unit.side != actor.side && unit.Alive)
                        Hit(actor, unit, actor.attack * .65f); break;
                default: Hit(actor, target, actor.attack * 1.6f); break;
            }
            Floating("SKILL", actor, Color.yellow);
        }

        private void CastUltimate(Combatant actor, Combatant target)
        {
            switch (actor.hero.role)
            {
                case CombatRole.Healer:
                    foreach (var unit in units) if (unit.side == actor.side && unit.Alive)
                        Heal(unit, actor.attack * 3.2f);
                    break;
                case CombatRole.Support:
                case CombatRole.Tank:
                    foreach (var unit in units) if (unit.side == actor.side && unit.Alive)
                        Shield(unit, actor.attack * 2.6f);
                    break;
                case CombatRole.Controller:
                    foreach (var unit in units) if (unit.side != actor.side && unit.Alive)
                    {
                        Hit(actor, unit, actor.attack);
                        unit.stunned = Mathf.Max(unit.stunned, 1.4f);
                    }
                    break;
                case CombatRole.Mage:
                    foreach (var unit in units) if (unit.side != actor.side && unit.Alive)
                        Hit(actor, unit, actor.attack * 2f);
                    break;
                default: Hit(actor, target, actor.attack * 4f); break;
            }
            Floating("ULTIMATE!", actor, new Color(1f, .78f, .2f));
        }

        public void DrawWorldLabels(float uiScale)
        {
            if (cam == null || introTime > 0) return;
            foreach (var unit in units)
            {
                if (!unit.Alive) continue;
                Vector3 point = cam.WorldToScreenPoint(unit.model.transform.position + Vector3.up * 2f);
                if (point.z <= 0f) continue;
                float x = point.x / uiScale - 38f;
                float y = (Screen.height - point.y) / uiScale - 15f;
                GUI.Box(new Rect(x, y, 76, 30), "");
                GUI.Label(new Rect(x + 3f, y + 2f, 73f, 16f),
                    unit.hero.name.Length > 11 ? unit.hero.name.Substring(0, 11) : unit.hero.name);
                GUI.color = Color.green;
                GUI.DrawTexture(new Rect(x + 2f, y + 19f, 72f * unit.hp / unit.maxHp, 4f), Texture2D.whiteTexture);
                GUI.color = Color.cyan;
                GUI.DrawTexture(new Rect(x + 2f, y + 25f, 72f * unit.energy / 100f, 3f), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            foreach (var popup in popups)
            {
                Vector3 point = cam.WorldToScreenPoint(popup.world);
                if (point.z <= 0f) continue;
                GUI.color = popup.color;
                GUI.Label(new Rect(point.x / uiScale - 30f, (Screen.height - point.y) / uiScale - 18f, 100f, 30f), popup.text);
                GUI.color = Color.white;
            }
        }

        private void OnDestroy()
        {
            foreach (var item in materials) if (item.Value != null) Destroy(item.Value);
            if (goldMaterial != null) Destroy(goldMaterial);
            if (groundMaterial != null) Destroy(groundMaterial);
            if (redMaterial != null) Destroy(redMaterial);
        }
    }
}
