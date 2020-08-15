using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

using ExifTimeShifter.Common;
using ExifTimeShifter.Models;
using ExifTimeShifter.Views.Controls;

namespace ExifTimeShifter.ViewModels
{
	public class MainWindowViewModel : NotificationObject
	{
		#region Files

		public ObservableCollection<FileItem> Files { get; } = new ObservableCollection<FileItem>();

		public async Task SelectFilesAsync()
		{
			var ofd = new OpenFileDialog
			{
				Filter = "JPG Files|*.jpg",
				Multiselect = true
			};
			if (ofd.ShowDialog() != true)
				return;

			Files.Clear();

			foreach (var filePath in ofd.FileNames)
			{
				if (!Path.GetExtension(filePath).Equals(".jpg", StringComparison.OrdinalIgnoreCase))
					continue;

				var (success, _, originalDate) = await ExifDate.ReadDateTakenAsync(filePath);
				if (!success)
					continue;

				Files.Add(new FileItem(filePath, originalDate));
			}

			ChangeCanApply();
		}

		#endregion

		#region Date

		public int Day
		{
			get => _day;
			set => SetPropertyValue(ref _day, value, ChangeCanApply);
		}
		private int _day = 0;

		public int Hour
		{
			get => _hour;
			set => SetPropertyValue(ref _hour, value, ChangeCanApply);
		}
		private int _hour = 0;

		public int Minute
		{
			get => _minute;
			set => SetPropertyValue(ref _minute, value, ChangeCanApply);
		}
		private int _minute = 0;

		public void Reset()
		{
			Day = 0;
			Hour = 0;
			Minute = 0;
		}

		private TimeSpan ChangeSpan => new TimeSpan(Day, Hour, Minute, 0);

		#endregion

		#region Settings

		public bool SavesInAnotherLocation
		{
			get => _savesInAnotherLocation;
			set => SetPropertyValue(ref _savesInAnotherLocation, value, ChangeCanApply);
		}
		private bool _savesInAnotherLocation = true; // Safer side

		public string AnotherLocationPath
		{
			get => _anotherLocationPath;
			set => SetPropertyValue(ref _anotherLocationPath, value, ChangeCanApply);
		}
		private string _anotherLocationPath;

		public void SelectFolder()
		{
			var initialPath = AnotherLocationPath;

			if (!string.IsNullOrEmpty(initialPath) && !Directory.Exists(initialPath))
			{
				var parentPath = Path.GetDirectoryName(initialPath);
				if (!string.IsNullOrEmpty(parentPath))
					initialPath = parentPath;
			}

			var ofd = new OpenFolderDialog
			{
				Title = "Select folder",
				InitialPath = initialPath
			};
			if (ofd.ShowDialog())
			{
				AnotherLocationPath = ofd.SelectedPath;
			}
		}

		public bool SetsSameFileCreationTime { get; set; } = true;

		public bool CanApply
		{
			get => _canApply;
			private set => SetPropertyValue(ref _canApply, value);
		}
		private bool _canApply = false;

		private void ChangeCanApply()
		{
			CanApply = Files.Any()
				&& (ChangeSpan != TimeSpan.Zero)
				&& (!SavesInAnotherLocation || Directory.Exists(AnotherLocationPath));
		}

		public bool IsApplying
		{
			get => _isApplying;
			set => SetPropertyValue(ref _isApplying, value);
		}
		private bool _isApplying;

		#endregion

		public async Task<(bool success, string message)> ApplyAsync()
		{
			try
			{
				IsApplying = true;

				var semaphore = new SemaphoreSlim(3, 3);

				await Task.WhenAll(Files.Select(async x =>
				{
					x.IsSuccess = null;
					x.Message = null;

					try
					{
						await semaphore.WaitAsync();

						var sourceFilePath = x.Path;
						if (!File.Exists(sourceFilePath))
							return;

						var destinationFilePath = !SavesInAnotherLocation
							? sourceFilePath
							: Path.Combine(AnotherLocationPath, Path.GetFileName(sourceFilePath));

						var (success, message, changedDate) = await ExifDate.ChangeDateTakenAsync(sourceFilePath, destinationFilePath, ChangeSpan);
						if (!success)
						{
							x.IsSuccess = false;
							x.Message = message;
							return;
						}

						Debug.WriteLine($"{sourceFilePath} - {x.Date:yyyy/MM/dd HH:mm:ss} -> {changedDate:yyyy/MM/dd HH:mm:ss}");

						if (!SavesInAnotherLocation)
							x.Date = changedDate;

						if (SetsSameFileCreationTime)
						{
							(success, message) = await SetFileCreationTime(destinationFilePath, changedDate);
							if (!success)
							{
								x.IsSuccess = false;
								x.Message = message;
								return;
							}
						}

						x.IsSuccess = true;
					}
					finally
					{
						semaphore.Release();
					}
				}));

				var messages = Files
					.Where(x => x.IsSuccess == false)
					.Select(x => $"{Path.GetFileName(x.Path)} - {x.Message}")
					.ToArray();

				var successTotal = !messages.Any();
				var messageTotal = successTotal
					? $"Applied successfully."
					: $"Failed.\r\n{string.Join("\r\n", messages)}";

				await SaveLogFile(messageTotal);

				return (successTotal, messageTotal);
			}
			finally
			{
				IsApplying = false;
			}
		}

		private Task<(bool success, string message)> SetFileCreationTime(string filePath, DateTime date)
		{
			return Task.Run(() =>
			{
				try
				{
					FileTime.SetTime(filePath, date, date);
					return (true, null);
				}
				catch (Exception ex)
				{
					return (false, $"Failed to set file creation time. {ex.Message}");
				}
			});
		}

		private const string LogFileName = "log.txt";

		private Task SaveLogFile(string contents)
		{
			return Task.Run(() =>
			{
				try
				{
					File.AppendAllText(LogFileName, $"[{DateTime.Now:HH:ss:ss}]\r\n{contents}\r\n\r\n");
				}
				catch
				{ }
			});
		}
	}

	public class FileItem : NotificationObject
	{
		public string Path { get; }

		public DateTime Date
		{
			get => _date;
			set => SetPropertyValue(ref _date, value);
		}
		private DateTime _date;

		public bool? IsSuccess
		{
			get => _isSuccess;
			set => SetPropertyValue(ref _isSuccess, value);
		}
		private bool? _isSuccess = null;

		public string Message { get; set; }

		public FileItem(string path, DateTime date)
		{
			this.Path = path;
			this.Date = date;
		}
	}
}