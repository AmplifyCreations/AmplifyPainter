#include "stdafx.h"
#include <memory>
#include <thread>
#include <Unity/IUnityInterface.h>
#include "Wintab.h"
#include "Tablet.h"
//#include "Debug.h"

std::unique_ptr<Tablet> g_tablet;
HINSTANCE g_hInstance = nullptr;
bool g_initialize = FALSE;
HHOOK g_hook = nullptr;
HWND g_hWnd = nullptr;
std::thread g_thread;

UINT32 g_pressure = 0;

BOOL APIENTRY DllMain( HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved )
{
	switch( ul_reason_for_call )
	{
		case DLL_PROCESS_ATTACH:
		{
			g_hInstance = hModule;
			break;
		}
		case DLL_PROCESS_DETACH:
		{
			g_hInstance = nullptr;
			break;
		}
	}
	return TRUE;
}

std::string GetLastErrorAsString()
{
	DWORD errorMessageID = GetLastError();
	if( errorMessageID == 0 )
		return std::string();

	LPSTR messageBuffer = nullptr;
	size_t size = FormatMessageA( FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
		NULL, errorMessageID, MAKELANGID( LANG_NEUTRAL, SUBLANG_DEFAULT ), (LPSTR)&messageBuffer, 0, NULL );

	std::string message( messageBuffer, size );

	LocalFree( messageBuffer );

	return message;
}

LRESULT CALLBACK HookCallback( int nCode, WPARAM wp, LPARAM lp )
{
	if( nCode >= 0 )
	{
		LPMSG pStruct = (LPMSG)lp;
		UINT msg = pStruct->message;
		WPARAM wParam = pStruct->wParam;
		switch( msg )
		{
			case WM_POINTERDEVICECHANGE:
			{
				Debug::Log( "Device Changed" );
			}
			break;
			case WM_POINTERDEVICEINRANGE:
			{
				Debug::Log( "Device in range" );
			}
			break;
			case WM_POINTERDEVICEOUTOFRANGE:
			{
				Debug::Log( "Device out of range" );
			}
			break;
			case WM_NCPOINTERUPDATE:
			case WM_NCPOINTERDOWN:
			case WM_NCPOINTERUP:
			case WM_POINTERUPDATE:
			case WM_POINTERDOWN:
			case WM_POINTERUP:
			case WM_POINTERENTER:
			case WM_POINTERLEAVE:
			{
				UINT32 pointerId = GET_POINTERID_WPARAM( wParam );
				POINTER_PEN_INFO pointerPenInfo = {};
				if( GetPointerPenInfo( pointerId, &pointerPenInfo ) )
				{
					g_pressure = pointerPenInfo.pressure;
					Debug::Log( pointerPenInfo.pressure );
				}
				else
				{
					g_pressure = 1024;
					Debug::Error( GetLastErrorAsString() );
				}
			}
			break;
		}
	}
	return CallNextHookEx( g_hook, nCode, wp, lp );
}

LRESULT CALLBACK WindowProcess( HWND hWnd, UINT msg, WPARAM wp, LPARAM lp )
{
	switch( msg )
	{
		case WM_CREATE:
		{
			g_tablet = std::make_unique<Tablet>();
			g_tablet->Open( hWnd );
			break;
		}
		case WM_CLOSE:
		{
			DestroyWindow( hWnd );
			break;
		}
		case WM_DESTROY:
		{
			g_tablet->Close();
			g_tablet.reset();
			PostQuitMessage( 0 );
			return 0;
		}
		case WM_ACTIVATE:
		{
			const auto state = GET_WM_ACTIVATE_STATE( wp, lp );
			if( state )
			{
				g_tablet->Overwrap();
			}
			break;
		}
		case WT_PACKET:
		{
			g_tablet->ReceivePacket( lp, wp );
			return 0;
		}
		case WT_PACKETEXT:
		{
			g_tablet->ReceivePacketExt( lp, wp );
			return 0;
		}
		//case WT_CTXUPDATE:
		case WT_CSRCHANGE:
		case WT_PROXIMITY:
		{
			g_tablet->ReceiveProximity( lp, wp );
			break;
		}
	}

	return ::DefWindowProc( hWnd, msg, wp, lp );
}

