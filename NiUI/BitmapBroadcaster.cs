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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;

namespace NiUI
{
    internal class BitmapBroadcaster
    {
        private const int FileSize = 1280 * 1024 * 3 + 3;

        private readonly MemoryMappedFile _file;

        private readonly int _lastChecked = Environment.TickCount;

        private readonly MemoryMappedViewAccessor _memoryAccessor;

        private bool? _hasClient;

        public BitmapBroadcaster()
        {
            _file = MemoryMappedFile.CreateOrOpen(
                "OpenNiVirtualCamFrameData",
                FileSize,
                MemoryMappedFileAccess.ReadWrite);
            _memoryAccessor = _file.CreateViewAccessor(0, FileSize, MemoryMappedFileAccess.ReadWrite);

            if (HasServer())
            {
                throw new Exception("Only one server is allowed.");
            }
        }

        public bool HasClient
        {
            get
            {
                if (_file != null && _memoryAccessor != null)
                {
                    if (_hasClient == null || Environment.TickCount - _lastChecked > 2000)
                    {
                        _hasClient = _memoryAccessor.ReadByte(FileSize - 2) == 1;
                        _memoryAccessor.Write(FileSize - 2, (byte) 0);
                    }

                    return _hasClient.Value;
                }

                return false;
            }
        }

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl,
            SetLastError = false)]
        public static extern IntPtr MemoryCopy(IntPtr destination, IntPtr src, uint count);

        public bool ClearScreen()
        {
            var bi = new Bitmap(640, 480, PixelFormat.Format24bppRgb);

            return SendBitmap(bi);
        }

        public bool HasServer()
        {
            if (_file != null && _memoryAccessor != null)
            {
                _memoryAccessor.Write(FileSize - 1, (byte) 0);
                var tried = 0;

                while (tried < 10)
                {
                    if (_memoryAccessor.ReadByte(FileSize - 1) != 0)
                    {
                        return true;
                    }

                    Thread.Sleep(100);
                    tried++;
                }
            }

            return false;
        }

        public unsafe bool SendBitmap(Bitmap image)
        {
            lock (image)
            {
                var hd54 = image.Width == 1280 && image.Height == 1024;
                var hd43 = image.Width == 1280 && image.Height == 960;
                var size = 640 * 480 * 3;

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

                var bitData = image.LockBits(
                    new Rectangle(new Point(0, 0), image.Size),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);
                _memoryAccessor.Write(FileSize - 1, (byte) 1);

                if (hd54)
                {
                    _memoryAccessor.Write(FileSize - 3, (byte) 1);
                }
                else if (hd43)
                {
                    _memoryAccessor.Write(FileSize - 3, (byte) 2);
                }
                else
                {
                    _memoryAccessor.Write(FileSize - 3, (byte) 0);
                }

                var memAddress = (byte*) 0;
                _memoryAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref memAddress);
                MemoryCopy(new IntPtr(memAddress), bitData.Scan0, (uint) size);
                _memoryAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                image.UnlockBits(bitData);
            }

            return true;
        }
    }
}