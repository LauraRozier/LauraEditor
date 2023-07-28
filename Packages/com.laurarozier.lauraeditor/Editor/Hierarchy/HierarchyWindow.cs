using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
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
using System.Reflection;

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

        struct AnimClipAndTime
        {
            public GameObject root;
            public AnimationClip clip;
            public float time;
        }

        private static bool initialized = false;
        private static int firstInstanceID = 0;
        private static readonly Dictionary<int, InstanceInfo> sceneGameObjects = new Dictionary<int, InstanceInfo>();
        private static bool temp_alternatingDrawed;
        private static int temp_iconsDrawedCount;
        private static InstanceInfo currentItem;
        private static readonly GUIStyle ImgBtnStyle = new GUIStyle { imagePosition = ImagePosition.ImageOnly };
        private static Type animationWindowType = null;

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
        // AR Icons
        private static readonly Texture texWorldAnchor = EditorGUIUtility.IconContent("WorldAnchor Icon").image;
        // Audio Icons
        private static readonly Texture texAudioChorusFilter = EditorGUIUtility.IconContent("d_AudioChorusFilter Icon").image;
        private static readonly Texture texAudioDistortionFilter = EditorGUIUtility.IconContent("d_AudioDistortionFilter Icon").image;
        private static readonly Texture texAudioEchoFilter = EditorGUIUtility.IconContent("d_AudioEchoFilter Icon").image;
        private static readonly Texture texAudioHighPassFilter = EditorGUIUtility.IconContent("d_AudioHighPassFilter Icon").image;
        private static readonly Texture texAudioListener = EditorGUIUtility.IconContent("d_AudioListener Icon").image;
        private static readonly Texture texAudioLowPassFilter = EditorGUIUtility.IconContent("d_AudioLowPassFilter Icon").image;
        private static readonly Texture texAudioReverbFilter = EditorGUIUtility.IconContent("d_AudioReverbFilter Icon").image;
        private static readonly Texture texAudioReverbZone = EditorGUIUtility.IconContent("d_AudioReverbZone Icon").image;
        private static readonly Texture texAudioSource = EditorGUIUtility.IconContent("d_AudioSource Icon").image;
        // Effects Icons
        private static readonly Texture texHalo = EditorGUIUtility.IconContent("d_Halo Icon").image;
        private static readonly Texture texLensFlare = EditorGUIUtility.IconContent("LensFlare Icon").image;
        private static readonly Texture texLineRenderer = EditorGUIUtility.IconContent("d_LineRenderer Icon").image;
        private static readonly Texture texParticleSystem = EditorGUIUtility.IconContent("d_ParticleSystem Icon").image;
        private static readonly Texture texProjector = EditorGUIUtility.IconContent("d_Projector Icon").image;
        private static readonly Texture texTrailRenderer = EditorGUIUtility.IconContent("d_TrailRenderer Icon").image;
        private static readonly Texture texVisualEffect = EditorGUIUtility.IconContent("d_VisualEffect Icon").image;
        // Event Icons
        private static readonly Texture texEventSystem = EditorGUIUtility.IconContent("d_EventSystem Icon").image;
        private static readonly Texture texEventTrigger = EditorGUIUtility.IconContent("d_EventTrigger Icon").image;
        private static readonly Texture texGraphicRaycaster = EditorGUIUtility.IconContent("d_GraphicRaycaster Icon").image;
        private static readonly Texture texPhysics2DRaycaster = EditorGUIUtility.IconContent("d_Physics2DRaycaster Icon").image;
        private static readonly Texture texPhysicsRaycaster = EditorGUIUtility.IconContent("d_PhysicsRaycaster Icon").image;
        private static readonly Texture texStandaloneInputModule = EditorGUIUtility.IconContent("d_StandaloneInputModule Icon").image;
        private static readonly Texture texTouchInputModule = EditorGUIUtility.IconContent("d_TouchInputModule Icon").image;
        // Layout Icons
        private static readonly Texture texAspectRatioFitter = EditorGUIUtility.IconContent("d_AspectRatioFitter Icon").image;
        private static readonly Texture texCanvas = EditorGUIUtility.IconContent("d_Canvas Icon").image;
        private static readonly Texture texCanvasGroup = EditorGUIUtility.IconContent("d_CanvasGroup Icon").image;
        private static readonly Texture texCanvasScaler = EditorGUIUtility.IconContent("d_CanvasScaler Icon").image;
        private static readonly Texture texContentSizeFitter = EditorGUIUtility.IconContent("d_ContentSizeFitter Icon").image;
        private static readonly Texture texGridLayoutGroup = EditorGUIUtility.IconContent("d_GridLayoutGroup Icon").image;
        private static readonly Texture texHorizontalLayoutGroup = EditorGUIUtility.IconContent("d_HorizontalLayoutGroup Icon").image;
        private static readonly Texture texLayoutElement = EditorGUIUtility.IconContent("d_LayoutElement Icon").image;
        private static readonly Texture texRectTransform = EditorGUIUtility.IconContent("d_RectTransform Icon").image;
        private static readonly Texture texVerticalLayoutGroup = EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image;
        // Mesh Icons
        private static readonly Texture texMeshFilter = EditorGUIUtility.IconContent("d_MeshFilter Icon").image;
        private static readonly Texture texMeshRenderer = EditorGUIUtility.IconContent("d_MeshRenderer Icon").image;
        private static readonly Texture texSkinnedMeshRenderer = EditorGUIUtility.IconContent("d_SkinnedMeshRenderer Icon").image;
        private static readonly Texture texTextMesh = EditorGUIUtility.IconContent("TextMesh Icon").image;
        // Miscellaneous Icons
        private static readonly Texture texAimConstraint = EditorGUIUtility.IconContent("d_AimConstraint Icon").image;
        private static readonly Texture texAnimation = EditorGUIUtility.IconContent("d_Animation Icon").image;
        private static readonly Texture texAnimator = EditorGUIUtility.IconContent("d_Animator Icon").image;
        private static readonly Texture texBillboardRenderer = EditorGUIUtility.IconContent("d_BillboardRenderer Icon").image;
        private static readonly Texture texGrid = EditorGUIUtility.IconContent("d_Grid Icon").image;
        private static readonly Texture texLookAtConstraint = EditorGUIUtility.IconContent("d_LookAtConstraint Icon").image;
        private static readonly Texture texParentConstraint = EditorGUIUtility.IconContent("d_ParentConstraint Icon").image;
        private static readonly Texture texParticleSystemForceField = EditorGUIUtility.IconContent("d_ParticleSystemForceField Icon").image;
        private static readonly Texture texPositionConstraint = EditorGUIUtility.IconContent("d_PositionConstraint Icon").image;
        private static readonly Texture texRotationConstraint = EditorGUIUtility.IconContent("d_RotationConstraint Icon").image;
        private static readonly Texture texScaleConstraint = EditorGUIUtility.IconContent("d_ScaleConstraint Icon").image;
        private static readonly Texture texSpriteMask = EditorGUIUtility.IconContent("d_SpriteMask Icon").image;
        private static readonly Texture texSpriteShapeRenderer = EditorGUIUtility.IconContent("d_SpriteShapeRenderer Icon").image;
        private static readonly Texture texTerrain = EditorGUIUtility.IconContent("d_Terrain Icon").image;
        private static readonly Texture texWindZone = EditorGUIUtility.IconContent("d_WindZone Icon").image;
        // Navigation Icons
        private static readonly Texture texNavMeshAgent = EditorGUIUtility.IconContent("d_NavMeshAgent Icon").image;
        private static readonly Texture texNavMeshObstacle = EditorGUIUtility.IconContent("d_NavMeshObstacle Icon").image;
        private static readonly Texture texOffMeshLink = EditorGUIUtility.IconContent("d_OffMeshLink Icon").image;
        // Physics 2D Icons
        // Physics Icons
        private static readonly Texture texBoxCollider = EditorGUIUtility.IconContent("d_BoxCollider Icon").image;
        private static readonly Texture texCapsuleCollider = EditorGUIUtility.IconContent("d_CapsuleCollider Icon").image;
        private static readonly Texture texCharacterController = EditorGUIUtility.IconContent("d_CharacterController Icon").image;
        private static readonly Texture texCharacterJoint = EditorGUIUtility.IconContent("d_CharacterJoint Icon").image;
        private static readonly Texture texCloth = EditorGUIUtility.IconContent("d_Cloth Icon").image;
        private static readonly Texture texConfigurableJoint = EditorGUIUtility.IconContent("d_ConfigurableJoint Icon").image;
        private static readonly Texture texConstantForce = EditorGUIUtility.IconContent("d_ConstantForce Icon").image;
        private static readonly Texture texFixedJoint = EditorGUIUtility.IconContent("d_FixedJoint Icon").image;
        private static readonly Texture texHingeJoint = EditorGUIUtility.IconContent("d_HingeJoint Icon").image;
        private static readonly Texture texMeshCollider = EditorGUIUtility.IconContent("d_MeshCollider Icon").image;
        private static readonly Texture texRigidbody = EditorGUIUtility.IconContent("d_Rigidbody Icon").image;
        private static readonly Texture texSphereCollider = EditorGUIUtility.IconContent("d_SphereCollider Icon").image;
        private static readonly Texture texSpringJoint = EditorGUIUtility.IconContent("d_SpringJoint Icon").image;
        private static readonly Texture texTerrainCollider = EditorGUIUtility.IconContent("d_TerrainCollider Icon").image;
        private static readonly Texture texWheelCollider = EditorGUIUtility.IconContent("d_WheelCollider Icon").image;
        // Playables Icons
        private static readonly Texture texPlayableDirector = EditorGUIUtility.IconContent("d_PlayableDirector Icon").image;
        // Rendering Icons
        private static readonly Texture texCamera = EditorGUIUtility.IconContent("d_Camera Icon").image;
        private static readonly Texture texCanvasRenderer = EditorGUIUtility.IconContent("d_CanvasRenderer Icon").image;
        private static readonly Texture texFlareLayer = EditorGUIUtility.IconContent("d_FlareLayer Icon").image;
        private static readonly Texture texLight = EditorGUIUtility.IconContent("d_Light Icon").image;
        private static readonly Texture texLightProbeGroup = EditorGUIUtility.IconContent("d_LightProbeGroup Icon").image;
        private static readonly Texture texLightProbeProxyVolume = EditorGUIUtility.IconContent("d_LightProbeProxyVolume Icon").image;
        private static readonly Texture texLODGroup = EditorGUIUtility.IconContent("d_LODGroup Icon").image;
        private static readonly Texture texOcclusionArea = EditorGUIUtility.IconContent("d_OcclusionArea Icon").image;
        private static readonly Texture texOcclusionPortal = EditorGUIUtility.IconContent("d_OcclusionPortal Icon").image;
        private static readonly Texture texReflectionProbe = EditorGUIUtility.IconContent("d_ReflectionProbe Icon").image;
        private static readonly Texture texSkybox = EditorGUIUtility.IconContent("d_Skybox Icon").image;
        private static readonly Texture texSortingGroup = EditorGUIUtility.IconContent("d_SortingGroup Icon").image;
        private static readonly Texture texSpriteRenderer = EditorGUIUtility.IconContent("d_SpriteRenderer Icon").image;
        private static readonly Texture texStreamingController = EditorGUIUtility.IconContent("d_StreamingController Icon").image;
        // Tilemap Icons
        private static readonly Texture texTilemap = EditorGUIUtility.IconContent("d_Tilemap Icon").image;
        private static readonly Texture texTilemapCollider2D = EditorGUIUtility.IconContent("d_TilemapCollider2D Icon").image;
        private static readonly Texture texTilemapRenderer = EditorGUIUtility.IconContent("d_TilemapRenderer Icon").image;
        // UI Icons
        // Video Icons
        private static readonly Texture texVideoPlayer = EditorGUIUtility.IconContent("d_VideoPlayer Icon").image;
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

        static Type GetAnimationWindowType()
        {
            if (null == animationWindowType)
                animationWindowType = Type.GetType("UnityEditor.AnimationWindow,UnityEditor");

            return animationWindowType;
        }

        static UnityEngine.Object GetOpenAnimationWindow()
        {
            UnityEngine.Object[] openAnimationWindows = Resources.FindObjectsOfTypeAll(GetAnimationWindowType());

            if (openAnimationWindows.Length > 0)
                return openAnimationWindows[0];

            return null;
        }

        static AnimClipAndTime GetAnimationWindowCurrentClip()
        {
            UnityEngine.Object w = GetOpenAnimationWindow();
            AnimClipAndTime result = new AnimClipAndTime();

            if (null != w) {
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                FieldInfo animEditor = GetAnimationWindowType().GetField("m_AnimEditor", flags);

                object animEditorObject = animEditor.GetValue(w);
                FieldInfo animWindowState = animEditor.FieldType.GetField("m_State", flags);
                Type windowStateType = animWindowState.FieldType;
                object stateObject = animWindowState.GetValue(animEditorObject);

                object root = windowStateType.GetProperty("activeRootGameObject").GetValue(stateObject);
                //object root = windowStateType.GetProperty("activeGameObject").GetValue(stateObject);
                object clip = windowStateType.GetProperty("activeAnimationClip").GetValue(stateObject);
                object time = windowStateType.GetProperty("currentTime").GetValue(stateObject);

                result.root = (GameObject)root;
                result.clip = (AnimationClip)clip;
                result.time = (float)time;
            }

            return result;
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
                    /*
                    if (AnimationMode.InAnimationMode() && !EditorApplication.isPlaying) {
                        AnimClipAndTime clipAndtime = GetAnimationWindowCurrentClip();
                        go.SetActive(!go.activeSelf);

                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(clipAndtime.root, clipAndtime.clip, clipAndtime.time);
                        AnimationMode.EndSampling();
                    } else {
                    */
                        go.SetActive(!go.activeSelf);
                        EditorUtility.SetDirty(go);

                        if (!EditorApplication.isPlaying)
                            EditorSceneManager.MarkSceneDirty(go.scene);
                    //}
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
                DrawComponentIconsToGUI(component, selectionRect, ref addedTypes);
            #endregion
        }

        static void DrawComponentIconsToGUI(Component componentType, Rect selectionRect, ref List<string> addedTypes)
        {
            if (null == componentType)
                return;

            #region AR Icons
            if (componentType is WorldAnchor) {
                var fullName = typeof(WorldAnchor).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texWorldAnchor
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }
            #endregion

            #region Audio Icons
            if (componentType is AudioChorusFilter) {
                var fullName = typeof(AudioChorusFilter).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAudioChorusFilter
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is AudioDistortionFilter) {
                var fullName = typeof(AudioDistortionFilter).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAudioDistortionFilter
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is AudioEchoFilter) {
                var fullName = typeof(AudioEchoFilter).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAudioEchoFilter
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is AudioHighPassFilter) {
                var fullName = typeof(AudioHighPassFilter).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAudioHighPassFilter
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is AudioListener) {
                var fullName = typeof(AudioListener).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAudioListener
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is AudioLowPassFilter) {
                var fullName = typeof(AudioLowPassFilter).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAudioLowPassFilter
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is AudioReverbFilter) {
                var fullName = typeof(AudioReverbFilter).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAudioReverbFilter
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is AudioReverbZone) {
                var fullName = typeof(AudioReverbZone).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAudioReverbZone
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is AudioSource) {
                var fullName = typeof(AudioSource).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAudioSource
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }
            #endregion

            #region Effects Icons
            if (componentType.GetType().Name == "Halo") {
                var fullName = componentType.GetType().FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texHalo
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is LensFlare) {
                var fullName = typeof(LensFlare).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texLensFlare
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is LineRenderer) {
                var fullName = typeof(LineRenderer).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texLineRenderer
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is ParticleSystem) {
                var fullName = typeof(ParticleSystem).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texParticleSystem
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is Projector) {
                var fullName = typeof(Projector).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texProjector
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is TrailRenderer) {
                var fullName = typeof(TrailRenderer).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texTrailRenderer
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }

            if (componentType is VisualEffect) {
                var fullName = typeof(VisualEffect).FullName;

                if (!addedTypes.Contains(fullName)) {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texVisualEffect
                    );
                    addedTypes.Add(fullName);
                }

                return;
            }
            #endregion

            #region Events Icons
            if (componentType is EventSystem)
            {
                var fullName = typeof(EventSystem).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texEventSystem
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is EventTrigger)
            {
                var fullName = typeof(EventTrigger).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texEventTrigger
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is GraphicRaycaster)
            {
                var fullName = typeof(GraphicRaycaster).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texGraphicRaycaster
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is Physics2DRaycaster)
            {
                var fullName = typeof(Physics2DRaycaster).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texPhysics2DRaycaster
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is PhysicsRaycaster)
            {
                var fullName = typeof(PhysicsRaycaster).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texPhysicsRaycaster
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is StandaloneInputModule)
            {
                var fullName = typeof(StandaloneInputModule).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texStandaloneInputModule
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (componentType is TouchInputModule)
            {
                var fullName = typeof(TouchInputModule).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texTouchInputModule
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }
#pragma warning restore CS0618 // Type or member is obsolete
            #endregion

            #region Layout Icons
            if (componentType is AspectRatioFitter)
            {
                var fullName = typeof(AspectRatioFitter).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAspectRatioFitter
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is Canvas)
            {
                var fullName = typeof(Canvas).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texCanvas
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is CanvasGroup)
            {
                var fullName = typeof(CanvasGroup).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texCanvasGroup
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is CanvasScaler)
            {
                var fullName = typeof(CanvasScaler).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texCanvasScaler
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is ContentSizeFitter)
            {
                var fullName = typeof(ContentSizeFitter).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texContentSizeFitter
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is GridLayoutGroup)
            {
                var fullName = typeof(GridLayoutGroup).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texGridLayoutGroup
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is HorizontalLayoutGroup)
            {
                var fullName = typeof(HorizontalLayoutGroup).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texHorizontalLayoutGroup
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is LayoutElement)
            {
                var fullName = typeof(LayoutElement).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texLayoutElement
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is RectTransform)
            {
                var fullName = typeof(RectTransform).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texRectTransform
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is VerticalLayoutGroup)
            {
                var fullName = typeof(VerticalLayoutGroup).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texVerticalLayoutGroup
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }
            #endregion

            #region Mesh Icons
            if (componentType is MeshFilter)
            {
                var fullName = typeof(MeshFilter).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texMeshFilter
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is MeshRenderer)
            {
                var fullName = typeof(MeshRenderer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texMeshRenderer
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is SkinnedMeshRenderer)
            {
                var fullName = typeof(SkinnedMeshRenderer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texSkinnedMeshRenderer
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is TextMesh)
            {
                var fullName = typeof(TextMesh).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texTextMesh
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }
            #endregion

            #region Miscellaneous Icons
            if (componentType is AimConstraint)
            {
                var fullName = typeof(AimConstraint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAimConstraint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is Animation)
            {
                var fullName = typeof(Animation).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAnimation
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is Animator)
            {
                var fullName = typeof(Animator).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texAnimator
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is BillboardRenderer)
            {
                var fullName = typeof(BillboardRenderer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texBillboardRenderer
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is Grid)
            {
                var fullName = typeof(Grid).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texGrid
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is LookAtConstraint)
            {
                var fullName = typeof(LookAtConstraint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texLookAtConstraint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is ParentConstraint)
            {
                var fullName = typeof(ParentConstraint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texParentConstraint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is ParticleSystemForceField)
            {
                var fullName = typeof(ParticleSystemForceField).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texParticleSystemForceField
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is PositionConstraint)
            {
                var fullName = typeof(PositionConstraint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texPositionConstraint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is RotationConstraint)
            {
                var fullName = typeof(RotationConstraint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texRotationConstraint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is ScaleConstraint)
            {
                var fullName = typeof(ScaleConstraint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texScaleConstraint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is SpriteMask)
            {
                var fullName = typeof(SpriteMask).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texSpriteMask
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is SpriteShapeRenderer)
            {
                var fullName = typeof(SpriteShapeRenderer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texSpriteShapeRenderer
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is Terrain)
            {
                var fullName = typeof(Terrain).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texTerrain
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is WindZone)
            {
                var fullName = typeof(WindZone).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texWindZone
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }
            #endregion

            #region Navigation Icons
            if (componentType is NavMeshAgent)
            {
                var fullName = typeof(NavMeshAgent).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texNavMeshAgent
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is NavMeshObstacle)
            {
                var fullName = typeof(NavMeshObstacle).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texNavMeshObstacle
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is OffMeshLink)
            {
                var fullName = typeof(OffMeshLink).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texOffMeshLink
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }
            #endregion

            #region Physics 2D Icons
            /*
            if (componentType is VideoPlayer)
            {
                var fullName = typeof(VideoPlayer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texVideoPlayer
                    );
                    addedTypes.Add(fullName);
                }
            }
            */
            #endregion

            #region Physics Icons
            if (componentType is BoxCollider)
            {
                var fullName = typeof(BoxCollider).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texBoxCollider
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is CapsuleCollider)
            {
                var fullName = typeof(CapsuleCollider).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texCapsuleCollider
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is CharacterController)
            {
                var fullName = typeof(CharacterController).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texCharacterController
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is CharacterJoint)
            {
                var fullName = typeof(CharacterJoint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texCharacterJoint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is Cloth)
            {
                var fullName = typeof(Cloth).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texCloth
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is ConfigurableJoint)
            {
                var fullName = typeof(ConfigurableJoint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texConfigurableJoint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is ConstantForce)
            {
                var fullName = typeof(ConstantForce).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texConstantForce
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is FixedJoint)
            {
                var fullName = typeof(FixedJoint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texFixedJoint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is HingeJoint)
            {
                var fullName = typeof(HingeJoint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texHingeJoint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is MeshCollider)
            {
                var fullName = typeof(MeshCollider).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texMeshCollider
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is Rigidbody)
            {
                var fullName = typeof(Rigidbody).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texRigidbody
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is SphereCollider)
            {
                var fullName = typeof(SphereCollider).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texSphereCollider
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is SpringJoint)
            {
                var fullName = typeof(SpringJoint).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texSpringJoint
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is TerrainCollider)
            {
                var fullName = typeof(TerrainCollider).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texTerrainCollider
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is WheelCollider)
            {
                var fullName = typeof(WheelCollider).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texWheelCollider
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }
            #endregion

            #region Playables Icons
            if (componentType is PlayableDirector)
            {
                var fullName = typeof(PlayableDirector).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texPlayableDirector
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }
            #endregion

            #region Rendering Icons
            if (componentType is Camera)
            {
                var fullName = typeof(Camera).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texCamera
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is CanvasRenderer)
            {
                var fullName = typeof(CanvasRenderer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texCanvasRenderer
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is FlareLayer)
            {
                var fullName = typeof(FlareLayer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texFlareLayer
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is Light)
            {
                var fullName = typeof(Light).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texLight
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is LightProbeGroup)
            {
                var fullName = typeof(LightProbeGroup).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texLightProbeGroup
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is LightProbeProxyVolume)
            {
                var fullName = typeof(LightProbeProxyVolume).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texLightProbeProxyVolume
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is LODGroup)
            {
                var fullName = typeof(LODGroup).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texLODGroup
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is OcclusionArea)
            {
                var fullName = typeof(OcclusionArea).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texOcclusionArea
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is OcclusionPortal)
            {
                var fullName = typeof(OcclusionPortal).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texOcclusionPortal
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is ReflectionProbe)
            {
                var fullName = typeof(ReflectionProbe).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texReflectionProbe
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is Skybox)
            {
                var fullName = typeof(Skybox).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texSkybox
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is SortingGroup)
            {
                var fullName = typeof(SortingGroup).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texSortingGroup
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is SpriteRenderer)
            {
                var fullName = typeof(SpriteRenderer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texSpriteRenderer
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }

            if (componentType is StreamingController)
            {
                var fullName = typeof(StreamingController).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texStreamingController
                    );
                    addedTypes.Add(fullName);
                }
                return;
            }
            #endregion

            #region Tilemap Icons
            if (componentType is Tilemap)
            {
                var fullName = typeof(Tilemap).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texTilemap
                    );
                    addedTypes.Add(fullName);
                }
            }

            if (componentType is TilemapCollider2D)
            {
                var fullName = typeof(TilemapCollider2D).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texTilemapCollider2D
                    );
                    addedTypes.Add(fullName);
                }
            }

            if (componentType is TilemapRenderer)
            {
                var fullName = typeof(TilemapRenderer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texTilemapRenderer
                    );
                    addedTypes.Add(fullName);
                }
            }
            #endregion

            #region UI Icons
            /*
            if (componentType is VideoPlayer)
            {
                var fullName = typeof(VideoPlayer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texVideoPlayer
                    );
                    addedTypes.Add(fullName);
                }
            }
            */
            #endregion

            #region Video Icons
            if (componentType is VideoPlayer)
            {
                var fullName = typeof(VideoPlayer).FullName;

                if (!addedTypes.Contains(fullName))
                {
                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * ++temp_iconsDrawedCount - 2, selectionRect.yMin, 16, 16),
                        texVideoPlayer
                    );
                    addedTypes.Add(fullName);
                }
            }
            #endregion
        }
    }
}
