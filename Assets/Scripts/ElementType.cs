using UnityEngine;

// Almighty is not meant to be listed in affinities, so it always hits as Normal
public enum ElementType { Physical, Fire, Ice, Electric, Wind, Almighty }

// new values go to the end, Unity stores enums in assets as numbers
public enum Affinity { Normal, Weak, Resist, Null }

public enum HitResult { Normal, Weak, Resisted, Nullified, KnockedDown }

[System.Serializable]
public struct ElementAffinity
{
    [SerializeField] private ElementType _element;
    [SerializeField] private Affinity _affinity;

    public ElementType Element => _element;
    public Affinity Affinity => _affinity;
}
