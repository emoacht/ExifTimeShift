using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ExifTimeShift.ViewModels;

namespace ExifTimeShift.Views
{
	public partial class MainWindow : Window
	{
		private readonly MainWindowViewModel _mainWindowViewModel;

		public MainWindow()
		{
			InitializeComponent();

			this.DataContext = _mainWindowViewModel = new MainWindowViewModel();

			this.Loaded += (_, _) => CheckHeights();
			this.DpiChanged += (_, _) => CheckHeights();
		}

		private void CheckHeights()
		{
			var chromeHeight = WindowGeometry.GetWindowRect(this).Height - WindowGeometry.GetClientRect(this).Height;
			DesiredMinHeight = this.Dashboard.ActualHeight + chromeHeight;
		}

		public double DesiredMinHeight
		{
			get { return (double)GetValue(DesiredMinHeightProperty); }
			set { SetValue(DesiredMinHeightProperty, value); }
		}
		public static readonly DependencyProperty DesiredMinHeightProperty =
			DependencyProperty.Register(
				nameof(DesiredMinHeight),
				typeof(double),
				typeof(MainWindow),
				new PropertyMetadata(0D));

		public string ProductName => _productName.Value;

		private readonly Lazy<string> _productName = new Lazy<string>(() =>
			Assembly.GetExecutingAssembly()
				.GetCustomAttributes(typeof(AssemblyProductAttribute))
				.Cast<AssemblyProductAttribute>()
				.First().Product);

		private async void SelectFiles_Click(object sender, RoutedEventArgs e)
		{
			await _mainWindowViewModel.SelectFilesAsync();
		}

		private void Reset_Click(object sender, RoutedEventArgs e)
		{
			_mainWindowViewModel.Reset();
		}

		private void SelectFolder_Click(object sender, RoutedEventArgs e)
		{
			_mainWindowViewModel.SelectFolder();
		}

		private async void Apply_Click(object sender, RoutedEventArgs e)
		{
			var (_, message) = await _mainWindowViewModel.ApplyAsync();
			SystemSounds.Asterisk.Play();
			MessageBox.Show(this, message, ProductName);
		}
	}
}