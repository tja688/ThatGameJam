# Feature: BackpackFeature

## 1. Purpose
- Maintain backpack item data, selection index, and held item state.
- Provide commands for add/drop/select/hold and queries for external lookup.
- Expose events for UI refresh and held-slot synchronization.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/BackpackFeature/`
- Controllers:
  - `InventorySelectorController.cs` 〞 scroll-driven selection + held slot
  - `BackpackWheelUIController.cs` 〞 3-slot wheel UI refresh + fade
  - `BackpackUIProvider.cs` 〞 `IInventoryProvider` bridge for UI Toolkit panel
  - `BackpackItemDatabase.cs` 〞 ItemDefinition lookup for save restore
- Models:
  - `IBackpackModel`, `BackpackModel` 〞 items + selected/held indices
  - `ItemDefinition` 〞 item metadata (id, icon, prefab, tags)
  - `BackpackSaveState` 〞 save data schema
- Commands:
  - `AddItemCommand`, `RemoveItemCommand`
  - `SetSelectedIndexCommand`, `SetHeldItemCommand`
  - `DropHeldItemCommand`, `ResetBackpackCommand`
- Queries:
  - `GetBackpackItemsQuery`, `GetSelectedIndexQuery`, `GetHeldIndexQuery`
  - `ContainsItemQuery`, `FindItemByIdQuery`, `FindItemsByTagQuery`
  - `GetHeldItemQuery`
- Events:
  - `BackpackChangedEvent`
  - `BackpackSelectionChangedEvent`
  - `HeldItemChangedEvent`

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- Registered modules:
  - Model: `ThatGameJam.Features.BackpackFeature.Models.IBackpackModel`

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `InventorySelectorController` on player (bind `holdPoint`).
  - `BackpackUIProvider` on a scene object that lives with UI.
  - `BackpackItemDatabase` on a scene object (list all `ItemDefinition` assets).
- Optional UI:
  - `BackpackWheelUIController` on your wheel UI root.

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `struct BackpackChangedEvent`
  - When fired: items added/removed or reset.
  - Typical listener: UI refresh.
- `struct BackpackSelectionChangedEvent`
  - When fired: selected index changes.
- `struct HeldItemChangedEvent`
  - When fired: held index changes.

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- `AddItemCommand`
  - Mutates: add/stack item in backpack.
  - Typical sender: `Interactable` pickup.
- `DropHeldItemCommand`
  - Mutates: remove held item and spawn/drop into world.
  - Typical sender: self-drop or death logic.
- `SetSelectedIndexCommand` / `SetHeldItemCommand`
  - Mutates: selection and held slot.

### 4.4 Queries (Read Path, optional)
- `GetBackpackItemsQuery`
  - Returns: ordered list of `BackpackItemInfo`.
- `FindItemByIdQuery` / `FindItemsByTagQuery`
  - Returns: lookup results by id or tag.

### 4.5 Model Read Surface
- Bindables:
  - `IBackpackModel.SelectedIndex`
  - `IBackpackModel.HeldIndex`

## 5. Typical Integrations
- Inventory UI listens to `BackpackChangedEvent` then queries `GetBackpackItemsQuery`.
- Scroll wheel input drives `SetSelectedIndexCommand` and `SetHeldItemCommand`.

## 6. Verify Checklist
1. Add items via pickup, confirm `BackpackChangedEvent` fires.
2. Scroll changes selection; held slot updates if enabled.
3. Drop held item spawns prefab or updates instance.
4. UI provider populates PlayerPanel inventory slots.

## 7. UNVERIFIED (only if needed)
- None.
