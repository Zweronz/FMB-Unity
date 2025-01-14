using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "fmb")]
public unsafe class FMBImporter : ScriptedImporter
{
    private static Shader OpaqueShader
    {
        get
        {
            if (opaqueShader == null) opaqueShader = Resources.Load<Shader>("FMB Diffuse");

            return opaqueShader;
        }
    }

    private static Shader TransparentShader
    {
        get
        {
            if (transparentShader == null) transparentShader = Resources.Load<Shader>("FMB Transparent Diffuse");

            return transparentShader;
        }
    }

    private static Shader opaqueShader;

    private static Shader transparentShader;

    //todo: pair fmbs with fmds! (there is no way I'm implementing that in the native library)
    public override void OnImportAsset(AssetImportContext ctx)
    {
        Model* model;

        fixed (byte* path = System.Text.Encoding.UTF8.GetBytes(ctx.assetPath + '\0'))
        {
            model = FMNative.load_model((char*)path);
        }

        if (model->header != FMNative.FMBHeader)
        {
            Debug.LogError(Path.GetFileNameWithoutExtension(ctx.assetPath) + " does not match the FMB format!");
            FMNative.delete_model(model);

            return;
        }

        FMB* fmb = (FMB*)model->ptr;
        string modelName = Path.GetFileNameWithoutExtension(ctx.assetPath);

        GameObject root = new GameObject(modelName);
        FMD fmd = null;

        ctx.AddObjectToAsset(modelName, root);

        Material[] materials = new Material[fmb->numMaterials];
        string parentPath = Path.GetDirectoryName(ctx.assetPath);

        string fmdPath = Path.Combine(parentPath, modelName + ".fmd");

        if (!File.Exists(fmdPath))
        {
            Debug.LogWarning(modelName + ".fmb has no corresponding fmd file! the materials are bound to be off. please import the corresponding .fmd file and reimport " + modelName + ".fmb");
        }
        else
        {
            fmd = FMXML.Read<FMD>(File.ReadAllText(fmdPath));
        }

        if (fmb->numMaterials > 0)
        {
            for (int i = 0; i < fmb->numMaterials; i++)
            {
                if (fmd == null || (fmd != null && fmd.materials.Find(x => x.index == i) == null))
                {
                    string path = Marshal.PtrToStringUTF8((IntPtr)fmb->materials[i].texturePath);

                    materials[i] = new Material(Path.GetExtension(path) == ".png" ? TransparentShader : OpaqueShader)
                    {
                        name = Marshal.PtrToStringUTF8((IntPtr)fmb->materials[i].name),
                    };

                    materials[i].SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(parentPath, path)));
                    ctx.AddObjectToAsset(materials[i].name + " (" + i + ")", materials[i]);
                }
                else
                {
                    FMD.FMDMaterial material = fmd.materials.Find(x => x.index == i);

                    materials[i] = new Material(material.hasAlpha ? TransparentShader : OpaqueShader)
                    {
                        name = material.name,
                    };

                    if (material.textures != "NA")
                    {
                        materials[i].SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(parentPath, material.textures)));
                    }

