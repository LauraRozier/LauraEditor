using System;
using System.Timers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LauraEditor.Editor.AutoSave
{
    [InitializeOnLoad]
    public static class AutoSaveUtil
    {
        private static Timer timer = null;
        private static bool doUpdate = false;

        static AutoSaveUtil()
        {
            Initialize();
        }

        #region Initialization
        /// <summary>
        /// Initializes the script at the beginning. 
        /// </summary>
        public static void Initialize()
        {
            if (timer == null)
            {
                timer = new Timer();
                timer.Elapsed += OnTimer;
                timer.AutoReset = true;

                EditorApplication.update += OnEditorUpdate;
            }

            if (Config.AutoSaveConfig.Delay.value <= 0)
            {
                timer.Enabled = false;
                return;
            }

            // Seconds to ms
            timer.Interval = Config.AutoSaveConfig.Delay.value * 1000;
            timer.Enabled = Config.AutoSaveConfig.Enabled.value;
        }
        #endregion Initialization

        static void OnTimer(object sender, ElapsedEventArgs e)
        {
            doUpdate = true;
        }

        static void OnEditorUpdate()
        {
            if (!doUpdate)
                return;

            try
            {
                AssetDatabase.SaveAssets();
                bool saveOK = EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("Saved Scene " + (saveOK ? "OK" : "Error!"));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            doUpdate = false;
        }
    }
}
