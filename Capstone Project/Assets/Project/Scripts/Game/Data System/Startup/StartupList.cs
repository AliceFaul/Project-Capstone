using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Startup", menuName = "Startup/List")]
public class StartupList : ScriptableObject
{
    public List<StartupStep> steps = new List<StartupStep>();
}