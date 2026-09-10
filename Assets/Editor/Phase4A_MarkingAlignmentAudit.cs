using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase4A_MarkingAlignmentAudit
{
    static readonly (string name, float longAxis, float widthAxis)[] Spots = {
        ("Yellow", -1.0475f, -0.292f),
        ("Green", -1.0475f,  0.292f),
        ("Brown", -1.0475f,  0.000f),
        ("Blue",   0.0000f,  0.000f),
        ("Pink",   0.89225f, 0.000f),
        ("Black",  1.4605f,  0.000f)
    };
    const float Gate=0.001f;
    const string ScenePath="Assets/Scenes/147VR_MainScene.unity";
    const string LogPath="C:/Users/mongo/AppData/Local/Temp/147VR_PHASE4A_MARKING_AUDIT_EVIDENCE.log";
    [MenuItem("147VR/Phase4/Run 4A Marking Alignment Audit")]
    public static void Run()
    {
        var lines=new List<string>();
        Action<string> L=x=>{ lines.Add(x); Debug.Log(x); };
        try {
            var scene=EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var table=GameObject.Find("Prefab_WPBSA_12Foot_Snooker");
            L($"[4A] ROOT localScale={table.transform.localScale} worldScale={table.transform.lossyScale} localRot={table.transform.localEulerAngles}");
            if(table==null) throw new Exception("Physics table root not found");
            var bed=Find(table.transform,"Bed_Collider");
            var surface=Find(table.transform,"TABLE SURFACE");
            if(bed==null||surface==null) throw new Exception("Bed_Collider or TABLE SURFACE not found");
            var sr=surface.GetComponent<Renderer>();
            var cb=bed.GetComponent<Collider>();
            L($"[4A] BED localScale={bed.localScale} worldScale={bed.lossyScale} localPos={bed.localPosition} worldPos={bed.position}");
            L($"[4A] SURFACE localScale={surface.localScale} worldScale={surface.lossyScale} localPos={surface.localPosition} worldPos={surface.position} localRot={surface.localEulerAngles}");
            Bounds vb=sr.bounds, pb=cb.bounds;
            L("[4A] Scene="+scene.path);
            L($"[4A] PhysicsBed center={pb.center} size={pb.size}");
            L($"[4A] VisualSurface center={vb.center} size={vb.size}");
            bool all=true;
            foreach(var spot in Spots){
                Transform ball=FindBall(table.transform,spot.name);
                if(ball==null){ L($"[4A] {spot.name}: BALL NOT FOUND"); all=false; continue; }
                Vector3 expected=new Vector3(vb.center.x + spot.widthAxis, ball.position.y, vb.center.z - spot.longAxis);
                float delta=Vector2.Distance(new Vector2(ball.position.x,ball.position.z),new Vector2(expected.x,expected.z));
                L($"[4A] {spot.name}: actual=({ball.position.x:F6},{ball.position.z:F6}) expected=({expected.x:F6},{expected.z:F6}) delta={delta*1000f:F3}mm");
                if(delta>=Gate) all=false;
            }
            L("[4A] D/Baulk visual convention: long-axis Z, width-axis -X after TABLE SURFACE Y=90deg integration.");
            L(all ? ">>> [PHASE 4A MARKING ALIGNMENT PASS] <<<" : ">>> [PHASE 4A MARKING ALIGNMENT FAIL] <<<");
            File.WriteAllLines(LogPath,lines);
            EditorSceneManager.SaveScene(scene);
            EditorApplication.Exit(all?0:2);
        } catch(Exception e){ L("[4A ERROR] "+e); File.WriteAllLines(LogPath,lines); EditorApplication.Exit(3); }
    }
    static Transform Find(Transform root,string n){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
    static Transform FindBall(Transform root,string n){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
}
