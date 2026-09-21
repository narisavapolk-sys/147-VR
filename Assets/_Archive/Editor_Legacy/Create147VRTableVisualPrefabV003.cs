using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
public static class Create147VRTableVisualPrefabV003
{
 const string ModelPath="Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Base_v001.fbx";
 const string PrefabPath="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v003.prefab";
 static Material Make(string n,Color c){var p="Assets/AAA/ImportedSnooker/Materials/Balls/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"));AssetDatabase.CreateAsset(m,p);}m.color=c;if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",c);return m;}
 public static void Execute(){AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);if(model==null)throw new System.Exception("Missing FBX");
 var mats=new Dictionary<string,Material>{{"Red",Make("Ball_Red",new Color(.82f,.015f,.015f))},{"Yellow",Make("Ball_Yellow",new Color(.95f,.72f,.02f))},{"Green",Make("Ball_Green",new Color(.025f,.28f,.045f))},{"Brown",Make("Ball_Brown",new Color(.2f,.055f,.012f))},{"Blue",Make("Ball_Blue",new Color(.02f,.16f,.78f))},{"Pink",Make("Ball_Pink",new Color(.95f,.2f,.52f))},{"Black",Make("Ball_Black",new Color(.004f,.004f,.004f))},{"White",Make("Ball_White",new Color(.93f,.93f,.93f))}};
 var inst=Object.Instantiate(model);inst.name="147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v003";int balls=0;foreach(var r in inst.GetComponentsInChildren<Renderer>(true)){var n=r.gameObject.name;string k=null;if(n.StartsWith("Red"))k="Red";else if(n.StartsWith("Yellow"))k="Yellow";else if(n.StartsWith("Green"))k="Green";else if(n.StartsWith("Brown"))k="Brown";else if(n.StartsWith("Blue"))k="Blue";else if(n.StartsWith("Pink"))k="Pink";else if(n.StartsWith("Black"))k="Black";else if(n=="White_CueBall")k="White";if(k!=null){r.sharedMaterial=mats[k];balls++;}}
 var prefab=PrefabUtility.SaveAsPrefabAsset(inst,PrefabPath);Object.DestroyImmediate(inst);AssetDatabase.SaveAssets();AssetDatabase.Refresh();if(prefab==null)throw new System.Exception("Prefab save failed");Debug.Log("147VR V003 PREFAB CREATED "+PrefabPath+" BALL_RENDERERS="+balls);}
}
