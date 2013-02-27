using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;

namespace NiUI
{
    class BitmapBroadcaster
    {
        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, uint count);
        MemoryMappedFile file;
        MemoryMappedViewAccessor memoryAccessor;
        int fileSize = (1280 * 1024 * 3) + 3;
        public BitmapBroadcaster()
        {
            file = MemoryMappedFile.CreateOrOpen("OpenNiVirtualCamFrameData", fileSize, MemoryMappedFileAccess.ReadWrite);
            memoryAccessor = file.CreateViewAccessor(0, fileSize, MemoryMappedFileAccess.ReadWrite);
            if (hasServer())
                throw new Exception("Only one server is allowed.");
        }

        public unsafe bool SendBitmap(Bitmap image)
        {
            lock (image)
            {
                bool HD54 = image.Width == 1280 && image.Height == 1024;
                bool HD43 = image.Width == 1280 && image.Height == 960;
                int Size = 640 * 480 * 3;
                if (HD54)
                    Size = 1280 * 1024 * 3;
                else if(HD43)
                    Size = 1280 * 960 * 3;
                if (!(image.Width == 640 && image.Height == 480) && !HD54 && !HD43)
                    throw new ArgumentException("Bad image size");
                if (image.PixelFormat != PixelFormat.Format24bppRgb)
                    throw new ArgumentException("Bad image format");
                BitmapData bitData = image.LockBits(
                    new Rectangle(new Point(0, 0), image.Size),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                memoryAccessor.Write(fileSize - 1, (byte)1);
                if (HD54)
                    memoryAccessor.Write(fileSize - 3, (byte)1);
                else if (HD43)
                    memoryAccessor.Write(fileSize - 3, (byte)2);
                else
                    memoryAccessor.Write(fileSize - 3, (byte)0);
                byte* memAddress = (byte*)0;
                memoryAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref memAddress);
                memcpy(new IntPtr(memAddress), bitData.Scan0, (uint)Size);
                memoryAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                if (bitData != null)
                    image.UnlockBits(bitData);
            }
            return true;
        }

        int lastChecked = Environment.TickCount;
        bool? _hasClient = null;
        public bool hasClient
        {
            get
            {
                if (file != null && memoryAccessor != null)
                {
                    if (_hasClient == null || Environment.TickCount - lastChecked > 2000)
                    {
                        _hasClient = memoryAccessor.ReadByte(fileSize - 2) == 1;
                        memoryAccessor.Write(fileSize - 2, (byte)0);
                    }
                    return _hasClient.Value;
                }
                return false;
            }
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
   