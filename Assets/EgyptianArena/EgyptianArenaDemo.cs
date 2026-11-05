using System;
using System.Collections.Generic;
using UnityEngine;

namespace EgyptianArena
{
    // Standalone playable combat prototype: no changes to the original AutoBattler scenes.
    public class EgyptianArenaDemo : MonoBehaviour
    {
        public enum Hero { Horus, Anubis, Ra, Sobek, Set, Isis, Sekhmet, Nephthys, Bastet, Maat }
        public enum Role { Tank, Warrior, Assassin, Mage, Healer, Support, Control }

        [Serializable]
        public sealed class HeroData
        {
            public Hero id;
            public string displayName;
            public bool female;
            public Role role;
            public int hp, attack, armor;
            public float attackSpeed, range, skillCooldown;
            public Color color;
            public string skill, ultimate;
            public HeroData(Hero id, string name, bool female, Role role, int hp, int attack, int armor,
                float speed, float range, float cooldown, Color color, string skill, string ultimate)
            {
                this.id = id; displayName = name; this.female = female; this.role = role;
                this.hp = hp; this.attack = attack; this.armor = armor;
                attackSpeed = speed; this.range = range; skillCooldown = cooldown;
                this.color = color; this.skill = skill; this.ultimate = ultimate;
            }
        }

        [Header("Prototype")]
        [SerializeField] private bool startAutomatically = true;
        [SerializeField, Range(0.25f, 3f)] private float battleSpeed = 1f;
        [SerializeField] private float durationSeconds = 90f;
        // Initial balance values: these are NOT final production numbers.
        public static readonly HeroData[] Roster =
        {
            new HeroData(Hero.Horus, "HORUS", false, Role.Assassin, 1150, 120, 12, 1.20f, 1.7f, 6.0f, new Color(.93f,.75f,.32f), "Falcon Strike", "Falcon Dive"),
            new HeroData(Hero.Anubis, "ANUBIS", false, Role.Tank, 1850, 65, 31, .78f, 1.8f, 7f, new Color(.29f,.36f,.53f), "Soul Barrier", "Judgement"),
            new HeroData(Hero.Ra, "RA", false, Role.Mage, 1100, 110, 9, .92f, 5.2f, 6f, new Color(1f,.55f,.11f), "Solar Flare", "Sunburst"),
            new HeroData(Hero.Sobek, "SOBEK", false, Role.Tank, 2050, 76, 34, .73f, 1.7f, 7f, new Color(.23f,.60f,.48f), "River Guard", "Crocodile Rampage"),
            new HeroData(Hero.Set, "SET", false, Role.Warrior, 1470, 105, 19, 1.05f, 1.8f, 6.5f, new Color(.69f,.31f,.30f), "Sand Rend", "Desert Tempest"),
            new HeroData(Hero.Isis, "ISIS", true, Role.Healer, 1180, 69, 12, .91f, 5f, 5.2f, new Color(.43f,.76f,.95f), "Healing Light", "Wings of Life"),
            new HeroData(Hero.Sekhmet, "SEKHMET", true, Role.Warrior, 1390, 124, 16, 1.13f, 1.7f, 6f, new Color(.95f,.31f,.20f), "Lioness Claw", "Wrath of Sekhmet"),
            new HeroData(Hero.Nephthys, "NEPHTHYS", true, Role.Control, 1150, 94, 11, .9f, 5f, 6.8f, new Color(.59f,.39f,.84f), "Night Curse", "Silent Night"),
            new HeroData(Hero.Bastet, "BASTET", true, Role.Assassin, 1040, 124, 11, 1.32f, 1.7f, 6f, new Color(.97f,.64f,.75f), "Twin Claws", "Moon Hunt"),
            new HeroData(Hero.Maat, "MAAT", true, Role.Support, 1290, 77, 19, .89f, 5f, 6.5f, new Color(.76f,.83f,.99f), "Scales of Order", "Divine Balance")
        };

        private sealed class Fighter
        {
            public HeroData data;
            public int team, attacks;
            public GameObject body;
            public float health, shield, energy, attackCd, skillCd, stun, silence, slow, burn, curse, dodge, taunt, passiveCd;
            public Fighter forcedTarget;
            public bool rescueSpent;
            public bool Alive { get { return health > 0f; } }
        }

