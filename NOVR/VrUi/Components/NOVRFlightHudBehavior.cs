using UnityEngine;
using UnityEngine.UI;

namespace NOVR.VrUi.SpecialBehavior;

[DefaultExecutionOrder(400)]
public class NOVRFlightHudBehavior : UIRenderedCanvasBehavior
{
    public static NOVRFlightHudBehavior? Instance { get; private set; }
    private readonly HudStatusPolicy _statusPolicy = new();
    private HudPanelPlacement[] _statusPanels = System.Array.Empty<HudPanelPlacement>();
    private readonly System.Collections.Generic.List<(HudPanelPlacement placement, Vector3 position)> _statusChildren = new(3);
    private Transform? _helmetCenter;
    private HudPanelPlacement? _compactWeapon;
    internal bool HelmetCenterAvailable => _helmetCenter != null && _helmetCenter.gameObject.activeInHierarchy;
    public bool StatusFollowsHelmet { get; private set; }
    public float StatusBoresightAngle { get; private set; }
    public float StatusLookingDownAngle { get; private set; }
    public bool StatusCockpitDecluttered => _statusPolicy.CockpitDecluttered;
    public static HudPanelSnapshot[] CaptureStatusPanelSnapshots()
    {
        var source = Instance;
        if (source == null) return System.Array.Empty<HudPanelSnapshot>();
        var result = new HudPanelSnapshot[source._statusPanels.Length + source._statusChildren.Count];
        for (var i = 0; i < source._statusPanels.Length; i++) result[i] = source._statusPanels[i].Snapshot();
        for (var i = 0; i < source._statusChildren.Count; i++) result[source._statusPanels.Length + i] = source._statusChildren[i].placement.Snapshot();
        return result;
    }
    public override void Awake()
    {
        base.Awake();
        Instance = this;
        SetRootPose();
        var hudcenter = FindChildStartingWith(transform, "HUDCenter");
        if (hudcenter != null) hudcenter.gameObject.AddComponent(typeof(NoVrHudBehavior));
        
        var hmdcenter = FindChildStartingWith(transform, "HMDCenter");
        _helmetCenter = hmdcenter;
        if (hmdcenter != null) hmdcenter.gameObject.AddComponent(typeof(NOVRHMDBehavior));
        
        if (hudcenter != null)
        {
            MoveHmdPanelToHud("TopRightPanel", hudcenter, new Vector3(330, 290, 0f), new Vector3(0.6f, 0.6f, 0.6f));
            MoveHmdPanelToHud("LowerLeftPanel", hudcenter, new Vector3(-400f, 80f, 0f), new Vector3(0.6f, 0.6f, 0.6f));
        }

        // Capture the existing NOVR aircraft layout after one-time setup. Only
        // these panels move; registered markers keep their projection/visibility.
        var panels = new System.Collections.Generic.List<HudPanelPlacement>(2);
        var topRight = FindChildStartingWith(transform, "TopRightPanel");
        if (topRight != null) panels.Add(new HudPanelPlacement(topRight));
        if (topRight != null)
        {
            CacheHelmetChild(topRight, "countermeasuresBackground", new Vector3(-370, -55, 0));
            CacheHelmetChild(topRight, "PowerPanel", new Vector3(-220, -80, 0));
            CacheHelmetChild(topRight, "weaponPanel", new Vector3(-100, -55, 0));
        }
        // LowerLeft contains hudMapAnchor: its map has an independent angular
        // layout. Never magnify/reparent that complete subtree with status.
        var weapon = topRight != null ? FindChildStartingWith(topRight, "weaponPanel") : null;
        if (weapon != null) _compactWeapon = new HudPanelPlacement(weapon);
        _statusPanels = panels.ToArray();
        var targetDesignator = FindChildStartingWith(transform, "targetDesignator");
        if (targetDesignator != null) targetDesignator.gameObject.AddComponent(typeof(NOVRTargetDesignatorBehavior));

        if (!gameObject.TryGetComponent<PitchCompassBehavior>(out _))
        {
            gameObject.AddComponent<PitchCompassBehavior>();
        }

        // var velocityVector = FindChildStartingWith(transform, "velocityVector");
        // if (velocityVector != null) velocityVector.gameObject.AddComponent(typeof(NOVRVelocityVectorBehavior));
    }
    
