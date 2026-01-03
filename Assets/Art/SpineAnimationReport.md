# Spine Animation Report (postman)

Generated: 2026-01-02T19:13:37

Source: Assets/Art/postman (Spine JSON + atlas)

## Summary
- Skins: default
- Events: (none)
- Animations: 17 total

## Animation List
| Name | Duration (s) | Loop | Category | Confidence | Track |
| --- | ---: | :---: | :--- | :---: | :---: |
| back/climb_horizon | 0.500 | yes | Climb | medium | 0 |
| back/climb_verticle | 0.700 | yes | Climb | medium | 0 |
| back/default_rev | 0.000 | maybe | Unknown | low | 0 |
| front/animations/blink | 0.800 | yes | Overlay | medium | 1 |
| front/animations/handsswing | 0.500 | maybe | Overlay | medium | 1 |
| front/animations/puzzled | 1.200 | maybe | Unknown | low | 0 |
| front/animations/standby | 0.800 | yes | Idle | medium | 0 |
| front/animations/walk | 0.500 | yes | Run | medium | 0 |
| front/animations/walk_1 | 0.500 | yes | Run | medium | 0 |
| front/animations/walk_2 | 0.500 | yes | Run | medium | 0 |
| front/skin/additional/with_eyes_shut | 0.000 | maybe | Overlay | medium | 1 |
| front/skin/additional/with_mouth_shut | 0.000 | maybe | Overlay | medium | 1 |
| front/skin/default | 5.333 | maybe | Unknown | low | 0 |
| static/static_back_hang | 0.000 | yes | Idle | medium | 0 |
| static/static_back_stand | 0.000 | yes | Idle | medium | 0 |
| static/static_front | 0.000 | yes | Idle | medium | 0 |
| static/static_front_eyes_shut | 0.000 | yes | Overlay | medium | 1 |

## Recommended Mapping (Draft)
### Base (Track 0)
- Idle: front/animations/standby
- Run/Walk: front/animations/walk
- JumpUp: (missing)
- Fall: (missing)
- Land: (missing)
- ClimbMove (vertical): back/climb_verticle
- ClimbMove (horizontal): back/climb_horizon
- ClimbIdle: static/static_back_hang

### Overlay (Track 1)
- Blink/Face: front/animations/blink
- UpperBody/Hands: front/animations/handsswing
- Breath/IdleAdd: (missing)
- HitReact: (missing)

## Unknown or Ambiguous
- back/default_rev
- front/animations/puzzled
- front/skin/default

## Notes
- Some entries report 0.0s duration because they only swap skins/attachments or have no keyed timelines.
- No jump/fall/land animations were found in this asset; driver will fall back to Idle/Run unless names are supplied.