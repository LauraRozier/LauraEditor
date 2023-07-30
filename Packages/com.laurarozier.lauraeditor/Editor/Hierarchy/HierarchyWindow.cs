using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;
using UnityEngine.XR.WSA;
using UnityEngine.VFX;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.U2D;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.Video;
using System.Linq;

namespace LauraEditor.Editor.Hierarchy
{
    [InitializeOnLoad]
    public static class HierarchyWindow
    {
        static HierarchyWindow()
        {
            Initialize();
        }

        enum HLineType
        {
            None = 0,
            Half,
            Full
        }

        [Serializable]
        struct InstanceInfo
        {
            public string GoName;
            public int prefabInstanceID;
            public bool IsGoActive;

            public bool HasChilds;
            public bool TopParentHasChild;

            public int NestingGroup;
            public int NestingLevel;
            public HLineType[] HLines;
        }

        private struct TypeIconData
        {
            public Type ObjType;
            public Texture Icon;
        }

        static class HierarchyRenderer
        {
            private const float barWidth = 2;

            static float GetStartX(Rect originalRect, int nestLevel) => 37 + (originalRect.height - 2) * nestLevel;

            public static void DrawHalfVerticalLineFrom(Rect originalRect, bool startsOnTop, int nestLevel)
            {
                // Vertical rect, starts from the very left and then proceeds to the right
                EditorGUI.DrawRect(
                    new Rect(
                        GetStartX(originalRect, nestLevel),
                        startsOnTop ? originalRect.y : (originalRect.y + originalRect.height / 2f),
                        barWidth,
                        originalRect.height / 2f
                    ),
                    Config.HierarchyConfig.TreeLineColor
                );
            }

            public static void DrawHorizontalLineFrom(Rect originalRect, int nestLevel, bool hasChilds)
            {
                // Vertical rect, starts from the very left and then proceeds to the right
                EditorGUI.DrawRect(
                    new Rect(
                        GetStartX(originalRect, nestLevel),
                        originalRect.y + originalRect.height / 2f,
                        originalRect.height + (hasChilds ? -5 : 2),
                        barWidth
                    ),
                    Config.HierarchyConfig.TreeLineColor
                );
            }
        }

        private static bool initialized = false;
        private static int firstInstanceID = 0;
        private static readonly Dictionary<int, InstanceInfo> sceneGameObjects = new Dictionary<int, InstanceInfo>();
        private static bool temp_alternatingDrawed;
        private static int temp_iconsDrawedCount;
        private static InstanceInfo currentItem;
        private static readonly GUIStyle ImgBtnStyle = new GUIStyle { imagePosition = ImagePosition.ImageOnly };

        #region Icon Textures
        // Custom Icons
        private static Texture texBtnOff = null;
        private static Texture texBtnOn = null;
        private static Texture texStatic = null;
        // Look here for more icons: https://github.com/halak/unity-editor-icons
        // Issue Icons
        private static readonly Texture texErr = EditorGUIUtility.IconContent("console.erroricon").image;
        // Script Icons
        private static readonly Texture texScript = EditorGUIUtility.IconContent("cs Script Icon").image;
        // Special Snowflake Icons
        private static readonly Texture texHalo = EditorGUIUtility.IconContent("d_Halo Icon").image;

