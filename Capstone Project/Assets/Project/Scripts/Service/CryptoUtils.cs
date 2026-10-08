using System.Text;
using System.Security.Cryptography;
using System.IO;
using System;
using UnityEngine;

public static class CryptoUtils
{
    private static readonly string GlobalSalt = "BCGCapstoneProjectGameSalt";
    private static readonly string SaveFilePath = Path.Combine(Application.persistentDataPath, "user_auth.dat");
    
    private static byte[] GetSecureKey()
    {
        string rawKey = SystemInfo.deviceUniqueIdentifier + GlobalSalt;
        using SHA256 sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
    }
    
    private static byte[] GetSecureIV()
    {
        string rawIV = GlobalSalt + SystemInfo.deviceUniqueIdentifier;
        using MD5 md5 = MD5.Create();
        return md5.ComputeHash(Encoding.UTF8.GetBytes(rawIV));
    }
    
    public static byte[] EncryptStringToBytes_Aes(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return null;
        
        byte[] key = GetSecureKey();
        byte[] iv = GetSecureIV();

        using Aes aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;
        ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msEncrypt = new MemoryStream();
        using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        
        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }
        
        var encrypted = msEncrypt.ToArray();
        return encrypted;
    }
    
    public static string DecryptStringFromBytes_Aes(byte[] cipherText)
    {
        if(cipherText is not { Length: > 0 }) return  null;
        
        byte[] key = GetSecureKey();
        byte[] iv = GetSecureIV();

        using Aes aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = iv;
        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
        
        using MemoryStream msDecrypt = new MemoryStream(cipherText);
        using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new StreamReader(csDecrypt);
        
        string plainText = srDecrypt.ReadToEnd();
        return plainText;
    }

    public static void SaveCredentials(string email, string password)
    {
        try
        {
            byte[] encryptedEmail = EncryptStringToBytes_Aes(email);
            byte[] encryptedPassword = EncryptStringToBytes_Aes(password);

            if (encryptedEmail == null || encryptedPassword == null) return;

            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);
            // Ghi độ dài mảng byte trước để khi đọc biết lối trích xuất
            writer.Write(encryptedEmail.Length);
            writer.Write(encryptedEmail);

            writer.Write(encryptedPassword.Length);
            writer.Write(encryptedPassword);

            // Ghi đè file nhị phân xuống ổ cứng
            File.WriteAllBytes(SaveFilePath, ms.ToArray());
        }
        catch (Exception e)
        {
            Debug.LogError($"[CryptoUtils] Lỗi ghi file xác thực: {e.Message}");
        }
    }
    
    public static bool TryLoadCredentials(out string email, out string password)
    {
        email = null;
        password = null;

        if (!File.Exists(SaveFilePath)) return false;

        try
        {
            byte[] fileBytes = File.ReadAllBytes(SaveFilePath);

            using MemoryStream ms = new MemoryStream(fileBytes);
            using BinaryReader reader = new BinaryReader(ms);
            int emailLength = reader.ReadInt32();
            byte[] encryptedEmail = reader.ReadBytes(emailLength);

            int passwordLength = reader.ReadInt32();
            byte[] encryptedPassword = reader.ReadBytes(passwordLength);

            email = DecryptStringFromBytes_Aes(encryptedEmail);
            password = DecryptStringFromBytes_Aes(encryptedPassword);

            return !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CryptoUtils] Lỗi đọc file hoặc giải mã (File có thể bị hỏng/can thiệp): {e.Message}");
            return false;
        }
    }
    
    public static void ClearCredentials()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
        }
    }
}