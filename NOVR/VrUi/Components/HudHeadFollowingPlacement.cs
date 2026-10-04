using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

internal static class HudNotificationLayout
{
    public static Vector3 Offset(bool messageRoot, Vector3 worldPosition, Vector3 canvasOrigin)
    {
        if (messageRoot) return new Vector3(0, 0, 3);
        var relative = worldPosition - canvasOrigin;
        return new Vector3(relative.x, relative.y, 3 + relative.z);
    }
}

// Keep the native canvas parent and visibility. Only pose changes; no event,
// notification timer, alpha, or text mutation belongs to this placement helper.
internal sealed class HudHeadFollowingPlacement
{
    private readonly Transform _root;
    private readonly Vector3 _position, _worldPosition, _offset;
    private readonly Quaternion _rotation, _worldRotation;
    private readonly bool _restoreWorld;
    private bool _following;
    public HudHeadFollowingPlacement(Transform root, Vector3 offset, bool restoreWorld = false)
    { _root = root; _position = root.localPosition; _worldPosition = root.position; _rotation = root.localRotation; _worldRotation = root.rotation; _offset = offset; _restoreWorld = restoreWorld; }
    public void Apply(Vector3 headPosition, Quaternion headRotation)
    {
        if (_root == null) return;
        _root.SetPositionAndRotation(headPosition + headRotation * _offset, headRotation * _worldRotation);
        _following = true;
    }
    public void Restore()
    {
        if (_root == null || (!_following && !_restoreWorld)) return;
        if (_restoreWorld) _root.SetPositionAndRotation(_worldPosition, _worldRotation);
        else { _root.localPosition = _position; _root.localRotation = _rotation; }
        _following = false;
    }
}
