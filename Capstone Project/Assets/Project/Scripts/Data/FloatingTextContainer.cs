using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewFloatingTextContainer", menuName = "PrefabContainer/FloatingTextContainer")]
public class FloatingTextContainer : ScriptableObject
{
    [SerializeField] private List<Entry> floatingTexts;
    public List<Entry> FloatingTexts => floatingTexts;
}

[System.Serializable]
public class Entry
{
    public string id;
    public GameObject prefab;
}