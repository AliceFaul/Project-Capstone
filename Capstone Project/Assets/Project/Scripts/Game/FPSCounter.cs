using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float updateInterval = 0.5f;

    private float _accum = 0f;
    private int _frameCount = 0;
    private float _timer;

    private void Start()
    {
        _timer = updateInterval;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        _accum += Time.timeScale / Time.deltaTime;
        ++_frameCount;

        if (_timer <= 0f)
        {
            float fps = _accum / _frameCount;
            string format = $"FPS: {fps:F2}";
            fpsText.text = format;
            
            // Change text color
            if(fps < 30) fpsText.color = Color.red;
            else if (fps < 60) fpsText.color = Color.yellow;
            else if (fps < 90) fpsText.color = Color.green;
            
            _timer = updateInterval;
            _accum = 0f;
            _frameCount = 0;
        }
    }
}