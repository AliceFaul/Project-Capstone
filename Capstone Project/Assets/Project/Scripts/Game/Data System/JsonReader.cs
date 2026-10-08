using UnityEngine;
using System.IO;

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
            Debug.LogError($"[JsonReader] File not found: {path}");
            return null;
        }
        
        return File.ReadAllText(path);
    }
}