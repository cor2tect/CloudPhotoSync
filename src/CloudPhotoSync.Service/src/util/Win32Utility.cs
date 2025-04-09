﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CloudPhotoSync.Service.src.util
{
	public static class Win32Utility
	{
		internal static class NativeMethods
		{
			[DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
			public static extern Int32 WaitForSingleObject(Microsoft.Win32.SafeHandles.SafeWaitHandle handle, uint milliseconds);

			public const uint QS_KEY = 0x0001;
			public const uint QS_MOUSEMOVE = 0x0002;
			public const uint QS_MOUSEBUTTON = 0x0004;
			public const uint QS_POSTMESSAGE = 0x0008;
			public const uint QS_TIMER = 0x0010;
			public const uint QS_PAINT = 0x0020;
			public const uint QS_SENDMESSAGE = 0x0040;
			public const uint QS_HOTKEY = 0x0080;
			public const uint QS_ALLPOSTMESSAGE = 0x0100;
			public const uint QS_RAWINPUT = 0x0400;

			public const uint QS_MOUSE = (QS_MOUSEMOVE | QS_MOUSEBUTTON);
			public const uint QS_INPUT = (QS_MOUSE | QS_KEY | QS_RAWINPUT);
			public const uint QS_ALLEVENTS = (QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY);
			public const uint QS_ALLINPUT = (QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE);

			public const uint MWMO_INPUTAVAILABLE = 0x0004;
			public const uint MWMO_WAITALL = 0x0001;

			public const uint PM_REMOVE = 0x0001;
			public const uint PM_NOREMOVE = 0;

			public const uint WAIT_TIMEOUT = 0x00000102;
			public const uint WAIT_FAILED = 0xFFFFFFFF;
			public const uint INFINITE = 0xFFFFFFFF;
			public const uint WAIT_OBJECT_0 = 0;
			public const uint WAIT_ABANDONED_0 = 0x00000080;
			public const uint WAIT_IO_COMPLETION = 0x000000C0;

			[StructLayout(LayoutKind.Sequential)]
			public struct MSG
			{
				public IntPtr hwnd;
				public uint message;
				public IntPtr wParam;
				public IntPtr lParam;
				public uint time;
				public int x;
				public int y;
			}

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll")]
			public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool TranslateMessage([In] ref MSG lpMsg);

			[DllImport("ole32.dll", PreserveSig = false)]
			public static extern void OleInitialize(IntPtr pvReserved);

			[DllImport("ole32.dll", PreserveSig = true)]
			public static extern void OleUninitialize();

			[DllImport("kernel32.dll")]
			public static extern uint GetTickCount();

			[DllImport("user32.dll")]
			public static extern uint GetQueueStatus(uint flags);

			[DllImport("user32.dll", SetLastError = true)]
			public static extern uint MsgWaitForMultipleObjectsEx(
					uint nCount, IntPtr[] pHandles, uint dwMilliseconds, uint dwWakeMask, uint dwFlags);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern uint WaitForMultipleObjects(
					uint nCount, IntPtr[] lpHandles, bool bWaitAll, uint dwMilliseconds);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SetEvent(IntPtr hEvent);

			[DllImport("kernel32.dll", SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool CloseHandle(IntPtr hObject);
		}
	}
}
