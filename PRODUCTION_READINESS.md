# 147 VR — Production Readiness

## GATE 3

### Source control

- `main` is the protected working baseline branch by convention.
- Every Gate completion is represented by a commit.
- Large binary assets are covered by Git LFS attributes.
- Generated Unity folders remain excluded from source control.

### Validation

The project already contains `Assets/Editor/ProjectValidator.cs`.
Its contract is to open enabled build scenes, inspect every child `MonoBehaviour`, detect missing script references, and return a non-zero exit code on failure.

### Build configuration

Unity Editor version is pinned by `ProjectSettings/ProjectVersion.txt` to `6000.4.4f1`.
XR dependencies are present through XR Management and OpenXR.
URP is pinned to `17.4.0`.
Input System is pinned to `1.19.0`.

### Asset workflow

Binary-heavy source assets are routed through Git LFS attributes. Blender remains an optional production tool and is intentionally not required for normal code/Git work.

### Safe handoff

`GATE_STATUS.md`, `TODO.md`, `ARCHITECTURE.md`, and this file are the persistent handoff set. A fresh session can determine project state without relying on chat history.
