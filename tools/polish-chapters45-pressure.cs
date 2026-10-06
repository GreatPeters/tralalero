using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Run after the tower fold. Only this named generated subtree is replaced.
public static class PolishChapters45Pressure
{
    const string OwnedRoot = "Chapter45PressurePolish";
    const string AssetRoot = "Assets/ShooterSurvival/Models/Chapters/Chapters45/";
    static Material amber, navy, ivory;
    static TMP_FontAsset font;

    public static object Main()
    {
        Guard(); var setup = EditorSceneManager.GetSceneManagerSetup();
        var receipts = new List<object>();
        try
        {
            foreach (string name in new[] { "Jamsil", "ShoeTower" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
                receipts.Add(Apply(scene));
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Pressure polish save failed: " + name);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return receipts;
    }

    public static object OpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new Exception("Idle Edit Mode required");
        var scene = SceneManager.GetActiveScene();
        var receipt = Apply(scene); EditorSceneManager.MarkSceneDirty(scene); return receipt;
    }

    static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new Exception("Idle Edit Mode required");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new Exception("Preserve dirty scene before pressure polish");
    }

    static object Apply(Scene scene)
    {
        if (!Chapter45Director.IsChapterScene(scene.name)) throw new Exception("Only Jamsil / ShoeTower are supported");
        var director = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Chapter45Director>(true)).Single();
        if (director.route == null || director.route.segments.Length == 0) throw new Exception("Authored route required");
        amber = Load("Brass.mat"); navy = Load("Navy.mat"); ivory = Load("Ivory.mat");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ShooterSurvival/Resources/UI/GmarketHarbor SDF.asset");
        if (font == null) throw new Exception("Verified Korean UI font required");
        var old = director.transform.Find(OwnedRoot);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var owned = Group(director.transform, OwnedRoot);
        int holds = 0, pressureStations = 0;
        var details = new List<object>();
        foreach (var encounter in director.GetComponentsInChildren<Chapter45Encounter>(true).Where(e => e.requiredToProceed))
        {
            var station = Station(owned, director, "Hold_" + encounter.name, encounter.stopDistance,
                encounter.floor, encounter.choiceIndex, encounter.requiredChoice);
            HoldLine(station, 0, 7, "경비 제압 후 전진"); holds++;
            Pressure(station, director, encounter, null, encounter.stopDistance, encounter.floor, encounter.choiceIndex, encounter.requiredChoice);
            pressureStations++;
            details.Add(new { encounter = encounter.name, stop = encounter.stopDistance, floor = encounter.floor,
                choice = encounter.choiceIndex, requiredChoice = encounter.requiredChoice, position = station.position.ToString("F2") });
        }
        foreach (var target in director.GetComponentsInChildren<Chapter45Target>(true).Where(t => t.blocking))
        {
            float stop = target.distance - 6;
            var station = Station(owned, director, "Hold_" + target.name, stop, target.floor, target.choiceIndex, target.requiredChoice);
            HoldLine(station, target.blocksAllLanes ? 0 : target.lane, target.blocksAllLanes ? 7 : 3.1f,
                target.captain ? "코어 개방 시 사격" : "패널을 열고 전진"); holds++;
            if (!target.captain && target.choiceIndex >= 0 && target.requiredChoice == 1)
            {
                Pressure(station, director, null, target, stop, target.floor, target.choiceIndex, target.requiredChoice);
                pressureStations++;
            }
        }
        // Preserve all earlier hazards, including the captain's separate phase-owned pressure.
        director.hazards = director.GetComponentsInChildren<Chapter45Hazard>(true);
        EditorUtility.SetDirty(director);
        return new { scene = scene.name, holds, pressureStations, manualPressureHazards = pressureStations * 2,
            firstWarningDelay = 1.8f, cadence = 6f, footprint = "3.1m x 11.2m swept volume per side; opposite side remains clear", details };
    }

    static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>(AssetRoot + name)
        ?? throw new Exception("Missing chapter material " + name);

