using UnityEngine;

namespace VR147.AAA.Physics
{
    public static class CalibrationEnvironmentIsolator
    {
        public static bool Isolate(Rigidbody cueBall, SnookerPhysicsSetup setup)
        {
            if (cueBall == null || setup == null) return false;

            int disabled = 0;
            foreach (var rb in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (rb == null || rb == cueBall) continue;
                var sphere = rb.GetComponent<SphereCollider>();
                if (sphere == null) continue;
                sphere.enabled = false;
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
                rb.detectCollisions = false;
                disabled++;
            }

            const float snookerBallRadius = 0.02625f;
            var cueCollider = cueBall.GetComponent<SphereCollider>();
            if (cueCollider == null) return false;
            cueCollider.enabled = true;
            cueCollider.isTrigger = false;
            cueCollider.radius = snookerBallRadius;
            cueBall.mass = 0.14f;
            float y = setup.SurfaceTopY + snookerBallRadius + 0.001f;
            Vector3 p = cueBall.position;
            p.y = y;
            cueBall.position = p;
            cueBall.linearVelocity = Vector3.zero;
            cueBall.angularVelocity = Vector3.zero;
            cueBall.isKinematic = false;
            cueBall.detectCollisions = true;
            cueBall.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var surface = GameObject.Find("Surface")?.GetComponent<BoxCollider>();
            if (surface != null)
            {
                surface.enabled = true;
                surface.isTrigger = false;
                surface.gameObject.SetActive(true);
                UnityEngine.Physics.IgnoreLayerCollision(cueBall.gameObject.layer, surface.gameObject.layer, false);
            }
            UnityEngine.Physics.SyncTransforms();

            bool rayHit = false;
            bool directRayHit = false;
            RaycastHit rayInfo = default;
            RaycastHit directInfo = default;
            if (surface != null)
            {
                rayHit = UnityEngine.Physics.Raycast(cueBall.position + Vector3.up * 0.05f, Vector3.down, out rayInfo, 0.25f, ~0, QueryTriggerInteraction.Ignore) && rayInfo.collider == surface;
                directRayHit = surface.Raycast(new Ray(cueBall.position + Vector3.up * 0.05f, Vector3.down), out directInfo, 0.25f);
            }
            bool ignoredPair = surface != null && UnityEngine.Physics.GetIgnoreCollision(cueCollider, surface);
            bool ignoredLayers = surface != null && UnityEngine.Physics.GetIgnoreLayerCollision(cueBall.gameObject.layer, surface.gameObject.layer);
            bool penetration = false;
            if (surface != null)
            {
                Vector3 original = cueBall.position;
                cueBall.position = new Vector3(original.x, setup.SurfaceTopY + 0.01f, original.z);
                UnityEngine.Physics.SyncTransforms();
                penetration = UnityEngine.Physics.ComputePenetration(cueCollider, cueBall.position, cueBall.rotation, surface, surface.transform.position, surface.transform.rotation, out _, out _);
                cueBall.position = original;
                UnityEngine.Physics.SyncTransforms();
            }
            Debug.Log($"[147VR Calibration] Isolated environment: disabledOtherBalls={disabled} cue={cueBall.name} cuePos={cueBall.position} cueCollider={cueCollider.enabled} cueLayer={cueBall.gameObject.layer} surface={(surface != null ? surface.name : "NULL")} surfaceEnabled={(surface != null && surface.enabled)} surfaceLayer={(surface != null ? surface.gameObject.layer : -1)} surfaceBounds={(surface != null ? surface.bounds.ToString() : "NULL")} rayHit={rayHit} rayName={(rayInfo.collider != null ? rayInfo.collider.name : "NONE")} directRayHit={directRayHit} directName={(directInfo.collider != null ? directInfo.collider.name : "NONE")} ignoredPair={ignoredPair} ignoredLayers={ignoredLayers} penetration={penetration} surfaceY={setup.SurfaceTopY:F6}");
            return true;
        }
    }
}
