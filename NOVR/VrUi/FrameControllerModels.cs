using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using NOVR.SteamVr;

namespace NOVR.VrUi;

/// <summary>Static installed Frame assets at the OpenXR grip pose. Input remains entirely OpenXR.</summary>
[DefaultExecutionOrder(200)]
internal sealed class FrameControllerModels : NOVRBehaviour
{
    private sealed class HandModel
    {
        public Task<FrameModelData>? Load;
        public GameObject? Root;
        public Mesh? Mesh;
        public Material? Material;
        public Texture2D? Texture;
        public bool Finished;
    }
    private readonly HandModel _left = new(), _right = new();

    private void Start()
    {
        var configured = ModConfiguration.Instance.SteamVrRuntimeDirectory.Value;
        // Disk reads, JSON and OBJ parsing run off the Unity main thread.
        _left.Load = Task.Run(() => FrameModelAssets.Read(string.IsNullOrWhiteSpace(configured) ? FrameModelAssets.DiscoverRuntime() : configured, true));
        _right.Load = Task.Run(() => FrameModelAssets.Read(string.IsNullOrWhiteSpace(configured) ? FrameModelAssets.DiscoverRuntime() : configured, false));
    }

    private void Update()
    {
        FinishLoad(_left);
        FinishLoad(_right);
    }

    private void FinishLoad(HandModel hand)
    {
        if (hand.Finished || hand.Load == null || !hand.Load.IsCompleted) return;
        hand.Finished = true;
        try
        {
            var data = hand.Load.GetAwaiter().GetResult();
            var shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Controller model shader unavailable.");
            hand.Root = new GameObject(data.Name);
            hand.Root.transform.SetParent(transform, false);
            var model = new GameObject("Valve static mesh");
            model.transform.SetParent(hand.Root.transform, false);
            LayerHelper.SetLayerRecursive(hand.Root.transform, LayerHelper.GetVrUiLayer());
            // Valve metadata describes grip in model coordinates. Reflect RH Z, then invert
            // this anchor so the model origin attaches to the application's OpenXR grip pose.
            var p = data.GripPosition; var r = data.GripRotationXyz;
            var anchorRotation = Quaternion.AngleAxis(r[2], Vector3.forward) * Quaternion.AngleAxis(-r[1], Vector3.up) * Quaternion.AngleAxis(-r[0], Vector3.right);
            model.transform.localRotation = Quaternion.Inverse(anchorRotation);
            model.transform.localPosition = -(model.transform.localRotation * new Vector3(p[0], p[1], -p[2]));
            var vertices = new Vector3[data.Positions.Length / 3];
            var normals = new Vector3[vertices.Length]; var uv = new Vector2[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector3(data.Positions[i * 3], data.Positions[i * 3 + 1], data.Positions[i * 3 + 2]);
                normals[i] = new Vector3(data.Normals[i * 3], data.Normals[i * 3 + 1], data.Normals[i * 3 + 2]);
                uv[i] = new Vector2(data.Uv[i * 2], data.Uv[i * 2 + 1]);
            }
            hand.Mesh = new Mesh { name = data.Name, indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            hand.Mesh.vertices = vertices; hand.Mesh.normals = normals; hand.Mesh.uv = uv;
            hand.Mesh.triangles = data.Triangles; hand.Mesh.RecalculateBounds(); hand.Mesh.UploadMeshData(true);
            hand.Texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!hand.Texture.LoadImage(data.Texture, true)) throw new InvalidOperationException("Cannot decode Frame texture.");
            hand.Material = new Material(shader) { mainTexture = hand.Texture };
            model.AddComponent<MeshFilter>().sharedMesh = hand.Mesh;
            var renderer = model.AddComponent<MeshRenderer>(); renderer.sharedMaterial = hand.Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            hand.Root.SetActive(false);
            NOVRPlugin.LogSource?.LogInfo($"Loaded installed controller model '{data.Name}' ({vertices.Length} vertices), using OpenXR grip anchor.");
        }
        catch (Exception ex)
        {
            Release(hand);
            NOVRPlugin.LogSource?.LogWarning($"Frame controller model unavailable: {ex.Message}. Controller input continues.");
        }
        finally { hand.Load = null; }
    }

    private void LateUpdate() => UpdateModels();
    [BeforeRenderOrder(300)]
    protected override void OnBeforeRender() => UpdateModels();
    private void UpdateModels()
    {
        UpdateHand(_left, true); UpdateHand(_right, false);
    }
    private static void UpdateHand(HandModel hand, bool left)
    {
        if (hand.Root == null) return;
        if (!Application.isFocused || !ModConfiguration.Instance.ShowFrameControllerModels.Value ||
            !VrControllerInput.TryGetCurrentPoseInUi(left, false, out var position, out var rotation))
        { hand.Root.SetActive(false); return; }
        if (!hand.Root.activeSelf) hand.Root.SetActive(true);
        hand.Root.transform.SetPositionAndRotation(position, rotation);
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        if (_left.Root != null) _left.Root.SetActive(false);
        if (_right.Root != null) _right.Root.SetActive(false);
    }
    private void OnDestroy() { Release(_left); Release(_right); }
    private static void Release(HandModel hand)
    {
        if (hand.Root != null) Destroy(hand.Root);
        if (hand.Mesh != null) Destroy(hand.Mesh);
        if (hand.Material != null) Destroy(hand.Material);
        if (hand.Texture != null) Destroy(hand.Texture);
    }
}
