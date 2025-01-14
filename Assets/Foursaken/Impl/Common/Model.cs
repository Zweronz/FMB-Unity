using UnityEngine;

public unsafe struct Model
{
    public int header;

    public float version;

    public void* ptr;
};