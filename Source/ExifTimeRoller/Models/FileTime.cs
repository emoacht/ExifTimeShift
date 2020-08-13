using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ExifTimeRoller.Models
{
	public class FileTime
	{
		#region Win32

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetFileTime(
			SafeFileHandle hFile,
			ref long lpCreationTime,
			ref long lpLastAccessTime,
			ref long lpLastWriteTime);

		[DllImport("Kernel32.dll")]
		private static extern uint FormatMessage(
			uint dwFlags,
			IntPtr lpSource,
			uint dwMessageId,
			uint dwLanguageId,
			StringBuilder lpBuffer, // IntPtr
			int nSize,
			IntPtr Arguments);

		private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

		#endregion

		public static void SetTime(string filePath, DateTime creationTime, DateTime lastWriteTime)
		{
			// 0 as Windows file time means that the current value will be preserved. 
			SetTime(filePath, creationTime.ToFileTime(), 0L, lastWriteTime.ToFileTime());
		}

		public static void SetTime(string filePath, DateTime creationTime, DateTime lastAccessTime, DateTime lastWriteTime)
		{
			SetTime(filePath, creationTime.ToFileTime(), lastAccessTime.ToFileTime(), lastWriteTime.ToFileTime());
		}

		private static void SetTime(string filePath, long creationTime, long lastAccessTime, long lastWriteTime)
		{
			using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite, 1))
			using (var handle = fs.SafeFileHandle)
			{
				if (handle.IsInvalid ||
					!SetFileTime(handle, ref creationTime, ref lastAccessTime, ref lastWriteTime))
				{
					var errorCode = Marshal.GetLastWin32Error();
					throw new Win32Exception(GetErrorMessage(errorCode));
				}
			}
		}

		private static string GetErrorMessage(int errorCode)
		{
			var buff = new StringBuilder(512);

			var length = FormatMessage(
				FORMAT_MESSAGE_FROM_SYSTEM,
				IntPtr.Zero,
				(uint)errorCode,
				0,
				buff,
				buff.Capacity,
				IntPtr.Zero);
			if (length == 0)
				throw new Win32Exception("Failed to get error message.");

			return buff.ToString().TrimEnd();
		}
	}
}