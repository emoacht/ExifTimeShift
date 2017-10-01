using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace ExifDateEditor.Views
{
	public class WindowGeometry
	{
		#region Win32

		[DllImport("User32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowRect(
			IntPtr hwnd,
			out RECT lpRect);

		[DllImport("User32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetClientRect(
			IntPtr hwnd,
			out RECT lpRect);

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public static implicit operator Rect(RECT r) =>
				new Rect(r.left, r.top, r.right - r.left, r.bottom - r.top);
		}

		[DllImport("Dwmapi.dll")]
		private static extern int DwmGetWindowAttribute(
			IntPtr hwnd,
			uint dwAttribute,
			out RECT pvAttribute, // IntPtr
			uint cbAttribute);

		private const uint DWMWA_EXTENDED_FRAME_BOUNDS = 9;

		#endregion

		public static Rect GetWindowRect(Window window)
		{
			var handle = new WindowInteropHelper(window).Handle;

			return GetWindowRect(handle, out RECT lpRect)
				? lpRect
				: default(Rect);
		}

		public static Rect GetClientRect(Window window)
		{
			var handle = new WindowInteropHelper(window).Handle;

			return GetClientRect(handle, out RECT lpRect)
				? lpRect
				: default(Rect);
		}

		public static Rect GetDwmWindowRect(Window window)
		{
			var handle = new WindowInteropHelper(window).Handle;

			return (DwmGetWindowAttribute(
				handle,
				DWMWA_EXTENDED_FRAME_BOUNDS,
				out RECT rect,
				(uint)Marshal.SizeOf<RECT>()) != 0)
				? rect
				: default(Rect);
		}
	}
}