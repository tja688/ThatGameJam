# LightVitality Feature Note

1) Capability
- Provides global light-as-vitality state with Current/Max, CQRS commands for writes, queries for reads, and events for cross-feature notifications.

2) Scene setup (Unity)
- Create an empty GameObject (e.g., "LightVitalityDebug").
- Add `LightVitalityDebugController`.

3) Configuration
- Default Max/Initial light is 100/100 (hard-coded in `LightVitalityModel`).
- Debug controller settings:
  - `addAmount`, `consumeAmount`, `addKey`, `consumeKey`, `setToMaxKey`.

4) Minimal verification steps
- Enter Play Mode.
- Press the consume key to reduce light; verify LogKit output shows decreasing values.
- Press the add key to restore light; verify LogKit output increases.
- Press the set-to-max key to reset current to max.

5) Common pitfalls
- Do not write light values from Controllers/Systems directly; always use Commands.
- Ensure any event subscriptions are paired with unregistration helpers.
