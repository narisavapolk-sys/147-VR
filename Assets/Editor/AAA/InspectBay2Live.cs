#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _147VR.EditorTools
{
    public static class InspectBay2Live
    {
        [MenuItem("147VR/AAA/Debug/Inspect All Bay 2 Table Children")]
        public static void RunInspection()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ALL BAY 2 TABLE CHILDREN ===");
            Scene scene = SceneManager.GetActiveScene();
            sb.AppendLine("Active Scene: " + scene.name);

            var root = GameObject.Find("NarisVintage_Room/ROOT_PoolTable_02") ?? GameObject.Find("ROOT_PoolTable_02");
            if (root == null)
            {
                sb.AppendLine("ROOT_PoolTable_02 NOT FOUND!");
            }
            else
            {
                sb.AppendLine("Found ROOT: " + root.name + " at " + root.transform.position);
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    var mr = child.GetComponent<MeshRenderer>();
                    var mf = child.GetComponent<MeshFilter>();
                    string mrInfo = mr != null ? ("MR(enabled=" + mr.enabled + ", bounds=" + mr.bounds + ")") : "No MR";
                    string mfInfo = mf != null ? ("MF(mesh=" + (mf.sharedMesh != null ? mf.sharedMesh.name : "null") + ")") : "No MF";
                    sb.AppendLine("  " + child.name.PadRight(35) + " | Pos: " + child.position + " | " + mrInfo + " | " + mfInfo);
                }
            }

            File.WriteAllText("Logs/Bay2_All_Children.txt", sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
#endif
