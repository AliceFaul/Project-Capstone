using UnityEngine;

public class ConfigLoader : IConfigLoader
{
    private readonly JsonReader _reader;

    public ConfigLoader()
    {
        _reader = new JsonReader(Application.persistentDataPath + ".json");
    }
    
    public void LoadConfig(ScriptableObject config)
    {
        if (config is not IConfig)
        {
            Debug.LogError($"[ConfigLoader] {config.name} is not correct type. Config data must have IConfig!");
            return;
        }

        string fileName = config.name + ".json";
        
        string json = _reader.Read(fileName);
        JsonUtility.FromJsonOverwrite(json, config);
    }
}