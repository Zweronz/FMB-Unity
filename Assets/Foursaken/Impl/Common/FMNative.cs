using System.Runtime.InteropServices;

public static unsafe class FMNative
{
    [DllImport("libfmb", EntryPoint = "_Z12delete_modelP5Model", CallingConvention = CallingConvention.Cdecl)]
    public static extern void delete_model(Model* model);

    [DllImport("libfmb", EntryPoint = "_Z10load_modelPc", CallingConvention = CallingConvention.Cdecl)]
    public static extern Model* load_model(char* path);

    public const int UMBHeader = 6450549,
    FMBHeader = 6450534, FMB2Header = 845311334,
    FMAHeader = 6384998;
}
