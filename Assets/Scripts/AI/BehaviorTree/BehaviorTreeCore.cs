using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Core.VoidGame.AI
{
    /// <summary>
    /// Behavior Tree Node State - returned by node evaluation
    /// </summary>
    public enum NodeState
    {
        Running,
        Success,
        Failure
    }

    /// <summary>
    /// AI Context passed to behavior tree nodes during evaluation
    /// </summary>
    public class AIContext
    {
        public GameUnit unit;
        public List<GameUnit> allies;
        public List<GameUnit> enemies;
        public float currentTime;
        public float delta;
        public Vector2 arenaBounds;
        public int round;
        public GameMode gameMode;
        public List<Obstacle> obstacles;

        // Optional systems
        public ICombatActions combat;
        public INavigationSystem navigation;

        public AIContext()
        {
            allies = new List<GameUnit>();
            enemies = new List<GameUnit>();
            obstacles = new List<Obstacle>();
        }
    }

    /// <summary>
    /// Interface for combat action callbacks
    /// </summary>
    public interface ICombatActions
    {
        DamageResult ApplyDamage(GameUnit source, GameUnit target, int damage, DamageType damageType, float currentTime, bool isCrit);
        int ApplyHeal(GameUnit source, GameUnit target, int amount);
        void ApplyStatusEffect(GameUnit target, StatusEffectType effectType, float value, float duration, float currentTime);
        void ExecuteAbility(GameUnit unit, Ability ability, GameUnit target, Position? position);
    }

    /// <summary>
    /// Interface for navigation system
    /// </summary>
    public interface INavigationSystem
    {
        List<Position> FindPath(Position start, Position end);
        bool IsPositionValid(Position pos);
        Position GetRandomValidPosition(Vector2 bounds);
    }

    /// <summary>
    /// Obstacle data for navigation
    /// </summary>
    [Serializable]
    public class Obstacle
    {
        public string id;
        public Position position;
        public string type; // "barrel", "pillar", "rock"
        public int maxHp;
        public int hp;
        public float radius;
        public bool destroyed;
    }

    #region Base Node

    /// <summary>
    /// Abstract base class for all behavior tree nodes
    /// </summary>
    public abstract class BehaviorNode
    {
        protected List<BehaviorNode> children = new List<BehaviorNode>();

        public abstract NodeState Evaluate(AIContext context);

        public void AddChild(BehaviorNode child)
        {
            children.Add(child);
        }
    }

    #endregion

    #region Composite Nodes

    /// <summary>
    /// Selector Node - Returns success if any child succeeds (OR logic)
    /// Evaluates children left to right, returns on first non-failure
    /// </summary>
    public class SelectorNode : BehaviorNode
    {
        public SelectorNode(params BehaviorNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public override NodeState Evaluate(AIContext context)
        {
            foreach (var child in children)
            {
                var state = child.Evaluate(context);
                if (state != NodeState.Failure)
                    return state;
            }
            return NodeState.Failure;
        }
    }

    /// <summary>
    /// Sequence Node - Returns success only if all children succeed (AND logic)
    /// Evaluates children left to right, returns on first non-success
    /// </summary>
    public class SequenceNode : BehaviorNode
    {
        public SequenceNode(params BehaviorNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public override NodeState Evaluate(AIContext context)
        {
            foreach (var child in children)
            {
                var state = child.Evaluate(context);
                if (state != NodeState.Success)
                    return state;
            }
            return NodeState.Success;
        }
    }

    /// <summary>
    /// Parallel Node - Runs all children simultaneously
    /// Returns based on policy: RequireAll (all must succeed) or RequireOne (any can succeed)
    /// </summary>
    public class ParallelNode : BehaviorNode
    {
        public enum Policy { RequireAll, RequireOne }
        
        private Policy successPolicy;

        public ParallelNode(Policy policy, params BehaviorNode[] nodes)
        {
            successPolicy = policy;
            children.AddRange(nodes);
        }

        public override NodeState Evaluate(AIContext context)
        {
            int successCount = 0;
            int failureCount = 0;
            bool anyRunning = false;

            foreach (var child in children)
            {
                var state = child.Evaluate(context);
                switch (state)
                {
                    case NodeState.Success:
                        successCount++;
                        break;
                    case NodeState.Failure:
                        failureCount++;
                        break;
                    case NodeState.Running:
                        anyRunning = true;
                        break;
                }
            }

            if (successPolicy == Policy.RequireOne && successCount > 0)
                return NodeState.Success;
            
            if (successPolicy == Policy.RequireAll && successCount == children.Count)
                return NodeState.Success;

            if (anyRunning)
                return NodeState.Running;

            return NodeState.Failure;
        }
    }

    /// <summary>
    /// Random Selector - Selects a random child to evaluate
    /// </summary>
    public class RandomSelectorNode : BehaviorNode
    {
        public RandomSelectorNode(params BehaviorNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public override NodeState Evaluate(AIContext context)
        {
            if (children.Count == 0)
                return NodeState.Failure;

            // Shuffle and try each
            var shuffled = new List<BehaviorNode>(children);
            ShuffleList(shuffled);

            foreach (var child in shuffled)
            {
                var state = child.Evaluate(context);
                if (state != NodeState.Failure)
                    return state;
            }

            return NodeState.Failure;
        }

        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }
    }

    #endregion

    #region Decorator Nodes

    /// <summary>
    /// Inverter Node - Inverts the result of its child
    /// Success becomes Failure, Failure becomes Success, Running stays Running
    /// </summary>
    public class InverterNode : BehaviorNode
    {
        public InverterNode(BehaviorNode child)
        {
            children.Add(child);
        }

        public override NodeState Evaluate(AIContext context)
        {
            if (children.Count == 0)
                return NodeState.Failure;

            var state = children[0].Evaluate(context);
            return state switch
            {
                NodeState.Success => NodeState.Failure,
                NodeState.Failure => NodeState.Success,
                _ => state
            };
        }
    }

    /// <summary>
    /// Succeeder Node - Always returns success regardless of child result
    /// </summary>
    public class SucceederNode : BehaviorNode
    {
        public SucceederNode(BehaviorNode child)
        {
            children.Add(child);
        }

        public override NodeState Evaluate(AIContext context)
        {
            if (children.Count > 0)
                children[0].Evaluate(context);
            return NodeState.Success;
        }
    }

    /// <summary>
    /// Repeater Node - Repeats child evaluation a number of times
    /// </summary>
    public class RepeaterNode : BehaviorNode
    {
        private int repeatCount;
        private int currentCount;

        public RepeaterNode(int count, BehaviorNode child)
        {
            repeatCount = count;
            children.Add(child);
        }

        public override NodeState Evaluate(AIContext context)
        {
            if (children.Count == 0)
                return NodeState.Failure;

            if (currentCount < repeatCount)
            {
                children[0].Evaluate(context);
                currentCount++;
                return NodeState.Running;
            }

            currentCount = 0;
            return NodeState.Success;
        }
    }

    /// <summary>
    /// Cooldown Node - Prevents child execution until cooldown expires
    /// </summary>
    public class CooldownNode : BehaviorNode
    {
        private float cooldownDuration;
        private float lastExecutionTime = float.MinValue;

        public CooldownNode(float cooldown, BehaviorNode child)
        {
            cooldownDuration = cooldown;
            children.Add(child);
        }

        public override NodeState Evaluate(AIContext context)
        {
            if (children.Count == 0)
                return NodeState.Failure;

            if (context.currentTime - lastExecutionTime < cooldownDuration)
                return NodeState.Failure;

            var state = children[0].Evaluate(context);
            if (state == NodeState.Success)
                lastExecutionTime = context.currentTime;

            return state;
        }
    }

    #endregion

    #region Leaf Nodes

    /// <summary>
    /// Condition Node - Evaluates a condition and returns Success/Failure
    /// </summary>
    public class ConditionNode : BehaviorNode
    {
        private Func<AIContext, bool> condition;

        public ConditionNode(Func<AIContext, bool> conditionFunc)
        {
            condition = conditionFunc;
        }

        public override NodeState Evaluate(AIContext context)
        {
            return condition(context) ? NodeState.Success : NodeState.Failure;
        }
    }

    /// <summary>
    /// Action Node - Executes an action and returns the result
    /// </summary>
    public class ActionNode : BehaviorNode
    {
        private Func<AIContext, NodeState> action;

        public ActionNode(Func<AIContext, NodeState> actionFunc)
        {
            action = actionFunc;
        }

        public override NodeState Evaluate(AIContext context)
        {
            return action(context);
        }
    }

    /// <summary>
    /// Wait Node - Waits for a specified duration
    /// </summary>
    public class WaitNode : BehaviorNode
    {
        private float waitDuration;
        private float startTime = -1f;

        public WaitNode(float duration)
        {
            waitDuration = duration;
        }

        public override NodeState Evaluate(AIContext context)
        {
            if (startTime < 0f)
                startTime = context.currentTime;

            if (context.currentTime - startTime >= waitDuration)
            {
                startTime = -1f;
                return NodeState.Success;
            }

            return NodeState.Running;
        }
    }

    /// <summary>
    /// Log Node - Logs a message for debugging
    /// </summary>
    public class LogNode : BehaviorNode
    {
        private string message;
        private Func<AIContext, string> dynamicMessage;

        public LogNode(string msg)
        {
            message = msg;
        }

        public LogNode(Func<AIContext, string> msgFunc)
        {
            dynamicMessage = msgFunc;
        }

        public override NodeState Evaluate(AIContext context)
        {
            string msg = dynamicMessage != null ? dynamicMessage(context) : message;
            Debug.Log($"[BehaviorTree] {context.unit?.name ?? "Unknown"}: {msg}");
            return NodeState.Success;
        }
    }

    #endregion

    #region Behavior Tree Manager

    /// <summary>
    /// Behavior Tree - Root container that manages tree execution
    /// </summary>
    public class BehaviorTree
    {
        public string Name { get; private set; }
        public BehaviorNode Root { get; private set; }
        
        private AIContext cachedContext;
        private NodeState lastState = NodeState.Failure;

        public BehaviorTree(string name, BehaviorNode root)
        {
            Name = name;
            Root = root;
        }

        public NodeState Tick(AIContext context)
        {
            cachedContext = context;
            lastState = Root?.Evaluate(context) ?? NodeState.Failure;
            return lastState;
        }

        public NodeState GetLastState() => lastState;
    }

    #endregion
}
