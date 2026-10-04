using UnityEngine;
using NOVR.VrUi;

namespace NOVR.Controllers;

/// <summary>Opt-in noninteractive gaze feedback. Never changes selection or aircraft input.</summary>
internal sealed class GazeReticle : MonoBehaviour
{
    private LineRenderer? _ring;
    private Material? _material;
    private void Start()
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) { enabled = false; return; }
        var go = new GameObject("NOVR Eye Gaze");
        go.transform.SetParent(transform, false);
        go.layer = (int)LayerHelper.GetVrUiLayer();
        _material = new Material(shader);
        _ring = go.AddComponent<LineRenderer>();
        _ring.sharedMaterial = _material;
        _ring.useWorldSpace = false; _ring.loop = true; _ring.positionCount = 12;
        _ring.startColor = _ring.endColor = new Color(.3f, 1f, .6f, .8f);
        for (var i = 0; i < 12; i++)
        { var angle = i * Mathf.PI / 6; _ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0)); }
        _ring.startWidth = _ring.endWidth = .15f;
        _ring.enabled = false;
    }
    private void LateUpdate()
    {
        if (_ring == null) return;
        _ring.enabled = false;
        if (!Application.isFocused || !ModConfiguration.Instance.ShowGazeReticle.Value ||
            !EyeGazeInput.TryGetTrackingPose(out var gazePosition, out var gazeRotation, out _) ||
            !VrControllerInput.TryGetHeadPose(out var headPosition, out var headRotation)) return;
        var camera = APIBus.CockpitHudCamera;
        if (camera == null) return;
        var trackingToUi = camera.transform.rotation * Quaternion.Inverse(headRotation);
        var origin = camera.transform.position + trackingToUi * (gazePosition - headPosition);
        var ray = new Ray(origin, trackingToUi * gazeRotation * Vector3.forward);
        if (!VrCanvasHitTester.RaycastCanvasPlanes(ray, out var hit)) return;
        _ring.transform.position = hit.WorldPoint - ray.direction * .002f;
        _ring.transform.rotation = camera.transform.rotation;
        _ring.transform.localScale = Vector3.one * Mathf.Max(.001f, Vector3.Distance(origin, hit.WorldPoint) * .0008f);
        _ring.enabled = true;
    }
    private void OnDisable() { if (_ring != null) _ring.enabled = false; }
    private void OnDestroy()
    {
        if (_ring != null) Destroy(_ring.gameObject);
        if (_material != null) Destroy(_material);
    }
}
