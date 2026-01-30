---
name: unity-codeintel
description: Semantic code navigation and search for Unity C# projects via the local CodeIntel bridge (OmniSharp). Use when a Unity task requires precise implementation details, symbol resolution, definitions, or references across project code or third-party plugins, and fast evidence gathering is needed.
---

# Unity CodeIntel

## Overview

Use the Unity CodeIntel bridge to perform semantic queries (definition, references, symbol search) against the Unity C# solution via HTTP, without opening files manually. This is the fastest way to confirm exact implementations and reduce context size.

## Quick Start (PowerShell)

1) Ensure the service is running

- In Unity: `Window > CodeIntel > Dashboard` and click `Start Services` (or enable auto-start).

2) Discover the Bridge base URL and token

```powershell
$root = (Get-Location).Path
$config = Get-Content "$root\Tools\CodeIntel\bridge-config.json" | ConvertFrom-Json
$logDir = $config.logDir
$endpoints = Get-Content "$root\$logDir\codeintel-endpoints.json" | ConvertFrom-Json
$baseUrl = $endpoints.bridge.baseUrl
$token = $endpoints.bridge.token
```

3) Build headers (only when token is set)

```powershell
$headers = @{}
if ($token) { $headers["Authorization"] = "Bearer $token" }
```

4) Run semantic queries

Definition:

```powershell
$body = @{ file = "Assets/Scripts/Foo.cs"; line = 10; col = 15 } | ConvertTo-Json
Invoke-RestMethod "$baseUrl/v1/definition" -Method Post -Headers $headers -ContentType "application/json" -Body $body
```

References:

```powershell
$body = @{ file = "Assets/Scripts/Foo.cs"; line = 10; col = 15; includeDeclaration = $true } | ConvertTo-Json
Invoke-RestMethod "$baseUrl/v1/references" -Method Post -Headers $headers -ContentType "application/json" -Body $body
```

Symbol search:

```powershell
$body = @{ query = "PlayerController" } | ConvertTo-Json
Invoke-RestMethod "$baseUrl/v1/symbols" -Method Post -Headers $headers -ContentType "application/json" -Body $body
```

## Workflow Guidance

1) Prefer semantic queries when you need authoritative answers

- **Definition**: "Where is this symbol implemented exactly?"
- **References**: "Who calls/uses this type or member?"
- **Symbols**: "List all symbols matching a name fragment or pattern."

2) Get precise `file/line/col` inputs

- Use Unity or text search (`rg -n`) to find an approximate location, then pass the exact line/col to `/v1/definition` or `/v1/references`.
- Use project-relative paths like `Assets/...` (matching the README examples).

3) Keep context small

- Use symbol search to narrow candidates, then pull only the minimal files/locations you need.

## Health Check and Troubleshooting

Health probe (no token needed):

```powershell
Invoke-RestMethod "$baseUrl/health"
```

If endpoints file is missing or stale:

- Open the CodeIntel Dashboard and confirm services are running.
- Re-check the configured log directory in `Tools/CodeIntel/bridge-config.json`.
- If `bridgePort=0`, the actual port is written to `Library/CodeIntelLogs/codeintel-endpoints.json` (or the configured `logDir`).

For deeper details, read the plugin docs at `Assets/Editor/CodeIntel/README.md`.
