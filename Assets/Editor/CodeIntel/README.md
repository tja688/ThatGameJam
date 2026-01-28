# Unity Code Intel Service

This tool provides a bridge between Unity Editor and OmniSharp, exposing a semantic code intelligence API for external agents.

## Installation

1.  Copy `Assets/Editor/CodeIntel` to your Unity Project's `Assets/Editor/` folder.
2.  Copy `Tools/CodeIntel` to your Unity Project's `Tools/` folder (or project root).
3.  **Important:** Download OmniSharp HTTP executable and place it in `Tools/CodeIntel/omnisharp/OmniSharp.exe`.
    *   You can download it from [OmniSharp Releases](https://github.com/OmniSharp/omnisharp-roslyn/releases).
    *   Ensure `omnisharpExePath` in `bridge-config.json` points to the correct executable.

## Features Implemented (Phase 1-3)

*   **Editor Window:** Control OmniSharp process, view logs, and monitor status. (Window > CodeIntel > Dashboard)
*   **Bridge Server:** HTTP Server running on `localhost` (default port 8080 or random).
*   **Agent API:**
    *   `GET /health`: Check service status.
    *   `POST /v1/definition`: Go to definition.
    *   `POST /v1/references`: Find usages.
    *   `POST /v1/symbols`: Search symbols.
*   **Auto-Recovery:** Automatically restarts OmniSharp on Unity Domain Reload and Compilation to ensure sync.

## Configuration

Edit `Tools/CodeIntel/bridge-config.json` to customize ports, paths, and restart behavior.

## Usage

1.  Open Unity Project.
2.  Open **Window > CodeIntel > Dashboard**.
3.  Click **Start Services** (if not auto-started).
4.  Use external agent to query `http://127.0.0.1:{Port}/...`.
