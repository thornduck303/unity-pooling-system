using UnityEngine;
using System.Collections.Generic;

namespace ThornDuck.PoolingSystem
{
    /// <summary>
    /// Represents a pool of <see cref="PoolableObject"/> instances for a specific prefab.
    /// </summary>
    /// <seealso cref="PoolableObject"/>
    /// <seealso cref="PoolingSystem"/>
    /// <author>Murilo M. Grosso</author>
    public class ObjectPool
    {
        private readonly Transform container;
        private readonly Stack<PoolableObject> stack = new();

        private float lastAccessTimeSeconds;
        public float LastAccessTimeSeconds => lastAccessTimeSeconds;

        public ObjectPool(PoolableObject prefab)
        {
            container = new GameObject($"{prefab.name}Pool").transform;
            container.SetParent(PoolingSystem.GetContainer());
        }

        /// <summary>
        /// Retrieves an instance from the pool if available.
        /// </summary>
        /// <returns>
        /// A <see cref="PoolableObject"/> instance if the pool contains one; otherwise, <c>null</c>.
        /// </returns>
        public PoolableObject TryRetrieveObject()
        {
            lastAccessTimeSeconds = Time.unscaledTime;

            if (stack.Count > 0)
                return stack.Pop();

            return null;
        }

        /// <summary>
        /// Returns an instance to the pool.
        /// </summary>
        /// <param name="returnObject">The <see cref="PoolableObject"/> instance to return.</param>
        public void ReturnObject(PoolableObject returnObject)
        {
            returnObject.transform.SetParent(container, false);
            stack.Push(returnObject);
        }

        /// <summary>
        /// Destroys the pool.
        /// </summary>
        public void Destroy()
            => Object.Destroy(container.gameObject);
    }
}
