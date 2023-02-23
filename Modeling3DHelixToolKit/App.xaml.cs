using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WpfWeCat3D
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			ChangeCulture(CultureInfo.CurrentCulture.Name);
		}

		public static void ChangeCulture(string culture)
		{
			CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture(culture);
			Thread.CurrentThread.CurrentCulture = cultureInfo;
			Thread.CurrentThread.CurrentUICulture = cultureInfo;
		}
	}
}
