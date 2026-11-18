using UnityEngine;

public enum ConfigResult { Success, Skipped, FileNotFound }

public class ConfigLoader : IConfigLoader
{
    private readonly JsonReader _reader;

    public ConfigLoader()
    {
        _reader = new JsonReader(Application.persistentDataPath);
    }
    
    public ConfigResult LoadConfig(ScriptableObject config)
    {
        if (config is not IConfig)
        {
            Debug.LogError($"[ConfigLoader] {config.name} is not correct type. Config data must have IConfig!");
            return ConfigResult.FileNotFound; 
        }

        if (config is PlayerDataConfig)
        {
            Debug.Log($"[ConfigLoader] Skip loading local json for {config.name}.");
            return ConfigResult.Skipped; 
        }

        string fileName = config.name + ".json";
        string json = _reader.Read(fileName);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning($"[ConfigLoader] Not found config file {fileName} (Is your first time play?) - return default value {config.name}.");
            return ConfigResult.FileNotFound;
        }
        
        JsonUtility.FromJsonOverwrite(json, config);
        return ConfigResult.Success;
    }
}