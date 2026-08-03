using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public sealed class GameDataIntegrityException : Exception
{
    public GameDataIntegrityException(string message)
        : base(message)
    {
    }

    public GameDataIntegrityException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public static class GameDataWorkbook
{
    public const string FileName = "Data.xlsx";
    public const string EditorSourceAssetPath =
        "Assets/ShooterSurvival/GameData/Editor/Data.xlsx";
    public const string RuntimeResourcePath = "GameData/Data";

#if !UNITY_EDITOR
    private static byte[] _runtimeWorkbookBytes;
#endif

    public static Stream OpenRead(string fileName = FileName)
    {
        if (!string.Equals(fileName, FileName, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Only the protected game data workbook '{FileName}' is supported.");
        }

#if UNITY_EDITOR
        string sourcePath = GetEditorSourceAbsolutePath();
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                $"Game data workbook is missing at '{EditorSourceAssetPath}'.",
                sourcePath);
        }

        return new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
#else
        if (_runtimeWorkbookBytes == null)
        {
            TextAsset protectedWorkbook =
                Resources.Load<TextAsset>(RuntimeResourcePath);
            if (protectedWorkbook == null)
            {
                throw new GameDataIntegrityException(
                    $"Protected game data resource '{RuntimeResourcePath}' is missing.");
            }

            _runtimeWorkbookBytes =
                GameDataArchive.Unprotect(protectedWorkbook.bytes);
        }

        return new MemoryStream(_runtimeWorkbookBytes, writable: false);
#endif
    }

#if UNITY_EDITOR
    public static string GetEditorSourceAbsolutePath()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        if (string.IsNullOrEmpty(projectRoot))
            throw new DirectoryNotFoundException("Unity project root could not be resolved.");

        return Path.GetFullPath(Path.Combine(
            projectRoot,
            EditorSourceAssetPath.Replace('/', Path.DirectorySeparatorChar)));
    }
#endif
}

