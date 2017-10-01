using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ExifDateEditor.Models
{
	public class ExifDate
	{
		public static async Task<(bool success, string message, DateTime originalDate)> ReadDateTakenAsync(string sourceFilePath)
		{
			if (string.IsNullOrWhiteSpace(sourceFilePath))
				throw new ArgumentNullException(nameof(sourceFilePath));

			var (success, message, originalDate, _) = await ChangeDateTakenBaseAsync(sourceFilePath, null, TimeSpan.Zero);
			return (success, message, originalDate);
		}

		public static async Task<(bool success, string message, DateTime changedDate)> ChangeDateTakenAsync(string sourceFilePath, string destinationFilePath, TimeSpan changeSpan)
		{
			if (string.IsNullOrWhiteSpace(sourceFilePath))
				throw new ArgumentNullException(nameof(sourceFilePath));
			if (string.IsNullOrWhiteSpace(destinationFilePath))
				throw new ArgumentNullException(nameof(destinationFilePath));
			if (changeSpan == TimeSpan.Zero)
				throw new ArgumentNullException(nameof(changeSpan));

			var (success, message, _, changedDate) = await ChangeDateTakenBaseAsync(sourceFilePath, destinationFilePath, changeSpan);
			return (success, message, changedDate);
		}

		private static async Task<(bool success, string message, DateTime originalDate, DateTime changedDate)> ChangeDateTakenBaseAsync(string sourceFilePath, string destinationFilePath, TimeSpan changeSpan)
		{
			const string dateFormat = "yyyy:MM:dd HH:mm:ss"; // The date and time format is "YYYY:MM:DD HH:MM:SS".

			var (success, exception, sourceBytes) = await ReadAllBytes(sourceFilePath).ConfigureAwait(false);
			if (!success)
				return (false, exception?.Message, default(DateTime), default(DateTime));

			string originalString;
			using (var ms = new MemoryStream(sourceBytes))
				(success, originalString) = GetDateTaken(ms);

			if (!success)
				return (false, "Failed to get data taken string.", default(DateTime), default(DateTime));

			DateTime originalDate;
			if (!DateTime.TryParseExact(originalString, dateFormat, null, DateTimeStyles.None, out originalDate))
				return (false, "Failed to parse date taken string.", default(DateTime), default(DateTime));

			if (changeSpan == TimeSpan.Zero)
				return (true, "Skipped.", originalDate, default(DateTime));

			var changedDate = originalDate.Add(changeSpan);
			var changedString = changedDate.ToString(dateFormat);

			Debug.WriteLine($"{sourceFilePath} - {originalString} -> {changedString}");

			var originalBytes = Encoding.ASCII.GetBytes(originalString);
			var changedBytes = Encoding.ASCII.GetBytes(changedString);

			var destinationBytes = sourceBytes.SequenceReplace(originalBytes, changedBytes);

			if (sourceBytes.Length != destinationBytes.Length)
				return (false, "Lengths don't match.", originalDate, changedDate);

			//CompareBytes(sourceBytes, destinationBytes);

			(success, exception) = await WriteAllBytes(destinationFilePath, destinationBytes).ConfigureAwait(false);
			return (success, exception?.Message, originalDate, changedDate);
		}

		private static Task<(bool success, Exception exception, byte[] bytes)> ReadAllBytes(string filePath)
		{
			return Task.Run(() =>
			{
				try
				{
					var bytes = File.ReadAllBytes(filePath);
					return (true, null, bytes);
				}
				catch (Exception ex)
				{
					return (false, ex, Array.Empty<byte>());
				}
			});
		}

		private static Task<(bool success, Exception exception)> WriteAllBytes(string filePath, byte[] bytes)
		{
			return Task.Run(() =>
			{
				try
				{
					File.WriteAllBytes(filePath, bytes);
					return (true, null);
				}
				catch (Exception ex)
				{
					return (false, ex);
				}
			});
		}

		private static (bool success, string value) GetDateTaken(Stream source)
		{
			const string dateTakenQuery = "/app1/ifd/exif/{ushort=36867}";

			var decoder = BitmapDecoder.Create(source, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);

			if (!decoder.CodecInfo.FileExtensions.ToLower().Contains("jpg"))
				return (false, null);

			if (!(decoder.Frames[0]?.Metadata?.Clone() is BitmapMetadata metadata))
				return (false, null);

			if (!metadata.ContainsQuery(dateTakenQuery))
				return (false, null);

			return (true, metadata.GetQuery(dateTakenQuery).ToString());
		}

		private static void CompareBytes(byte[] sourceBytes, byte[] destinationBytes)
		{
			for (int i = 0; i < sourceBytes.Length; i++)
			{
				if (sourceBytes[i] == destinationBytes[i])
					continue;

				Debug.WriteLine("Position: {0} (0x{0:X4}) Value: {1} -> {2}",
				  i,
				  BitConverter.ToString(new[] { sourceBytes[i] }),
				  BitConverter.ToString(new[] { destinationBytes[i] }));
			}
		}
	}
}