using UnityEngine;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct UMBVector3
{
    public float x, z, y;

    public Vector3 ToUnityVector()
    {
        return new Vector3(x, y, z);
    }
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct UMBVertex
{
    public UMBVector3 vertex, normal;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct UMBFrame
{
    public int number;

    public bool usePreviousIndexData, usePreviousTextureData;

    public int numFaces;

    public ushort* indices;

    public int numTextures;

    public Vec2* textures;

    public int numColors;

    public OpaqueColor32* colors;

    public int numVertices;

    public UMBVertex* vertex;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct UMBObject
{
    public int materialIndex, numKeyFrames, numAnimationFrames;

    public UMBFrame* frames;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct UMBMaterial
{
    public char* name, texturePath, textureBase;

    public bool hasTexture;

    public OpaqueColor ambient, diffuse, specular;

    public float glossiness;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct UMB
{
    public int numMaterials;

    public UMBMaterial* materials;

    public int numObjects;

    public UMBObject* objects;
};