using UnityEngine;

public interface IConfigLoader
{
    ConfigResult LoadConfig(ScriptableObject config);
}