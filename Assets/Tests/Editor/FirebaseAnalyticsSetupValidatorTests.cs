#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FirebaseAnalyticsSetupValidatorTests
{
    [Test]
    public void Manifest_PinsFirebaseAndDependencyManagerPackages()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string manifest = File.ReadAllText(
            Path.Combine(projectRoot, "Packages", "manifest.json"));

        Assert.That(
            FirebaseAnalyticsSetupValidator.HasPinnedFirebasePackages(manifest),
            Is.True);
    }

    [TestCase("com.google.external-dependency-manager-1.2.186.tgz")]
    [TestCase("com.google.firebase.app-13.14.0.tgz")]
    [TestCase("com.google.firebase.analytics-13.14.0.tgz")]
    public void ManifestPinning_RejectsEachMissingPackage(string archiveName)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string manifest = File.ReadAllText(
            Path.Combine(projectRoot, "Packages", "manifest.json"));

        Assert.That(
            FirebaseAnalyticsSetupValidator.HasPinnedFirebasePackages(
                manifest.Replace(archiveName, "missing-package.tgz")),
            Is.False);
    }

    [Test]
    public void PinnedFirebasePackageArchives_MatchDocumentedHashes()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);

        Assert.That(
            FirebaseAnalyticsSetupValidator.PackageArchivesMatchExpectedHashes(
                projectRoot,
                out string detail),
            Is.True,
            detail);
    }

    [Test]
    public void PinnedFirebasePackageArchives_RejectMissingArchive()
    {
        string temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "FirebaseAnalyticsSetupValidatorTests",
            System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryRoot);
        try
        {
            Assert.That(
                FirebaseAnalyticsSetupValidator.PackageArchivesMatchExpectedHashes(
                    temporaryRoot,
                    out string detail),
                Is.False);
            Assert.That(detail, Does.Contain("archive is missing"));
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void AndroidClientParser_RequiresExactStructuredClient()
    {
        const string config =
            "{\"project_info\":{\"project_id\":\"safe-project\"}," +
            "\"client\":[" +
            "{\"client_info\":{\"mobilesdk_app_id\":\"other-app\"," +
            "\"android_client_info\":{\"package_name\":\"com.example.other\"}}}," +
            "{\"client_info\":{\"mobilesdk_app_id\":\"target-app\"," +
            "\"android_client_info\":{\"package_name\":" +
            "\"com.mzkoreagames.tralaleroshooter\"}}}]}";

        Assert.That(
            FirebaseAnalyticsSetupValidator.TryParseAndroidClient(
                config,
                "com.mzkoreagames.tralaleroshooter",
                out string projectId,
                out string mobileSdkAppId),
            Is.True);
        Assert.That(projectId, Is.EqualTo("safe-project"));
        Assert.That(mobileSdkAppId, Is.EqualTo("target-app"));
        Assert.That(
            FirebaseAnalyticsSetupValidator.ContainsAndroidPackage(
                config,
                "com.mzkoreagames.tralaleroshooter.debug"),
            Is.False);
    }

    [TestCase("{not-json")]
    [TestCase("{\"package_name\":\"com.mzkoreagames.tralaleroshooter\"}")]
    [TestCase("{\"project_info\":{\"project_id\":\"x\"},\"client\":[]}")]
    [TestCase(
        "{\"project_info\":{\"project_id\":\"x\"},\"client\":[" +
        "{\"client_info\":{\"android_client_info\":{\"package_name\":" +
        "\"com.mzkoreagames.tralaleroshooter\"}}}]}")]
    public void AndroidClientParser_RejectsMalformedOrDecoyConfigs(string config)
    {
        Assert.That(
            FirebaseAnalyticsSetupValidator.TryParseAndroidClient(
                config,
                "com.mzkoreagames.tralaleroshooter",
                out _,
                out _),
            Is.False);
    }

    [Test]
    public void AndroidClientParser_RejectsDuplicateMatchingClients()
    {
        const string config =
            "{\"project_info\":{\"project_id\":\"project\"},\"client\":[" +
            "{\"client_info\":{\"mobilesdk_app_id\":\"one\"," +
            "\"android_client_info\":{\"package_name\":\"same\"}}}," +
            "{\"client_info\":{\"mobilesdk_app_id\":\"two\"," +
            "\"android_client_info\":{\"package_name\":\"same\"}}}]}";

        Assert.That(
            FirebaseAnalyticsSetupValidator.TryParseAndroidClient(
                config,
                "same",
                out _,
                out _),
            Is.False);
    }

    [Test]
    public void DestinationPin_RequiresExactProjectAppAndPackage()
    {
        const string pin =
            "{\"schemaVersion\":1,\"applicationId\":\"package\"," +
            "\"projectId\":\"project\",\"mobileSdkAppId\":\"app\"}";

        Assert.That(
            FirebaseAnalyticsSetupValidator.DestinationMatches(
                pin,
                "package",
                "project",
                "app"),
            Is.True);
        Assert.That(
            FirebaseAnalyticsSetupValidator.DestinationMatches(
                pin,
                "package",
                "wrong-project",
                "app"),
            Is.False);
        Assert.That(
            FirebaseAnalyticsSetupValidator.DestinationMatches(
                pin,
                "wrong-package",
                "project",
                "app"),
            Is.False);
        Assert.That(
            FirebaseAnalyticsSetupValidator.DestinationMatches(
                pin,
                "package",
                "project",
                "wrong-app"),
            Is.False);
        Assert.That(
            FirebaseAnalyticsSetupValidator.DestinationMatches(
                "{not-json",
                "package",
                "project",
                "app"),
            Is.False);
    }

    [Test]
    public void AndroidManifest_DisablesCollectionAndAdvertisingSignalsByDefault()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string manifest = File.ReadAllText(
            Path.Combine(
                projectRoot,
                FirebaseAnalyticsSetupValidator.AndroidManifestAssetPath));

        Assert.That(
            FirebaseAnalyticsSetupValidator.HasPrivacySafeAndroidManifest(manifest),
            Is.True);
    }

    [TestCase("firebase_analytics_collection_enabled")]
    [TestCase("google_analytics_adid_collection_enabled")]
    [TestCase("google_analytics_default_allow_ad_personalization_signals")]
    [TestCase("com.google.android.gms.permission.AD_ID")]
    [TestCase("com.unity3d.player.UnityPlayerActivity")]
    public void AndroidManifest_RejectsEachMissingPrivacyOrEntryRequirement(
        string requiredValue)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string manifest = File.ReadAllText(
            Path.Combine(
                projectRoot,
                FirebaseAnalyticsSetupValidator.AndroidManifestAssetPath));

        Assert.That(
            FirebaseAnalyticsSetupValidator.HasPrivacySafeAndroidManifest(
                manifest.Replace(requiredValue, $"missing.{requiredValue}")),
            Is.False);
    }

    [Test]
    public void AndroidTemplates_ArePresentAndEnabled()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);

        Assert.That(
            FirebaseAnalyticsSetupValidator.HasRequiredAndroidTemplates(
                projectRoot),
            Is.True);
    }

    [TestCase("ProjectSettings/ProjectSettings.asset")]
    [TestCase("Assets/Plugins/Android/AndroidManifest.xml")]
    [TestCase("Assets/Plugins/Android/mainTemplate.gradle")]
    [TestCase("Assets/Plugins/Android/gradleTemplate.properties")]
    [TestCase("Assets/Plugins/Android/settingsTemplate.gradle")]
    public void AndroidTemplates_RejectEachMissingRequiredFile(
        string missingRelativePath)
    {
        string temporaryRoot = CreateValidAndroidTemplateFixture();
        try
        {
            File.Delete(GetFixturePath(
                temporaryRoot,
                missingRelativePath));

            Assert.That(
                FirebaseAnalyticsSetupValidator.HasRequiredAndroidTemplates(
                    temporaryRoot),
                Is.False);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [TestCase("useCustomMainManifest: 1")]
    [TestCase("useCustomMainGradleTemplate: 1")]
    [TestCase("useCustomGradlePropertiesTemplate: 1")]
    [TestCase("useCustomGradleSettingsTemplate: 1")]
    public void AndroidTemplates_RejectEachDisabledProjectSetting(
        string requiredSetting)
    {
        string temporaryRoot = CreateValidAndroidTemplateFixture();
        try
        {
            string settingsPath = GetFixturePath(
                temporaryRoot,
                "ProjectSettings/ProjectSettings.asset");
            string settings = File.ReadAllText(settingsPath);
            File.WriteAllText(
                settingsPath,
                settings.Replace(requiredSetting, requiredSetting.Replace(": 1", ": 0")));

            Assert.That(
                FirebaseAnalyticsSetupValidator.HasRequiredAndroidTemplates(
                    temporaryRoot),
                Is.False);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void AndroidDependencies_AreResolvedForPinnedFirebaseSdk()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);

        Assert.That(
            FirebaseAnalyticsSetupValidator.HasResolvedAndroidDependencies(
                projectRoot),
            Is.True);
    }

    [TestCase("Assets/Plugins/Android/mainTemplate.gradle")]
    [TestCase("Assets/Plugins/Android/settingsTemplate.gradle")]
    [TestCase("ProjectSettings/GvhProjectSettings.xml")]
    [TestCase(
        "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/firebase/" +
        "firebase-analytics-unity/13.14.0/" +
        "firebase-analytics-unity-13.14.0.aar")]
    [TestCase(
        "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/firebase/" +
        "firebase-analytics-unity/13.14.0/" +
        "firebase-analytics-unity-13.14.0.pom")]
    [TestCase(
        "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/firebase/" +
        "firebase-app-unity/13.14.0/firebase-app-unity-13.14.0.aar")]
    [TestCase(
        "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/firebase/" +
        "firebase-app-unity/13.14.0/firebase-app-unity-13.14.0.pom")]
    public void AndroidDependencies_RejectEachMissingResolverOutput(
        string missingRelativePath)
    {
        string temporaryRoot = CreateResolvedAndroidDependencyFixture();
        try
        {
            File.Delete(GetFixturePath(
                temporaryRoot,
                missingRelativePath));

            Assert.That(
                FirebaseAnalyticsSetupValidator.HasResolvedAndroidDependencies(
                    temporaryRoot,
                    verifyArtifactHashes: false),
                Is.False);
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void AndroidDependencies_RejectEmptyOrLfsPointerArtifacts()
    {
        string temporaryRoot = CreateResolvedAndroidDependencyFixture();
        try
        {
            Assert.That(
                FirebaseAnalyticsSetupValidator.HasResolvedAndroidDependencies(
                    temporaryRoot,
                    verifyArtifactHashes: false),
                Is.True,
                "The fixture must be structurally complete.");
            Assert.That(
                FirebaseAnalyticsSetupValidator.HasResolvedAndroidDependencies(
                    temporaryRoot),
                Is.False,
                "Empty placeholder artifacts must not satisfy pinned hashes.");

            string appAarPath = GetFixturePath(
                temporaryRoot,
                "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/" +
                "firebase/firebase-app-unity/13.14.0/" +
                "firebase-app-unity-13.14.0.aar");
            File.WriteAllText(
                appAarPath,
                "version https://git-lfs.github.com/spec/v1\n" +
                "oid sha256:000000000000000000000000000000000000000000000000" +
                "0000000000000000\nsize 22839405\n");

            Assert.That(
                FirebaseAnalyticsSetupValidator.HasResolvedAndroidDependencies(
                    temporaryRoot),
                Is.False,
                "An unsmudged Git LFS pointer must not pass as an AAR.");
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Test]
    public void BuildValidation_IsRequiredOnlyForAndroid()
    {
        Assert.That(
            FirebaseAnalyticsSetupValidator.RequiresBuildValidation(
                BuildTarget.Android),
            Is.True);
        Assert.That(
            FirebaseAnalyticsSetupValidator.RequiresBuildValidation(
                BuildTarget.StandaloneWindows64),
            Is.False);
    }

    [Test]
    public void FirebaseSetupPaths_AreStable()
    {
        Assert.That(
            FirebaseAnalyticsSetupValidator.FirebaseSetupDocumentPath,
            Is.EqualTo("docs/firebase-analytics-bigquery.md"));
        Assert.That(
            FirebaseAnalyticsSetupValidator.DestinationPinProjectPath,
            Is.EqualTo(
                "ProjectSettings/FirebaseAnalyticsDestination.json"));
    }

    private static string CreateValidAndroidTemplateFixture()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "FirebaseAnalyticsSetupValidatorTests",
            System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(GetFixturePath(root, "ProjectSettings"));
        Directory.CreateDirectory(GetFixturePath(
            root,
            "Assets/Plugins/Android"));
        File.WriteAllText(
            GetFixturePath(root, "ProjectSettings/ProjectSettings.asset"),
            "useCustomMainManifest: 1\n" +
            "useCustomMainGradleTemplate: 1\n" +
            "useCustomGradlePropertiesTemplate: 1\n" +
            "useCustomGradleSettingsTemplate: 1\n");
        File.WriteAllText(
            GetFixturePath(root, "Assets/Plugins/Android/AndroidManifest.xml"),
            "<manifest />");
        File.WriteAllText(
            GetFixturePath(root, "Assets/Plugins/Android/mainTemplate.gradle"),
            string.Empty);
        File.WriteAllText(
            GetFixturePath(
                root,
                "Assets/Plugins/Android/gradleTemplate.properties"),
            string.Empty);
        File.WriteAllText(
            GetFixturePath(
                root,
                "Assets/Plugins/Android/settingsTemplate.gradle"),
            string.Empty);
        return root;
    }

    private static string CreateResolvedAndroidDependencyFixture()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "FirebaseAnalyticsSetupValidatorTests",
            System.Guid.NewGuid().ToString("N"));
        string mainGradlePath = GetFixturePath(
            root,
            "Assets/Plugins/Android/mainTemplate.gradle");
        string settingsGradlePath = GetFixturePath(
            root,
            "Assets/Plugins/Android/settingsTemplate.gradle");
        string resolverSettingsPath = GetFixturePath(
            root,
            "ProjectSettings/GvhProjectSettings.xml");
        string analyticsAarPath = GetFixturePath(
            root,
            "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/" +
            "firebase/firebase-analytics-unity/13.14.0/" +
            "firebase-analytics-unity-13.14.0.aar");
        string appAarPath = GetFixturePath(
            root,
            "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/" +
            "firebase/firebase-app-unity/13.14.0/" +
            "firebase-app-unity-13.14.0.aar");
        string analyticsPomPath = GetFixturePath(
            root,
            "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/" +
            "firebase/firebase-analytics-unity/13.14.0/" +
            "firebase-analytics-unity-13.14.0.pom");
        string appPomPath = GetFixturePath(
            root,
            "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/" +
            "firebase/firebase-app-unity/13.14.0/" +
            "firebase-app-unity-13.14.0.pom");

        Directory.CreateDirectory(Path.GetDirectoryName(mainGradlePath));
        Directory.CreateDirectory(Path.GetDirectoryName(resolverSettingsPath));
        Directory.CreateDirectory(Path.GetDirectoryName(analyticsAarPath));
        Directory.CreateDirectory(Path.GetDirectoryName(appAarPath));
        File.WriteAllText(
            mainGradlePath,
            "com.google.android.gms:play-services-base:18.10.0\n" +
            "com.google.firebase:firebase-analytics:23.2.0\n" +
            "com.google.firebase:firebase-analytics-unity:13.14.0\n" +
            "com.google.firebase:firebase-app-unity:13.14.0\n" +
            "com.google.firebase:firebase-common:22.1.0\n");
        File.WriteAllText(
            settingsGradlePath,
            "Assets/GeneratedLocalRepo/Firebase/m2repository");
        File.WriteAllText(
            resolverSettingsPath,
            "<projectSetting " +
            "name=\"GooglePlayServices.PatchSettingsTemplateGradle\" " +
            "value=\"True\" />");
        File.WriteAllText(analyticsAarPath, string.Empty);
        File.WriteAllText(appAarPath, string.Empty);
        File.WriteAllText(analyticsPomPath, string.Empty);
        File.WriteAllText(appPomPath, string.Empty);
        return root;
    }

    private static string GetFixturePath(
        string root,
        string relativePath)
    {
        return Path.Combine(
            root,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
#endif
