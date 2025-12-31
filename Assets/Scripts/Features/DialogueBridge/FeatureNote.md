# Feature: DialogueBridge

## 1. Purpose
- Provide a single in-game source of truth for two Dialogue System number variables and bridge them to the save system.

## 2. Folder & Key Files
- Root: `Assets/Scripts/Features/DialogueBridge/`
- Unity bridge:
  - `Assets/Scripts/Independents/DialogueVariableBridge.cs` - singleton bridge + SaveParticipant

## 3. Runtime Wiring
### 3.1 Root registration (`GameRootApp`)
- None (MonoBehaviour singleton, not registered in QFramework architecture).

### 3.2 Scene setup (Unity)
- Required MonoBehaviours:
  - `DialogueVariableBridge` on a persistent GameObject (optional `DontDestroyOnLoad`).
- Inspector fields:
  - `Variable A Name` / `Variable B Name` - Dialogue System variable names (number).
  - `Sync From Dialogue On Enable` - pull values from Dialogue System on enable.
  - `Save Key` - save block key for SaveSystem.

## 4. Public API Surface (How other Features integrate)
### 4.1 Events (Outbound)
- `DialogueVariableBridge.ValueChanged`
  - When fired: whenever the bridge detects or applies a value change.
  - Payload: `(string variableName, float value)`

### 4.2 Request Events (Inbound write requests, optional)
- None.

### 4.3 Commands (Write Path)
- None (use bridge methods).

### 4.4 Queries (Read Path, optional)
- None (use bridge methods).

### 4.5 Model Read Surface
- `DialogueVariableBridge.VariableAValue`, `DialogueVariableBridge.VariableBValue`
- `DialogueVariableBridge.TryGet(...)`

## 5. Typical Integrations
- Set a dialogue variable:
  ```csharp
  DialogueVariableBridge.TrySet("VarA", 1f);
  ```
- Listen for changes:
  ```csharp
  DialogueVariableBridge.Instance.ValueChanged += OnDialogueVariableChanged;
  ```

## 6. Verify Checklist
1. Add `DialogueVariableBridge` to a scene object, assign variable names, enter Play.
2. Change the variable in Dialogue System or via bridge and confirm the bridge updates and Dialogue System reflects it.
3. Trigger Save/Load and verify variables persist.

## 7. UNVERIFIED (only if needed)
- None.
