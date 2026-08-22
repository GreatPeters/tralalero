using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerStatusHudTests
{
    [Test]
    public void SetHealth_FormatsCurrentAndMaxAndClampsFill()
    {
        GameObject root = new("PlayerStatusHUD Test");
        GameObject healthTextObject = new("HealthValue", typeof(RectTransform));
        GameObject healthFillObject = new("HealthFill", typeof(RectTransform));
        GameObject attackTextObject = new("AttackValue", typeof(RectTransform));

        try
        {
            TextMeshProUGUI healthText = healthTextObject.AddComponent<TextMeshProUGUI>();
            Image healthFill = healthFillObject.AddComponent<Image>();
            TextMeshProUGUI attackText = attackTextObject.AddComponent<TextMeshProUGUI>();
            PlayerStatusHud hud = root.AddComponent<PlayerStatusHud>();
            hud.Configure(healthText, healthFill, attackText);

            hud.SetHealth(75.4f, 100f);

            Assert.That(healthText.text, Is.EqualTo("75 / 100"));
            Assert.That(healthFill.fillAmount, Is.EqualTo(0.754f).Within(0.0001f));

            hud.SetHealth(150f, 100f);

            Assert.That(healthText.text, Is.EqualTo("100 / 100"));
            Assert.That(healthFill.fillAmount, Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(healthTextObject);
            Object.DestroyImmediate(healthFillObject);
            Object.DestroyImmediate(attackTextObject);
        }
    }

    [Test]
    public void CanvasUpdates_UseConfiguredPlayerStatusHud()
    {
        GameObject canvasObject = new("Canvas");
        GameObject hudObject = new("PlayerStatusHUD");
        GameObject healthTextObject = new("HealthValue", typeof(RectTransform));
        GameObject healthFillObject = new("HealthFill", typeof(RectTransform));
        GameObject attackTextObject = new("AttackValue", typeof(RectTransform));

        try
        {
            TextMeshProUGUI healthText = healthTextObject.AddComponent<TextMeshProUGUI>();
            Image healthFill = healthFillObject.AddComponent<Image>();
            TextMeshProUGUI attackText = attackTextObject.AddComponent<TextMeshProUGUI>();
            PlayerStatusHud hud = hudObject.AddComponent<PlayerStatusHud>();
            hud.Configure(healthText, healthFill, attackText);

            CanvasScript canvas = canvasObject.AddComponent<CanvasScript>();
            canvas.ConfigurePlayerStatusHud(hud);
            canvas.UpdatePlayerHealthStatus(82f, 120f);
            canvas.UpdateAttackDebugText(57.6f);

            Assert.That(canvas.HasPlayerStatusHud, Is.True);
            Assert.That(healthText.text, Is.EqualTo("82 / 120"));
            Assert.That(healthFill.fillAmount, Is.EqualTo(82f / 120f).Within(0.0001f));
            Assert.That(attackText.text, Is.EqualTo("58"));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(healthTextObject);
            Object.DestroyImmediate(healthFillObject);
            Object.DestroyImmediate(attackTextObject);
        }
    }

    [Test]
    public void PlayerChildCanvas_RemainsVisibleForUnrelatedHud()
    {
        GameObject canvasObject = new("Canvas");
        GameObject playerObject = new("Player");
        GameObject childCanvasObject = new("Player World Canvas");
        GameObject unrelatedHudObject = new("Unrelated HUD");

        try
        {
            CanvasScript canvas = canvasObject.AddComponent<CanvasScript>();
            childCanvasObject.transform.SetParent(playerObject.transform, false);
            childCanvasObject.AddComponent<Canvas>();
            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            unrelatedHudObject.AddComponent<PlayerStatusHud>();

            player.EnsurePlayerChildCanvasVisible();

            Assert.That(canvas.HasPlayerStatusHud, Is.False);
            Assert.That(childCanvasObject.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(unrelatedHudObject);
        }
    }

    [Test]
    public void LegacyAttackText_UpdatesWhenHudIsNotConfigured()
    {
        GameObject canvasObject = new("Canvas");
        GameObject attackObject = new("ATT", typeof(RectTransform));
        GameObject partialHudObject = new("PlayerStatusHUD");

        try
        {
            attackObject.transform.SetParent(canvasObject.transform, false);
            partialHudObject.transform.SetParent(canvasObject.transform, false);
            partialHudObject.AddComponent<PlayerStatusHud>();
            TextMeshProUGUI attackText = attackObject.AddComponent<TextMeshProUGUI>();
            CanvasScript canvas = canvasObject.AddComponent<CanvasScript>();

            canvas.UpdateAttackDebugText(57.6f);

            Assert.That(attackText.text, Is.EqualTo("ATT : 58"));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void ConfiguredHud_HidesPlayerChildCanvas()
    {
        GameObject canvasObject = new("Canvas");
        GameObject playerObject = new("Player");
        GameObject childCanvasObject = new("Player World Canvas");
        GameObject hudObject = new("PlayerStatusHUD");
        GameObject healthTextObject = new("HealthValue", typeof(RectTransform));
        GameObject healthFillObject = new("HealthFill", typeof(RectTransform));
        GameObject attackTextObject = new("AttackValue", typeof(RectTransform));

        try
        {
            CanvasScript canvas = canvasObject.AddComponent<CanvasScript>();
            childCanvasObject.transform.SetParent(playerObject.transform, false);
            childCanvasObject.AddComponent<Canvas>();
            PlayerScript player = playerObject.AddComponent<PlayerScript>();

            PlayerStatusHud hud = hudObject.AddComponent<PlayerStatusHud>();
            hud.Configure(
                healthTextObject.AddComponent<TextMeshProUGUI>(),
                healthFillObject.AddComponent<Image>(),
                attackTextObject.AddComponent<TextMeshProUGUI>());

            canvas.ConfigurePlayerStatusHud(hud, player);

            Assert.That(hud.gameObject.activeSelf, Is.False);
            Assert.That(childCanvasObject.activeSelf, Is.False);

            childCanvasObject.SetActive(true);
            player.EnsurePlayerChildCanvasVisible();
            Assert.That(childCanvasObject.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(healthTextObject);
            Object.DestroyImmediate(healthFillObject);
            Object.DestroyImmediate(attackTextObject);
        }
    }

    [Test]
    public void HealthStatus_UpdatesWithoutLegacyHealthWidgets()
    {
        GameObject canvasObject = new("Canvas");
        GameObject playerObject = new("Player");
        GameObject hudObject = new("PlayerStatusHUD");
        GameObject healthTextObject = new("HealthValue", typeof(RectTransform));
        GameObject healthFillObject = new("HealthFill", typeof(RectTransform));
        GameObject attackTextObject = new("AttackValue", typeof(RectTransform));

        try
        {
            CanvasScript canvas = canvasObject.AddComponent<CanvasScript>();
            PlayerStatusHud hud = hudObject.AddComponent<PlayerStatusHud>();
            TextMeshProUGUI healthText = healthTextObject.AddComponent<TextMeshProUGUI>();
            Image healthFill = healthFillObject.AddComponent<Image>();
            hud.Configure(
                healthText,
                healthFill,
                attackTextObject.AddComponent<TextMeshProUGUI>());
            canvas.ConfigurePlayerStatusHud(hud);

            PlayerScript player = playerObject.AddComponent<PlayerScript>();
            player.currentHealth = 100f;
            player.ApplyHarnessHealthDelta(-25f);
            Assert.That(healthText.text, Is.EqualTo("75 / 100"));
            Assert.That(healthFill.fillAmount, Is.EqualTo(0.75f).Within(0.0001f));

            player.ApplyHarnessHealthDelta(-5f);
            Assert.That(healthText.text, Is.EqualTo("70 / 100"));
            Assert.That(healthFill.fillAmount, Is.EqualTo(0.7f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(healthTextObject);
            Object.DestroyImmediate(healthFillObject);
            Object.DestroyImmediate(attackTextObject);
        }
    }

    [Test]
    public void Canvas_RebindsAfterConfiguredHudIsDestroyed()
    {
        GameObject canvasObject = new("Canvas");
        GameObject firstHudObject = new("First HUD");
        GameObject replacementHudObject = new("Replacement HUD");
        GameObject healthTextObject = new("HealthValue", typeof(RectTransform));
        GameObject healthFillObject = new("HealthFill", typeof(RectTransform));
        GameObject attackTextObject = new("AttackValue", typeof(RectTransform));

        try
        {
            CanvasScript canvas = canvasObject.AddComponent<CanvasScript>();
            firstHudObject.transform.SetParent(canvasObject.transform, false);
            PlayerStatusHud firstHud = firstHudObject.AddComponent<PlayerStatusHud>();
            canvas.ConfigurePlayerStatusHud(firstHud);
            Object.DestroyImmediate(firstHudObject);

            replacementHudObject.transform.SetParent(canvasObject.transform, false);
            PlayerStatusHud replacementHud = replacementHudObject.AddComponent<PlayerStatusHud>();
            TextMeshProUGUI attackText = attackTextObject.AddComponent<TextMeshProUGUI>();
            replacementHud.Configure(
                healthTextObject.AddComponent<TextMeshProUGUI>(),
                healthFillObject.AddComponent<Image>(),
                attackText);

            canvas.UpdateAttackDebugText(42f);

            Assert.That(canvas.HasPlayerStatusHud, Is.True);
            Assert.That(attackText.text, Is.EqualTo("42"));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(replacementHudObject);
            Object.DestroyImmediate(healthTextObject);
            Object.DestroyImmediate(healthFillObject);
            Object.DestroyImmediate(attackTextObject);
        }
    }
}
