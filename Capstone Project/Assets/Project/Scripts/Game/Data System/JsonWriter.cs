using System;
using System.IO;
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
            byte[] encryptedBytes = CryptoUtils.EncryptStringToBytes_Aes(json);
            File.WriteAllBytes(path, encryptedBytes);
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonWriter] Encrypt and write failed. Error: {e.Message}");
        }
    }
}