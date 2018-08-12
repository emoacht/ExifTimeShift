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

using ExifDateEditor.Common;
using ExifDateEditor.Models;

namespace ExifDateEditor.ViewModels
{
	public class MainWindowViewModel : NotificationObject
	{
		#region Files

		public ObservableCollection<FileItem> Files { get; } = new ObservableCollection<FileItem>();

		public async Task FindAsync()
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

			UpdateCanApply();
		}

		#endregion

		#region Date

		public int Day
		{
			get => _day;
			set { SetProperty(ref _day, value); UpdateCanApply(); }
		}
		private int _day = 0;

		public int Hour
		{
			get => _hour;
			set { SetProperty(ref _hour, value); UpdateCanApply(); }
		}
		private int _hour = 0;

		public int Minute
		{
			get => _minute;
			set { SetProperty(ref _minute, value); UpdateCanApply(); }
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
			set { SetProperty(ref _savesInAnotherLocation, value); UpdateCanApply(); }
		}
		private bool _savesInAnotherLocation = true; // Safer side

		public string AnotherLocationPath
		{
			get => _anotherLocationPath;
			set { SetProperty(ref _anotherLocationPath, value); UpdateCanApply(); }
		}
		private string _anotherLocationPath;

		public void Select()
		{
			var initialPath = AnotherLocationPath;

			if (!string.IsNullOrEmpty(initialPath) && !Directory.Exists(initialPath))
			{
				var parent = Path.GetDirectoryName(initialPath);
				if (!string.IsNullOrEmpty(parent))
					initialPath = parent;
			}

			using (var fbd = new System.Windows.Forms.FolderBrowserDialog
			{
				Description = "Select folder",
				SelectedPath = initialPath,
			})
			{
				if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					AnotherLocationPath = fbd.SelectedPath;
				}
			}
		}

		public bool SetsSameFileCreationTime { get; set; }

		public bool CanApply
		{
			get => _canApply;
			private set { SetProperty(ref _canApply, value); }
		}
		private bool _canApply = false;

		private void UpdateCanApply()
		{
			CanApply = Files.Any()
				&& (ChangeSpan != TimeSpan.Zero)
				&& (!SavesInAnotherLocation || Directory.Exists(AnotherLocationPath));
		}

		public bool IsApplying
		{
			get => _isApplying;
			set => SetProperty(ref _isApplying, value);
		}
		private bool _isApplying;

		#endregion

		public async Task<(bool success, string message)> ApplyAsync()
		{
			try
			{
				IsApplying = true;

				Files.ToList().ForEach(x =>
				{
					x.IsSuccess = null;
					x.Message = null;
				});

				await Task.WhenAll(Files
					.Where(x => File.Exists(x.Path))
					.Select(async x =>
					{
						var destinationFilePath = !SavesInAnotherLocation
							? x.Path
							: Path.Combine(AnotherLocationPath, Path.GetFileName(x.Path));

						var (success, message, changedDate) = await ExifDate.ChangeDateTakenAsync(x.Path, destinationFilePath, ChangeSpan);
						if (!success)
						{
							x.IsSuccess = false;
							x.Message = message;
							return;
						}

						Debug.WriteLine($"{x.Path} - {x.Date:yyyy/MM/dd HH:mm:ss} -> {changedDate:yyyy/MM/dd HH:mm:ss}");

						if (!SavesInAnotherLocation)
						{
							x.Date = changedDate;
						}

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
			set => SetProperty(ref _date, value);
		}
		private DateTime _date;

		public bool? IsSuccess
		{
			get => _isSuccess;
			set => SetProperty(ref _isSuccess, value);
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