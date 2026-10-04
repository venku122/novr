using System;
using UnityEngine.XR.OpenXR.Features;

namespace NOVR.Controllers;

/// <summary>Not registered as a feature; exposes existing read-only native helpers.</summary>
internal sealed class EyeGazeRuntimeDiagnostics : OpenXRFeature
{
    public static string CurrentEyeProfile()
    {
        try
        {
            var path = GetCurrentInteractionProfile("/user/eyes_ext");
            return path == 0 ? "unavailable" : PathToString(path) ?? "unavailable";
        }
        catch (DllNotFoundException) { return "unavailable:native-library"; }
        catch (EntryPointNotFoundException) { return "unavailable:native-entry-point"; }
        catch (InvalidOperationException) { return "unavailable:session"; }
    }
}
