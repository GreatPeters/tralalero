#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public enum GameDataRuntimeArchiveStatus
{
    Current = 0,
    MissingSource = 1,
    LegacyRawWorkbookPresent = 2,
    MissingArchive = 3,
    Stale = 4,
    Invalid = 5
}

public static class GameDataWorkbookEditor
{
    public const string RuntimeArchiveAssetPath =
        "Assets/ShooterSurvival/Resources/GameData/Data.bytes";
    public const string LegacySigningKeyAssetPath =
        "Assets/ShooterSurvival/GameData/Editor/GameDataSigningKey.json";
    public const string SigningKeyPathEnvironmentVariable =
        "TRALALERO_GAME_DATA_SIGNING_KEY_PATH";
    public const string LegacyStreamingAssetsPath =
        "Assets/StreamingAssets/Data.xlsx";

    [Serializable]
    private sealed class SigningKeyFile
    {
        public string modulus = string.Empty;
        public string exponent = string.Empty;
        public string d = string.Empty;
        public string p = string.Empty;
        public string q = string.Empty;
        public string dp = string.Empty;
        public string dq = string.Empty;
        public string inverseQ = string.Empty;
    }

    [MenuItem("Tools/Data/게임 데이터 Excel 열기", false, 2100)]
    public static void OpenSourceWorkbookMenu()
    {
        OpenSourceWorkbook();
    }

    [MenuItem("Tools/Data/게임 데이터 프로젝트에서 찾기", false, 2101)]
    public static void SelectSourceWorkbookMenu()
    {
        SelectSourceWorkbook();
    }

    [MenuItem("Tools/Data/런타임 보호 데이터 갱신", false, 2110)]
    public static void GenerateRuntimeArchiveMenu()
    {
        EnsureRuntimeArchiveCurrent(logResult: true);
    }

    [MenuItem("Tools/Data/런타임 보호 데이터 검증", false, 2111)]
    public static void ValidateRuntimeArchiveMenu()
    {
        GameDataRuntimeArchiveStatus status =
            GetRuntimeArchiveStatus(out string detail);
        if (status == GameDataRuntimeArchiveStatus.Current)
            Debug.Log($"[GameData] {detail}");
        else
            Debug.LogError($"[GameData] {detail}");
    }

    public static bool OpenSourceWorkbook()
    {
        UnityEngine.Object workbook =
            AssetDatabase.LoadMainAssetAtPath(GameDataWorkbook.EditorSourceAssetPath);
        if (workbook == null)
        {
            Debug.LogError(
                $"[GameData] Excel source not found: " +
                GameDataWorkbook.EditorSourceAssetPath);
            return false;
        }

        SelectSourceWorkbook();
        if (!AssetDatabase.OpenAsset(workbook))
            EditorUtility.OpenWithDefaultApp(
                GameDataWorkbook.GetEditorSourceAbsolutePath());

        return true;
    }

    public static bool SelectSourceWorkbook()
    {
        UnityEngine.Object workbook =
            AssetDatabase.LoadMainAssetAtPath(GameDataWorkbook.EditorSourceAssetPath);
        if (workbook == null)
        {
            Debug.LogError(
                $"[GameData] Excel source not found: " +
                GameDataWorkbook.EditorSourceAssetPath);
            return false;
        }

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = workbook;
        EditorGUIUtility.PingObject(workbook);
        return true;
    }

    internal static void ValidateSourceWorkbookOrThrow()
    {
        ValidateSourceLocationOrThrow();
        byte[] workbookBytes =
            ReadAllBytesShared(GameDataWorkbook.GetEditorSourceAbsolutePath());
        GameDataWorkbookSchema.Validate(workbookBytes);
    }

