using ImprovedTimers;
using UnityEngine;

public class DamageFlasher
{
    private readonly Material[] _defaultMaterials;
    private readonly Material[] _flashMaterials;
    private readonly float _flashDuration = 0.15f;
    private readonly CountdownTimer _flashTimer;

    private readonly Renderer _renderer;

    public DamageFlasher(Renderer renderer, Material flashMaterial)
    {
        _renderer = renderer;
        if (_renderer != null)
        {
            _defaultMaterials = _renderer.materials;
            _flashMaterials = new Material[_defaultMaterials.Length];
            for(int i = 0; i < _flashMaterials.Length; i++) _flashMaterials[i] = flashMaterial;
        }
        _flashTimer = new CountdownTimer(_flashDuration);
        _flashTimer.OnTimerStop = UpdateFlash;
    }

    public void Trigger()
    {
        if(_renderer == null) return;
        _renderer.materials = _flashMaterials;
        _flashTimer.Start();
    }

    private void UpdateFlash()
    {
        if(_renderer == null) return;
        _renderer.materials = _defaultMaterials;
    }
    
    public void Stop()
    {
        _flashTimer.Stop();
        if(_renderer != null) _renderer.materials = _defaultMaterials;
    }
}