using System;
using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public enum HudStatusDetail { Compact, Full }
internal static class HudPeripheralLayout
{
    public static bool Compact(HudStatusMode mode, HudStatusDetail detail) => mode == HudStatusMode.Smart && detail == HudStatusDetail.Compact;
    public static float MinimapDegrees(float value) => Bound(value, 12, 6, 22);
    public static float StatusScale(float value) => Bound(value, .9f, .6f, 1.35f);
    public static float MinimapWorldSize(float degrees) => 2000f * (float)Math.Tan(MinimapDegrees(degrees) * Math.PI / 360);
    public static Vector3 MinimapOffset(bool fromHead) => new(-531.71f, -324.92f, fromHead ? 1000 : 0);
    public static Vector3 WeaponOffset => new(450, -200, 0);
    public static Vector3 NumberOffset(int row) => new(470, -250 - row * 75, 1000);
    private static float Bound(float value, float fallback, float min, float max) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
}
