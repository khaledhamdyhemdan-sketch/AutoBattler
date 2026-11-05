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
            public float burn, burnTick, cursed, frozen, rallied, tauntSeconds, passiveTimer;
            public bool savedOnce;
            public Combatant forcedTarget;
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
                Spawn(hero, 0, i, squad[i].level, false, squad[i].weaponLevel, squad[i].armorLevel, squad[i].stars);
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

        private void Spawn(HeroDefinition hero, int side, int slot, int level, bool boss,
            int weaponLevel = 0, int armorLevel = 0, int stars = 1)
        {
            bool front = slot < 2;
            float x = front ? (slot == 0 ? -1.55f : 1.55f) : (slot - 3) * 2.45f;
            float z = (front ? 1.6f : 3.9f) * (side == 0 ? -1f : 1f);
            Vector3 destination = new Vector3(x, 1.0f, z);
            float tier = hero.rarity == Rarity.Legendary ? 1.20f : 1f;
            float roleHp = hero.role == CombatRole.Tank ? 1.8f : hero.role == CombatRole.Warrior ? 1.35f : 1f;
            float roleAttack = hero.role == CombatRole.Assassin ? 1.4f : hero.role == CombatRole.Mage ? 1.35f : 1f;
            float chapterScale = side == 1 ? 1f + stage.id * .042f : 1f;
            float ascensionScale = 1f + (stars - 1) * .075f;
            Combatant unit = new Combatant
            {
                hero = hero, side = side, level = level, boss = boss,
                maxHp = (230f + level * 28f + armorLevel * 14f) * roleHp * tier * chapterScale * ascensionScale * (boss ? 2.3f : 1f),
                attack = (22f + level * 4.4f + weaponLevel * 2.8f) * roleAttack * tier * chapterScale * ascensionScale * (boss ? 1.3f : 1f),
                armor = 9f + level * 1.3f + armorLevel * 2f + (hero.role == CombatRole.Tank ? 18f : 0f),
                attackCd = .5f + slot * .09f, skillCd = 2.6f + slot * .4f,
                formationPosition = destination,
                arrivalPosition = destination + new Vector3(0f, 0f, side == 0 ? -5f : 5f)
            };
            unit.hp = unit.maxHp;
            unit.model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unit.model.name = hero.name + (boss ? " - Chapter Boss" : "");
            unit.model.transform.SetParent(transform);
            unit.model.transform.position = unit.arrivalPosition;
            unit.model.transform.localScale = Vector3.one * .001f;
            unit.model.GetComponent<Renderer>().sharedMaterial = side == 0 ? OriginMaterial(hero.origin) : redMaterial;
            Collider collider = unit.model.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            GameObject emblem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            emblem.name = "Hero Emblem";
            emblem.transform.SetParent(unit.model.transform, false);
            emblem.transform.localPosition = new Vector3(0f, .90f, 0f);
            emblem.transform.localScale = new Vector3(.45f, .20f, .45f);
            emblem.GetComponent<Renderer>().sharedMaterial = goldMaterial;
            Collider emblemCollider = emblem.GetComponent<Collider>();
            if (emblemCollider != null) Destroy(emblemCollider);
            units.Add(unit);
        }

        private void Update()
        {
            if (ended || Paused) return;
            float dt = Mathf.Min(.05f, Time.deltaTime * Speed);
            if (introTime > 0f)
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
                TickStatuses(f, dt);
                if (!f.Alive) continue;
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
                        f.skillCd *= .85f;
                    CastSkill(f, target);
                    continue;
                }

                float range = f.hero.role == CombatRole.Mage || f.hero.role == CombatRole.Healer ||
                    f.hero.role == CombatRole.Support || f.hero.role == CombatRole.Controller ? 4.5f : 1.6f;
                Vector3 direction = target.model.transform.position - f.model.transform.position;
                direction.y = 0f;
                if (direction.magnitude > range)
                {
                    float moveSpeed = f.frozen > 0f ? 1.0f : 2.1f;
                    f.model.transform.position += direction.normalized *
                        Mathf.Min(moveSpeed * dt, direction.magnitude - range);
                    f.model.transform.LookAt(new Vector3(target.model.transform.position.x,
                        f.model.transform.position.y, target.model.transform.position.z));
                }
                else if (f.attackCd <= 0f)
                {
                    Hit(f, target, f.attack);
                    float baseCooldown = f.hero.role == CombatRole.Assassin ? .85f : 1.25f;
                    if (f.frozen > 0f) baseCooldown *= 1.45f;
                    if (kit != null && kit.passive == HeroPassive.Stormborn) baseCooldown *= .8f;
                    f.attackCd = baseCooldown;
                    f.energy = Mathf.Min(100f, f.energy + (kit != null &&
                        kit.passive == HeroPassive.Enchantress ? 23f : 17f));
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
                if (finished != null)
                    finished(playerAlive && (!enemyAlive || left > right));
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
                Heal(hero, hero.maxHp * .03f);
            }
            else if (kit.passive == HeroPassive.Equilibrium && hero.passiveTimer >= 8f)
            {
                hero.passiveTimer = 0f;
                Shield(LowestFriend(hero), hero.attack * 2f);
            }
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

        private void Floating(string message, Combatant target, Color color)
        {
            if (popups.Count >= 22) popups.RemoveAt(0);
            popups.Add(new FloatingText { text = message, color = color, remaining = .9f,
                world = target.model.transform.position + Vector3.up * 1.6f });
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
                        if (defender.hp < defender.maxHp * .4f) raw *= 1.4f;
                        break;
                    case HeroPassive.SolarFlame:
                        defender.burn = Mathf.Max(defender.burn, 3.5f);
                        break;
                    case HeroPassive.Fury:
                        raw *= 1f + .45f * (1f - attacker.hp / attacker.maxHp);
                        break;
                    case HeroPassive.NightHunter:
                    case HeroPassive.DeathChill:
                        if (defender.cursed > 0f) raw *= 1.28f;
                        break;
                    case HeroPassive.Winterborn:
                        if (defender.frozen > 0f) raw *= 1.3f;
                        break;
                    case HeroPassive.Hunter:
                        if (UnityEngine.Random.value < .22f) raw *= 1.65f;
                        break;
                    case HeroPassive.Thunderlord:
                        if (UnityEngine.Random.value < .12f)
                            defender.stunned = Mathf.Max(defender.stunned, .65f);
                        break;
                }

                foreach (Combatant ally in units)
                {
                    if (ally.side != attacker.side || !ally.Alive) continue;
                    HeroKit allyKit = HeroAbilityCatalog.For(ally.hero.id);
                    if (allyKit != null && (allyKit.passive == HeroPassive.Banner ||
                        allyKit.passive == HeroPassive.Tactician))
                        { raw *= 1.06f; break; }
                }
                if (attacker.rallied > 0f) raw *= 1.22f;
            }

            float damage = raw * 100f / (100f + defender.armor);
            if (defender.hero.role == CombatRole.Tank) damage *= .84f;
            if (defender.cursed > 0f) damage *= 1.2f;
            if (defendingKit != null && (defendingKit.passive == HeroPassive.Guardian ||
                defendingKit.passive == HeroPassive.Ironhide || defendingKit.passive == HeroPassive.Knight))
                damage *= .87f;
            float absorbed = Mathf.Min(defender.shield, damage);
            defender.shield -= absorbed;
            damage = Mathf.Max(0f, damage - absorbed);
            defender.hp = Mathf.Max(0f, defender.hp - damage);
            defender.energy = Mathf.Min(100f, defender.energy + damage * .12f);
            Floating("-" + Mathf.RoundToInt(damage), defender, new Color(1f, .72f, .54f));

            if (attacker != null && attackingKit != null &&
                attackingKit.passive == HeroPassive.Bloodthirst && attacker.Alive)
                Heal(attacker, damage * .12f);

            if (defender.Alive) return;
            if (defendingKit != null &&
                (defendingKit.passive == HeroPassive.Colossus || defendingKit.passive == HeroPassive.Phoenix) &&
                !defender.savedOnce)
            {
                defender.savedOnce = true;
                defender.hp = Mathf.Max(1f, defender.maxHp * .16f);
                Shield(defender, defender.maxHp * .13f);
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
                    defender.hp = defender.maxHp * .23f;
                    Shield(defender, defender.maxHp * .15f);
                    Floating("RESCUED", defender, Color.green);
                    return;
                }
            }
            defender.model.SetActive(false);
            foreach (Combatant unit in units)
            {
                if (!unit.Alive || unit.side == defender.side) continue;
                HeroKit kit = HeroAbilityCatalog.For(unit.hero.id);
                if (kit != null && kit.passive == HeroPassive.Gravekeeper)
                    Shield(unit, unit.attack * .8f);
            }
        }

        private void Heal(Combatant target, float value)
        {
            if (target == null || !target.Alive) return;
            foreach (Combatant teammate in units)
            {
                if (teammate.side != target.side || !teammate.Alive) continue;
                HeroKit kit = HeroAbilityCatalog.For(teammate.hero.id);
                if (kit != null && kit.passive == HeroPassive.Dawnborn) { value *= 1.10f; break; }
            }
            float actual = Mathf.Min(value, target.maxHp - target.hp);
            if (actual <= 0f) return;
            target.hp += actual;
            Floating("+" + Mathf.RoundToInt(actual), target, Color.green);
            HeroKit healedKit = HeroAbilityCatalog.For(target.hero.id);
            if (healedKit != null && healedKit.passive == HeroPassive.Compassion)
                Shield(target, actual * .16f);
        }

        private void Shield(Combatant target, float value)
        {
            if (target == null || !target.Alive) return;
            target.shield = Mathf.Min(target.maxHp * .75f, target.shield + value);
            Floating("SHIELD", target, Color.cyan);
        }

        private void CastSkill(Combatant actor, Combatant target)
        {
            HeroKit kit = HeroAbilityCatalog.For(actor.hero.id);
            if (kit == null) { Hit(actor, target, actor.attack * 1.4f); return; }
            actor.energy = Mathf.Min(100f, actor.energy + 13f);
            CastEffect(actor, target, kit.skill, kit.skillPower, false);
            Floating(kit.skillName, actor, Color.yellow);
        }

        private void CastUltimate(Combatant actor, Combatant target)
        {
            HeroKit kit = HeroAbilityCatalog.For(actor.hero.id);
            if (kit == null) { Hit(actor, target, actor.attack * 3f); return; }
            CastEffect(actor, target, kit.ultimate, kit.ultimatePower, true);
            Floating(kit.ultimateName, actor, new Color(1f, .75f, .22f));
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
                        if (enemy.Alive) enemy.cursed = Mathf.Max(enemy.cursed, ultimate ? 6f : 4.5f);
                    }
                    break;
                case AbilityEffect.LifeDrain:
                    Hit(actor, target, actor.attack * power);
                    Heal(actor, actor.attack * power * .52f);
                    break;
                case AbilityEffect.HealLowest:
                    Heal(LowestFriend(actor), actor.attack * power);
                    break;
                case AbilityEffect.HealTeam:
                    foreach (Combatant ally in units)
                        if (ally.side == actor.side && ally.Alive)
                            Heal(ally, actor.attack * power * .85f);
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
