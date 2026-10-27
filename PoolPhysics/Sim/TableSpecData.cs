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

        /// <summary>
        /// Segmented rails with 6 pocket mouths (4 corners + 2 mid-side), snooker-style.
        /// Pockets sit slightly OUTSIDE the playfield and the rails stop short of them,
        /// so balls reach the pocket circles through the mouths. Sweeping the rail
        /// ENDPOINTS as round caps (jaw tips) closes the "escape past both rail ends"
        /// tunneling that a line-only CCD cannot see.
        /// </summary>
        public void BuildSnookerRails(float pocketRadius)
        {
            Rails.Clear(); Pockets.Clear();
            float x0 = Origin.X, z0 = Origin.Z;
            float x1 = x0 + PlayfieldSize.X, z1 = z0 + PlayfieldSize.Y;
            float mx = (x0 + x1) * 0.5f, mz = (z0 + z1) * 0.5f;
            float m = pocketRadius * 1.45f;                      // mouth half-gap at each jaw

            // corner pocket centres, offset OUT of the playfield along each corner diagonal
            var cA = new Vector3(x0 - pocketRadius*0.45f, 0, z0 - pocketRadius*0.45f);
            var cB = new Vector3(x1 + pocketRadius*0.45f, 0, z0 - pocketRadius*0.45f);
            var cC = new Vector3(x1 + pocketRadius*0.45f, 0, z1 + pocketRadius*0.45f);
            var cD = new Vector3(x0 - pocketRadius*0.45f, 0, z1 + pocketRadius*0.45f);
            var cE = new Vector3(mx, 0, z0 - pocketRadius*0.6f);   // bottom side pocket
            var cF = new Vector3(mx, 0, z1 + pocketRadius*0.6f);   // top side pocket
            foreach (var c in new[]{cA,cB,cC,cD,cE,cF}) Pockets.Add(new Pocket{ Center=c, Radius=pocketRadius });

            // bottom (-Z, inward +Z): two segments split by the side-pocket mouth
            Rails.Add(new Rail{ A=new Vector3(x0+m,0,z0), B=new Vector3(mx-m,0,z0), Inward=new Vector3(0,0, 1) });
            Rails.Add(new Rail{ A=new Vector3(mx+m,0,z0), B=new Vector3(x1-m,0,z0), Inward=new Vector3(0,0, 1) });
            // top (+Z, inward -Z)
            Rails.Add(new Rail{ A=new Vector3(x1-m,0,z1), B=new Vector3(mx+m,0,z1), Inward=new Vector3(0,0,-1) });
            Rails.Add(new Rail{ A=new Vector3(mx-m,0,z1), B=new Vector3(x0+m,0,z1), Inward=new Vector3(0,0,-1) });
            // left (-X, inward +X)
            Rails.Add(new Rail{ A=new Vector3(x0,0,z1-m), B=new Vector3(x0,0,z0+m), Inward=new Vector3( 1,0,0) });
            // right (+X, inward -X)
            Rails.Add(new Rail{ A=new Vector3(x1,0,z0+m), B=new Vector3(x1,0,z1-m), Inward=new Vector3(-1,0,0) });
        }
    }
}
