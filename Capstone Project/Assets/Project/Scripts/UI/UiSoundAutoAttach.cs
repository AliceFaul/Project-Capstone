using UnityEngine;
using UnityEngine.UI;

public class UiSoundAutoAttach : MonoBehaviour
{
    [Tooltip("Seconds between two checks. Also the longest delay before a freshly spawned button gets its sound.")]
    [SerializeField] private float interval = 0.2f;
    
    private Selectable[] _buffer = new Selectable[128];
    private float _nextCheckTime;

    private void Update()
    {
        float now = Time.unscaledTime;
        if(now < _nextCheckTime) return;
        _nextCheckTime = now + interval;
        
        int count = Selectable.allSelectables.Count;
        if(count == 0) return;
        if(count > _buffer.Length) _buffer = new Selectable[count * 2];

        int filled = Selectable.AllSelectablesNoAlloc(_buffer);
        for (int i = 0; i < filled; i++)
        {
            Selectable selectable = _buffer[i];
            _buffer[i] = null; // do not keep references to UI that may be destroyed soon.
            
            if(selectable == null || !UiSound.IsSoundTarget(selectable)) return;
            if(!selectable.TryGetComponent<UiSound>(out _)) selectable.gameObject.AddComponent<UiSound>();
        }
    }
}