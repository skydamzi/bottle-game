using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(UIThemeScope))]
public class UIThemeScopeInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Theme: drag your sprite set into a UITheme asset. Layouts and unbound artwork are preserved. Nested scopes use their own theme.", MessageType.Info);
        DrawDefaultInspector();
        if (!GUILayout.Button("Apply Theme / Preview")) return;
        ApplyPreview((UIThemeScope)target);
    }

    public static void ApplyPreview(UIThemeScope scope)
    {
        List<Object> objects = new List<Object>();
        foreach (UIThemeBinding binding in scope.GetComponentsInChildren<UIThemeBinding>(true))
        {
            if (binding.GetComponentInParent<UIThemeScope>(true) != scope) continue;
            if (binding.image != null) objects.Add(binding.image);
            if (binding.label != null) objects.Add(binding.label);
            if (binding.selectable != null) objects.Add(binding.selectable);
        }
        Undo.RecordObjects(objects.ToArray(), "Apply UI theme");
        scope.Apply();
        foreach (Object obj in objects)
        {
            EditorUtility.SetDirty(obj);
            if (PrefabUtility.IsPartOfPrefabInstance(obj)) PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }
    }
}

[CustomEditor(typeof(UITheme))]
public class UIThemeInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Empty sprites and unchecked overrides preserve the prefab's authored artwork. Font Size 0 preserves the existing size.", MessageType.Info);
        DrawDefaultInspector();
        if (!GUILayout.Button("Preview Theme on Loaded UI")) return;
        foreach (UIThemeScope scope in Resources.FindObjectsOfTypeAll<UIThemeScope>())
            if (!EditorUtility.IsPersistent(scope) && scope.gameObject.scene.IsValid() && scope.theme == target)
                UIThemeScopeInspector.ApplyPreview(scope);
    }
}

[CustomEditor(typeof(ExpeditionUIView))]
public class ExpeditionUIViewInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Layout contract only. Assign UGUI Text or TextMeshProUGUI to text slots. Theme Scope controls artwork; Card Prefab overrides replace the three existing card slots once at startup.", MessageType.Info);
        DrawDefaultInspector();
        ExpeditionUIView view = (ExpeditionUIView)target;
        List<string> errors = GetErrors(view);
        if (errors.Count > 0) EditorGUILayout.HelpBox(string.Join("\n", errors), MessageType.Warning);
        else EditorGUILayout.HelpBox("Required references connected.", MessageType.Info);
    }

    public static List<string> GetErrors(ExpeditionUIView view)
    {
        List<string> errors = new List<string>();
        if (view.hudLayer == null || view.modalLayer == null) errors.Add("Connect HUD and Modal CanvasGroups. Modal needs a full-screen raycast blocker.");
        if (view.preparingScreen == null || view.rewardScreen == null || view.routeScreen == null || view.visitScreen == null || view.resultScreen == null)
            errors.Add("Connect all five screen roots.");
        if (view.startButton == null || view.preparingReturnButton == null || view.skipRewardButton == null || view.purchaseButton == null || view.continueButton == null || view.restartButton == null || view.resultReturnButton == null)
            errors.Add("Connect all action buttons (including skip, return and purchase).");
        CheckCards(view.rewardCards, view.rewardCardPrefab, "Reward", errors);
        CheckCards(view.routeCards, view.routeCardPrefab, "Route", errors);
        Graphic[] labels = { view.roundStatus, view.targetingStatus, view.buildSummary, view.feedback, view.preparingDescription, view.visitTitle, view.visitDescription, view.purchaseHint, view.purchaseLabel, view.resultTitle, view.resultSummary, view.buildDetails };
        foreach (Graphic label in labels)
            if (label != null && !(label is Text) && !(label is TMP_Text)) errors.Add(label.name + " must be Text or TextMeshProUGUI, not an Image.");
        return errors;
    }

    private static void CheckCards(ExpeditionChoiceView[] cards, ExpeditionChoiceView prefab, string name, List<string> errors)
    {
        if (cards == null || cards.Length != 3) { errors.Add(name + " needs three card slots."); return; }
        for (int i = 0; i < cards.Length; i++)
        {
            ExpeditionChoiceView card = cards[i];
            if (card == null) { errors.Add(name + " card slot " + i + " is empty."); continue; }
            ExpeditionChoiceView content = prefab != null ? prefab : card;
            if (content.button == null || content.title == null || content.description == null) errors.Add(name + " card needs a button, title and description.");
            for (int j = 0; j < i; j++) if (cards[j] == card) errors.Add(name + " card slots must reference different objects.");
            if (content.title != null && !(content.title is Text) && !(content.title is TMP_Text)) errors.Add(name + " title must be a text component.");
            if (content.description != null && !(content.description is Text) && !(content.description is TMP_Text)) errors.Add(name + " description must be a text component.");
        }
    }
}

[CustomEditor(typeof(ExpeditionChoiceView))]
public class ExpeditionChoiceViewInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Connect the Button and Text/TMP labels. For imported artwork, leave Tint Background By Rarity off and use a separate Rarity Accent image.", MessageType.Info);
        DrawDefaultInspector();
        ExpeditionChoiceView card = (ExpeditionChoiceView)target;
        if (card.button == null || card.title == null || card.description == null)
            EditorGUILayout.HelpBox("Button, Title and Description are required.", MessageType.Warning);
        if ((card.title != null && !(card.title is Text) && !(card.title is TMP_Text)) ||
            (card.description != null && !(card.description is Text) && !(card.description is TMP_Text)))
            EditorGUILayout.HelpBox("Title and Description must reference UGUI Text or TextMeshProUGUI.", MessageType.Warning);
    }
}
