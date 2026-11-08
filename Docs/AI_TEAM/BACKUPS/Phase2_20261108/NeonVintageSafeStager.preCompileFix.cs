#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Non-destructive NeonVintage staging.
    /// Operates ONLY on the owner-approved working copy and adds visual FBX groups.
    /// It never reparents or edits the certified table, colliders, rigidbodies, XR rig,
    /// gameplay objects, tags, layers, or Build Settings.
    /// </summary>
    public static class NeonVintageSafeStager
    {
        private const string WorkingScenePath = "Assets/Scenes/147VR_MainScene_AAA_WORKING_v001.unity";
        private const string SourceRoot = "Assets/AAA/Imports/NeonVintageArtPass";
        private const string MaterialsRoot = SourceRoot + "/Materials";
        private const string SceneRootName = "147VR_AAA_NeonVintage";
        private const string StaticFbxPath = SourceRoot + "/Static_Baked.fbx";
        private const string DynamicFbxPath = SourceRoot + "/Dynamic.fbx";
        private const string ProfilePath = SourceRoot + "/ArtpassPost_v002.asset";
        private const string PostProcessChildName = "147VR_AAA_PostProcess_v002";

        private static readonly StaticEditorFlags StaticVisualFlags =
            StaticEditorFlags.BatchingStatic |
            StaticEditorFlags.ContributeGI |
            StaticEditorFlags.OccluderStatic |
            StaticEditorFlags.OccludeeStatic |
            StaticEditorFlags.ReflectionProbeStatic;

        [MenuItem("147VR/AAA/NeonVintage/Safe/1) Open AAA Working Scene")]
        public static void OpenWorkingScene()
        {
            if (!File.Exists(WorkingScenePath))
            {
                EditorUtility.DisplayDialog("147VR NeonVintage", "Working Scene copy not found:\n" + WorkingScenePath, "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWants())
                return;

            EditorSceneManager.OpenScene(WorkingScenePath, OpenSceneMode.Single);
            Debug.Log("[NeonVintageSafe] Opened owner-approved working copy. Original MainScene was not changed.");
        }

        [MenuItem("147VR/AAA/NeonVintage/Safe/2) Prepare Assets (importers + materials)")]
        public static void PrepareAssets()
        {
            if (!File.Exists(SourceRoot + "/manifest_flat.json"))
            {
                EditorUtility.DisplayDialog("147VR NeonVintage", "manifest_flat.json is missing. Asset preparation cancelled.", "OK");
                return;
            }

            // Existing prepare-only path does not modify any Scene.
            NeonVintageArtPassImporter.MenuPrepareAssets();
        }

        [MenuItem("147VR/AAA/NeonVintage/Safe/3) Stage Static + Dynamic FBX (additive)")]
        public static void StageAggregateFbx()
        {
            Scene scene;
            if (!RequireWorkingScene(out scene))
                return;

            GameObject tableRoot = FindInScene(scene, "147VR_Table_V008_OPT_VISUAL");
            if (tableRoot == null || !tableRoot.activeInHierarchy)
            {
                EditorUtility.DisplayDialog("147VR NeonVintage", "V008 table root is missing or inactive in the working copy. Nothing was changed.", "OK");
                return;
            }

            GameObject staticPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StaticFbxPath);
            GameObject dynamicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DynamicFbxPath);
            if (staticPrefab == null || dynamicPrefab == null)
            {
                EditorUtility.DisplayDialog("147VR NeonVintage",
                    "Aggregate FBX assets are not imported yet.\nRun Safe > 2) Prepare Assets first, then retry.", "OK");
                return;
            }

            if (HasPhysics(staticPrefab) || HasPhysics(dynamicPrefab))
            {
                EditorUtility.DisplayDialog("147VR NeonVintage",
                    "Safety stop: an aggregate visual FBX unexpectedly contains Collider or Rigidbody components. Nothing was staged.", "OK");
                return;
            }

            GameObject sceneRoot = FindRoot(scene, SceneRootName);
            bool createdRoot = false;
            if (sceneRoot != null)
            {
                bool hasStatic = sceneRoot.transform.Find("Static_Baked") != null;
                bool hasDynamic = sceneRoot.transform.Find("Dynamic") != null;
                if (hasStatic && hasDynamic)
                {
                    EditorUtility.DisplayDialog("147VR NeonVintage", "Static_Baked and Dynamic are already staged. No duplicate was created.", "OK");
                    return;
                }
                if (hasStatic || hasDynamic)
                {
                    EditorUtility.DisplayDialog("147VR NeonVintage",
                        "Partial ArtPass root detected. To avoid duplicates, staging stopped. Use Undo or inspect the working Scene first.", "OK");
                    return;
                }
            }
            else
            {
                sceneRoot = new GameObject(SceneRootName);
                Undo.RegisterCreatedObjectUndo(sceneRoot, "Create NeonVintage visual root");
                createdRoot = true;
                sceneRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                sceneRoot.transform.localScale = Vector3.one;
            }

            Dictionary<string, Material> materialMap = BuildMaterialMap();
            if (materialMap.Count == 0)
            {
                if (createdRoot && sceneRoot.transform.childCount == 0)
                    Undo.DestroyObjectImmediate(sceneRoot);
                EditorUtility.DisplayDialog("147VR NeonVintage",
                    "No prepared materials were found under " + MaterialsRoot + ". Run Prepare Assets first.", "OK");
                return;
            }

            GameObject staticInstance = null;
            GameObject dynamicInstance = null;
            try
            {
                staticInstance = InstantiateVisual(staticPrefab, scene, sceneRoot.transform, "Static_Baked");
                dynamicInstance = InstantiateVisual(dynamicPrefab, scene, sceneRoot.transform, "Dynamic");

                ApplyStaticFlags(staticInstance, StaticVisualFlags);
                ApplyStaticFlags(dynamicInstance, StaticEditorFlags.None);

                int remapped = 0;
                int missing = 0;
                RemapMaterials(staticInstance, materialMap, ref remapped, ref missing);
                RemapMaterials(dynamicInstance, materialMap, ref remapped, ref missing);

                if (HasPhysics(staticInstance) || HasPhysics(dynamicInstance))
                    throw new InvalidOperationException("A staged instance contains physics components; staging was rolled back.");

                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("[NeonVintageSafe] Staged aggregate FBX only: Static_Baked (653 objects / 76,874 tris) + Dynamic (59 objects / 9,568 tris). " +
                          "Material slots remapped: " + remapped + "; unmatched slots: " + missing +
                          ". No table/XR/physics objects were edited. Scene is dirty but NOT saved.");
                EditorUtility.DisplayDialog("147VR NeonVintage",
                    "Staged 2 aggregate visual FBX groups.\nMaterial remaps: " + remapped +
                    "\nUnmatched material slots: " + missing +
                    "\n\nScene is NOT saved. Review the rendered result before saving.", "OK");
            }
            catch (Exception ex)
            {
                if (dynamicInstance != null) Undo.DestroyObjectImmediate(dynamicInstance);
                if (staticInstance != null) Undo.DestroyObjectImmediate(staticInstance);
                if (createdRoot && sceneRoot != null && sceneRoot.transform.childCount == 0)
                    Undo.DestroyObjectImmediate(sceneRoot);

                Debug.LogError("[NeonVintageSafe] Stage failed and created instances were rolled back: " + ex);
                EditorUtility.DisplayDialog("147VR NeonVintage", "Staging failed and was rolled back:\n" + ex.Message, "OK");
            }
        }

        [MenuItem("147VR/AAA/NeonVintage/Safe/4) Repair PostProcess Profile (new v002 asset)")]
        public static void RepairPostProcess()
        {
            Scene scene;
            if (!RequireWorkingScene(out scene))
                return;

            GameObject sceneRoot = FindRoot(scene, SceneRootName);
            if (sceneRoot == null)
            {
                EditorUtility.DisplayDialog("147VR NeonVintage", "ArtPass root is not present. Stage the aggregate FBX first.", "OK");
                return;
            }

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                if (File.Exists(ProfilePath))
                {
                    EditorUtility.DisplayDialog("147VR NeonVintage",
                        "ArtpassPost_v002.asset already exists but Unity cannot load it. It was preserved; no overwrite was attempted.", "OK");
                    return;
                }

                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "ArtpassPost_v002";
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            Tonemapping tonemapping = EnsureProfileComponent<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.ACES);
            EditorUtility.SetDirty(tonemapping);

            Bloom bloom = EnsureProfileComponent<Bloom>(profile);
            bloom.intensity.Override(0.15f);
            bloom.threshold.Override(1.1f);
            EditorUtility.SetDirty(bloom);

            AssetDatabase.SaveAssets();

            Transform postTransform = sceneRoot.transform.Find(PostProcessChildName);
            GameObject postObject;
            if (postTransform == null)
            {
                postObject = new GameObject(PostProcessChildName);
                Undo.RegisterCreatedObjectUndo(postObject, "Create NeonVintage PostProcess volume");
                Undo.SetTransformParent(postObject.transform, sceneRoot.transform, "Parent NeonVintage PostProcess");
            }
            else
            {
                postObject = postTransform.gameObject;
            }

            Volume volume = postObject.GetComponent<Volume>();
            if (volume == null)
                volume = Undo.AddComponent<Volume>(postObject);

            Undo.RecordObject(volume, "Assign NeonVintage PostProcess profile");
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;
            volume.blendDistance = 0f;
            volume.sharedProfile = profile;

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[NeonVintageSafe] Created/repaired separate ArtpassPost_v002 with ACES + restrained Bloom. Original ArtpassPost.asset was preserved. Scene NOT saved.");
            EditorUtility.DisplayDialog("147VR NeonVintage",
                "PostProcess v002 is assigned to the working Scene.\nOriginal ArtpassPost.asset was preserved.\n\nReview before saving.", "OK");
        }

        [MenuItem("147VR/AAA/NeonVintage/Safe/5) Audit Physics Boundary + ArtPass")]
        public static void AuditBoundary()
        {
            Scene scene;
            if (!RequireWorkingScene(out scene))
                return;

            GameObject table = FindInScene(scene, "147VR_Table_V008_OPT_VISUAL");
            GameObject questSetup = FindInScene(scene, "Quest Setup");
            GameObject artRoot = FindRoot(scene, SceneRootName);

            if (table == null)
            {
                EditorUtility.DisplayDialog("147VR NeonVintage", "V008 table root not found. Audit stopped.", "OK");
                return;
            }

            int tableColliders = table.GetComponentsInChildren<Collider>(true).Length;
            int tableBodies = table.GetComponentsInChildren<Rigidbody>(true).Length;
            int artColliders = artRoot != null ? artRoot.GetComponentsInChildren<Collider>(true).Length : 0;
            int artBodies = artRoot != null ? artRoot.GetComponentsInChildren<Rigidbody>(true).Length : 0;
            int artRenderers = artRoot != null ? artRoot.GetComponentsInChildren<Renderer>(true).Length : 0;
            int artMeshes = artRoot != null ? artRoot.GetComponentsInChildren<MeshFilter>(true).Length : 0;

            string report =
                "147VR NeonVintage Phase 2 Boundary Audit\n" +
                "Scene: " + scene.path + "\n" +
                "Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n" +
                "Table root active: " + table.activeInHierarchy + "\n" +
                "Table root position: " + table.transform.position + "\n" +
                "Table colliders (including inactive): " + tableColliders + "\n" +
                "Table rigidbodies (including inactive): " + tableBodies + "\n" +
                "Quest Setup found: " + (questSetup != null) + "\n" +
                "Quest Setup active: " + (questSetup != null && questSetup.activeInHierarchy) + "\n" +
                "ArtPass root found: " + (artRoot != null) + "\n" +
                "ArtPass renderers: " + artRenderers + "\n" +
                "ArtPass mesh filters: " + artMeshes + "\n" +
                "ArtPass colliders: " + artColliders + "\n" +
                "ArtPass rigidbodies: " + artBodies + "\n" +
                "Build Settings modified by this tool: NO\n" +
                "Scene saved by this tool: NO\n";

            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/NeonVintage_Phase2_Boundary_Audit.txt", report);
            Debug.Log(report);

            bool clean = artColliders == 0 && artBodies == 0;
            EditorUtility.DisplayDialog("147VR NeonVintage",
                "Boundary audit written to Logs/NeonVintage_Phase2_Boundary_Audit.txt\n" +
                "ArtPass physics components: " + (artColliders + artBodies) +
                "\nTable colliders: " + tableColliders + " | Table rigidbodies: " + tableBodies +
                "\nVisual/physics separation: " + (clean ? "PASS" : "STOP - inspect ArtPass"), "OK");
        }

        private static bool RequireWorkingScene(out Scene scene)
        {
            scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.path, WorkingScenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("147VR NeonVintage",
                    "Safety stop: this menu only operates on:\n" + WorkingScenePath +
                    "\n\nOpen the AAA Working Scene first. No other Scene was changed.", "OK");
                return false;
            }
            return true;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(g => string.Equals(g.name, name, StringComparison.Ordinal));
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, name, StringComparison.Ordinal))
                    return root;

                Transform match = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => string.Equals(t.name, name, StringComparison.Ordinal));
                if (match != null)
                    return match.gameObject;
            }
            return null;
        }

        private static GameObject InstantiateVisual(GameObject prefab, Scene scene, Transform parent, string instanceName)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not instantiate FBX prefab: " + prefab.name);

            Undo.RegisterCreatedObjectUndo(instance, "Stage NeonVintage " + instanceName);
            Undo.SetTransformParent(instance.transform, parent, "Parent NeonVintage " + instanceName);
            instance.name = instanceName;

            if (HasPhysics(instance))
            {
                Undo.DestroyObjectImmediate(instance);
                throw new InvalidOperationException(instanceName + " unexpectedly contains Collider/Rigidbody components.");
            }

            return instance;
        }

        private static bool HasPhysics(GameObject root)
        {
            return root.GetComponentsInChildren<Collider>(true).Length > 0 ||
                   root.GetComponentsInChildren<Rigidbody>(true).Length > 0;
        }

        private static void ApplyStaticFlags(GameObject root, StaticEditorFlags flags)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                Undo.RecordObject(child.gameObject, "Set NeonVintage static flags");
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
            }
        }

        private static Dictionary<string, Material> BuildMaterialMap()
        {
            var map = new Dictionary<string, Material>();
            var ambiguous = new HashSet<string>();
            if (!AssetDatabase.IsValidFolder(MaterialsRoot))
                return map;

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { MaterialsRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                    continue;

                string key = Normalize(material.name);
                if (string.IsNullOrEmpty(key) || ambiguous.Contains(key))
                    continue;

                if (map.ContainsKey(key))
                {
                    // Never silently bind a renderer to an arbitrary same-normalized-name material.
                    map.Remove(key);
                    ambiguous.Add(key);
                    Debug.LogWarning("[NeonVintageSafe] Ambiguous material key skipped: " + key);
                }
                else
                {
                    map.Add(key, material);
                }
            }
            return map;
        }

        private static void RemapMaterials(GameObject root, Dictionary<string, Material> map, ref int remapped, ref int missing)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] current = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < current.Length; i++)
                {
                    Material source = current[i];
                    if (source == null)
                    {
                        missing++;
                        continue;
                    }

                    Material replacement;
                    if (map.TryGetValue(Normalize(source.name), out replacement) && replacement != source)
                    {
                        current[i] = replacement;
                        remapped++;
                        changed = true;
                    }
                    else if (!map.ContainsKey(Normalize(source.name)))
                    {
                        missing++;
                    }
                }

                if (changed)
                {
                    Undo.RecordObject(renderer, "Remap NeonVintage materials");
                    renderer.sharedMaterials = current;
                    EditorUtility.SetDirty(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
            }
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            string raw = value.Replace(" (Instance)", string.Empty);
            return new string(raw.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }

        private static T EnsureProfileComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.components.OfType<T>().FirstOrDefault();
            if (component != null)
                return component;

            component = profile.Add<T>(true);
            if (AssetDatabase.GetAssetPath(component) != AssetDatabase.GetAssetPath(profile))
                AssetDatabase.AddObjectToAsset(component, profile);

            return component;
        }
    }
}
#endif
