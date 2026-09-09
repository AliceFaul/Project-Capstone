using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayFabServiceList", menuName = "PlayFab/List")]
public class PlayFabServiceList : ScriptableObject
{
    [SerializeField] private List<PlayFabService> services = new List<PlayFabService>();
    public List<PlayFabService> Services => services;
}