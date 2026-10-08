using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class JsonWriter
{
    private readonly string _filePath;
    
    public JsonWriter(string filePath)
    {
        _filePath = filePath;
    }

    public void Write<T>(T data, string fileName)
    {
        string path = Path.Combine(_filePath, fileName);
        string json = JsonUtility.ToJson(data, false);

        try
        {
            byte[] encryptedBytes = EncryptStringToBytes_Aes(json, EncryptionConfig.SecretKey, EncryptionConfig.SecretIv);
            File.WriteAllBytes(path, encryptedBytes);
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonWriter] Encrypt and write failed. Error: {e.Message}");
        }
    }

    private byte[] EncryptStringToBytes_Aes(string plainText, string keyStr, string ivStr)
    {
        if (string.IsNullOrEmpty(plainText)) return null;
        byte[] key = Encoding.UTF8.GetBytes(keyStr);
        byte[] iv = Encoding.UTF8.GetBytes(ivStr);

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
}