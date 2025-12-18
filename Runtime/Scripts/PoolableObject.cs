using System;
using UnityEngine;

namespace ThornDuck.PoolingSystem
{
    /// <summary>
    /// Represents an object that can be managed by the <see cref="PoolingSystem"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="PoolableObject"/> instances are designed to be reused via object pooling.
    /// The <see cref="OnRetrieveFromPool"/> event allows reseting.
    /// </remarks>
    /// <seealso cref="PoolingSystem"/>
    /// <seealso cref="ObjectPool"/>
    /// <author>Murilo M. Grosso</author>
    public class PoolableObject : MonoBehaviour
    {
        public Action OnRetrieveFromPool;

        private bool isRegistered;
        /// <summary>
        /// Indicates whether this instance is registered with the pooling system.
        /// </summary>
        public bool IsRegistered => isRegistered;

        private PoolableObject prefab;
        /// <summary>
        /// Gets the original prefab associated with this instance.
        /// </summary>
        public PoolableObject Prefab => prefab;

        /// <summary>
        /// Registers this instance with the pooling system and associates it with a prefab.
        /// </summary>
        /// <param name="prefab">The prefab this instance was created from.</param>
        public void Register(PoolableObject prefab)
        {
            isRegistered = true;
            this.prefab = prefab;
        }

        /// <summary>
        /// Deregisters this instance from the pooling system.
        /// </summary>
        public void Deregister()
            => isRegistered = false;

        private void OnDestroy()
        {
            if(isRegistered)
                PoolingSystem.RemoveInstance(this);
        }
    }
}