using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PopupEntry
{
    public PopupType type;
    public Popup prefab;
}

[CreateAssetMenu(fileName = "PopupContainer", menuName = "PrefabContainer/PopupContainer")]
public class PopupContainer : ScriptableObject
{
    [SerializeField] private List<PopupEntry> popups;
    public List<PopupEntry> Popups => popups;
}