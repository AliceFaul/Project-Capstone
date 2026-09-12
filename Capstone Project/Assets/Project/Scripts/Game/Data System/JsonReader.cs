using UnityEngine;
using System.IO;
using System;
using System.Security.Cryptography;
using System.Text;

public class JsonReader
{
    private readonly string _filePath;

    public JsonReader(string filePath)
    {
        _filePath = filePath;
    }

    public string Read(string fileName)
    {
        string path = Path.Combine(_filePath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[JsonReader] File not found: {path}");
            return null;
        }

        try
        {
            byte[] encrypted = File.ReadAllBytes(path);
            string decrypted = DecryptStringFromBytes_Aes(encrypted, EncryptionConfig.SecretKey, EncryptionConfig.SecretIv);
            return decrypted;
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonReader] Decrypt file failed: {e.Message}");
            return null;
        }
    }

    private string DecryptStringFromBytes_Aes(byte[] cipherText, string keyStr, string ivStr)
    {
        if(cipherText is not { Length: > 0 }) return  null;
        byte[] key = Encoding.UTF8.GetBytes(keyStr);
        byte[] iv = Encoding.UTF8.GetBytes(ivStr);

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
}