    static Transform Station(Transform parent, Chapter45Director owner, string name, float distance, int floor, int choiceIndex, int selection)
    {
        owner.route.Sample(distance, out var center, out var forward);
        // Runtime Director.Sample adds this offset only after commitment. Author the same
        // branch frame explicitly; never use an old straight-Z coordinate after folding.
        if (choiceIndex >= 0)
        {
            var choices = owner.choices.Length > 0 ? owner.choices : owner.GetComponentsInChildren<Chapter45Choice>(true);
            if (choiceIndex >= choices.Length) throw new Exception("Missing choice " + choiceIndex);
            var choice = choices[choiceIndex];
            var right = Vector3.Cross(Vector3.up, forward);
            center += right * choice.Offset(distance, selection);
            float slope = (choice.Offset(distance + .25f, selection) - choice.Offset(distance - .25f, selection)) / .5f;
            forward = (forward + right * slope).normalized;
        }
        var station = Group(parent, name); station.SetPositionAndRotation(center, Quaternion.LookRotation(forward));
        var visibility = station.gameObject.AddComponent<Chapter45SceneryGroup>();
        visibility.floor = floor; visibility.startDistance = distance - 20; visibility.endDistance = distance + 20;
        return station;
    }

    static void HoldLine(Transform station, float lane, float width, string caption)
    {
        Box(station, "VisibleHoldLine", new Vector3(lane, .035f, .15f), new Vector3(width, .04f, .48f), ivory, false);
        int count = Mathf.FloorToInt(width / .8f);
        for (int i = 0; i < count; i++)
        {
            var stripe = Box(station, "HoldHatch_" + i, new Vector3(lane - width * .5f + .4f + i * .8f, .061f, .15f),
                new Vector3(.26f, .02f, .42f), navy, false);
            stripe.transform.localRotation = Quaternion.Euler(0, -25, 0);
        }
        var text = new GameObject("HoldInstruction", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        text.transform.SetParent(station, false); text.transform.localPosition = new Vector3(lane, .085f, -2.1f);
        text.transform.localRotation = Quaternion.Euler(90, 0, 0);
        text.rectTransform.sizeDelta = new Vector2(width, 1.7f); text.font = font; text.fontSize = 2.1f;
        text.alignment = TextAlignmentOptions.Center; text.color = new Color(.96f, .91f, .69f); text.text = caption;
        text.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    static void Pressure(Transform station, Chapter45Director director, Chapter45Encounter encounter, Chapter45Target target,
        float stop, int floor, int choiceIndex, int selection)
    {
        var pressure = station.gameObject.AddComponent<Chapter45EncounterPressure>();
        pressure.director = director; pressure.encounter = encounter; pressure.target = target;
        pressure.stopDistance = stop; pressure.floor = floor; pressure.firstWarningDelay = 1.8f; pressure.warningCadence = 6;
        pressure.alternatingSweeps = new Chapter45Hazard[2];
        for (int index = 0; index < 2; index++)
        {
            float side = index == 0 ? -1 : 1;
            var root = Group(station, index == 0 ? "PressureLeft" : "PressureRight");
            var hazard = root.gameObject.AddComponent<Chapter45Hazard>();
            hazard.manualOnly = true; hazard.distance = stop; hazard.floor = floor;
            hazard.choiceIndex = choiceIndex; hazard.requiredChoice = selection;
            hazard.safeLane = -side * 2.4f; hazard.safeHalfWidth = 1.5f;
            hazard.warningSeconds = 1.6f; hazard.operationSeconds = 1.3f; hazard.damageFraction = .06f;
            hazard.warning = side < 0 ? "왼쪽 바닥 경고 · 오른쪽으로 피하세요" : "오른쪽 바닥 경고 · 왼쪽으로 피하세요";
            // Exact cube primitive/collider transform: no model pivot/scale mismatch.
            var body = Box(root, "VisibleSecurityRam", new Vector3(side * 1.75f, 1.1f, 0), new Vector3(3.1f, 2.2f, 1.2f), amber, true);
            hazard.body = body.transform;
            hazard.sweepFrom = new Vector3(0, 0, 5.5f); hazard.sweepTo = new Vector3(0, 0, -4.5f);
            // Projected union of the moving body's exact bounds: -5.1..6.1 in Z.
            hazard.footprint = Box(root, "ExactSweptFootprint", new Vector3(side * 1.75f, .032f, .5f), new Vector3(3.1f, .035f, 11.2f), amber, false);
            Box(hazard.footprint.transform, "DirectionArrowStem", new Vector3(0, .85f, 0), new Vector3(.07f, .3f, .52f), navy, false);
            body.SetActive(false); hazard.footprint.SetActive(false);
            pressure.alternatingSweeps[index] = hazard;
        }
    }

    static Transform Group(Transform parent, string name)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
    }
    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool contact)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material; go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        var collider = go.GetComponent<BoxCollider>();
        if (contact) { collider.isTrigger = true; collider.center = Vector3.zero; collider.size = Vector3.one; }
        else Object.DestroyImmediate(collider);
        return go;
    }
}
