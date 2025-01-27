using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Redbox.DirectShow.Interop
{
    [Guid("56A868A6-0Ad4-11CE-B03A-0020AF0BA770")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IFileSourceFilter
    {
        [MethodImpl(MethodImplOptions.PreserveSig)]
        int Load([MarshalAs(UnmanagedType.LPWStr), In] string fileName, [MarshalAs(UnmanagedType.LPStruct), In] AMMediaType mediaType);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string fileName, [MarshalAs(UnmanagedType.LPStruct), Out] AMMediaType mediaType);
    }
}
