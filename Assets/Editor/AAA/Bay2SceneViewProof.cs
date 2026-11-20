#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VR147.AAA.Editor
{
    /// <summary>
    /// Opens the real production scene and frames Bay2 in Scene View for a genuine Editor screenshot.
    /// Read-only: no hierarchy edits, no prefab Apply/Revert, and no scene save.
    /// </summary>
    public static class Bay2SceneViewProof
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";

        public static void OpenForCapture()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Could not open production WipRoom.");

            Transform table = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(t => t.name == "ROOT_PoolTable_02");
            if (table == null)
                throw new InvalidOperationException("Bay2 visual root ROOT_PoolTable_02 was not found.");

            Renderer[] renderers = table.GetComponentsInChildren<Renderer>(true)
                .Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Bay2 has no enabled renderers; cannot produce visual proof.");

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);

            SceneView view = SceneView.lastActiveSceneView;
            if (view == null)
                view = EditorWindow.GetWindow<SceneView>();

            view.orthographic = false;
            view.LookAtDirect(bounds.center, Quaternion.Euler(28f, 38f, 0f),
                Mathf.Max(bounds.extents.magnitude * 2.4f, 4.5f));
            Selection.activeGameObject = table.gameObject;
            view.Repaint();
            SceneView.RepaintAll();

            Debug.Log("[Bay2 SceneView Proof] Read-only WipRoom loaded. Table=" + table.name +
                " Renderers=" + renderers.Length + " BoundsCenter=" + bounds.center.ToString("F3") +
                " BoundsSize=" + bounds.size.ToString("F3") + ". Scene NOT saved.");
        }
    }
}
#endif
