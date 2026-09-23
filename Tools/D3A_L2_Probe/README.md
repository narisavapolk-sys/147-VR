# D3A L2 Package Manager endpoint probe

Purpose: reproduce the three Package Manager IPC endpoints on Unity 6000.4.12f1 without placing executable probe code in Assets/ on the repository.

## Placement

The source lives at Tools/D3A_L2_Probe/D3A_L2_Probe.cs. It is intentionally outside Assets/ and therefore is not compiled or executed by a normal project open.

## Fresh-project procedure

1. Use a fresh empty Unity 6000.4.12f1 project.
2. Copy D3A_L2_Probe.cs into the throwaway project's Assets/Editor/.
3. Start a deliberately detached long-lived process and record its PID as the UPM liveness token.
4. Start UnityPackageManager.exe from the 6000.4.12f1 Editor/Data/Resources/PackageManager/Server directory with:
   server -s <TOKEN_PID> --ipc-path Unity-D3A-L2-ipc -l 2
5. Launch Unity with an absolute log path and:
   -batchmode -nographics -projectPath <EMPTY_PROJECT> -upmIpcPath D3A-L2-ipc -executeMethod D3A_L2_Probe.Run
6. Do not add -quit; the probe owns shutdown after the asynchronous requests finish.
7. Read the Unity log and the UPM log.
8. Delete the copied probe and its .meta file from the throwaway project.

## Expected endpoint evidence

UPM log must contain successful 200 responses for:
- project:list-packages
- packages:get-all-packageinfo
- config:project:get-registries

The registry path is non-obvious: the probe resolves the internal
UnityEditor.PackageManager.UI.Internal.UpmRegistryClient through its
ServicesContainer, then invokes CheckRegistriesChanged() on that initialized service.
Its getRegistriesOperation property is used only to observe completion state.

## Determinism / validity

Capture Unity argv, CWD, Unity version, absolute log path, UPM log path, HEAD,
and SHA256 values. A launcher timeout, missing argv/CWD, relative log path, or
unknown tree/HEAD makes the run INVALID rather than FAIL.

## Negative acceptance

After this directory is committed, opening the real integration project must
produce no Assets/** mutation, no new probe file, and no D3A L2 probe execution
line in the Unity log. If the probe can execute from the repository without
being copied into a throwaway project's Assets/Editor, placement is wrong;
do not solve that with a guard flag.
