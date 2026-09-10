using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class LunaPhase4StaticAudit
{
 public static void Run(){var s=EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity",OpenSceneMode.Single);var setup=Object.FindFirstObjectByType<SnookerPhysicsSetup>(FindObjectsInactive.Include);if(!setup)throw new System.Exception("SnookerPhysicsSetup missing");Debug.Log($"[PHASE4 STATIC] catchRadius={setup.catchRadius:F6} catchY={setup.catchY:F6} pocketGapHalf={setup.pocketGapHalf:F6} railThickness={setup.railThickness:F6} railHeight={setup.railHeight:F6}");var catches=Object.FindObjectsByType<SnookerPocketCatch>(FindObjectsInactive.Include,FindObjectsSortMode.None);Debug.Log($"[PHASE4 STATIC] existingPocketCatchComponents={catches.Length}");foreach(var c in catches)Debug.Log($"[PHASE4 STATIC] pocket={c.name} pos={c.transform.position.ToString("F6")} enabled={c.enabled}");EditorApplication.Exit(0);}
}
