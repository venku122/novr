using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace NOVR.SteamVr;

/// <summary>Reads installed Valve assets; no redistributed mesh, runtime initialization or input binding.</summary>
public static class FrameModelAssets
{
    public static string DiscoverRuntime()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "openvr", "openvrpaths.vrpath");
        var paths = ReadJson<RuntimePaths>(path);
        if (paths.runtime == null) throw new InvalidDataException("SteamVR runtime paths unavailable.");
        foreach (var runtime in paths.runtime)
            if (Directory.Exists(runtime)) return runtime;
        throw new DirectoryNotFoundException("SteamVR runtime is not installed.");
    }

    public static FrameModelData Read(string runtime, bool left)
    {
        var name = "frame_controller_" + (left ? "left" : "right");
        var root = Path.Combine(runtime, "drivers", "frame_controller", "resources", "rendermodels", name);
        using var reader = OpenBounded(Path.Combine(root, name + ".obj"), 16 * 1024 * 1024);
        var data = ParseObj(reader);
        var definition = ReadJson<ModelDefinition>(Path.Combine(root, name + ".json"));
        var grip = definition.components?.openxr_grip?.component_local;
        if (grip == null || grip.origin?.Length != 3 || grip.rotate_xyz?.Length != 3)
            throw new InvalidDataException("Frame model has no OpenXR grip anchor.");
        foreach (var value in grip.origin) RequireFinite(value);
        foreach (var value in grip.rotate_xyz) RequireFinite(value);
        data.GripPosition = grip.origin; data.GripRotationXyz = grip.rotate_xyz;
        var texturePath = Path.Combine(root, name + "_color.png");
        if (new FileInfo(texturePath).Length > 16 * 1024 * 1024) throw new InvalidDataException("Controller texture exceeds limit.");
        data.Texture = File.ReadAllBytes(texturePath);
        data.Name = name;
        return data;
    }

    public static FrameModelData ParseObj(TextReader reader)
    {
        var positions = new List<float[]>(); var normals = new List<float[]>(); var uv = new List<float[]>();
        var output = new List<float>(); var outputNormals = new List<float>(); var outputUv = new List<float>(); var triangles = new List<int>();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Length > 65536) throw new InvalidDataException("OBJ line exceeds limit.");
            var items = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length == 0 || items[0].StartsWith("#", StringComparison.Ordinal)) continue;
            if (items[0] == "v" || items[0] == "vn" || items[0] == "vt")
            {
                var count = items[0] == "vt" ? 2 : 3;
                if (items.Length < count + 1) throw new InvalidDataException("Incomplete OBJ vector.");
                var values = new float[count];
                for (var i = 0; i < count; i++) { values[i] = float.Parse(items[i + 1], CultureInfo.InvariantCulture); RequireFinite(values[i]); }
                if (items[0] == "v") positions.Add(values); else if (items[0] == "vn") normals.Add(values); else uv.Add(values);
                if (positions.Count + normals.Count + uv.Count > 1000000) throw new InvalidDataException("OBJ exceeds geometry limit.");
            }
            else if (items[0] == "f")
            {
                if (items.Length < 4 || items.Length > 257) throw new InvalidDataException("Invalid OBJ face.");
                var first = output.Count / 3;
                for (var i = 1; i < items.Length; i++)
                {
                    var corner = items[i].Split('/');
                    var v = positions[Index(corner[0], positions.Count)];
                    output.Add(v[0]); output.Add(v[1]); output.Add(-v[2]);
                    var n = corner.Length > 2 && corner[2].Length > 0 ? normals[Index(corner[2], normals.Count)] : new float[3];
                    outputNormals.Add(n[0]); outputNormals.Add(n[1]); outputNormals.Add(-n[2]);
                    var t = corner.Length > 1 && corner[1].Length > 0 ? uv[Index(corner[1], uv.Count)] : new float[2];
                    outputUv.Add(t[0]); outputUv.Add(t[1]);
                }
                for (var i = 1; i < items.Length - 2; i++) { triangles.Add(first); triangles.Add(first + i + 1); triangles.Add(first + i); }
                if (output.Count > 3000000 || triangles.Count > 3000000) throw new InvalidDataException("OBJ exceeds mesh limit.");
            }
        }
        if (triangles.Count == 0) throw new InvalidDataException("OBJ contains no faces.");
        return new FrameModelData { Positions = output.ToArray(), Normals = outputNormals.ToArray(), Uv = outputUv.ToArray(), Triangles = triangles.ToArray() };
    }

    private static int Index(string token, int count)
    {
        var value = int.Parse(token, CultureInfo.InvariantCulture);
        var index = value > 0 ? value - 1 : count + value;
        if (value == 0 || index < 0 || index >= count) throw new InvalidDataException("OBJ index out of range.");
        return index;
    }
    private static void RequireFinite(float value)
    { if (float.IsNaN(value) || float.IsInfinity(value)) throw new InvalidDataException("Non-finite model coordinate."); }
    private static StreamReader OpenBounded(string path, long limit)
    { if (new FileInfo(path).Length > limit) throw new InvalidDataException("Model asset exceeds limit."); return File.OpenText(path); }
    private static T ReadJson<T>(string path)
    {
        if (new FileInfo(path).Length > 1024 * 1024) throw new InvalidDataException("Model metadata exceeds limit.");
        using var stream = File.OpenRead(path);
        return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
    }
    [DataContract] private sealed class RuntimePaths { [DataMember] public string[]? runtime; }
    [DataContract] private sealed class ModelDefinition { [DataMember] public Components? components; }
    [DataContract] private sealed class Components { [DataMember] public Component? openxr_grip; }
    [DataContract] private sealed class Component { [DataMember] public Pose? component_local; }
    [DataContract] private sealed class Pose { [DataMember] public float[]? origin; [DataMember] public float[]? rotate_xyz; }
}

public sealed class FrameModelData
{
    public string Name = "";
    public float[] Positions = Array.Empty<float>(), Normals = Array.Empty<float>(), Uv = Array.Empty<float>();
    public int[] Triangles = Array.Empty<int>();
    public byte[] Texture = Array.Empty<byte>();
    public float[] GripPosition = Array.Empty<float>(), GripRotationXyz = Array.Empty<float>();
}