        private readonly List<Fighter> fighters = new List<Fighter>(10);
        private readonly List<string> events = new List<string>();
        private Camera arenaCamera;
        private float elapsed;
        private bool running, paused;
        private string result = "READY";

        private void Start()
        {
            MakeArena();
            ResetMatch();
            if (startAutomatically) BeginMatch();
        }

        private static Material Mat(Color color)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Sprites/Default");
            Material m = new Material(s);
            m.color = color;
            return m;
        }

        private void MakeArena()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Egyptian Arena - Prototype Floor";
            floor.transform.SetParent(transform);
            floor.transform.position = new Vector3(0f, -.30f, 0f);
            floor.transform.localScale = new Vector3(20f, .5f, 12f);
            floor.GetComponent<Renderer>().sharedMaterial = Mat(new Color(.13f, .11f, .20f));

            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "Center Divider";
            line.transform.SetParent(transform);
            line.transform.position = Vector3.zero;
            line.transform.localScale = new Vector3(.08f, .05f, 11.7f);
            line.GetComponent<Renderer>().sharedMaterial = Mat(new Color(.87f, .70f, .31f));

            arenaCamera = Camera.main;
            if (arenaCamera == null)
            {
                GameObject cameraObject = new GameObject("Egyptian Arena Camera");
                cameraObject.tag = "MainCamera";
                arenaCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            arenaCamera.transform.position = new Vector3(0f, 17f, -17f);
            arenaCamera.transform.LookAt(Vector3.zero);
            arenaCamera.fieldOfView = 55f;
            arenaCamera.backgroundColor = new Color(.055f, .045f, .09f);
            if (FindFirstObjectByType<Light>() == null)
            {
                GameObject sun = new GameObject("Demo Sun");
                Light light = sun.AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1.5f;
                sun.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            }
        }

        private void ResetMatch()
        {
            foreach (Fighter old in fighters)
                if (old.body != null) Destroy(old.body);
            fighters.Clear(); events.Clear(); running = false; paused = false;
            elapsed = 0f; result = "READY";

            Hero[] men = { Hero.Sobek, Hero.Anubis, Hero.Set, Hero.Horus, Hero.Ra };
            Hero[] women = { Hero.Sekhmet, Hero.Bastet, Hero.Nephthys, Hero.Isis, Hero.Maat };
            for (int i = 0; i < 5; i++)
            {
                Spawn(men[i], 0, i);
                Spawn(women[i], 1, i);
            }
            Log("10 Egyptian heroes loaded | 5 vs 5");
        }

        private void Spawn(Hero id, int team, int index)
        {
            HeroData data = Roster[(int)id];
            Fighter f = new Fighter();
            f.data = data; f.team = team; f.health = data.hp;
            f.attackCd = .2f + index * .12f; f.skillCd = 1.5f + index * .45f;
            float x = index <= 1 ? 3f : index == 2 ? 4.1f : 5.2f;
            Vector3 pos = new Vector3(team == 0 ? -x : x, 1.0f, (index - 2f) * 1.9f);

            f.body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            f.body.name = data.displayName + (team == 0 ? " - Men" : " - Women");
            f.body.transform.SetParent(transform);
            f.body.transform.position = pos;
            f.body.GetComponent<Renderer>().sharedMaterial = Mat(data.color);
            Destroy(f.body.GetComponent<Collider>());

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "Headpiece placeholder";
            crown.transform.SetParent(f.body.transform, false);
            crown.transform.localPosition = new Vector3(0f, .92f, 0f);
            crown.transform.localScale = new Vector3(.65f, .22f, .65f);
            crown.GetComponent<Renderer>().sharedMaterial = Mat(new Color(.95f, .77f, .30f));
            Destroy(crown.GetComponent<Collider>());
            fighters.Add(f);
        }

        private void BeginMatch()
        {
            elapsed = 0f; running = true; paused = false; result = "";
            Log("BATTLE STARTED - automatic combat");
        }

