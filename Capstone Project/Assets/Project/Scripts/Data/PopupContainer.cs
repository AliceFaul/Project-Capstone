using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PopupEntry
{
    public string id;
    public GameObject prefab;
}

[CreateAssetMenu(fileName = "PopupContainer", menuName = "PrefabContainer/PopupContainer")]
public class PopupContainer : ScriptableObject
{
    [SerializeField] private List<PopupEntry> popups;
    public List<PopupEntry> Popups => popups;
}