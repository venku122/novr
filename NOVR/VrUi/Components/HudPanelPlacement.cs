using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

// Capture once; mode switches never accumulate transform/RectTransform changes.
internal sealed class HudPanelPlacement
{
    public readonly Transform Panel;
    private readonly Transform? _parent;
    private readonly int _sibling;
    private readonly Vector3 _position, _scale;
    private readonly Quaternion _rotation;
    private readonly RectTransform? _rect;
    private readonly Vector2 _anchorMin, _anchorMax, _pivot, _size;
    private readonly Vector3 _anchoredPosition;
    private bool _helmet;

    public HudPanelPlacement(Transform panel)
    {
        Panel = panel; _parent = panel.parent; _sibling = panel.GetSiblingIndex();
        _position = panel.localPosition; _rotation = panel.localRotation; _scale = panel.localScale;
        _rect = panel as RectTransform;
        if (_rect != null)
        {
            _anchorMin = _rect.anchorMin; _anchorMax = _rect.anchorMax;
            _pivot = _rect.pivot; _size = _rect.sizeDelta; _anchoredPosition = _rect.anchoredPosition3D;
        }
    }
    public void SetHelmet(bool helmet, Transform? helmetParent)
    {
        helmet &= helmetParent != null;
        if (Panel == null || helmet == _helmet) return;
        if (helmet)
        {
            Panel.SetParent(helmetParent, false);
            Panel.localPosition = _position; Panel.localRotation = _rotation; Panel.localScale = _scale;
            _helmet = true;
        }
        else Restore();
    }
    public void Restore()
    {
        if (Panel == null || !_helmet || _parent == null) return;
        Panel.SetParent(_parent, false);
        Panel.SetSiblingIndex(_sibling);
        if (_rect != null)
        {
            _rect.anchorMin = _anchorMin; _rect.anchorMax = _anchorMax;
            _rect.pivot = _pivot; _rect.sizeDelta = _size; _rect.anchoredPosition3D = _anchoredPosition;
        }
        else Panel.localPosition = _position;
        Panel.localRotation = _rotation; Panel.localScale = _scale;
        _helmet = false;
    }
    public HudPanelSnapshot Snapshot() => Panel == null ? new HudPanelSnapshot() : new HudPanelSnapshot
    {
        available = true, name = Panel.name, parent = Panel.parent != null ? Panel.parent.name : "",
        aircraftParent = _parent != null ? _parent.name : "", helmet = _helmet,
        activeSelf = Panel.gameObject.activeSelf, activeInHierarchy = Panel.gameObject.activeInHierarchy,
        localPosition = Values(Panel.localPosition), worldPosition = Values(Panel.position), localRotation = Values(Panel.localRotation),
        localScale = Values(Panel.localScale), aircraftLocalPosition = Values(_position)
    };
    private static float[] Values(Vector3 value) => new[] { value.x, value.y, value.z };
    private static float[] Values(Quaternion value) => new[] { value.x, value.y, value.z, value.w };
}

public sealed class HudPanelSnapshot
{
    public bool available, helmet, activeSelf, activeInHierarchy;
    public string name = "", parent = "", aircraftParent = "";
    public float[] localPosition = System.Array.Empty<float>(), worldPosition = System.Array.Empty<float>(),
        localScale = System.Array.Empty<float>(), aircraftLocalPosition = System.Array.Empty<float>(), localRotation = System.Array.Empty<float>();
}
