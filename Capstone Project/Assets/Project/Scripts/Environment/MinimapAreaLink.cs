using UnityEngine;

[DisallowMultipleComponent]
public class MinimapAreaLink : MonoBehaviour
{
    private MinimapController _controller;
    private LevelRunController _run;

    private void Start()
    {
        _controller = GetComponent<MinimapController>();
        _run = LevelRunController.Instance;
        if (_controller == null || _run == null)
        {
            enabled = false;
            return;
        }

        _run.AreaEntered += OnAreaEntered;
        if (_run.ActiveArea != null) OnAreaEntered(_run.ActiveArea); // the run started before this minimap
    }

    private void OnDestroy()
    {
        if (_run != null) _run.AreaEntered -= OnAreaEntered;
    }

    private void OnAreaEntered(AreaController area) => _controller.SetAreaWindow(area.Center, area.Radius);
}