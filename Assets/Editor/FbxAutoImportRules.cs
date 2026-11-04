using UnityEditor;
using UnityEngine;

namespace _147VR.ArtPipeline
{
    /// <summary>
    /// Auto-import rules for Blender -> Unity FBX/GLB drops (Unity 6-safe API subset).
    /// Anything placed under Assets/AAA/Imports/ is configured on import:
    ///   - metres scale (useFileScale + globalScale 1)
    ///   - materials searched recursively so existing AAA materials are reused by name
    ///   - meshes: not readable, no blendshapes, no animation (static props/room)
    ///   - textures: 2K cap (Quest RAM), mipmaps on, sRGB only for colour,
    ///     linear for normal / mask / occlusion-roughness-metallic channels
    /// Bulk re-apply: menu 147VR/Art/Re-Apply Import Rules (folder under Assets/AAA/Imports selected).
    /// </summary>
    public sealed class FbxAutoImportRules : AssetPostprocessor
    {
        const string RootFolder = "Assets/AAA/Imports";
        static readonly string[] LinearMarkers = { "normal", "nrm", "_n", "mask", "occlusion", "roughness", "metallic", "orm", "ao" };

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(RootFolder)) return;
            var imp = (ModelImporter)assetImporter;
            imp.useFileScale   = true;
            imp.globalScale    = 1.0f;
            imp.materialSearch = ModelImporterMaterialSearch.RecursiveUp;   // reuse existing AAA materials by name
            imp.animationType  = ModelImporterAnimationType.None;           // room/table/props are static
            imp.importBlendShapes = false;
            imp.isReadable     = false;
            imp.importVisibility = true;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(RootFolder)) return;
            var imp = (TextureImporter)assetImporter;
            string n = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();

            bool linear = false;
            foreach (var m in LinearMarkers) if (n.Contains(m)) { linear = true; break; }

            if (n.Contains("normal")) imp.textureType = TextureImporterType.NormalMap;
            else imp.textureType = TextureImporterType.Default;
            imp.sRGBTexture      = !linear;
            imp.maxTextureSize   = 2048;                       // Quest memory reality; raise per-asset if needed
            imp.mipmapEnabled    = true;
            imp.wrapMode         = TextureWrapMode.Repeat;
            imp.textureCompression = TextureImporterCompression.Compressed;
        }

        [MenuItem("147VR/Art/Re-Apply Import Rules (selected folder under Assets/AAA/Imports)", false, 20)]
        public static void ReApply()
        {
            string sel = Selection.assetGUIDs.Length > 0 ? AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]) : null;
            if (string.IsNullOrEmpty(sel) || !sel.StartsWith(RootFolder))
            { Debug.LogError($"[ImportRules] Select a folder under {RootFolder} first."); return; }

            int touched = 0;
            foreach (var guid in AssetDatabase.FindAssets("", new[] { sel }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ai   = AssetImporter.GetAtPath(path);
                if (ai is ModelImporter || ai is TextureImporter)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    touched++;
                }
            }
            Debug.Log($"[ImportRules] re-applied rules to {touched} assets under {sel}");
        }
    }
}
