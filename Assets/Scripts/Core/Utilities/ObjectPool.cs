using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Core.VoidGame
{
    /// <summary>
    /// Generic Object Pool for efficient object reuse
    /// Converted from TypeScript ObjectPool.ts
    /// </summary>
    /// <typeparam name="T">Type of objects to pool</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> items;
        private readonly Func<T> factory;
        private readonly Action<T> reset;
        private readonly Action<T> onGet;

        /// <summary>
        /// Number of items currently in the pool
        /// </summary>
        public int Count => items.Count;

        /// <summary>
        /// Total number of items created by this pool
        /// </summary>
        public int TotalCreated { get; private set; }

        /// <summary>
        /// Create a new object pool
        /// </summary>
        /// <param name="factory">Factory function to create new items</param>
        /// <param name="reset">Optional reset function called when item is returned to pool</param>
        /// <param name="onGet">Optional function called when item is retrieved from pool</param>
        /// <param name="initialSize">Initial number of items to pre-create</param>
        public ObjectPool(Func<T> factory, Action<T> reset = null, Action<T> onGet = null, int initialSize = 20)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.reset = reset;
            this.onGet = onGet;
            this.items = new Stack<T>(initialSize);

            // Pre-populate the pool
            for (int i = 0; i < initialSize; i++)
            {
                items.Push(factory());
                TotalCreated++;
            }
        }

        /// <summary>
        /// Get an item from the pool, or create a new one if empty
        /// </summary>
        public T Get()
        {
            T item;
            if (items.Count > 0)
            {
                item = items.Pop();
            }
            else
            {
                item = factory();
                TotalCreated++;
            }

            onGet?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Return an item to the pool for reuse
        /// </summary>
        public void Release(T item)
        {
            if (item == null)
                return;

            reset?.Invoke(item);
            items.Push(item);
        }

        /// <summary>
        /// Clear the pool
        /// </summary>
        public void Clear()
        {
            items.Clear();
        }

        /// <summary>
        /// Pre-warm the pool with additional items
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                items.Push(factory());
                TotalCreated++;
            }
        }
    }

    /// <summary>
    /// GameObject-specific pool with MonoBehaviour support
    /// </summary>
    public class GameObjectPool
    {
        private readonly Stack<GameObject> pool;
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Action<GameObject> onGet;
        private readonly Action<GameObject> onRelease;

        public int Count => pool.Count;
        public int TotalCreated { get; private set; }

        public GameObjectPool(
            GameObject prefab, 
            Transform parent = null, 
            int initialSize = 10,
            Action<GameObject> onGet = null,
            Action<GameObject> onRelease = null)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.pool = new Stack<GameObject>(initialSize);

            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNew();
                obj.SetActive(false);
                pool.Push(obj);
            }
        }

        private GameObject CreateNew()
        {
            var obj = UnityEngine.Object.Instantiate(prefab, parent);
            TotalCreated++;
            return obj;
        }

        public GameObject Get()
        {
            GameObject obj;
            if (pool.Count > 0)
            {
                obj = pool.Pop();
            }
            else
            {
                obj = CreateNew();
            }

            obj.SetActive(true);
            onGet?.Invoke(obj);
            return obj;
        }

        public void Release(GameObject obj)
        {
            if (obj == null)
                return;

            onRelease?.Invoke(obj);
            obj.SetActive(false);
            pool.Push(obj);
        }

        public void Clear()
        {
            while (pool.Count > 0)
            {
                var obj = pool.Pop();
                if (obj != null)
                    UnityEngine.Object.Destroy(obj);
            }
        }
    }

    /// <summary>
    /// Pool Manager - Manages multiple pools by type/prefab
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager instance;
        public static PoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PoolManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("PoolManager");
                        instance = go.AddComponent<PoolManager>();
                    }
                }
                return instance;
            }
        }

        private Dictionary<string, GameObjectPool> pools = new Dictionary<string, GameObjectPool>();
        private Dictionary<Type, object> typedPools = new Dictionary<Type, object>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Register a GameObject pool
        /// </summary>
        public void RegisterPool(string key, GameObject prefab, int initialSize = 10)
        {
            if (!pools.ContainsKey(key))
            {
                var poolParent = new GameObject($"Pool_{key}");
                poolParent.transform.SetParent(transform);
                pools[key] = new GameObjectPool(prefab, poolParent.transform, initialSize);
            }
        }

        /// <summary>
        /// Get a GameObject from a pool
        /// </summary>
        public GameObject Get(string key)
        {
            if (pools.TryGetValue(key, out var pool))
            {
                return pool.Get();
            }
            Debug.LogWarning($"Pool '{key}' not found");
            return null;
        }

        /// <summary>
        /// Release a GameObject back to its pool
        /// </summary>
        public void Release(string key, GameObject obj)
        {
            if (pools.TryGetValue(key, out var pool))
            {
                pool.Release(obj);
            }
            else
            {
                Debug.LogWarning($"Pool '{key}' not found, destroying object");
                Destroy(obj);
            }
        }

        /// <summary>
        /// Get or create a typed pool
        /// </summary>
        public ObjectPool<T> GetOrCreatePool<T>(Func<T> factory, Action<T> reset = null, int initialSize = 20) where T : class
        {
            var type = typeof(T);
            if (!typedPools.TryGetValue(type, out var poolObj))
            {
                poolObj = new ObjectPool<T>(factory, reset, null, initialSize);
                typedPools[type] = poolObj;
            }
            return (ObjectPool<T>)poolObj;
        }

        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAll()
        {
            foreach (var pool in pools.Values)
            {
                pool.Clear();
            }
            pools.Clear();
            typedPools.Clear();
        }
    }
}
