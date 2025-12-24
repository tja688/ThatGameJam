# Feature: Mechanisms

## 1. Purpose
- Provide extensible level mechanisms (hazards/traps) implemented as scene controllers.
- Prototype spike hazards (touch to die) and lamp-activated vine growth platforms.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/Mechanisms/`
- Controllers:
  - `MechanismControllerBase.cs` - base IController for mechanism scripts
  - `SpikeHazard2D.cs` - trigger hazard that kills the player on contact
  - `VineMechanism2D.cs` - grows a vine/platform when a lamp spawns nearby
- Systems/Models/Commands/Queries: None.

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- None.

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `SpikeHazard2D` on spike objects with a Trigger Collider2D
  - `VineMechanism2D` on a vine object with a BoxCollider2D (set size/offset for the fully grown shape)
- Inspector fields (if any):
  - `SpikeHazard2D.deathReason` - death reason sent to DeathRespawn (default Script)
  - `VineMechanism2D.lightAffectRadius` - lamp influence radius
  - `VineMechanism2D.growthDirection` / `growthLength` - axis and length of growth
  - `VineMechanism2D.growthDuration` / `growthCurve` - growth animation timing
  - `VineMechanism2D.visualRoot` / `growthCollider` - visuals and collider targets (use a child for visuals if you want collider sizing to stay independent of scaling)
  - `VineMechanism2D.enableColliderDuringGrowth` - whether the collider grows during the animation

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- None. (Mechanisms listen to `LampSpawnedEvent` and `RunResetEvent`.)

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None.

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- Example: Extend a new mechanism by listening to reset events.
  ```csharp
  public class CustomMechanism2D : MechanismControllerBase
  {
      private void OnEnable()
      {
          this.RegisterEvent<RunResetEvent>(_ => ResetState())
              .UnRegisterWhenDisabled(gameObject);
      }

      private void ResetState()
      {
      }
  }
  ```

## 6. Verify Checklist
1. Add `SpikeHazard2D` to a trigger collider and touch it with the player; expect `PlayerDiedEvent` and lamp spawn.
2. Place `VineMechanism2D` within `lightAffectRadius` of a lamp spawn; after death, vine grows and becomes walkable.
3. Trigger `RunResetEvent`; vine resets to the collapsed state and collider is disabled.

## 7. UNVERIFIED (only if needed)
- None.
