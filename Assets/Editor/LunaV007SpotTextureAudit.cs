using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LunaV007SpotTextureAudit
{
    const string ScenePath="Assets/Scenes/147VR_MainScene.unity"; const int W=8192,H=4096;
    struct Hit { public int tri; public Vector3 bary; public Vector2 uv; public Vector3 world; }
    public static void Run(){
        EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single); var root=GameObject.Find("V007_VISUAL_MAIN"); if(!root)throw new Exception("V007 missing");
        MeshFilter mf=null; foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name=="TABLE SURFACE"){mf=t.GetComponent<MeshFilter>();break;}
        if(!mf||!mf.sharedMesh)throw new Exception("TABLE SURFACE mesh missing"); var m=mf.sharedMesh; var uv=m.uv2; var v=m.vertices; var tr=m.triangles;
        if(uv==null||uv.Length!=v.Length)throw new Exception("UV1 mismatch"); Debug.Log($"[V007 UV->WORLD] verts={v.Length} tris={tr.Length/3} UV1={uv.Length}");
        Check("Yellow",6435.7f,1287.7f,-1.019668f,-0.330263f,uv,v,tr,mf.transform);
        Check("Green",6435.7f,2048.3f,-1.019668f,-0.000329f,uv,v,tr,mf.transform);
        Check("Brown",6435.7f,2808.3f,-1.019668f,0.329844f,uv,v,tr,mf.transform);
        Check("Blue",4095.5f,2047.5f,0f,0f,uv,v,tr,mf.transform);
        Check("Pink",2123.3f,2047.5f,0.859162f,0f,uv,v,tr,mf.transform);
        Check("Black",802.3f,2047.5f,1.434759f,0f,uv,v,tr,mf.transform);
    }
    static void Check(string name,float px,float py,float tx,float tz,Vector2[] uv,Vector3[] verts,int[] tris,Transform tr){
        Vector2 p=new Vector2(px/W,1f-py/H); float best2=float.MaxValue; Vector3 bestWorld=default,bestBary=default; Vector2 bestUV=default; int bestTri=-1; bool inside=false;
        for(int i=0;i<tris.Length;i+=3){int i0=tris[i],i1=tris[i+1],i2=tris[i+2]; Vector2 a=uv[i0],b=uv[i1],c=uv[i2]; Vector2 q; Vector3 bary; bool hit=ClosestPointTriangle2D(p,a,b,c,out q,out bary); float d2=(q-p).sqrMagnitude; if(d2<best2){best2=d2;bestTri=i/3;bestBary=bary;bestUV=q;bestWorld=tr.TransformPoint(verts[i0]*bary.x+verts[i1]*bary.y+verts[i2]*bary.z);inside=hit;}}
        float gap=Mathf.Sqrt((bestWorld.x-tx)*(bestWorld.x-tx)+(bestWorld.z-tz)*(bestWorld.z-tz)); float du=(bestUV.x-p.x)*W,dv=(bestUV.y-p.y)*H; float dx=bestWorld.x-tx,dz=bestWorld.z-tz;
        Debug.Log($"[V007 UV->WORLD] {name}: tex=({px:F1},{py:F1}) uv=({p.x:F6},{p.y:F6}) {(inside?"INSIDE":"NEAREST")} tri={bestTri} bary=({bestBary.x:F5},{bestBary.y:F5},{bestBary.z:F5}) actualUV=({bestUV.x:F6},{bestUV.y:F6}) uvDeltaPx=({du:F2},{dv:F2}) worldFromUV=({bestWorld.x:F6},{bestWorld.z:F6}) targetBall=({tx:F6},{tz:F6}) worldDelta=({dx*1000f:F1}mm,{dz*1000f:F1}mm) gap={gap*1000f:F1}mm");
    }
    static bool ClosestPointTriangle2D(Vector2 p,Vector2 a,Vector2 b,Vector2 c,out Vector2 q,out Vector3 bary){float den=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Mathf.Abs(den)>1e-10f){float w0=((b.y-c.y)*(p.x-c.x)+(c.x-b.x)*(p.y-c.y))/den;float w1=((c.y-a.y)*(p.x-c.x)+(a.x-c.x)*(p.y-c.y))/den;float w2=1f-w0-w1;if(w0>=0&&w1>=0&&w2>=0){q=p;bary=new Vector3(w0,w1,w2);return true;}} Vector2 q1,q2,q3;float d1,d2,d3;Vector3 b1,b2,b3;Seg(p,a,b,out q1,out d1,out b1);Seg(p,b,c,out q2,out d2,out b2);Seg(p,c,a,out q3,out d3,out b3);if(d1<=d2&&d1<=d3){q=q1;bary=new Vector3(b1.x,b1.y,0);}else if(d2<=d3){q=q2;bary=new Vector3(0,b2.x,b2.y);}else{q=q3;bary=new Vector3(b3.y,0,b3.x);}return false;}
    static void Seg(Vector2 p,Vector2 a,Vector2 b,out Vector2 q,out float d2,out Vector3 bary){Vector2 ab=b-a;float den=ab.sqrMagnitude;float t=den>1e-12f?Mathf.Clamp01(Vector2.Dot(p-a,ab)/den):0;q=a+t*ab;d2=(q-p).sqrMagnitude;bary=new Vector3(1-t,t,0);}
}


