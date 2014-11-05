SetCompressor /SOLID lzma
!include "nsProcess.nsh"
!include "Locate.nsh"
!include "x64.nsh"
!define KRB_URL "http://download.microsoft.com/download/A/3/5/A35E3B22-BE74-40AF-A46B-229E071452C1/KinectRuntime-v1.7-Setup.exe"

;--------------------------------
;Include Modern UI

  !include "MUI2.nsh"

   ; include for some of the windows messages defines
   !include "winmessages.nsh"

;--------------------------------
;General
  !include "x64.nsh"
  ;Name and file
  Name "NiVirtualCam for Windows v0.9.5.0"
  OutFile "..\NiVirtualCam-Win-v0.9.5.exe"

  ;Default installation folder
  InstallDir "$PROGRAMFILES\NiVirtualCam"

  ;Get installation folder from registry if available
  InstallDirRegKey HKLM "Software\NiVirtualCam" ""

  ;Request application privileges for Windows Vista
  RequestExecutionLevel admin ;Require admin rights on NT6+ (When UAC is turned on)

  !include LogicLib.nsh

  BrandingText "Soroush Falahati (Falahati.net)"

;--------------------------------
;Interface Configuration

  !define MUI_HEADERIMAGE
  !define MUI_HEADERIMAGE_BITMAP "Setup.bmp" ; optional
  !define MUI_ABORTWARNING

;--------------------------------
;Pages

  !insertmacro MUI_PAGE_LICENSE "GPL.txt"
  !insertmacro MUI_PAGE_COMPONENTS
  !insertmacro MUI_PAGE_DIRECTORY
  Page Custom ShowLockedFilesList
  !insertmacro MUI_PAGE_INSTFILES
    !define MUI_FINISHPAGE_RUN "$INSTDIR\NiUI.exe"
    !define MUI_FINISHPAGE_RUN_PARAMETERS " /autoRun"
    !define MUI_FINISHPAGE_RUN_TEXT "Launch Server Application"
  !insertmacro MUI_PAGE_FINISH

  !insertmacro MUI_UNPAGE_CONFIRM
  UninstPage Custom un.ShowLockedFilesList
  !insertmacro MUI_UNPAGE_INSTFILES
  !insertmacro MUI_UNPAGE_FINISH

!macro MYMACRO un
Function ${un}ShowLockedFilesList
  ${If} ${RunningX64}
    File /oname=$PLUGINSDIR\LockedList64.dll `${NSISDIR}\Plugins\LockedList64.dll`
  ${EndIf}
  !insertmacro MUI_HEADER_TEXT `Searching for locked files` `Free all locked files before proceeding by closing the applications listed below`
  ${locate::Open} "$INSTDIR" `/F=1 /D=0 /M=*.* /B=1` $0
    StrCmp $0 0 close
    loop:
    ${locate::Find} $0 $1 $2 $3 $4 $5 $6
      StrCmp $1 '' close
      LockedList::AddFile $1
      goto loop
    close:
  ${locate::Close} $0

  ${locate::Open} "$INSTDIR" `/F=1 /D=0 /X=exe|dll /M=*.* /B=1` $0
    StrCmp $0 0 close2
    loop2:
    ${locate::Find} $0 $1 $2 $3 $4 $5 $6
      StrCmp $1 '' close2
      LockedList::AddModule $1
      goto loop2
    close2:
  ${locate::Close} $0
  ${locate::Unload}

  ${nsProcess::FindProcess} "NiUI.exe" $R0
  StrCmp $R0 0 0 noprocess
     ${nsProcess::KillProcess} "NiUI.exe" $R0
     Sleep 3000
  noprocess:
  ${nsProcess::Unload}

  LockedList::Dialog /autonext
  Pop $R0
FunctionEnd
!macroend
!insertmacro MYMACRO ""
!insertmacro MYMACRO "un."
;--------------------------------
;Languages

  !insertmacro MUI_LANGUAGE "English"

;--------------------------------
;Installer Sections

