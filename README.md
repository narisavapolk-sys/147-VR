# 147 VR

A VR snooker and pool game for **Meta Quest 2 / 3**, built with Unity.

> Status: pre-release. Core systems are implemented; runtime verification on device is in progress.

---

## Requirements

| Tool | Version |
|---|---|
| Unity | `6000.4.4f1` (pinned in `ProjectSettings/ProjectVersion.txt`) |
| Render pipeline | URP `17.4.0` |
| Input | Input System `1.19.0` |
| XR | XR Management + OpenXR |
| Git LFS | required |

## Getting started

```bash
git lfs install
git clone https://github.com/narisavapolk-sys/147-VR.git
cd 147-VR
```

Large binaries (`.blend`, `.fbx`) are stored in **Git LFS**. If you clone without Git LFS installed, those files arrive as small text pointer files and the project will not import correctly.

Open the folder with Unity Hub using the exact Unity version above.

## Build scenes

| Scene | Purpose |
|---|---|
| `Assets/Scenes/147VR_MainScene.unity` | Main VR scene |
| `Assets/Scenes/SampleScene.unity` | Sandbox / experiments |
| `Assets/Scenes/PoolTable_8Ball.unity` | 8-ball table |
| `Assets/Scenes/PoolTable_9Ball.unity` | 9-ball table |

## Architecture

The gameplay spine is one-way; simulation never depends on presentation.

```
Cue Controller -> Physics -> Ball Tracker -> Shot Tracker -> Score Manager -> Turn Manager -> UI / View
```

| Layer | Location | Role |
|---|---|---|
| Gameplay spine | `Assets/Scripts/Quest/` | Cue, tracking, scoring, turns |
| Physics research | `Assets/Scripts/AAA/Physics/` | Ball motion, cushion, pocket, calibration |
| Golden regression | `Assets/Scripts/AAA/Physics/Golden/` | Measured-truth physics test cases |
| Table variants | `Assets/Scripts/PoolTable/` | 8-ball / 9-ball rules |
| Editor tooling | `Assets/Editor/` | Validation and import automation |

Full contract and dependency rules: [`ARCHITECTURE.md`](ARCHITECTURE.md)

### Physics approach

Ball behaviour is not hand-tuned. Shots are executed in a controlled scene, measured, and compared against **Golden Cases** derived from real-table measurements. Cushion response, pocket capture, and spin (stun / follow / draw / english) each have dedicated batch runners.

## Repository layout

```
Assets/          Unity project assets
Packages/        Package manifest and locked packages
ProjectSettings/ Unity project configuration
Docs/            Design notes, handoff state, working memory
SNOOKER   VR pool table/  Blender source files (LFS)
```

Generated Unity folders (`Library/`, `Temp/`, `Logs/`, `Obj/`, `UserSettings/`) are intentionally excluded.

## Project documentation

| File | Contents |
|---|---|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | System boundaries and dependency rules |
| [`PRODUCTION_READINESS.md`](PRODUCTION_READINESS.md) | Build, validation, and asset workflow |
| [`GATE_STATUS.md`](GATE_STATUS.md) | Production gate acceptance record |
| [`TODO.md`](TODO.md) | Roadmap and open work |
| [`147VR_WORKING_DIRECTIVE.md`](147VR_WORKING_DIRECTIVE.md) | Working rules for contributors and AI assistants |

## Conventions

- Source completeness is never reported as runtime completeness. A feature is "done" only after Unity compile and runtime verification.
- Every meaningful work block updates the persistent handoff docs so a fresh session can resume without chat history.
- Never commit credentials, tokens, or API keys. Runtime secrets belong outside source control.
- `main` is the baseline branch. Changes arrive through pull requests.

## License

All rights reserved. This repository is private and not licensed for redistribution.
