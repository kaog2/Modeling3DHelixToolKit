using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Modeling3DHelixToolKit.Models;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using Modeling3DHelixToolKit.Classes;
using Accord.Statistics.Models.Regression.Linear;
using PolynomialRegression = Accord.Statistics.Models.Regression.Linear.PolynomialRegression;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Modeling3DHelixToolKit
{

	/// <summary>
	/// Interaction logic for Evaluation.xaml
	/// </summary>
	public partial class Evaluation : Window
	{
		ShowRange showRange = null;

		private DataTable infoResultTable;
		private DataTable circleTable;
		private DataTable keyPointsTable;
		private DataTable denthTable;
		private DataTable fittedLines;
		private DataTable circleEcuation;

		//public List<Point3D> profilePoints = new List<Point3D>();
		//public List<Point3D> circlePoints = new List<Point3D>();
		//public List<Point3D> dentPoints = new List<Point3D>();
		//public List<Point3D> linePoints = new List<Point3D>();
		//public List<Point3D> keyPoints = new List<Point3D>();

		public ConcurrentBag<Point3D> profilePoints = new ConcurrentBag<Point3D>();
		public ConcurrentBag<Point3D> circlePoints = new ConcurrentBag<Point3D>();
		public ConcurrentBag<Point3D> dentPoints = new ConcurrentBag<Point3D>();
		public ConcurrentBag<Point3D> linePoints = new ConcurrentBag<Point3D>();
		public ConcurrentBag<Point3D> keyPoints = new ConcurrentBag<Point3D>();

		ScatterPlot profilPlot;
		ScatterPlot circlePlot;
		ScatterPlot diffBetweenNeighborsPlot;
		ScatterPlot diffBetweenNeighborhoodPlot;
		ScatterPlot distanceGroupToCirclePlot;
		ScatterPlot fittedLinesPlot;
		ScatterPlot polyRegresionPlot;

		List<ScatterPlot> denthPlot = new List<ScatterPlot>();
		List<ScatterPlot> keyPointsPlot = new List<ScatterPlot>();

		List<Tuple<Point, int>> slopeNeighborhood = new List<Tuple<Point, int>>();
		List<Tuple<int, double>> distanceD5D0 = new List<Tuple<int, double>>();
		List<Tuple<int, double>> distanceD5Line = new List<Tuple<int, double>>();

		private int LastHighlightedIndex = -1;
		private List<IPlottable> keyPointTxt = new List<IPlottable>();
		private List<IPlottable> dentPointTxt = new List<IPlottable>();
		public List<Profil> profiles;
		private List<ProcessEscapeType> processEscapeTypes = new List<ProcessEscapeType>();
		private List<int> circleProfiles;
		public string pathFiles;
		public string PathFiles { get => pathFiles; set => pathFiles = value; }
		public List<Profil> Profiles { get => profiles; set => profiles = value; }

		public IList<ProfilAnalisys> ProfilAnalisys
		{
			get;
			private set;
		}

		public float distanceSearchAreaNeighborhood = 0;
		public int numberOfNeighborhood = 0;
		public float distanceToCenterOfCircle = 0;
		public float minWidthD5D0 = 0;
		public float maxWidthD5D0 = 0;
		public int groupCircleDistance = 0;
		private static double scala = 34;
		private List<Tuple<int, PolynomialRegression>> polyRegList;


		MarkerPlot HighlightedPoint;
		bool fireTrackBar = true;
		ResourceDictionary resourceDictionary;
		LoadingOverlay splashScreen = new LoadingOverlay();

		public Evaluation(ResourceDictionary resourceDictionary)
		{
			InitializeComponent();
			this.resourceDictionary = resourceDictionary;
			Resources.MergedDictionaries.Add(resourceDictionary);
			showRange = new ShowRange(resourceDictionary);
			WpfPlot1.Configuration.WarnIfRenderNotCalledManually = false;

			Task.Run(() => {
				Dispatcher.BeginInvoke(new Action(() =>
				{
					splashScreen.Show();
				}));
			});
		}

		public async Task CreateEvaluation(string pathFiles, List<Profil> profiles
			, float distanceSearchAreaNeighborhood, int numberOfNeighborhood, float distanceToCenterOfCircle
			, float maxWidthD5D0, float minWidthD5D0, int groupCircleDistance, bool skipStartEnd
			, List<Tuple<int, PolynomialRegression>> polyRegList)
		{

			await Task.Run(() =>
			{
				try
				{
					PathFiles = pathFiles;
					Profiles = profiles;
					this.distanceSearchAreaNeighborhood = distanceSearchAreaNeighborhood;
					this.numberOfNeighborhood = numberOfNeighborhood;
					this.distanceToCenterOfCircle = distanceToCenterOfCircle;
					this.maxWidthD5D0 = maxWidthD5D0;
					this.minWidthD5D0 = minWidthD5D0;
					this.groupCircleDistance = groupCircleDistance;
					this.polyRegList = polyRegList;
					
					LoadProcessEscapeType();

					// Add a red circle we can move around later as a highlighted point indicator
					HighlightedPoint = WpfPlot1.Plot.AddPoint(0, 0);
					HighlightedPoint.Color = System.Drawing.Color.Red;
					HighlightedPoint.MarkerSize = 10;
					HighlightedPoint.MarkerShape = ScottPlot.MarkerShape.openCircle;
					HighlightedPoint.IsVisible = false;

					DataSource dataSource = new DataSource(PathFiles);

					// Wait for all tasks to complete.
					List<Task> tasks = new List<Task>();

					tasks.Add(Task.Run(() => infoResultTable = dataSource.GenerateDataTable("result.csv")));
					tasks.Add(Task.Run(() => circleTable = dataSource.GenerateDataTable("circles.xyz", false)));
					tasks.Add(Task.Run(() => keyPointsTable = dataSource.GenerateDataTable("keyPoints.xyz", false)));
					tasks.Add(Task.Run(() => denthTable = dataSource.GenerateDataTable("dent.xyz", false)));
					tasks.Add(Task.Run(() => fittedLines = dataSource.GenerateDataTable("fittedLines.poly", false)));
					tasks.Add(Task.Run(() => circleEcuation = dataSource.GenerateDataTable("EcCircle.xyz", false)));

					Task.WaitAll(tasks.ToArray());

					AddDenthPoints();

					if (skipStartEnd)
					{
						infoResultTable = FilterResultV2(infoResultTable);
					}

					var results = (from ir in infoResultTable.AsEnumerable()
								   select new ProfilAnalisys
								   {
									   Perfil = (int)Math.Round(float.Parse((string)ir.ItemArray[0])),
									   Width = (string)ir.ItemArray[1],
									   WidthRatio = (string)ir.ItemArray[2],
									   Depth = (string)ir.ItemArray[3],
									   DepthScale = (string)ir.ItemArray[4],
									   TransitionPointDistance = (string)ir.ItemArray[5],
									   TransitionPointScale = (string)ir.ItemArray[6],
									   ProfileGap = (string)ir.ItemArray[7],
									   WidthOk = (string)ir.ItemArray[8],
									   DepthOk = (string)ir.ItemArray[9],
									   TransitionPointOk = (string)ir.ItemArray[10],
									   HadCircle = (string)ir.ItemArray[11],
									   WidthD0D5 = distanceD5D0.Where(x => x.Item1 == (int)Math.Round(float.Parse((string)ir.ItemArray[0])))
																.Select(x => x.Item2).FirstOrDefault() != 0 ?
																	distanceD5D0.Where(x => x.Item1 == (int)Math.Round(float.Parse((string)ir.ItemArray[0])))
																		.Select(x => x.Item2).FirstOrDefault().ToString("n6", CultureInfo.InvariantCulture)
																		: string.Empty,
									   WidthD0D5Ok = WidthD0D5OK(distanceD5D0.Where(x => x.Item1 == (int)Math.Round(float.Parse((string)ir.ItemArray[0])))
																.Select(x => x.Item2).FirstOrDefault()),

									   WidthRelative = distanceD5Line.Where(x => x.Item1 == (int)Math.Round(float.Parse((string)ir.ItemArray[0])))
																.Select(x => x.Item2).FirstOrDefault() != 0 ?
																	distanceD5Line.Where(x => x.Item1 == (int)Math.Round(float.Parse((string)ir.ItemArray[0])))
																		.Select(x => x.Item2).FirstOrDefault().ToString("n6", CultureInfo.InvariantCulture)
																		: string.Empty,
									   ProcessEscapeTypeName =
										 processEscapeTypes.Where(x => x.ProcessEscapeTypeId.ToString() == (string)ir.ItemArray[12]).FirstOrDefault() != null ?
											 processEscapeTypes.Where(x => x.ProcessEscapeTypeId.ToString() == (string)ir.ItemArray[12]).FirstOrDefault().ProcessEscapeTypeName : string.Empty,

									   ProcessEscapeTypeId = (string)ir.ItemArray[12],

									   PositionMM = (string)ir.ItemArray[13]

								   }).ToList();

					ProfilAnalisys = new List<ProfilAnalisys>();
					ProfilAnalisys = results;

					circleProfiles = circleTable.AsEnumerable().Select(x => int.Parse(x.Field<string>("Col1"))).Distinct().ToList();
					double tbValue = 0;

					Dispatcher.BeginInvoke(new Action(() =>
					{
						tbValue = trackBar1.Value;
					}));

					Task.Factory.StartNew(() =>
					{
						int profilNumber = circleProfiles.FirstOrDefault() != 0 ? circleProfiles.FirstOrDefault() : (int)Math.Round(profiles[Convert.ToInt32(tbValue)].profilNumber);
						SelecValueByProfilNumber(profilNumber);
					});

					Dispatcher.BeginInvoke(new Action(() =>
					{
						trackBar1.Minimum = 0;
						trackBar1.Maximum = infoResultTable.Rows.Count - 1;
						dataGridView1.ItemsSource = ProfilAnalisys;
					}));

					distanceD5D0.Clear();
					var gapsList = GetGaps(ProfilAnalisys.ToList());

					LoadResumePanel(gapsList);
					//showRange.SetGapList(gapsList);
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("Error in Evaluation Constructor: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			});

			//Start create the 3D Model
			//await Task.Run(() => CreateView3D());
		}

		private void CreateView3D()
		{
			try
			{
				LoadData();
				Dispatcher.BeginInvoke(new Action(() =>
					{
						ScatterPlotVisual3D sc = new ScatterPlotVisual3D();

						ModelVisual3D visualProfil = new ModelVisual3D();
						ModelVisual3D visualCircle = new ModelVisual3D();
						ModelVisual3D visualKeyPoints = new ModelVisual3D();
						ModelVisual3D visualDent = new ModelVisual3D();

						visualProfil.Content = sc.CreateModel(profilePoints.ToArray(), Materials.Blue, 0.06, 2, 2);
						visualCircle.Content = sc.CreateModel(circlePoints.ToArray(), Materials.Red, 0.06, 2, 2);
						visualKeyPoints.Content = sc.CreateModel(keyPoints.ToArray(), Materials.Gray, 0.25, 2, 2);
						visualDent.Content = sc.CreateModel(dentPoints.ToArray(), Materials.Gold, 0.25, 2, 2);

						ViewPort3D.Children.Add(visualProfil);
						ViewPort3D.Children.Add(visualCircle);
						ViewPort3D.Children.Add(visualKeyPoints);
						ViewPort3D.Children.Add(visualDent);

						var linePointLst = linePoints.ToList();

						for (int i = 0; i < linePointLst.Count; i = i + 2)
						{
							LinesVisual3D line = new LinesVisual3D();
							line.Color = Colors.Gold;
							line.Thickness = 4;
							line.Points.Add(linePointLst[i]);
							line.Points.Add(linePointLst[i + 1]);
							ViewPort3D.Children.Add(line);
						}

						foreach (var item in Profiles)
						{
							if (item.XPoint.Count() > 0 && item.ZPoint.Count() > 0)
							{
								TextVisual3D textVisual3D = new TextVisual3D();
								textVisual3D.Text = item.profilNumber.ToString();
								textVisual3D.FontSize = 55;
								textVisual3D.Height = 0.5;
								textVisual3D.Position = new Point3D(item.XPoint.Max(), item.ZPoint.Max(), item.profilNumber);
								ViewPort3D.Children.Add(textVisual3D);
							}
						}
					}
				));
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error in CreateView3D: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LoadData()
		{
			//profiles
			Parallel.ForEach(Profiles, currentProfil =>
			{
				Parallel.For(0, currentProfil.XPoint.Count(), (i, state) =>
				{
					profilePoints.Add(new Point3D(currentProfil.XPoint[i], currentProfil.ZPoint[i], currentProfil.profilNumber));
				});
			});

			//circles
			Parallel.ForEach(circleTable.AsEnumerable(), row =>
			{
				circlePoints.Add(new Point3D(float.Parse((string)row.ItemArray[0], CultureInfo.InvariantCulture),
					float.Parse((string)row.ItemArray[2], CultureInfo.InvariantCulture),
					float.Parse((string)row.ItemArray[1], CultureInfo.InvariantCulture)));
			});

			//keyPoints
			Parallel.ForEach(keyPointsTable.AsEnumerable(), row =>
			{
				keyPoints.Add(new Point3D(float.Parse((string)row.ItemArray[0], CultureInfo.InvariantCulture),
					float.Parse((string)row.ItemArray[2], CultureInfo.InvariantCulture),
					float.Parse((string)row.ItemArray[1], CultureInfo.InvariantCulture)));
			});

			//denth
			Parallel.ForEach(denthTable.AsEnumerable(), row =>
			{
				dentPoints.Add(new Point3D(float.Parse((string)row.ItemArray[0], CultureInfo.InvariantCulture),
					float.Parse((string)row.ItemArray[2], CultureInfo.InvariantCulture),
					float.Parse((string)row.ItemArray[1], CultureInfo.InvariantCulture)));
			});

			//fittedLine
			foreach (DataRow item in fittedLines.AsEnumerable())
			{
				linePoints.Add(new Point3D(float.Parse((string)item.ItemArray[0], CultureInfo.InvariantCulture),
					float.Parse((string)item.ItemArray[2], CultureInfo.InvariantCulture),
					float.Parse((string)item.ItemArray[1], CultureInfo.InvariantCulture)));
			}
		}

		private void LoadProcessEscapeType()
		{
			ProcessEscapeType pet = new ProcessEscapeType();

			processEscapeTypes = pet.GetProcessEscapeTypes();
		}

		private void LoadResumePanel(List<Tuple<string, string>> gapsList)
		{

			var data = from pro in infoResultTable.AsEnumerable()
					   select new
					   {
						   ProfilGap = (string)pro.ItemArray[7],
						   WidthOk = (string)pro.ItemArray[8],
						   Width = (string)pro.ItemArray[1],
						   DepthOk = (string)pro.ItemArray[9],
						   Depth = (string)pro.ItemArray[3],
						   TransitionOk = (string)pro.ItemArray[10],
						   Transition = (string)pro.ItemArray[5],
						   ProcessEscapeTypeId = (string)pro.ItemArray[12],
					   };

			var profilGap = data.Select(x => int.Parse(!string.IsNullOrEmpty(x.ProfilGap) ? x.ProfilGap : "0")).ToList();

			Dispatcher.BeginInvoke(new Action(() =>
			{
				//trackbar will ask the index
				totalAnalyzedProfilesTxt.Text = $"{data.Where(x => x.ProcessEscapeTypeId == "-1").Count()} {Resources.MergedDictionaries.First()["totalAnalyzedProfilesTxt"]}";
				totalAnalyzedProfilesTxt.Text = totalAnalyzedProfilesTxt.Text.Replace("[COUNT]", data.Count().ToString());

				profilTotalGapTxt.Text = gapsList.Count().ToString();//profilGap.Where(x => x > 0).Count().ToString();
				profilMaxGapTxt.Text = gapsList.Count() > 0 ? gapsList.Select(x => int.Parse(x.Item2) - int.Parse(x.Item1) + 1).Max().ToString() : string.Empty;//profilGap.Max().ToString();
				profilAverageGapTxt.Text = ((double)profilGap.Where(x => x > 0).Count() / data.Count()).ToString("P2");
				widthAverageTxt.Text = data.Select(x => double.Parse(string.IsNullOrEmpty(x.Width) ? "0" : x.Width, CultureInfo.InvariantCulture)).ToList().Average().ToString("n4");
				widthOkCountTxt.Text = data.Where(y => y.WidthOk == "TRUE").Count().ToString();
				widthOkTotalAverageTxt.Text = ((double)data.Where(y => y.WidthOk == "TRUE").Count() / data.Count()).ToString("P2");
				depthAverageTxt.Text = data.Select(x => double.Parse(string.IsNullOrEmpty(x.Depth) ? "0" : x.Depth, CultureInfo.InvariantCulture)).ToList().Average().ToString("n4");
				depthOkCountTxt.Text = data.Where(y => y.DepthOk == "TRUE").Count().ToString();
				depthOkTotalAverageTxt.Text = ((double)data.Where(y => y.DepthOk == "TRUE").Count() / data.Count()).ToString("P2");
				transitionAverageTxt.Text = data.Select(x => double.Parse(x.Transition.Contains("nan") || string.IsNullOrEmpty(x.Transition) ? "0" : x.Transition, CultureInfo.InvariantCulture)).ToList().Average().ToString("n4");
				transitionOkCountTxt.Text = data.Where(y => y.TransitionOk == "TRUE").Count().ToString();
				transitionTotalAverage.Text = ((double)data.Where(y => y.TransitionOk == "TRUE").Count() / data.Count()).ToString("P2");
			}));	
		}

		private void dataGridView1_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				int profilNumber = circleProfiles.FirstOrDefault() != 0 ? circleProfiles.FirstOrDefault() : (int)Math.Round(profiles[Convert.ToInt32(trackBar1.Value)].profilNumber);
				UpdateFocusDataGrid(profilNumber);

				Task.Run(() => {

					Dispatcher.BeginInvoke(new Action(() =>
					{
						splashScreen.Close();
					}));
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error in dataGridView1_Loaded: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		/// <summary>
		/// This fuction will return a point with a distance "d" to the center of the circle, actually is the distance a constant  
		/// </summary>
		/// <param name="profilNumber"></param>
		/// <param name="orderedList"></param>
		/// <returns></returns>
		private Point? FindPointByDistanceToCircleCenter(string profilNumber, List<Point> orderedList, List<DataRow> circleEc)
		{
			try
			{
				//var circleEc = circleEcuation.AsEnumerable().Where(x => (string)x.ItemArray[0] == profilNumber).ToList();
				List<Tuple<Point, double>> nearlyPoints = new List<Tuple<Point, double>>();
				double radius = 0;
				double distance = 0;

				foreach (var point in orderedList)
				{
					//Distance from a point "p" to the center circle 
					distance = DistanceBetweenPoints(point.X, point.Y, float.Parse((string)circleEc.First().ItemArray[1], CultureInfo.InvariantCulture), float.Parse((string)circleEc.First().ItemArray[3], CultureInfo.InvariantCulture));
					//distance = Math.Pow(p.X - float.Parse((string)circleEc.First().ItemArray[1]), 2) + Math.Pow(p.Y - float.Parse((string)circleEc.First().ItemArray[3]), 2);
					//radius of circle
					//radius = Math.Pow(float.Parse((string)circleEc.First().ItemArray[2]), 2);
					radius = float.Parse((string)circleEc.First().ItemArray[2], CultureInfo.InvariantCulture);

					if (Math.Abs(distance - radius) < distanceSearchAreaNeighborhood)
					{
						return point;
					}
				}

				return null;

				//if (nearlyPoints.Count() > 0)
				//{
				//	return nearlyPoints.First().Item1;
				//}
				//else
				//{
				//	return null;
				//}
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		private void SelecValueByProfilNumber(int profilNumber)
		{
			try
			{
				var profil = profiles.Where(x => x.profilNumber == profilNumber).FirstOrDefault();

				// Wait for all tasks to complete.
				List<Task> tasks = new List<Task>();
				tasks.Add(Task.Run(() => PlottCircle(profilNumber)));
				tasks.Add(Task.Run(() =>
				{

					List<double> kpx = new List<double>();
					List<double> kpy = new List<double>();

					PlottKeyPoint(profilNumber, kpx, kpy);
					PlottDenth(profilNumber, profil, kpx);

				}));

				tasks.Add(Task.Run(() => PlottFittedLine(profilNumber)));
				tasks.Add(Task.Run(() => PlottProfil(profil)));
				tasks.Add(Task.Run(() => PlottPoRe(profil)));

				Task.WaitAll(tasks.ToArray());

				Dispatcher.BeginInvoke(new Action(() =>
				{
					WpfPlot1.Render();
					currentProfilTxt.Text = profilNumber.ToString();
				}));
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void UpdateFocusDataGrid(int profilNumber)
		{
			if (dataGridView1.Items.Count > 0)
			{
				var profil = profiles.Where(x => x.profilNumber == profilNumber).FirstOrDefault();
				var index = Convert.ToInt32(trackBar1.Value);
				dataGridView1.UpdateLayout();
				dataGridView1.ScrollIntoView(dataGridView1.Items[index]);
				DataGridRow row = (DataGridRow)dataGridView1.ItemContainerGenerator.ContainerFromIndex(index);
				if (row != null)
				{
					TextBlock cellContent = dataGridView1.Columns[0].GetCellContent(row) as TextBlock;
					if (cellContent != null /*&& cellContent.Text.Equals(currentProfilTxt.Text)*/)
					{
						object item = dataGridView1.Items[index];
						dataGridView1.SelectedItem = item;
						dataGridView1.ScrollIntoView(item);
						row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
						//break;
					}
				}
			}
		}

		private void PlottCircle(int profilNumber)
		{
			if (circleProfiles.Where(x => x == profilNumber).FirstOrDefault() != 0)
			{
				var dataCircle = circleTable.AsEnumerable().Where(x => (string)x.ItemArray[1] == profilNumber.ToString()).ToList();
				List<double> cx = new List<double>();
				List<double> cy = new List<double>();
				foreach (DataRow item in dataCircle)
				{
					cx.Add(double.Parse((string)item.ItemArray[0], CultureInfo.InvariantCulture));
					cy.Add(double.Parse((string)item.ItemArray[2], CultureInfo.InvariantCulture));
					//cy.Add(double.Parse((string)item.ItemArray[2], CultureInfo.InvariantCulture) * -1);
				}

				if (circlePlot != null)
				{
					circlePlot.Update(cx.ToArray(), cy.ToArray());
					//lock (WpfPlot1)
					//{
					//	WpfPlot1.Refresh();
					//}
				}
				else
				{
					lock (WpfPlot1)
					{
						circlePlot = WpfPlot1.Plot.AddScatter(cx.ToArray(), cy.ToArray(), System.Drawing.Color.IndianRed, 0, 2, ScottPlot.MarkerShape.filledCircle, ScottPlot.LineStyle.None);
					}
				}
			}
			else
			{
				lock (WpfPlot1)
				{
					WpfPlot1.Plot.Remove(circlePlot);
				}

				circlePlot = null;
			}
		}

		private void PlottKeyPoint(int profilNumber, List<double> kpx, List<double> kpy)
		{

			if (keyPointsTable.AsEnumerable().Where(x => (string)x.ItemArray[1] == profilNumber.ToString()).FirstOrDefault() != null)
			{
				RemovePlottedTxt(keyPointTxt);

				var keyPoints = keyPointsTable.AsEnumerable().Where(x => (string)x.ItemArray[1] == profilNumber.ToString()).ToList();

				foreach (var keyPoint in keyPoints)
				{
					kpx.Add(double.Parse((string)keyPoint.ItemArray[0], CultureInfo.InvariantCulture));
					kpy.Add(double.Parse((string)keyPoint.ItemArray[2], CultureInfo.InvariantCulture));
					//kpy.Add(double.Parse((string)keyPoint.ItemArray[2], CultureInfo.InvariantCulture) * -1);
				}

				//if (keyPointsPlot != null)
				if (keyPointsPlot.Count() > 0)
				{
					for (int i = 0; i < kpy.Count(); i++)
					{
						lock (WpfPlot1)
						{
							keyPointTxt.Add(WpfPlot1.Plot.AddText(string.Format("KP{0}", i), kpx[i], kpy[i], size: 16, System.Drawing.Color.Green));
						}

						double[] x = { kpx[i] };
						double[] y = { kpy[i] };

						keyPointsPlot[i].Update(x, y);
					}

					for (int i = 0; i < keyPointTxt.Count; i++)
					{
						//keyPointTxt[i].IsVisible = (graphPoints.Items[dentPointTxt.Count + i + 1] as MenuItem).IsChecked;
						//keyPointsPlot[i].IsVisible = (graphPoints.Items[dentPointTxt.Count + i + 1] as MenuItem).IsChecked;
					}
				}
				else
				{
					for (int i = 0; i < kpy.Count(); i++)
					{
						lock (WpfPlot1)
						{
							keyPointTxt.Add(WpfPlot1.Plot.AddText(string.Format("KP{0}", i), kpx[i], kpy[i], size: 16, System.Drawing.Color.Green));

							double[] x = { kpx[i] };
							double[] y = { kpy[i] };

							keyPointsPlot.Add(WpfPlot1.Plot.AddScatter(x, y, System.Drawing.Color.Green, 0, 10, ScottPlot.MarkerShape.filledCircle, ScottPlot.LineStyle.None, "KP"));
						}

					}
					for (int i = 0; i < keyPointTxt.Count; i++)
					{
						//keyPointTxt[i].IsVisible = (graphPoints.Items[dentPointTxt.Count + i + 1] as MenuItem).IsChecked;
						//keyPointsPlot[i].IsVisible = (graphPoints.Items[dentPointTxt.Count + i + 1] as MenuItem).IsChecked;
					}
				}
			}
			else
			{
				RemovePlottedTxt(keyPointTxt);

				foreach (var item in keyPointsPlot)
				{
					lock (WpfPlot1)
					{
						WpfPlot1.Plot.Remove(item);
					}
				}

				keyPointsPlot.Clear();
			}
		}

		private void PlottFittedLine(int profilNumber)
		{
			if (fittedLines.AsEnumerable().Where(x => (string)x.ItemArray[1] == profilNumber.ToString()).FirstOrDefault() != null)
			{
				var lines = fittedLines.AsEnumerable().Where(x => (string)x.ItemArray[1] == profilNumber.ToString()).ToList();
				List<double> ftx = new List<double>();
				List<double> fty = new List<double>();
				foreach (var line in lines)
				{
					ftx.Add(double.Parse((string)line.ItemArray[0], CultureInfo.InvariantCulture));
					fty.Add(double.Parse((string)line.ItemArray[2], CultureInfo.InvariantCulture));
					//fty.Add(double.Parse((string)line.ItemArray[2], CultureInfo.InvariantCulture) * -1);
				}

				if (fittedLinesPlot != null)
				{
					lock (WpfPlot1)
					{
						fittedLinesPlot.Update(ftx.ToArray(), fty.ToArray());
						//WpfPlot1.Refresh();
					}

				}
				else
				{
					lock (WpfPlot1)
					{
						fittedLinesPlot = WpfPlot1.Plot.AddScatter(ftx.ToArray(), fty.ToArray(), System.Drawing.Color.Gray, 2, 2, ScottPlot.MarkerShape.filledCircle, ScottPlot.LineStyle.Solid);
					}
				}
			}
			else
			{
				lock (WpfPlot1)
				{
					WpfPlot1.Plot.Remove(fittedLinesPlot);
				}

				fittedLinesPlot = null;
			}
		}

		private void PlottDenth(int profilNumber, Profil profil, List<double> kpx)
		{
			List<Point> points = new List<Point>();

			for (int i = 0; i < profil.XPoint.Count(); i++)
			{
				points.Add(new Point(profil.XPoint[i], profil.ZPoint[i]));
			}

			var orderedList = points.OrderBy(x => x.X).ToList();
			List<double> denthx = new List<double>();
			List<double> denthy = new List<double>();

			if (denthTable.AsEnumerable().Where(x => (string)x.ItemArray[1] == profilNumber.ToString()).FirstOrDefault() != null)
			{
				RemovePlottedTxt(dentPointTxt);

				foreach (var row in denthTable.AsEnumerable().Where(x => (string)x.ItemArray[1] == profilNumber.ToString()))
				{
					denthx.Add(double.Parse((string)row.ItemArray[0], CultureInfo.InvariantCulture));
					denthy.Add(double.Parse((string)row.ItemArray[2], CultureInfo.InvariantCulture));
					//denthy.Add(double.Parse((string)row.ItemArray[2], CultureInfo.InvariantCulture) * 0.5);
				}

				//Scop to add or update Scatter.
				if (denthPlot.Count() > 0)
				{
					for (int i = 0; i < denthx.Count(); i++)
					{
						int di = i;
						if (i == 2) di = 3;
						if (i == 3) di = 2;

						lock (WpfPlot1)
						{
							dentPointTxt.Add(WpfPlot1.Plot.AddText($"D {di}", denthx[i], denthy[i], size: 16, System.Drawing.Color.Green));
						}


						double[] x = { denthx[i] };
						double[] y = { denthy[i] };

						denthPlot[i].Update(x, y);
					}

					Dispatcher.BeginInvoke(new Action(() =>
					{
						for (int i = 0; i < dentPointTxt.Count; i++)
						{
							dentPointTxt[i].IsVisible = (graphPoints.Items[i + 1] as MenuItem).IsChecked;
							denthPlot[i].IsVisible = (graphPoints.Items[i + 1] as MenuItem).IsChecked;
						}
					}));
				}
				else
				{
					for (int i = 0; i < denthx.Count(); i++)
					{
						int di = i;
						if (i == 2) di = 3;
						if (i == 3) di = 2;

						lock (WpfPlot1)
						{
							dentPointTxt.Add(WpfPlot1.Plot.AddText($"D {di}", denthx[i], denthy[i], size: 16, System.Drawing.Color.DarkCyan));

							double[] x = { denthx[i] };
							double[] y = { denthy[i] };

							denthPlot.Add(WpfPlot1.Plot.AddScatter(x, y, System.Drawing.Color.DarkCyan, 3, 10, ScottPlot.MarkerShape.filledCircle, ScottPlot.LineStyle.None));
						}
					}

					Dispatcher.BeginInvoke(new Action(() =>
					{
						for (int i = 0; i < dentPointTxt.Count; i++)
						{
							dentPointTxt[i].IsVisible = (graphPoints.Items[i + 1] as MenuItem).IsChecked;
							denthPlot[i].IsVisible = (graphPoints.Items[i + 1] as MenuItem).IsChecked;
						}
					}));

				}
			}
			else
			{
				RemovePlottedTxt(dentPointTxt);

				lock (WpfPlot1)
				{
					foreach (var item in denthPlot)
					{
						WpfPlot1.Plot.Remove(item);
					}

					WpfPlot1.Plot.Remove(diffBetweenNeighborsPlot);
					WpfPlot1.Plot.Remove(diffBetweenNeighborhoodPlot);
					WpfPlot1.Plot.Remove(distanceGroupToCirclePlot);
				}

				denthPlot.Clear();
				diffBetweenNeighborsPlot = null;
				diffBetweenNeighborhoodPlot = null;
				distanceGroupToCirclePlot = null;
			}
		}

		/// <summary>
		/// This function apply to find (for now) only the Point D5
		/// </summary>
		/// <param name="orderedList"></param>
		/// <param name="toPoint"></param>
		/// <param name="pointNearlyToCircle"></param>
		/// <param name="profilNumber"></param>
		/// <param name="pointsGrp"></param>
		/// <param name="slopeGradeNeighborhood"></param>
		private Point? GetFarRegresionPoint(List<Point> orderedList, double toPoint, Point? pointNearlyToCircle, float profilNumber, int pointsGrp)
		{
			var searchArea = orderedList.Where(h => h.X >= pointNearlyToCircle.Value.X && h.X <= toPoint).ToList();

			//List<Tuple<Point, int>> slopeNeighborhoodAux = new List<Tuple<Point, int>>();
			List<Tuple<Point, int>> slopeNeighborsAux = new List<Tuple<Point, int>>();
			//List<Tuple<Point, int>> diffBetweenNeighborhoodAux = new List<Tuple<Point, int>>();
			Point? denthPoint = null;
			List<Point> trueTable = new List<Point>();
			List<Point> lastTrueTable = new List<Point>();
			List<Point> maxErrorPoints = new List<Point>();

			var lines = fittedLines.AsEnumerable().Where(d => (string)d.ItemArray[1] == profilNumber.ToString()).ToList();
			List<double> ftx = new List<double>();
			List<double> fty = new List<double>();
			foreach (var line in lines)
			{
				ftx.Add(double.Parse((string)line.ItemArray[0], CultureInfo.InvariantCulture));
				fty.Add(double.Parse((string)line.ItemArray[2], CultureInfo.InvariantCulture));
			}

			PolynomialRegression poly = polyRegList.Where(x => x.Item1 == (int)profilNumber).Select(x => x.Item2).First();

			for (int i = 0; i < searchArea.Count(); i++)
			{
				double result = 0;// result -> f(x)
				int pow = poly.Weights.Length;

				for (int j = 0; j < poly.Weights.Length; j++)
				{
					result = result + (poly.Weights[j] * Math.Pow(searchArea[i].X, pow));
					pow = pow - 1;
				}

				result = result + poly.Intercept;

				var error = searchArea[i].Y - result;

				if (!denthPoint.HasValue || denthPoint.Value.Y > error)
				{
					denthPoint = new Point(searchArea[i].X, error);
				}
			}

			if (denthPoint.HasValue)
			{
				denthPoint = new Point(denthPoint.Value.X, searchArea.Where(j => j.X == denthPoint.Value.X).First().Y);
			}

			return denthPoint;
		}

		[Obsolete]
		private void GetD7Regresion(List<Point> orderedList, double toPoint, Point? pointNearlyToCircle, float profilNumber, int pointsGrp, List<Point> slopeGradeNeighborhood)
		{
			var searchArea = orderedList.Where(h => h.X >= pointNearlyToCircle.Value.X && h.X <= toPoint).ToList();

			List<Tuple<Point, int>> slopeNeighborhoodAux = new List<Tuple<Point, int>>();
			List<Tuple<Point, int>> slopeNeighborsAux = new List<Tuple<Point, int>>();
			List<Tuple<Point, int>> diffBetweenNeighborhoodAux = new List<Tuple<Point, int>>();
			List<Point> trueTable = new List<Point>();
			List<Point> lastTrueTable = new List<Point>();
			List<Point> maxErrorPoints = new List<Point>();

			var lines = fittedLines.AsEnumerable().Where(d => (string)d.ItemArray[1] == profilNumber.ToString()).ToList();
			List<double> ftx = new List<double>();
			List<double> fty = new List<double>();
			foreach (var line in lines)
			{
				ftx.Add(double.Parse((string)line.ItemArray[0], CultureInfo.InvariantCulture));
				fty.Add(double.Parse((string)line.ItemArray[2], CultureInfo.InvariantCulture));
			}

			PolynomialRegression poly = polyRegList.Where(x => x.Item1 == (int)profilNumber).Select(x => x.Item2).First();

			for (int i = 0; i < searchArea.Count(); i++)
			{
				double result = 0;// result -> f(x)
				int pow = poly.Weights.Length;

				for (int j = 0; j < poly.Weights.Length; j++)
				{
					result = result + (poly.Weights[j] * Math.Pow(searchArea[i].X, pow));
					pow = pow - 1;
				}

				result = result + poly.Intercept;

				var error = searchArea[i].Y - result;

				slopeNeighborhoodAux.Add(new Tuple<Point, int>(new Point(searchArea[i].X, error + scala), (int)profilNumber));
			}

			Point dPoint = slopeNeighborhoodAux.OrderBy(j => j.Item1.Y).FirstOrDefault().Item1;
			if (dPoint != null)
			{
				double x = dPoint.X;
				double y = searchArea.Where(j => j.X == dPoint.X).First().Y;
				slopeGradeNeighborhood.Add(new Point(x, y));
				slopeNeighborhood.AddRange(slopeNeighborhoodAux);
			}
		}

		private void PlottPoRe(Profil profil)
		{

			#region
			// Let's retrieve the input and output data:
			double[] inputs = profil.XPoint.ToArray();  // X
			double[] outputs = profil.ZPoint.ToArray(); // Y
			List<double> cy = new List<double>();
			//int profilNumber = (int)profil.profilNumber;
			// We can create a learning algorithm
			//var ls = new PolynomialLeastSquares()
			//{
			//	Degree = 30
			//};

			// Now, we can use the algorithm to learn a polynomial
			PolynomialRegression poly = polyRegList.Where(x => x.Item1 == (int)profil.profilNumber).Select(x => x.Item2).First();

			for (int i = 0; i < inputs.Length; i++)
			{

				double result = 0;// result -> f(x)
				int pow = poly.Weights.Length;

				for (int j = 0; j < poly.Weights.Length; j++)
				{
					result = result + (poly.Weights[j] * Math.Pow(inputs[i], pow));
					pow = pow - 1;
				}

				cy.Add(result + poly.Intercept);


				//var distance = outputs[i] - result;

				//if (distance > 0.1)
				//{
				//	int index = profil.XPoint.IndexOf(inputs[i]);
				//	profil.XPoint.RemoveAt(index);
				//	profil.ZPoint.RemoveAt(index);
				//}
				//else if (distance < -0.3)
				//{
				//	int index = profil.XPoint.IndexOf(inputs[i]);
				//	profil.XPoint.RemoveAt(index);
				//	profil.ZPoint.RemoveAt(index);
				//}
			}
			#endregion

			if (polyRegresionPlot != null)
			{
				lock (WpfPlot1)
				{
					polyRegresionPlot.Update(inputs, cy.ToArray());
					//WpfPlot1.Render();
				}

			}
			else
			{
				lock (WpfPlot1)
				{
					polyRegresionPlot = WpfPlot1.Plot.AddScatter(inputs, cy.ToArray(), System.Drawing.Color.Orange, 2, 2, ScottPlot.MarkerShape.filledCircle, ScottPlot.LineStyle.Solid);
					//WpfPlot1.Render();
				}

			}

		}

		private void PlottProfil(Profil profil)
		{
			if (profilPlot != null)
			{
				profilPlot.Update(profil.XPoint.ToArray(), profil.ZPoint.Select(x => x).ToArray());

				//lock (WpfPlot1)
				//{
				//	WpfPlot1.Render();
				//}

			}
			else
			{
				profilPlot = WpfPlot1.Plot.AddScatter(profil.XPoint.ToArray(), profil.ZPoint.Select(x => x).ToArray(), System.Drawing.Color.Blue, 0, 2, ScottPlot.MarkerShape.filledCircle, ScottPlot.LineStyle.None);

				//lock (WpfPlot1)
				//{
				//	WpfPlot1.Render();
				//}
			}
		}

		/// <summary>
		/// Return distance between two points
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		private double DistanceBetweenPoints(double px1, double py1, double px2, double py2)
		{
			return Math.Sqrt(Math.Pow(px2 - px1, 2) + Math.Pow(py2 - py1, 2));
		}

		private void RemovePlottedTxt(List<IPlottable> pointTxt)
		{
			foreach (var item in pointTxt)
			{
				lock (WpfPlot1)
				{
					WpfPlot1.Plot.Remove(item);
				}

			}
			pointTxt.Clear();
		}

		private void trackBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (fireTrackBar)
			{
				var profilNumber = (string)infoResultTable.AsEnumerable().ToList()[Convert.ToInt32(trackBar1.Value)].ItemArray.First();
				SelecValueByProfilNumber(Convert.ToInt32(profilNumber));
				UpdateFocusDataGrid(Convert.ToInt32(profilNumber));
			}

			fireTrackBar = true;
		}

		/// <summary>
		/// this function will filter the starts and ends profiles.
		/// </summary>
		/// <param name="infoResultTable"></param>
		private DataTable FilterResult(DataTable infoResultTable)
		{
			infoResultTable.AcceptChanges();
			////loop from the start profil to the first analysed profil
			foreach (var row in infoResultTable.AsEnumerable())
			{
				if (string.IsNullOrEmpty((string)row.ItemArray[1]) && string.IsNullOrEmpty((string)row.ItemArray[2]) && string.IsNullOrEmpty((string)row.ItemArray[3])
					&& string.IsNullOrEmpty((string)row.ItemArray[4]) && string.IsNullOrEmpty((string)row.ItemArray[5]))
				{
					row.Delete();
				}
				else
				{
					break;
				}
			}

			infoResultTable.AcceptChanges();

			//lastProfiles
			foreach (var row in infoResultTable.AsEnumerable().Reverse())
			{
				if (string.IsNullOrEmpty((string)row.ItemArray[1]) && string.IsNullOrEmpty((string)row.ItemArray[2]) && string.IsNullOrEmpty((string)row.ItemArray[3])
					&& string.IsNullOrEmpty((string)row.ItemArray[4]) && string.IsNullOrEmpty((string)row.ItemArray[5]))
				{
					row.Delete();
				}
				else
				{
					break;
				}
			}

			infoResultTable.AcceptChanges();
			return infoResultTable;
		}
		private DataTable FilterResultV2(DataTable infoResultTable)
		{
			infoResultTable.AcceptChanges();
			////loop from the start profil to the first analysed profil
			foreach (var row in infoResultTable.AsEnumerable())
			{
				if (string.IsNullOrEmpty((string)row.ItemArray[1]) && string.IsNullOrEmpty((string)row.ItemArray[2]) && string.IsNullOrEmpty((string)row.ItemArray[3])
					&& string.IsNullOrEmpty((string)row.ItemArray[4]) && string.IsNullOrEmpty((string)row.ItemArray[5]))
				{
					row.Delete();
				}
				else
				{
					break;
				}
			}

			infoResultTable.AcceptChanges();

			int idx = Profiles.Count() - 1;
			//lastProfiles
			foreach (var row in infoResultTable.AsEnumerable().Reverse())
			{
				if (Profiles[idx].scannedData < 500)//500 scannedData points
				{
					row.Delete();
				}
				else
				{
					break;
				}
				idx--;
			}

			infoResultTable.AcceptChanges();
			return infoResultTable;
		}

		private List<Tuple<string, string>> GetGaps(List<ProfilAnalisys> profilAnalisys)
		{
			string startProfil = string.Empty;
			string endProfil = string.Empty;
			List<Tuple<string, string>> gapList = new List<Tuple<string, string>>();

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
					gapList.Add(new Tuple<string, string>(startProfil, endProfil));
					startProfil = string.Empty;
					endProfil = string.Empty;
					continue;
				}

				if (string.IsNullOrEmpty(endProfil) && i == profilAnalisys.Count() - 1 && profilAnalisys[i].HadCircle == "FALSE")
				{
					if (string.IsNullOrEmpty(startProfil))
					{
						endProfil = profilAnalisys[i].Perfil.ToString();
						gapList.Add(new Tuple<string, string>(endProfil, endProfil));
						startProfil = string.Empty;
						endProfil = string.Empty;
					}
					else
					{
						endProfil = profilAnalisys[i].Perfil.ToString();
						gapList.Add(new Tuple<string, string>(startProfil, endProfil));
						startProfil = string.Empty;
						endProfil = string.Empty;
					}
				}
			}

			return gapList;
		}

		private string WidthD0D5OK(double width)
		{
			if (width >= minWidthD5D0 && width <= maxWidthD5D0)
			{
				return string.Format("TRUE");
			}

			return string.Format("FALSE");
		}

		private double DistancePointToBaseline(Point point, float profilNumber)
		{
			var lines = fittedLines.AsEnumerable().Where(x => (string)x.ItemArray[1] == profilNumber.ToString()).ToList();
			List<double> ftx = new List<double>();
			List<double> fty = new List<double>();
			foreach (var line in lines)
			{
				ftx.Add(double.Parse((string)line.ItemArray[0], CultureInfo.InvariantCulture));
				fty.Add(double.Parse((string)line.ItemArray[2], CultureInfo.InvariantCulture));
			}

			//line equation of baseplate (y - y1)= m(x-x1)
			//m*x-y+c = 0 -> c = m*-x1
			double m = (fty.Last() - fty.First()) / (ftx.Last() - ftx.First());
			var y1 = fty.First();
			var mftx = m * -1 * ftx.First();//c = m*-x1

			var d = Math.Abs(m * point.X - point.Y + ((mftx + y1)));
			return d / Math.Sqrt(Math.Pow(m, 2) + 1);
		}

		/// <summary>
		/// This function add the D4 and D5.
		/// For each profile the point D5 will be calculated only and only if there are previous D-points.
		/// For this, a respective limited area given by D4 will be analyzed. D4 is a point that is analyzed in 
		/// an area given by KP1 and D2. Furthermore, it is a random point given a distance away from the radius of 
		/// the circle and D2 who is inside the dent.
		/// D5 is selected given a list which has all the neighbors relative to a point. 
		/// The point that has the furthest neighbor will be chosen, for our purpose it will be
		/// the one that has a value in the lowest dependent coordinate.
		/// </summary>
		private void AddDenthPoints()
		{
			foreach (var profil in profiles)
			{
				try
				{
					List<Point> points = new List<Point>();
					List<double> denthx = new List<double>();
					List<double> denthy = new List<double>();
					Point? pointNearlyOfCircle = new Point();//this is D4
					List<double> kpx = new List<double>();
					List<double> kpy = new List<double>();

					//only if only exists D-Points
					if (denthTable.AsEnumerable().Where(x => (string)x.ItemArray[1] == profil.profilNumber.ToString()).FirstOrDefault() != null)
					{
						var circleEc = circleEcuation.AsEnumerable().Where(x => (string)x.ItemArray[0] == profil.profilNumber.ToString()).ToList();

						var centerCircleX = float.Parse((string)circleEc.First().ItemArray[1], CultureInfo.InvariantCulture);
						var centerCircleY = float.Parse((string)circleEc.First().ItemArray[3], CultureInfo.InvariantCulture);

						scala = centerCircleY - 0.05;
						//take the saved KeyPoints
						foreach (var keyPoint in keyPointsTable.AsEnumerable().Where(x => (string)x.ItemArray[1] == profil.profilNumber.ToString()))
						{
							kpx.Add(double.Parse((string)keyPoint.ItemArray[0], CultureInfo.InvariantCulture));
							kpy.Add(double.Parse((string)keyPoint.ItemArray[2], CultureInfo.InvariantCulture));
						}
						//take the saved Denthpoints (from D0 to D3)
						foreach (var row in denthTable.AsEnumerable().Where(x => (string)x.ItemArray[1] == profil.profilNumber.ToString()))
						{
							denthx.Add(double.Parse((string)row.ItemArray[0], CultureInfo.InvariantCulture));
							denthy.Add(double.Parse((string)row.ItemArray[2], CultureInfo.InvariantCulture));
						}
						//save the profil points into an ordered list 
						for (int i = 0; i < profil.XPoint.Count(); i++)
						{
							points.Add(new Point(profil.XPoint[i], profil.ZPoint[i]));
						}

						var orderedList = points.OrderBy(x => x.X).ToList();

						//D4
						pointNearlyOfCircle = FindPointByDistanceToCircleCenter(profil.profilNumber.ToString(), orderedList.Where(x => x.X > kpx[1] && x.X < denthy[3]).ToList(), circleEc);

						if (pointNearlyOfCircle.HasValue)
						{
							denthx.Add(pointNearlyOfCircle.Value.X);
							denthy.Add(pointNearlyOfCircle.Value.Y);
							denthTable.Rows.Add(new Object[] { pointNearlyOfCircle.Value.X.ToString("n4", CultureInfo.InvariantCulture), profil.profilNumber, pointNearlyOfCircle.Value.Y.ToString("n4", CultureInfo.InvariantCulture), "85", "255", "255" });

							//GetD7Regresion(orderedList, centerCircleX < denthx[4] ? denthx[0] : centerCircleX, pointNearlyOfCircle, profil.profilNumber, numberOfNeighborhood, slopeGradeNeighborhood);

							var dentPoint = GetFarRegresionPoint(orderedList, centerCircleX < denthx[4] ? denthx[0] : centerCircleX, pointNearlyOfCircle, profil.profilNumber, numberOfNeighborhood);
							
							if (dentPoint.HasValue)
							{
								denthTable.Rows.Add(new Object[] { dentPoint.Value.X.ToString("n4", CultureInfo.InvariantCulture), profil.profilNumber, dentPoint.Value.Y.ToString("n4", CultureInfo.InvariantCulture), "85", "255", "255" });
							}

							/*
							 if (slopeGradeNeighborhood.Count() > 0)
							{
								denthTable.Rows.Add(new Object[] { slopeGradeNeighborhood.OrderBy(x => x.Y).FirstOrDefault().X.ToString("n4", CultureInfo.InvariantCulture), profil.profilNumber, slopeGradeNeighborhood.OrderBy(x => x.Y).FirstOrDefault().Y.ToString("n4", CultureInfo.InvariantCulture), "85", "255", "255" });
							}*/

							distanceD5D0.Add(new Tuple<int, double>((int)profil.profilNumber, DistanceBetweenPoints(dentPoint.Value.X, dentPoint.Value.Y, denthx[0], denthy[0])));

							distanceD5Line.Add(new Tuple<int, double>((int)profil.profilNumber, DistancePointToBaseline(new Point(dentPoint.Value.X, dentPoint.Value.Y), profil.profilNumber)));

						}

						//D6 -> test of Appointment 05.08
						#region
						/*List<double> distanceToCenterCircle = new List<double>();//int -> index
						List<double> distanceDiff = new List<double>();
						//Distance to center of circle
						foreach (var item in orderedList)
						{
							distanceToCenterCircle.Add(DistanceBetweenPoints(item.X, item.Y, centerCircleX, centerCircleY));
						}

						distanceDiff.Add(0);

						for (int i = 1; i < distanceToCenterCircle.Count(); i++)
						{
							distanceDiff.Add(distanceToCenterCircle[i] - distanceToCenterCircle[i - 1]);
						}

						List<Tuple<int, double, int>> distanceGroupToCircleAux = new List<Tuple<int, double, int>>();

						distanceGroupToCircleAux.Add(new Tuple<int, double, int>((int)profil.profilNumber, scala, 0));

						for (int i = 1; i < distanceDiff.Count(); i++)
						{
							//0.05 use to scale the points in the plot, it has not efect for the calculation
							var sum = distanceGroupToCircleAux[i - 1].Item2;
							var count = distanceGroupToCircleAux[i - 1].Item3;

							distanceGroupToCircleAux.Add(
									new Tuple<int, double, int>(
												(int)profil.profilNumber,
												Math.Abs(distanceDiff[i]) < distanceToCenterOfCircle ? sum + 0.05 : scala,
												Math.Abs(distanceDiff[i]) < distanceToCenterOfCircle ? count + 1 : 0)
									);
						}

						distanceGroupToCircle.AddRange(distanceGroupToCircleAux);

						//find the first index of the point that satified the condition
						var index = distanceGroupToCircleAux.FindIndex(x => x.Item3 >= groupCircleDistance);
						denthTable.Rows.Add(new Object[] { orderedList[index].X.ToString("n4", CultureInfo.InvariantCulture), profil.profilNumber, orderedList[index].Y.ToString("n4", CultureInfo.InvariantCulture), "85", "255", "255" });*/
						#endregion
						//D7
						#region
						/*if (pointNearlyOfCircle.HasValue)
						{
							GetD7Regresion(orderedList, centerCircleX < denthx[4] ? denthx[0] : centerCircleX, pointNearlyOfCircle, profil.profilNumber, numberOfNeighborhood, slopeGradeNeighborhood);
							//GetD7(orderedList, centerCircleX < denthx[4] ? denthx[0] : centerCircleX, pointNearlyOfCircle, profil.profilNumber, numberOfNeighborhood, slopeGradeNeighborhood);
							if (slopeGradeNeighborhood.Count() > 0)
							{
								denthTable.Rows.Add(new Object[] { slopeGradeNeighborhood.OrderBy(x => x.Y).FirstOrDefault().X.ToString("n4", CultureInfo.InvariantCulture), profil.profilNumber, slopeGradeNeighborhood.OrderBy(x => x.Y).FirstOrDefault().Y.ToString("n4", CultureInfo.InvariantCulture), "85", "255", "255" });
							}
						}*/
						#endregion
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(string.Format("Error in AddDenthPoint profil {0}: {1}", profil.profilNumber, ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
				}

			}
		}

		private void resetZoomBtn1_Click(object sender, RoutedEventArgs e)
		{
			//chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
			//chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset();
		}

		private void DataGridRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender != null)
			{
				DataGrid grid = sender as DataGrid;
				if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
				{
					DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
					var an = (ProfilAnalisys)dgr.Item;
					SelecValueByProfilNumber(an.Perfil);
					//set trackbar
					var index = profiles.FindIndex(x => x.profilNumber == an.Perfil);
					trackBar1.Value = index;
				}
			}
		}

		private void WpfPlot1_MouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				// determine point nearest the cursor
				var coor = WpfPlot1.GetMouseCoordinates();
				double xyRatio = WpfPlot1.Plot.XAxis.Dims.PxPerUnit / WpfPlot1.Plot.YAxis.Dims.PxPerUnit;
				var point = profilPlot.GetPointNearest(coor.x, coor.y, xyRatio);

				// place the highlight over the point of interest
				HighlightedPoint.X = point.x;
				HighlightedPoint.Y = point.y;
				HighlightedPoint.IsVisible = true;

				// render if the highlighted point chnaged
				if (LastHighlightedIndex != point.index)
				{
					LastHighlightedIndex = point.index;
					WpfPlot1.Render();
				}

				coordinatesLbl.Content = $"Point index {point.index} at ({point.x:N4}, {point.y:N4})";
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error in WpfPlot1_MouseMove: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void dataGridView1_KeyUp(object sender, KeyEventArgs e)
		{
			if (sender != null && (e.Key == Key.Up || e.Key == Key.Down))
			{
				DataGrid grid = sender as DataGrid;

				if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
				{
					DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
					var an = (ProfilAnalisys)dgr.Item;
					SelecValueByProfilNumber(an.Perfil); ;
					//set trackbar
					//updateGridView = true;
					//var index = profiles.FindIndex(x => x.profilNumber == an.Perfil);
					var index = infoResultTable.AsEnumerable().ToList().FindIndex(x => (string)x.ItemArray[0] == an.Perfil.ToString());
					trackBar1.Value = index;
				}
			}
		}

		private void trackBar1_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (sender != null)
			{
				if (e.Delta > 0 && trackBar1.Value < profiles.Count())
				{
					var index = trackBar1.Value + 1;
					trackBar1.Value = index;
					int profilNumber = (int)Math.Round(profiles[Convert.ToInt32(trackBar1.Value)].profilNumber);
					SelecValueByProfilNumber(profilNumber);
				}
				else if (e.Delta < 0 && trackBar1.Value > 0)
				{
					var index = trackBar1.Value - 1;
					trackBar1.Value = index;
					int profilNumber = (int)Math.Round(profiles[Convert.ToInt32(trackBar1.Value)].profilNumber);
					SelecValueByProfilNumber(profilNumber);
				}
			}
		}

		private void trackBar1_KeyUp(object sender, KeyEventArgs e)
		{
			if (sender != null)
			{
				if (e.Key == Key.Up && trackBar1.Value < profiles.Count())
				{
					var index = trackBar1.Value + 1;
					trackBar1.Value = index;
					int profilNumber = (int)Math.Round(profiles[Convert.ToInt32(trackBar1.Value)].profilNumber);
					SelecValueByProfilNumber(profilNumber);
				}
				else if (e.Key == Key.Down && trackBar1.Value > 0)
				{
					var index = trackBar1.Value - 1;
					trackBar1.Value = index;
					int profilNumber = (int)Math.Round(profiles[Convert.ToInt32(trackBar1.Value)].profilNumber);
					SelecValueByProfilNumber(profilNumber);
				}
			}

		}

		private void dataGridView1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (sender != null)
			{
				DataGrid grid = sender as DataGrid;
				if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
				{
					DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
					var an = (ProfilAnalisys)dgr.Item;
					SelecValueByProfilNumber(an.Perfil);
					//set trackbar
					//updateGridView = true;
					fireTrackBar = false;
					var index = profiles.FindIndex(x => x.profilNumber == an.Perfil);
					trackBar1.Value = index;
				}
			}
		}

		private void showProfilChkbox_Checked(object sender, RoutedEventArgs e)
		{
			if (profilPlot != null)
			{
				profilPlot.IsVisible = true;
				WpfPlot1.Refresh();
			}
		}

		private void showProfilChkbox_Unchecked(object sender, RoutedEventArgs e)
		{
			if (profilPlot != null)
			{
				profilPlot.IsVisible = false;
				WpfPlot1.Refresh();
			}
		}

		private void showBaseplateChkbox_Checked(object sender, RoutedEventArgs e)
		{
			if (fittedLinesPlot != null)
			{
				fittedLinesPlot.IsVisible = true;
				WpfPlot1.Refresh();
			}
		}

		private void showBaseplateChkbox_Unchecked(object sender, RoutedEventArgs e)
		{
			if (fittedLinesPlot != null)
			{
				fittedLinesPlot.IsVisible = false;
				WpfPlot1.Refresh();
			}
		}

		private void showCircleChkbox_Checked(object sender, RoutedEventArgs e)
		{
			if (circlePlot != null)
			{
				circlePlot.IsVisible = true;
				WpfPlot1.Refresh();
			}
		}

		private void showCircleChkbox_Unchecked(object sender, RoutedEventArgs e)
		{
			if (circlePlot != null)
			{
				circlePlot.IsVisible = false;
				WpfPlot1.Refresh();
			}
		}

		private void showPointsChkbox_Checked(object sender, RoutedEventArgs e)
		{
			//if (denthPlot != null)
			if (denthPlot.Count() > 0)
			{
				//denthPlot.IsVisible = true;
				foreach (var item in denthPlot)
				{
					item.IsVisible = true;
				}

				foreach (var item in keyPointTxt)
				{
					item.IsVisible = true;
				}

				foreach (MenuItem item in graphPoints.Items)
				{
					item.IsChecked = true;
				}

				WpfPlot1.Refresh();
			}

			if (keyPointsPlot.Count() > 0)
			{
				//keyPointsPlot.IsVisible = true;

				foreach (var item in keyPointsPlot)
				{
					item.IsVisible = true;
				}

				foreach (var item in dentPointTxt)
				{
					item.IsVisible = true;
				}

				WpfPlot1.Refresh();
			}
		}

		private void showPointsChkbox_Unchecked(object sender, RoutedEventArgs e)
		{
			//if (denthPlot != null)
			if (denthPlot.Count() > 0)
			{
				//denthPlot.IsVisible = false;

				foreach (var item in denthPlot)
				{
					item.IsVisible = false;
				}

				foreach (var item in keyPointTxt)
				{
					item.IsVisible = false;
				}

				foreach (MenuItem item in graphPoints.Items)
				{
					item.IsChecked = false;
				}

				WpfPlot1.Refresh();
			}

			//if (keyPointsPlot != null)
			if (keyPointsPlot.Count() > 0)
			{
				//keyPointsPlot.IsVisible = false;

				foreach (var item in keyPointsPlot)
				{
					item.IsVisible = false;
				}

				foreach (var item in dentPointTxt)
				{
					item.IsVisible = false;
				}

				WpfPlot1.Refresh();
			}
		}

		private void graphShowDPoint(MenuItem menuItem, int index, bool show)
		{
			if (denthPlot.Count() > 0)
			{
				menuItem.IsChecked = show;
				denthPlot[index].IsVisible = show;
				dentPointTxt[index].IsVisible = show;
				WpfPlot1.Refresh();
			}
		}

		private void graphShowKPPoint(MenuItem menuItem, int index, bool show)
		{
			if (keyPointsPlot.Count() > 0)
			{
				menuItem.IsChecked = show;
				keyPointsPlot[index].IsVisible = show;
				keyPointTxt[index].IsVisible = show;
				WpfPlot1.Refresh();
			}
		}

		private void graphShowD0_Checked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 0, true);
		}

		private void graphShowD0_Unchecked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 0, false);
		}

		private void graphShowD1_Checked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 1, true);
		}

		private void graphShowD1_Unchecked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 1, false);
		}

		private void graphShowD2_Checked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 3, true);
		}

		private void graphShowD2_Unchecked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 3, false);
		}

		private void graphShowD3_Checked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 2, true);
		}

		private void graphShowD3_Unchecked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 2, false);
		}

		private void graphShowD4_Checked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 4, true);
		}

		private void graphShowD4_Unchecked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 4, false);
		}

		private void graphShowK0_Checked(object sender, RoutedEventArgs e)
		{
			graphShowKPPoint(sender as MenuItem, 0, true);
		}

		private void graphShowK0_Unchecked(object sender, RoutedEventArgs e)
		{
			graphShowKPPoint(sender as MenuItem, 0, false);
		}

		private void graphShowK1_Checked(object sender, RoutedEventArgs e)
		{
			graphShowKPPoint(sender as MenuItem, 1, true);
		}

		private void graphShowK1_Unchecked(object sender, RoutedEventArgs e)
		{
			graphShowKPPoint(sender as MenuItem, 1, false);
		}

		private void graphShowK2_Checked(object sender, RoutedEventArgs e)
		{
			graphShowKPPoint(sender as MenuItem, 2, true);
		}

		private void graphShowK2_Unchecked(object sender, RoutedEventArgs e)
		{
			graphShowKPPoint(sender as MenuItem, 2, false);
		}

		private void graphShowD5_Checked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 5, true);
		}

		private void graphShowD5_Unchecked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 5, false);
		}

		private void graphShowDiffBetweenNeighbors_Checked(object sender, RoutedEventArgs e)
		{
			if (diffBetweenNeighborsPlot != null)
			{
				diffBetweenNeighborsPlot.IsVisible = true;
				WpfPlot1.Refresh();
			}
		}

		private void graphShowDiffBetweenNeighbors_Unchecked(object sender, RoutedEventArgs e)
		{
			if (diffBetweenNeighborsPlot != null)
			{
				diffBetweenNeighborsPlot.IsVisible = false;
				WpfPlot1.Refresh();
			}
		}

		private void graphShowDiffNeighborhood_Checked(object sender, RoutedEventArgs e)
		{
			if (diffBetweenNeighborhoodPlot != null)
			{
				diffBetweenNeighborhoodPlot.IsVisible = true;
				WpfPlot1.Refresh();
			}
		}

		private void graphShowDiffNeighborhood_Unchecked(object sender, RoutedEventArgs e)
		{
			if (diffBetweenNeighborhoodPlot != null)
			{
				diffBetweenNeighborhoodPlot.IsVisible = false;
				WpfPlot1.Refresh();
			}
		}

		private void graphShowDiffDistCircle_Checked(object sender, RoutedEventArgs e)
		{
			if (distanceGroupToCirclePlot != null)
			{
				distanceGroupToCirclePlot.IsVisible = true;
				WpfPlot1.Refresh();
			}
		}

		private void graphShowDiffDistCircle_Unchecked(object sender, RoutedEventArgs e)
		{
			if (distanceGroupToCirclePlot != null)
			{
				distanceGroupToCirclePlot.IsVisible = false;
				WpfPlot1.Refresh();
			}
		}

		private void graphShowD6_Checked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 6, true);
		}

		private void graphShowD6_Unchecked(object sender, RoutedEventArgs e)
		{
			graphShowDPoint(sender as MenuItem, 6, false);
		}

		private void LabelShowRange_Click(object sender, RoutedEventArgs e)
		{
			showRange.Show();
		}

		private void graphShowPolyRegresion_Checked(object sender, RoutedEventArgs e)
		{
			if (polyRegresionPlot != null)
			{
				polyRegresionPlot.IsVisible = true;
				WpfPlot1.Refresh();
			}
		}

		private void graphShowPolyRegresion_Unchecked(object sender, RoutedEventArgs e)
		{
			if (polyRegresionPlot != null)
			{
				polyRegresionPlot.IsVisible = false;
				WpfPlot1.Refresh();
			}
		}

		private void exportResultBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				// Create the CSV file to which grid data will be exported.
				StreamWriter sw = new StreamWriter(new FileStream(Path.Combine(PathFiles, "export.csv"), FileMode.Create, FileAccess.Write));

				DataSet ds = ConvertToDataSet(ProfilAnalisys, "table");

				DataTable dt = ds.Tables[0];
				//CultureInfo.InvariantCulture.TextInfo.ListSeparator = ";";
				int iColCount = dt.Columns.Count;
				for (int i = 0; i < iColCount; i++)
				{
					sw.Write(dt.Columns[i]);
					if (i < iColCount - 1)
					{
						sw.Write(";");
					}
				}
				sw.Write(sw.NewLine);
				// Now write all the rows.
				foreach (DataRow dr in dt.Rows)
				{
					for (int i = 0; i < iColCount; i++)
					{
						if (!Convert.IsDBNull(dr[i]))
						{
							sw.Write(dr[i].ToString());
						}
						if (i < iColCount - 1)
						{
							sw.Write(";");
						}
					}
					sw.Write(sw.NewLine);
				}
				sw.Close();

				MessageBox.Show($"Export successful in {Path.Combine(PathFiles, "export.csv")}", "INFORMATION", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Exception in Export Table: {ex.Message}", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public DataSet ConvertToDataSet<T>(IEnumerable<T> source, string name)
		{
			if (source == null)
				throw new ArgumentNullException("source ");
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");
			var converted = new DataSet(name);
			converted.Tables.Add(NewTable(name, source));
			return converted;
		}

		private DataTable NewTable<T>(string name, IEnumerable<T> list)
		{
			PropertyInfo[] propInfo = typeof(T).GetProperties();
			DataTable table = Table<T>(name, list, propInfo);
			IEnumerator<T> enumerator = list.GetEnumerator();
			while (enumerator.MoveNext())
				table.Rows.Add(CreateRow<T>(table.NewRow(), enumerator.Current, propInfo));
			return table;
		}

		private DataRow CreateRow<T>(DataRow row, T listItem, PropertyInfo[] pi)
		{
			foreach (PropertyInfo p in pi)
				row[p.Name.ToString()] = p.GetValue(listItem, null);
			return row;
		}

		private DataTable Table<T>(string name, IEnumerable<T> list, PropertyInfo[] pi)
		{
			DataTable table = new DataTable(name);
			foreach (PropertyInfo p in pi)
				table.Columns.Add(p.Name, p.PropertyType);
			return table;
		}

	}

	public class ChangeColorCell : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((string)parameter == "row")
			{
				string input = (string)value;
				switch (input)
				{
					case "":
						return DependencyProperty.UnsetValue;
					default:
						return Brushes.OrangeRed;

				}
			}
			else if ((string)parameter == "number")
			{
				if (!string.IsNullOrEmpty((string)value))
				{
					int gap = Int32.Parse((string)value);

					if (gap > 0) return Brushes.YellowGreen;

				}

				return DependencyProperty.UnsetValue;
			}
			else
			{
				string input = (string)value;
				switch (input)
				{
					case "TRUE":
						return Brushes.LightGreen;
					case "FALSE":
						return Brushes.Orange;
					default:
						return Brushes.YellowGreen;
				}
			}

		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

}
