#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Bay2FbxMeshAudit
{
    private const string Clean = "Assets/AAA/ImportedSnooker/Bay2/SM_TABLE_FRAME_Bay2_Clean.fbx";
    private const string Original = "Assets/AAA/Imports/naris_vintage/naris_vintage.fbx";
    private const string Key = "VR147.Bay2FbxMeshAudit.20261119";

    static Bay2FbxMeshAudit()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, true);
        Audit(Clean);
        Audit(Original);
    }

    private static void Audit(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var meshes = assets.OfType<Mesh>().ToArray();
        Debug.Log("[Bay2FbxMeshAudit] FILE=" + path + " importedAssets=" + assets.Length + " meshCount=" + meshes.Length);
        foreach (var mesh in meshes)
        {
            var counts = Enumerable.Range(0, mesh.subMeshCount)
                .Select(i => mesh.GetIndexCount(i).ToString());
            Debug.Log("[Bay2FbxMeshAudit] MESH=" + mesh.name +
                " vertices=" + mesh.vertexCount +
                " subMeshCount=" + mesh.subMeshCount +
                " indexCounts=[" + string.Join(",", counts) + "]" +
                " bounds=" + mesh.bounds);
        }
    }
}
#endif
