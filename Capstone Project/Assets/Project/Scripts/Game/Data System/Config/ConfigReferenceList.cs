using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ConfigReferenceList", menuName = "Config/List")]
public class ConfigReferenceList : ScriptableObject
{
    [SerializeField] private List<ScriptableObject> configs = new List<ScriptableObject>();
    public List<ScriptableObject> Configs => configs;

    private void OnValidate()
    {
        List<ScriptableObject> configRemoved = new();
        foreach (var config in configs)
        {
            if (config is not IConfig)
            {
                Debug.LogError($"[ConfigReferenceList] {config.name} is not correct type. Must implement IConfig!");
                configRemoved.Add(config);
            }
        }

        foreach (var config in configRemoved)
        {
            configs.Remove(config);
        }
    }
}