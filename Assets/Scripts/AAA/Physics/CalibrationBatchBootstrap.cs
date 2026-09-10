using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VR147.AAA.Physics
{
    public sealed class CalibrationBatchBootstrap : MonoBehaviour
    {
        [SerializeField] private CalibrationBatchRunner runner;
        [SerializeField] private bool beginOnStart = true;
        private bool batchStarted;

        private void Start()
        {
            Debug.Log($"[147VR Calibration] Bootstrap.Start runner={(runner != null ? "BOUND" : "NULL")} begin={beginOnStart}", this);
            var physicsSetup = FindFirstObjectByType<SnookerPhysicsSetup>();
            if (physicsSetup != null)
            {
                physicsSetup.EnsurePhysics();
                Debug.Log($"[147VR Calibration] Physics bootstrap surfaceY={physicsSetup.SurfaceTopY:F4} bounds={physicsSetup.TableBounds.size}", physicsSetup);
                var cue = GameObject.Find("Sphere.009");
                var cueBody = cue != null ? cue.GetComponent<Rigidbody>() : null;
                var cueCollider = cueBody != null ? cueBody.GetComponent<SphereCollider>() : null;
                var surface = GameObject.Find("Surface")?.GetComponent<BoxCollider>();
                if (cueBody != null)
                {
                    // The calibration impulse is intentionally high enough that a discrete
                    // 50 Hz step can tunnel through the thin table surface. Use CCD on the
                    // real cue-ball Rigidbody; this is runtime collision policy, not a fake
                    // correction or teleport.
                    cueBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    cueBody.interpolation = RigidbodyInterpolation.Interpolate;
                }
                if (surface != null)
                {
                    surface.enabled = true;
                    surface.isTrigger = false;
                    UnityEngine.Physics.IgnoreLayerCollision(cueBody != null ? cueBody.gameObject.layer : 0, surface.gameObject.layer, false);
                }
                UnityEngine.Physics.SyncTransforms();
                bool layerIgnored = cueBody != null && surface != null &&
                    UnityEngine.Physics.GetIgnoreLayerCollision(cueBody.gameObject.layer, surface.gameObject.layer);
                RaycastHit hit = default;
                bool rayHit = cueBody != null && surface != null &&
                    UnityEngine.Physics.Raycast(cueBody.position + Vector3.up * 0.05f, Vector3.down, out hit, 0.25f) &&
                    hit.collider == surface;
                RaycastHit directHit = default;
                bool directRayHit = surface != null && surface.Raycast(
                    new Ray(cueBody != null ? cueBody.position + Vector3.up * 0.05f : Vector3.up, Vector3.down),
                    out directHit, 0.25f);
                Collider[] overlaps = cueBody != null
                    ? UnityEngine.Physics.OverlapSphere(cueBody.position, 0.04f, ~0, QueryTriggerInteraction.Ignore)
                    : System.Array.Empty<Collider>();
                string overlapNames = string.Join(",", System.Array.ConvertAll(overlaps, c => c != null ? c.name : "NULL"));
                string rayName = hit.collider != null ? hit.collider.name : "NONE";
                Debug.Log($"[147VR Calibration] CollisionProbe cue={(cueBody != null ? cueBody.position.ToString() : "NULL")} detect={(cueBody != null && cueBody.detectCollisions)} mode={(cueBody != null ? cueBody.collisionDetectionMode.ToString() : "NULL")} sphereEnabled={(cueCollider != null && cueCollider.enabled)} sphereBounds={(cueCollider != null ? cueCollider.bounds.ToString() : "NULL")} surfaceEnabled={(surface != null && surface.enabled)} active={(surface != null && surface.gameObject.activeInHierarchy)} trigger={(surface != null && surface.isTrigger)} surfaceBounds={(surface != null ? surface.bounds.ToString() : "NULL")} layerIgnored={layerIgnored} rayHit={rayHit} rayName={rayName} directRayHit={directRayHit} overlapCount={overlaps.Length} overlaps={overlapNames}", physicsSetup);
            }
            else
            {
                Debug.LogError("[147VR Calibration] SnookerPhysicsSetup missing.");
            }
        }

        private void Update()
        {
            if (beginOnStart && !batchStarted && runner != null)
            {
                runner.BeginBatch();
                batchStarted = true;
                return;
            }

            if (runner == null || runner.Running || runner.CompletedRuns == 0) return;
#if UNITY_EDITOR
            EditorApplication.Exit(0);
#endif
        }
    }
}
