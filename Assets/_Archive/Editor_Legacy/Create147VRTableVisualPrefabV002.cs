using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class Create147VRTableVisualPrefabV002
{
    const string ModelPath="Assets/AAA/ImportedSnooker/147VR_Table_WPBSA_Visual_Clean_v002.fbx";
    const string PrefabPath="Assets/AAA/ImportedSnooker/Prefabs/147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v002.prefab";
    const string MatDir="Assets/AAA/ImportedSnooker/Materials/Balls";
    static Material Make(string name, Color color)
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath,"AAA/ImportedSnooker/Materials/Balls"));
        string p=MatDir+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        if(m==null){ var sh=Shader.Find("Universal Render Pipeline/Lit"); if(sh==null) sh=Shader.Find("Standard"); m=new Material(sh); AssetDatabase.CreateAsset(m,p); }
        m.color=color; if(m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",color); if(m.HasProperty("_Metallic")) m.SetFloat("_Metallic",0f); if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",0.28f);
        EditorUtility.SetDirty(m); return m;
    }
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath); if(model==null) throw new System.Exception("Missing FBX: "+ModelPath);
        var mats=new Dictionary<string,Material>{
            ["Red"]=Make("Ball_Red",new Color(0.82f,0.015f,0.015f)),["Yellow"]=Make("Ball_Yellow",new Color(0.95f,0.72f,0.02f)),["Green"]=Make("Ball_Green",new Color(0.025f,0.28f,0.045f)),["Brown"]=Make("Ball_Brown",new Color(0.20f,0.055f,0.012f)),["Blue"]=Make("Ball_Blue",new Color(0.02f,0.16f,0.78f)),["Pink"]=Make("Ball_Pink",new Color(0.95f,0.20f,0.52f)),["Black"]=Make("Ball_Black",new Color(0.004f,0.004f,0.004f)),["White"]=Make("Ball_White",new Color(0.93f,0.93f,0.93f))};
        var inst=Object.Instantiate(model); inst.name="147VR_Table_WPBSA_12ft_VISUAL_CLEAN_v002"; inst.transform.position=Vector3.zero; inst.transform.rotation=Quaternion.identity; inst.transform.localScale=Vector3.one;
        int balls=0;
        foreach(var r in inst.GetComponentsInChildren<Renderer>(true)){
            string n=r.gameObject.name; string k=null;
            if(n.StartsWith("Red")) k="Red"; else if(n.StartsWith("Yellow")) k="Yellow"; else if(n.StartsWith("Green")) k="Green"; else if(n.StartsWith("Brown")) k="Brown"; else if(n.StartsWith("Blue")) k="Blue"; else if(n.StartsWith("Pink")) k="Pink"; else if(n.StartsWith("Black")) k="Black"; else if(n=="White_CueBall") k="White";
            if(k!=null){r.sharedMaterial=mats[k]; balls++;}
        }
        Directory.CreateDirectory(Path.Combine(Application.dataPath,"AAA/ImportedSnooker/Prefabs")); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        var prefab=PrefabUtility.SaveAsPrefabAsset(inst,PrefabPath); Object.DestroyImmediate(inst); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        if(prefab==null) throw new System.Exception("Prefab save failed");
        Debug.Log("147VR CLEAN TABLE PREFAB CREATED: "+PrefabPath+" BALL_RENDERERS="+balls);
    }
}
