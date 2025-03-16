using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FMBMaterial
{
    public char* name, texturePath;

    public OpaqueColor ambient, diffuse, specular;

    public float glossiness;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FMBFrame
{
    public short index, frameNumber, verticesOffset;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FMBObject
{
    //most likely unused
    public char* name;

    public int materialIndex;

    public ushort hasNormals, hasTextures, hasColors;

    public int numKeyFrames;

    public FMBFrame* frames;

    public int numFaces;

    public byte* indices;

    public int numVertices;

    public byte* vertices, normals, textures, colors;

    public Vec3* centers;

    public float* radiuses;

    public ushort* keyFrameLookUp;
}

public enum FMBDataType
{
    FMB_BYTE, FMB_UNSIGNED_BYTE, FMB_SHORT, FMB_UNSIGNED_SHORT, FMB_FLOAT
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FMB
{
    public float version;

    public FMBDataType indexDataType, vertexDataType, normalDataType, textureDataType, colorDataType;

    public int indexDataSize, vertexDataSize, normalDataSize, textureDataSize, colorDataSize;

    public float offset, scale;

    public int numFrames, numMaterials;

    public float inverseScale;

    public FMBMaterial* materials;

    public int numObjects;

    public FMBObject* objects;

    public Vec3* mins, maxes;
}