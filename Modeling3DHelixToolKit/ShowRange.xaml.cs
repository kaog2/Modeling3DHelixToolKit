using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Modeling3DHelixToolKit.Models;

namespace Modeling3DHelixToolKit
{
	/// <summary>
	/// Interaction logic for ShowRange.xaml
	/// </summary>
	public partial class ShowRange : Window
	{

		public List<ProfilAnalisys> profilAnalisys = new List<ProfilAnalisys>();

		public ShowRange(ResourceDictionary resourceDictionary)
		{
			InitializeComponent();
			Resources.MergedDictionaries.Add(resourceDictionary);
		}

		public void SetGapList(List<Tuple<string, string>> gapsList)
		{
			this.profilAnalisys.AddRange(profilAnalisys);

			var measure = from source in gapsList.AsEnumerable()
						  select new
						  {
							  From = source.Item1,
							  To = source.Item2,
							  SplitedProfiles = int.Parse(source.Item2) - int.Parse(source.Item1) + 1,
							  SplitedDistance = (int.Parse(source.Item2) - int.Parse(source.Item1) + 1) * 0.2
						  };

			Dispatcher.BeginInvoke(new Action(() =>
			{
				dataGridView2.ItemsSource = measure;
			}));
			
		}
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			string startProfil = string.Empty;
			string endProfil = string.Empty;
			List<Tuple<string, string>> startEndProfil = new List<Tuple<string, string>>();

			for (int i = 0; i < profilAnalisys.Count(); i++)
			{
				if (profilAnalisys[i].HadCircle == "FALSE" && string.IsNullOrEmpty(startProfil))
				{
					startProfil = profilAnalisys[i].Perfil.ToString();
					continue;
				}

				if (profilAnalisys[i].HadCircle == "TRUE" && string.IsNullOrEmpty(endProfil) && !string.IsNullOrEmpty(startProfil))
				{
					endProfil = profilAnalisys[i - 1].Perfil.ToString();
					startEndProfil.Add(new Tuple<string, string>(startProfil, endProfil));
					startProfil = string.Empty;
					endProfil = string.Empty;
					continue;
				}

				if (string.IsNullOrEmpty(endProfil) && i == profilAnalisys.Count() - 1 && profilAnalisys[i].HadCircle == "FALSE")
				{
					if (string.IsNullOrEmpty(startProfil))
					{
						endProfil = profilAnalisys[i].Perfil.ToString();
						startEndProfil.Add(new Tuple<string, string>(endProfil, endProfil));
						startProfil = string.Empty;
						endProfil = string.Empty;
					}
					else
					{
						endProfil = profilAnalisys[i].Perfil.ToString();
						startEndProfil.Add(new Tuple<string, string>(startProfil, endProfil));
						startProfil = string.Empty;
						endProfil = string.Empty;
					}
				}
			}

			var measure = from source in startEndProfil.AsEnumerable()
						  select new
						  {
							  From = source.Item1,
							  To = source.Item2,
							  SplitedProfiles = int.Parse(source.Item2) - int.Parse(source.Item1) + 1,
							  SplitedDistance = (int.Parse(source.Item2) - int.Parse(source.Item1) + 1) * 0.2
						  };

			dataGridView2.ItemsSource = measure;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}
	}
}
