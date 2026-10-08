using System;
using UnityEngine;

public class GameConfig : MonoBehaviour
{
    private void Awake()
    {
        #if PLATFORM_ANDROID
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        #else
            Application.targetFrameRate = 144;
            QualitySettings.vSyncCount = 0;
        #endif
    }
}