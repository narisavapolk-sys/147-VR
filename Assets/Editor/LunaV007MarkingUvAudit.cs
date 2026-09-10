using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LunaV007MarkingUvAudit
{
    const string ScenePath="Assets/Scenes/147VR_MainScene.unity";
    public static void Run(){EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);var visual=GameObject.Find("V007_VISUAL_MAIN");if(!visual)throw new Exception("V007_VISUAL_MAIN not found");MeshFilter mf=null;foreach(var t in visual.GetComponentsInChildren<Transform>(true))if(t.name=="TABLE SURFACE"){mf=t.GetComponent<MeshFilter>();break;}if(!mf||!mf.sharedMesh)throw new Exception("TABLE SURFACE mesh not found");var mesh=mf.sharedMesh;var uv=mesh.uv2;var b=mf.GetComponent<Renderer>().bounds;Debug.Log($"[V007 UV AXIS] surface rot={mf.transform.rotation.eulerAngles} worldSize={b.size}");Fit(mesh,mf.transform,0,true);Fit(mesh,mf.transform,2,false);foreach(var name in new[]{"White_CueBall","Yellow","Green","Brown","Blue","Pink","Black"}){var go=GameObject.Find(name);if(go){var p=go.transform.position;var lp=mf.transform.InverseTransformPoint(p);Debug.Log($"[V007 BALL EXACT] {name} worldX={p.x:F9} worldZ={p.z:F9} localX={lp.x:F9} localZ={lp.z:F9}");}}}
    static void Fit(Mesh mesh,Transform tr,int axis,bool useU){var uv=mesh.uv2;var vv=mesh.vertices;int n=Mathf.Min(uv.Length,vv.Length);double sq=0,sa=0,sa2=0,sqa=0;for(int i=0;i<n;i++){var p=tr.TransformPoint(vv[i]);double q=useU?uv[i].x:uv[i].y;double a=axis==0?p.x:p.z;sq+=q;sa+=a;sa2+=a*a;sqa+=q*a;}double den=n*sa2-sa*sa;double slope=(n*sqa-sa*sq)/den;double intercept=(sq-slope*sa)/n;double rmse=0;for(int i=0;i<n;i++){var p=tr.TransformPoint(vv[i]);double q=useU?uv[i].x:uv[i].y;double a=axis==0?p.x:p.z;double e=q-(slope*a+intercept);rmse+=e*e;}rmse=Math.Sqrt(rmse/n);Debug.Log($"[V007 UV AXIS] {(useU?"U_vs_X":"V_vs_Z")}: slope={slope:F9} intercept={intercept:F9} rmse={rmse:F9}");}
}
