using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PlayerStatusHudBuilderTests
{
    [Test]
    public void Build_CreatesOneModernDarkHudAndIsIdempotent()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            CanvasScript canvas = canvasObject.AddComponent<CanvasScript>();

            PlayerStatusHud first = PlayerStatusHudBuilder.Build(canvas, null);
            PlayerStatusHud second = PlayerStatusHudBuilder.Build(canvas, null);

            Assert.That(second, Is.Not.Null);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(canvas.HasPlayerStatusHud, Is.True);
            Assert.That(
                canvas.GetComponentsInChildren<PlayerStatusHud>(true).Length,
                Is.EqualTo(1));

            Transform root = second.transform;
            Assert.That(root.name, Is.EqualTo(PlayerStatusHudBuilder.HudRootName));
            Assert.That(root.Find("HealthCard"), Is.Not.Null);
            Assert.That(root.Find("AttackCard"), Is.Not.Null);
            Assert.That(root.Find("HealthCard/HeartIcon"), Is.Not.Null);
            Assert.That(root.Find("AttackCard/AttackIcon"), Is.Not.Null);
            Assert.That(root.Find("HealthCard/Rivet_1"), Is.Null);
            Assert.That(root.Find("AttackCard/Rivet_1"), Is.Null);

            RectTransform rootRect = (RectTransform)root;
            RectTransform healthCard = (RectTransform)root.Find("HealthCard");
            RectTransform attackCard = (RectTransform)root.Find("AttackCard");
            Assert.That(rootRect.sizeDelta, Is.EqualTo(new Vector2(488f, 216f)));
            Assert.That(healthCard.sizeDelta, Is.EqualTo(new Vector2(452f, 112f)));
            Assert.That(attackCard.sizeDelta, Is.EqualTo(new Vector2(332f, 70f)));

            Image healthPanel = healthCard.GetComponent<Image>();
            Image attackPanel = attackCard.GetComponent<Image>();
            Assert.That(healthPanel.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(attackPanel.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(healthPanel.color, Is.EqualTo((Color)new Color32(27, 34, 41, 220)));
            Assert.That(attackPanel.color, Is.EqualTo((Color)new Color32(27, 34, 41, 220)));

            Image healthFill = root.Find("HealthCard/HealthBarTrack/HealthFill")
                .GetComponent<Image>();
            Assert.That(healthFill.color, Is.EqualTo((Color)new Color32(255, 91, 85, 255)));

            string[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true)
                .Select(text => text.text)
                .ToArray();
            Assert.That(labels, Does.Contain("체력"));
            Assert.That(labels, Does.Contain("공격력"));
            Assert.That(root.gameObject.activeSelf, Is.False);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void FindInScene_ReturnsOnlyComponentsFromRequestedScene()
    {
        Scene firstScene = EditorSceneManager.NewPreviewScene();
        Scene secondScene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject firstCanvasObject = new("First Canvas");
            SceneManager.MoveGameObjectToScene(firstCanvasObject, firstScene);
            firstCanvasObject.AddComponent<CanvasScript>();

            GameObject secondCanvasObject = new("Second Canvas");
            SceneManager.MoveGameObjectToScene(secondCanvasObject, secondScene);
            CanvasScript expected = secondCanvasObject.AddComponent<CanvasScript>();

            CanvasScript result = PlayerStatusHudBuilder.FindInScene<CanvasScript>(secondScene);

            Assert.That(result, Is.SameAs(expected));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(firstScene);
            EditorSceneManager.ClosePreviewScene(secondScene);
        }
    }

    [Test]
    public void ApplyMenuPath_IsStableForAutomation()
    {
        MethodInfo method = typeof(PlayerStatusHudBuilder).GetMethod(
            nameof(PlayerStatusHudBuilder.ApplyToOpenNoryangjinScene),
            BindingFlags.Public | BindingFlags.Static);
        CustomAttributeData attribute = method?.CustomAttributes
            .SingleOrDefault(candidate => candidate.AttributeType == typeof(UnityEditor.MenuItem));

        Assert.That(method, Is.Not.Null);
        Assert.That(attribute, Is.Not.Null);
        Assert.That(
            attribute.ConstructorArguments[0].Value,
            Is.EqualTo("Tools/Shooter Survival/UI/Apply Player Status HUD"));
    }

    [Test]
    public void Build_UndoRestoresPreviousPlayerCanvasBinding()
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject initialCanvasObject = new(
                "Initial Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            SceneManager.MoveGameObjectToScene(initialCanvasObject, scene);
            CanvasScript initialCanvas = initialCanvasObject.AddComponent<CanvasScript>();

            GameObject targetCanvasObject = new(
                "Target Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            SceneManager.MoveGameObjectToScene(targetCanvasObject, scene);
            CanvasScript targetCanvas = targetCanvasObject.AddComponent<CanvasScript>();

            GameObject playerObject = new("Player");
            SceneManager.MoveGameObjectToScene(playerObject, scene);
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            player.ConfigureCanvasScript(initialCanvas);

            UnityEditor.Undo.IncrementCurrentGroup();
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            PlayerStatusHudBuilder.Build(targetCanvas, player);
            UnityEditor.Undo.RevertAllDownToGroup(undoGroup);

            FieldInfo field = typeof(PlayerScript).GetField(
                "canvasScript",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            Assert.That(field.GetValue(player), Is.SameAs(initialCanvas));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [Test]
    public void SavedNoryangjinMap1_HasOneBoundModernDarkHud()
    {
        Scene previousActive = SceneManager.GetActiveScene();
        string map1Path = NoryangjinForwardGameplayInstaller.TargetScenePath;
        Scene map1 = SceneManager.GetSceneByPath(map1Path);
        bool openedMap1 = !map1.IsValid() || !map1.isLoaded;

        try
        {
            if (openedMap1)
                map1 = EditorSceneManager.OpenScene(map1Path, OpenSceneMode.Additive);

            CanvasScript[] canvases = map1.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CanvasScript>(true))
                .ToArray();
            PlayerStatusHud[] huds = map1.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlayerStatusHud>(true))
                .ToArray();

            Assert.That(canvases, Has.Length.EqualTo(1));
            Assert.That(huds, Has.Length.EqualTo(1));

            CanvasScript canvas = canvases[0];
            PlayerStatusHud hud = huds[0];
            Assert.That(hud.name, Is.EqualTo(PlayerStatusHudBuilder.HudRootName));
            Assert.That(hud.IsConfigured, Is.True);

            SerializedProperty boundHud = new SerializedObject(canvas)
                .FindProperty("playerStatusHud");
            Assert.That(boundHud, Is.Not.Null);
            Assert.That(boundHud.objectReferenceValue, Is.SameAs(hud));

            Transform healthCardTransform = hud.transform.Find("HealthCard");
            Transform attackCardTransform = hud.transform.Find("AttackCard");
            Assert.That(healthCardTransform, Is.Not.Null);
            Assert.That(attackCardTransform, Is.Not.Null);

            RectTransform healthCard = (RectTransform)healthCardTransform;
            RectTransform attackCard = (RectTransform)attackCardTransform;
            Image healthPanel = healthCard.GetComponent<Image>();
            Image attackPanel = attackCard.GetComponent<Image>();
            Assert.That(healthPanel.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(attackPanel.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(healthPanel.color, Is.EqualTo((Color)new Color32(27, 34, 41, 220)));
            Assert.That(attackPanel.color, Is.EqualTo((Color)new Color32(27, 34, 41, 220)));
            Assert.That(attackCard.rect.width, Is.LessThan(healthCard.rect.width));

            Shadow healthShadow = healthCard.GetComponent<Shadow>();
            Assert.That(healthShadow, Is.Not.Null);
            Assert.That(
                healthShadow.effectColor,
                Is.EqualTo((Color)new Color32(5, 12, 18, 145)));
            Assert.That(healthShadow.effectDistance, Is.EqualTo(new Vector2(0f, -4f)));

            Image healthTrack = healthCardTransform.Find("HealthBarTrack").GetComponent<Image>();
            Image healthFill = healthTrack.transform.Find("HealthFill").GetComponent<Image>();
            Assert.That(healthTrack.color, Is.EqualTo((Color)new Color32(52, 61, 69, 255)));
            Assert.That(healthFill.color, Is.EqualTo((Color)new Color32(255, 91, 85, 255)));

            TextMeshProUGUI[] text = hud.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.That(text, Is.Not.Empty);
            Assert.That(
                text.All(label => label.color == (Color)new Color32(241, 246, 250, 255)),
                Is.True);
            Assert.That(
                hud.GetComponentsInChildren<Transform>(true)
                    .Any(child => child.name.StartsWith("Rivet_")),
                Is.False);
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded)
                EditorSceneManager.SetActiveScene(previousActive);
            if (openedMap1 && map1.IsValid() && map1.isLoaded)
                EditorSceneManager.CloseScene(map1, true);
        }
    }
}
