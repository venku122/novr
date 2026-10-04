// Pure placement boundary; no Unity native calls or rendering claims.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public struct Vector2 { public float x,y; public Vector2(float x,float y) { this.x=x; this.y=y; } }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; } public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z); public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z); }
    public struct Quaternion { public float x,y,z,w; public Quaternion(float x,float y,float z,float w) { this.x=x; this.y=y; this.z=z; this.w=w; } public static Quaternion identity=>new(0,0,0,1); public static Vector3 operator *(Quaternion q,Vector3 v) { var n=System.Numerics.Vector3.Transform(new(v.x,v.y,v.z),new System.Numerics.Quaternion(q.x,q.y,q.z,q.w));return new(n.X,n.Y,n.Z); } public static Quaternion operator *(Quaternion a,Quaternion b) { var n=new System.Numerics.Quaternion(a.x,a.y,a.z,a.w)*new System.Numerics.Quaternion(b.x,b.y,b.z,b.w);return new(n.X,n.Y,n.Z,n.W); } }
    public sealed class GameObject { public bool activeSelf=true; public bool activeInHierarchy=>activeSelf; }
    public class Transform
    {
        public string name=""; public bool Destroyed; public readonly GameObject gameObject=new();
        private readonly List<Transform> _children=new();
        public Transform? parent { get; private set; }
        public Vector3 localPosition, localScale=new(1,1,1); public Quaternion localRotation=Quaternion.identity;
        public Vector3 lossyScale=>parent is null?localScale:new(localScale.x*parent.lossyScale.x,localScale.y*parent.lossyScale.y,localScale.z*parent.lossyScale.z);
        public Vector3 position { get=>parent is null?localPosition:parent.position+new Vector3(localPosition.x*parent.lossyScale.x,localPosition.y*parent.lossyScale.y,localPosition.z*parent.lossyScale.z); set=>localPosition=parent is null?value:new((value.x-parent.position.x)/parent.lossyScale.x,(value.y-parent.position.y)/parent.lossyScale.y,(value.z-parent.position.z)/parent.lossyScale.z); }
        public Quaternion rotation { get=>localRotation; set=>localRotation=value; }
        public void SetPositionAndRotation(Vector3 p,Quaternion r) { position=p;rotation=r; }
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
