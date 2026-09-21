using System.Collections.Generic;
using UnityEngine;
namespace VR147.AAA.Physics.Golden
{
 [CreateAssetMenu(menuName="147VR/Physics/M4 Ball Collision Golden Catalog")]
 public sealed class M4BallCollisionGoldenCatalog : ScriptableObject
 {
  public List<M4BallCollisionGolden> cases=new();
  public bool Validate(){if(cases==null||cases.Count!=6)return false;foreach(var c in cases)if(c==null||!c.IsValid())return false;return true;}
 }
}
