#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    /// <summary>
    /// Two-step, owner-controlled fix for the proven flag facing defect.
    ///
    /// Evidence (Logs/WipRoom_Flag_Texture_Audit.txt 2026-11-08 11:49):
    ///   26 staged SM_147VR_AAA_Flag_* renderers, all active/enabled, textured URP Lit,
    ///   UV 0..1, no lightmap, and "front faces pointing into the wall: 26" with
    ///   avgWorldNormal ~= (0, 0, -1) while the room interior is at +Z. All materials use
    ///   _Cull=Back, so from inside the room the cloth is discarded entirely.
    ///
    /// FIRST APPLY ATTEMPT FAILED AND IS RECORDED HERE SO IT IS NOT REPEATED:
    ///   this room FBX bakes world position into the mesh vertices and leaves every child
    ///   transform at the origin (pivot 0,0,0). Rotating such a transform 180 deg about Y
    ///   swings the baked geometry to the mirrored side of the room - the row union went from
    ///   z[-14.31,-14.17] to z[-14.31,+14.37]. The Scene was never saved, so the session was
    ///   reverted by reopening it. This version rotates each flag AROUND ITS OWN WORLD BOUNDS
    ///   CENTRE (p' = c - F*(c - p), R' = F*R), which flips the normal while leaving the cloth
    ///   hanging exactly where it is. It also measures facing from the bounds centre instead of
    ///   the pivot, because a pivot at the origin makes the old dot meaningless.
    ///
    /// Menu 6 (dry-run) changes NOTHING and measures the real wall surface behind the cloth
    /// from vertex data, so the push-out distance is measured rather than guessed.
    /// Menu 7 (apply) writes those transforms through Undo, swaps the two mis-bound
    /// Thailand/Turkey materials, re-checks the physics invariants AND that the row did not
    /// move, frames the row from inside the room, and never saves the Scene.
    /// </summary>
    public static class WipRoomFlagFacingFix
    {
        private const string SourceScenePath = "Assets/Scenes/147VR_MainScene_WipRoom.unity";
        private const string DryRunReport = "Logs/WipRoom_Flag_Facing_DryRun.txt";
        private const string ApplyReport = "Logs/WipRoom_Flag_Facing_Apply.txt";

        /// <summary>Extra air gap in front of the measured wall face.</summary>
        private const float ClearanceM = 0.04f;
        /// <summary>OWNER INSTRUCTION 2026-11-08: "ลองแค่หันหน้าแต่ละธงกลับมา 180 องศา" -
        /// menu 7 rotates each flag in place and translates NOTHING. The wall measurement
        /// still runs so the report can show which cloths sit behind trim, but the value is
        /// only applied if this switch is turned on after the render has been reviewed.</summary>
        private const bool ApplyPushOut = false;
        /// <summary>Hard ceiling on the measured push-out. The row currently hangs ~0.44 m
        /// BEHIND the innermost wall trim, so a real clear-the-trim move is legitimate;
        /// 0.60 m still bounds any measurement failure to well under a metre.</summary>
        private const float MaxPushOutM = 0.60f;
        private const int MaxVerticesForWallScan = 600000;
        private static readonly Vector3 RoomCentre = Vector3.zero;

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/6) Flag Facing Dry-Run (Read-Only)")]
        public static void DryRun()
        {
            Scene scene = RequireWipRoom();
            if (!scene.IsValid()) return;

            List<Renderer> flags = GetFlags(scene);
            List<Renderer> others = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => r != null && !flags.Contains(r))
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("147VR Flag Facing DRY-RUN (READ-ONLY - nothing was modified)");
            sb.AppendLine("Scene: " + scene.path + " | Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Flag renderers: " + flags.Count);

            if (flags.Count == 0)
            {
                sb.AppendLine("NO FLAG RENDERERS - nothing to report.");
                Write(DryRunReport, sb);
                return;
            }

            Bounds row = UnionOf(flags);
            sb.AppendLine("ROW UNION: min=" + Fmt(row.min) + " max=" + Fmt(row.max) + " size=" + Fmt(row.size));
            sb.AppendLine("  (a healthy row is a thin band: one z value with ~0.14 m cloth depth)");

            float rowDepth = row.max.z - row.min.z;
            bool swung = rowDepth > 1.0f;
            if (swung)
            {
                sb.AppendLine();
                sb.AppendLine("  *** ROW IS NOT A THIN BAND (z depth " + rowDepth.ToString("F2") + " m).");
                sb.AppendLine("  *** The Scene is still in the swung state from the bad apply. Do NOT run menu 7:");
                sb.AppendLine("  *** push-out would be computed across the whole room. Recover with");
                sb.AppendLine("  *** File > Open Scene > 147VR_MainScene_WipRoom > Don't Save, then re-run menu 6.");
            }

            sb.AppendLine();
            sb.AppendLine("WALL SIDE CHECK (which bay is this row next to?)");
            float[] centreZ = flags.Select(f => f.bounds.center.z).ToArray();
            int minusWall = centreZ.Count(z => z < 0f);
            int plusWall = centreZ.Length - minusWall;
            float minusMean = centreZ.Where(z => z < 0f).DefaultIfEmpty(0f).Average();
            float plusMean = centreZ.Where(z => z >= 0f).DefaultIfEmpty(0f).Average();
            if (minusWall > 0 && plusWall > 0)
            {
                sb.AppendLine("  *** SPLIT ROW: " + minusWall + " flags on the -Z wall (mean z=" + minusMean.ToString("F2") +
                              ") and " + plusWall + " on the +Z wall (mean z=" + plusMean.ToString("F2") + ")");
            }
            float sideZ = minusWall >= plusWall ? minusMean : plusMean;
            sb.AppendLine("  dominant side z=" + sideZ.ToString("F2") +
                          " (" + (minusWall >= plusWall ? "-Z wall" : "+Z wall") + ")");
            GameObject nearestBay = null;
            float nearestDist = float.MaxValue;
            foreach (string bayName in new[] { "Bay1_PhysicsRoot", "Bay2_PhysicsRoot", "Bay3_PhysicsRoot", "Bay4_PhysicsRoot" })
            {
                GameObject bay = GameObject.Find(bayName);
                if (bay == null) { sb.AppendLine("  " + bayName + ": NOT FOUND in Scene"); continue; }
                float bayZ = bay.transform.position.z;
                float dist = Mathf.Abs(bayZ - sideZ);
                if (dist < nearestDist) { nearestDist = dist; nearestBay = bay; }
                sb.AppendLine("  " + bayName + " z=" + bayZ.ToString("F2") + " | distance from dominant side z(" +
                              sideZ.ToString("F2") + ") = " + dist.ToString("F2") + " m");
            }
            if (nearestBay != null)
                sb.AppendLine("  => the dominant row hangs on the wall nearest to " + nearestBay.name +
                              " (owner rule: the flag row belongs on the Bay4 wall)");
            sb.AppendLine();

            sb.AppendLine("WALL FACE MEASUREMENT (wall/shell vertices inside the row band)");
            List<WallPoint> band = CollectWallPointsInBand(others, row, sb);
            sb.AppendLine();
            sb.AppendLine("PLANNED: rotate 180 deg about world Y around each flag's OWN bounds centre.");
            sb.AppendLine("         Translation: " + (ApplyPushOut ? "push each flag clear of the trim behind IT"
                                                                   : "NONE (owner instruction: rotate only)") +
                          " | measured push would be up to " + MaxPushOutM.ToString("F2") + " m cap.");
            sb.AppendLine("         Pivot-only rotation is NOT used because this FBX bakes position into");
            sb.AppendLine("         vertices (pivots sit at the origin), which swings the cloth to the");
            sb.AppendLine("         opposite wall - exactly what the first apply attempt did.");
            sb.AppendLine();

            int wrongFacing = 0, buried = 0;
            float maxPush = 0f;
            sb.AppendLine("PER-FLAG CURRENT -> PLANNED");
            foreach (Renderer flag in flags.OrderBy(r => r.bounds.center.x))
            {
                Transform t = flag.transform;
                Vector3 c = flag.bounds.center;
                Vector3 normal = AverageWorldNormal(flag, GetMesh(flag));
                Vector3 toRoom = FlatDir(c, RoomCentre);
                float dotBefore = Vector3.Dot(normal, toRoom);
                if (dotBefore <= 0f) wrongFacing++;

                WallFace face = NearestWallFaceBehind(band, flag.bounds);
                float measuredPush = PushOutFor(face, flag.bounds);
                float pushOut = ApplyPushOut ? measuredPush : 0f;
                if (measuredPush > maxPush) maxPush = measuredPush;
                bool isBuried = face.Valid && flag.bounds.max.z < face.Z;
                if (isBuried) buried++;

                Quaternion flip = Quaternion.AngleAxis(180f, Vector3.up);
                Quaternion plannedRot = flip * t.rotation;
                Vector3 plannedPos = c - flip * (c - t.position) + toRoom * pushOut;
                Vector3 plannedNormal = flip * normal;
                float dotAfter = Vector3.Dot(plannedNormal, FlatDir(plannedPos, RoomCentre));

                sb.AppendLine("  " + flag.name);
                sb.AppendLine("     now pivot=" + Fmt(t.position) + " rot=" + Fmt(t.eulerAngles) +
                              " boundsCentre=" + Fmt(c) + " normal=" + Fmt(normal) + " dot=" + dotBefore.ToString("F3"));
                sb.AppendLine("     wall behind it: z=" + (face.Valid ? face.Z.ToString("F3") : "<none>") +
                              " (" + face.SourceName + ") | cloth front z=" + flag.bounds.max.z.ToString("F3") +
                              " | push applied=" + pushOut.ToString("F3") +
                              " (measured " + measuredPush.ToString("F3") + ")" +
                              (isBuried ? "  *** CLOTH CURRENTLY BEHIND THAT SURFACE" : ""));
                sb.AppendLine("     plan pivot=" + Fmt(plannedPos) + " rot=" + Fmt(plannedRot.eulerAngles) +
                              " normal=" + Fmt(plannedNormal.normalized) + " dot=" + dotAfter.ToString("F3") +
                              (dotAfter > 0.5f ? "  -> faces the room" : "  -> *** STILL NOT FACING THE ROOM"));
            }

            sb.AppendLine();
            sb.AppendLine("MATERIAL SWAP CHECK (production carries the Blender-side swap)");
            foreach (string pairName in new[] { "thailand", "turkey" })
            {
                Renderer r = flags.FirstOrDefault(f => f.name.EndsWith("_" + pairName, StringComparison.OrdinalIgnoreCase));
                if (r == null) { sb.AppendLine("  " + pairName + ": renderer not found"); continue; }
                Material m = r.sharedMaterials.Length > 0 ? r.sharedMaterials[0] : null;
                bool ok = m != null && m.name.ToLowerInvariant().Contains(pairName);
                sb.AppendLine("  " + r.name + " -> " + (m != null ? m.name : "<null>") + (ok ? "  (correct)" : "  (*** WRONG COUNTRY)"));
            }

            sb.AppendLine();
            sb.AppendLine("WRONG-FACING FLAGS NOW: " + wrongFacing + " / " + flags.Count);
            sb.AppendLine("FLAGS BURIED BEHIND THEIR WALL SURFACE: " + buried + " / " + flags.Count);
            sb.AppendLine("LARGEST MEASURED PUSH-OUT: " + maxPush.ToString("F3") + " m | translation " +
                          (ApplyPushOut ? "ENABLED" : "DISABLED (rotate only, per owner instruction)"));
            Write(DryRunReport, sb);

            Debug.Log("[FlagFacing] dry-run -> " + DryRunReport + " | wrongFacing=" + wrongFacing + "/" + flags.Count +
                      " | buried=" + buried + " | maxPushOut=" + maxPush.ToString("F3") + " m");
            EditorUtility.DisplayDialog("147VR Flag Facing",
                "Dry-run complete. Nothing was modified.\n\nReport: " + DryRunReport +
                "\nWrong-facing flags: " + wrongFacing + " / " + flags.Count +
                "\nBuried behind wall trim: " + buried + " / " + flags.Count +
                "\nLargest push-out planned: " + maxPush.ToString("F3") + " m", "OK");
        }

        [MenuItem("147VR/AAA/NeonVintage/WipRoom/7) Apply Flag Facing Fix (Undoable / No Save)")]
        public static void Apply()
        {
            Scene scene = RequireWipRoom();
            if (!scene.IsValid()) return;

            List<Renderer> flags = GetFlags(scene);
            if (flags.Count == 0)
            {
                EditorUtility.DisplayDialog("147VR Flag Facing", "No flag renderers in this Scene.", "OK");
                return;
            }

            List<Renderer> others = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => r != null && !flags.Contains(r))
                .ToList();

            Bounds rowBefore = UnionOf(flags);

            float beforeDepth = rowBefore.max.z - rowBefore.min.z;
            if (beforeDepth > 1.0f)
            {
                var refusal = new StringBuilder();
                refusal.AppendLine("147VR Flag Facing APPLY - REFUSED");
                refusal.AppendLine("Reason: the flag row spans " + beforeDepth.ToString("F2") +
                                   " m in z, so the Scene is still in the swung state from the earlier bad apply.");
                refusal.AppendLine("Action taken: NONE. Nothing was modified.");
                refusal.AppendLine("Recover with File > Open Scene > 147VR_MainScene_WipRoom > Don't Save, then re-run menu 6.");
                Write(ApplyReport, refusal);
                EditorUtility.DisplayDialog("147VR Flag Facing",
                    "REFUSED - nothing was modified.\n\nThe row spans " + beforeDepth.ToString("F2") +
                    " m in z, which means the Scene is still in the swung state.\n\n" +
                    "File > Open Scene > 147VR_MainScene_WipRoom > Don't Save, then run menu 6 again.", "OK");
                return;
            }

            int collidersBefore = Count<Collider>(scene);
            int bodiesBefore = Count<Rigidbody>(scene);
            int lightsBefore = Count<Light>(scene);

            var sb = new StringBuilder();
            sb.AppendLine("147VR Flag Facing APPLY");
            sb.AppendLine("Scene: " + scene.path + " | Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            sb.AppendLine("WALL FACE MEASUREMENT (wall/shell vertices inside the row band)");
            List<WallPoint> band = CollectWallPointsInBand(others, rowBefore, sb);
            sb.AppendLine("Row union BEFORE: min=" + Fmt(rowBefore.min) + " max=" + Fmt(rowBefore.max));
            sb.AppendLine();

            Quaternion flip = Quaternion.AngleAxis(180f, Vector3.up);
            int rotated = 0, skipped = 0, drifted = 0;
            float maxPush = 0f;

            foreach (Renderer flag in flags)
            {
                Transform t = flag.transform;
                Vector3 c = flag.bounds.center;
                Vector3 normal = AverageWorldNormal(flag, GetMesh(flag));
                Vector3 toRoom = FlatDir(c, RoomCentre);
                float dotBefore = Vector3.Dot(normal, toRoom);
                if (dotBefore > 0f)
                {
                    skipped++;
                    sb.AppendLine("  SKIP (already faces the room) " + flag.name + " dot=" + dotBefore.ToString("F3"));
                    continue;
                }

                WallFace face = NearestWallFaceBehind(band, flag.bounds);
                float measuredPush = PushOutFor(face, flag.bounds);
                float pushOut = ApplyPushOut ? measuredPush : 0f;
                if (measuredPush > maxPush) maxPush = measuredPush;

                Vector3 plannedPivot = c - flip * (c - t.position) + toRoom * pushOut;
                Undo.RecordObject(t, "147VR Flag Facing Fix");
                t.rotation = flip * t.rotation;
                t.position = plannedPivot;
                EditorUtility.SetDirty(t);
                rotated++;

                Vector3 centreAfter = flag.bounds.center;
                float drift = Vector3.Distance(centreAfter, c);
                if (drift > pushOut + 0.15f) drifted++;
                float dotAfter = Vector3.Dot(AverageWorldNormal(flag, GetMesh(flag)), FlatDir(centreAfter, RoomCentre));
                sb.AppendLine("  FIXED " + flag.name + " dot " + dotBefore.ToString("F3") + " -> " + dotAfter.ToString("F3") +
                              " | wall z=" + (face.Valid ? face.Z.ToString("F3") : "<none>") + " (" + face.SourceName + ")" +
                              " push=" + pushOut.ToString("F3") + "/measured " + measuredPush.ToString("F3") +
                              " | centre " + Fmt(c) + " -> " + Fmt(centreAfter) + " drift=" + drift.ToString("F3") + " m");
            }

            int swaps = FixThailandTurkeySwap(flags, sb);

            Bounds rowAfter = UnionOf(flags);
            int collidersAfter = Count<Collider>(scene);
            int bodiesAfter = Count<Rigidbody>(scene);
            int lightsAfter = Count<Light>(scene);

            sb.AppendLine();
            sb.AppendLine("RESULT");
            sb.AppendLine("  rotated=" + rotated + " skipped=" + skipped + " centre drift >0.25 m=" + drifted);
            sb.AppendLine("  Row union AFTER : min=" + Fmt(rowAfter.min) + " max=" + Fmt(rowAfter.max));
            sb.AppendLine("  The row must stay a thin band near z=" + rowBefore.center.z.ToString("F2") +
                          "; a second z value means the cloth swung across the room -> Ctrl+Z.");
            sb.AppendLine("  Colliders: " + collidersBefore + " -> " + collidersAfter);
            sb.AppendLine("  Rigidbodies: " + bodiesBefore + " -> " + bodiesAfter);
            sb.AppendLine("  Lights: " + lightsBefore + " -> " + lightsAfter);
            Write(ApplyReport, sb);

            float rowDepthBefore = rowBefore.max.z - rowBefore.min.z;
            float rowDepthAfter = rowAfter.max.z - rowAfter.min.z;
            bool rowBlewUp = rowDepthAfter > rowDepthBefore + 1.0f || drifted > 0;
            bool physicsChanged = collidersBefore != collidersAfter || bodiesBefore != bodiesAfter || lightsBefore != lightsAfter;

            EditorSceneManager.MarkSceneDirty(scene);
            FrameRowFromInsideRoom(rowBefore);

            if (physicsChanged || rowBlewUp)
            {
                Debug.LogError("[FlagFacing] SELF-CHECK FAILED (physicsChanged=" + physicsChanged +
                               ", rowDrift=" + rowBlewUp + ") - press Ctrl+Z. See " + ApplyReport);
                EditorUtility.DisplayDialog("147VR Flag Facing",
                    "SELF-CHECK FAILED. Press Ctrl+Z to revert everything, then report this.\n" + ApplyReport, "OK");
                return;
            }

            Debug.Log("[FlagFacing] applied rotated=" + rotated + " skipped=" + skipped + " swaps=" + swaps +
                      " | row stayed at z=" + rowAfter.center.z.ToString("F2") + " | Scene DIRTY, NOT saved");
            EditorUtility.DisplayDialog("147VR Flag Facing",
                "Rotated " + rotated + " flags (skipped " + skipped + "), country material swaps=" + swaps +
                ".\nRow stayed at z=" + rowAfter.center.z.ToString("F2") + " m; physics counts unchanged." +
                "\nScene view now looks at the row from inside the room.\n\n" +
                "NOTHING WAS SAVED: inspect the render, then Ctrl+S to keep or Ctrl+Z to undo.", "OK");
        }

        // ---- helpers ----

        private static Scene RequireWipRoom()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, SourceScenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("147VR Flag Facing",
                    "Safety stop. Open the actual gameplay Scene first:\n" + SourceScenePath, "OK");
                return default;
            }
            return scene;
        }

        private static List<Renderer> GetFlags(Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                .Where(r => r != null && r.name.StartsWith("SM_147VR_AAA_Flag_", StringComparison.OrdinalIgnoreCase))
                .ToList();

        /// <summary>Horizontal direction from a point toward the room authoring origin.</summary>
        private static Vector3 FlatDir(Vector3 from, Vector3 toward)
        {
            Vector3 d = toward - from;
            d.y = 0f;
            return d.sqrMagnitude < 1e-8f ? Vector3.forward : d.normalized;
        }

        private static Mesh GetMesh(Renderer renderer) =>
            renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                                                    : renderer.GetComponent<MeshFilter>()?.sharedMesh;

        private static Vector3 AverageWorldNormal(Renderer renderer, Mesh mesh)
        {
            if (mesh == null) return Vector3.zero;
            int[] triangles = mesh.triangles;
            Vector3[] normals = mesh.normals;
            Vector3[] vertices = mesh.vertices;
            if (triangles.Length < 3 || normals.Length == 0) return Vector3.zero;

            Matrix4x4 l2w = renderer.transform.localToWorldMatrix;
            Vector3 sum = Vector3.zero;
            int samples = 0;
            for (int i = 0; i + 2 < triangles.Length && samples < 240; i += 3)
            {
                int i0 = triangles[i], i1 = triangles[i + 1], i2 = triangles[i + 2];
                if (i0 >= normals.Length || i0 >= vertices.Length || i1 >= vertices.Length || i2 >= vertices.Length) continue;
                Vector3 face = Vector3.Cross(vertices[i1] - vertices[i0], vertices[i2] - vertices[i0]);
                if (face.sqrMagnitude < 1e-12f) continue;
                Vector3 nObj = normals[i0];
                if (nObj.sqrMagnitude < 1e-12f) continue;
                if (Vector3.Dot(nObj, face) < 0f) nObj = -nObj;
                Vector3 world = l2w.MultiplyVector(nObj);
                if (world.sqrMagnitude < 1e-12f) continue;
                sum += world.normalized;
                samples++;
            }
            return samples == 0 ? Vector3.zero : sum.normalized;
        }

        private static Bounds UnionOf(List<Renderer> renderers)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        /// <summary>One sampled wall vertex in world space, tagged with its source renderer.</summary>
        private sealed class WallPoint
        {
            public float X;
            public float Y;
            public float Z;
            public string Source;
        }

        private struct WallFace
        {
            public float Z;
            public string SourceName;
            public bool Valid => !float.IsNaN(Z);
        }

        /// <summary>Collect wall/shell vertices that sit in the row band ONCE, so the per-flag
        /// query below scans a few thousand points instead of re-reading every wall mesh 26 times.</summary>
        private static List<WallPoint> CollectWallPointsInBand(List<Renderer> others, Bounds row, StringBuilder sb)
        {
            var points = new List<WallPoint>();
            int scanned = 0, skipped = 0;

            foreach (Renderer r in others)
            {
                bool wallish = r.name.IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               r.name.Equals("SM_0020_Cube.004", StringComparison.OrdinalIgnoreCase);
                if (!wallish) continue;

                Bounds rb = r.bounds;
                if (rb.max.z < row.min.z - 2f || rb.min.z > row.max.z + 2f) continue;
                if (rb.min.x > row.max.x || rb.max.x < row.min.x) continue;

                Mesh mesh = GetMesh(r);
                if (mesh == null) continue;
                if (mesh.vertexCount > MaxVerticesForWallScan)
                {
                    skipped++;
                    sb.AppendLine("  skipped " + r.name + " (" + mesh.vertexCount + " verts, too large to scan)");
                    continue;
                }

                scanned++;
                Matrix4x4 l2w = r.transform.localToWorldMatrix;
                Vector3[] verts = mesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 w = l2w.MultiplyPoint(verts[i]);
                    if (w.y < row.min.y - 0.3f || w.y > row.max.y + 0.3f) continue;
                    if (w.x < row.min.x - 0.5f || w.x > row.max.x + 0.5f) continue;
                    if (w.z < row.min.z - 1.5f || w.z > row.max.z + 1.0f) continue;
                    points.Add(new WallPoint { X = w.x, Y = w.y, Z = w.z, Source = r.name });
                }
            }

            sb.AppendLine("  wall vertices sampled=" + points.Count + " from meshes scanned=" + scanned +
                          " skipped=" + skipped + " | global innermost z=" +
                          (points.Count == 0 ? "<none>" : points.Max(p => p.Z).ToString("F3")) +
                          " | cloth z range=[" + row.min.z.ToString("F3") + ".." + row.max.z.ToString("F3") + "]");
            return points;
        }

        /// <summary>Innermost wall surface directly behind one flag, inside its own x/y window.</summary>
        private static WallFace NearestWallFaceBehind(List<WallPoint> band, Bounds flagBounds)
        {
            float best = float.MinValue;
            string source = null;
            foreach (WallPoint p in band)
            {
                if (p.X < flagBounds.min.x - 0.15f || p.X > flagBounds.max.x + 0.15f) continue;
                if (p.Y < flagBounds.min.y - 0.10f || p.Y > flagBounds.max.y + 0.10f) continue;
                if (p.Z > best) { best = p.Z; source = p.Source; }
            }
            return new WallFace { Z = source == null ? float.NaN : best, SourceName = source ?? "<none>" };
        }

        /// <summary>Push-out that hangs the cloth ClearanceM in front of the measured surface,
        /// bounded so a bad measurement can never fling it.</summary>
        private static float PushOutFor(WallFace face, Bounds flagBounds)
        {
            if (!face.Valid) return ClearanceM;
            return Mathf.Clamp(face.Z - flagBounds.max.z + ClearanceM, ClearanceM, MaxPushOutM);
        }

        private static int FixThailandTurkeySwap(List<Renderer> flags, StringBuilder sb)
        {
            Renderer thai = flags.FirstOrDefault(f => f.name.EndsWith("_thailand", StringComparison.OrdinalIgnoreCase));
            Renderer turkey = flags.FirstOrDefault(f => f.name.EndsWith("_turkey", StringComparison.OrdinalIgnoreCase));
            if (thai == null || turkey == null) { sb.AppendLine("SWAP: one of the two renderers is missing"); return 0; }

            Material[] thaiSlots = thai.sharedMaterials;
            Material[] turkeySlots = turkey.sharedMaterials;
            if (thaiSlots.Length == 0 || turkeySlots.Length == 0) { sb.AppendLine("SWAP: empty material arrays"); return 0; }

            bool swapped = thaiSlots[0] != null && turkeySlots[0] != null &&
                thaiSlots[0].name.ToLowerInvariant().Contains("turkey") &&
                turkeySlots[0].name.ToLowerInvariant().Contains("thailand");
            if (!swapped)
            {
                sb.AppendLine("SWAP: not needed (thailand=" + thaiSlots[0]?.name + ", turkey=" + turkeySlots[0]?.name + ")");
                return 0;
            }

            Undo.RecordObjects(new UnityEngine.Object[] { thai, turkey }, "147VR Flag Country Material Swap");
            Material tmp = thaiSlots[0];
            thaiSlots[0] = turkeySlots[0];
            turkeySlots[0] = tmp;
            thai.sharedMaterials = thaiSlots;
            turkey.sharedMaterials = turkeySlots;
            EditorUtility.SetDirty(thai);
            EditorUtility.SetDirty(turkey);
            sb.AppendLine("SWAP DONE: thailand -> " + thaiSlots[0].name + " | turkey -> " + turkeySlots[0].name);
            return 1;
        }

        private static void FrameRowFromInsideRoom(Bounds row)
        {
            SceneView view = SceneView.lastActiveSceneView;
            if (view == null) return;
            view.orthographic = false;
            view.size = Mathf.Clamp(row.size.x * 0.40f, 4f, 16f);
            view.pivot = new Vector3(0f, row.center.y, row.center.z + 5.0f);
            view.rotation = Quaternion.LookRotation(new Vector3(0f, -0.10f, -1f), Vector3.up);
            view.Repaint();
        }

        private static int Count<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<T>(true).Length);

        private static string Fmt(Vector3 v) =>
            "(" + v.x.ToString("F3") + ", " + v.y.ToString("F3") + ", " + v.z.ToString("F3") + ")";

        private static void Write(string path, StringBuilder sb)
        {
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText(path, sb.ToString());
        }
    }
}
#endif