void ThreadFunc()
{
	WNDCLASS wc;
	wc.style = CS_HREDRAW | CS_VREDRAW;// 0;
	wc.lpfnWndProc = WindowProcess;
	wc.cbClsExtra = 0;
	wc.cbWndExtra = 0;
	wc.hInstance = g_hInstance;
	wc.hIcon = LoadIcon( NULL, IDI_APPLICATION );
	wc.hCursor = LoadCursor( NULL, IDC_ARROW );
	wc.hbrBackground = (HBRUSH)0;
	wc.lpszMenuName = kLibraryName;
	wc.lpszClassName = kLibraryName;

	if( RegisterClass( &wc ) )
	{
		g_hWnd = ::CreateWindowEx(
			WS_EX_TOOLWINDOW,
			kLibraryName,
			kLibraryName,
			WS_POPUP & ~WS_VISIBLE,
			0,
			0,
			0,
			0,
			HWND_DESKTOP,
			NULL,
			g_hInstance,
			NULL );

		if( g_hWnd )
		{
			ShowWindow( g_hWnd, SW_SHOWNOACTIVATE );
			SetWindowPos( g_hWnd, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE );
		}
	}

	MSG msg;
	while( GetMessage( &msg, NULL, 0, 0 ) )
	{
		DispatchMessage( &msg );

		if( g_tablet )
		{
			g_tablet->Enable( TRUE );
			g_tablet->Update();
		}

		InvalidateRect( g_hWnd, NULL, TRUE );
		UpdateWindow( g_hWnd );
	}
	g_hWnd = nullptr;

	UnregisterClass( kLibraryName, g_hInstance );
}

void InstallHooks()
{
	//HWND Find = FindWindow( NULL, L"Unity 2019.1.0f2 - teste.unity - Amplify Painter - PC, Mac & Linux Standalone <DX11>" );
	HWND mainWindow = GetActiveWindow();
	if( mainWindow == NULL )
	{
		Debug::Log( "Failed to find window" );
	}
	else
	{
		RegisterPointerDeviceNotifications( mainWindow, TRUE );
	}

	if( g_hook = SetWindowsHookEx( WH_GETMESSAGE, HookCallback, g_hInstance, GetCurrentThreadId() ) )
	{
		Debug::Log( "Installed hook!" );
	}
	else
	{
		Debug::Error( "Failed to install hook!" );
	}
}

void RemoveHooks()
{
	if( UnhookWindowsHookEx( g_hook ) )
	{
		Debug::Log( "Removed hook!" );
	}
	else
	{
		Debug::Error( "Failed to removed hook! Already removed?" );
	}
}

void CreateTabletWindow()
{
	g_thread = std::thread( []
	{
		ThreadFunc();
	} );
}


void DestroyTabletWindow()
{
	if( !g_hWnd ) 
		return;

	SendMessage( g_hWnd, WM_CLOSE, 0, 0 );
	if( g_thread.joinable() )
	{
		g_thread.join();
	}
}

extern "C"
{
	UNITY_INTERFACE_EXPORT bool UNITY_INTERFACE_API APTIsAvailable()
	{
		return Tablet::IsAvailable();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API APTInitialize()
	{
		if( g_initialize )
			return;

		g_initialize = TRUE;
		Debug::Initialize();
		if( Wintab::Load() )
		{
			if( Tablet::IsAvailable() )
			{
				CreateTabletWindow();
				Debug::Log( "Window Created" );
			}
			else
			{
				Debug::Error( "Tablet is not connected." );
			}
		}
		else
		{
			InstallHooks();
		}
	}


	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API APTFinalize()
	{
		if( !g_initialize )
			return;

		if( g_tablet )
		{
			DestroyTabletWindow();
			Wintab::Unload();
		}
		else
		{
			RemoveHooks();
		}

		Debug::Finalize();
		g_initialize = FALSE;
	}

	UNITY_INTERFACE_EXPORT float UNITY_INTERFACE_API APTGetPressure()
	{
		if (!g_tablet) 
			return static_cast<float>(g_pressure) / (float)1024.0;

		return g_tablet->GetPressure();
	}

	UNITY_INTERFACE_EXPORT Tablet::CursorType UNITY_INTERFACE_API APTGetCursorType()
	{
		if( !g_tablet ) return Tablet::CursorType::Invalid;
			return g_tablet->GetCursor();
	}

	UNITY_INTERFACE_EXPORT bool UNITY_INTERFACE_API APTGetProximity()
	{
		if( !g_tablet ) return false;
			return g_tablet->GetProximity();
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API APTGetPenId()
	{
		if( !g_tablet ) return -1;
			return g_tablet->GetPenId();
	}

	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API APTGetCursorNum()
	{
		if( !g_tablet ) return -1;
			return g_tablet->GetCursorNum();
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API APTSetDebugMode( Debug::Mode mode )
	{
#if NDEBUG
		Debug::SetMode(Debug::Mode::None);
#else
		Debug::SetMode(mode);
#endif
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API APTLinkDebugLog( Debug::DebugLogFuncPtr debugCal )
	{
#if NDEBUG
		Debug::SetMode( Debug::Mode::None );
#else
		Debug::SetMode(Debug::Mode::UnityLog);
#endif
		Debug::SetLogFunc( debugCal );
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API APTLinkDebugErr( Debug::DebugLogFuncPtr debugCal )
	{
#if NDEBUG
		Debug::SetMode(Debug::Mode::None);
#else
		Debug::SetMode(Debug::Mode::UnityLog);
#endif
		Debug::SetErrorFunc( debugCal );
	}
}
