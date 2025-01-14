using UnityEngine;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Vec4
{
    public float x, y, z, w;

    public Vector4 ToUnityVector()
    {
        return new Vector4(x, y, z, w);
    }
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Vec3
{
    public float x, y, z;

    public Vector3 ToUnityVector()
    {
        return new Vector3(x, y, z);
    }

    public Vector3 ToUnityVectorFMB()
    {
        return new Vector3(-x, y, z);
    }
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct Vec2
{
    public float x, y;

    public Vector2 ToUnityVector()
    {
        return new Vector2(x, y);
    }
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct HalfVec3
{
    public short x, y, z;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct HalfVec2
{
    public short x, y, z;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct OpaqueColor
{
    public float r, g, b;

    public Color ToUnityColor()
    {
        return new Color(r, g, b, 1f);
    }
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct OpaqueColor32
{
    public byte r, g, b;

    public Color32 ToUnityColor()
    {
        return new Color32(r, g, b, 255);
    }
};