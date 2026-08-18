using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewFloatingTextContainer", menuName = "PrefabContainer/FloatingTextContainer")]
public class FloatingTextContainer : ScriptableObject
{
    [SerializeField] private List<PopupEntry> floatingTexts;
    public List<PopupEntry> FloatingTexts => floatingTexts;
}