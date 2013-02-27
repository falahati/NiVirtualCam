//-----------------------------------------------------------
//															|
// Soroush Falahati, s_falahati@hotmail.com, falahati.net	|
//															|
//-----------------------------------------------------------

#pragma warning(disable:4244)
#pragma warning(disable:4711)
//---------------------------------------------------------------------------
// Includes
//---------------------------------------------------------------------------
#include <streams.h>
#include <stdio.h>
#include <olectl.h>
#include <dvdmedia.h>
#include <math.h>
#include <direct.h>
#include <dos.h>
#include <math.h>
#include "NiVirtualCam.h"
#include "Settings.h"

//Context g_context;
//DepthGenerator g_depth;
//DepthMetaData g_depthMD;
BOOLEAN b1u = false;
BOOLEAN b2u = false;
BOOLEAN b3u = false;
byte i1u = 0;
byte i2u = 0;
byte i3u = 0;
BOOLEAN isok = true;

#define MAX_DEPTH 10000
float g_pDepthHist[MAX_DEPTH];

//////////////////////////////////////////////////////////////////////////
//  CKCam is the source filter which masquerades as a capture device
//////////////////////////////////////////////////////////////////////////
CUnknown * WINAPI CKCam::CreateInstance(LPUNKNOWN lpunk, HRESULT *phr)
{
    ASSERT(phr);
    CUnknown *punk = new CKCam(lpunk, phr);
    return punk;
}

CKCam::CKCam(LPUNKNOWN lpunk, HRESULT *phr) : 
    CSource(NAME("OpenNi Virtual Camera"), lpunk, CLSID_NiVirtualCam)
{
    ASSERT(phr);
    CAutoLock cAutoLock(&m_cStateLock);

	// Create the one and only output pin
    m_paStreams = (CSourceStream **) new CKCamStream*[1];
    m_paStreams[0] = new CKCamStream(phr, this, L"OpenNi Virtual Camera");
}

HRESULT CKCam::QueryInterface(REFIID riid, void **ppv)
{
    //Forward request for IAMStreamConfig & IKsPropertySet to the pin
    if(riid == _uuidof(IAMStreamConfig) || riid == _uuidof(IKsPropertySet))
        return m_paStreams[0]->QueryInterface(riid, ppv);
    else
        return CSource::QueryInterface(riid, ppv);
}

//////////////////////////////////////////////////////////////////////////
// CKCamStream is the one and only output pin of CKCam which handles 
// all the stuff.
//////////////////////////////////////////////////////////////////////////
CKCamStream::CKCamStream(HRESULT *phr, CKCam *pParent, LPCWSTR pPinName) :
    CSourceStream(NAME("OpenNi Virtual Camera"),phr, pParent, pPinName), m_pParent(pParent)
{
    // Set the default media type as 640x480x24@30
    GetMediaType(8, &m_mt);
}

CKCamStream::~CKCamStream()
{

} 
ULONG CKCamStream::Release(){
	return GetOwner()->Release();
}
ULONG CKCamStream::AddRef(){

	return GetOwner()->AddRef();
}

HRESULT CKCamStream::QueryInterface(REFIID riid, void **ppv)
{   

    // Standard OLE stuff
    if(riid == _uuidof(IAMStreamConfig))
        *ppv = (IAMStreamConfig*)this;
    else if(riid == _uuidof(IKsPropertySet))
        *ppv = (IKsPropertySet*)this;
    else
        return CSourceStream::QueryInterface(riid, ppv);

    AddRef();
    return S_OK;
}


