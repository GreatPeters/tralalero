#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FirebaseAnalyticsSetupValidator
{
    public const string AndroidConfigAssetPath = "Assets/google-services.json";
    public const string AndroidManifestAssetPath =
        "Assets/Plugins/Android/AndroidManifest.xml";
    public const string AndroidMainGradleAssetPath =
        "Assets/Plugins/Android/mainTemplate.gradle";
    public const string AndroidGradlePropertiesAssetPath =
        "Assets/Plugins/Android/gradleTemplate.properties";
    public const string AndroidGradleSettingsAssetPath =
        "Assets/Plugins/Android/settingsTemplate.gradle";
    public const string DestinationPinProjectPath =
        "ProjectSettings/FirebaseAnalyticsDestination.json";
    public const string FirebaseSetupDocumentPath =
        "docs/firebase-analytics-bigquery.md";
    public const string FirebaseAppPackageVersion = "13.14.0";
    public const string ExternalDependencyManagerVersion = "1.2.186";

    public const string ExternalDependencyManagerSha256 =
        "46684B475C2A39844C44C07945B5AEE02895C41A9BFF97D5CD4B5D9E85E021D8";
    public const string FirebaseAppSha256 =
        "BB54CC7AAB6DEC3430BC2F628E9A500D44A7E5BB05727D0372D30D6B68438FCB";
    public const string FirebaseAnalyticsSha256 =
        "ABB780995D77A98ACD3362201E3B849651717EADF726DD22D277CE98638C3A3B";

    private static readonly ArchivePin[] ArchivePins =
    {
        new ArchivePin(
            $"com.google.external-dependency-manager-{ExternalDependencyManagerVersion}.tgz",
            ExternalDependencyManagerSha256),
        new ArchivePin(
            $"com.google.firebase.app-{FirebaseAppPackageVersion}.tgz",
            FirebaseAppSha256),
        new ArchivePin(
            $"com.google.firebase.analytics-{FirebaseAppPackageVersion}.tgz",
            FirebaseAnalyticsSha256)
    };

    private static readonly string[] ResolvedAndroidDependencyMarkers =
    {
        "com.google.android.gms:play-services-base:18.10.0",
        "com.google.firebase:firebase-analytics:23.2.0",
        "com.google.firebase:firebase-analytics-unity:13.14.0",
        "com.google.firebase:firebase-app-unity:13.14.0",
        "com.google.firebase:firebase-common:22.1.0"
    };

    private static readonly ArtifactPin[] ResolvedFirebaseUnityArtifacts =
    {
        new ArtifactPin(
            "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/" +
            "firebase/firebase-analytics-unity/13.14.0/" +
            "firebase-analytics-unity-13.14.0.aar",
            "F4F0003E0B99475EE7698138F7E588F5077A322A28914457A5D0F5A6A025A974"),
        new ArtifactPin(
            "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/" +
            "firebase/firebase-analytics-unity/13.14.0/" +
            "firebase-analytics-unity-13.14.0.pom",
            "BB58F246A26AB99A3A8BB6DCAFBA98F09564E565B9F8CCF9E6BDD39F1CA95523"),
        new ArtifactPin(
            "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/" +
            "firebase/firebase-app-unity/13.14.0/" +
            "firebase-app-unity-13.14.0.aar",
            "F961945DF4743C027B80A997E39D32844CBE4C0FE2D16BC1316A9F701A1EFCC5"),
        new ArtifactPin(
            "Assets/GeneratedLocalRepo/Firebase/m2repository/com/google/" +
            "firebase/firebase-app-unity/13.14.0/" +
            "firebase-app-unity-13.14.0.pom",
            "98ED51F9988575973993567B58EE4DDF47946A30FC21E47EB9CB459806C53BA6")
    };

    [MenuItem("Tools/Analytics/Firebase 설정 검증", false, 2200)]
    public static void ValidateFromMenu()
    {
        if (TryValidateAndroidSetup(out string detail))
            Debug.Log($"[Analytics] {detail}");
        else
            Debug.LogError($"[Analytics] {detail}");
    }

    [MenuItem("Tools/Analytics/Firebase 대상 고정", false, 2201)]
    public static void PinDestinationFromCurrentConfig()
    {
        string applicationId =
            PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        string configPath = GetProjectAbsolutePath(AndroidConfigAssetPath);
        if (!File.Exists(configPath))
        {
            Debug.LogError(
                $"[Analytics] Firebase Android config is missing: " +
                $"{AndroidConfigAssetPath}");
            return;
        }

        string configJson = File.ReadAllText(configPath, Encoding.UTF8);
        if (!TryParseAndroidClient(
                configJson,
                applicationId,
                out string projectId,
                out string mobileSdkAppId))
        {
            Debug.LogError(
                $"[Analytics] The config does not contain exactly one valid " +
                $"Android client for '{applicationId}'.");
            return;
        }

        string pinPath = GetProjectAbsolutePath(DestinationPinProjectPath);
        if (File.Exists(pinPath) &&
            !EditorUtility.DisplayDialog(
                "Firebase 대상 다시 고정",
                "기존 Firebase 대상 고정을 현재 google-services.json 값으로 " +
                "바꾸시겠습니까?",
                "바꾸기",
                "취소"))
        {
            return;
        }

        var pin = new FirebaseDestinationPin
        {
            schemaVersion = 1,
            applicationId = applicationId,
            projectId = projectId,
            mobileSdkAppId = mobileSdkAppId
        };
        File.WriteAllText(
            pinPath,
            JsonUtility.ToJson(pin, true),
            new UTF8Encoding(false));
        Debug.Log(
            $"[Analytics] Firebase destination pinned: " +
            $"{projectId} / {mobileSdkAppId}");
    }

    [MenuItem("Tools/Analytics/Firebase 연결 문서 열기", false, 2202)]
    public static void OpenSetupDocument()
    {
        string absolutePath = GetProjectAbsolutePath(FirebaseSetupDocumentPath);
        if (!File.Exists(absolutePath))
        {
            Debug.LogError(
                $"[Analytics] Setup document is missing: {FirebaseSetupDocumentPath}");
            return;
        }

        EditorUtility.OpenWithDefaultApp(absolutePath);
    }

    public static bool TryValidateAndroidSetup(out string detail)
    {
        string projectRoot = GetProjectAbsolutePath(".");
        string manifestPath = Path.Combine(projectRoot, "Packages", "manifest.json");
        if (!File.Exists(manifestPath))
        {
            detail = "Packages/manifest.json is missing.";
            return false;
        }

        string packageManifest = File.ReadAllText(manifestPath, Encoding.UTF8);
        if (!HasPinnedFirebasePackages(packageManifest))
        {
            detail =
                $"Firebase App/Analytics {FirebaseAppPackageVersion} and " +
                $"External Dependency Manager {ExternalDependencyManagerVersion} " +
                "are not pinned to the expected local archives.";
            return false;
        }

        if (!PackageArchivesMatchExpectedHashes(projectRoot, out detail))
            return false;

        if (!HasRequiredAndroidTemplates(projectRoot))
        {
            detail =
                "Android custom manifest or required Gradle templates are " +
                "missing or disabled.";
            return false;
        }

        if (!HasResolvedAndroidDependencies(projectRoot))
        {
            detail =
                "Firebase Android dependencies have not been resolved into " +
                "the Gradle templates and local Maven repository. Run External " +
                "Dependency Manager > Android Resolver > Force Resolve.";
            return false;
        }

        string androidManifestPath =
            GetProjectAbsolutePath(AndroidManifestAssetPath);
        if (!File.Exists(androidManifestPath) ||
            !HasPrivacySafeAndroidManifest(
                File.ReadAllText(androidManifestPath, Encoding.UTF8)))
        {
            detail =
                $"{AndroidManifestAssetPath} must disable collection by default, " +
                "Advertising ID collection, and ad-personalization signals.";
            return false;
        }

        string configPath = GetProjectAbsolutePath(AndroidConfigAssetPath);
        if (!File.Exists(configPath))
        {
            detail =
                $"Firebase Android config is missing: {AndroidConfigAssetPath}. " +
                "Register the Android app in Firebase and place " +
                "google-services.json there.";
            return false;
        }

        string applicationId =
            PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        string configJson = File.ReadAllText(configPath, Encoding.UTF8);
        if (!TryParseAndroidClient(
                configJson,
                applicationId,
                out string projectId,
                out string mobileSdkAppId))
        {
            detail =
                $"The Firebase config does not contain exactly one structurally " +
                $"valid Android client for package '{applicationId}'.";
            return false;
        }

        string destinationPinPath =
            GetProjectAbsolutePath(DestinationPinProjectPath);
        if (!File.Exists(destinationPinPath))
        {
            detail =
                $"Firebase destination is not pinned. Review the config, then run " +
                "Tools/Analytics/Firebase 대상 고정.";
            return false;
        }

        string destinationPinJson =
            File.ReadAllText(destinationPinPath, Encoding.UTF8);
        if (!DestinationMatches(
                destinationPinJson,
                applicationId,
                projectId,
                mobileSdkAppId))
        {
            detail =
                "google-services.json does not match the versioned Firebase " +
                "destination pin. Review the project/app target and pin it again.";
            return false;
        }

        detail =
            $"Firebase Analytics SDK {FirebaseAppPackageVersion}, Android package " +
            $"'{applicationId}', and pinned destination '{projectId}' are ready.";
        return true;
    }

    public static bool HasPinnedFirebasePackages(string manifestJson)
    {
        if (string.IsNullOrWhiteSpace(manifestJson))
            return false;

        return manifestJson.Contains(
                   $"com.google.external-dependency-manager-" +
                   $"{ExternalDependencyManagerVersion}.tgz",
                   StringComparison.Ordinal) &&
               manifestJson.Contains(
                   $"com.google.firebase.app-{FirebaseAppPackageVersion}.tgz",
                   StringComparison.Ordinal) &&
               manifestJson.Contains(
                   $"com.google.firebase.analytics-{FirebaseAppPackageVersion}.tgz",
                   StringComparison.Ordinal);
    }

    public static bool PackageArchivesMatchExpectedHashes(
        string projectRoot,
        out string detail)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
        {
            detail = "Project root is required.";
            return false;
        }

        foreach (ArchivePin pin in ArchivePins)
        {
            string path = Path.Combine(projectRoot, "GooglePackages", pin.fileName);
            if (!File.Exists(path))
            {
                detail = $"Pinned Firebase package archive is missing: {pin.fileName}";
                return false;
            }

            string actualHash = ComputeSha256(path);
            if (!string.Equals(
                    actualHash,
                    pin.sha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                detail =
                    $"Pinned Firebase package archive hash mismatch: {pin.fileName}";
                return false;
            }
        }

        detail = "Pinned Firebase package archive hashes are valid.";
        return true;
    }

    public static bool ContainsAndroidPackage(
        string configJson,
        string applicationId)
    {
        return TryParseAndroidClient(
            configJson,
            applicationId,
            out _,
            out _);
    }

    public static bool TryParseAndroidClient(
        string configJson,
        string applicationId,
        out string projectId,
        out string mobileSdkAppId)
    {
        projectId = string.Empty;
        mobileSdkAppId = string.Empty;
        if (string.IsNullOrWhiteSpace(configJson) ||
            string.IsNullOrWhiteSpace(applicationId))
        {
            return false;
        }

        try
        {
            GoogleServicesConfig config =
                JsonUtility.FromJson<GoogleServicesConfig>(configJson);
            if (config?.project_info == null ||
                string.IsNullOrWhiteSpace(config.project_info.project_id) ||
                config.client == null)
            {
                return false;
            }

            GoogleServicesClient matchingClient = null;
            int matchCount = 0;
            foreach (GoogleServicesClient client in config.client)
            {
                if (!string.Equals(
                        client?.client_info?.android_client_info?.package_name,
                        applicationId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                matchingClient = client;
                matchCount++;
            }

            if (matchCount != 1 ||
                string.IsNullOrWhiteSpace(
                    matchingClient?.client_info?.mobilesdk_app_id))
            {
                return false;
            }

            projectId = config.project_info.project_id;
            mobileSdkAppId = matchingClient.client_info.mobilesdk_app_id;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool DestinationMatches(
        string pinJson,
        string applicationId,
        string projectId,
        string mobileSdkAppId)
    {
        if (string.IsNullOrWhiteSpace(pinJson))
            return false;

        try
        {
            FirebaseDestinationPin pin =
                JsonUtility.FromJson<FirebaseDestinationPin>(pinJson);
            return pin != null &&
                   pin.schemaVersion == 1 &&
                   string.Equals(
                       pin.applicationId,
                       applicationId,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       pin.projectId,
                       projectId,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       pin.mobileSdkAppId,
                       mobileSdkAppId,
                       StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool HasPrivacySafeAndroidManifest(string manifestXml)
    {
        if (string.IsNullOrWhiteSpace(manifestXml))
            return false;

        try
        {
            var document = new XmlDocument();
            document.LoadXml(manifestXml);
            var namespaces = new XmlNamespaceManager(document.NameTable);
            namespaces.AddNamespace(
                "android",
                "http://schemas.android.com/apk/res/android");
            namespaces.AddNamespace(
                "tools",
                "http://schemas.android.com/tools");

            return HasMetadata(
                       document,
                       namespaces,
                       "firebase_analytics_collection_enabled",
                       "false") &&
                   HasMetadata(
                       document,
                       namespaces,
                       "google_analytics_adid_collection_enabled",
                       "false") &&
                   HasMetadata(
                       document,
                       namespaces,
                       "google_analytics_default_allow_ad_personalization_signals",
                       "false") &&
                   document.SelectSingleNode(
                       "/manifest/uses-permission[" +
                       "@android:name='com.google.android.gms.permission.AD_ID' and " +
                       "@tools:node='remove']",
                       namespaces) != null &&
                   document.SelectSingleNode(
                       "/manifest/application/activity[" +
                       "@android:name='com.unity3d.player.UnityPlayerActivity']",
                       namespaces) != null;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    public static bool HasRequiredAndroidTemplates(string projectRoot)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
            return false;

        string projectSettingsPath =
            Path.Combine(projectRoot, "ProjectSettings", "ProjectSettings.asset");
        string androidPluginsPath =
            Path.Combine(projectRoot, "Assets", "Plugins", "Android");
        if (!File.Exists(projectSettingsPath) ||
            !File.Exists(Path.Combine(androidPluginsPath, "AndroidManifest.xml")) ||
            !File.Exists(Path.Combine(androidPluginsPath, "mainTemplate.gradle")) ||
            !File.Exists(Path.Combine(
                androidPluginsPath,
                "gradleTemplate.properties")) ||
            !File.Exists(Path.Combine(
                androidPluginsPath,
                "settingsTemplate.gradle")))
        {
            return false;
        }

        string projectSettings =
            File.ReadAllText(projectSettingsPath, Encoding.UTF8);
        return projectSettings.Contains(
                   "useCustomMainManifest: 1",
                   StringComparison.Ordinal) &&
               projectSettings.Contains(
                   "useCustomMainGradleTemplate: 1",
                   StringComparison.Ordinal) &&
               projectSettings.Contains(
                   "useCustomGradlePropertiesTemplate: 1",
                   StringComparison.Ordinal) &&
               projectSettings.Contains(
                   "useCustomGradleSettingsTemplate: 1",
                   StringComparison.Ordinal);
    }

    public static bool HasResolvedAndroidDependencies(string projectRoot)
    {
        return HasResolvedAndroidDependencies(
            projectRoot,
            verifyArtifactHashes: true);
    }

    internal static bool HasResolvedAndroidDependencies(
        string projectRoot,
        bool verifyArtifactHashes)
    {
        if (string.IsNullOrWhiteSpace(projectRoot))
            return false;

        string mainGradlePath = Path.Combine(
            projectRoot,
            AndroidMainGradleAssetPath.Replace(
                '/',
                Path.DirectorySeparatorChar));
        string settingsGradlePath = Path.Combine(
            projectRoot,
            AndroidGradleSettingsAssetPath.Replace(
                '/',
                Path.DirectorySeparatorChar));
        string resolverSettingsPath = Path.Combine(
            projectRoot,
            "ProjectSettings",
            "GvhProjectSettings.xml");
        if (!File.Exists(mainGradlePath) ||
            !File.Exists(settingsGradlePath) ||
            !File.Exists(resolverSettingsPath))
        {
            return false;
        }

        string mainGradle = File.ReadAllText(mainGradlePath, Encoding.UTF8);
        foreach (string marker in ResolvedAndroidDependencyMarkers)
        {
            if (!mainGradle.Contains(marker, StringComparison.Ordinal))
                return false;
        }

        string settingsGradle =
            File.ReadAllText(settingsGradlePath, Encoding.UTF8);
        if (!settingsGradle.Contains(
                "Assets/GeneratedLocalRepo/Firebase/m2repository",
                StringComparison.Ordinal))
        {
            return false;
        }

        string resolverSettings =
            File.ReadAllText(resolverSettingsPath, Encoding.UTF8);
        if (!resolverSettings.Contains(
                "GooglePlayServices.PatchSettingsTemplateGradle\" value=\"True",
                StringComparison.Ordinal))
        {
            return false;
        }

        foreach (ArtifactPin artifact in ResolvedFirebaseUnityArtifacts)
        {
            string artifactPath = Path.Combine(
                projectRoot,
                artifact.relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar));
            if (!File.Exists(artifactPath) ||
                (verifyArtifactHashes &&
                 !string.Equals(
                     ComputeSha256(artifactPath),
                     artifact.sha256,
                     StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    public static bool RequiresBuildValidation(BuildTarget buildTarget)
    {
        return buildTarget == BuildTarget.Android;
    }

    private static bool HasMetadata(
        XmlDocument document,
        XmlNamespaceManager namespaces,
        string name,
        string value)
    {
        return document.SelectSingleNode(
                   "/manifest/application/meta-data[" +
                   $"@android:name='{name}' and @android:value='{value}']",
                   namespaces) != null;
    }

    private static string ComputeSha256(string path)
    {
        using var sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(path);
        byte[] hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    private static string GetProjectAbsolutePath(string projectRelativePath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        if (string.IsNullOrEmpty(projectRoot))
        {
            throw new DirectoryNotFoundException(
                "Unity project root could not be resolved.");
        }

        return Path.GetFullPath(Path.Combine(
            projectRoot,
            projectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private readonly struct ArchivePin
    {
        public ArchivePin(string fileName, string sha256)
        {
            this.fileName = fileName;
            this.sha256 = sha256;
        }

        public readonly string fileName;
        public readonly string sha256;
    }

    private readonly struct ArtifactPin
    {
        public ArtifactPin(string relativePath, string sha256)
        {
            this.relativePath = relativePath;
            this.sha256 = sha256;
        }

        public readonly string relativePath;
        public readonly string sha256;
    }

    [Serializable]
    private sealed class GoogleServicesConfig
    {
        public GoogleProjectInfo project_info = new GoogleProjectInfo();
        public GoogleServicesClient[] client = Array.Empty<GoogleServicesClient>();
    }

    [Serializable]
    private sealed class GoogleProjectInfo
    {
        public string project_id = string.Empty;
    }

    [Serializable]
    private sealed class GoogleServicesClient
    {
        public GoogleClientInfo client_info = new GoogleClientInfo();
    }

    [Serializable]
    private sealed class GoogleClientInfo
    {
        public string mobilesdk_app_id = string.Empty;
        public GoogleAndroidClientInfo android_client_info =
            new GoogleAndroidClientInfo();
    }

    [Serializable]
    private sealed class GoogleAndroidClientInfo
    {
        public string package_name = string.Empty;
    }

    [Serializable]
    private sealed class FirebaseDestinationPin
    {
        public int schemaVersion;
        public string applicationId = string.Empty;
        public string projectId = string.Empty;
        public string mobileSdkAppId = string.Empty;
    }
}

public sealed class FirebaseAnalyticsBuildPreprocessor :
    IPreprocessBuildWithReport
{
    public int callbackOrder => -900;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report == null ||
            !FirebaseAnalyticsSetupValidator.RequiresBuildValidation(
                report.summary.platform))
        {
            return;
        }

        if (!FirebaseAnalyticsSetupValidator.TryValidateAndroidSetup(
                out string detail))
        {
            throw new BuildFailedException(
                $"Firebase Analytics build preparation failed: {detail}");
        }
    }
}
#endif
