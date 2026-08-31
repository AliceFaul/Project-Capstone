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
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }
}