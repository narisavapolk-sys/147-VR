using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VR147.AAA.Editor
{
    public static class WipRoomGameplaySystemsInstaller
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";

        public static void ApplyBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Could not open WipRoom.");

            GameObject[] questRoots = scene.GetRootGameObjects()
                .Where(go => go != null && go.name == "Quest Setup")
                .ToArray();
            if (questRoots.Length != 1)
                throw new InvalidOperationException("Expected exactly one root GameObject named Quest Setup; found " + questRoots.Length + ".");

            GameObject questSetup = questRoots[0];
            SnookerBallTracker ballTracker = questSetup.GetComponent<SnookerBallTracker>();
            SnookerShotTracker shotTracker = questSetup.GetComponent<SnookerShotTracker>();
            SnookerTurnManager turnManager = questSetup.GetComponent<SnookerTurnManager>();
            SnookerScoreManager scoreManager = questSetup.GetComponent<SnookerScoreManager>();
            M5ShotLifecycle lifecycle = questSetup.GetComponent<M5ShotLifecycle>();

            if (ballTracker == null || shotTracker == null || turnManager == null || scoreManager == null || lifecycle == null)
                throw new InvalidOperationException("Quest Setup is missing one or more required gameplay components. No scene changes were saved.");

            int duplicateBallTrackers = questSetup.GetComponents<SnookerBallTracker>().Length;
            int duplicateShotTrackers = questSetup.GetComponents<SnookerShotTracker>().Length;
            int duplicateTurnManagers = questSetup.GetComponents<SnookerTurnManager>().Length;
            int duplicateScoreManagers = questSetup.GetComponents<SnookerScoreManager>().Length;
            int duplicateLifecycles = questSetup.GetComponents<M5ShotLifecycle>().Length;
            if (duplicateBallTrackers != 1 || duplicateShotTrackers != 1 || duplicateTurnManagers != 1 || duplicateScoreManagers != 1 || duplicateLifecycles != 1)
                throw new InvalidOperationException("Duplicate gameplay components detected on Quest Setup. No scene changes were saved.");

            // Preserve the existing single rules stack; repair only its references.
            shotTracker.ballTracker = ballTracker;
            shotTracker.scoreManager = scoreManager;
            shotTracker.turnManager = turnManager;
            shotTracker.shotLifecycle = lifecycle;
            scoreManager.ballTracker = ballTracker;
            scoreManager.turnManager = turnManager;
            lifecycle.ballTracker = ballTracker;
            shotTracker.snookerRulesEnabled = true;

            SerializedObject trackerObject = new SerializedObject(ballTracker);
            SerializedProperty autoDetect = RequireProperty(trackerObject, "autoDetectOnStart");
            autoDetect.boolValue = false; // PoolPhysicsRulesBridge binds the active bay and its exact simulator order.
            trackerObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject scoreObject = new SerializedObject(scoreManager);
            RequireProperty(scoreObject, "player1Score").intValue = 0;
            RequireProperty(scoreObject, "player2Score").intValue = 0;
            RequireProperty(scoreObject, "redsRemaining").intValue = 15;
            RequireProperty(scoreObject, "colourIndex").intValue = 0;
            RequireProperty(scoreObject, "ballOn").enumValueIndex = 0;
            RequireProperty(scoreObject, "frameOver").boolValue = false;
            scoreObject.ApplyModifiedPropertiesWithoutUndo();

            questSetup.SetActive(true);
            EditorUtility.SetDirty(questSetup);
            EditorUtility.SetDirty(ballTracker);
            EditorUtility.SetDirty(shotTracker);
            EditorUtility.SetDirty(turnManager);
            EditorUtility.SetDirty(scoreManager);
            EditorUtility.SetDirty(lifecycle);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("Unity failed to save WipRoom after applying gameplay systems.");

            string report =
                "WipRoom gameplay systems apply: PASS\n" +
                "Scene: " + ScenePath + "\n" +
                "Quest Setup active: " + questSetup.activeSelf + "\n" +
                "Components: SnookerBallTracker=1, SnookerShotTracker=1, SnookerTurnManager=1, SnookerScoreManager=1, M5ShotLifecycle=1\n" +
                "Auto detect on Start: false (runtime bridge owns the active bay binding)\n" +
                "Score reset: 0:0, reds=15, ballOn=Red, colourIndex=0, frameOver=false\n" +
                "References: shotTracker -> tracker/score/turn/lifecycle; scoreManager -> tracker/turn; lifecycle -> tracker\n" +
                "Table root: assigned dynamically by PoolPhysicsRulesBridge on every active-bay bind\n";
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Path.Combine("Logs", "WipRoom_GameplaySystemsApply_20261119.txt"), report, Encoding.UTF8);
            Debug.Log("[WipRoom Gameplay Systems] PASS | Quest Setup activated | rules stack wired | fresh frame score state.");
        }

        private static SerializedProperty RequireProperty(SerializedObject target, string name)
        {
            SerializedProperty property = target.FindProperty(name);
            if (property == null)
                throw new InvalidOperationException("Serialized field not found: " + target.targetObject.GetType().Name + "." + name);
            return property;
        }
    }
}
