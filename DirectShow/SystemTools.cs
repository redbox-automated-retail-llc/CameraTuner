using System;
using System.Runtime.InteropServices;

namespace Redbox.DirectShow
{
    public static class SystemTools
    {
        public static unsafe IntPtr CopyUnmanagedMemory(IntPtr dst, IntPtr src, int count)
        {
            SystemTools.CopyUnmanagedMemory((byte*)dst.ToPointer(), (byte*)src.ToPointer(), count);
            return dst;
        }

        public static unsafe byte* CopyUnmanagedMemory(byte* dst, byte* src, int count)
        {
            return SystemTools.memcpy(dst, src, count);
        }

        public static unsafe IntPtr SetUnmanagedMemory(IntPtr dst, int filler, int count)
        {
            SystemTools.SetUnmanagedMemory((byte*)dst.ToPointer(), filler, count);
            return dst;
        }

        public static unsafe byte* SetUnmanagedMemory(byte* dst, int filler, int count)
        {
            return SystemTools.memset(dst, filler, count);
        }

        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe byte* memcpy(byte* dst, byte* src, int count);

        [DllImport("ntdll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe byte* memset(byte* dst, int filler, int count);
    }
}
