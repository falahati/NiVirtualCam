#include "stdafx.h"
#include <stdio.h>
#include <windows.h>
#include <time.h>

class BitmapBroadcaster
{
private:
	static const int fileSize;
	HANDLE fileHandle;
	void* file;
	int lastChecked;
    bool* _hasClient;
	unsigned int getTimeStamp();
public:
	BitmapBroadcaster();

	bool ClearScreen();

	bool SendBitmap(void* image, int w, int h);

    bool hasClient();

	bool hasServer();
};