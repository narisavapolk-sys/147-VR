using System;
using UnityEditor;
using UnityEngine;

namespace VR147.AAA.Editor
{
    internal static class M4ManualRunMenu
    {
        [MenuItem("147VR/M4/Run Ball Collision Matrix")]
        private static void Run()
        {
            Debug.Log("[147VR M4 MENU] BEGIN");
            try
            {
                M4BallCollisionRuntimeRunner.RunAll();
                Debug.Log("[147VR M4 MENU] END OK");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("[147VR M4 MENU] END EXCEPTION");
            }
        }
    }
}
