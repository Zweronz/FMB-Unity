using System.Runtime.InteropServices;

public static unsafe class FMNative
{
    [DllImport("libfmb")]
    public static extern void delete_model(Model* model);

    [DllImport("libfmb")]
    public static extern Model* load_model(char* path);

    public const int UMBHeader = 6450549,
    FMBHeader = 6450534, FMB2Header = 845311334,
    FMAHeader = 6384998;
}
