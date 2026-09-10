using UnityEditor;
using UnityEngine;

public static class M5_1_LifecycleVerify_TMP
{
    public static void Run()
    {
        var lifecycle = Object.FindObjectOfType<M5ShotLifecycle>();
        if (lifecycle == null) { Debug.LogError("[M5.1 FAIL] M5ShotLifecycle not found"); EditorApplication.Exit(1); return; }

        lifecycle.ResetToIdle();
        int started = 0;
        lifecycle.ShotStarted += () => started++;
        int before = lifecycle.ShotSequence;
        bool first = lifecycle.BeginShot();
        bool second = lifecycle.BeginShot();

        bool ok = first && !second && lifecycle.CurrentState == M5ShotLifecycle.State.Active
                  && lifecycle.ShotSequence == before + 1 && started == 1;
        Debug.Log(ok
            ? $"[M5.1 PASS] BeginShot accepted once seq={lifecycle.ShotSequence}; duplicate rejected; state={lifecycle.CurrentState}; ShotStarted={started}"
            : $"[M5.1 FAIL] first={first} second={second} state={lifecycle.CurrentState} seq={lifecycle.ShotSequence} started={started}");

        lifecycle.ShotStarted -= () => started++;
        lifecycle.ResetToIdle();
        EditorApplication.Exit(ok ? 0 : 1);
    }
}
