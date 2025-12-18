using UnityEngine;
using System.Collections.Generic;

namespace ThornDuck.PoolingSystem
{
    /// <summary>
    /// Static class that manages pooling of <see cref="PoolableObject"/> instances.
    /// </summary>
    /// <remarks>
    /// This system helps reduce runtime allocations and improve performance by reusing instances
    /// of objects instead of repeatedly instantiating and destroying them. It automatically
    /// manages a maximum number of pools and instances per prefab.
    /// </remarks>
    /// <seealso cref="PoolableObject"/>
    /// <seealso cref="ObjectPool"/>
    /// <author>Murilo M. Grosso</author>
    public static class PoolingSystem
    {
        private const int MAX_POOLS = 64;
        private const int MAX_INSTANCES = 4096;
        private const int MAX_INSTANCES_PER_PREFAB = 512;

        private static readonly Dictionary<int, ObjectPool> pools = new(MAX_POOLS);
        private static readonly Dictionary<int,int> countPerInstance = new(MAX_POOLS);

        private static int instanceCount;
        private static Transform rootContainer;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnPlayMode()
        {
            pools.Clear();
            countPerInstance.Clear();
            instanceCount = 0;
            rootContainer = null;
        }
#endif

        /// <summary>
        /// Gets the root <see cref="Transform"/> used to organize pooled objects in the hierarchy.
        /// </summary>
        /// <returns>The root <see cref="Transform"/> for all pools.</returns>
        public static Transform GetContainer()
        {
            if(rootContainer == null)
                rootContainer = new GameObject("ObjectPoolRoot").transform;
            return rootContainer;
        }

        /// <summary>
        /// Pre-populates the pool for a given prefab with a specified number of inactive instances.
        /// </summary>
        /// <param name="prefab">The prefab to prewarm in the pool.</param>
        /// <param name="count">The number of instances to create.</param>
        public static void Prewarm(PoolableObject prefab, int count)
        {
            ObjectPool objectPool = GetPool(prefab);
            for (int i = 0; i < count; i++)
            {
                PoolableObject instance = TryCreateInstance(prefab);
                if(instance != null)
                    objectPool.ReturnObject(instance);
            }
        }

        /// <summary>
        /// Retrieves an instance of a prefab from the pool, or creates a new one if none are available.
        /// </summary>
        /// <remarks>
        /// The returned instance is automatically activated and detached from the pool container.
        /// Note that the system may return <c>null</c> if the maximum number of instances has been reached.
        /// </remarks>
        /// <param name="prefab">The prefab to retrieve.</param>
        /// <param name="spawnPosition">The world position to place the instance.</param>
        /// <param name="spawnRotation">The rotation to apply to the instance.</param>
        /// <returns>An active <see cref="PoolableObject"/> instance, or <c>null</c> if creation limits are exceeded.</returns>
        public static PoolableObject TryGetInstance(
            PoolableObject prefab, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if(prefab == null)
                return null;
            
            ObjectPool targetPool = GetPool(prefab);
            PoolableObject retrievedPoolable = targetPool.TryRetrieveObject();

            if(retrievedPoolable == null)
            {
                retrievedPoolable = TryCreateInstance(prefab);
                if(retrievedPoolable == null)
                    return null;
            }

            Transform retrievedTransform = retrievedPoolable.transform;
            retrievedTransform.position = spawnPosition;
            retrievedTransform.rotation = spawnRotation;
            retrievedTransform.SetParent(null, false);

            retrievedPoolable.OnRetrieveFromPool?.Invoke();
            retrievedPoolable.gameObject.SetActive(true);

            return retrievedPoolable;
        }

        /// <summary>
        /// Returns a <see cref="PoolableObject"/> instance back to its pool.
        /// </summary>
        /// <remarks>
        /// If the instance was not registered in the pooling system, it will be destroyed instead.
        /// The instance is automatically deactivated before being returned to the pool.
        /// </remarks>
        /// <param name="instance">The instance to return.</param>
        public static void ReturnInstance(PoolableObject instance)
        {
            if(instance == null)
                return;

            if(!instance.IsRegistered)
            {
                if (Debug.isDebugBuild)
                    Debug.LogWarning($"[OBJECT POOLING SYSTEM] {instance.name} instance not registered!");
                Object.Destroy(instance);
            }

            ObjectPool targetPool = GetPool(instance.Prefab);

            instance.gameObject.SetActive(false);
            targetPool.ReturnObject(instance);
        }

        /// <summary>
        /// Permanently removes a <see cref="PoolableObject"/> instance.
        /// </summary>
        /// <param name="instance">The instance to destroy.</param>
        public static void RemoveInstance(PoolableObject instance)
        {
            if(instance == null)
                return;

            if(!instance.IsRegistered)
                return;

            int prefabId = instance.Prefab.GetInstanceID();
            if (countPerInstance.ContainsKey(prefabId))
            {
                instanceCount--;
                countPerInstance[prefabId]--;
            }

            instance.Deregister();
            Object.Destroy(instance);
        }

        private static ObjectPool GetPool(PoolableObject prefab)
        {
            int prefabId = prefab.GetInstanceID();
            if (!pools.TryGetValue(prefabId, out ObjectPool pool))
            {
                if(pools.Count > MAX_POOLS - 1)
                {
                    if (Debug.isDebugBuild)
                        Debug.LogWarning($"[OBJECT POOLING SYSTEM] Maximum number of pools reached!");
                    DeleteLastAccessTimePool();
                }

                pool = new ObjectPool(prefab);
                pools.Add(prefabId, pool);
            }
            return pool;
        }

        private static void DeleteLastAccessTimePool()
        {
            KeyValuePair<int, ObjectPool>? oldestPoolEntry = null;
            float longestTimeSeconds = float.MaxValue;

            foreach (KeyValuePair<int, ObjectPool> poolEntry in pools)
            {
                float lastAccessTimeSeconds = poolEntry.Value.LastAccessTimeSeconds;
                if (lastAccessTimeSeconds < longestTimeSeconds)
                {
                    oldestPoolEntry = poolEntry;
                    longestTimeSeconds = lastAccessTimeSeconds;
                }
            }

            if (oldestPoolEntry != null)
            {
                oldestPoolEntry.Value.Value.Destroy();
                pools.Remove(oldestPoolEntry.Value.Key);
            }
        }

        private static PoolableObject TryCreateInstance(PoolableObject prefab)
        {
            if (prefab == null)
                return null;

            if (instanceCount + 1 > MAX_INSTANCES)
            {
                if (Debug.isDebugBuild)
                    Debug.LogWarning($"[OBJECT POOLING SYSTEM] Maximum number of instances reached!");
                return null; 
            }

            int prefabId = prefab.GetInstanceID();
            if (!countPerInstance.ContainsKey(prefabId))
                countPerInstance.Add(prefabId, 0);

            if (countPerInstance[prefabId] + 1 > MAX_INSTANCES_PER_PREFAB)
            {
                if (Debug.isDebugBuild)
                    Debug.LogWarning($"[OBJECT POOLING SYSTEM] Maximum number of instances for {prefab.name} reached!");
                return null;
            }

            instanceCount++;
            countPerInstance[prefabId]++;
            PoolableObject instance = Object.Instantiate(prefab);
            instance.Register(prefab);
            return instance;
        }
    }
}