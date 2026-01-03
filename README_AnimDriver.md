# Spine Character Animation Driver

This doc covers the quick setup for `SpineCharacterAnimDriver` and the optional editor scan tool.

## Setup

1. Add a `SkeletonAnimation` component to the character (or a child object) and assign the `SkeletonDataAsset`.
2. Add `SpineCharacterAnimDriver` to the same GameObject as `PlatformerCharacterController` (or a parent).
3. Drag the `SkeletonAnimation` reference into the driver. The driver maps ThatCompanyGJ animation names internally.
4. Tune thresholds and mix values if transitions feel too sharp.

## Animation Name Mapping

- This driver targets the naming from `Assets/Art/ThatCompanyGJ/主行走图.json`.
- If names change, update the constants in `SpineCharacterAnimDriver.cs`.
- You can enable the debug browser and use `[` / `]` to cycle animations.

## Expression Keys

- `4`: blink (one-shot)
- `5`: eyes shut (persistent)
- `6`: upset mouth (persistent)
- `7`: shocked eyes (persistent)
- `8`: puzzled/scratch (one-shot)
- `9`: clear expression track

## Auto Expressions

- Auto blink/scratch run during idle and walking.
- Tune in `SpineCharacterAnimDriver` via `blinkIntervalRange` and `scratchIntervalRange`.

## State Inputs Used

The driver reads from `IPlayerCharacter2DModel` (grounded, climbing, velocity, move input) via QFramework.
If the model is unavailable, it falls back to `PlatformerCharacterController` and `Rigidbody2D`.

## Debug Browser

- Enable `enableDebugBrowser` in the inspector.
- Use `[` and `]` to step through all animations on Track 0.

## Report Tool

Menu: `Tools/Spine/Scan Character Animations`

- Select a `SkeletonDataAsset` in the Project window and run the tool.
- Output file: `Assets/Art/SpineAnimationReport.md`
