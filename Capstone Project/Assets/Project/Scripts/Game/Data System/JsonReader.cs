using UnityEngine;
using System.IO;
using System;

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
            string decrypted = CryptoUtils.DecryptStringFromBytes_Aes(encrypted);
            return decrypted;
        }
        catch (Exception e)
        {
            Debug.LogError($"[JsonReader] Decrypt file failed: {e.Message}");
            return null;
        }
    }
}