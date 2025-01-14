using System;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

[Serializable]
public class FMD : IFMXMLReadable
{
    public void Read(XmlNode node)
    {
        foreach (XmlNode childNode in node["data"].ChildNodes)
        {
            switch (childNode.Name)
            {
                case "materials":
                    materials.Add((FMDMaterial)FMXML.ReadValue(typeof(FMDMaterial), childNode));
                    break;

                case "animations":
                    foreach (XmlNode animationNode in childNode.ChildNodes)
                    {
                        if (!animationNode.Name.StartsWith("#"))
                        {
                            FMDAnimationClip clip = (FMDAnimationClip)FMXML.ReadValue(typeof(FMDAnimationClip), animationNode);
                            clip.name = animationNode.Name;

                            animations.Add(clip);
                        }
                    }
                    break;

                case "soundframes":
                    foreach (XmlNode soundNode in childNode.ChildNodes)
                    {
                        if (soundNode.Name == "sound")
                        {
                            soundFrames.Add((FMDSoundFrame)FMXML.ReadValue(typeof(FMDSoundFrame), soundNode));
                        }
                    }
                    break;

                case "effectframes":
                    foreach (XmlNode effectNode in childNode.ChildNodes)
                    {
                        if (effectNode.Name == "effect")
                        {
                            effectFrames.Add((FMDEffectFrame)FMXML.ReadValue(typeof(FMDEffectFrame), effectNode));
                        }
                    }
                    break;

                case "hitsound":
                    hitSounds.Add(childNode.InnerText);
                    break;

                case "fallsound":
                    fallSounds.Add(childNode.InnerText);
                    break;

                case "usesound":
                    useSounds.Add(childNode.InnerText);
                    break;

                case "shootsound":
                    shootSounds.Add(childNode.InnerText);
                    break;

                case "loopsound":
                    loopSounds.Add(childNode.InnerText);
                    break;
            }
        }
    }


    public List<FMDMaterial> materials = new List<FMDMaterial>();

    public List<FMDAnimationClip> animations = new List<FMDAnimationClip>();

    public List<FMDSoundFrame> soundFrames = new List<FMDSoundFrame>();

    public List<FMDEffectFrame> effectFrames = new List<FMDEffectFrame>();

    public List<string> hitSounds = new List<string>(),
    fallSounds = new List<string>(), useSounds = new List<string>(),
    shootSounds = new List<string>(), loopSounds = new List<string>();

    [Serializable]
    public class FMDMaterial
    {
        public int index;

        public string name;

        //I have no clue why it's called "textures" and not "texture", there's never more than one texture.
        public string textures;

        public bool hasAlpha, ignoreLighting;

        public FMDColor ambient, diffuse, specular;

        public float glossiness;

        public bool combinable;
    }

    [Serializable]
    public class FMDAnimationClip
    {
        public string name;

        public int start, end, hitframe1, hitframe2, hitframe3, hitframe4, type;

        public string graphicsRequirement;

        public int death, hit1;
    }

    [Serializable]
    public class FMDSoundFrame
    {
        public int frame;

        public string s1, s2, s3;
    }

    [Serializable]
    public class FMDEffectFrame
    {
        public int frame;

        public string type;
    }

    [Serializable]
    public struct FMDColor
    {
        public float r, g, b;
    }
}