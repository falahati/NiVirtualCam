#include "stdafx.h"
#include <stdio.h>
#include <windows.h>
#include <time.h>

class BitmapBroadcaster
{
private:
	static const int fileSize = (1280 * 1024 * 3) + 3;
	HANDLE fileHandle;
	void* file;
	int lastChecked;
    bool* _hasClient;
	unsigned int getTimeStamp()
	{
		return (unsigned int)time(NULL);
	}
public:
	BitmapBroadcaster()
	{
		_hasClient = 0;
		lastChecked = getTimeStamp();
		fileHandle = OpenFileMapping(FILE_MAP_ALL_ACCESS, FALSE, L"OpenNiVirtualCamFrameData");
		if (fileHandle == NULL)
			fileHandle = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, fileSize, L"OpenNiVirtualCamFrameData");
		file = MapViewOfFile(fileHandle, FILE_MAP_ALL_ACCESS, 0, 0, fileSize);  
		if (hasServer())
			throw "Only one server is allowed.";
	}

	bool ClearScreen()
    {
		void* bi = malloc(640 * 480 * 3);
		memset(bi, 0, 640 * 480 * 3);
        bool ret = SendBitmap(bi, 640, 480);
		free(bi);
		return ret;
    }

	bool SendBitmap(const void* image, int w, int h)
    {
		bool SD = w == 640 && h == 480;
        bool HD54 = w == 1280 && h == 1024;
        bool HD43 = w == 1280 && h == 960;
        int Size = w * h * 3;
        if (!SD && !HD54 && !HD43)
			throw "Bad image size";
		char *resByte = ((char*)file + fileSize - 3);
        if (HD54)
            *resByte = 1;
        else if (HD43)
            *resByte = 2;
        else
            *resByte = 0;
		char rgb[3];
		for (int i = 0; i < w * h; i++)
		{
			int offset = i * 3;
			rgb[2] = *((char*)image + offset);
			rgb[1] = *((char*)image + (offset + 1));
			rgb[0] = *((char*)image + (offset + 2));
			memcpy((char*)file + offset, &rgb, 3);
		}
		char *serverByte = ((char*)file + fileSize - 1);
		*serverByte = 1;
        return true;
    }

    bool hasClient()
    {
        if (fileHandle != NULL && file != NULL)
        {
			char *clientByte = ((char*)file + fileSize - 2);
            if (_hasClient == 0 || getTimeStamp() - lastChecked > 2000)
            {
                *_hasClient = *clientByte == 1;
				*clientByte = 0;
            }
            return *_hasClient;
        }
        return false;
    }

	bool hasServer()
	{
		if (fileHandle != NULL && file != NULL)
        {
			char *serverByte = ((char*)file + fileSize - 1);
			*serverByte = 0;
            int tried = 0;
            while (tried < 10)
            {
                if (*serverByte != 0)
                    return true;
                Sleep(100);
                tried++;
            }
        }
		return false;
	}
};