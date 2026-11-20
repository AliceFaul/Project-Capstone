using System;
using System.Collections.Generic;
using UnityEngine;

public enum ControlSection { Movement, Combat }

[CreateAssetMenu(fileName = "ControlsConfig", menuName = "Settings/Controls Config")]
public class ControlsConfig : ScriptableObject
{
    [Serializable]
    public class Row
    {
        public string labelKey;
        public string actionPath;
        public ControlSection section;
    }

    public List<Row> rows = new List<Row>
    {
        new Row { labelKey = "CTRL_MOVE", actionPath = "Player/LeftClick", section = ControlSection.Movement },
        new Row { labelKey = "CTRL_ROLL", actionPath = "Player/Roll", section = ControlSection.Movement },
        new Row { labelKey = "CTRL_SPRINT", actionPath = "Player/Sprint", section = ControlSection.Movement },
        new Row { labelKey = "CTRL_INVENTORY", actionPath = "Player/ToggleInventory", section = ControlSection.Movement },
        new Row { labelKey = "CTRL_MINIMAP", actionPath = "Player/ToggleMinimap", section = ControlSection.Movement },
        new Row { labelKey = "CTRL_RANGED", actionPath = "Player/RightClick", section = ControlSection.Combat },
        new Row { labelKey = "CTRL_ARTIFACT1", actionPath = "Player/Artifact1", section = ControlSection.Combat },
        new Row { labelKey = "CTRL_ARTIFACT2", actionPath = "Player/Artifact2", section = ControlSection.Combat },
        new Row { labelKey = "CTRL_ARTIFACT3", actionPath = "Player/Artifact3", section = ControlSection.Combat },
        new Row { labelKey = "CTRL_HEAL", actionPath = "Player/Heal", section = ControlSection.Combat },
    };
}