using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AmplifyPainter
{
	public enum CursorType
	{
		Invalid = -1,
		Cursor = 0,
		Pen = 1,
		TailSwitch = 2,
	}

	public static class APLibrary
	{

		public const string name = "TabletLibrary";

		[UnmanagedFunctionPointer( CallingConvention.StdCall )]
		private delegate void DebugLog( string log );

		private static readonly DebugLog debugLog = DebugWrapperLog;
		private static readonly DebugLog debugErr = DebugWrapperErr;
		private static readonly IntPtr functionPointerLog = Marshal.GetFunctionPointerForDelegate( debugLog );
		private static readonly IntPtr functionPointerErr = Marshal.GetFunctionPointerForDelegate( debugErr );

		private static void DebugWrapperLog( string log )
		{
			Debug.Log( log );
		}

		private static void DebugWrapperErr( string log )
		{
			Debug.LogError( log );
		}

		[DllImport( name, EntryPoint = "APTLinkDebugLog" )]
		private static extern void LinkDebugLog( [MarshalAs( UnmanagedType.FunctionPtr )]IntPtr debugCal );

		[DllImport( name, EntryPoint = "APTLinkDebugErr" )]
		private static extern void LinkDebugErr( [MarshalAs( UnmanagedType.FunctionPtr )]IntPtr debugCal );

		[DllImport( name, EntryPoint = "APTInitialize" )]
		private static extern void TabletInitialize();

		public static void TabletInitialize( bool usingUnityDebug = true )
		{
			if( usingUnityDebug )
			{
				LinkDebugLog( functionPointerLog );
				LinkDebugErr( functionPointerErr );
			}

			TabletInitialize();
		}

		[DllImport( name, EntryPoint = "APTFinalize" )]
		public static extern void TabletFinalize();

		[DllImport( name, EntryPoint = "APTGetPressure" )]
		public static extern float TabletGetPressure();

		[DllImport( name, EntryPoint = "APTGetCursorType" )]
		public static extern CursorType GetCursor();

		[DllImport( name, EntryPoint = "APTGetProximity" )]
		public static extern bool GetProximity();

		[DllImport( name, EntryPoint = "APTGetPenId" )]
		public static extern int GetPenId();

		[DllImport( name, EntryPoint = "APTGetCursorNum" )]
		public static extern int GetCursorNum();
	}
}
