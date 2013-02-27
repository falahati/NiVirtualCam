#include <stdio.h>
#include <windows.h>

class Settings
{
public:
	Settings::Settings();
	Settings::~Settings();
	long Settings::ReadInt(LPCWSTR valueName, long defualtValue);
private:
	bool success;
	HKEY keyHandle;
};