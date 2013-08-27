using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;

namespace NiCAM_CSharpClient
{
    class BitmapReceiver
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, uint count);
        MemoryMappedFile file;
        MemoryMappedViewAccessor memoryAccessor;
        int fileSize = (1280 * 1024 * 3) + 3;

        enum Res 
        {
            SD = 0,
            HD54 = 1,
            HD43 = 2,
        }

        public BitmapReceiver()
        {
            file = MemoryMappedFile.CreateOrOpen("OpenNiVirtualCamFrameData", fileSize, MemoryMappedFileAccess.ReadWrite);
            memoryAccessor = file.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.ReadWrite);
        }

        public unsafe Bitmap ReadBitmap()
        {
            Res res = (Res)memoryAccessor.ReadByte(fileSize - 3);
            int w, h;
            switch (res)
            {
                case Res.SD:
                    w = 640;
                    h = 480;
                    break;
                case Res.HD54:
                    w = 1280;
                    h = 1024;
                    break;
                case Res.HD43:
                    w = 1280;
                    h = 960;
                    break;
                default:
                    throw new ArgumentException("Bad image size");
            }
            int Size = w * h * 3;
            Bitmap image = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            BitmapData bitData = image.LockBits(
                new Rectangle(new Point(0, 0), image.Size),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            memoryAccessor.Write(fileSize - 2, (byte)1);
            byte* memAddress = (byte*)0;
            memoryAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref memAddress);
            memcpy(bitData.Scan0, new IntPtr(memAddress), (uint)Size);
            memoryAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
            if (bitData != null)
                image.UnlockBits(bitData);
            return image;
        }

        public bool hasServer()
        {
            if (file != null && memoryAccessor != null)
            {
                memoryAccessor.Write(fileSize - 1, (byte)0);
                int tried = 0;
                while (tried < 10)
                {
                    if (memoryAccessor.ReadByte(fileSize - 1) != 0)
                        return true;
                    System.Threading.Thread.Sleep(100);
                    tried++;
                }
            }
            return false;
        }
    }
}
