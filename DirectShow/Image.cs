using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Redbox.DirectShow
{
    public static class Image
    {
        public static bool IsGrayscale(Bitmap image)
        {
            bool flag = false;
            if (image.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                flag = true;
                ColorPalette palette = image.Palette;
                for (int index = 0; index < 256; ++index)
                {
                    Color entry = palette.Entries[index];
                    if ((int)entry.R != index || (int)entry.G != index || (int)entry.B != index)
                    {
                        flag = false;
                        break;
                    }
                }
            }
            return flag;
        }

        public static Bitmap CreateGrayscaleImage(int width, int height)
        {
            Bitmap image = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            Image.SetGrayscalePalette(image);
            return image;
        }

        public static void SetGrayscalePalette(Bitmap image)
        {
            ColorPalette colorPalette = image.PixelFormat == PixelFormat.Format8bppIndexed ? image.Palette : throw new UnsupportedImageFormatException("Source image is not 8 bpp image.");
            for (int index = 0; index < 256; ++index)
                colorPalette.Entries[index] = Color.FromArgb(index, index, index);
            image.Palette = colorPalette;
        }

        public static Bitmap Clone(Bitmap source, PixelFormat format)
        {
            if (source.PixelFormat == format)
                return Image.Clone(source);
            int width = source.Width;
            int height = source.Height;
            Bitmap bitmap = new Bitmap(width, height, format);
            Graphics graphics = Graphics.FromImage((System.Drawing.Image)bitmap);
            graphics.DrawImage((System.Drawing.Image)source, 0, 0, width, height);
            graphics.Dispose();
            return bitmap;
        }

        public static Bitmap Clone(Bitmap source)
        {
            BitmapData bitmapData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);
            Bitmap bitmap = Image.Clone(bitmapData);
            source.UnlockBits(bitmapData);
            if (source.PixelFormat == PixelFormat.Format1bppIndexed || source.PixelFormat == PixelFormat.Format4bppIndexed || source.PixelFormat == PixelFormat.Format8bppIndexed || source.PixelFormat == PixelFormat.Indexed)
            {
                ColorPalette palette1 = source.Palette;
                ColorPalette palette2 = bitmap.Palette;
                int length = palette1.Entries.Length;
                for (int index = 0; index < length; ++index)
                    palette2.Entries[index] = palette1.Entries[index];
                bitmap.Palette = palette2;
            }
            return bitmap;
        }

        public static Bitmap Clone(BitmapData sourceData)
        {
            int width = sourceData.Width;
            int height = sourceData.Height;
            Bitmap bitmap = new Bitmap(width, height, sourceData.PixelFormat);
            BitmapData bitmapdata = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            SystemTools.CopyUnmanagedMemory(bitmapdata.Scan0, sourceData.Scan0, height * sourceData.Stride);
            bitmap.UnlockBits(bitmapdata);
            return bitmap;
        }

        public static Bitmap FromFile(string fileName)
        {
            FileStream fileStream = (FileStream)null;
            try
            {
                fileStream = File.OpenRead(fileName);
                MemoryStream memoryStream = new MemoryStream();
                byte[] buffer = new byte[10000];
                while (true)
                {
                    int count = fileStream.Read(buffer, 0, 10000);
                    if (count != 0)
                        memoryStream.Write(buffer, 0, count);
                    else
                        break;
                }
                return (Bitmap)System.Drawing.Image.FromStream((Stream)memoryStream);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }
        }

        public static unsafe Bitmap Convert16bppTo8bpp(Bitmap bimap)
        {
            int width = bimap.Width;
            int height = bimap.Height;
            Bitmap bitmap;
            int num1;
            switch (bimap.PixelFormat)
            {
                case PixelFormat.Format16bppGrayScale:
                    bitmap = Image.CreateGrayscaleImage(width, height);
                    num1 = 1;
                    break;
                case PixelFormat.Format48bppRgb:
                    bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                    num1 = 3;
                    break;
                case PixelFormat.Format64bppPArgb:
                    bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
                    num1 = 4;
                    break;
                case PixelFormat.Format64bppArgb:
                    bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    num1 = 4;
                    break;
                default:
                    throw new UnsupportedImageFormatException("Invalid pixel format of the source image.");
            }
            BitmapData bitmapdata1 = bimap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bimap.PixelFormat);
            BitmapData bitmapdata2 = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr scan0 = bitmapdata1.Scan0;
            byte* pointer1 = (byte*)scan0.ToPointer();
            scan0 = bitmapdata2.Scan0;
            byte* pointer2 = (byte*)scan0.ToPointer();
            int stride1 = bitmapdata1.Stride;
            int stride2 = bitmapdata2.Stride;
            for (int index = 0; index < height; ++index)
            {
                ushort* numPtr1 = (ushort*)(pointer1 + index * stride1);
                byte* numPtr2 = pointer2 + index * stride2;
                int num2 = 0;
                int num3 = width * num1;
                while (num2 < num3)
                {
                    *numPtr2 = (byte)((uint)*numPtr1 >> 8);
                    ++num2;
                    ++numPtr1;
                    ++numPtr2;
                }
            }
            bimap.UnlockBits(bitmapdata1);
            bitmap.UnlockBits(bitmapdata2);
            return bitmap;
        }

        public static unsafe Bitmap Convert8bppTo16bpp(Bitmap bimap)
        {
            int width = bimap.Width;
            int height = bimap.Height;
            Bitmap bitmap;
            int num1;
            switch (bimap.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    bitmap = new Bitmap(width, height, PixelFormat.Format48bppRgb);
                    num1 = 3;
                    break;
                case PixelFormat.Format8bppIndexed:
                    bitmap = new Bitmap(width, height, PixelFormat.Format16bppGrayScale);
                    num1 = 1;
                    break;
                case PixelFormat.Format32bppPArgb:
                    bitmap = new Bitmap(width, height, PixelFormat.Format64bppPArgb);
                    num1 = 4;
                    break;
                case PixelFormat.Format32bppArgb:
                    bitmap = new Bitmap(width, height, PixelFormat.Format64bppArgb);
                    num1 = 4;
                    break;
                default:
                    throw new UnsupportedImageFormatException("Invalid pixel format of the source image.");
            }
            BitmapData bitmapdata1 = bimap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, bimap.PixelFormat);
            BitmapData bitmapdata2 = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr scan0 = bitmapdata1.Scan0;
            byte* pointer1 = (byte*)scan0.ToPointer();
            scan0 = bitmapdata2.Scan0;
            byte* pointer2 = (byte*)scan0.ToPointer();
            int stride1 = bitmapdata1.Stride;
            int stride2 = bitmapdata2.Stride;
            for (int index = 0; index < height; ++index)
            {
                byte* numPtr1 = pointer1 + index * stride1;
                ushort* numPtr2 = (ushort*)(pointer2 + index * stride2);
                int num2 = 0;
                int num3 = width * num1;
                while (num2 < num3)
                {
                    *numPtr2 = (ushort)((uint)*numPtr1 << 8);
                    ++num2;
                    ++numPtr1;
                    ++numPtr2;
                }
            }
            bimap.UnlockBits(bitmapdata1);
            bitmap.UnlockBits(bitmapdata2);
            return bitmap;
        }
    }
}
