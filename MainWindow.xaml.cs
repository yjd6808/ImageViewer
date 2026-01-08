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
using System.Windows.Media.Media3D;
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

		private HwndSource hwndSounce_;
		private Form.Screen screen_ = null;
		private BitmapImage bitmap_ = null;
		private string imagePath_ = string.Empty;
		private double imageForceWidth = -1;
		private double imageForceHeight = -1;
		private int imageScreenIndex_ = 0;
		private static DateTime lastCloseTime_ = DateTime.Now;
		private bool fitToScreenWidth_ = false;
		private bool adjustAutoResizing_ = true;

		public bool IsImageLoaded => bitmap_ != null;
		private bool isResizing_ = false;
		private double resizeStartWidth_ = 0;
		private double resizeStartHeight_ = 0;

		public MainWindow()
		{ 
			InitializeComponent();
			Loaded += OnLoaded;
		}

		public MainWindow(string imagePath, int screenIndex, float x, float y, bool fitToScreenWidth, double width, double height, bool topMost)
		{
			InitializeComponent();
			Loaded += OnLoaded;
			Load(imagePath, screenIndex, x, y, fitToScreenWidth, width, height, topMost);
		}

		void Load(string imagePath, int screenIndex, float x, float y, bool fitToScreenWidth, double width, double height, bool topMost)
		{
			imagePath_ = imagePath;
			fitToScreenWidth_ = fitToScreenWidth;
			imageScreenIndex_ = screenIndex;
			Topmost = topMost;

			if (screenIndex < 0 || screenIndex >= Form.Screen.AllScreens.Length)
			{
				MessageBox.Show($"Invalid screen index: {screenIndex}. Using primary.");
				screenIndex = 0;
				Application.Current.Shutdown();
				return;
			}

			imageForceWidth = width;
			imageForceHeight = height;

			screen_ = Form.Screen.AllScreens[screenIndex];
			// 1. Screen 위치 적용
			SetPositionOnScreen(x, y);

			// 2. 이미지 로드 후 크기 자동 조정 (Stretch="Uniform")
			LoadImage(imagePath);
		}

		private void SetPositionOnScreen(float x, float y)
		{
			Left = screen_.Bounds.Left + x;
			Top = screen_.Bounds.Top + y;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			hwndSounce_ = PresentationSource.FromVisual(this) as HwndSource;
			hwndSounce_?.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			const int WM_ENTERSIZEMOVE = 0x0231;
			const int WM_EXITSIZEMOVE = 0x0232;
			const int WM_SIZING = 0x0214;

			if (msg == WM_ENTERSIZEMOVE)
			{
				resizeStartWidth_ = Width;
				resizeStartHeight_ = Height;
				return IntPtr.Zero;
			}
			if (msg == WM_SIZING)
			{
				isResizing_ = true;
				return IntPtr.Zero;
			}
			if (msg == WM_EXITSIZEMOVE)
			{
				if (isResizing_)
				{
					if (adjustAutoResizing_ && resizeStartWidth_ > 0 && resizeStartHeight_ > 0)
					{
						double widthRatio = 1.0;
						double heightRatio = 1.0;

						if (Width > resizeStartWidth_)
							widthRatio = Width / resizeStartWidth_;
						else
						{
							widthRatio = resizeStartWidth_ / Width;
						}

						if (Height > resizeStartHeight_)
							heightRatio = Height / resizeStartHeight_;
						else
						{
							heightRatio = resizeStartHeight_ / Height;
						}

						bool imageForceWidth = widthRatio > heightRatio; // 더 많은 비율의 변화가 생긴쪽으로 비율을 자동조정한다.
						double aspect = resizeStartWidth_ / resizeStartHeight_;
						if (imageForceWidth)
						{
							Height = Math.Max(1, Width / aspect);
						}
						else 
						{
							Width = Math.Max(1, Height * aspect);
						}

						resizeStartWidth_ = -1;
						resizeStartHeight_ = -1;
					}

					isResizing_ = false;
				}
				return IntPtr.Zero;
			}
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
				bitmap_ = bmp;

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

			if (imageForceWidth >= 1 && imageForceHeight >= 1)
			{
				Width = Math.Max(1, imageForceWidth);
				Height = Math.Max(1, imageForceHeight);
				return;
			}
			else if (imageForceWidth >= 1)
			{
				double aspect = width / height;
				width = imageForceWidth;
				height = width / aspect;
				Width = Math.Max(1, width);
				Height = Math.Max(1, height);
				return;
			}
			else if (imageForceHeight >= 1)
			{
				double aspect = width / height;
				height = imageForceHeight;
				width = height * aspect;
				Width = Math.Max(1, width);
				Height = Math.Max(1, height);
				return;
			}

			if (fitToScreenWidth_)
			{
				// Compute image aspect ratio
				double imgAspect = width / height;

				// Available area on the selected screen from current position
				double leftOnScreen = Left - screen_.Bounds.Left;
				double topOnScreen = Top - screen_.Bounds.Top;

				double maxWidthOnScreen = Math.Max(0, screen_.Bounds.Width - leftOnScreen);
				double maxHeightOnScreen = Math.Max(0, screen_.Bounds.Height - topOnScreen);

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
				if (width >= screen_.Bounds.Width)
				{
					double aspect = width / height;
					width = screen_.Bounds.Width;
					height = width / aspect;
				}

				if (height >= screen_.Bounds.Height)
				{
					double aspect = width / height;
					height = screen_.Bounds.Height;
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
				if ((DateTime.Now - lastCloseTime_).TotalMilliseconds < 200)
				{
					Application.Current.Shutdown();
					return;
				}
				Close();
				lastCloseTime_ = DateTime.Now;
			}
		}

		private string GetClipboardCommandLineArg()
		{
			int screenIndex = -1;
			int x = 0;
			int y = 0;
			for (int i = 0; i < Form.Screen.AllScreens.Length; i++)
			{
				var scr = Form.Screen.AllScreens[i];
				if (scr.Bounds.Contains((int)Left, (int)Top))
				{
					screenIndex = i;
					x = (int)(Left - scr.Bounds.Left);
					y = (int)(Top - scr.Bounds.Top);
					break;
				}
			}

			if (screenIndex == -1)
			{
				screenIndex = imageScreenIndex_;
			}

			string arg =
				$"\'" +
				$"path={imagePath_}," +
				$"screen={screenIndex}," +
				$"x={x}," +
				$"y={y}," +
				$"width={(int)Width}," +
				$"height={(int)Height}," +
				$"fit={(fitToScreenWidth_ ? 1 : 0)}," +
				$"topmost={(Topmost ? 1 : 0)}," +
				$"\'";
			return arg;
		}


		private void SaveArgClipboardSingle(object sender, RoutedEventArgs e)
		{
			if (!IsImageLoaded)
			{
				MessageBox.Show("이미지가 로드되지 않았습니다.");
				return;
			}

			string arg = GetClipboardCommandLineArg();
			Clipboard.SetText(arg);
			MessageBox.Show("클립보드에 내용이 복사되었습니다:\n" + arg);
		}

		private void SaveArgClipboardAll(object sender, RoutedEventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			bool isFirst = true;
			foreach (var wnd in Application.Current.Windows)
			{
				if (wnd is MainWindow mw && mw.IsImageLoaded)
				{
					if (!isFirst)
					{
						sb.Append(",");
						sb.AppendLine();
					}

					sb.Append(mw.GetClipboardCommandLineArg());
					isFirst = false;
				}
			}

			if (isFirst)
			{
				MessageBox.Show("로드된 이미지가 없습니다.");
				return;
			}

			string args = sb.ToString();
			Clipboard.SetText(args);
			MessageBox.Show($"모든 창의 명령줄 인수가 클립보드에 복사되었습니다:\n{args}");
		}

		private void Go_Topmost(object sender, RoutedEventArgs e)
		{
			Topmost = !Topmost;
		}

		private void DropImageFile(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				List<string> files = ((string[])e.Data.GetData(DataFormats.FileDrop)).ToList();
				string dropIamgeFile = files.FirstOrDefault(_x =>
				{
					string ext = Path.GetExtension(_x);
					if (string.IsNullOrWhiteSpace(ext))
						return false;

					switch (ext)
					{
					case ".jpg":
					case ".png":
					case ".bmp":
					case ".tiff":
					case ".webp":
					case ".gif":
					case ".ico":
						return true;
					}
					return false;
				});

				if (string.IsNullOrEmpty(dropIamgeFile))
				{
					MessageBox.Show("드롭된 파일 중 이미지 파일이 없습니다.");
					return;
				}

				int defaultScreenIdx = GetDefaultScreenIndex();

				if (IsImageLoaded)
				{
					new MainWindow(
						dropIamgeFile,
						defaultScreenIdx,
						0,
						0,
						fitToScreenWidth_,
						-1,
						-1,
						Topmost).Show();
				}
				else
				{
					Load(dropIamgeFile, defaultScreenIdx, 0, 0, false, -1, -1, false);
				}
			}
		}

		private int GetDefaultScreenIndex()
		{
			for (int i = 0; i < Form.Screen.AllScreens.Length; i++)
			{
				var scr = Form.Screen.AllScreens[i];
				if (scr.Bounds.Contains(0, 0))
				{
					return i;
				}
			}
			return 0; // 못찾으면 그냥 0
		}

		private void Go_AutoResizing(object sender, RoutedEventArgs e)
		{
			adjustAutoResizing_ = !adjustAutoResizing_;
		}
	}
}