//////////////////////////////////////////////////////////////////////////
//  This is the routine where we create the data being output by the Virtual
//  Camera device.
//////////////////////////////////////////////////////////////////////////
bool badRes = false;
bool badServer = false;
Settings *settings;
HANDLE fileHandle = NULL;
void* file;
int frameWidth = 640;
int frameHeight = 480;
int fileSize = (1280 * 1024 * 3) + 3;
int serverDown = 0;
HRESULT CKCamStream::FillBuffer(IMediaSample *pms)
{
	// Init setting object
	if (!settings)
		settings = new Settings();
	if (fileHandle == NULL){
		fileHandle = OpenFileMapping(FILE_MAP_ALL_ACCESS, FALSE, L"OpenNiVirtualCamFrameData");
		if (fileHandle == NULL){
			//fileHandle = CreateFileMapping(INVALID_HANDLE_VALUE, NULL, FILE_MAP_ALL_ACCESS, 0, fileSize, L"OpenNiVirtualCamFrameData");
			if (fileHandle == NULL && !badServer){
				badServer = true;
                MessageBox(NULL, L"Can not connect to the Server; please make sure that NiVirtualCam Controller Application is running. We keep trying until you open it.", L"Connection failed", MB_ICONWARNING);
            }
		}else
			badServer = false;
	}

	if (fileHandle != NULL && file == NULL){
		file = MapViewOfFile(fileHandle, FILE_MAP_ALL_ACCESS, 0, 0, fileSize);  
		if (file == NULL){
			if (!badServer){
				badServer = true;
				TCHAR msg[100];
				wsprintf (msg, L"Error: #%d", GetLastError());
				MessageBox(NULL, msg, L"Connection failed", MB_ICONWARNING);
			}
		}
		else
			badServer = false;
	}

	if (fileHandle != NULL && file != NULL)
	{
		char *serverByte = ((char*)file + fileSize - 1);
		if (*serverByte == 1)
		{
			badServer = false;
			serverDown = 0;
			*serverByte = 0;
		}
		else if (!badServer)
		{
			if (serverDown > 3600) // 2 min of inactivity from server
			{
				badServer = true;
                MessageBox(NULL, L"Connection to the server timed out; please make sure that NiVirtualCam Controller Application is running and active. We keep trying until you solve this problem.", L"Connection timed out", MB_ICONWARNING);
			}
			serverDown++;
		}
	}

	// Updating frame data and frame rate
	REFERENCE_TIME rtNow;
    REFERENCE_TIME avgFrameTime = ((VIDEOINFOHEADER*)m_mt.pbFormat)->AvgTimePerFrame;
	rtNow = m_rtLastTime;
    m_rtLastTime += avgFrameTime;
    pms->SetTime(&rtNow, &m_rtLastTime);
    pms->SetSyncPoint(TRUE);

	// Validating and guessing selected resolution
    BYTE *pData;
    long lDataLen;
    pms->GetPointer(&pData);
    lDataLen = pms->GetSize();
	int deswidth, desheight = 0;
	if (lDataLen % (16 * 9 * 3) == 0)
	{
		// 16/9
		badRes = false;
		int desmainpl = sqrt((DOUBLE)(lDataLen/(16 * 9 * 3)));
		deswidth = desmainpl * 16;
		desheight = desmainpl * 9;

	}else if (lDataLen % (11 * 9 * 3) == 0)
	{
		// 11/9
		badRes = false;
		int desmainpl = sqrt((DOUBLE)(lDataLen/(11 * 9 * 3)));
		deswidth = desmainpl * 11;
		desheight = desmainpl * 9;
	}else if (lDataLen % (3 * 4 * 3) == 0)
	{
		// 3/4
		badRes = false;
		int desmainpl = sqrt((DOUBLE)(lDataLen/(3 * 4 * 3)));
		deswidth = desmainpl * 4;
		desheight = desmainpl * 3;
	}else if (!badRes){
		badRes = true;
		MessageBox(NULL, L"Bad parameter sent for resolution. Only resolutions with 3/4, 11/9 or 16/9 ratio are supported.", L"Bad Parameter", MB_ICONSTOP);
	}

	if (badServer || badRes || file == NULL)
	{
		b1u = (i1u==255 || i1u==0 ? !b1u : b1u);
		b2u = (i2u==255 || i2u==0 ? !b2u : b2u);
		b3u = (i3u==255 || i3u==0 ? !b3u : b3u);
		i1u = (b1u ? i1u+3 : i1u-1);
		i2u = (b2u ? i2u+5 : i2u-3);
		i3u = (b3u ? i3u+1 : i3u-5);
		for (int i = lDataLen; i >= 3; i-=3){
			pData[i-1]=i1u;
			pData[i-2]=i2u;
			pData[i-3]=i3u;
		}
	}else{
		char *stateByte = ((char*)file + fileSize - 2);
		*stateByte = 1;
		char *resByte = ((char*)file + fileSize - 3);
		if (*resByte == 1)
		{
			frameWidth = 1280;
			frameHeight = 1024;
		}else if (*resByte == 2){
			frameWidth = 1280;
			frameHeight = 960;
		}else{
			frameWidth = 640;
			frameHeight = 480;
		}
		double resizeFactor = min(
			(deswidth / (double)frameWidth),
			(desheight / (double)frameHeight));
		int texture_x = (unsigned int)(deswidth - 
			(resizeFactor * frameWidth)) / 2;
		int texture_y = (unsigned int)(desheight - 
			(resizeFactor * frameHeight)) / 2;
		for	(int y = 0;
			y < (desheight - (2 * texture_y)); ++y)
		{
			int y2 = desheight - y;
			char* texturePixel = (char*)pData + ((((y2 + texture_y) * deswidth) + texture_x) * 3);
			for	(int x = 0;
				x < (deswidth - (2 * texture_x));
				++x, texturePixel += 3)
			{
				char* streamPixel = (char*)file + ((((int)(y / resizeFactor) * frameWidth) + (int)(x / resizeFactor)) * 3);
				memcpy(texturePixel, streamPixel, 3);
			}
		}
		Sleep(33);
	}
	return NOERROR;
} // FillBuffer