    public static bool EnsureRuntimeArchiveCurrent(bool logResult)
    {
        GameDataRuntimeArchiveStatus status =
            GetRuntimeArchiveStatus(out string detail);
        if (status == GameDataRuntimeArchiveStatus.Current)
        {
            if (logResult)
                Debug.Log($"[GameData] {detail}");
            return false;
        }

        ValidateSourceLocationOrThrow();

        byte[] workbookBytes =
            ReadAllBytesShared(GameDataWorkbook.GetEditorSourceAbsolutePath());
        GameDataWorkbookSchema.Validate(workbookBytes);

        RSAParameters privateSigningKey = LoadPrivateSigningKey();
        byte[] protectedBytes =
            GameDataArchive.Protect(workbookBytes, privateSigningKey);

        byte[] verifiedWorkbook = GameDataArchive.Unprotect(protectedBytes);
        if (!ByteArraysEqual(workbookBytes, verifiedWorkbook))
        {
            throw new InvalidDataException(
                "Generated game data archive did not round-trip correctly.");
        }

        byte[] latestWorkbookBytes =
            ReadAllBytesShared(GameDataWorkbook.GetEditorSourceAbsolutePath());
        if (!ByteArraysEqual(workbookBytes, latestWorkbookBytes))
        {
            throw new IOException(
                "The Excel source changed while runtime data was being generated. " +
                "Wait for Excel to finish saving, then try again.");
        }

        string archiveAbsolutePath =
            GetAbsoluteAssetPath(RuntimeArchiveAssetPath);
        string archiveDirectory = Path.GetDirectoryName(archiveAbsolutePath);
        if (string.IsNullOrEmpty(archiveDirectory))
            throw new DirectoryNotFoundException(RuntimeArchiveAssetPath);

        Directory.CreateDirectory(archiveDirectory);
        WriteAllBytesAtomically(archiveAbsolutePath, protectedBytes);
        AssetDatabase.ImportAsset(
            RuntimeArchiveAssetPath,
            ImportAssetOptions.ForceSynchronousImport |
            ImportAssetOptions.ForceUpdate);

        GameDataRuntimeArchiveStatus generatedStatus =
            GetRuntimeArchiveStatus(out string generatedDetail);
        if (generatedStatus != GameDataRuntimeArchiveStatus.Current)
            throw new InvalidDataException(generatedDetail);

        if (logResult)
            Debug.Log($"[GameData] {generatedDetail}");

        return true;
    }

    public static GameDataRuntimeArchiveStatus GetRuntimeArchiveStatus(
        out string detail)
    {
        if (!IsEditorOnlyAssetPath(GameDataWorkbook.EditorSourceAssetPath))
        {
            detail =
                "Excel source must stay inside an Editor folder so it is excluded from player builds.";
            return GameDataRuntimeArchiveStatus.Invalid;
        }

        string forbiddenSigningKeyPath = FindSigningKeyUnderAssets();
        if (!string.IsNullOrEmpty(forbiddenSigningKeyPath))
        {
            detail =
                "The private game-data signing key must not be stored under " +
                $"Assets: {forbiddenSigningKeyPath}";
            return GameDataRuntimeArchiveStatus.Invalid;
        }

        string sourceAbsolutePath =
            GameDataWorkbook.GetEditorSourceAbsolutePath();
        if (!File.Exists(sourceAbsolutePath))
        {
            detail =
                $"Excel source is missing: {GameDataWorkbook.EditorSourceAssetPath}";
            return GameDataRuntimeArchiveStatus.MissingSource;
        }

        if (File.Exists(GetAbsoluteAssetPath(LegacyStreamingAssetsPath)))
        {
            detail =
                $"Raw Excel must not remain in StreamingAssets: {LegacyStreamingAssetsPath}";
            return GameDataRuntimeArchiveStatus.LegacyRawWorkbookPresent;
        }

        string archiveAbsolutePath =
            GetAbsoluteAssetPath(RuntimeArchiveAssetPath);
        if (!File.Exists(archiveAbsolutePath))
        {
            detail =
                $"Protected runtime data is missing: {RuntimeArchiveAssetPath}";
            return GameDataRuntimeArchiveStatus.MissingArchive;
        }

        try
        {
            byte[] workbookBytes = ReadAllBytesShared(sourceAbsolutePath);
            GameDataWorkbookSchema.Validate(workbookBytes);

            byte[] protectedBytes = ReadAllBytesShared(archiveAbsolutePath);
            byte[] restoredWorkbook =
                GameDataArchive.Unprotect(protectedBytes);

            if (!ByteArraysEqual(workbookBytes, restoredWorkbook))
            {
                detail =
                    "Protected runtime data is stale. Regenerate it from the Excel source.";
                return GameDataRuntimeArchiveStatus.Stale;
            }

            detail =
                "Protected runtime data is valid and matches the Excel source.";
            return GameDataRuntimeArchiveStatus.Current;
        }
        catch (Exception exception) when (
            exception is GameDataIntegrityException ||
            exception is InvalidDataException ||
            exception is CryptographicException ||
            exception is ArgumentException ||
            exception is IOException)
        {
            detail =
                $"Protected runtime data is invalid: {exception.Message}";
            return GameDataRuntimeArchiveStatus.Invalid;
        }
    }

    public static void ValidateRuntimeArchiveOrThrow()
    {
        ValidateSourceLocationOrThrow();

        GameDataRuntimeArchiveStatus status =
            GetRuntimeArchiveStatus(out string detail);
        if (status != GameDataRuntimeArchiveStatus.Current)
            throw new InvalidDataException(detail);
    }

    public static bool IsEditorOnlyAssetPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return false;

