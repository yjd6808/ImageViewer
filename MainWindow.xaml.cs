using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Form = System.Windows.Forms;

namespace ImageViewer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private const int WM_NCLBUTTONDOWN = 0xA1;
		private const int HT_CAPTION = 2;
		private HwndSource hwndSource;
		private Form.Screen Screen = null;
		private BitmapImage Bitmap = null;

		private static DateTime lastCloseTime_ = DateTime.Now;
		private bool fitToScreenWidth_ = false;

		public MainWindow(string imagePath, int screenIndex, float x, float y, bool fitToScreenWidth = false)
		{
			InitializeComponent();
			Loaded += OnLoaded;
			fitToScreenWidth_ = fitToScreenWidth;

			if (screenIndex < 0 || screenIndex >= Form.Screen.AllScreens.Length)
			{
				MessageBox.Show($"Invalid screen index: {screenIndex}. Using primary.");
				screenIndex = 0;
				Application.Current.Shutdown();
				return;
			}

			Screen = Form.Screen.AllScreens[screenIndex];
			// 1. Screen 위치 적용
			SetPositionOnScreen(x, y);

			// 2. 이미지 로드 후 크기 자동 조정 (Stretch="Uniform")
			LoadImage(imagePath);
		}

		private void SetPositionOnScreen(float x, float y)
		{
			Left = Screen.Bounds.Left + x;
			Top = Screen.Bounds.Top + y;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			hwndSource = PresentationSource.FromVisual(this) as HwndSource;
			hwndSource?.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			return IntPtr.Zero;
		}

		private void LoadImage(string imagePath)
		{
			if (!File.Exists(imagePath))
			{
				MessageBox.Show($"File not found: {imagePath}");
				Application.Current.Shutdown();
				Close();
				return;
			}

			try
			{
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new Uri(Path.GetFullPath(imagePath), UriKind.Absolute);
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.EndInit();

				DisplayedImage.Source = bmp;
				Bitmap = bmp;

				// 이미지 로드 후 윈도우 크기를 이미지 비율에 맞춤 (최초 1회)
				AdjustWindowToImage(bmp);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to load: " + ex.Message);
				Application.Current.Shutdown();
				Close();
			}
		}

		private void AdjustWindowToImage(BitmapImage bmp)
		{
			double width = (double)bmp.PixelWidth;
			double height = (double)bmp.PixelHeight;

			if (fitToScreenWidth_)
			{
				// Compute image aspect ratio
				double imgAspect = width / height;

				// Available area on the selected screen from current position
				double leftOnScreen = Left - Screen.Bounds.Left;
				double topOnScreen = Top - Screen.Bounds.Top;

				double maxWidthOnScreen = Math.Max(0, Screen.Bounds.Width - leftOnScreen);
				double maxHeightOnScreen = Math.Max(0, Screen.Bounds.Height - topOnScreen);

				// Desired size starts from image pixel size
				double desiredWidth = bmp.PixelWidth;
				double desiredHeight = bmp.PixelHeight;

				// If fitToScreenWidth is enabled, cap width to remaining width on screen
				if (desiredWidth > maxWidthOnScreen)
				{
					desiredWidth = maxWidthOnScreen;
					desiredHeight = desiredWidth / imgAspect;
				}

				// Always ensure we don't exceed remaining height on screen
				if (desiredHeight > maxHeightOnScreen)
				{
					desiredHeight = maxHeightOnScreen;
					desiredWidth = desiredHeight * imgAspect;

					// If width exceeds available width after height cap, cap again by width
					if (desiredWidth > maxWidthOnScreen)
					{
						desiredWidth = maxWidthOnScreen;
						desiredHeight = desiredWidth / imgAspect;
					}
				}

				// Apply the calculated size to the window
				Width = Math.Max(1, desiredWidth);
				Height = Math.Max(1, desiredHeight);
			}
			else
			{
				if (width >= Screen.Bounds.Width)
				{
					double aspect = width / height;
					width = Screen.Bounds.Width;
					height = width / aspect;
				}

				if (height >= Screen.Bounds.Height)
				{
					double aspect = width / height;
					height = Screen.Bounds.Height;
					width = height * aspect;
				}

				Width = Math.Max(1, width);
				Height = Math.Max(1, height);

			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				Opacity = 0.3;
				DragMove();
				Opacity = 1.0;
			}
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				if (Keyboard.Modifiers == ModifierKeys.Control)
				{
					Application.Current.Shutdown();
					return;
				}

				if ((DateTime.Now - lastCloseTime_).TotalMilliseconds < 200)
					return;
				Close();
				lastCloseTime_ = DateTime.Now;
			}
		}
	}

}
