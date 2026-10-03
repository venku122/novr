using System;
using UnityEngine.XR.OpenXR.Features;

namespace UnityEngine.XR.OpenXR;

/// <summary>Read-only access to the existing plugin's current profile query.</summary>
public sealed class OpenXRInputDiagnostics : OpenXRFeature
{
    // This type is never registered as a feature. It only exposes existing native helpers.
    public static string CurrentProfile(bool leftHand)
    {
        try
        {
            var path = GetCurrentInteractionProfile(leftHand ? "/user/hand/left" : "/user/hand/right");
            return path == 0 ? "unavailable" : PathToString(path) ?? "unavailable";
        }
        catch (DllNotFoundException) { return "unavailable:native-library"; }
        catch (EntryPointNotFoundException) { return "unavailable:native-entry-point"; }
        catch (InvalidOperationException) { return "unavailable:session"; }
    }
}