        private void Update()
        {
            if (!running || paused) return;
            float dt = Time.deltaTime * battleSpeed;
            elapsed += dt;
            foreach (Fighter f in fighters)
            {
                if (!f.Alive) continue;
                TickStatus(f, dt);
                if (!f.Alive) continue;
                f.attackCd -= dt; f.skillCd -= dt;
                if (f.stun > 0f) continue;
                Fighter target = FindTarget(f);
                if (target == null) continue;

                if (f.energy >= 100f && f.silence <= 0f)
                {
                    f.energy = 0f;
                    Ultimate(f, target);
                    continue;
                }
                if (f.skillCd <= 0f && f.silence <= 0f)
                {
                    f.skillCd = f.data.skillCooldown;
                    Skill(f, target);
                    continue;
                }
                Vector3 delta = target.body.transform.position - f.body.transform.position;
                delta.y = 0;
                if (delta.magnitude > f.data.range)
                {
                    float speed = f.slow > 0f ? 1.2f : 2.6f;
                    f.body.transform.position += delta.normalized * Mathf.Min(delta.magnitude - .9f * f.data.range, speed * dt);
                }
                else if (f.attackCd <= 0f)
                {
                    Basic(f, target);
                    f.attackCd = 1f / (f.data.attackSpeed * (f.slow > 0f ? .7f : 1f));
                }
            }
            CheckResult();
        }

        private void TickStatus(Fighter f, float dt)
        {
            f.stun = Mathf.Max(0f, f.stun - dt);
            f.silence = Mathf.Max(0f, f.silence - dt);
            f.slow = Mathf.Max(0f, f.slow - dt);
            f.curse = Mathf.Max(0f, f.curse - dt);
            f.dodge = Mathf.Max(0f, f.dodge - dt);
            f.taunt = Mathf.Max(0f, f.taunt - dt);
            if (f.taunt <= 0f) f.forcedTarget = null;
            if (f.burn > 0f)
            {
                f.burn = Mathf.Max(0f, f.burn - dt);
                Damage(null, f, 18f * dt, true);
            }
            f.passiveCd += dt;
            if (f.data.id == Hero.Maat && f.passiveCd > 9f)
            {
                f.passiveCd = 0f;
                Shield(LowestAlly(f), 85f);
            }
        }

        private List<Fighter> Team(int team)
        {
            List<Fighter> resultList = new List<Fighter>();
            foreach (Fighter f in fighters)
                if (f.Alive && f.team == team) resultList.Add(f);
            return resultList;
        }

        private Fighter LowestAlly(Fighter actor)
        {
            Fighter resultFighter = null;
            foreach (Fighter f in fighters)
                if (f.Alive && f.team == actor.team &&
                    (resultFighter == null || f.health / f.data.hp < resultFighter.health / resultFighter.data.hp))
                    resultFighter = f;
            return resultFighter;
        }

        private Fighter WeakestEnemy(Fighter actor)
        {
            Fighter resultFighter = null;
            foreach (Fighter f in fighters)
                if (f.Alive && f.team != actor.team && (resultFighter == null || f.health < resultFighter.health))
                    resultFighter = f;
            return resultFighter;
        }

        private Fighter FindTarget(Fighter actor)
        {
            if (actor.taunt > 0f && actor.forcedTarget != null && actor.forcedTarget.Alive)
                return actor.forcedTarget;
            Fighter chosen = null;
            float score = float.MaxValue;
            foreach (Fighter candidate in fighters)
            {
                if (!candidate.Alive || candidate.team == actor.team) continue;
                float distance = Vector3.SqrMagnitude(actor.body.transform.position - candidate.body.transform.position);
                float rating = actor.data.role == Role.Assassin
                    ? distance * .08f + 20f * candidate.health / candidate.data.hp : distance;
                if (rating < score) { score = rating; chosen = candidate; }
            }
            return chosen;
        }

        private void Basic(Fighter f, Fighter target)
        {
            f.attacks++;
            float amount = f.data.attack;
            if (f.data.id == Hero.Horus && target.health < target.data.hp * .4f) amount *= 1.35f;
            if (f.data.id == Hero.Sekhmet) amount *= 1f + .7f * (1f - f.health / f.data.hp);
            if (f.data.id == Hero.Set && f.attacks % 3 == 0) amount *= 1.65f;
            if (f.data.id == Hero.Bastet && UnityEngine.Random.value < .22f) amount *= 1.8f;
            Damage(f, target, amount);
            f.energy = Mathf.Min(100f, f.energy + 15f);
            if (f.data.id == Hero.Ra && target.Alive) target.burn = Mathf.Max(target.burn, 2.5f);
        }

