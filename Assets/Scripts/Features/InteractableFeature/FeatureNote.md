# Feature: InteractableFeature

## 1. Purpose
- Provide Interactable components that can trigger Dialogue or Pickup behavior.
- Track nearby interactables and choose a single candidate by priority + distance.
- Expose candidate change events for UI prompts and interaction flow.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/InteractableFeature/`
- Controllers:
  - `Interactable.cs` 〞 world object interaction entry (Dialogue/Pickup)
  - `PlayerInteractSensor.cs` 〞 trigger-based candidate tracking
  - `InteractionController.cs` 〞 listens to Interact input and executes interaction
  - `PlayerInteractionInput.cs` 〞 Input System reader for Interact + Scroll
- Events:
  - `InteractPressedEvent`
  - `ScrollUpEvent` / `ScrollDownEvent`
  - `InteractableCandidateChangedEvent`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- None (Controller-only feature).

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `PlayerInteractionInput` on player input root (bind Interact + Scroll actions).
  - `PlayerInteractSensor` on player (trigger collider).
  - `InteractionController` on player (points at `PlayerInteractSensor`).
- World setup:
  - Add `Interactable` to objects with a Trigger Collider2D.
  - Dialogue: assign `DialogueSystemTrigger`.
  - Pickup: assign `ItemDefinition`.
- Priority rule:
  - Higher `Interactable.priority` wins, then nearest distance, then enter order.

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `struct InteractableCandidateChangedEvent`
  - When fired: current candidate changes.
  - Payload: `HasCandidate`, `Type`, `DisplayName`, `ItemDefinition`.
  - Example:
    ```csharp
    this.RegisterEvent<InteractableCandidateChangedEvent>(OnCandidateChanged)
        .UnRegisterWhenDisabled(gameObject);
    ```
- `struct InteractPressedEvent`
  - When fired: Interact input pressed.
- `struct ScrollUpEvent` / `ScrollDownEvent`
  - When fired: scroll delta passes threshold.

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None (pickup uses BackpackFeature commands internally).

### 4.4 Queries (Read Path, optional)
- None.

### 4.5 Model Read Surface
- None.

## 5. Typical Integrations
- UI prompt listens to `InteractableCandidateChangedEvent` to display name or icon.
- Inventory selector listens to `ScrollUpEvent`/`ScrollDownEvent`.

## 6. Verify Checklist
1. Player enters trigger; confirm candidate event fires and is stable.
2. Press Interact; confirm exactly one interaction triggers.
3. Multiple interactables: highest priority and nearest is chosen.

## 7. UNVERIFIED (only if needed)
- None.
