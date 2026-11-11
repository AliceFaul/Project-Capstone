using System.Collections.Generic;

public class MusicDirector
{
    private class Entry
    {
        public MusicTrack track;
        public MusicPriority priority;
        public object owner;
        public int order;
    }

    // Owner used for the "base" music (scene / level / map track).
    private static readonly object BaseOwner = new object();

    private readonly MusicPlayer _player;
    private readonly List<Entry> _entries = new List<Entry>();
    private int _counter;

    public MusicDirector(MusicPlayer player)
    {
        _player = player;
    }
    
    public void SetBase(MusicTrack track, float fade)
    {
        RemoveOwner(BaseOwner);
        
        if(track != null)
            _entries.Add(new Entry { track = track, priority = MusicPriority.Scene, owner = BaseOwner, order = _counter++ });
        
        Evaluate(fade);
    }

    // One entry per owner: pushing again from the same owner replaces its previous entry.
    public void Push(MusicTrack track, MusicPriority priority, object owner, float fade)
    {
        if(track == null || owner == null) return;

        RemoveOwner(owner);
        _entries.Add(new Entry { track = track, priority = priority, owner = owner, order = _counter++ });
        Evaluate(fade);
    }

    public void Pop(object owner, float fade)
    {
        if(owner == null) return;
        if(RemoveOwner(owner)) Evaluate(fade);
    }

    // Clears everything (base + stack)
    public void Clear(float fade)
    {
        _entries.Clear();
        _player.Stop(fade);
    }

    // Drops entries whose owner was destroyed (e.g. scene unloaded while combat) so nothing gets stuck.
    public void Tick()
    {
        bool removed = false;

        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].owner is UnityEngine.Object unityObject && unityObject == null)
            {
                _entries.RemoveAt(i);
                removed = true;
            }
        }
        
        if(removed) Evaluate(-1f);
    }

    private bool RemoveOwner(object owner)
    {
        bool removed = false;

        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_entries[i].owner, owner))
            {
                _entries.RemoveAt(i);
                removed = true;
            }
        }
        
        return removed;
    }

    private void Evaluate(float fade)
    {
        Entry top = null;

        foreach (var e in _entries)
        {
            if(top == null || e.priority > top.priority || (e.priority == top.priority && e.order >= top.order))
                top = e;
        }
        
        if(top == null) _player.Stop(fade);
        else _player.Play(top.track, fade);
    }
}