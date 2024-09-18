#if UNITY_EDITOR
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
        private static Timer _timer = null;
        private static bool _indPerformSave = false;

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
            if (_timer == null)
            {
                _timer = new Timer();
                _timer.Elapsed += OnTimer;
                _timer.AutoReset = true;

                EditorApplication.update += OnEditorUpdate;
            }

            if (Config.AutoSaveConfig.Delay.value <= 0)
            {
                _timer.Enabled = false;
                return;
            }

            // Seconds to ms
            _timer.Interval = Config.AutoSaveConfig.Delay.value * 1000;
            _timer.Enabled = Config.AutoSaveConfig.Enabled.value;
        }
        #endregion Initialization

        static void OnTimer(object sender, ElapsedEventArgs e)
        {
            _indPerformSave = true;
        }

        static void OnEditorUpdate()
        {
            if (!_indPerformSave)
                return;

            try
            {
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();

                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                    EditorSceneManager.IsPreviewScene(scene) || EditorSceneManager.IsReloading(scene))
                    return;

                AssetDatabase.SaveAssets();
                bool saveOK = EditorSceneManager.SaveScene(scene);
                Debug.Log($"Saved Scene {(saveOK ? "OK" : "Error!")}");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _indPerformSave = false;
            }
        }
    }
}
#endif
