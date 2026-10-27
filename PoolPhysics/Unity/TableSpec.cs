using System.Collections.Generic;
using UnityEngine;
using PoolPhysics.Sim;

namespace PoolPhysics.Unity
{
    /// <summary>
    /// ScriptableObject wrapper around <see cref="TableSpecData"/>. Author one
    /// per table (snooker / 9-ball / 8-ball) and assign to PoolPhysicsRunner.
    /// This is the "ทุกโต๊ะ" hook: same solver, different spec.
    /// </summary>
    [CreateAssetMenu(menuName = "147VR/Pool/Table Spec", fileName = "TableSpec")]
    public sealed class TableSpec : ScriptableObject
    {
        [Header("Playfield (metres)")]
        public Vector2 playfieldSize = new Vector2(2.54f, 1.27f); // 12-ft snooker
        public Vector3 origin = Vector3.zero;

        [Header("Pockets (XZ, radius m)")]
        public List<PocketEntry> pockets = new List<PocketEntry>();

        [Header("Cloth")]
        [Range(0,3)] public int clothProfile = 0;
        public float napHeadingDeg = 0f;
        public float frictionScale = 1f;

        [System.Serializable]
        public struct PocketEntry { public Vector2 xz; public float radius; }

        public TableSpecData ToData()
        {
            var d = new TableSpecData {
                Name = name,
                PlayfieldSize = playfieldSize,
                Origin = origin,
                ClothProfile = clothProfile,
                NapHeading = napHeadingDeg * Mathf.Deg2Rad,
                FrictionScale = frictionScale,
            };
            d.BuildRectRails();
            foreach (var p in pockets)
                d.Pockets.Add(new Pocket { Center = new System.Numerics.Vector3(p.xz.x, 0, p.xz.y), Radius = p.radius });
            return d;
        }
    }
}
