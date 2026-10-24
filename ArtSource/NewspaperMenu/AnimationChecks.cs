if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Run this isolated check in Edit Mode.");
var source = UnityEngine.GameObject.Find("MainMenuUIRoot");
var beforeTweenObjects = UnityEngine.Object.FindObjectsByType<DG.Tweening.Core.DOTweenComponent>(UnityEngine.FindObjectsSortMode.None).Select(c => c.gameObject).ToArray();
var preview = UnityEngine.Object.Instantiate(source);
preview.name = "NewspaperAnimationCheck";
var results = new System.Collections.Generic.List<string>();
void Check(bool ok, string message)
{
    if (!ok) throw new System.Exception(message);
    results.Add("PASS " + message);
}
try
{
    var canvas = preview.GetComponent<UnityEngine.Canvas>();
    canvas.enabled = true;
    var controller = preview.GetComponentInChildren<NewspaperMenuUI>();
    var paper = (UnityEngine.RectTransform)controller.transform;
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    typeof(NewspaperMenuUI).GetMethod("Awake", flags).Invoke(controller, null);
    UnityEngine.Canvas.ForceUpdateCanvases();
    var originalPosition = paper.anchoredPosition;
    var texts = controller.GetComponentsInChildren<TMPro.TMP_Text>();
    foreach (var text in texts) text.ForceMeshUpdate();
    Check(texts.Length == 20 && texts.All(t => !t.isTextOverflowing), "Twenty visible labels fit their text rectangles");
    Check(texts.All(t => t.font.atlasPopulationMode == TMPro.AtlasPopulationMode.Static), "Every newspaper label uses a persisted static font atlas");
    Check(texts.All(t => t.text.Where(c => !char.IsWhiteSpace(c)).All(c => t.font.HasCharacter(c))), "Every displayed character exists in its assigned font");
    var manager = preview.GetComponent<MainMenuUIManager>();
    foreach (var item in new[] { new {name="StartGameButton", method="StartGame"}, new {name="QuitButton", method="QuitGame"} })
    {
        var button = paper.Find(item.name).GetComponent<UnityEngine.UI.Button>();
        Check(button.onClick.GetPersistentEventCount() == 1 && button.onClick.GetPersistentTarget(0) == manager && button.onClick.GetPersistentMethodName(0) == item.method, item.name + " retains its manager binding");
        Check(button.interactable && button.GetComponent<UnityEngine.UI.Image>().raycastTarget, item.name + " has an enabled click target");
    }
    Check(!paper.Find("ContinueButton").GetComponent<UnityEngine.UI.Button>().interactable, "Continue stays disabled");
    var serialized = new UnityEditor.SerializedObject(manager);
    Check(serialized.FindProperty("newspaper").objectReferenceValue == controller && serialized.FindProperty("openingSequence").objectReferenceValue != null, "Manager references the newspaper and existing opening sequence");
    var lateUpdate = typeof(NewspaperMenuUI).GetMethod("LateUpdate", flags);
    var baseline = (UnityEngine.Vector3[])texts[0].textInfo.meshInfo[0].vertices.Clone();
    lateUpdate.Invoke(controller, null);
    Check(texts[0].textInfo.meshInfo[0].vertices.Where((v,i) => UnityEngine.Vector3.Distance(v, baseline[i]) > .01f).Any(), "Idle ripple deforms actual TMP vertices");
    int completed = 0;
    controller.BlowAway(() => completed++);
    var tween = (DG.Tweening.Tween)typeof(NewspaperMenuUI).GetField("exitTween", flags).GetValue(controller);
    DG.Tweening.TweenSettingsExtensions.SetUpdate(tween, DG.Tweening.UpdateType.Manual, true);
    Check(System.Math.Abs(DG.Tweening.TweenExtensions.Duration(tween, false) - 1.1f) < .001f, "Paper departure lasts 1.1 seconds");
    DG.Tweening.TweenExtensions.Goto(tween, .55f, false);
    lateUpdate.Invoke(controller, null);
    UnityEngine.Canvas.ForceUpdateCanvases();
    lateUpdate.Invoke(controller, null);
    Check(completed == 0 && paper.anchoredPosition.x > originalPosition.x && paper.anchoredPosition.y > originalPosition.y, "Halfway departure moves upper right without advancing the opening");
    Check(texts.All(t => t.textInfo.meshInfo.All(m => m.vertices.Take(m.vertexCount).All(v => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z)))), "Curled text vertices remain finite after a canvas rebuild");
    Check(controller.GetComponentInChildren<NewspaperGraphicBend>().GetComponent<UnityEngine.CanvasRenderer>().GetMesh().vertexCount > 4, "Paper surface is subdivided for bending");
    DG.Tweening.TweenExtensions.Goto(tween, 1.1f, false);
    Check(completed == 1, "Completion invokes the opening handoff exactly once");
    var corners = new UnityEngine.Vector3[4];
    paper.GetWorldCorners(corners);
    var canvasRect = (UnityEngine.RectTransform)canvas.transform;
    Check(corners.All(v => canvasRect.InverseTransformPoint(v).x > canvasRect.rect.xMax), "Entire paper is beyond the right edge at handoff");
    controller.gameObject.SetActive(false);
    typeof(NewspaperMenuUI).GetMethod("OnDisable", flags).Invoke(controller, null);
    Check(UnityEngine.Vector2.Distance(paper.anchoredPosition, originalPosition) < .01f, "Disabling restores the authored paper position");
    results.Add("NOT RUN Play Mode lifecycle, input, frame-to-frame animation and cancellation; Edit Mode does not initialize the normal DOTween player lifecycle.");
}
finally
{
    var controller = preview.GetComponentInChildren<NewspaperMenuUI>(true);
    typeof(NewspaperMenuUI).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(controller, null);
    UnityEngine.Object.DestroyImmediate(preview);
    if (beforeTweenObjects.Length == 0) DG.Tweening.DOTween.Clear(true);
    foreach (var component in UnityEngine.Object.FindObjectsByType<DG.Tweening.Core.DOTweenComponent>(UnityEngine.FindObjectsSortMode.None))
        if (!beforeTweenObjects.Contains(component.gameObject)) UnityEngine.Object.DestroyImmediate(component.gameObject);
}
System.IO.File.WriteAllLines("ArtSource/NewspaperMenu/checks.txt", results);
return results;
