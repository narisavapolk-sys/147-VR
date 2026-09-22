// 147VR - 008 table-prop staging (COACH-provided Editor tool).
//
// WHY THIS EXISTS
//   Unity 6000.4.4f1 batch invocation did not pick up the previous staging helper. This file
//   lives in Assets/Editor/, which is where all 129 of the project's other Editor-only scripts
//   live (Assets/Editor/*.cs and the AssetDatabase-visible Assets/Editor/AAA/*.cs). The project
//   has exactly one asmdef (Assets/Tests/PlayMode/147VR.PlayModeTests.asmdef), so anything under
//   an "Editor" folder compiles into Assembly-CSharp-Editor. A helper placed anywhere else will
//   not, and -executeMethod will not find it.
//
//   It also works as a plain MENU ITEM, so batch mode is optional:
//     147VR / 008 / 1. Configure Prop FBX Import Settings
//     147VR / 008 / 2. Stage Table Props Into Current Scene
//     147VR / 008 / 3. Verify Prop Import (shader family + materials)
//
// RENDER PIPELINE: THIS PROJECT IS URP, SO MATERIAL IMPORT MODE MATTERS
//   Verified from the repo: URP 17.4.0 in the manifest; URP registered in GraphicsSettings
//   (m_RenderPipelineGlobalSettingsMap -> UnityEngine.Rendering.Universal.UniversalRenderPipeline);
//   Mobile_RPAsset (guid 5e6cbd92db86f4b18aec3ed561671858) assigned per quality level in
//   QualitySettings.asset; the project's own shader Assets/Shaders/147VR_TableSurfaceMarking.shader
//   includes Packages/com.unity.render-pipelines.universal/ShaderLibrary; and existing imported
//   materials here use URP/Lit (shader guid 933532a4fcc9baf4fa0491de14d08ed7, with the
//   URP-specific properties _BaseMap, _ClearCoatMask, _BlendModePreserveSpecular,
//   _AddPrecomputedVelocity).
//
//   Consequence: the model importer MUST use ImportViaMaterialDescription, which maps the FBX
//   material description onto the ACTIVE pipeline's Lit shader. ModelImporterMaterialImportMode
//   .ImportStandard forces the BUILT-IN Standard shader, which renders MAGENTA under URP. An
//   earlier revision of this file set ImportStandard; that is fixed here.
//
//   Material location is External, matching this project's existing convention of separate
//   "<Model>__<Material>.mat" assets next to the model (see Assets/Prefabs/PoolTable/*.mat), so
//   the prop materials can be art-directed rather than hidden inside the prefab.
//
// BATCH (optional):
//     Unity.exe -quit -batchmode -projectPath "<proj>" -logFile "<log>" \
//       -executeMethod VR147.EditorTools.Stage008Props.StageMainSceneBatch
//
// SAFETY RULES ENFORCED IN CODE (see Verify())
//   * Props are parented under a NEW root that is a SIBLING of the table instance.
//     They are never parented under the table transform: SnookerPhysicsSetup does
//     "foreach (Collider c in root.GetComponentsInChildren<Collider>(true)) ... c.enabled = false;"
//     for everything under tableRoot except 2-4 cm sphere colliders with a Rigidbody, so a prop
//     parented there would come out silently non-collidable.
//   * Nothing is named "Bed_Collider" or "TABLE SURFACE" (both are physics-lookup names).
//   * Props get no Rigidbody, so they add no simulation cost on Quest.
//   * Idempotent: re-running replaces the prop root's children instead of duplicating.
//   * Each instantiated prop's world-space renderer bounds are compared against the manifest
//     sizes produced by the Blender generator. This catches both an FBX import-scale problem and
//     an axis-convention mistake, in one check.
//
// TABLE AXES (verified, COACH D1 closure):
//     X = WIDTH (1.778 m)      Z = LENGTH (3.569 m)      Y = UP
//   The visual chain inside Prefab_WPBSA_12Foot_Snooker rotates the Blender mesh
//   (Visual_Meshes_Drop_Here identity -> VISUAL_MAIN Ry(+90) -> VISUAL_ROOT Rx(-90), scale 100),
//   which maps Blender X -> world Z, Blender Y -> world X, Blender Z -> world Y.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VR147.EditorTools
{
    public static class Stage008Props
    {
        const string PropFolder = "Assets/AAA/Props/008";
        const string RootName = "147VR_PROPS_ROOT";
        const string MainScenePath = "Assets/Scenes/147VR_MainScene.unity";

        // Assumption, stated out loud: the scene floor is at Y = 0. Logged on every run so a
        // wrong value is visible immediately. Adjust here if the real floor differs.
        const float FloorY = 0f;

        // Bounds AREA of each prop's Renderers, in UNITY world axes, from the committed manifest
        // (Artifacts/M5_3/008_Measurement/m53_008_props_manifest.json, 12/12 assertions).
        // Manifest sizes are Blender-frame (X,Y,Z) with Z up, so they are permuted here:
        //   blender (X, Y, Z)  ->  unity (x = Y, y = Z, z = X)
        static readonly (string name, Vector3 size)[] Expected =
        {
            ("CHALK", new Vector3(0.035f, 0.022f, 0.035f)),
            ("REST", new Vector3(0.135f, 0.0253f, 1.58f)),
            ("CUERACK", new Vector3(0.3f, 0.93f, 0.42f)),
            ("TRIANGLE", new Vector3(0.270258f, 0.038f, 0.312067f)),
            ("SCOREBOARD", new Vector3(0.24f, 1.35f, 0.62f)),
        };

        // ------------------------------------------------------------------ import settings

        [MenuItem("147VR/008/1. Configure Prop FBX Import Settings")]
        public static void ConfigureImports()
        {
            int changed = 0;
            foreach (var (name, _) in Expected)
            {
                string path = PropFolder + "/147VR_PROP_" + name + ".fbx";
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null)
                {
                    Debug.LogError("[Stage008Props] no ModelImporter at " + path + " - is the FBX imported?");
                    continue;
                }

                imp.globalScale = 1f;                 // metres in, metres out
                imp.useFileScale = false;
                imp.bakeAxisConversion = false;       // the FBX already carries Unity axes
                imp.importCameras = false;
                imp.importLights = false;
                imp.importBlendShapes = false;
                imp.importAnimation = false;
                // NOT ImportStandard - that forces the built-in Standard shader and renders
                // magenta under URP. This maps onto the active pipeline's Lit shader instead.
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                imp.materialLocation = ModelImporterMaterialLocation.External;
                imp.materialSearch = ModelImporterMaterialSearch.Local;
                imp.SaveAndReimport();
                changed++;
            }
            AssetDatabase.Refresh();
            Debug.Log("[Stage008Props] import settings applied to " + changed + "/" + Expected.Length + " prop FBX");
            VerifyImports();
        }

        /// <summary>
        /// Checks the thing that fails silently: a material imported with the wrong shader family
        /// looks fine in the log and magenta in the headset. Run this after importing.
        /// </summary>
        [MenuItem("147VR/008/3. Verify Prop Import (shader family + materials)")]
        public static void VerifyImports()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { PropFolder });
            if (guids.Length == 0)
            {
                Debug.LogWarning("[Stage008Props] no materials under " + PropFolder +
                                 " yet - run menu 1 (Configure Imports) first.");
                return;
            }

            int bad = 0;
            foreach (string g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                string sh = (m != null && m.shader != null) ? m.shader.name : "<null>";
                bool urpOk = sh.StartsWith("Universal Render Pipeline/");
                if (!urpOk) bad++;
                Debug.Log(string.Format("[Stage008Props] {0}  shader='{1}'  {2}",
                    Path.GetFileName(p), sh, urpOk ? "URP OK" : "NOT URP -> WILL RENDER WRONG"));
            }
            Debug.Log("[Stage008Props] " + guids.Length + " prop material(s), " + bad +
                      " not URP" + (bad > 0 ? "  <-- fix before staging" : ""));
        }

        // ------------------------------------------------------------------ staging

        [MenuItem("147VR/008/2. Stage Table Props Into Current Scene")]
        public static void StageCurrentScene()
        {
            Stage(false);
        }

        /// Batch entry point (no namespace issues: fully qualified in -executeMethod).
        public static void StageMainSceneBatch()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[Stage008Props] could not open " + MainScenePath);
                EditorApplication.Exit(2);
                return;
            }
            ConfigureImports();
            Stage(true);
        }

        static Bounds GetWorldBounds(BoxCollider box)
        {
            Vector3 h = box.size * 0.5f;
            Vector3 c = box.center;
            Vector3 p0 = box.transform.TransformPoint(c + new Vector3(-h.x, -h.y, -h.z));
            Bounds b = new Bounds(p0, Vector3.zero);
            for (int ix = -1; ix <= 1; ix += 2)
            for (int iy = -1; iy <= 1; iy += 2)
            for (int iz = -1; iz <= 1; iz += 2)
                b.Encapsulate(box.transform.TransformPoint(c + new Vector3(ix * h.x, iy * h.y, iz * h.z)));
            return b;
        }
        static void Stage(bool saveScene)
        {
            // ---- locate the table without guessing
            var setups = Object.FindObjectsByType<SnookerPhysicsSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (setups.Length == 0)
            {
                Debug.LogError("[Stage008Props] no SnookerPhysicsSetup in the scene - aborting, scene untouched.");
                if (saveScene) EditorApplication.Exit(3);
                return;
            }

            var setup = setups[0];
            Transform tableRoot = setup.tableRoot != null ? setup.tableRoot : setup.transform;

            // Bed_Collider is the authoritative surface collider (SnookerPhysicsSetup.FindSurfaceCollider
            // prefers it). Read it in WORLD space, and do it without triggering runtime setup.
            BoxCollider bed = null;
            foreach (var c in tableRoot.GetComponentsInChildren<BoxCollider>(true))
                if (c.name == "Bed_Collider") { bed = c; break; }

            if (bed == null)
            {
                Debug.LogError("[Stage008Props] no Bed_Collider under " + tableRoot.name +
                               " - aborting, scene untouched.");
                if (saveScene) EditorApplication.Exit(4);
                return;
            }

            Bounds tb = GetWorldBounds(bed);                 // world-space AABB from collider geometry (works even when the source collider is disabled)
            float surfaceTopY = tb.max.y;
            float halfX = tb.size.x * 0.5f;               // half WIDTH  (1.778 axis)
            float halfZ = tb.size.z * 0.5f;               // half LENGTH (3.569 axis)
            float cx = tb.center.x, cz = tb.center.z;
            float railTopY = surfaceTopY + setup.railHeight - 0.02f;   // matches BuildRails()

            Debug.Log(string.Format(
                "[Stage008Props] table root='{0}' bed=({1}) center=({2:F4},{3:F4}) " +
                "size=(x {4:F4}, z {5:F4}) surfaceTopY={6:F4} railTopY={7:F4}",
                tableRoot.name, bed.name, cx, cz, tb.size.x, tb.size.z, surfaceTopY, railTopY));

            // ---- (re)create the props root: SIBLING of the table, never a child
            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Create 147VR_PROPS_ROOT");
            }
            root.transform.SetParent(null, false);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(root.transform.GetChild(i).gameObject);

            if (root.transform.IsChildOf(tableRoot) || tableRoot.IsChildOf(root.transform))
            {
                Debug.LogError("[Stage008Props] REFUSING: props root and table are in the same " +
                               "hierarchy. Props must be a sibling. Scene untouched.");
                if (saveScene) EditorApplication.Exit(5);
                return;
            }

            // ---- placement table. FloorY for floor items, railTopY for the chalk.
            var placements = new Dictionary<string, (Vector3 pos, float yaw)>
            {
                // on the +X long rail, a third of the way along the length toward one end
                { "CHALK", (new Vector3(cx + halfX, railTopY, cz - 0.35f * halfZ), 0f) },
                // lying on the floor alongside the -X long side, long axis already along Z
                { "REST", (new Vector3(cx - (halfX + 0.30f), FloorY, cz), 0f) },
                // on the floor past the +Z end, turned to face the table
                { "CUERACK", (new Vector3(cx, FloorY, cz + halfZ + 0.45f), 180f) },
                // on the floor beside the +X long side
                { "TRIANGLE", (new Vector3(cx + (halfX + 0.30f), FloorY, cz + 0.60f), 0f) },
                // on the floor beyond the -Z head end, facing the table
                { "SCOREBOARD", (new Vector3(cx, FloorY, cz - (halfZ + 0.60f)), 0f) },
            };

            var report = new List<string>();
            int failures = 0;

            foreach (var (name, expectedSize) in Expected)
            {
                string path = PropFolder + "/147VR_PROP_" + name + ".fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                {
                    Debug.LogError("[Stage008Props] missing model: " + path +
                                   " - run menu 1 first, then re-run staging.");
                    failures++;
                    continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                go.name = "PROP_" + name;
                var (pos, yaw) = placements[name];
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));

                // no simulation cost: props are static scenery
                foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
                    Object.DestroyImmediate(rb);

                // no physics-lookup name collisions
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Bed_Collider" || t.name == "TABLE SURFACE")
                    {
                        Debug.LogError("[Stage008Props] " + t.name + " name collision inside " + go.name);
                        failures++;
                    }
                }

                // ---- measure: catches import scale AND axis convention in one check
                var rends = go.GetComponentsInChildren<Renderer>(true);
                if (rends.Length == 0)
                {
                    Debug.LogError("[Stage008Props] " + go.name + " has no Renderer - import may have failed");
                    failures++;
                    continue;
                }
                Bounds b = rends[0].bounds;
                for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                Vector3 got = b.size;
                float tol = Mathf.Max(0.002f, expectedSize.magnitude * 0.02f);
                bool ok = (got - expectedSize).magnitude <= tol;
                if (!ok) failures++;

                report.Add(string.Format("  {0,-12} pos=({1:F3},{2:F3},{3:F3}) yaw={4,5:F1} " +
                                         "worldSize=({5:F4},{6:F4},{7:F4}) expected=({8:F4},{9:F4},{10:F4}) {11}",
                    go.name, pos.x, pos.y, pos.z, yaw, got.x, got.y, got.z,
                    expectedSize.x, expectedSize.y, expectedSize.z, ok ? "OK" : "SIZE MISMATCH"));
            }

            Debug.Log("[Stage008Props] ---- staged under " + RootName + " (sibling of '" +
                      tableRoot.name + "') ----\n" + string.Join("\n", report.ToArray()));
            Debug.Log("[Stage008Props] verified " + (Expected.Length - failures) + "/" + Expected.Length +
                      " props. Failures=" + failures);

            if (saveScene)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                bool saved = EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("[Stage008Props] SaveScene -> " + saved);
                EditorApplication.Exit(failures == 0 ? 0 : 6);
            }
        }
    }
}
