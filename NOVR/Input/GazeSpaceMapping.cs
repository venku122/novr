using System.Numerics;

namespace NOVR.Controllers;

/// <summary>Maps validated tracking-space gaze through the calibrated head/UI
/// basis. Eye movement changes the ray, never the camera or seated origin.</summary>
internal static class GazeSpaceMapping
{
    public static void ToUi(Vector3 headPosition, Quaternion headRotation,
        Vector3 gazePosition, Quaternion gazeRotation, Vector3 uiPosition, Quaternion uiRotation,
        out Vector3 origin, out Vector3 direction)
    {
        var trackingToUi = uiRotation * Quaternion.Inverse(headRotation);
        origin = uiPosition + Vector3.Transform(gazePosition - headPosition, trackingToUi);
        direction = Vector3.Transform(Vector3.UnitZ, trackingToUi * gazeRotation);
    }
}
