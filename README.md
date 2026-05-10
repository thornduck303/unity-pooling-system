# Unity Pooling System
Custom object pooling system for Unity.

Instead of constantly creating and destroying **GameObjects**, this system reuses inactive instances to improve runtime performance and reduce garbage collection spikes.

## How to use

### 1. Create a Poolable Prefab
Attach ```PoolableObject`` to your prefab.

```csharp
using ThornDuck.PoolingSystem;
using UnityEngine;

public class Bullet : PoolableObject
{
  private void Awake()
    => OnRetrieveFromPool += Reset;

  private void Reset()
  {
    // Reset state here
  }
}
```

### 2. Prewarm the Pool (Optional)
Prewarming creates inactive instances ahead of time to avoid runtime instantiation spikes.

```csharp
PoolingSystem.Prewarm(bulletPrefab, 50);
```

### 3. Spawn Objects
```csharp
PoolableObject bullet = PoolingSystem.TryGetInstance(
  bulletPrefab,
  spawnPosition,
  spawnRotation
);
```

If the pool is empty, the system automatically creates a new instance.

### 4. Return Objects to the Pool
Instead of destroying objects:

```csharp
PoolingSystem.ReturnInstance(bullet);
```

The object becomes inactive and is reused later.

### Example
```csharp
public class Gun : MonoBehaviour
{
  [SerializeField] private PoolableObject bulletPrefab;

  private void Start()
    => PoolingSystem.Prewarm(bulletPrefab, 100);

  private void Shoot()
  {
    PoolableObject bullet = PoolingSystem.TryGetInstance(
      bulletPrefab,
      transform.position,
      transform.rotation
    );

    if (bullet == null)
        return;
  }
}
```

## Notes
Never use ```Destroy()``` on pooled instances directly, use ```PoolingSystem.ReturnInstance()``` instead. 
However, if a pooled object is destroyed manually, the system automatically deregisters it safely.

The system includes built-in **safety limits** (max number of pools, max number of instances and max instances per prefab).
If limits are exceeded, the **oldest unused pool** is automatically **removed** and instance creation safely fails with debug warnings.

When an object is retrieved:
- Position and rotation are applied
- Object is detached from the pool container
- ```OnRetrieveFromPool``` is invoked
- **GameObject** is activated

When returned:
- **GameObject** is disabled
- Object is parented back into its pool
- Instance is stored for reuse
