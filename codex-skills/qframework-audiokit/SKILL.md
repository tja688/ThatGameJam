---
name: qframework-audiokit
description: Use QFramework AudioKit only when the user explicitly requests QFramework AudioKit or its Music/Sound/Voice APIs and settings. Do not use when the user wants Unity AudioSource or avoids QFramework toolkits. Use alongside qframework-playbook and qframework-core-architecture in this repo.
---

# QFramework AudioKit

## Overview

Apply AudioKit for music, sound, and voice playback, plus settings and loader overrides.

## Workflow

1. Confirm explicit request for QFramework AudioKit; if the user wants Unity audio or avoids QFramework toolkits, stop and follow the override.
2. Apply `qframework-playbook` and `qframework-core-architecture` rules.
3. Use AudioKit playback APIs for music, sound, and voice; use AudioKit.Settings for toggles and volume.
4. If using custom audio loaders, set `AudioKit.Config.AudioLoaderPool` in `ProjectToolkitBootstrap` before first AudioKit use.
5. If sequencing audio with actions is requested, use ActionKit support where applicable.

## Verification

- Verify APIs in local docs first; mark unknowns with `UNVERIFIED` and provide `NEXT_SEARCH`.

## Resources

- `references/audiokit_rules.md` for source paths, key APIs, and lookup keywords.
