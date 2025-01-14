using System.Runtime.InteropServices;
using UnityEngine;

public enum FMB2VertexChannelType
{
    UVChannel0, UVChannel1, UVChannel2, UVChannel3, 
    Position, Normal, Tangent, Binormal, Color
};

//unsure if this is correct
public enum FMB2DataType
{
    FMB2_BYTE, FMB2_UNSIGNED_BYTE,
    FMB2_SHORT, FMB2_UNSIGNED_SHORT,
    FMB2_INT, FMB2_UNSIGNED_INT,
    FMB2_FLOAT
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FMB2VertexChannel
{
    public FMB2VertexChannelType exportedType;

    public int dataType, dataSize, numComponents, numOffsets;

    public char* data;

    public ushort* keyFrameToOffset;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FMB2Model
{
    public char* name;

    public int materialIndex, numFaces, numVertices, indexDataType, indexDataSize, numKeyFrames;

    public char* indices;

    public int numChannels;

    public FMB2VertexChannel* channels;

    public int numBoundingOffsets;

    public Vec4* boundingSpheres;

    public Vec3* mins, maxes;

    public ushort* boundingOffsetToKeyFrame;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FMB2Dummy
{
    public char* name, frameData;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct FMB2
{
    public int chunkCount;

    public float version, offset, scale;

    public int numKeyFrames, numFrames;

    public ushort* frameToKeyFrame, keyFrameToFrameNumber;

    public int numMaterials, numModels, numDummies;

    public FMB2Model* models;

    public FMB2Dummy* dummies;
};
