// NiCAM CServer.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
// General headers
#include <stdio.h>
#include <iostream>
#include <conio.h>
// OpenNI2 headers
#include <OpenNI.h>
#include "BitmapBroadcaster.cpp"
using namespace openni;
BitmapBroadcaster* broadcaster;

char ReadLastCharOfLine()
{
	int newChar = 0;
	int lastChar;
	fflush(stdout);
	do 
	{
		lastChar = newChar;
		newChar = getchar();
	}
	while ((newChar != '\n') 
		&& (newChar != EOF));
	return (char)lastChar;
}

bool HandleStatus(Status status)
{
	if (status == STATUS_OK)
		return true;
	printf("ERROR: #%d, %s", status,
		OpenNI::getExtendedError());
	ReadLastCharOfLine();
	return false;
}

void ReadNewFrame(VideoStream* sensor)
{
	if (sensor->isValid())
	{
		Status status = STATUS_OK;
		int streamReadyIndex;
		status = OpenNI::waitForAnyStream(&sensor, 1, &streamReadyIndex, 10);
		if (status == STATUS_OK && streamReadyIndex == 0)
		{
			VideoFrameRef newFrame;
			status = sensor->readFrame(&newFrame);
			if (status == STATUS_OK && newFrame.isValid())
			{
				std::cout<<"Frame Read. ";
				bool isSent = broadcaster->SendBitmap(newFrame.getData(), newFrame.getWidth(), newFrame.getHeight());
				newFrame.release();
				if (isSent)
					std::cout<<"Frame Sent. ";
				std::cout<<std::endl;
			}
		}
	}
}

int _tmain(int argc, _TCHAR* argv[])
{
	printf("\r\n------------------- Init Broadcaster -----------------------\r\n");
	broadcaster = new BitmapBroadcaster();

	Device device;
	VideoStream sensor;
	Status status = STATUS_OK;
	printf("\r\n---------------------- Init OpenNI --------------------------\r\n");
	printf("Scanning machine for devices and loading "
			"modules/drivers ...\r\n");
	
	status = OpenNI::initialize();
	if (!HandleStatus(status)) return 1;
	printf("Completed.\r\n");

	printf("\r\n---------------------- Open Device --------------------------\r\n");
	printf("Opening first device ...\r\n");
	status = device.open(ANY_DEVICE);
	if (!HandleStatus(status)) return 1;
	printf("%s Opened, Completed.\r\n",
		device.getDeviceInfo().getName());

	printf("\r\n---------------------- Init Sensor -------------------------\r\n");
	if (!device.hasSensor(SENSOR_COLOR))
	{
		printf("Stream not supported by this device.\r\n");
		return 1;
	}
	printf("Asking device to create a stream ...\r\n");
	status = sensor.create(device, SENSOR_COLOR);
	if (!HandleStatus(status)) return 1;

	printf("Setting video mode to 640x480x30 RGB24 ...\r\n");
	VideoMode vmod;
	vmod.setFps(30);
	vmod.setPixelFormat(PIXEL_FORMAT_RGB888);
	vmod.setResolution(640, 480);
	status = sensor.setVideoMode(vmod);
	if (!HandleStatus(status)) return 1;
	printf("Done.\r\n");

	printf("Starting stream ...\r\n");
	status = sensor.start();
	if (!HandleStatus(status)) return 1;
	printf("Done.\r\n");

	while(!_kbhit())
		ReadNewFrame(&sensor);

	sensor.destroy();
	device.close();
	OpenNI::shutdown();
	return 0;
}