                    ctx.AddObjectToAsset(materials[i].name + " (" + i + ")", materials[i]);
                }
            }
        }

        if (fmb->numObjects > 0)
        {
            for (int i = 0; i < fmb->numObjects; i++)
            {
                if (fmb->objects[i].numKeyFrames > 0)
                {
                    GameObject fmbObject = new GameObject("Object " + i);
                    fmbObject.transform.parent = root.transform;

                    fmbObject.transform.localPosition = Vector3.zero;
                    fmbObject.transform.localRotation = Quaternion.identity;
                    fmbObject.transform.localScale = Vector3.one;

                    MeshRenderer renderer = fmbObject.AddComponent<MeshRenderer>();
                    MeshFilter filter = fmbObject.AddComponent<MeshFilter>();

                    renderer.sharedMaterial = materials[fmb->objects[i].materialIndex];
                    Vector3[] vertices = ReadVector3s(fmb->vertexDataType, fmb->objects[i].vertices, fmb->objects[i].numVertices);

                    for (int j = 0; j < fmb->objects[i].numVertices; j++)
                    {
                        vertices[j] = (vertices[j] - (Vector3.one * fmb->offset)) * fmb->inverseScale;
                        vertices[j].x = -vertices[j].x;
                    }

                    Mesh mesh = new Mesh
                    {
                        vertices = vertices,
                        triangles = ReadIndices(fmb->indexDataType, fmb->objects[i].indices, fmb->objects[i].numFaces).Reverse().ToArray(),
                        name = "Object " + i
                    };

                    //if (fmb->objects[i].hasNormals)
                    //{
                    //    Vector3[] normals = ReadVector3s(fmb->normalDataType, fmb->objects[i].normals, fmb->objects[i].numVertices);
//
                    //    for (int j = 0; j < fmb->objects[i].numVertices; j++)
                    //    {
                    //        normals[j].Normalize();
                    //    }
//
                    //    mesh.normals = normals;
                    //}

                    if (fmb->objects[i].hasTextures) mesh.uv = ReadVector2s(fmb->textureDataType, fmb->objects[i].textures, fmb->objects[i].numVertices);
                    if (fmb->objects[i].hasColors) mesh.colors32 = ReadColors(fmb->colorDataType, fmb->objects[i].colors, fmb->objects[i].numVertices);

                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals(); //here for the time being. the normals are being weird

                    ctx.AddObjectToAsset("Object " + i + " Mesh", mesh);
                    filter.sharedMesh = mesh;

                    ctx.AddObjectToAsset("Object " + i, fmbObject);
                }
            }
        }

        root.AddComponent<FMBAnimator>().fmd = fmd;

        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        
        FMNative.delete_model(model);
    }

    //below this point is very clearly extremely readable and probably the best c# code you've ever read in your entire life
    //I might just implement this into the native library at some point because..... yikes.

    private static Vector3[] ReadVector3s(FMBDataType dataType, char* data, int length)
    {
        int index = 0;
        Vector3[] vectors = new Vector3[length];

        for (int i = 0; i < length; i++)
        {
            switch (dataType)
            {
                case FMBDataType.FMB_BYTE:
                    vectors[i] = new Vector3(((byte*)data)[index], ((byte*)data)[index + 1], ((byte*)data)[index + 2]);
                    break;

                case FMBDataType.FMB_SIGNED_BYTE:
                    vectors[i] = new Vector3(((sbyte*)data)[index], ((sbyte*)data)[index + 1], ((sbyte*)data)[index + 2]);
                    break;

                case FMBDataType.FMB_SHORT:
                    vectors[i] = new Vector3(((short*)data)[index], ((short*)data)[index + 1], ((short*)data)[index + 2]);
                    break;

                case FMBDataType.FMB_UNSIGNED_SHORT:
                    vectors[i] = new Vector3(((ushort*)data)[index], ((ushort*)data)[index + 1], ((ushort*)data)[index + 2]);
                    break;

                case FMBDataType.FMB_FLOAT:
                    vectors[i] = new Vector3(((float*)data)[index], ((float*)data)[index + 1], ((float*)data)[index + 2]);
                    break;
            }

            index += 3;
        }

        return vectors;
    }

    private static Vector2[] ReadVector2s(FMBDataType dataType, char* data, int length)
    {
        int index = 0;
        Vector2[] vectors = new Vector2[length];

        for (int i = 0; i < length; i++)
        {
            switch (dataType)
            {
                case FMBDataType.FMB_BYTE:
                    vectors[i] = new Vector2(((byte*)data)[index], ((byte*)data)[index + 1]);
                    break;

                case FMBDataType.FMB_SIGNED_BYTE:
                    vectors[i] = new Vector2(((sbyte*)data)[index], ((sbyte*)data)[index + 1]);
                    break;

                case FMBDataType.FMB_SHORT:
                    vectors[i] = new Vector2(((short*)data)[index], ((short*)data)[index + 1]);
                    break;

                case FMBDataType.FMB_UNSIGNED_SHORT:
                    vectors[i] = new Vector2(((ushort*)data)[index], ((ushort*)data)[index + 1]);
                    break;

                case FMBDataType.FMB_FLOAT:
                    vectors[i] = new Vector2(((float*)data)[index], ((float*)data)[index + 1]);
                    break;
            }

            index += 2;
        }

        return vectors;
    }

    private static Color32[] ReadColors(FMBDataType dataType, char* data, int length)
    {
        int index = 0;
        Color32[] colors = new Color32[length];

        for (int i = 0; i < length; i++)
        {
            switch (dataType)
            {
                case FMBDataType.FMB_BYTE:
                    colors[i] = new Color32(((byte*)data)[index], ((byte*)data)[index + 1], ((byte*)data)[index + 2], ((byte*)data)[index + 3]);
                    break;

                case FMBDataType.FMB_SIGNED_BYTE:
                    colors[i] = new Color32((byte)((sbyte*)data)[index], (byte)((sbyte*)data)[index + 1], (byte)((sbyte*)data)[index + 2], (byte)((sbyte*)data)[index + 3]);
                    break;

                case FMBDataType.FMB_SHORT:
                    colors[i] = new Color32((byte)((short*)data)[index], (byte)((short*)data)[index + 1], (byte)((short*)data)[index + 2], (byte)((short*)data)[index + 3]);
                    break;

                case FMBDataType.FMB_UNSIGNED_SHORT:
                    colors[i] = new Color32((byte)((ushort*)data)[index], (byte)((ushort*)data)[index + 1], (byte)((ushort*)data)[index + 2], (byte)((ushort*)data)[index + 3]);
                    break;

                case FMBDataType.FMB_FLOAT:
                    colors[i] = (Color32)new Color(((float*)data)[index], ((float*)data)[index + 1], ((float*)data)[index + 2], ((float*)data)[index + 3]);
                    break;
            }

            index += 4;
        }

        return colors;
    }

    private static int[] ReadIndices(FMBDataType dataType, char* data, int length)
    {
        int fullLength = length * 3;
        int[] indices = new int[fullLength];

        for (int i = 0; i < fullLength; i++)
        {
            switch (dataType)
            {
                case FMBDataType.FMB_BYTE:
                    indices[i] = ((byte*)data)[i];
                    break;

                case FMBDataType.FMB_SIGNED_BYTE:
                    indices[i] = ((sbyte*)data)[i];
                    break;

                case FMBDataType.FMB_SHORT:
                    indices[i] = ((short*)data)[i];
                    break;

                case FMBDataType.FMB_UNSIGNED_SHORT:
                    indices[i] = ((ushort*)data)[i];
                    break;

                case FMBDataType.FMB_FLOAT:
                    //idk why this would ever happen but ¯\_(ツ)_/¯
                    indices[i] = (int)((float*)data)[i];
                    break;
            }
        }

        return indices;
    }
}
