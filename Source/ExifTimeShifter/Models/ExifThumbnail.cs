using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ExifTimeShifter.Models
{
	public class ExifThumbnail
	{
		public static async Task<BitmapImage> ReadThumbnailAsync(string sourceFilePath)
		{
			if (!File.Exists(sourceFilePath))
				return null;

			try
			{
				using var fs = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				return await Task.Run(() => ReadThumbnail(fs));
			}
			catch (Exception ex) when (IsImageNotSupported(ex))
			{
				return null;
			}
		}

		private static BitmapImage ReadThumbnail(Stream stream)
		{
			var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
			var source = frame.Thumbnail;
			if (source is null)
				return null;

			using (var ms = new MemoryStream())
			{
				var encoder = new JpegBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(source));
				encoder.Save(ms);
				ms.Seek(0, SeekOrigin.Begin);

				var image = new BitmapImage();
				image.BeginInit();
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.StreamSource = ms;
				image.EndInit();
				image.Freeze(); // This is necessary for other thread to use the image.

				return image;
			}
		}

		private static bool IsImageNotSupported(Exception ex)
		{
			if (ex is FileFormatException)
				return true;

			// Windows Imaging Component (WIC) defined error code: 0x88982F50 = WINCODEC_ERR_COMPONENTNOTFOUND
			// Error message: No imaging component suitable to complete this operation was found.
			const uint WINCODEC_ERR_COMPONENTNOTFOUND = 0x88982F50;

			if ((ex is NotSupportedException) &&
				(ex.InnerException is COMException) &&
				((uint)ex.InnerException.HResult == WINCODEC_ERR_COMPONENTNOTFOUND))
				return true;

			return false;
		}
	}
}