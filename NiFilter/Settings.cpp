#include <stdio.h>
#include <windows.h>
#include "Settings.h"

Settings::Settings()
{
	success = false;
	LONG result;
	result = RegOpenKeyEx(HKEY_CURRENT_USER, L"Software\\Soroush Falahati\\NiVirtualCam", 0, KEY_READ, &keyHandle);
	if (result == ERROR_SUCCESS)
		success = true;
}

Settings::~Settings()
{
	LONG result;
	result = RegCloseKey(keyHandle);
	if (result == ERROR_SUCCESS)
		success = false;
}

long Settings::ReadInt(LPCWSTR valueName, long defualtValue)
{
	DWORD buffer;
	unsigned long type=REG_DWORD, size=1024;
	if (success)
	{
		LONG result;
		result = RegQueryValueEx(keyHandle, valueName, NULL, &type, (LPBYTE)&buffer, &size);
		if (result == ERROR_SUCCESS)
		{
			return buffer;
		}
	}
	return defualtValue;
}