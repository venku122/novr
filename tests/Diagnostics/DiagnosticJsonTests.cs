using System;
using System.Text.Json;
using NOVR.Diagnostics;

internal static class DiagnosticJsonTests
{
    private sealed class Snapshot
    {
        public string name = "seat\n\"anchor\"";
        public Device[] devices = { new Device() };
        public Vector position = new() { x = .1f, y = 1.2f, z = -.3f };
    }
    private sealed class Device { public string name = "Left"; public float[] trigger = { .6f }; }
    private struct Vector
    {
        public float x, y, z;
        public Vector normalized => throw new Exception("Property/native getters must never be read");
    }
    public static void Run()
    {
        using var json = JsonDocument.Parse(DiagnosticJson.Serialize(new Snapshot()));
        var root = json.RootElement;
        if (root.GetProperty("devices")[0].GetProperty("trigger")[0].GetSingle() != .6f ||
            root.GetProperty("position").GetProperty("y").GetSingle() != 1.2f ||
            root.GetProperty("name").GetString() != "seat\n\"anchor\"")
            throw new Exception("Nested snapshot fields, vector components and escaped text must survive serialization");
        Console.WriteLine("PASS: nested device/pose JSON, numeric values, escaping and no property/native getter access");
    }
}