//
// Notify
// Ignore quality management messages sent from the downstream filter
STDMETHODIMP CKCamStream::Notify(IBaseFilter * pSender, Quality q)
{
    return E_NOTIMPL;
} // Notify

//////////////////////////////////////////////////////////////////////////
// This is called when the output format has been negotiated
//////////////////////////////////////////////////////////////////////////
HRESULT CKCamStream::SetMediaType(const CMediaType *pmt)
{
    DECLARE_PTR(VIDEOINFOHEADER, pvi, pmt->Format());
    HRESULT hr = CSourceStream::SetMediaType(pmt);
    return hr;
}

// See Directshow help topic for IAMStreamConfig for details on this method
HRESULT CKCamStream::GetMediaType(int iPosition, CMediaType *pmt)
{
    if(iPosition < 0) return E_INVALIDARG;
    if(iPosition > 8) return VFW_S_NO_MORE_ITEMS;

    if(iPosition == 0) 
    {
        *pmt = m_mt;
        return S_OK;
    }

    DECLARE_PTR(VIDEOINFOHEADER, pvi, pmt->AllocFormatBuffer(sizeof(VIDEOINFOHEADER)));
    ZeroMemory(pvi, sizeof(VIDEOINFOHEADER));

    pvi->bmiHeader.biCompression = BI_RGB;
    pvi->bmiHeader.biBitCount    = 24;
    pvi->bmiHeader.biSize       = sizeof(BITMAPINFOHEADER);
    pvi->bmiHeader.biWidth      = 80 * iPosition;
    pvi->bmiHeader.biHeight     = 60 * iPosition;
    pvi->bmiHeader.biPlanes     = 1;
    pvi->bmiHeader.biSizeImage  = GetBitmapSize(&pvi->bmiHeader);
    pvi->bmiHeader.biClrImportant = 0;

    pvi->AvgTimePerFrame = 500000;

    SetRectEmpty(&(pvi->rcSource)); // we want the whole image area rendered.
    SetRectEmpty(&(pvi->rcTarget)); // no particular destination rectangle

    pmt->SetType(&MEDIATYPE_Video);
    pmt->SetFormatType(&FORMAT_VideoInfo);
    pmt->SetTemporalCompression(FALSE);

    // Work out the GUID for the subtype from the header info.
    const GUID SubTypeGUID = GetBitmapSubtype(&pvi->bmiHeader);
    pmt->SetSubtype(&SubTypeGUID);
    pmt->SetSampleSize(pvi->bmiHeader.biSizeImage);
    
    return NOERROR;

} // GetMediaType