        private void Skill(Fighter f, Fighter target)
        {
            if (target == null) return;
            f.energy = Mathf.Min(100f, f.energy + 18f);
            Log(f.data.displayName + " skill: " + f.data.skill);
            switch (f.data.id)
            {
                case Hero.Horus: Damage(f, WeakestEnemy(f), f.data.attack * 1.55f); break;
                case Hero.Anubis: Shield(LowestAlly(f), 260f); Shield(f, 145f); break;
                case Hero.Ra:
                    foreach (Fighter enemy in Team(1 - f.team))
                    { Damage(f, enemy, 70f); enemy.burn = Mathf.Max(enemy.burn, 4f); }
                    break;
                case Hero.Sobek:
                    Shield(f, 280f);
                    foreach (Fighter enemy in Team(1 - f.team))
                    { enemy.taunt = 3.5f; enemy.forcedTarget = f; }
                    break;
                case Hero.Set:
                    Damage(f, target, f.data.attack * 1.55f);
                    if (target.Alive) target.slow = Mathf.Max(target.slow, 3f);
                    break;
                case Hero.Isis: Heal(LowestAlly(f), 290f); Shield(LowestAlly(f), 95f); break;
                case Hero.Sekhmet: Damage(f, target, f.data.attack * 1.75f); Heal(f, 85f); break;
                case Hero.Nephthys:
                    Damage(f, target, 86f);
                    if (target.Alive) { target.curse = 5f; target.silence = Mathf.Max(target.silence, 2.2f); }
                    break;
                case Hero.Bastet:
                    Damage(f, WeakestEnemy(f), f.data.attack * 1.9f);
                    f.dodge = Mathf.Max(f.dodge, 3f);
                    break;
                case Hero.Maat:
                    Fighter ally = LowestAlly(f);
                    Shield(ally, 205f);
                    if (ally != null) { ally.stun = 0f; ally.silence = 0f; }
                    break;
            }
        }

        private void Ultimate(Fighter f, Fighter target)
        {
            Log(f.data.displayName + " ULTIMATE: " + f.data.ultimate);
            switch (f.data.id)
            {
                case Hero.Horus: Damage(f, WeakestEnemy(f), f.data.attack * 3.25f); Shield(f, 110f); break;
                case Hero.Anubis:
                    foreach (Fighter enemy in Team(1 - f.team))
                    { Damage(f, enemy, 135f); if (enemy.Alive) enemy.stun = Mathf.Max(enemy.stun, 1.2f); }
                    break;
                case Hero.Ra:
                    foreach (Fighter enemy in Team(1 - f.team))
                    { Damage(f, enemy, 245f); if (enemy.Alive) enemy.burn = Mathf.Max(enemy.burn, 5f); }
                    break;
                case Hero.Sobek:
                    foreach (Fighter enemy in Team(1 - f.team))
                    { Damage(f, enemy, 110f); if (enemy.Alive) { enemy.taunt = 2.3f; enemy.forcedTarget = f; } }
                    Heal(f, 290f);
                    break;
                case Hero.Set:
                    foreach (Fighter enemy in Team(1 - f.team))
                    { Damage(f, enemy, 185f); if (enemy.Alive) enemy.slow = Mathf.Max(enemy.slow, 4f); }
                    break;
                case Hero.Isis:
                    foreach (Fighter ally in Team(f.team)) { Heal(ally, 345f); Shield(ally, 135f); }
                    break;
                case Hero.Sekhmet:
                    for (int i = 0; i < 3; i++) Damage(f, WeakestEnemy(f), f.data.attack * 1.65f);
                    break;
                case Hero.Nephthys:
                    foreach (Fighter enemy in Team(1 - f.team))
                    { Damage(f, enemy, 115f); if (enemy.Alive) { enemy.curse = 6f; enemy.silence = Mathf.Max(enemy.silence, 3.2f); } }
                    break;
                case Hero.Bastet:
                    for (int i = 0; i < 3; i++) Damage(f, WeakestEnemy(f), f.data.attack * 1.45f);
                    f.dodge = Mathf.Max(f.dodge, 4f);
                    break;
                case Hero.Maat:
                    foreach (Fighter ally in Team(f.team))
                    { Shield(ally, 210f); ally.energy = Mathf.Min(100f, ally.energy + 17f); }
                    break;
            }
        }

