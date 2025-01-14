using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

[Serializable]
public class FMB2XML : IFMXMLReadable
{
    public void Read(XmlNode node)
    {
        foreach (XmlNode childNode in node["data"].ChildNodes)
        {
            switch (childNode.Name)
            {
                case "animations":
                    foreach (XmlNode animationNode in childNode.ChildNodes)
                    {
                        if (!animationNode.Name.StartsWith("#"))
                        {
                            FMB2XMLAnimation animation = (FMB2XMLAnimation)FMXML.ReadValue(typeof(FMB2XMLAnimation), animationNode);
                            animation.name = animationNode.Name;

                            animations.Add(animation);
                        }
                    }
                    break;

                case "materialSets":
                    foreach (XmlNode materialNode in childNode.ChildNodes)
                    {
                        if (materialNode.Name == "materials")
                        {
                            FMB2XMLMaterial material = (FMB2XMLMaterial)FMXML.ReadValue(typeof(FMB2XMLMaterial), materialNode);

                            foreach (XmlNode childMaterialNode in materialNode.ChildNodes)
                            {
                                if (childMaterialNode.Name == "maps")
                                {
                                    FMB2XMLTextureMap textureMap = (FMB2XMLTextureMap)FMXML.ReadValue(typeof(FMB2XMLTextureMap), childMaterialNode);
                                    List<float> xTilings = new List<float>(), yTilings = new List<float>(xTilings);

                                    foreach (XmlNode childTextureNode in childMaterialNode.ChildNodes)
                                    {
                                        switch (childTextureNode.Name)
                                        {
                                            case "mixes":
                                                textureMap.mixTextures.Add(childTextureNode.InnerText);
                                                break;

                                            case "mixTilingsX":
                                                xTilings.Add(float.Parse(childTextureNode.InnerText));
                                                break;

                                            case "mixTilingsY":
                                                yTilings.Add(float.Parse(childTextureNode.InnerText));
                                                break;
                                        }
                                    }

                                    for (int i = 0; i < xTilings.Count; i++)
                                    {
                                        textureMap.mixTilings.Add(new Vector2(xTilings[i], yTilings[i]));
                                    }

                                    material.textureMaps.Add(textureMap);
                                }
                            }

                            materials.Add(material);
                        }
                    }
                    break;

                case "renderObjects":
                    objects.Add((FMB2XMLRenderObject)FMXML.ReadValue(typeof(FMB2XMLRenderObject), childNode));
                    break;

                case "textureSets":
                    FMB2XMLTextureSets textureSet = new FMB2XMLTextureSets();

                    foreach (XmlNode textureNode in childNode.ChildNodes)
                    {
                        if (textureNode.Name == "texture")
                        {
                            textureSet.textures.Add(textureNode.InnerText);
                        }
                    }

                    textures.Add(textureSet);
                    break;
            }
        }
    }

    public List<FMB2XMLAnimation> animations = new List<FMB2XMLAnimation>();

    public List<FMB2XMLMaterial> materials = new List<FMB2XMLMaterial>();

    public List<FMB2XMLRenderObject> objects = new List<FMB2XMLRenderObject>();

    public List<FMB2XMLTextureSets> textures = new List<FMB2XMLTextureSets>();

    [Serializable]
    public class FMB2XMLAnimation
    {
        public string name;

        public int start, end;
    }

    [Serializable]
    public class FMB2XMLMaterial
    {
        public bool alphaOverride;

        public FMB2XMLColor ambient, diffuse, specular;

        public float glossiness;

        public bool emission, hasAlpha;

        public List<FMB2XMLTextureMap> textureMaps = new List<FMB2XMLTextureMap>();

        public string name;

        public float specularIntensity;

        public bool forceUnboundVertexLighting;
    }

    [Serializable]
    public class FMB2XMLColor
    {
        public float x, y, z;
    }

    [Serializable]
    public class FMB2XMLTextureMap
    {
        public int channel;

        public string name, texture;

        public int method;

        public string mixMask;

        public int mixMaskChannel;

        public List<Vector2> mixTilings = new List<Vector2>();

        public List<string> mixTextures = new List<string>();
    }

    [Serializable]
    public class FMB2XMLRenderObject
    {
        public int index;

        public string info;

        public int lightmapChannel;

        public string lightmapTexture, shader;
    }

    [Serializable]

    public class FMB2XMLTextureSets
    {
        public List<string> textures = new List<string>();
    }
}
