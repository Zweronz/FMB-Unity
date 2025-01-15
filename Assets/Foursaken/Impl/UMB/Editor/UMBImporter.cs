using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "umb")]
public unsafe class UMBImporter : ScriptedImporter
{
    private static Shader OpaqueShader
    {
        get
        {
            if (opaqueShader == null) opaqueShader = Resources.Load<Shader>("UMB Diffuse");

            return opaqueShader;
        }
    }

    private static Shader TransparentShader
    {
        get
        {
            if (transparentShader == null) transparentShader = Resources.Load<Shader>("UMB Transparent Diffuse");

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

        if (model->header != FMNative.UMBHeader)
        {
            Debug.LogError(Path.GetFileNameWithoutExtension(ctx.assetPath) + " does not match the UMB format!");
            FMNative.delete_model(model);
            
            return;
        }

        UMB* umb = (UMB*)model->ptr;
        string modelName = Path.GetFileNameWithoutExtension(ctx.assetPath);

        GameObject root = new GameObject(modelName);

        ctx.AddObjectToAsset(modelName, root);

        Material[] materials = new Material[umb->numMaterials];
        string parentPath = Path.GetDirectoryName(ctx.assetPath);

        if (umb->numMaterials > 0)
        {
            for (int i = 0; i < umb->numMaterials; i++)
            {
                string path = Marshal.PtrToStringUTF8((IntPtr)umb->materials[i].texturePath);

                materials[i] = new Material(Path.GetExtension(path) == ".png" ? TransparentShader : OpaqueShader)
                {
                    name = Marshal.PtrToStringUTF8((IntPtr)umb->materials[i].name),
                };

                //todo: setup shaders to work with ambient/diffuse/specular/glossiness

                materials[i].SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(parentPath, path)));
                ctx.AddObjectToAsset(materials[i].name + " (" + i + ")", materials[i]);
            }
        }
        
        if (umb->numObjects > 0)
        {
            for (int i = 0; i < umb->numObjects; i++)
            {
                if (umb->objects[i].numKeyFrames > 0)
                {
                    GameObject umbObject = new GameObject("Object " + i);
                    umbObject.transform.parent = root.transform;

                    umbObject.transform.localPosition = Vector3.zero;
                    umbObject.transform.localRotation = Quaternion.identity;
                    umbObject.transform.localScale = Vector3.one;

                    MeshRenderer renderer = umbObject.AddComponent<MeshRenderer>();
                    MeshFilter filter = umbObject.AddComponent<MeshFilter>();

                    renderer.sharedMaterial = materials[umb->objects[i].materialIndex];

                    Vector3[] vertices = new Vector3[umb->objects[i].frames[0].numVertices], normals = new Vector3[umb->objects[i].frames[0].numVertices];
                    Color32[] colors = new Color32[umb->objects[i].frames[0].numColors];
                    Vector2[] coords = new Vector2[umb->objects[i].frames[0].numTextures];

                    for (int j = 0; j < umb->objects[i].frames[0].numVertices; j++)
                    {
                        vertices[j] = umb->objects[i].frames[0].vertex[j].vertex.ToUnityVector();
                        //normals[j] = umb->objects[i].frames[0].normals[j].ToUnityVector();

                        //normals[j].Normalize();

                        if (colors.Length > 0)
                        {
                            colors[j] = umb->objects[i].frames[0].colors[j].ToUnityColor();
                        }

                        if (coords.Length > 0)
                        {
                            coords[j] = umb->objects[i].frames[0].textures[j].ToUnityVector();
                        }
                    }

                    int[] triangles = new int[umb->objects[i].frames[0].numFaces * 3];

                    for (int j = 0; j < umb->objects[i].frames[0].numFaces * 3; j++)
                    {
                        triangles[j] = umb->objects[i].frames[0].indices[j];
                    }

                    Mesh mesh = new Mesh
                    {
                        vertices = vertices,
                        normals = normals,
                        colors32 = colors,
                        uv = coords,
                        triangles = triangles.Reverse().ToArray(),
                        name = "Object " + i
                    };

                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals(); //here for the time being, the normals are being weird

                    ctx.AddObjectToAsset("Object " + i + " Mesh", mesh);
                    filter.sharedMesh = mesh;

                    ctx.AddObjectToAsset("Object " + i, umbObject);
                }
            }
        }

        

        root.AddComponent<UMBAnimator>().path = ctx.assetPath;
        
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        
        FMNative.delete_model(model);
    }
}
