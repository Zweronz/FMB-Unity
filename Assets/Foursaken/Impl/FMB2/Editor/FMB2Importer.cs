using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "fmb2")]
public unsafe class FMB2Importer : ScriptedImporter
{
    //placeholder
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

    public override void OnImportAsset(AssetImportContext ctx)
    {
        Model* model;

        fixed (byte* path = System.Text.Encoding.UTF8.GetBytes(ctx.assetPath + '\0'))
        {
            model = FMNative.load_model((char*)path);
        }

        if (model->header != FMNative.FMB2Header)
        {
            Debug.LogError(Path.GetFileNameWithoutExtension(ctx.assetPath) + " does not match the FMB format!");
            FMNative.delete_model(model);

            return;
        }

        FMB2* fmb2 = (FMB2*)model->ptr;

        string modelName = Path.GetFileNameWithoutExtension(ctx.assetPath);

        GameObject root = new GameObject(modelName);
        FMB2XML xml = null;

        ctx.AddObjectToAsset(modelName, root);

        string parentPath = Path.GetDirectoryName(ctx.assetPath);
        string xmlPath = Path.Combine(parentPath, modelName + ".xml");

        if (!File.Exists(xmlPath))
        {
            Debug.LogWarning(modelName + ".fmb2 has no corresponding xml file! no materials will be imported. please import the corresponding .xml file and reimport " + modelName + ".fmb2");
        }
        else
        {
            xml = FMXML.Read<FMB2XML>(File.ReadAllText(xmlPath));
        }

        Material[] materials = null;

        if (xml != null)
        {
            materials = new Material[fmb2->numMaterials];
            
            for (int i = 0; i < fmb2->numMaterials; i++)
            {
                //PLACEHOLDER
                materials[i] = new Material(xml.materials[i].hasAlpha ? TransparentShader : OpaqueShader)
                {
                    name = xml.materials[i].name
                };

                FMB2XML.FMB2XMLTextureMap texture = xml.materials[i].textureMaps.Find(x => x.name == "Diffuse Color");

                if (texture != null)
                {
                    materials[i].SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(parentPath, texture.texture)));
                }

                ctx.AddObjectToAsset(materials[i].name  + "(" + i + ")", materials[i]);
            }
        }

        float inverseScale = 1f / fmb2->scale;

        for (int i = 0; i < fmb2->numModels; i++)
        {
            string name = Marshal.PtrToStringUTF8((IntPtr)fmb2->models[i].name);

            GameObject fmb2Object = new GameObject(name);
            fmb2Object.transform.parent = root.transform;

            fmb2Object.transform.localPosition = Vector3.zero;
            fmb2Object.transform.localRotation = Quaternion.identity;
            fmb2Object.transform.localScale = Vector3.one;

            MeshFilter filter = fmb2Object.AddComponent<MeshFilter>();
            MeshRenderer renderer = fmb2Object.AddComponent<MeshRenderer>();
            
            if (xml != null)
            {
                renderer.sharedMaterial = materials[fmb2->models[i].materialIndex];
            }

            Mesh mesh = new Mesh();
            FMB2VertexChannel? positionChannel = GetChannel(fmb2->models[i], FMB2VertexChannelType.Position);

            string fullName = name + " (" + i + ")";

            if (positionChannel != null)
            {
                Vector3[] vertices = ReadVector3s(fmb2->models[i].numVertices, positionChannel.Value);

                for (int j = 0; j < vertices.Length; j++)
                {
                    vertices[j] = (vertices[j] - (Vector3.one * fmb2->offset)) * inverseScale;
                    vertices[j].x = -vertices[j].x;
                }

                mesh.vertices = vertices;

                //normals will be skipped for the time being
                //unity doesn't allow setting binormals
                //tangents are 3 elements instead of four(?)

                FMB2VertexChannel? colorChannel = GetChannel(fmb2->models[i], FMB2VertexChannelType.Color);

                if (colorChannel != null)
                {
                    mesh.colors32 = ReadColors(fmb2->models[i].numVertices, colorChannel.Value);
                }

                for (int j = 0; j < 4; j++)
                {
                    FMB2VertexChannel? uvChannel = GetChannel(fmb2->models[i], (FMB2VertexChannelType)j);

                    if (uvChannel != null)
                    {
                        mesh.SetUVs(j, ReadVector2s(fmb2->models[i].numVertices, uvChannel.Value));
                    }
                }

                mesh.triangles = ReadIndices(fmb2->models[i]).Reverse().ToArray();
                mesh.name = name + " Mesh";

                mesh.RecalculateBounds();
                mesh.RecalculateNormals(); //here for the time being, the normals are being weird

                ctx.AddObjectToAsset(fullName + " Mesh", mesh);
                filter.sharedMesh = mesh;
            }

            ctx.AddObjectToAsset(fullName, fmb2Object);
        }

        if (model->version >= 1.01f)
        {
            for (int i = 0; i < fmb2->numDummies; i++)
            {
                string name = Marshal.PtrToStringUTF8((IntPtr)fmb2->dummies[i].name);

                GameObject dummy = new GameObject(name);
                dummy.transform.parent = root.transform;

                dummy.transform.localPosition = ((Vec3*)fmb2->dummies[i].frameData)[0].ToUnityVector();
                dummy.transform.localRotation = Quaternion.Euler(((Vec3*)fmb2->dummies[i].frameData)[1].ToUnityVector());

                dummy.transform.localPosition = new Vector3(-dummy.transform.localPosition.x, dummy.transform.localPosition.y, dummy.transform.localPosition.z);

                dummy.transform.localScale = Vector3.one;
                ctx.AddObjectToAsset("(DUMMY) " + name + " (" + i + ")", dummy);
            }
        }

        root.AddComponent<FMB2Animator>().xml = xml;

        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        FMNative.delete_model(model);
    }

    private static int[] ReadIndices(FMB2Model model)
    {
        int fullCount = model.numFaces * 3;
        int[] indices = new int[fullCount];
        
        //not using indexDataType. indexDataType is only available in fmb2 versions before 1.03
        for (int i = 0; i < fullCount; i++)
        {
            switch (model.indexDataSize)
            {
                case 1:
                    indices[i] = ((byte*)model.indices)[i];
                    break;

                case 2:
                    indices[i] = ((ushort*)model.indices)[i];
                    break;

                case 4:
                    indices[i] = ((int*)model.indices)[i]; //why, unity? why are negative indices allowed??
                    break;
            }
        }

        return indices;
    }

    private static Vector2[] ReadVector2s(int vertexCount, FMB2VertexChannel channel)
    {
        Vector2[] vectors = new Vector2[vertexCount];
        int index = 0;

        for (int i = 0; i < vertexCount; i++)
        {
            switch ((FMB2DataType)channel.dataType)
            {
                case FMB2DataType.FMB2_BYTE:
                    vectors[i] = new Vector2(((sbyte*)channel.data)[index], ((sbyte*)channel.data)[index + 1]);
                    break;

                case FMB2DataType.FMB2_UNSIGNED_BYTE:
                    vectors[i] = new Vector2(((byte*)channel.data)[index], ((byte*)channel.data)[index + 1]);
                    break;

                case FMB2DataType.FMB2_SHORT:
                    vectors[i] = new Vector2(((short*)channel.data)[index], ((short*)channel.data)[index + 1]);
                    break;

                case FMB2DataType.FMB2_UNSIGNED_SHORT:
                    vectors[i] = new Vector2(((ushort*)channel.data)[index], ((ushort*)channel.data)[index + 1]);
                    break;

                case FMB2DataType.FMB2_INT:
                    vectors[i] = new Vector2(((int*)channel.data)[index], ((int*)channel.data)[index + 1]);
                    break;

                case FMB2DataType.FMB2_UNSIGNED_INT:
                    vectors[i] = new Vector2(((uint*)channel.data)[index], ((uint*)channel.data)[index + 1]);
                    break;

                case FMB2DataType.FMB2_FLOAT:
                    vectors[i] = new Vector2(((float*)channel.data)[index], ((float*)channel.data)[index + 1]);
                    break;
            }

            index += 2;
        }

        return vectors;
    }

    private static Vector3[] ReadVector3s(int vertexCount, FMB2VertexChannel channel)
    {
        Vector3[] vectors = new Vector3[vertexCount];
        int index = 0;

        for (int i = 0; i < vertexCount; i++)
        {
            switch ((FMB2DataType)channel.dataType)
            {
                case FMB2DataType.FMB2_BYTE:
                    vectors[i] = new Vector3(((sbyte*)channel.data)[index], ((sbyte*)channel.data)[index + 1], ((sbyte*)channel.data)[index + 2]);
                    break;

                case FMB2DataType.FMB2_UNSIGNED_BYTE:
                    vectors[i] = new Vector3(((byte*)channel.data)[index], ((byte*)channel.data)[index + 1], ((byte*)channel.data)[index + 2]);
                    break;

                case FMB2DataType.FMB2_SHORT:
                    vectors[i] = new Vector3(((short*)channel.data)[index], ((short*)channel.data)[index + 1], ((short*)channel.data)[index + 2]);
                    break;

                case FMB2DataType.FMB2_UNSIGNED_SHORT:
                    vectors[i] = new Vector3(((ushort*)channel.data)[index], ((ushort*)channel.data)[index + 1], ((ushort*)channel.data)[index + 2]);
                    break;

                case FMB2DataType.FMB2_INT:
                    vectors[i] = new Vector3(((int*)channel.data)[index], ((int*)channel.data)[index + 1], ((int*)channel.data)[index + 2]);
                    break;

                case FMB2DataType.FMB2_UNSIGNED_INT:
                    vectors[i] = new Vector3(((uint*)channel.data)[index], ((uint*)channel.data)[index + 1], ((uint*)channel.data)[index + 2]);
                    break;

                case FMB2DataType.FMB2_FLOAT:
                    vectors[i] = new Vector3(((float*)channel.data)[index], ((float*)channel.data)[index + 1], ((float*)channel.data)[index + 2]);
                    break;
            }

            index += 3;
        }

        return vectors;
    }

    private static Color32[] ReadColors(int vertexCount, FMB2VertexChannel channel)
    {
        Color32[] colors = new Color32[vertexCount];
        int index = 0;

        if (channel.numComponents == 4)
        {
            for (int i = 0; i < vertexCount; i++)
            {
                switch ((FMB2DataType)channel.dataType)
                {
                    case FMB2DataType.FMB2_UNSIGNED_BYTE:
                        colors[i] = new Color32(((byte*)channel.data)[index], ((byte*)channel.data)[index + 1], ((byte*)channel.data)[index + 2], ((byte*)channel.data)[index + 3]);
                        break;

                    case FMB2DataType.FMB2_FLOAT:
                        colors[i] = new Color(((float*)channel.data)[index], ((float*)channel.data)[index + 1], ((float*)channel.data)[index + 2], ((float*)channel.data)[index + 3]);
                        break;
                }

                index += 4;
            }
        }
        else
        {
            for (int i = 0; i < vertexCount; i++)
            {
                switch ((FMB2DataType)channel.dataType)
                {
                    case FMB2DataType.FMB2_UNSIGNED_BYTE:
                        colors[i] = new Color32(((byte*)channel.data)[index], ((byte*)channel.data)[index + 1], ((byte*)channel.data)[index + 2], 255);
                        break;

                    case FMB2DataType.FMB2_FLOAT:
                        colors[i] = new Color(((float*)channel.data)[index], ((float*)channel.data)[index + 1], ((float*)channel.data)[index + 2], 255);
                        break;
                }

                index += 3;
            }
        }

        return colors;
    }

    private static FMB2VertexChannel? GetChannel(FMB2Model model, FMB2VertexChannelType type)
    {
        for (int i = 0; i < model.numChannels; i++)
        {
            if (model.channels[i].exportedType == type)
            {
                return model.channels[i];
            }
        }

        return null;
    }
}
