using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

// Keep the native canvas parent and visibility. Only pose changes; no event,
// notification timer, alpha, or text mutation belongs to this placement helper.
internal sealed class HudHeadFollowingPlacement
{
    private readonly Transform _root;
    private readonly Vector3 _position, _offset;
    private readonly Quaternion _rotation, _worldRotation;
    private bool _following;
    public HudHeadFollowingPlacement(Transform root, Vector3 offset)
    { _root = root; _position = root.localPosition; _rotation = root.localRotation; _worldRotation = root.rotation; _offset = offset; }
    public void Apply(Vector3 headPosition, Quaternion headRotation)
    {
        if (_root == null) return;
        _root.SetPositionAndRotation(headPosition + headRotation * _offset, headRotation * _worldRotation);
        _following = true;
    }
    public void Restore()
    {
        if (_root == null || !_following) return;
        _root.localPosition = _position; _root.localRotation = _rotation; _following = false;
    }
}