Section "Main Application" SecMain
  SectionIn RO
  SetOutPath "$INSTDIR"
  File "GPL.txt"
  File "..\Release\*.ini"
  File "..\Release\*.dll"
  File "..\Release\NiUI.exe"
  File "..\Release\NiUI.exe.config"

  CreateDirectory "$INSTDIR\Primesense Drivers"
  SetOutPath "$INSTDIR\Primesense Drivers"
  File "Primesense Drivers\*"

  CreateDirectory "$INSTDIR\Primesense Drivers\amd64"
  SetOutPath "$INSTDIR\Primesense Drivers\amd64"
  File "Primesense Drivers\amd64\*"

  CreateDirectory "$INSTDIR\Primesense Drivers\x86"
  SetOutPath "$INSTDIR\Primesense Drivers\x86"
  File "Primesense Drivers\x86\*"

  CreateDirectory "$INSTDIR\OpenNI2"
  CreateDirectory "$INSTDIR\OpenNI2\Drivers"
  SetOutPath "$INSTDIR\OpenNI2\Drivers"
  File "..\Release\OpenNI2\Drivers\*"
  
  CreateDirectory "$INSTDIR\NiTE2"
  SetOutPath "$INSTDIR\NiTE2"
  File /x 'FeatureExtraction.ini' "..\Release\NiTE2\*"
  
  ${If} ${RunningX64}
     CreateDirectory "$PROGRAMFILES64\Microsoft SDKs\Kinect\v1.6\Assemblies"
     CreateDirectory "$PROGRAMFILES32\Microsoft SDKs\Kinect\v1.6\Assemblies"
     WriteRegStr HKLM "SOFTWARE\Microsoft\Kinect" "SDKInstallPath" "$PROGRAMFILES64\Microsoft SDKs\Kinect"
     WriteRegStr HKLM "SOFTWARE\Wow6432Node\Microsoft\Kinect" "SDKInstallPath" "$PROGRAMFILES32\Microsoft SDKs\Kinect"
     ExecWait '$INSTDIR\Primesense Drivers\dpinst-amd64.exe /SW /LM'
  ${Else}
     CreateDirectory "$PROGRAMFILES\Microsoft SDKs\Kinect\v1.6\Assemblies"
     WriteRegStr HKLM "SOFTWARE\Microsoft\Kinect" "SDKInstallPath" "$PROGRAMFILES\Microsoft SDKs\Kinect"
     ExecWait '$INSTDIR\Primesense Drivers\dpinst-x86.exe /SW /LM'
  ${EndIf}
  SetOutPath "$INSTDIR"
  ${If} ${RunningX64}
     ExecWait '$WINDIR\SysWoW64\regsvr32.exe /s /u "$INSTDIR\NiVirtualCamFilter.dll"'
     ExecWait '$WINDIR\SysWoW64\regsvr32.exe /s "$INSTDIR\NiVirtualCamFilter.dll"'
  ${Else}
     ExecWait '$WINDIR\System32\regsvr32.exe /s /u "$INSTDIR\NiVirtualCamFilter.dll"'
     ExecWait '$WINDIR\System32\regsvr32.exe /s "$INSTDIR\NiVirtualCamFilter.dll"'
  ${EndIf}
  ;Store installation folder
  WriteRegStr HKLM "Software\NiVirtualCam" "InstallDir" $INSTDIR
  WriteRegStr HKLM "Software\NiVirtualCam" "Version" "0.9.5"

  CreateShortCut "$SMPROGRAMS\OpenNI Virtual Webcam.lnk" "$INSTDIR\NiUI.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "OpenNI Virtual Webcam Server" '"$INSTDIR\NiUI.exe" /autoRun'

  ;Create uninstaller
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\NiVirtualCam 0.9.5 for Windows" "DisplayName" "OpenNI Virtual Webcam 0.9.5 for Windows (remove only)"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\NiVirtualCam 0.9.5 for Windows" "UninstallString" "$INSTDIR\Uninstall NiVirtualCam.exe"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\NiVirtualCam 0.9.5 for Windows" "Publisher" "Soroush Falahati (falahati.net)"

  WriteUninstaller "$INSTDIR\Uninstall NiVirtualCam.exe"


SectionEnd
Function GetKRB
        StrCpy $2 "$TEMP\Kinect Runtime Binaries 1.7.exe"
        nsisdl::download /TIMEOUT=30000 ${KRB_URL} $2
        Pop $R0 ;Get the return value
                StrCmp $R0 "success" +3 0
                MessageBox MB_OK|MB_ICONEXCLAMATION "Download Failed: $R0"
		Goto +2
        ExecWait $2
	Delete $2
FunctionEnd
Section "Microsoft Kinect Drivers" SecKinect
	CALL GetKRB
SectionEnd
Function .onInit
	;SetRebootFlag true 
	SectionSetSize "${SecKinect}" 225280
	UserInfo::GetAccountType
	pop $0
	${If} $0 != "admin" ;Require admin rights on NT4+
		MessageBox mb_iconstop "Administrator rights required!"
		SetErrorLevel 740 ;ERROR_ELEVATION_REQUIRED
		Quit
	${EndIf}
FunctionEnd
;--------------------------------
;Descriptions

  ;Language strings
  LangString DESC_SecMain ${LANG_ENGLISH} "OpenNI Virtual Webcam (NiVirtualCam) along with Primesense Sensor and Asus Xtion Drivers."
  LangString DESC_SecKinect ${LANG_ENGLISH} "Download and Install Microsoft Kinect Runtime 1.7 including Kinect for Windows and Kinect for Xbox Drivers."

  ;Assign language strings to sections
  !insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${SecMain} $(DESC_SecMain)
    !insertmacro MUI_DESCRIPTION_TEXT ${SecKinect} $(DESC_SecKinect)
  !insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
;Uninstaller Section

Section "Uninstall"
  ${If} ${RunningX64}
     ExecWait '$WINDIR\SysWoW64\regsvr32.exe /s /u "$INSTDIR\NiVirtualCamFilter.dll"'
  ${Else}
     ExecWait '$WINDIR\System32\regsvr32.exe /s /u "$INSTDIR\NiVirtualCamFilter.dll"'
  ${EndIf}
  RMDir /r "$INSTDIR\*.*"
  RMDir "$INSTDIR"
  Delete "$SMPROGRAMS\OpenNI Virtual Webcam.lnk"


  DeleteRegKey /ifempty HKLM "Software\NiVirtualCam"
  DeleteRegKey HKEY_LOCAL_MACHINE "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\NiVirtualCam 0.9.5 for Windows"
SectionEnd
Function un.onInit
	SetRebootFlag true 
	UserInfo::GetAccountType
	pop $0
	${If} $0 != "admin" ;Require admin rights on NT4+
		MessageBox mb_iconstop "Administrator rights required!"
		SetErrorLevel 740 ;ERROR_ELEVATION_REQUIRED
		Quit
	${EndIf}
FunctionEnd