using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NOVR.Diagnostics;

/// <summary>Snapshots contain public data fields only, including Unity vector components.
/// Excluding properties avoids Vector3.normalized recursion and native getters.</summary>
internal static class DiagnosticJson
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new PublicFieldsResolver(),
        MaxDepth = 16,
        ReferenceLoopHandling = ReferenceLoopHandling.Error
    };

    public static string Serialize(object snapshot) => JsonConvert.SerializeObject(snapshot, Formatting.Indented, Settings);

    private sealed class PublicFieldsResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers(Type objectType) =>
            new(objectType.GetFields(BindingFlags.Public | BindingFlags.Instance));
    }
}
