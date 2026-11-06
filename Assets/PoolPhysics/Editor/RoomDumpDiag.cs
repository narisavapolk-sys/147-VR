#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _147VR.Wiring
{
    /// <summary>Read-only diagnostic: dump every Renderer under NarisVintage_Room
    /// (name + world bounds center/size) to Logs/room_dump.txt. No save.</summary>
    public static class RoomDumpDiag
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/147VR_MainScene.unity", OpenSceneMode.Single);
            var room = GameObject.Find("NarisVintage_Room");
            var sb = new StringBuilder();
            if (room == null) { sb.AppendLine("NarisVintage_Room NOT FOUND. All roots:");
                foreach (var g in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.parent == null))
                    sb.AppendLine("  root: " + g.name); }
            else
            {
                sb.AppendLine($"room active={room.activeInHierarchy} children={room.transform.childCount}");
                foreach (var r in room.GetComponentsInChildren<Renderer>(true)
                             .Where(r => Regexish(r.name))
                             .OrderBy(r => r.bounds.center.z))
                    sb.AppendLine($"{r.name,-42} c=({r.bounds.center.x,6:F2},{r.bounds.center.y,5:F2},{r.bounds.center.z,6:F2}) s=({r.bounds.size.x,5:F2},{r.bounds.size.y,5:F2},{r.bounds.size.z,5:F2})");
                sb.AppendLine("--- sample of ALL renderer names (first 40) ---");
                foreach (var r in room.GetComponentsInChildren<Renderer>(true).Take(40))
                    sb.AppendLine("  " + r.name);
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/room_dump.txt", sb.ToString());
            Debug.Log("[Diag] wrote Logs/room_dump.txt");
        }
        static bool Regexish(string n) =>
            n.ToLower().Contains("table") || n.ToLower().Contains("pool") || n.ToLower().Contains("glowpad") ||
            n.ToLower().Contains("surface") || n.ToLower().Contains("frame") || n.ToLower().Contains("base");
    }
}
#endif
