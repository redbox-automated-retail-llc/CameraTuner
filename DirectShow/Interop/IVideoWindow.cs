using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Redbox.DirectShow.Interop
{
    [Guid("56A868B4-0AD4-11CE-B03A-0020AF0BA770")]
    [InterfaceType(ComInterfaceType.InterfaceIsDual)]
    [ComImport]
    internal interface IVideoWindow
    {
        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_Caption(string caption);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_Caption(out string caption);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_WindowStyle(int windowStyle);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_WindowStyle(out int windowStyle);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_WindowStyleEx(int windowStyleEx);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_WindowStyleEx(out int windowStyleEx);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_AutoShow([MarshalAs(UnmanagedType.Bool), In] bool autoShow);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_AutoShow([MarshalAs(UnmanagedType.Bool)] out bool autoShow);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_WindowState(int windowState);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_WindowState(out int windowState);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_BackgroundPalette([MarshalAs(UnmanagedType.Bool), In] bool backgroundPalette);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_BackgroundPalette([MarshalAs(UnmanagedType.Bool)] out bool backgroundPalette);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_Visible([MarshalAs(UnmanagedType.Bool), In] bool visible);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_Visible([MarshalAs(UnmanagedType.Bool)] out bool visible);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_Left(int left);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_Left(out int left);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_Width(int width);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_Width(out int width);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_Top(int top);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_Top(out int top);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_Height(int height);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_Height(out int height);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_Owner(IntPtr owner);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_Owner(out IntPtr owner);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_MessageDrain(IntPtr drain);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_MessageDrain(out IntPtr drain);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_BorderColor(out int color);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_BorderColor(int color);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int get_FullScreenMode([MarshalAs(UnmanagedType.Bool)] out bool fullScreenMode);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int put_FullScreenMode([MarshalAs(UnmanagedType.Bool), In] bool fullScreenMode);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int SetWindowForeground(int focus);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int NotifyOwnerMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int SetWindowPosition(int left, int top, int width, int height);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int GetWindowPosition(out int left, out int top, out int width, out int height);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int GetMinIdealImageSize(out int width, out int height);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int GetMaxIdealImageSize(out int width, out int height);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int GetRestorePosition(out int left, out int top, out int width, out int height);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int HideCursor([MarshalAs(UnmanagedType.Bool), In] bool hideCursor);

        [MethodImpl(MethodImplOptions.PreserveSig)]
        int IsCursorHidden([MarshalAs(UnmanagedType.Bool)] out bool hideCursor);
    }
}