// This method is called to see if a given output format is supported
HRESULT CKCamStream::CheckMediaType(const CMediaType *pMediaType)
{
    VIDEOINFOHEADER *pvi = (VIDEOINFOHEADER *)(pMediaType->Format());
    if(*pMediaType != m_mt) 
        return E_INVALIDARG;
    return S_OK;
} // CheckMediaType

// This method is called after the pins are connected to allocate buffers to stream data
HRESULT CKCamStream::DecideBufferSize(IMemAllocator *pAlloc, ALLOCATOR_PROPERTIES *pProperties)
{
    CAutoLock cAutoLock(m_pFilter->pStateLock());
    HRESULT hr = NOERROR;

    VIDEOINFOHEADER *pvi = (VIDEOINFOHEADER *) m_mt.Format();
    pProperties->cBuffers = 1;
    pProperties->cbBuffer = pvi->bmiHeader.biSizeImage;

    ALLOCATOR_PROPERTIES Actual;
    hr = pAlloc->SetProperties(pProperties,&Actual);

    if(FAILED(hr)) return hr;
    if(Actual.cbBuffer < pProperties->cbBuffer) return E_FAIL;

    return NOERROR;
} // DecideBufferSize

// Called when graph is run
HRESULT CKCamStream::OnThreadCreate()
{
    m_rtLastTime = 0;
    return NOERROR;
} // OnThreadCreate


//////////////////////////////////////////////////////////////////////////
//  IAMStreamConfig
//////////////////////////////////////////////////////////////////////////

