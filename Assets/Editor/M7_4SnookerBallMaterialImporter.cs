using UnityEditor;
using UnityEngine;

public sealed class M7_4SnookerBallMaterialImporter : AssetPostprocessor
{
    static readonly (string key, Color color)[] Balls = {
        ("147VR_BALL_Red", new Color(.8f,0,0)), ("147VR_BALL_Yellow", new Color(.95f,.75f,.02f)),
        ("147VR_BALL_Green", new Color(.03f,.3f,.06f)), ("147VR_BALL_Brown", new Color(.2f,.055f,.015f)),
        ("147VR_BALL_Blue", new Color(.02f,.18f,.8f)), ("147VR_BALL_Pink", new Color(.95f,.2f,.55f)),
        ("147VR_BALL_Black", new Color(.006f,.006f,.006f)), ("147VR_BALL_White", new Color(.92f,.92f,.92f)) };
    void OnPreprocessModel() {
        if (!assetPath.Contains("Assets/AAA/ImportedSnooker/")) return;
        var m=(ModelImporter)assetImporter;
        m.materialImportMode=ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        m.materialLocation=ModelImporterMaterialLocation.InPrefab;
    }
    static void OnAssignMaterialModel(Material material, Renderer renderer) {
        if (material==null) return;
        foreach(var b in Balls) if(material.name.Contains(b.key)) {
            if(material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",b.color);
            if(material.HasProperty("_Color")) material.SetColor("_Color",b.color);
            if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.65f);
        }
    }
}
