/*  
    Copyright (C) 2013  Soroush Falahati - soroush@falahati.net

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see [http://www.gnu.org/licenses/].
    */

namespace NiUI
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO.MemoryMappedFiles;
    using System.Runtime.InteropServices;
    using System.Threading;

    internal class BitmapBroadcaster
    {
        private const int FileSize = (1280 * 1024 * 3) + 3;

        private readonly MemoryMappedFile file;

        private readonly int lastChecked = Environment.TickCount;

        private readonly MemoryMappedViewAccessor memoryAccessor;

        private bool? hasClient;

        public BitmapBroadcaster()
        {
            this.file = MemoryMappedFile.CreateOrOpen(
                "OpenNiVirtualCamFrameData",
                FileSize,
                MemoryMappedFileAccess.ReadWrite);
            this.memoryAccessor = this.file.CreateViewAccessor(0, FileSize, MemoryMappedFileAccess.ReadWrite);
            if (this.HasServer())
            {
                throw new Exception("Only one server is allowed.");
            }
        }

        public bool HasClient
        {
            get
            {
                if (this.file != null && this.memoryAccessor != null)
                {
                    if (this.hasClient == null || Environment.TickCount - this.lastChecked > 2000)
                    {
                        this.hasClient = this.memoryAccessor.ReadByte(FileSize - 2) == 1;
                        this.memoryAccessor.Write(FileSize - 2, (byte)0);
                    }
                    return this.hasClient.Value;
                }
                return false;
            }
        }

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl,
            SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, uint count);

        public bool ClearScreen()
        {
            Bitmap bi = new Bitmap(640, 480, PixelFormat.Format24bppRgb);
            return this.SendBitmap(bi);
        }

        public unsafe bool SendBitmap(Bitmap image)
        {
            lock (image)
            {
                bool hd54 = image.Width == 1280 && image.Height == 1024;
                bool hd43 = image.Width == 1280 && image.Height == 960;
                int size = 640 * 480 * 3;
                if (hd54)
                {
                    size = 1280 * 1024 * 3;
                }
                else if (hd43)
                {
                    size = 1280 * 960 * 3;
                }
                if (!(image.Width == 640 && image.Height == 480) && !hd54 && !hd43)
                {
                    throw new ArgumentException("Bad image size");
                }
                if (image.PixelFormat != PixelFormat.Format24bppRgb)
                {
                    throw new ArgumentException("Bad image format");
                }
                BitmapData bitData = image.LockBits(
                    new Rectangle(new Point(0, 0), image.Size),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);
                this.memoryAccessor.Write(FileSize - 1, (byte)1);
                if (hd54)
                {
                    this.memoryAccessor.Write(FileSize - 3, (byte)1);
                }
                else if (hd43)
                {
                    this.memoryAccessor.Write(FileSize - 3, (byte)2);
                }
                else
                {
                    this.memoryAccessor.Write(FileSize - 3, (byte)0);
                }
                byte* memAddress = (byte*)0;
                this.memoryAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref memAddress);
                memcpy(new IntPtr(memAddress), bitData.Scan0, (uint)size);
                this.memoryAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                image.UnlockBits(bitData);
            }
            return true;
        }

        public bool HasServer()
        {
            if (this.file != null && this.memoryAccessor != null)
            {
                this.memoryAccessor.Write(FileSize - 1, (byte)0);
                int tried = 0;
                while (tried < 10)
                {
                    if (this.memoryAccessor.ReadByte(FileSize - 1) != 0)
                    {
                        return true;
                    }
                    Thread.Sleep(100);
                    tried++;
                }
            }
            return false;
        }
    }
}