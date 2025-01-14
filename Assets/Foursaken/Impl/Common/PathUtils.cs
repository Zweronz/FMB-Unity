using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PathUtils
{
    public static string RelativeStreamingPath(string original)
    {
        List<string> streamingPath = Application.streamingAssetsPath.Split('/').ToList();
        string[] originalPath = original.Split('/');

        for (int i = 1; i < originalPath.Length; i++)
        {
            streamingPath.Add(originalPath[i]);
        }

        return string.Join('/', streamingPath);
    }
}
