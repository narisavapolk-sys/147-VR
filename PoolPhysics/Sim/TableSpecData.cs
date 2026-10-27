using System.Collections.Generic;
using System.Numerics;

namespace PoolPhysics.Sim
{
    /// <summary>A straight cushion segment on the table plane (XZ).</summary>
    public struct Rail
    {
        public Vector3 A;        // segment start (XZ plane, Y ignored)
        public Vector3 B;        // segment end
        public Vector3 Inward;   // unit normal pointing into the playfield
        public float   Height;   // [opt] cushion nose height above cloth (for jump/deflection)
    }

    /// <summary>A pocket capture volume (sphere centered on the cloth plane).</summary>
    public struct Pocket
    {
        public Vector3 Center;   // XZ center, Y at cloth
        public float   Radius;   // capture radius
    }

    /// <summary>
    /// Table-agnostic geometry + material. The SAME PoolSimulator runs any
    /// table (snooker / 9-ball / 8-ball) by swapping this data — this is what
    /// makes the core usable "กับทุกโต๊ะ" (all bays) in 147VR.
    /// Plain data so it can be a ScriptableObject (Unity/) or set in tests.
    /// </summary>
    public sealed class TableSpecData
    {
        public string Name = "table";
        /// <summary>Playfield inner dimensions (m): length (X) x width (Z).</summary>
        public Vector2 PlayfieldSize = new Vector2(2.54f, 1.27f); // 12-ft snooker default
        /// <summary>Origin (world) of the playfield's lower-left inner corner.</summary>
        public Vector3 Origin = Vector3.Zero;

        public List<Rail>   Rails   = new List<Rail>();
        public List<Pocket> Pockets = new List<Pocket>();

        /// <summary>Cloth profile index (0 = speed cloth … ). Left for per-table tuning.</summary>
        public int ClothProfile = 0;
        /// <summary>Directional nap heading (radians) on the XZ plane; 0 = along +X.</summary>
        public float NapHeading = 0f;
        /// <summary>Optional per-table friction multiplier over the PhysicsProfile.</summary>
        public float FrictionScale = 1f;

        /// <summary>Build the 4 default rectangular rails from PlayfieldSize/Origin.</summary>
        public void BuildRectRails()
        {
            Rails.Clear();
            float x0 = Origin.X, z0 = Origin.Z;
            float x1 = x0 + PlayfieldSize.X, z1 = z0 + PlayfieldSize.Y;
            var p00 = new Vector3(x0, 0, z0); var p10 = new Vector3(x1, 0, z0);
            var p11 = new Vector3(x1, 0, z1); var p01 = new Vector3(x0, 0, z1);
            Rails.Add(new Rail { A = p00, B = p10, Inward = new Vector3(0, 0,  1) }); // -Z side
            Rails.Add(new Rail { A = p11, B = p01, Inward = new Vector3(0, 0, -1) }); // +Z side
            Rails.Add(new Rail { A = p01, B = p00, Inward = new Vector3(1, 0,  0) }); // -X side
            Rails.Add(new Rail { A = p10, B = p11, Inward = new Vector3(-1,0,  0) }); // +X side
        }
    }
}