        string normalized = assetPath.Replace('\\', '/');
        return normalized.IndexOf(
            "/Editor/",
            StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string GetSigningKeyAbsolutePath()
    {
        string configuredPath =
            Environment.GetEnvironmentVariable(
                SigningKeyPathEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            string expandedPath =
                Environment.ExpandEnvironmentVariables(configuredPath);
            if (!Path.IsPathRooted(expandedPath))
            {
                throw new InvalidDataException(
                    $"{SigningKeyPathEnvironmentVariable} must contain an " +
                    "absolute path outside the project.");
            }

            return Path.GetFullPath(expandedPath);
        }

        string localApplicationData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new DirectoryNotFoundException(
                "Local application-data folder could not be resolved. Set " +
                $"{SigningKeyPathEnvironmentVariable} to an external secret path.");
        }

        return Path.GetFullPath(Path.Combine(
            localApplicationData,
            "MZKoreaGames",
            "TralaleroShooter",
            "Secrets",
            "GameDataSigningKey.json"));
    }

    public static bool IsPathInsideProject(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
            return false;

        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        if (string.IsNullOrWhiteSpace(projectRoot))
            throw new DirectoryNotFoundException(
                "Unity project root could not be resolved.");

        string normalizedRoot =
            Path.GetFullPath(projectRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string normalizedPath =
            Path.GetFullPath(absolutePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return string.Equals(
                   normalizedPath,
                   normalizedRoot,
                   StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.StartsWith(
                   normalizedRoot + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase);
    }

    public static string FindSigningKeyUnderAssets()
    {
        foreach (string path in Directory.EnumerateFiles(
                     Application.dataPath,
                     "GameDataSigningKey.json",
                     SearchOption.AllDirectories))
        {
            return path;
        }

        return null;
    }

    private static void ValidateSourceLocationOrThrow()
    {
        if (!IsEditorOnlyAssetPath(GameDataWorkbook.EditorSourceAssetPath))
        {
            throw new InvalidDataException(
                "The Excel source is not inside an Editor-only folder.");
        }

        string sourceAbsolutePath =
            GameDataWorkbook.GetEditorSourceAbsolutePath();
        if (!File.Exists(sourceAbsolutePath))
        {
            throw new FileNotFoundException(
                "The Excel source workbook is missing.",
                sourceAbsolutePath);
        }

        string legacyAbsolutePath =
            GetAbsoluteAssetPath(LegacyStreamingAssetsPath);
        if (File.Exists(legacyAbsolutePath))
        {
            throw new InvalidDataException(
                $"Remove the raw workbook from '{LegacyStreamingAssetsPath}' " +
                "before building.");
        }

        string forbiddenSigningKeyPath = FindSigningKeyUnderAssets();
        if (!string.IsNullOrEmpty(forbiddenSigningKeyPath))
        {
            throw new InvalidDataException(
                "The private game-data signing key must not be stored under " +
                $"Assets: {forbiddenSigningKeyPath}");
        }
    }

    private static RSAParameters LoadPrivateSigningKey()
    {
        string keyAbsolutePath = GetSigningKeyAbsolutePath();
        if (IsPathInsideProject(keyAbsolutePath))
        {
            throw new InvalidDataException(
                "The private game-data signing key must stay outside the " +
                $"project. Move it and update {SigningKeyPathEnvironmentVariable}.");
        }

        if (!File.Exists(keyAbsolutePath))
        {
            throw new FileNotFoundException(
                "The external game-data signing key is missing. Restore it " +
                $"outside the repository or set {SigningKeyPathEnvironmentVariable}.",
                keyAbsolutePath);
        }

        string json = File.ReadAllText(keyAbsolutePath);
        SigningKeyFile key = JsonUtility.FromJson<SigningKeyFile>(json);
        if (key == null)
            throw new InvalidDataException("The game data signing key is invalid.");

        try
        {
            return new RSAParameters
            {
                Modulus = Convert.FromBase64String(key.modulus),
                Exponent = Convert.FromBase64String(key.exponent),
                D = Convert.FromBase64String(key.d),
                P = Convert.FromBase64String(key.p),
                Q = Convert.FromBase64String(key.q),
                DP = Convert.FromBase64String(key.dp),
                DQ = Convert.FromBase64String(key.dq),
                InverseQ = Convert.FromBase64String(key.inverseQ)
            };
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException(
                "The game data signing key contains invalid Base64.",
                exception);
        }
    }

    private static string GetAbsoluteAssetPath(string assetPath)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        if (string.IsNullOrEmpty(projectRoot))
            throw new DirectoryNotFoundException("Unity project root could not be resolved.");

        return Path.GetFullPath(Path.Combine(
            projectRoot,
            assetPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static byte[] ReadAllBytesShared(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private static void WriteAllBytesAtomically(string targetPath, byte[] bytes)
    {
        string temporaryPath =
            targetPath + ".tmp-" + Guid.NewGuid().ToString("N");

        try
        {
            File.WriteAllBytes(temporaryPath, bytes);

            if (File.Exists(targetPath))
                File.Replace(temporaryPath, targetPath, null);
            else
                File.Move(temporaryPath, targetPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static bool ByteArraysEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null || left.Length != right.Length)
            return false;

        int difference = 0;
        for (int i = 0; i < left.Length; i++)
            difference |= left[i] ^ right[i];

        return difference == 0;
    }
}

public sealed class GameDataBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        try
        {
            GameDataWorkbookEditor.EnsureRuntimeArchiveCurrent(logResult: true);
            GameDataWorkbookEditor.ValidateRuntimeArchiveOrThrow();
        }
        catch (Exception exception)
        {
            throw new BuildFailedException(
                $"Protected game data build preparation failed: {exception.Message}");
        }
    }
}

[InitializeOnLoad]
public static class GameDataWorkbookAutoReload
{
    static GameDataWorkbookAutoReload()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    public static bool IsSourceWorkbookAssetPath(string assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return false;

        return string.Equals(
            assetPath.Replace('\\', '/'),
            GameDataWorkbook.EditorSourceAssetPath,
            StringComparison.OrdinalIgnoreCase);
    }

    public static void ReloadForImportedAssets(string[] importedAssets)
    {
        if (importedAssets == null)
            return;

        foreach (string importedAsset in importedAssets)
        {
            if (!IsSourceWorkbookAssetPath(importedAsset))
                continue;

            ReloadEnvironmentVariablesAndPlayers(logResult: true);
            return;
        }
    }

    internal static int ReloadLoadedPlayerDefaults()
    {
        if (!EditorApplication.isPlaying)
            return 0;

        int refreshedPlayerCount = 0;
        PlayerScript[] players = UnityEngine.Object.FindObjectsByType<PlayerScript>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (PlayerScript player in players)
        {
            if (player == null ||
                EditorUtility.IsPersistent(player) ||
                !player.gameObject.scene.IsValid() ||
                !player.gameObject.scene.isLoaded)
            {
                continue;
            }

            player.ReloadCharacterDefaults();
            refreshedPlayerCount++;
        }

        return refreshedPlayerCount;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            ReloadEnvironmentVariablesAndPlayers(logResult: false);
    }

    private static void ReloadEnvironmentVariablesAndPlayers(bool logResult)
    {
        try
        {
            GameDataWorkbookEditor.ValidateSourceWorkbookOrThrow();
            EnvironmentVariableTables.Reload();
            MonsterTables.Reload();
            MonsterGrowthTables.Reload();
            UpgradeTables.Reload();
            BonusTables.Reload();
            SkinTables.Reload();
            PatternTables.Reload();
            int refreshedPlayerCount = ReloadLoadedPlayerDefaults();
            int refreshedGameManagerCount = ReloadLoadedMonsterStats();
            int refreshedChapterEnemyControllerCount =
                ReloadLoadedChapterEnemyStats();
            if (logResult)
            {
                Debug.Log(
                    "[GameData] Data.xlsx runtime values reloaded. " +
                    $"activePlayers={refreshedPlayerCount}, " +
                    $"activeGameManagers={refreshedGameManagerCount}, " +
                    $"activeChapterEnemyControllers={refreshedChapterEnemyControllerCount}");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static int ReloadLoadedMonsterStats()
    {
        if (!EditorApplication.isPlaying)
            return 0;

        int refreshedCount = 0;
        GameManager[] managers = UnityEngine.Object.FindObjectsByType<GameManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (GameManager manager in managers)
        {
            if (manager == null ||
                EditorUtility.IsPersistent(manager) ||
                !manager.gameObject.scene.IsValid() ||
                !manager.gameObject.scene.isLoaded)
            {
                continue;
            }

            manager.SettingMonsterStats();
            manager.ApplyStatsToAllEnemies();
            refreshedCount++;
        }

        return refreshedCount;
    }

    private static int ReloadLoadedChapterEnemyStats()
    {
        if (!EditorApplication.isPlaying)
            return 0;

        int refreshedCount = 0;
        ChapterEnemyStatController[] controllers =
            UnityEngine.Object.FindObjectsByType<ChapterEnemyStatController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        foreach (ChapterEnemyStatController controller in controllers)
        {
            if (controller == null ||
                EditorUtility.IsPersistent(controller) ||
                !controller.gameObject.scene.IsValid() ||
                !controller.gameObject.scene.isLoaded)
            {
                continue;
            }

            controller.ApplyStats();
            refreshedCount++;
        }

        return refreshedCount;
    }
}

public sealed class GameDataWorkbookAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        GameDataWorkbookAutoReload.ReloadForImportedAssets(importedAssets);
    }
}
#endif