        private static readonly TypeIconData[] typeIconDataList = new TypeIconData[]
        {
            #region AR Icons
            new TypeIconData {
                ObjType = typeof(WorldAnchor),
                Icon = EditorGUIUtility.IconContent("WorldAnchor Icon").image
            },
            #endregion AR Icons

            #region Audio Icons
            new TypeIconData {
                ObjType = typeof(AudioChorusFilter),
                Icon = EditorGUIUtility.IconContent("d_AudioChorusFilter Icon").image
            },
            new TypeIconData {
                ObjType = typeof(AudioDistortionFilter),
                Icon = EditorGUIUtility.IconContent("d_AudioDistortionFilter Icon").image
            },
            new TypeIconData {
                ObjType = typeof(AudioEchoFilter),
                Icon = EditorGUIUtility.IconContent("d_AudioEchoFilter Icon").image
            },
            new TypeIconData {
                ObjType = typeof(AudioHighPassFilter),
                Icon = EditorGUIUtility.IconContent("d_AudioHighPassFilter Icon").image
            },
            new TypeIconData {
                ObjType = typeof(AudioListener),
                Icon = EditorGUIUtility.IconContent("d_AudioListener Icon").image
            },
            new TypeIconData {
                ObjType = typeof(AudioLowPassFilter),
                Icon = EditorGUIUtility.IconContent("d_AudioLowPassFilter Icon").image
            },
            new TypeIconData {
                ObjType = typeof(AudioReverbFilter),
                Icon = EditorGUIUtility.IconContent("d_AudioReverbFilter Icon").image
            },
            new TypeIconData {
                ObjType = typeof(AudioReverbZone),
                Icon = EditorGUIUtility.IconContent("d_AudioReverbZone Icon").image
            },
            new TypeIconData {
                ObjType = typeof(AudioSource),
                Icon = EditorGUIUtility.IconContent("d_AudioSource Icon").image
            },
            #endregion Audio Icons

            #region Effects Icons
            new TypeIconData {
                ObjType = typeof(LensFlare),
                Icon = EditorGUIUtility.IconContent("LensFlare Icon").image
            },
            new TypeIconData {
                ObjType = typeof(LineRenderer),
                Icon = EditorGUIUtility.IconContent("d_LineRenderer Icon").image
            },
            new TypeIconData {
                ObjType = typeof(ParticleSystem),
                Icon = EditorGUIUtility.IconContent("d_ParticleSystem Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Projector),
                Icon = EditorGUIUtility.IconContent("d_Projector Icon").image
            },
            new TypeIconData {
                ObjType = typeof(TrailRenderer),
                Icon = EditorGUIUtility.IconContent("d_TrailRenderer Icon").image
            },
            new TypeIconData {
                ObjType = typeof(VisualEffect),
                Icon = EditorGUIUtility.IconContent("d_VisualEffect Icon").image
            },
            #endregion Effects Icons

            #region Event Icons
            new TypeIconData {
                ObjType = typeof(EventSystem),
                Icon =EditorGUIUtility.IconContent("d_EventSystem Icon").image
            },
            new TypeIconData {
                ObjType = typeof(EventTrigger),
                Icon = EditorGUIUtility.IconContent("d_EventTrigger Icon").image
            },
            new TypeIconData {
                ObjType = typeof(GraphicRaycaster),
                Icon = EditorGUIUtility.IconContent("d_GraphicRaycaster Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Physics2DRaycaster),
                Icon = EditorGUIUtility.IconContent("d_Physics2DRaycaster Icon").image
            },
            new TypeIconData {
                ObjType = typeof(PhysicsRaycaster),
                Icon = EditorGUIUtility.IconContent("d_PhysicsRaycaster Icon").image
            },
            new TypeIconData {
                ObjType = typeof(StandaloneInputModule),
                Icon = EditorGUIUtility.IconContent("d_StandaloneInputModule Icon").image
            },
            new TypeIconData {
#pragma warning disable CS0618 // Type or member is obsolete
                ObjType = typeof(TouchInputModule),
                Icon = EditorGUIUtility.IconContent("d_TouchInputModule Icon").image
#pragma warning restore CS0618 // Type or member is obsolete
            },
            #endregion Event Icons

            #region Layout Icons
            new TypeIconData {
                ObjType = typeof(AspectRatioFitter),
                Icon = EditorGUIUtility.IconContent("d_AspectRatioFitter Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Canvas),
                Icon = EditorGUIUtility.IconContent("d_Canvas Icon").image
            },
            new TypeIconData {
                ObjType = typeof(CanvasGroup),
                Icon = EditorGUIUtility.IconContent("d_CanvasGroup Icon").image
            },
            new TypeIconData {
                ObjType = typeof(CanvasScaler),
                Icon = EditorGUIUtility.IconContent("d_CanvasScaler Icon").image
            },
            new TypeIconData {
                ObjType = typeof(ContentSizeFitter),
                Icon = EditorGUIUtility.IconContent("d_ContentSizeFitter Icon").image
            },
            new TypeIconData {
                ObjType = typeof(GridLayoutGroup),
                Icon = EditorGUIUtility.IconContent("d_GridLayoutGroup Icon").image
            },
            new TypeIconData {
                ObjType = typeof(HorizontalLayoutGroup),
                Icon = EditorGUIUtility.IconContent("d_HorizontalLayoutGroup Icon").image
            },
            new TypeIconData {
                ObjType = typeof(LayoutElement),
                Icon = EditorGUIUtility.IconContent("d_LayoutElement Icon").image
            },
            new TypeIconData {
                ObjType = typeof(RectTransform),
                Icon = EditorGUIUtility.IconContent("d_RectTransform Icon").image
            },
            new TypeIconData {
                ObjType = typeof(VerticalLayoutGroup),
                Icon = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image
            },
            #endregion Layout Icons

            #region Mesh Icons
            new TypeIconData {
                ObjType = typeof(MeshFilter),
                Icon = EditorGUIUtility.IconContent("d_MeshFilter Icon").image
            },
            new TypeIconData {
                ObjType = typeof(MeshRenderer),
                Icon = EditorGUIUtility.IconContent("d_MeshRenderer Icon").image
            },
            new TypeIconData {
                ObjType = typeof(SkinnedMeshRenderer),
                Icon = EditorGUIUtility.IconContent("d_SkinnedMeshRenderer Icon").image
            },
            new TypeIconData {
                ObjType = typeof(TextMesh),
                Icon = EditorGUIUtility.IconContent("TextMesh Icon").image
            },
            #endregion Mesh Icons

            #region Miscellaneous Icons
            new TypeIconData {
                ObjType = typeof(AimConstraint),
                Icon = EditorGUIUtility.IconContent("d_AimConstraint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Animation),
                Icon = EditorGUIUtility.IconContent("d_Animation Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Animator),
                Icon = EditorGUIUtility.IconContent("d_Animator Icon").image
            },
            new TypeIconData {
                ObjType = typeof(BillboardRenderer),
                Icon = EditorGUIUtility.IconContent("d_BillboardRenderer Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Grid),
                Icon = EditorGUIUtility.IconContent("d_Grid Icon").image
            },
            new TypeIconData {
                ObjType = typeof(LookAtConstraint),
                Icon = EditorGUIUtility.IconContent("d_LookAtConstraint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(ParentConstraint),
                Icon = EditorGUIUtility.IconContent("d_ParentConstraint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(ParticleSystemForceField),
                Icon = EditorGUIUtility.IconContent("d_ParticleSystemForceField Icon").image
            },
            new TypeIconData {
                ObjType = typeof(PositionConstraint),
                Icon = EditorGUIUtility.IconContent("d_PositionConstraint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(RotationConstraint),
                Icon = EditorGUIUtility.IconContent("d_RotationConstraint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(ScaleConstraint),
                Icon = EditorGUIUtility.IconContent("d_ScaleConstraint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(SpriteMask),
                Icon = EditorGUIUtility.IconContent("d_SpriteMask Icon").image
            },
            new TypeIconData {
                ObjType = typeof(SpriteShapeRenderer),
                Icon = EditorGUIUtility.IconContent("d_SpriteShapeRenderer Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Terrain),
                Icon = EditorGUIUtility.IconContent("d_Terrain Icon").image
            },
            new TypeIconData {
                ObjType = typeof(WindZone),
                Icon = EditorGUIUtility.IconContent("d_WindZone Icon").image
            },
            #endregion Miscellaneous Icons

            #region Navigation Icons
            new TypeIconData {
                ObjType = typeof(NavMeshAgent),
                Icon = EditorGUIUtility.IconContent("d_NavMeshAgent Icon").image
            },
            new TypeIconData {
                ObjType = typeof(NavMeshObstacle),
                Icon = EditorGUIUtility.IconContent("d_NavMeshObstacle Icon").image
            },
            new TypeIconData {
                ObjType = typeof(OffMeshLink),
                Icon = EditorGUIUtility.IconContent("d_OffMeshLink Icon").image
            },
            #endregion Navigation Icons

            #region Physics 2D Icons
            #endregion Physics 2D Icons

            #region Physics Icons
            new TypeIconData {
                ObjType = typeof(BoxCollider),
                Icon = EditorGUIUtility.IconContent("d_BoxCollider Icon").image
            },
            new TypeIconData {
                ObjType = typeof(CapsuleCollider),
                Icon = EditorGUIUtility.IconContent("d_CapsuleCollider Icon").image
            },
            new TypeIconData {
                ObjType = typeof(CharacterController),
                Icon = EditorGUIUtility.IconContent("d_CharacterController Icon").image
            },
            new TypeIconData {
                ObjType = typeof(CharacterJoint),
                Icon = EditorGUIUtility.IconContent("d_CharacterJoint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Cloth),
                Icon = EditorGUIUtility.IconContent("d_Cloth Icon").image
            },
            new TypeIconData {
                ObjType = typeof(ConfigurableJoint),
                Icon = EditorGUIUtility.IconContent("d_ConfigurableJoint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(ConstantForce),
                Icon = EditorGUIUtility.IconContent("d_ConstantForce Icon").image
            },
            new TypeIconData {
                ObjType = typeof(FixedJoint),
                Icon = EditorGUIUtility.IconContent("d_FixedJoint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(HingeJoint),
                Icon = EditorGUIUtility.IconContent("d_HingeJoint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(MeshCollider),
                Icon = EditorGUIUtility.IconContent("d_MeshCollider Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Rigidbody),
                Icon = EditorGUIUtility.IconContent("d_Rigidbody Icon").image
            },
            new TypeIconData {
                ObjType = typeof(SphereCollider),
                Icon = EditorGUIUtility.IconContent("d_SphereCollider Icon").image
            },
            new TypeIconData {
                ObjType = typeof(SpringJoint),
                Icon = EditorGUIUtility.IconContent("d_SpringJoint Icon").image
            },
            new TypeIconData {
                ObjType = typeof(TerrainCollider),
                Icon = EditorGUIUtility.IconContent("d_TerrainCollider Icon").image
            },
            new TypeIconData {
                ObjType = typeof(WheelCollider),
                Icon = EditorGUIUtility.IconContent("d_WheelCollider Icon").image
            },
            #endregion Physics Icons

            #region Playables Icons
            new TypeIconData {
                ObjType = typeof(PlayableDirector),
                Icon = EditorGUIUtility.IconContent("d_PlayableDirector Icon").image
            },
            #endregion Playables Icons

            #region Rendering Icons
            new TypeIconData {
                ObjType = typeof(Camera),
                Icon = EditorGUIUtility.IconContent("d_Camera Icon").image
            },
            new TypeIconData {
                ObjType = typeof(CanvasRenderer),
                Icon = EditorGUIUtility.IconContent("d_CanvasRenderer Icon").image
            },
            new TypeIconData {
                ObjType = typeof(FlareLayer),
                Icon = EditorGUIUtility.IconContent("d_FlareLayer Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Light),
                Icon = EditorGUIUtility.IconContent("d_Light Icon").image
            },
            new TypeIconData {
                ObjType = typeof(LightProbeGroup),
                Icon = EditorGUIUtility.IconContent("d_LightProbeGroup Icon").image
            },
            new TypeIconData {
                ObjType = typeof(LightProbeProxyVolume),
                Icon = EditorGUIUtility.IconContent("d_LightProbeProxyVolume Icon").image
            },
            new TypeIconData {
                ObjType = typeof(LODGroup),
                Icon = EditorGUIUtility.IconContent("d_LODGroup Icon").image
            },
            new TypeIconData {
                ObjType = typeof(OcclusionArea),
                Icon = EditorGUIUtility.IconContent("d_OcclusionArea Icon").image
            },
            new TypeIconData {
                ObjType = typeof(OcclusionPortal),
                Icon = EditorGUIUtility.IconContent("d_OcclusionPortal Icon").image
            },
            new TypeIconData {
                ObjType = typeof(ReflectionProbe),
                Icon = EditorGUIUtility.IconContent("d_ReflectionProbe Icon").image
            },
            new TypeIconData {
                ObjType = typeof(Skybox),
                Icon = EditorGUIUtility.IconContent("d_Skybox Icon").image
            },
            new TypeIconData {
                ObjType = typeof(SortingGroup),
                Icon = EditorGUIUtility.IconContent("d_SortingGroup Icon").image
            },
            new TypeIconData {
                ObjType = typeof(SpriteRenderer),
                Icon = EditorGUIUtility.IconContent("d_SpriteRenderer Icon").image
            },
            new TypeIconData {
                ObjType = typeof(StreamingController),
                Icon = EditorGUIUtility.IconContent("d_StreamingController Icon").image
            },
            #endregion Rendering Icons

            #region Tilemap Icons
            new TypeIconData {
                ObjType = typeof(Tilemap),
                Icon = EditorGUIUtility.IconContent("d_Tilemap Icon").image
            },
            new TypeIconData {
                ObjType = typeof(TilemapCollider2D),
                Icon = EditorGUIUtility.IconContent("d_TilemapCollider2D Icon").image
            },
            new TypeIconData {
                ObjType = typeof(TilemapRenderer),
                Icon = EditorGUIUtility.IconContent("d_TilemapRenderer Icon").image
            },
            #endregion Tilemap Icons

            #region UI Icons
            #endregion UI Icons

            #region Video Icons
            new TypeIconData {
                ObjType = typeof(VideoPlayer),
                Icon = EditorGUIUtility.IconContent("d_VideoPlayer Icon").image
            },
            #endregion Video Icons
        };
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the script at the beginning. 
        /// </summary>
        public static void Initialize()
        {
            // Unregisters previous events
            if (initialized) {
                // Prevents registering events multiple times
                EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUI;
                EditorApplication.hierarchyChanged -= RetrieveDataFromScene;
            }

            initialized = true;

            if (Config.HierarchyConfig.Enabled) {
                EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
                EditorApplication.hierarchyChanged += RetrieveDataFromScene;

                RetrieveDataFromScene();
            }

            EditorApplication.RepaintHierarchyWindow();
        }
        #endregion Initialization

        /// <summary>
        /// Updates the list of objects to draw, icons etc.
        /// </summary>
        static void RetrieveDataFromScene()
        {
            if (!Config.HierarchyConfig.UpdateInPlayMode && Application.isPlaying) // Fix for performance reasons while in play mode
                return;

            sceneGameObjects.Clear();
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if (null != prefabStage) {
                GameObject prefabContentsRoot = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                AnalyzeGoWithChildren(prefabContentsRoot, -1, 0, true, new HLineType[0]);
                firstInstanceID = prefabContentsRoot.GetInstanceID();
                return;
            }

            GameObject[] sceneRoots;
            Scene tempScene;
            firstInstanceID = -1;

            for (int i = 0; i < SceneManager.sceneCount; i++) {
                tempScene = SceneManager.GetSceneAt(i);

                if (tempScene.isLoaded) {
                    sceneRoots = tempScene.GetRootGameObjects();

                    // Analyzes all scene's gameObjects
                    for (int j = 0; j < sceneRoots.Length; j++)
                        AnalyzeGoWithChildren(sceneRoots[j], 0, j, j == sceneRoots.Length - 1, new HLineType[0]);

                    if (firstInstanceID == -1 && sceneRoots.Length > 0)
                        firstInstanceID = sceneRoots[0].GetInstanceID();
                }
            }
        }

        static void AnalyzeGoWithChildren(GameObject go, int nestingLevel, int nestingGroup, bool isLastChild, IEnumerable<HLineType> hLines)
        {
            int instanceID = go.GetInstanceID();
            List<HLineType> newHLines;

            if (!sceneGameObjects.ContainsKey(instanceID)) { // Processes the gameobject only if it wasn't processed already
                newHLines = new List<HLineType>(hLines);

                if (newHLines.Count > 0 && newHLines[newHLines.Count - 1] == HLineType.Half)
                    newHLines[newHLines.Count - 1] = HLineType.None;

                newHLines.Add(isLastChild ? HLineType.Half : HLineType.Full);

                InstanceInfo newInfo = new InstanceInfo {
                    NestingLevel = nestingLevel,
                    NestingGroup = nestingGroup,
                    HasChilds = go.transform.childCount > 0,
                    IsGoActive = go.activeInHierarchy,
                    GoName = go.name,
                    HLines = newHLines.ToArray()
                };

                // Adds element to the array
                sceneGameObjects.Add(instanceID, newInfo);
            } else {
                newHLines = new List<HLineType>(sceneGameObjects[instanceID].HLines);
            }

            // Analyzes Childrens
            int childCount = go.transform.childCount;

            for (int j = 0; j < childCount; j++) {
                AnalyzeGoWithChildren(
                    go.transform.GetChild(j).gameObject,
                    nestingLevel + 1,
                    nestingGroup,
                    j == childCount - 1,
                    newHLines
                );
            }
        }

        static bool HasError(GameObject go)
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                return true;

            return false;
        }

        static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            //skips early if item is not registered or not valid
            if (!sceneGameObjects.ContainsKey(instanceID))
                return;

            if (null == texBtnOff)
                texBtnOff = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.laurarozier.lauraeditor/Textures/BtnOff.png");

            if (null == texBtnOn)
                texBtnOn = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.laurarozier.lauraeditor/Textures/BtnOn.png");

            if (null == texStatic)
                texStatic = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.laurarozier.lauraeditor/Textures/IconPin.png");

            currentItem = sceneGameObjects[instanceID];
            temp_iconsDrawedCount = 0;

            if (firstInstanceID == instanceID)
                temp_alternatingDrawed = currentItem.NestingGroup % 2 == 0;

            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (null == go)
                return;

            #region Draw Alternating BG
            if (Config.HierarchyConfig.AlternateBackground) {
                if (temp_alternatingDrawed)
                    EditorGUI.DrawRect(selectionRect, Config.HierarchyConfig.AlternateBackgroundColor);

                temp_alternatingDrawed = !temp_alternatingDrawed;
            }
            #endregion

            #region Drawing Tree
            if (Config.HierarchyConfig.TreeEnabled && currentItem.NestingLevel >= 0) {
                if (selectionRect.x >= 60) { // Prevents drawing when the hierarchy search mode is enabled
                    for (var i = 0; i < currentItem.HLines.Length; i++) {
                        switch (currentItem.HLines[i]) {
                        case HLineType.None: break;
                        case HLineType.Half:
                            {
                                HierarchyRenderer.DrawHalfVerticalLineFrom(selectionRect, true, i);
                                break;
                            }
                        case HLineType.Full:
                            {
                                HierarchyRenderer.DrawHalfVerticalLineFrom(selectionRect, true, i);
                                HierarchyRenderer.DrawHalfVerticalLineFrom(selectionRect, false, i);
                                break;
                            }
                        }
                    }

                    HierarchyRenderer.DrawHorizontalLineFrom(
                        selectionRect,
                        currentItem.NestingLevel,
                        currentItem.HasChilds
                    );
                }

                // Draws a super small divider between different groups
                if (currentItem.NestingLevel == 0 && Config.HierarchyConfig.TreeDividerHeigth > 0) {
                    Rect boldGroupRect = new Rect(
                        32, selectionRect.y - Config.HierarchyConfig.TreeDividerHeigth / 2f,
                        selectionRect.width + (selectionRect.x - 32),
                        Config.HierarchyConfig.TreeDividerHeigth
                    );
                    EditorGUI.DrawRect(boldGroupRect, Color.black * .3f);
                }
            }
            #endregion

            #region Draw Activation Toggle
            if (Config.HierarchyConfig.DrawActivationToggle && texBtnOff != null && texBtnOn != null) {
                var btnContent = new GUIContent(go.activeSelf ? texBtnOn : texBtnOff, "GameObject Active");

                if (GUI.Button(
                    new Rect(
                        selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2,
                        selectionRect.yMin,
                        16,
                        16
                    ),
                    btnContent,
                    ImgBtnStyle
                )) {
                    bool enabledState = !go.activeSelf;
                    Undo.RecordObject(go, (enabledState ? "En" : "Dis") + "able Object");
                    EditorUtility.SetObjectEnabled(go, enabledState);
                }
            }
            #endregion

            #region Draw Icons
            bool hasScript = go.TryGetComponent<MonoBehaviour>(out var monoScript);

            if (hasScript && monoScript.GetType().FullName == "LauraEditor.Runtime.Separator.SeparatorHeader")
                return;

            if (HasError(go))
                GUI.DrawTexture(
                    new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                    texErr
                );

            if (go.isStatic && texStatic != null)
                GUI.DrawTexture(
                    new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                    texStatic
                );

            if (hasScript)
                GUI.DrawTexture(
                    new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                    texScript
                );

            List<string> addedTypes = new List<string>();

            foreach (var component in go.GetComponents(typeof(Component)))
            {
                // Skip the component
                if (null == component)
                    continue;

                DrawComponentIconsToGUI(component.GetType(), selectionRect, ref addedTypes);
            }
            #endregion
        }

        static void DrawComponentIconsToGUI(Type componentType, Rect selectionRect, ref List<string> addedTypes)
        {
            TypeIconData data = typeIconDataList.FirstOrDefault(x => x.ObjType.Equals(componentType));

            // Exit when we don't know the type
            if (data.Equals(default(TypeIconData))) {
                if (componentType.Name == "Halo") {
                    data = new TypeIconData {
                        ObjType = componentType,
                        Icon = texHalo
                    };
                } else {
                    return;
                }
            }

            string fullName = data.ObjType.FullName;

            if (!addedTypes.Contains(fullName)) {
                GUI.DrawTexture(
                    new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                    data.Icon
                );
                addedTypes.Add(fullName);
            }
        }
    }
}