    public override void OnEnable()
    {
        base.OnEnable();
        Application.onBeforeRender += BeforeRender;
    }
    public override void OnDisable()
    {
        Application.onBeforeRender -= BeforeRender;
        RestoreStatusPanels();
        base.OnDisable();
    }
    private void OnDestroy()
    {
        Application.onBeforeRender -= BeforeRender;
        RestoreStatusPanels();
        if (Instance == this) Instance = null;
    }
    private void LateUpdate() => UpdateLayout();
    private void Update() => SetRootPose();
    [BeforeRenderOrder(240)]
    private void BeforeRender() => UpdateLayout();
    private void UpdateLayout()
    {
        SetRootPose();
        var camera = APIBus.MainCamera;
        bool hasAircraft = GameManager.GetLocalAircraft(out var aircraft);
        bool referenceValid = _helmetCenter != null && _helmetCenter.gameObject.activeInHierarchy &&
            NOUIManager.I != null && camera != null && NOVRHeadsetData.HeadTracked && hasAircraft && aircraft != null && aircraft.cockpit != null;
        StatusBoresightAngle = referenceValid
            ? Vector3.Angle(camera.transform.forward, aircraft.cockpit.transform.forward) : 0;
        StatusLookingDownAngle = referenceValid
            ? -Mathf.Asin(Mathf.Clamp(Vector3.Dot(camera.transform.forward, aircraft.cockpit.transform.up), -1, 1)) * Mathf.Rad2Deg : 0;
        StatusFollowsHelmet = _statusPolicy.UseHelmet(ModConfiguration.Instance.HudStatusMode.Value,
            StatusBoresightAngle, ModConfiguration.Instance.HudSmartAngle.Value, referenceValid,
            ModConfiguration.Instance.HudCockpitDeclutter.Value, StatusLookingDownAngle, ModConfiguration.Instance.HudDeclutterDownAngle.Value);
        bool compact = HudPeripheralLayout.Compact(ModConfiguration.Instance.HudStatusMode.Value, ModConfiguration.Instance.HudStatusDetail.Value);
        bool fullHelmet = StatusFollowsHelmet && !compact;
        if (!StatusFollowsHelmet || !compact) _compactWeapon?.Restore();
        foreach (var panel in _statusPanels)
        {
            var offset = new Vector3(300, -160, 0);
            panel.SetHelmet(fullHelmet, _helmetCenter, offset, HudPeripheralLayout.StatusScale(ModConfiguration.Instance.HudStatusScale.Value));
        }
        foreach (var child in _statusChildren) child.placement.SetHelmetLocal(fullHelmet, child.position);
        if (compact && StatusFollowsHelmet)
            _compactWeapon?.SetHelmet(true, _helmetCenter, HudPeripheralLayout.WeaponOffset, HudPeripheralLayout.StatusScale(ModConfiguration.Instance.HudStatusScale.Value));
    }
    private void SetRootPose()
    {
        transform.position = new Vector3(0f, 0f, 1000f);
        transform.rotation = Quaternion.identity;
    }
    private void RestoreStatusPanels()
    {
        _compactWeapon?.Restore();
        foreach (var panel in _statusPanels) panel.Restore();
        foreach (var child in _statusChildren) child.placement.Restore();
        _statusPolicy.Reset(); StatusFollowsHelmet = false;
    }
    private void CacheHelmetChild(Transform parent, string name, Vector3 position)
    {
        var child = FindChildStartingWith(parent, name);
        if (child != null) _statusChildren.Add((new HudPanelPlacement(child), position));
    }
    
    private void MoveHmdPanelToHud(string panelName, Transform noVrHudParent, Vector3 localPosition, Vector3 localScale)
    {
        if (noVrHudParent == null)
            return;
        
        var panel = FindChildStartingWith(transform, panelName);
        if (panel == null)
            return;
        
        panel.SetParent(noVrHudParent, false);
        panel.localPosition = localPosition;
        panel.localEulerAngles = Vector3.zero;
        panel.localScale = localScale;
        MakePanelInvisible(panel);
        
        if (panelName == "TopRightPanel")
        {
            PositionTopRightPanelChildren(panel);
        }
    }
    
    private static void MakePanelInvisible(Transform panel)
    {
        var image = panel.GetComponent<Image>();
        if (image != null)
            image.enabled = false;
    }
    
    private static void MakeChildImageInvisible(Transform parent, string childName)
    {
        var child = FindChildStartingWith(parent, childName);
        if (child == null)
            return;
        
        var image = child.GetComponent<Image>();
        if (image != null)
            image.enabled = false;
    }
    
    
    private static void PositionTopRightPanelChildren(Transform topRightPanel)
    {
        SetChildLocalPosition(topRightPanel, "countermeasuresBackground", new Vector3(-750f, -55f, 0f));
        SetChildLocalPosition(topRightPanel, "weaponPanel", new Vector3(-100f, -55f, 0f));
        SetChildLocalPosition(topRightPanel, "PowerPanel", new Vector3(-350f, -80f, 0f));
        var powerPanel = FindChildStartingWith(topRightPanel, "PowerPanel");
        if (powerPanel == null)
            return;
        
        MakePanelInvisible(powerPanel);
        MakeChildImageInvisible(powerPanel, "chargeBarBackground");
    }
    
    private static void SetChildLocalPosition(Transform parent, string childName, Vector3 localPosition)
    {
        var child = FindChildStartingWith(parent, childName);
        if (child == null)
            return;
        
        child.localPosition = localPosition;
    }
}
