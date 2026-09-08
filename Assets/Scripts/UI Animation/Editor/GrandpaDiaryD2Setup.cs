#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class GrandpaDiaryD2Setup
{
    private const string SessionKey = "ProjectCoal.GrandpaDiaryD2Setup.p02BackgroundRect";
    private const float HalfPageWidth = 612f;

    static GrandpaDiaryD2Setup()
    {
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "Main Game Scene")
            return;

        GameObject page = FindInScene(scene, "DiaryPage_p02");
        if (page == null)
        {
            Debug.LogError("[GrandpaDiaryD2Setup] Cannot find DiaryPage_p02 in the active scene.");
            return;
        }

        Transform existingLeftPage = page.transform.Find("LeftPage");
        Transform existingRightPage = page.transform.Find("RightPage");
        if (existingLeftPage != null && existingRightPage != null)
        {
            ConfigureExistingBackgrounds(scene, page, existingLeftPage, existingRightPage);
            return;
        }

        if (existingLeftPage != null || existingRightPage != null)
        {
            Debug.LogError("[GrandpaDiaryD2Setup] DiaryPage_p02 has only one page half. No changes made.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure DiaryPage_p02 halves");

        try
        {
            RectTransform leftPage = CreateHalf(page.transform, "LeftPage", 0f, 0.5f, 1f);
            RectTransform rightPage = CreateHalf(page.transform, "RightPage", 0.5f, 1f, 0f);

            Transform pageBackground = FindDirectChild(page.transform, "PageBG");
            if (pageBackground == null || pageBackground.GetComponent<RawImage>() == null)
                throw new InvalidOperationException("DiaryPage_p02 requires a direct child RawImage named PageBG.");

            GameObject rightBackground = UnityEngine.Object.Instantiate(pageBackground.gameObject, page.transform);
            Undo.RegisterCreatedObjectUndo(rightBackground, "Duplicate p02 page background");

            Undo.RecordObject(pageBackground.gameObject, "Rename p02 left background");
            pageBackground.name = "PageBG_Left";
            rightBackground.name = "PageBG_Right";

            Undo.SetTransformParent(pageBackground, leftPage, "Move p02 left background");
            Undo.SetTransformParent(rightBackground.transform, rightPage, "Move p02 right background");
            ConfigureBackground((RectTransform)pageBackground, true);
            ConfigureBackground((RectTransform)rightBackground.transform, false);
            pageBackground.SetSiblingIndex(0);
            rightBackground.transform.SetSiblingIndex(0);

            MoveDirectChild(page.transform, leftPage, "Text_p02_body_01");
            MoveDirectChild(page.transform, rightPage, "Text_p02_body_02");

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = page;
            SessionState.SetBool(SessionKey, true);
            Debug.Log("[GrandpaDiaryD2Setup] Configured DiaryPage_p02. The scene remains unsaved for review.");
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private static void ConfigureExistingBackgrounds(
        Scene scene,
        GameObject page,
        Transform leftPage,
        Transform rightPage)
    {
        var leftBackground = leftPage.Find("PageBG_Left") as RectTransform;
        var rightBackground = rightPage.Find("PageBG_Right") as RectTransform;
        if (leftBackground == null || rightBackground == null)
        {
            Debug.LogError("[GrandpaDiaryD2Setup] Cannot find both p02 page backgrounds. No changes made.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Normalize DiaryPage_p02 backgrounds");

        ConfigureBackground(leftBackground, true);
        ConfigureBackground(rightBackground, false);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = page;
        SessionState.SetBool(SessionKey, true);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("[GrandpaDiaryD2Setup] Normalized p02 page backgrounds. The scene remains unsaved for review.");
    }

    private static void ConfigureBackground(RectTransform background, bool isLeftPage)
    {
        Undo.RecordObject(background, $"Configure {background.name}");
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.pivot = new Vector2(0.5f, 0.5f);
        background.offsetMin = isLeftPage ? Vector2.zero : new Vector2(-HalfPageWidth, 0f);
        background.offsetMax = isLeftPage ? new Vector2(HalfPageWidth, 0f) : Vector2.zero;
    }

    private static RectTransform CreateHalf(
        Transform parent,
        string objectName,
        float anchorMinX,
        float anchorMaxX,
        float pivotX)
    {
        var gameObject = new GameObject(objectName, typeof(RectTransform), typeof(RectMask2D));
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {objectName}");

        var rectTransform = (RectTransform)gameObject.transform;
        Undo.SetTransformParent(rectTransform, parent, $"Parent {objectName}");
        rectTransform.anchorMin = new Vector2(anchorMinX, 0f);
        rectTransform.anchorMax = new Vector2(anchorMaxX, 1f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(pivotX, 0.5f);
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    private static void MoveDirectChild(Transform oldParent, Transform newParent, string childName)
    {
        Transform child = FindDirectChild(oldParent, childName);
        if (child == null)
            throw new InvalidOperationException($"Cannot find direct child '{childName}'.");

        Undo.SetTransformParent(child, newParent, $"Move {childName}");
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindRecursive(root.transform, objectName);
            if (match != null)
                return match.gameObject;
        }

        return null;
    }

    private static Transform FindRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
            return parent;

        for (int index = 0; index < parent.childCount; index++)
        {
            Transform match = FindRecursive(parent.GetChild(index), objectName);
            if (match != null)
                return match;
        }

        return null;
    }
}
#endif
