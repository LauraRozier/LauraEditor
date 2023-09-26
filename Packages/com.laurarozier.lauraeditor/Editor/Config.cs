using LauraEditor.Editor.AutoSave;
using LauraEditor.Editor.Hierarchy;
using LauraEditor.Runtime.Separator;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace LauraEditor.Editor
{
    public static class Config
    {
        public static class SepConfig
        {
            [UserSetting] public static UserSetting<int> MaxLength = new UserSetting<int>(Instance, "separator.maxLength", 30);
            [UserSetting] public static UserSetting<int> MinFillLength = new UserSetting<int>(Instance, "separator.minFillLength", 10);
            [UserSetting] public static UserSetting<SepType> FillType = new UserSetting<SepType>(Instance, "separator.type", SepType.Default);
            [UserSetting] public static UserSetting<string> CustomFill = new UserSetting<string>(Instance, "separator.customFill", null);
            [UserSetting] public static UserSetting<SepAlignment> Alignment = new UserSetting<SepAlignment>(Instance, "separator.alignment", SepAlignment.Center);
        }

        public static class HierarchyConfig
        {
            [UserSetting] public static UserSetting<bool> Enabled = new UserSetting<bool>(Instance, "hierarchy.enabled", true);
            [UserSetting] public static UserSetting<bool> UpdateInPlayMode = new UserSetting<bool>(Instance, "hierarchy.updateInPlayMode", true);
            [UserSetting] public static UserSetting<bool> DrawActivationToggle = new UserSetting<bool>(Instance, "hierarchy.drawActivationToggle", true);
            [UserSetting] public static UserSetting<bool> AlternateBackground = new UserSetting<bool>(Instance, "hierarchy.alterBgEnabled", true);
            [UserSetting] public static UserSetting<Color> AlternateBackgroundColor = new UserSetting<Color>(Instance, "hierarchy.alterBgColor", new Color(0, 0, 0, .08f));
            [UserSetting] public static UserSetting<bool> TreeEnabled = new UserSetting<bool>(Instance, "hierarchy.treeEnabled", true);
            [UserSetting] public static UserSetting<float> TreeDividerHeigth = new UserSetting<float>(Instance, "hierarchy.treeDividerHeigth", 1);
            [UserSetting] public static UserSetting<Color> TreeLineColor = new UserSetting<Color>(Instance, "hierarchy.treeLineColor", new Color(0.6f, 0.6f, 0.6f, 1f));
        }

        public static class AutoSaveConfig
        {
            [UserSetting] public static UserSetting<bool> Enabled = new UserSetting<bool>(Instance, "autosave.enabled", true);
            [UserSetting] public static UserSetting<int> Delay = new UserSetting<int>(Instance, "autosave.delay", 300);
        }

        internal const string k_PackageName = "com.laurarozier.lauraeditor";
        internal const string k_PreferencesPath = "Preferences/LauraEditor";

        private static Settings s_Instance;

        internal static Settings Instance
        {
            get {
                if (null == s_Instance)
                    s_Instance = new Settings(k_PackageName);

                return s_Instance;
            }
        }

        private static bool showAltBg = true;
        private static bool showTree = true;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new UserSettingsProvider(
                k_PreferencesPath,
                Instance,
                new[] { typeof(Config).Assembly },
                SettingsScope.User
            );

            return provider;
        }

        [UserSettingBlock("Separator")]
        public static void SeparatorSettingsGUI(string searchContext)
        {
            using (var scope = new EditorGUI.ChangeCheckScope()) {
                SepConfig.MaxLength.value = SettingsGUILayout.SettingsSlider("Max Length", SepConfig.MaxLength, 10, 60, searchContext);
                SepConfig.FillType.value = (SepType)EditorGUILayout.EnumPopup("Type", SepConfig.FillType.value);
                SettingsGUILayout.DoResetContextMenuForLastRect(SepConfig.FillType);

                if (SepConfig.FillType.value == SepType.Custom) {
                    string v = SettingsGUILayout.SettingsTextField("Custom Fill", SepConfig.CustomFill, searchContext);

                    if (v?.Length <= 1)
                        SepConfig.CustomFill.value = v;
                }

                SepConfig.Alignment.value = (SepAlignment)EditorGUILayout.EnumPopup("Alignment", SepConfig.Alignment.value);
                SettingsGUILayout.DoResetContextMenuForLastRect(SepConfig.Alignment);

                if (SepConfig.Alignment.value == SepAlignment.Start || SepConfig.Alignment.value == SepAlignment.End)
                    SepConfig.MinFillLength.value = SettingsGUILayout.SettingsSlider("Min Fill Length", SepConfig.MinFillLength, 0, 10, searchContext);

                if (scope.changed) {
                    Instance.Save();
                    Separator.Editor.UpdateAll();
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Update Separators"))
                    Separator.Editor.UpdateAll();
            }
        }

        [UserSettingBlock("Hierarchy")]
        public static void HierarchySettingsGUI(string searchContext)
        {
            using (var scope = new EditorGUI.ChangeCheckScope()) {
                HierarchyConfig.Enabled.value = SettingsGUILayout.SettingsToggle("Enabled", HierarchyConfig.Enabled, searchContext);
                HierarchyConfig.UpdateInPlayMode.value = SettingsGUILayout.SettingsToggle("Update In Play Mode", HierarchyConfig.UpdateInPlayMode, searchContext);
                HierarchyConfig.DrawActivationToggle.value = SettingsGUILayout.SettingsToggle("Draw Activation Toggle", HierarchyConfig.DrawActivationToggle, searchContext);

                showAltBg = EditorGUILayout.Foldout(showAltBg, "Alternating Background Settings");

                if (showAltBg) {
                    using (var group = new SettingsGUILayout.IndentedGroup()) {
                        HierarchyConfig.AlternateBackground.value = SettingsGUILayout.SettingsToggle("Enabled", HierarchyConfig.AlternateBackground, searchContext);
                        HierarchyConfig.AlternateBackgroundColor.value = SettingsGUILayout.SettingsColorField("Color", HierarchyConfig.AlternateBackgroundColor, searchContext);
                    }
                }

                showTree = EditorGUILayout.Foldout(showTree, "Tree Settings");

                if (showTree) {
                    using (var group = new SettingsGUILayout.IndentedGroup()) {
                        HierarchyConfig.TreeEnabled.value = SettingsGUILayout.SettingsToggle("Enabled", HierarchyConfig.TreeEnabled, searchContext);
                        HierarchyConfig.TreeDividerHeigth.value = SettingsGUILayout.SettingsSlider("Divider Heigth", HierarchyConfig.TreeDividerHeigth, 0, 3, searchContext);
                        HierarchyConfig.TreeLineColor.value = SettingsGUILayout.SettingsColorField("Line Color", HierarchyConfig.TreeLineColor, searchContext);
                    }
                }

                if (scope.changed) {
                    Instance.Save();
                    HierarchyWindow.Initialize();
                }
            }
        }

        [UserSettingBlock("Auto Save")]
        public static void AutoSaveSettingsGUI(string searchContext)
        {
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                AutoSaveConfig.Enabled.value = SettingsGUILayout.SettingsToggle("Enabled", AutoSaveConfig.Enabled, searchContext);
                AutoSaveConfig.Delay.value = SettingsGUILayout.SettingsIntField("Delay in seconds", AutoSaveConfig.Delay, searchContext);

                if (scope.changed)
                {
                    Instance.Save();
                    AutoSaveUtil.Initialize();
                }
            }
        }
    }
}