        private void Damage(Fighter attacker, Fighter target, float raw, bool dot = false)
        {
            if (target == null || !target.Alive) return;
            if (!dot && target.dodge > 0f && UnityEngine.Random.value < .30f) return;
            float amount = raw * 100f / (100f + 1.9f * target.data.armor);
            if (target.data.id == Hero.Sobek) amount *= .87f;
            if (target.data.id == Hero.Anubis) amount *= .92f;
            if (target.curse > 0f && attacker != null)
            {
                amount *= 1.22f;
                if (attacker.data.id == Hero.Sekhmet) amount *= 1.15f;
            }
            float absorbed = Mathf.Min(target.shield, amount);
            target.shield -= absorbed;
            amount -= absorbed;
            target.health = Mathf.Max(0f, target.health - amount);
            target.energy = Mathf.Min(100f, target.energy + amount * .05f);
            if (target.health > 0f) return;

            // Isis: rescue one teammate once per match.
            foreach (Fighter ally in fighters)
            {
                if (ally.team == target.team && ally.Alive && ally.data.id == Hero.Isis
                    && ally != target && !ally.rescueSpent)
                {
                    ally.rescueSpent = true;
                    target.health = target.data.hp * .22f;
                    Shield(target, 160f);
                    Log("ISIS rescued " + target.data.displayName);
                    return;
                }
            }
            target.health = 0f;
            target.body.SetActive(false);
            Log(target.data.displayName + " defeated");
        }

        private static void Shield(Fighter f, float amount)
        {
            if (f != null && f.Alive) f.shield = Mathf.Min(600f, f.shield + amount);
        }
        private static void Heal(Fighter f, float amount)
        {
            if (f != null && f.Alive) f.health = Mathf.Min(f.data.hp, f.health + amount);
        }

        private void CheckResult()
        {
            List<Fighter> left = Team(0), right = Team(1);
            if (left.Count > 0 && right.Count > 0 && elapsed < durationSeconds) return;
            running = false;
            float a = 0f, b = 0f;
            foreach (Fighter f in left) a += f.health;
            foreach (Fighter f in right) b += f.health;
            result = a > b ? "MEN WIN" : b > a ? "WOMEN WIN" : "DRAW";
            Log("MATCH END - " + result);
        }
        private void Log(string message)
        {
            events.Insert(0, message);
            if (events.Count > 5) events.RemoveAt(events.Count - 1);
            Debug.Log("[EgyptianArena] " + message);
        }

        private void OnGUI()
        {
            float factor = Mathf.Max(.5f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
            Matrix4x4 before = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * factor);
            GUI.Box(new Rect(10, 10, 530, 66), "EGYPTIAN ARENA | AUTO BATTLE 5 vs 5");
            GUI.Label(new Rect(25, 42, 490, 25), "MEN " + Team(0).Count + "/5  |  WOMEN " + Team(1).Count + "/5  |  " + Mathf.CeilToInt(Mathf.Max(0, durationSeconds - elapsed)) + "s");
            if (GUI.Button(new Rect(740, 14, 200, 38), "START / RESTART")) { ResetMatch(); BeginMatch(); }
            if (GUI.Button(new Rect(815, 57, 125, 32), paused ? "RESUME" : "PAUSE")) paused = !paused;

            foreach (Fighter f in fighters)
            {
                if (!f.Alive || arenaCamera == null) continue;
                Vector3 screen = arenaCamera.WorldToScreenPoint(f.body.transform.position + new Vector3(0f, 1.9f, 0f));
                if (screen.z <= 0f) continue;
                float x = screen.x / factor - 42f;
                float y = (Screen.height - screen.y) / factor - 13f;
                GUI.Box(new Rect(x, y, 85, 32), "");
                GUI.Label(new Rect(x + 4, y, 85, 16), f.data.displayName);
                GUI.color = new Color(.3f, .88f, .4f);
                GUI.DrawTexture(new Rect(x + 3, y + 17, 79 * f.health / f.data.hp, 5), Texture2D.whiteTexture);
                GUI.color = new Color(.3f, .7f, 1f);
                GUI.DrawTexture(new Rect(x + 3, y + 24, 79 * f.energy / 100f, 4), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            GUI.Box(new Rect(10, 390, 560, 139), "BATTLE LOG");
            for (int i = 0; i < events.Count; i++) GUI.Label(new Rect(24, 412 + i * 22, 528, 20), events[i]);
            if (!running) GUI.Box(new Rect(355, 322, 270, 62), result);
            GUI.matrix = before;
        }
    }
}
