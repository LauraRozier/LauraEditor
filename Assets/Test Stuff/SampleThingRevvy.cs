using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class SampleThingRevvy : MonoBehaviour {
#if UNITY_EDITOR
    public Transform armature;

    private Transform eyeScaleL;
    private Transform eyeScaleR;

    private void Start() {
        Ensure_EditorTag();
        GetChildren();
    }

    private void Reset() {
        Ensure_EditorTag();
        GetChildren();
    }
    private void OnEnable() {
        Ensure_EditorTag();
        GetChildren();
    }
    private void Awake() {
        Ensure_EditorTag();
        GetChildren();
    }

    private void Ensure_EditorTag() {
        if (gameObject.tag != "EditorOnly") {
            EditorUtility.SetDirty(gameObject);
            gameObject.tag = "EditorOnly";
        }
    }

    private void GetChildren() {
        if (null == armature) {
            Debug.Log("Armature is null, ignoring.");
            return;
        }

        try { 
            eyeScaleL = armature.Find("Spine/Chest/Neck/Head/EyeScale L");
            if (null == eyeScaleL)
                Debug.LogError("Could not find child object `Spine/Chest/Neck/Head/EyeScale L`");

            eyeScaleR = armature.Find("Spine/Chest/Neck/Head/EyeScale R");
            if (null == eyeScaleR)
                Debug.LogError("Could not find child object `Spine/Chest/Neck/Head/EyeScale R`");

            Debug.Log("Found both child items.");
        } catch (Exception ex) {
            Debug.LogException(ex);
        }
    }
#endif
}
