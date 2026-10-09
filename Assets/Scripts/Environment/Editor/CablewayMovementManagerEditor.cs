using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(CablewayMovementManager))]
public class CablewayMovementManagerEditor : Editor
{
    private CablewayMovementManager manager;
    private bool previewing;
    private float previewDistance;
    private double previousTime;
    private readonly List<PropertyModification> originalProperties = new List<PropertyModification>();

    private void OnEnable()
    {
        manager = (CablewayMovementManager)target;
        AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.quitting += StopPreview;
        EditorSceneManager.sceneSaving += OnSceneSaving;
        Undo.undoRedoPerformed += StopPreview;
    }

    private void OnDisable()
    {
        StopPreview();
        AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.quitting -= StopPreview;
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        Undo.undoRedoPerformed -= StopPreview;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startAutomatically"), new GUIContent("进入游戏时自动启动"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("speed"), new GUIContent("移动速度 米每秒"));
        using (new EditorGUI.DisabledScope(previewing || Application.isPlaying))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("buckets"), new GUIContent("吊斗"), true);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.HelpBox("关闭自动启动后，可在关卡 UnityEvent 中绑定 StartMovement。StopMovement 停在当前位置，再次启动会继续移动。", MessageType.Info);
        if (!manager.HasConfiguration)
        {
            EditorGUILayout.HelpBox("需要配置闭合线路和吊斗引用。", MessageType.Warning);
            return;
        }

        if (Application.isPlaying)
        {
            if (GUILayout.Button(manager.IsMoving ? "停止移动" : "启动移动"))
            {
                if (manager.IsMoving) manager.StopMovement();
                else manager.StartMovement();
            }
            return;
        }

        using (new EditorGUI.DisabledScope(!manager.isActiveAndEnabled || (!previewing && AnimationMode.InAnimationMode())))
        {
            if (GUILayout.Button(previewing ? "停止预览并恢复原位" : "开始 Scene 预览"))
            {
                if (previewing) StopPreview();
                else StartPreview();
            }
        }
        if (previewing)
        {
            EditorGUI.BeginChangeCheck();
            float progress = EditorGUILayout.Slider("预览进度", previewDistance / manager.RouteLength, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                previewDistance = progress * manager.RouteLength;
                SamplePreview(previewDistance);
                SceneView.RepaintAll();
            }
        }
        EditorGUILayout.HelpBox("预览会循环运行。停止预览、切换选择、保存场景或进入 Play Mode 时自动恢复原位。", MessageType.None);
    }

    private void StartPreview()
    {
        if (previewing || AnimationMode.InAnimationMode() || !manager.PrepareMovement()) return;
        AnimationMode.StartAnimationMode();
        originalProperties.Clear();
        foreach (Transform bucket in manager.Buckets)
        {
            var properties = new SerializedObject(bucket);
            foreach (string property in new[] { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
                "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w" })
            {
                originalProperties.Add(new PropertyModification
                    {
                        target = bucket,
                        propertyPath = property,
                        value = properties.FindProperty(property).floatValue.ToString("R", CultureInfo.InvariantCulture)
                    });
            }
        }
        previewDistance = 0f;
        previousTime = EditorApplication.timeSinceStartup;
        previewing = true;
        EditorApplication.update += UpdatePreview;
    }

    private void UpdatePreview()
    {
        if (manager == null || !manager.isActiveAndEnabled || !AnimationMode.InAnimationMode())
        {
            StopPreview();
            return;
        }
        double now = EditorApplication.timeSinceStartup;
        previewDistance = Mathf.Repeat(previewDistance + manager.Speed * (float)(now - previousTime), manager.RouteLength);
        previousTime = now;
        SamplePreview(previewDistance);
        SceneView.RepaintAll();
        Repaint();
    }

    private void SamplePreview(float distance)
    {
        AnimationMode.BeginSampling();
        try
        {
            foreach (PropertyModification property in originalProperties)
                AnimationMode.AddPropertyModification(EditorCurveBinding.FloatCurve("", typeof(Transform), property.propertyPath), property, true);
            manager.SampleMovement(distance);
        }
        finally
        {
            AnimationMode.EndSampling();
        }
    }

    private void StopPreview()
    {
        if (!previewing) return;
        EditorApplication.update -= UpdatePreview;
        AnimationMode.StopAnimationMode();
        previewing = false;
        if (manager != null) manager.PrepareMovement();
        SceneView.RepaintAll();
    }

    private void OnSceneSaving(Scene scene, string path)
    {
        if (manager != null && scene == manager.gameObject.scene) StopPreview();
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode) StopPreview();
    }
}
