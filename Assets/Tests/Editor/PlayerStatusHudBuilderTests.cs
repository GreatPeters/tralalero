using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PlayerStatusHudBuilderTests
{
    [Test]
    public void Build_CreatesOneSlimTwoCardHudAndIsIdempotent()
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
}
