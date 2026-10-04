// Pure placement boundary; no Unity native calls or rendering claims.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public struct Vector2 { public float x,y; public Vector2(float x,float y) { this.x=x; this.y=y; } }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; } }
    public struct Quaternion { public float x,y,z,w; public Quaternion(float x,float y,float z,float w) { this.x=x; this.y=y; this.z=z; this.w=w; } }
    public sealed class GameObject { public bool activeSelf=true; public bool activeInHierarchy=>activeSelf; }
    public class Transform
    {
        public string name=""; public bool Destroyed; public readonly GameObject gameObject=new();
        private readonly List<Transform> _children=new();
        public Transform? parent { get; private set; }
        public Vector3 localPosition, localScale; public Quaternion localRotation;
        public Vector3 position=>localPosition;
        public void SetParent(Transform? p,bool preserveWorld) { parent?._children.Remove(this); parent=p; parent?._children.Add(this); }
        public int GetSiblingIndex()=>parent?._children.IndexOf(this)??0;
        public void SetSiblingIndex(int i) { if(parent is null)return; parent._children.Remove(this); parent._children.Insert(Math.Clamp(i,0,parent._children.Count),this); }
        public static bool operator ==(Transform? a,Transform? b) { bool an=ReferenceEquals(a,null)||a.Destroyed, bn=ReferenceEquals(b,null)||b.Destroyed; return an||bn ? an&&bn : ReferenceEquals(a,b); }
        public static bool operator !=(Transform? a,Transform? b)=>!(a==b);
        public override bool Equals(object? obj)=>ReferenceEquals(this,obj);
        public override int GetHashCode()=>base.GetHashCode();
    }
    public class RectTransform : Transform
    {
        public Vector2 anchorMin,anchorMax,pivot,sizeDelta;
        public Vector3 anchoredPosition3D { get=>localPosition; set=>localPosition=value; }
    }
}
