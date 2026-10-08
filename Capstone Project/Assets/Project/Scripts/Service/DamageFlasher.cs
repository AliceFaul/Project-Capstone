using ImprovedTimers;
using UnityEngine;

public class DamageFlasher
{
    private readonly Material _defaultMaterial;
    private readonly Material _flashMaterial;
    private readonly float _flashDuration = 0.15f;
    private readonly CountdownTimer _flashTimer;

    private readonly Renderer _renderer;

    public DamageFlasher(Renderer renderer, Material flashMaterial)
    {
        _renderer = renderer;
        _defaultMaterial = _renderer.material;
        _flashMaterial = flashMaterial;
        _flashTimer = new CountdownTimer(_flashDuration);
    }

    public void Trigger()
    {
        if(_renderer == null)
            return;
        
        _renderer.material = _flashMaterial;
        _flashTimer.OnTimerStop = UpdateFlash;
        _flashTimer.Start();
    }

    private void UpdateFlash()
    {
        if(_renderer == null)
            return;
        
        _renderer.material = _defaultMaterial;
    }
    
    public void Stop()
    {
        _flashTimer.Stop();
        if (_renderer != null)
        {
            _renderer.material = _defaultMaterial;
        }
    }
}