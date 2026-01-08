using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ImageViewer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{

		protected override void OnStartup(StartupEventArgs _e)
		{
			base.OnStartup(_e);

			string[] args = _e.Args;
			if (args.Length == 0)
			{
				new MainWindow().Show();
				return;
			}

			// 실행인자 테스트
			// "path=E:\Script\a.png,screen=1,x=100,y=200,fit=true,height=600" "path=E:\Script\b.png,screen=1,x=200,y=200,fit=true" "path=E:\Script\b.png,screen=1,x=300,y=200,fit=true" "path=E:\Script\b.png,screen=1,x=400,y=200,fit=true"

			for (int i = 0; i < args.Length; ++i)
			{
				string arg = args[i];
				string 	path = string.Empty;
				int 	screenIdx = 0;
				float 	x = 0;
				float 	y = 0;
				bool	fitToScreenWidth = false;
				double	width = -1;
				double	height = -1;
				bool	topMost = false;
				foreach (string token in arg.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					string[] kv = token.Trim().Split('=');
					if (kv.Length < 2)
					{
						MessageBox.Show($"토큰을 =문자로 나눌 수 없습니다.(인자를 올바르게 전달했는지 확인)\n인자:{arg}");
						Application.Current.Shutdown();
						return;
					}

					string k = kv[0].ToLower().Trim();
					string v = kv[1].Trim();

					switch (k)
					{
					case "path":
						path = v;
						break;
					case "screen":
						if (!int.TryParse(v, out screenIdx))
						{
							MessageBox.Show($"screen 인자는 정수여야 합니다.\n인자:{arg}");
						}
						break;
					case "x":
						if (!float.TryParse(v, out x))
						{
							MessageBox.Show($"x 인자는 실수여야 합니다.\n인자:{arg}");
						}
						break;
					case "y":
						if (!float.TryParse(v, out y))
						{
							MessageBox.Show($"y 인자는 실수여야 합니다.\n인자:{arg}");
						}
						break;
					case "fit":
						if (!bool.TryParse(v, out fitToScreenWidth))
						{
							if (int. TryParse(v, out int fitInt))
							{
								fitToScreenWidth = (fitInt != 0);
							}
							else
							{
								MessageBox.Show($"fit 인자는 true/false(1/0)여야 합니다.\n인자:{arg}");
							}
						}
						break;
					case "width":
						if (!double.TryParse(v, out width) && width < 1)
						{
							MessageBox.Show($"width 인자는 실수이고 1이상이어야 합니다.\n인자:{arg}");
						}
						break;
					case "height":
						if (!double.TryParse(v, out height) && height < 1)
						{
							MessageBox.Show($"height 인자는 실수이고 1이상이어야 합니다.\n인자:{arg}");
						}
						break;
					case "topmost":
						if (!bool.TryParse(v, out topMost))
						{
							if (int. TryParse(v, out int topMostInt))
							{
								topMost = (topMostInt != 0);
							}
							else
							{
								MessageBox.Show($"topmost 인자는 true/false(1/0)여야 합니다.\n인자:{arg}");
							}
						}
						break;
					}
				}

				// ImageViewer.exe "path=C:\image.jpg screen=1 x=100 y=200 fit=true" "path=C:\image2.png screen=0 x=0 y=0 fit=false"

				MainWindow mainWindow = new MainWindow(path, screenIdx, x, y, fitToScreenWidth, width, height, topMost);
				mainWindow.Show();
			}
		}
	}
}
