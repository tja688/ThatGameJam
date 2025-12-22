# Feature: PlayerCharacter2D

## 1. Purpose
- Provide 2D platformer character movement, jumping, and grounded state.
- Expose bindables/events for movement state and allow external input.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/PlayerCharacter2D/`
- Controllers:
  - `PlatformerCharacterController.cs` 〞 main movement driver (Update + FixedUpdate)
  - `PlatformerCharacterInput.cs` 〞 Input System reader (implements `IPlatformerFrameInputSource`)
- Systems:
  - `IPlayerCharacter2DSystem`, `PlayerCharacter2DSystem` 〞 empty orchestration hook
- Models:
  - `IPlayerCharacter2DModel`, `PlayerCharacter2DModel` 〞 movement state and bindables
- Commands:
  - `SetFrameInputCommand`
  - `TickFixedStepCommand`
- Queries:
  - `GetDesiredVelocityQuery`
- Events:
  - `PlayerGroundedChangedEvent`
  - `PlayerJumpedEvent`
- Configs:
  - `PlatformerCharacterStats.cs` 〞 ScriptableObject movement tuning
- Utilities: None

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.PlayerCharacter2D.Models.IPlayerCharacter2DModel`
  - System: `ThatGameJam.Features.PlayerCharacter2D.Systems.IPlayerCharacter2DSystem`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `PlatformerCharacterController` on the player (requires Rigidbody2D + CapsuleCollider2D)
  - `PlatformerCharacterInput` on the same GameObject (or any `IPlatformerFrameInputSource`)
- Inspector fields (if any):
  - `PlatformerCharacterController._stats` 〞 `PlatformerCharacterStats` asset
  - `PlatformerCharacterController._useExternalInput` / `_externalInput` 〞 bypass input source
  - `PlatformerCharacterController._inputSource` 〞 optional explicit input source
  - `PlatformerCharacterInput._move` / `_jump` 〞 InputActionReference bindings

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
> Other Features listen to these
- `struct PlayerGroundedChangedEvent`
  - When fired: grounded state toggles in `TickFixedStepCommand`
  - Payload: `Grounded` (bool), `ImpactSpeed` (float)
  - Typical listener: VFX, audio, camera shake
  - Example:
    ```csharp
    this.RegisterEvent<PlayerGroundedChangedEvent>(OnGrounded)
        .UnRegisterWhenDisabled(gameObject);
    ```
- `struct PlayerJumpedEvent`
  - When fired: jump executed in `TickFixedStepCommand`
  - Payload: none
  - Typical listener: SFX/animation

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `SetFrameInputCommand`
  - What state it mutates: `IPlayerCharacter2DModel.FrameInput`, time, jump buffer flags
  - Typical sender: `PlatformerCharacterController`
- `TickFixedStepCommand`
  - What state it mutates: `IPlayerCharacter2DModel.Grounded`, `Velocity`, jump state
  - Typical sender: `PlatformerCharacterController` (FixedUpdate)

### 4.4 Queries (Read Path, optional)
- `GetDesiredVelocityQuery`
  - What it returns: `Vector2` velocity from model
  - Typical caller: `PlatformerCharacterController` to drive Rigidbody2D

### 4.5 Model Read Surface
- Bindables / readonly properties:
  - `BindableProperty<bool> Grounded`
  - `BindableProperty<Vector2> Velocity`
  - Usage notes: model also exposes `FrameInput` and timing flags for advanced systems

## 5. Typical Integrations
- Example: Play landing dust ↙ listen to `PlayerGroundedChangedEvent`.

## 6. Verify Checklist
1. Add `PlatformerCharacterController` + `PlatformerCharacterInput` to the player.
2. Assign a `PlatformerCharacterStats` asset and InputAction references.
3. Press jump; expect `PlayerJumpedEvent` and movement to update.

## 7. UNVERIFIED (only if needed)
- None.