public static class GameDataArchive
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("SSGDATA1");

    private const int FormatVersion = 1;
    private const int AesIvLength = 16;
    private const int MaxEncryptedWorkbookLength = 64 * 1024 * 1024;
    private const int MaxSignatureLength = 1024;
    private const string EncryptionKeyBase64 =
        "5skh6fkSmkZ9kC9EoOQY7TDuRGwRtFo7ahTcD0Ocxog=";
    private const string PublicModulusBase64 =
        "nS6JK14ENLnVR0GMwHI3DRzs+EYk8EcRqk0bepIxGHAhLK+3alkD+HxxpxuBiABNOjXWEIEiyYTgSpfXApA0ajlj1Vq1CTMcZ6jkAF9FhA7ZI3OGTOqdPB04BLdagLLgjOemibJxTdgdxeYgQhTaKTYDBwv7HTukpR74l74IbsXubaMOQ5QYeGvx97DnarjbVKJf6RkXp0WP+Cf2k4ZdYUSO2E8imW81lUSaMqbWlWKhPpvNFXh9UC3H1jYzu7q0Ut8hbkZjZGlfvgoEdR1aQAEYU+Tz9JFnOqn8j7CGdCxt0/3iCv0/47g4W2txZ1SvI54+54p6rbDIdJwGk/bVHQ==";
    private const string PublicExponentBase64 = "AQAB";

    public static byte[] Protect(
        byte[] workbookBytes,
        RSAParameters privateSigningKey)
    {
        if (workbookBytes == null)
            throw new ArgumentNullException(nameof(workbookBytes));

        byte[] encryptionKey = Convert.FromBase64String(EncryptionKeyBase64);
        byte[] iv;
        byte[] encryptedWorkbook;

        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = encryptionKey;
            aes.GenerateIV();
            iv = aes.IV;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            encryptedWorkbook = encryptor.TransformFinalBlock(
                workbookBytes,
                0,
                workbookBytes.Length);
        }
        finally
        {
            Array.Clear(encryptionKey, 0, encryptionKey.Length);
        }

        byte[] signedData = BuildSignedData(iv, encryptedWorkbook);
        byte[] signature;
        using (RSA rsa = RSA.Create())
        {
            rsa.ImportParameters(privateSigningKey);
            signature = rsa.SignData(
                signedData,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }

        using var archive = new MemoryStream(
            signedData.Length + sizeof(int) + signature.Length);
        archive.Write(signedData, 0, signedData.Length);
        using (var writer = new BinaryWriter(archive, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(signature.Length);
            writer.Write(signature);
            writer.Flush();
        }

        return archive.ToArray();
    }

    public static byte[] Unprotect(byte[] archiveBytes)
    {
        return Unprotect(archiveBytes, GetEmbeddedPublicKey());
    }

    public static byte[] Unprotect(
        byte[] archiveBytes,
        RSAParameters publicSigningKey)
    {
        if (archiveBytes == null)
            throw new ArgumentNullException(nameof(archiveBytes));

        if (archiveBytes.Length > MaxEncryptedWorkbookLength + 4096)
            throw new GameDataIntegrityException("Protected game data is too large.");

        try
        {
            using var archive = new MemoryStream(archiveBytes, writable: false);
            using var reader = new BinaryReader(archive, Encoding.UTF8, leaveOpen: true);

            byte[] magic = reader.ReadBytes(Magic.Length);
            if (!ByteArraysEqual(magic, Magic))
                throw new GameDataIntegrityException("Protected game data header is invalid.");

            int version = reader.ReadInt32();
            if (version != FormatVersion)
                throw new GameDataIntegrityException(
                    $"Unsupported protected game data version: {version}.");

            int ivLength = reader.ReadInt32();
            int encryptedLength = reader.ReadInt32();
            if (ivLength != AesIvLength ||
                encryptedLength <= 0 ||
                encryptedLength > MaxEncryptedWorkbookLength ||
                encryptedLength % AesIvLength != 0)
            {
                throw new GameDataIntegrityException(
                    "Protected game data payload length is invalid.");
            }

            long remainingBeforePayload = archive.Length - archive.Position;
            long requiredPayloadBytes =
                (long)ivLength + encryptedLength + sizeof(int);
            if (remainingBeforePayload < requiredPayloadBytes)
                throw new GameDataIntegrityException("Protected game data is truncated.");

            byte[] iv = reader.ReadBytes(ivLength);
            byte[] encryptedWorkbook = reader.ReadBytes(encryptedLength);
            int signedDataLength = checked((int)archive.Position);

            int signatureLength = reader.ReadInt32();
            if (signatureLength <= 0 ||
                signatureLength > MaxSignatureLength ||
                archive.Length - archive.Position != signatureLength)
            {
                throw new GameDataIntegrityException(
                    "Protected game data signature length is invalid.");
            }

            byte[] signature = reader.ReadBytes(signatureLength);
            byte[] signedData = new byte[signedDataLength];
            Buffer.BlockCopy(
                archiveBytes,
                0,
                signedData,
                0,
                signedDataLength);

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(publicSigningKey);
                if (!rsa.VerifyData(
                        signedData,
                        signature,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1))
                {
                    throw new GameDataIntegrityException(
                        "Protected game data integrity check failed.");
                }
            }

            byte[] encryptionKey = Convert.FromBase64String(EncryptionKeyBase64);
            try
            {
                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;

                using ICryptoTransform decryptor = aes.CreateDecryptor();
                return decryptor.TransformFinalBlock(
                    encryptedWorkbook,
                    0,
                    encryptedWorkbook.Length);
            }
            finally
            {
                Array.Clear(encryptionKey, 0, encryptionKey.Length);
            }
        }
        catch (GameDataIntegrityException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is EndOfStreamException ||
            exception is OverflowException ||
            exception is ArgumentException ||
            exception is CryptographicException)
        {
            throw new GameDataIntegrityException(
                "Protected game data is invalid or has been modified.",
                exception);
        }
    }

    private static byte[] BuildSignedData(byte[] iv, byte[] encryptedWorkbook)
    {
        using var signedData = new MemoryStream(
            Magic.Length +
            sizeof(int) * 3 +
            iv.Length +
            encryptedWorkbook.Length);
        using var writer = new BinaryWriter(
            signedData,
            Encoding.UTF8,
            leaveOpen: true);

        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write(iv.Length);
        writer.Write(encryptedWorkbook.Length);
        writer.Write(iv);
        writer.Write(encryptedWorkbook);
        writer.Flush();
        return signedData.ToArray();
    }

    private static RSAParameters GetEmbeddedPublicKey()
    {
        return new RSAParameters
        {
            Modulus = Convert.FromBase64String(PublicModulusBase64),
            Exponent = Convert.FromBase64String(PublicExponentBase64)
        };
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
