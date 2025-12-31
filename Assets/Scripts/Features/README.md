# Backpack + Interactables + Lamp Integration

## 1. Configure Interactables
1. Add a `Collider2D` set to `IsTrigger`.
2. Add `Interactable` and set:
   - `Type`: `Dialogue` or `Pickup`.
   - `Priority`: higher wins when multiple candidates overlap.
   - `DisplayName`: shown by UI prompts.
3. Dialogue:
   - Assign a `DialogueSystemTrigger`.
4. Pickup:
   - Assign an `ItemDefinition`.
   - For Kerosene Lamp pickups, ensure the same object has `KeroseneLampInstance` with a matching `ItemDefinition`.

## 2. Player Input + Selection
1. Add `PlayerInteractionInput` and bind:
   - `Interact (E)`
   - `Scroll` (mouse wheel; read `Vector2.y`)
2. Add `PlayerInteractSensor` with a Trigger Collider2D (sensor volume).
3. Add `InteractionController` and assign the sensor.
4. Add `InventorySelectorController`:
   - Assign `holdPoint` (player hand/anchor).
   - `selectUpdatesHeld` = true if scrolling should also hold.

## 3. UI Wiring
- PlayerPanel (UI Toolkit inventory):
  - Add `BackpackUIProvider` to a scene object that lives with UI.
  - It registers itself to `UIServiceRegistry.Inventory`.
- 3-slot wheel UI:
  - Add `BackpackWheelUIController`.
  - Assign three slots (previous/current/next).
  - Each slot expects:
    - `CanvasGroup`
    - `RawImage` for icon
    - `Text` for name/count

## 4. Lamp States (KeroseneLamp)
- `KeroseneLampInstance` exposes UnityEvents:
  - `OnEnterInBackpack`
  - `OnEnterHeld`
  - `OnEnterDropped`
  - `OnEnterDisabled`
- Use these events to bind visuals or VFX per state.
- `LampRegionLightController` handles area-based light visibility:
  - Set `areaId` (or use `KeroseneLampPreplaced.AreaId`).
  - Assign light/VFX renderers to its visual targets.
- State transitions are handled by backpack/held/drop logic:
  - InBackpack: physics/colliders off; visuals hidden by default.
  - Held: attached to `holdPoint`; physics/colliders off.
  - Dropped: physics/colliders on at drop position.
  - Disabled: inert; visuals driven by UnityEvents.

## 5. Interaction Priority Rule
- Selection order is deterministic:
  1. Higher `Interactable.priority`
  2. Closer distance
  3. Earlier enter order

## 6. Save System Integration
1. Add `BackpackSaveAdapter` to the scene.
   - SaveKey: `feature.backpack.items`
   - Assign `BackpackItemDatabase`
2. For Kerosene Lamp instance binding:
   - Ensure `KeroseneLampSaveAdapter` restores before `BackpackSaveAdapter`.
   - Set `LoadOrder` in inspectors accordingly.
3. Add `BackpackItemDatabase` and include all `ItemDefinition` assets used.

## 7. Notes
- Pickup does not auto-hold; use scroll to select and hold.
- Dropping uses held item only; after drop, held slot clears.
- If you want a starting lamp, keep `KeroseneLampManager.spawnHeldLampOnStart` enabled.