HRESULT STDMETHODCALLTYPE CKCamStream::SetFormat(AM_MEDIA_TYPE *pmt)
{
    DECLARE_PTR(VIDEOINFOHEADER, pvi, m_mt.pbFormat);
	if (!pmt){
		return VFW_E_INVALIDMEDIATYPE;
	}
    m_mt = *pmt;
    IPin* pin; 
    ConnectedTo(&pin);
    if(pin)
    {
        IFilterGraph *pGraph = m_pParent->GetGraph();
        pGraph->Reconnect(this);
    }
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CKCamStream::GetFormat(AM_MEDIA_TYPE **ppmt)
{
    *ppmt = CreateMediaType(&m_mt);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CKCamStream::GetNumberOfCapabilities(int *piCount, int *piSize)
{
    *piCount = 8;
    *piSize = sizeof(VIDEO_STREAM_CONFIG_CAPS);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CKCamStream::GetStreamCaps(int iIndex, AM_MEDIA_TYPE **pmt, BYTE *pSCC)
{
    *pmt = CreateMediaType(&m_mt);
    DECLARE_PTR(VIDEOINFOHEADER, pvi, (*pmt)->pbFormat);

    if (iIndex == 0) iIndex = 4;

    pvi->bmiHeader.biCompression = BI_RGB;
    pvi->bmiHeader.biBitCount    = 24;
    pvi->bmiHeader.biSize       = sizeof(BITMAPINFOHEADER);
    pvi->bmiHeader.biWidth      = 80 * iIndex;
    pvi->bmiHeader.biHeight     = 60 * iIndex;
    pvi->bmiHeader.biPlanes     = 1;
    pvi->bmiHeader.biSizeImage  = GetBitmapSize(&pvi->bmiHeader);
    pvi->bmiHeader.biClrImportant = 0;

    SetRectEmpty(&(pvi->rcSource)); // we want the whole image area rendered.
    SetRectEmpty(&(pvi->rcTarget)); // no particular destination rectangle

    (*pmt)->majortype = MEDIATYPE_Video;
    (*pmt)->subtype = MEDIASUBTYPE_RGB24;
    (*pmt)->formattype = FORMAT_VideoInfo;
    (*pmt)->bTemporalCompression = FALSE;
    (*pmt)->bFixedSizeSamples= FALSE;
    (*pmt)->lSampleSize = pvi->bmiHeader.biSizeImage;
    (*pmt)->cbFormat = sizeof(VIDEOINFOHEADER);
    
    DECLARE_PTR(VIDEO_STREAM_CONFIG_CAPS, pvscc, pSCC);
    
    pvscc->guid = FORMAT_VideoInfo;
    pvscc->VideoStandard = AnalogVideo_None;
    pvscc->InputSize.cx = 640;
    pvscc->InputSize.cy = 480;
    pvscc->MinCroppingSize.cx = 80;
    pvscc->MinCroppingSize.cy = 60;
    pvscc->MaxCroppingSize.cx = 640;
    pvscc->MaxCroppingSize.cy = 480;
    pvscc->CropGranularityX = 80;
    pvscc->CropGranularityY = 60;
    pvscc->CropAlignX = 0;
    pvscc->CropAlignY = 0;

    pvscc->MinOutputSize.cx = 80;
    pvscc->MinOutputSize.cy = 60;
    pvscc->MaxOutputSize.cx = 640;
    pvscc->MaxOutputSize.cy = 480;
    pvscc->OutputGranularityX = 0;
    pvscc->OutputGranularityY = 0;
    pvscc->StretchTapsX = 0;
    pvscc->StretchTapsY = 0;
    pvscc->ShrinkTapsX = 0;
    pvscc->ShrinkTapsY = 0;
    pvscc->MinFrameInterval = 200000;   //50 fps
    pvscc->MaxFrameInterval = 50000000; // 0.2 fps
    pvscc->MinBitsPerSecond = (80 * 60 * 3 * 8) / 5;
    pvscc->MaxBitsPerSecond = 640 * 480 * 3 * 8 * 50;

    return S_OK;
}

//////////////////////////////////////////////////////////////////////////
// IKsPropertySet
//////////////////////////////////////////////////////////////////////////


HRESULT CKCamStream::Set(REFGUID guidPropSet, DWORD dwID, void *pInstanceData, 
                        DWORD cbInstanceData, void *pPropData, DWORD cbPropData)
{// Set: Cannot set any properties.
    return E_NOTIMPL;
}

// Get: Return the pin category (our only property). 
HRESULT CKCamStream::Get(
    REFGUID guidPropSet,   // Which property set.
    DWORD dwPropID,        // Which property in that set.
    void *pInstanceData,   // Instance data (ignore).
    DWORD cbInstanceData,  // Size of the instance data (ignore).
    void *pPropData,       // Buffer to receive the property data.
    DWORD cbPropData,      // Size of the buffer.
    DWORD *pcbReturned     // Return the size of the property.
)
{
    if (guidPropSet != AMPROPSETID_Pin)             return E_PROP_SET_UNSUPPORTED;
    if (dwPropID != AMPROPERTY_PIN_CATEGORY)        return E_PROP_ID_UNSUPPORTED;
    if (pPropData == NULL && pcbReturned == NULL)   return E_POINTER;
    
    if (pcbReturned) *pcbReturned = sizeof(GUID);
    if (pPropData == NULL)          return S_OK; // Caller just wants to know the size. 
    if (cbPropData < sizeof(GUID))  return E_UNEXPECTED;// The buffer is too small.
        
    *(GUID *)pPropData = PIN_CATEGORY_CAPTURE;
    return S_OK;
}

// QuerySupported: Query whether the pin supports the specified property.
HRESULT CKCamStream::QuerySupported(REFGUID guidPropSet, DWORD dwPropID, DWORD *pTypeSupport)
{
    if (guidPropSet != AMPROPSETID_Pin) return E_PROP_SET_UNSUPPORTED;
    if (dwPropID != AMPROPERTY_PIN_CATEGORY) return E_PROP_ID_UNSUPPORTED;
    // We support getting this property, but not setting it.
    if (pTypeSupport) *pTypeSupport = KSPROPERTY_SUPPORT_GET; 
    return S_OK;
}
