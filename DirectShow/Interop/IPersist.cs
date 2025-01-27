using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Redbox.DirectShow.Interop
{
    [Guid("0000010c-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    [ComImport]
    internal interface IPersist
    {
        [MethodImpl(MethodImplOptions.PreserveSig)]
        int GetClassID(out Guid pClassID);
    }
}
