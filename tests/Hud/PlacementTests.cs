using System;
using NOVR.VrUi.SpecialBehavior;
using UnityEngine;

static class PlacementTests
{
    static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
    public static void Run()
    {
        var aircraft=new Transform { name="HUDCenter" }; var helmet=new Transform { name="HMDCenter" };
        new Transform().SetParent(aircraft,false);
        var panel=new RectTransform { name="weaponPanel", localPosition=new Vector3(330,290,2),localScale=new Vector3(.6f,.6f,.6f),localRotation=new Quaternion(0,0,.3f,.95f),anchorMin=new Vector2(.1f,.2f),anchorMax=new Vector2(.8f,.9f),pivot=new Vector2(.2f,.4f),sizeDelta=new Vector2(270,130) };
        panel.SetParent(aircraft,false); panel.gameObject.activeSelf=false;
        var position=panel.localPosition; var rotation=panel.localRotation; var scale=panel.localScale;
        var min=panel.anchorMin; var max=panel.anchorMax; var pivot=panel.pivot; var size=panel.sizeDelta;
        var placement=new HudPanelPlacement(panel);
        for(int i=0;i<1000;i++)
        {
            placement.SetHelmet(true,helmet); Check(panel.parent==helmet,"helmet parent");
            panel.localPosition=new Vector3(1,2,3); panel.localScale=new Vector3(3,4,5);
            panel.anchorMin=default; panel.anchorMax=default; panel.pivot=default; panel.sizeDelta=default;
            placement.Restore();
            Check(panel.parent==aircraft&&panel.GetSiblingIndex()==1,"original parent/order restored");
            Check(panel.localPosition.Equals(position)&&panel.localRotation.Equals(rotation)&&panel.localScale.Equals(scale),"original transform restored");
            Check(panel.anchorMin.Equals(min)&&panel.anchorMax.Equals(max)&&panel.pivot.Equals(pivot)&&panel.sizeDelta.Equals(size),"original rect restored");
            Check(!panel.gameObject.activeSelf,"hidden panel never activated");
        }
        placement.SetHelmet(true,null); Check(panel.parent==aircraft,"missing helmet safe");
        placement.SetHelmet(true,helmet); aircraft.Destroyed=true; placement.Restore();
        Check(panel.parent==helmet,"destroyed original parent must not orphan a scene child");
        panel.Destroyed=true; placement.SetHelmet(true,helmet); Check(!placement.Snapshot().available,"destroyed panel safe snapshot");
        Console.WriteLine("HUD panel restore/hidden/destroyed tests passed.");
    }
}
