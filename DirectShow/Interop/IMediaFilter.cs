using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace Redbox.DirectShow.Interop
{
    [SuppressUnmanagedCodeSecurity]
    [Guid("56a86899-0ad4-11ce-b03a-0020af0ba770")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    internal interface IMediaFilter : IPersist
    {
        [MethodImpl(MethodImplOptions.PreserveSig)]
        new int GetClassID(out Guid pClassID);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int Stop();

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int Pause();

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int Run([In] long tStart);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int GetState([In] int dwMilliSecsTimeout, out FilterState filtState);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int SetSyncSource([In] IReferenceClock pClock);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int GetSyncSource(out IReferenceClock pClock);
    }
}
