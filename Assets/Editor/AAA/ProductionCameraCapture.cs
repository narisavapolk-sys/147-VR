#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VR147.AAA.Editor
{
    // Manual diagnostic utility only. Never auto-runs on editor load.
    public static class ProductionCameraCapture
    {
        private const string ScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string OutputPath = "Logs/renders/ProductionCamera_WipRoom_20261119.png";

        [MenuItem("AAA/Diagnostics/Capture Production Camera (Play Mode Required)")]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("Enter Play Mode first: XR/runtime camera may be created only at runtime.");

            var camera = Camera.main;
            if (camera == null || !camera.enabled || !camera.gameObject.activeInHierarchy)
                throw new InvalidOperationException("No active MainCamera exists in Play Mode.");

            const int width = 1920, height = 1080;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            var previous = RenderTexture.active;
            try
            {
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("URP does not support SingleCameraRequest for selected runtime camera.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply(false, false);
                System.IO.Directory.CreateDirectory("Logs/renders");
                System.IO.File.WriteAllBytes(OutputPath, image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
                Debug.Log("[ProductionCameraCapture] SUCCESS path=" + OutputPath +
                    " camera=" + camera.name + " size=" + width + "x" + height +
                    " scene=" + ScenePath + " sceneSaved=false");
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
