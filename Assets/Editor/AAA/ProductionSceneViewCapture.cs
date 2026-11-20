#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VR147.AAA.Editor
{
    // One-shot diagnostic capture from the existing Scene View camera.
    // Does not enter Play Mode, change scene data, or save the scene.
    [InitializeOnLoad]
    public static class ProductionSceneViewCapture
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string OutputPath = "Logs/renders/Bay2_SceneView_Render_20261119.png";
        private const string SessionKey = "147VR.Bay2.SceneViewCapture.Attempted";

        static ProductionSceneViewCapture()
        {
            EditorApplication.delayCall += TryCaptureOnce;
        }

        [MenuItem("AAA/Diagnostics/Capture Bay2 from Scene View")]
        public static void CaptureFromMenu()
        {
            Capture();
        }

        private static void TryCaptureOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            if (SceneManager.GetActiveScene().path != ScenePath)
                return;

            if (SceneView.lastActiveSceneView == null)
                return;

            SessionState.SetBool(SessionKey, true);
            try { Capture(); }
            catch (Exception exception)
            {
                Debug.LogError("[Bay2SceneViewCapture] FAILED: " + exception);
            }
        }

        public static void Capture()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Exit Play Mode before Scene View capture.");

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
                throw new InvalidOperationException("Active scene is not WipRoom: " + activeScene.path);

            var view = SceneView.lastActiveSceneView;
            if (view == null || view.camera == null)
                throw new InvalidOperationException("No active Scene View camera. Open the Scene tab and retry.");

            var bayRoot = GameObject.Find("ROOT_PoolTable_02");
            if (bayRoot == null)
                bayRoot = GameObject.Find("SM_TABLE_FRAME_CLEAN");
            if (bayRoot == null)
                throw new InvalidOperationException("Could not find Bay2 root or SM_TABLE_FRAME_CLEAN.");

            var renderers = bayRoot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException("Bay2 root has no enabled renderers to frame.");

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // Frame the Bay2 assembly in the editor viewport only; no scene transforms are changed.
            view.Frame(bounds, false);
            view.Repaint();

            const int width = 1920;
            const int height = 1080;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                var camera = view.camera;
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("URP does not support Scene View camera render requests.");

                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply(false, false);

                System.IO.Directory.CreateDirectory("Logs/renders");
                System.IO.File.WriteAllBytes(OutputPath, image.EncodeToPNG());
                Debug.Log("[Bay2SceneViewCapture] SUCCESS path=" + OutputPath +
                    " camera=SceneView size=" + width + "x" + height +
                    " framed=" + bayRoot.name + " renderers=" + renderers.Length +
                    " sceneSaved=false");
            }
            finally
            {
                RenderTexture.active = previous;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
