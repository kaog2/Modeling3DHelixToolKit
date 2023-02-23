using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Globalization;
using Modeling3DHelixToolKit.Models;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Modeling3DHelixToolKit.ViewModel;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Point3D = System.Windows.Media.Media3D.Point3D;
using PolynomialRegression = Accord.Statistics.Models.Regression.Linear.PolynomialRegression;
using PolynomialRegressionCreated = Modeling3DHelixToolKit.Classes.PolynomialRegression;
using System.Windows.Media;
using Accord.Statistics.Models.Regression.Linear;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Collections.Concurrent;

namespace Modeling3DHelixToolKit
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		[DllImport("EthernetScanner.dll", EntryPoint = "EthernetScanner_Connect", CallingConvention = CallingConvention.StdCall)]
		//private unsafe static extern IntPtr EthernetScanner_Connect(StringBuilder chIP, StringBuilder chPort, int uiNonBlockingTimeOut);
		public static extern IntPtr EthernetScanner_Connect(string ipAddress, string port, int nonBlockingTimeOut);

		[DllImport("EthernetScanner.dll", EntryPoint = "EthernetScanner_Disconnect", CallingConvention = CallingConvention.StdCall)]
		public static extern IntPtr EthernetScanner_Disconnect(IntPtr hScanner);

		[DllImport("EthernetScanner.dll", EntryPoint = "EthernetScanner_GetConnectStatus", CallingConvention = CallingConvention.StdCall)]
		public static extern void EthernetScanner_GetConnectStatus(IntPtr hScanner, int[] uiConnectStatus);

		[DllImport("EthernetScanner.dll", EntryPoint = "EthernetScanner_WriteData", CallingConvention = CallingConvention.StdCall)]
		public static extern int EthernetScanner_WriteData(IntPtr hScanner, byte[] chWriteBuffer, int uiWriteBufferLength);

		[DllImport("EthernetScanner.dll", EntryPoint = "EthernetScanner_GetVersion", CallingConvention = CallingConvention.StdCall)]
		public static extern int EthernetScanner_GetVersion(StringBuilder ucBuffer, int uiBuffer);

		[DllImport("EthernetScanner.dll", EntryPoint = "EthernetScanner_GetXZIExtended", CallingConvention = CallingConvention.StdCall)]
		public static extern int EthernetScanner_GetXZIExtended(IntPtr sensorHandle,
																double[] x,
																double[] z,
																int[] intensity,
																int[] peakWidth,
																int buffer,
																int[] encoder,
																byte[] pucUSRIO,
																int timeOut,
																byte[] ucBufferRaw,
																int iBufferRaw,
																int[] picCount);

		[DllImport("EthernetScanner.dll", EntryPoint = "EthernetScanner_ReadData", CallingConvention = CallingConvention.StdCall)]
		public static extern int EthernetScanner_ReadData(IntPtr sensorHandle, string strPropertyName, StringBuilder RetBuf, int iRetBuf, int iCashTime);

		[DllImport("EthernetScanner.dll", EntryPoint = "EthernetScanner_GetInfo", CallingConvention = CallingConvention.StdCall)]
		public static extern int EthernetScanner_GetInfo(IntPtr sensorHandle, StringBuilder info, int buffer, string mode);

		[DllImport("EthernetScanner.dll", EntryPoint = "EthernetScanner_ResetDllFiFo", CallingConvention = CallingConvention.StdCall)]
		public static extern int EthernetScanner_ResetDllFiFo(IntPtr sensorHandle);

		[DllImport("EthernetScanner.dll", EntryPoint = "EthernetScanner_GetDllFiFoState", CallingConvention = CallingConvention.StdCall)]
		public static extern int EthernetScanner_GetDllFiFoState(IntPtr sensorHandle);

		[DllImport("Calculation.Library.dll", EntryPoint = "processProfile", CallingConvention = CallingConvention.StdCall)]
		static extern void processProfile(int pointCount, float y, float[] profiles, float minWidthParameter, float maxWidthParameter, float minDepthParameter,
											float maxDepthParameter, float calculatedWidth, float minTransition, float maxTransition, StringBuilder path, int pathLen,
											int isNewObject, int minGroupPointSize, int minGroupSizeOfValidPtsFitLine, float convertedMinDistanceConstant,
											float minDepthOfDeepestPointConstant, StringBuilder resultStr, StringBuilder dentStr, StringBuilder ecCircleStr,
											StringBuilder fittedLineStr, StringBuilder keyPointStr);


		/// <summary>
		/// Wenglor Sensor Variable Definition
		/// </summary>
		//Constants by default. -> ScannerSettigs
		//public int iETHERNETSCANNER_TCPSCANNERDISCONNECTED = 0;
		public int iETHERNETSCANNER_TCPSCANNERCONNECTED = 3; //Status connected
		public int iETHERNETSCANNER_PEAKSPERCMOSSCANLINEMAX = 2;
		public int iETHERNETSCANNER_SCANXMAX = 2048;
		public int iETHERNETSCANNER_GETINFOSIZEMAX = 128 * 1024;
		//public int iETHERNETSCANNER_GETINFONOVALIDINFO = -3;
		//public int iETHERNETSCANNER_GETINFOSMALLBUFFER = -2;
		//public int iETHERNETSCANNER_ERROR = -1;


		//Arrays for the Coordinates: X, Z, Intensity
		//MultiPeakDetection: up to 2 Z-Values on the same X-Position: environment light and/or reflexions
		public IntPtr ScannerHandle;
		public int[] iConnectionStatus = null;
		public string strIPAddress = string.Empty;
		public StringBuilder m_strScannerInfoXML = null;
		public int m_iScannerDataLen = 0; // valid Points -> pair (x,z)
		public int m_iProfilesAvail = 0;
		public double[] m_doX = null;
		public double[] m_doZ = null;
		public int[] m_iIntensity = null;
		public int[] m_iPeakWidth = null;
		public int[] m_iEncoder = null;
		public byte[] m_bUSRIO = null;
		public int[] m_iPicCnt = null;
		public int[] m_iPicCntTemp = null;

		/// <summary>
		/// UI Variable Definitions
		/// </summary>
		ParameterDetails parameterDetails = null;
		Evaluation evaluation = null;

		bool fromStart = true;
		bool endZero = false;
		int countShowProfil = 0;
		int countAddedProfil = 0;
		int countProcessData = 0;
		int processingProfiles = 0;
		int isNewObject = 0;
		double currentProfileNumber = 0;

		//parameters in App.Config
		public float minWidth = 0;
		public float maxWidth = 0;
		public float minDepth = 0;
		public float maxDepth = 0;
		public float minTransition = 0;
		public float maxTransition = 0;
		public float minWidthD5D0 = 0;
		public float maxWidthD5D0 = 0;
		public int minGroupPointSize = 0;
		public int groupCircleDistance = 0;
		public int minGroupSizeOfValidPtsFitLine = 0;
		public float convertedMinDistanceConstant = 0;
		public float minDepthOfDeepestPointConstant = 0;
		public float distanceSearchAreaNeighborhood = 0;
		public int numberOfNeighborhood = 0;
		public float distanceToCenterOfCircle = 0;
		public float calculatedWidth = 0;
		public float calculatedWidthR = 0;
		public float calculatedWidthH = 0;
		public bool enableEntryCalculatedParameter = false; //this is for the function S = Width
		public bool skipStartEnd = true;

		List<Profil> profiles = new List<Profil>();
		string pathFiles = string.Empty;
		string destinyFile = string.Empty;
		string selectedFile = string.Empty;
		List<Tuple<int, PolynomialRegression>> polyRegList = new List<Tuple<int, PolynomialRegression>>();

		Thread takeDataThread;
		Thread showDataThread;
		Thread processDataThread;
		Thread showProcDataThread;

		DateTime timeSavedFile;
		DateTime startTakeDataTime = new DateTime();
		DateTime startProcessDataTime = new DateTime();

		int timeBetweenProfils = 0;
		int countGoodProfil = 0;
		int countBadProfil = 0;
		bool block = false;
		//testdata
		bool test = false;
		List<int> processingProfilesLst = new List<int>();
		List<int> skipedProfilLst = new List<int>();

		ResourceDictionary resourceDictionary = new ResourceDictionary();
		ConcurrentBag<DentPoint> dentPointLst = new ConcurrentBag<DentPoint>();
		ConcurrentBag<EcCircle> ecCircleLst = new ConcurrentBag<EcCircle>();
		ConcurrentBag<BasePlate> basePlateLst = new ConcurrentBag<BasePlate>();
		ConcurrentBag<KeyPoint> keyPointLst = new ConcurrentBag<KeyPoint>();

		public MainWindow()
		{
			try
			{
				InitializeComponent();

				SwitchLanguage(CultureInfo.CurrentCulture.Name);

				this.DataContext = new MainWindowViewModel("192.168.100.250");

				stateTxt.Background = Brushes.LightSalmon;
				stateTxt.Text = string.Format("Disconnected");

				test = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["Test"]) && ConfigurationManager.AppSettings["Test"] == "true";
				timeBetweenProfils = Int32.Parse(ConfigurationManager.AppSettings["DistanceTimeProfil"]);
				minWidth = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MinWidth"]) ? float.Parse(ConfigurationManager.AppSettings["MinWidth"], CultureInfo.InvariantCulture) : 0;
				maxWidth = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MaxWidth"]) ? float.Parse(ConfigurationManager.AppSettings["MaxWidth"], CultureInfo.InvariantCulture) : 0;
				minDepth = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MinDepth"]) ? float.Parse(ConfigurationManager.AppSettings["MinDepth"], CultureInfo.InvariantCulture) : 0;
				maxDepth = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MaxDepth"]) ? float.Parse(ConfigurationManager.AppSettings["MaxDepth"], CultureInfo.InvariantCulture) : 0;
				minTransition = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MinTransition"]) ? float.Parse(ConfigurationManager.AppSettings["MinTransition"], CultureInfo.InvariantCulture) : 0;
				maxTransition = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MaxTransition"]) ? float.Parse(ConfigurationManager.AppSettings["MaxTransition"], CultureInfo.InvariantCulture) : 0;
				minGroupPointSize = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MinGroupPointSize"]) ? int.Parse(ConfigurationManager.AppSettings["MinGroupPointSize"], CultureInfo.InvariantCulture) : 0;
				minGroupSizeOfValidPtsFitLine = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MinGroupSizeOfValidPtsFitLine"]) ? int.Parse(ConfigurationManager.AppSettings["MinGroupSizeOfValidPtsFitLine"], CultureInfo.InvariantCulture) : 0;
				convertedMinDistanceConstant = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ConvertedMinDistanceConstant"]) ? float.Parse(ConfigurationManager.AppSettings["ConvertedMinDistanceConstant"], CultureInfo.InvariantCulture) : 0;
				minDepthOfDeepestPointConstant = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MinDepthOfDeepestPointConstant"]) ? float.Parse(ConfigurationManager.AppSettings["MinDepthOfDeepestPointConstant"], CultureInfo.InvariantCulture) : 0;
				distanceSearchAreaNeighborhood = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["DistanceSearchAreaNeighborhood"]) ? float.Parse(ConfigurationManager.AppSettings["DistanceSearchAreaNeighborhood"], CultureInfo.InvariantCulture) : 0;
				numberOfNeighborhood = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["NumberOfNeighborhood"]) ? int.Parse(ConfigurationManager.AppSettings["NumberOfNeighborhood"], CultureInfo.InvariantCulture) : 0;
				distanceToCenterOfCircle = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["DistanceToCenterOfCircle"]) ? float.Parse(ConfigurationManager.AppSettings["DistanceToCenterOfCircle"], CultureInfo.InvariantCulture) : 0;
				minWidthD5D0 = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MinWidthD5D0"]) ? float.Parse(ConfigurationManager.AppSettings["MinWidthD5D0"], CultureInfo.InvariantCulture) : 0;
				maxWidthD5D0 = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["MaxWidthD5D0"]) ? float.Parse(ConfigurationManager.AppSettings["MaxWidthD5D0"], CultureInfo.InvariantCulture) : 0;
				groupCircleDistance = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["GroupCircleDistance"]) ? int.Parse(ConfigurationManager.AppSettings["GroupCircleDistance"], CultureInfo.InvariantCulture) : 0;
				skipStartEnd = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["SkipStartEnd"]) && ConfigurationManager.AppSettings["SkipStartEnd"] == "true";

				parameterDetails = new ParameterDetails(this, resourceDictionary);
				//evaluation = new Evaluation(pathFiles, profiles);

				if (!test)
					testModus.Visibility = Visibility.Collapsed;

				buttonDisconnect.IsEnabled = false;

				//Pointer to the EthernetScanner
				ScannerHandle = (IntPtr)null;
				//EthernetScanner connection status
				iConnectionStatus = new int[1];

				//Arrays for the Coordinates: X, Z, Intensity
				//MultiPeakDetection: up to 2 Z-Values on the same X-Position: environment light and/or reflexions 
				m_doX = new double[iETHERNETSCANNER_SCANXMAX * iETHERNETSCANNER_PEAKSPERCMOSSCANLINEMAX + 1];
				m_doZ = new double[iETHERNETSCANNER_SCANXMAX * iETHERNETSCANNER_PEAKSPERCMOSSCANLINEMAX + 1];
				m_iIntensity = new int[iETHERNETSCANNER_SCANXMAX * iETHERNETSCANNER_PEAKSPERCMOSSCANLINEMAX + 1];
				m_iPeakWidth = new int[iETHERNETSCANNER_SCANXMAX * iETHERNETSCANNER_PEAKSPERCMOSSCANLINEMAX + 1];
				m_iEncoder = new int[1];
				m_bUSRIO = new byte[1];
				m_iPicCnt = new int[1];
				m_iPicCntTemp = new int[1];

				m_strScannerInfoXML = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));

				//Task.Run(() => LoadPointCloud());
				//Task.Run(() => CreateView3DRealTime());
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("MainWindow Error: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ViewPortDefaultConfig()
		{
			ViewPort3D.CameraController.CameraUpDirection = new Vector3D(12.5510063830076, -11.8219718160806, 26.5087385809566);
			ViewPort3D.CameraController.CameraTarget = new Point3D(-34.8227512867219, 47.2486864217717, 325.864513461113);
			ViewPort3D.CameraController.CameraPosition = new Point3D(9.28255217984951, 17.436365245105, -46.5935366229902);
			ViewPort3D.Camera.Position = new Point3D(9.28255217984951, 17.436365245105, -46.5935366229902);
			ViewPort3D.CameraController.CameraLookDirection = new Vector3D(-44.1053034665714, 29.8123211766667, 372.458050084103);
			ViewPort3D.Camera.UpDirection = new Vector3D(12.5510063830076, -11.8219718160806, 26.5087385809566);
		}

		private void ResetValues()
		{
			profiles.Clear();
			polyRegList.Clear();
			dentPointLst = new ConcurrentBag<DentPoint>();
			//ecCircleLst.Clear();
			//keyPointLst.Clear();
			//basePlateLst.Clear();
			ecCircleLst = new ConcurrentBag<EcCircle>();
			basePlateLst = new ConcurrentBag<BasePlate>();
			keyPointLst = new ConcurrentBag<KeyPoint>();
			processingProfilesLst.Clear();
			skipedProfilLst.Clear();
			ViewPort3D.Children.Clear();
			ViewPort3D.Children.Add(new DefaultLights());
			countGoodProfil = 0;
			countBadProfil = 0;
			countShowProfil = 0;
			countAddedProfil = 0;
			isNewObject = 1;
		}

		private void buttonConnect_Click(object sender, EventArgs e)
		{
			ResetValues();

			if (evaluation != null)
			{
				evaluation.Close();
			}

			//evaluation = new Evaluation(pathFiles, profiles, resourceDictionary);

			if (!test)
			{
				//Check the Ip Format
				Regex rgx = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$");
				if (!rgx.IsMatch(textBoxIPAddress.Text))
				{
					System.Windows.MessageBox.Show("IP Address format incorrect", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
					return;
				}

				if (string.IsNullOrEmpty(destinyFile))
				{
					System.Windows.MessageBox.Show("please choose a destination path", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				if (enableEntryCalculatedParameter)
				{
					calculatedWidth = (float)(2 * Math.Sqrt((2 * calculatedWidthR * calculatedWidthH) - Math.Pow(calculatedWidthH, 2)));
				}
				else
				{
					calculatedWidth = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["CalculatedWidth"]) ? float.Parse(ConfigurationManager.AppSettings["CalculatedWidth"], CultureInfo.InvariantCulture) : 1;
				}

				if (ScannerHandle != (IntPtr)null)
					return;

				strIPAddress = textBoxIPAddress.Text;
				string strPort = "32001";
				int iTimeOut = Int32.Parse(string.Format("1000"));

				//Sensor Connection
				#region
				//start the connection to the Scanner
				ScannerHandle = EthernetScanner_Connect(strIPAddress, strPort, iTimeOut);

				//check the connection state with timeout 1500 ms
				DateTime startConnectTime = DateTime.Now;
				TimeSpan connectTime = new TimeSpan();
				do
				{
					if (connectTime.TotalMilliseconds > 1500)
					{
						ScannerHandle = EthernetScanner_Disconnect(ScannerHandle);
						System.Windows.MessageBox.Show("Error: No Connection!!!", "ERROR", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						return;
					}
					Thread.Sleep(10);
					EthernetScanner_GetConnectStatus(ScannerHandle, iConnectionStatus);
					connectTime = DateTime.Now - startConnectTime;
				} while (iConnectionStatus[0] != iETHERNETSCANNER_TCPSCANNERCONNECTED);

				int iGetInfoRes = EthernetScanner_GetInfo(ScannerHandle, m_strScannerInfoXML, iETHERNETSCANNER_GETINFOSIZEMAX, "xml");

				#endregion

				if (iGetInfoRes > 0)
				{
					buttonConnect.IsEnabled = false;
					buttonDisconnect.IsEnabled = true;

					stateTxt.Background = Brushes.LightGreen;
					stateTxt.Text = string.Format("Connected");

					fromStart = true;
					endZero = false;

					countProcessData = 0;

					takeDataThread = new Thread(ThTakeData);
					showDataThread = new Thread(ThShowData);

					startTakeDataTime = DateTime.Now;

					takeDataThread.Start();
					showDataThread.Start();
				}
				else
				{
					System.Windows.MessageBox.Show("Error: No Valid Info-Packet!!!", "ERROR", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				}
			}
			else
			{
				if (enableEntryCalculatedParameter)
				{
					calculatedWidth = (float)(2 * Math.Sqrt((2 * calculatedWidthR * calculatedWidthH) - Math.Pow(calculatedWidthH, 2)));
				}
				else
				{
					calculatedWidth = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["CalculatedWidth"]) ? float.Parse(ConfigurationManager.AppSettings["CalculatedWidth"], CultureInfo.InvariantCulture) : 1;
				}

				LoadPointCloud();

				Task.Run(() => ProcessProfiles());
				Task.Run(() => CreateView3DRealTime());

				//takeDataThread = new Thread(ThTakeDataTest);
				//takeDataThread.Start();
			}
		}

		private void CreateView3DRealTime()
		{
			while (true)
			{
				Thread.Sleep(20);

				if (!Dispatcher.CheckAccess())
				{
					Dispatcher.BeginInvoke(new Action(() =>
					{
						Update3DView();
					}
					));
				}

				if (countShowProfil >= profiles.Count())
					break;
			}

			//Add evaluation to Grid

			List<ProfilResult> pResult = new List<ProfilResult>();
			string evaluationResult = "Bad Quality welding";
			double evaluationRate = (double)countGoodProfil / (countGoodProfil + countBadProfil);

			if (evaluationRate > 0.7)
			{
				evaluationResult = "Good Quality welding";
			}
			else if (evaluationRate < 0.7 && evaluationRate > 0.5)
			{
				evaluationResult = "Middle Quality welding";
			}

			pResult.Add(new ProfilResult(countGoodProfil, countBadProfil, countGoodProfil + countBadProfil, evaluationResult));

			Dispatcher.BeginInvoke(new Action(() =>
				{
					dataGridView1.ItemsSource = pResult;
				}
			));
		}

		private void Update3DView()
		{
			try
			{
				List<Profil> profilLst = new List<Profil>(profiles.ToList());

				var isSkipedProfil = skipedProfilLst.Where(x => x == countShowProfil).ToList();

				if (isSkipedProfil.Count() > 0)
				{
					countShowProfil++;
				}
				else
				{
					var isProfileProcessed = processingProfilesLst.Where(x => x == countShowProfil).ToList();

					if (isProfileProcessed.Count() > 0)
					{
						var profilToShow = profilLst.Where(x => x.orderRead == countShowProfil).FirstOrDefault();

						if (profilToShow != null)
						{
							List<DentPoint> dpLst = new List<DentPoint>(dentPointLst.ToList());
							List<EcCircle> ecLst = new List<EcCircle>(ecCircleLst.ToList());
							List<KeyPoint> kpLst = new List<KeyPoint>(keyPointLst.ToList());
							List<BasePlate> bpLst = new List<BasePlate>(basePlateLst.ToList());
							//region for the others Profil features
							var circleToShow = ecLst.Where(x => x.profilNumber == profilToShow.profilNumber).FirstOrDefault();
							var dpToShow = dpLst.Where(x => x.profilNumber == profilToShow.profilNumber).ToList();
							var kpToShow = kpLst.Where(x => x.profilNumber == profilToShow.profilNumber).ToList();
							var bpToShow = bpLst.Where(x => x.profilNumber == profilToShow.profilNumber).ToList();

							if (circleToShow != null)
							{
								if (dpToShow.Count > 0 && kpToShow.Count > 0 && bpToShow.Count > 0)
								{
									countGoodProfil++;
									ScatterPlot3D(profilToShow, Materials.White);
								}
								else
								{
									countShowProfil--;
								}
							}
							else
							{
								if (profilToShow.scannedData > 500)
								{
									countBadProfil++;
								}

								ScatterPlot3D(profilToShow, Materials.Orange);
							}
						}
					}
				}

				#region
				/* 
				if (profilToShow != null)
				{
					//all points in the same scatterplot
					ScatterPlotVisual3D sc = new ScatterPlotVisual3D();
					//region only profile
					#region
					ModelVisual3D visualProfil = new ModelVisual3D();
					List<Point3D> profil3DPointLst = new List<Point3D>();

					for (int i = 0; i < profilToShow.XPoint.Count(); i++)
					{
						profil3DPointLst.Add(new Point3D(profilToShow.XPoint[i], profilToShow.ZPoint[i], profilToShow.profilNumber));
					}

					visualProfil.Content = sc.CreateModel(profil3DPointLst.ToArray(), Materials.Blue, 0.06, 2, 2);
					ViewPort3D.Children.Add(visualProfil);
					#endregion

					//region for the others Profil features
					#region
					var circleToShow = ecLst.Where(x => x.profilNumber == profilToShow.profilNumber).FirstOrDefault();
					var dpToShow = dpLst.Where(x => x.profilNumber == profilToShow.profilNumber).ToList();
					var kpToShow = kpLst.Where(x => x.profilNumber == profilToShow.profilNumber).ToList();
					var bpToShow = bpLst.Where(x => x.profilNumber == profilToShow.profilNumber).ToList();

					if (circleToShow != null && dpToShow.Count > 0 && kpToShow.Count > 0 && bpToShow.Count > 0)
					{
						ModelVisual3D visualCircle = new ModelVisual3D();
						ModelVisual3D visualDent = new ModelVisual3D();
						ModelVisual3D visualKp = new ModelVisual3D();
						ModelVisual3D visualBasePlate = new ModelVisual3D();

						List<Point3D> circle3DPointLst = new List<Point3D>();
						List<Point3D> dent3DPointLst = new List<Point3D>();
						List<Point3D> kp3DPointLst = new List<Point3D>();
						List<Point3D> bp3DPointLst = new List<Point3D>();

						double step = Math.PI / 180;
						double angle = 0;

						for (int i = 0; i < 360; i++)
						{
							var x = circleToShow.xc + circleToShow.radius * Math.Cos(angle);
							var y = circleToShow.zc + circleToShow.radius * Math.Sin(angle);
							circle3DPointLst.Add(new Point3D(x, y, circleToShow.profilNumber));
							angle += step;
						}

						foreach (var item in dpToShow)
						{
							dent3DPointLst.Add(new Point3D(item.dentPoint.X, item.dentPoint.Y, item.profilNumber));
						}
						
						foreach (var item in kpToShow)
						{
							kp3DPointLst.Add(new Point3D(item.keyPoint.X, item.keyPoint.Y, item.profilNumber));
						}
						
						foreach (var item in bpToShow)
						{
							bp3DPointLst.Add(new Point3D(item.point.X, item.point.Y, item.profilNumber));
						}

						visualCircle.Content = sc.CreateModel(circle3DPointLst.ToArray(), Materials.Red, 0.08, 3, 3);
						visualDent.Content = sc.CreateModel(dent3DPointLst.ToArray(), Materials.Green, 0.2, 4, 4);

						LinesVisual3D line = new LinesVisual3D();
						line.Color = Colors.Gold;
						line.Thickness = 4;
						
						foreach (var item in bp3DPointLst)
						{
							line.Points.Add(item);
						}

						ViewPort3D.Children.Add(line);
						ViewPort3D.Children.Add(visualCircle);
						ViewPort3D.Children.Add(visualDent);
					}
					#endregion

					countShowProfil++;
				}
				*/
				#endregion
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("UpdateChart Error: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ScatterPlot3D(Profil profil, Material material)
		{
			//all points in the same scatterplot
			ScatterPlotVisual3D sc = new ScatterPlotVisual3D();

			//region only profile
			ModelVisual3D visualProfil = new ModelVisual3D();
			List<Point3D> profil3DPointLst = new List<Point3D>();

			for (int i = 0; i < profil.XPoint.Count(); i++)
			{
				profil3DPointLst.Add(new Point3D(profil.XPoint[i], profil.ZPoint[i], profil.profilNumber));
			}

			visualProfil.Content = sc.CreateModel(profil3DPointLst.ToArray(), material, 0.15, 3, 3);
			ViewPort3D.Children.Add(visualProfil);
			countShowProfil++;
		}

		private void ThTakeDataTest()
		{
			LoadPointCloud();
		}

		private void buttonDisconnect_Click(object sender, EventArgs e)
		{
			takeDataThread.Abort();
			takeDataThread.Join();

			DateTime stopTakeDataTima = DateTime.Now;
			var totalTimeTakeData = stopTakeDataTima - startTakeDataTime;
			takeDataInfoTxt.Text = string.Format("Time taked data {0}:{1}:{2}", totalTimeTakeData.Hours, totalTimeTakeData.Minutes, totalTimeTakeData.Seconds);

			showDataThread.Abort();
			showDataThread.Join();

			countShowProfil = 0;
			countAddedProfil = 0;

			if (ScannerHandle != (IntPtr)null)
			{
				ScannerHandle = EthernetScanner_Disconnect(ScannerHandle);
				buttonConnect.IsEnabled = true;
				buttonDisconnect.IsEnabled = false;
				stateTxt.Background = Brushes.LightSalmon;
				stateTxt.Text = string.Format("Disconnected");
			}

			processDataThread = new Thread(ThProcessData);
			showProcDataThread = new Thread(ThShowProcessingData);
			processDataThread.Start();
			showProcDataThread.Start();
		}

		/// <summary>
		/// Finish Scan if Intensity is 0
		/// </summary>
		/// <param name="m_iIntensity"></param>
		private void CheckFinishScanByIntensity(int[] m_iIntensity)
		{
			//Check the Intensity, if all 0 then scan to nothing

			var isAllZero = m_iIntensity.All(x => x == 0);

			//scope to know if we have to end the scan on the End of the scaner Object.
			//It's happen when we are on the end of te scanned object. To disconect and finish the scan
			//endZero will by true if isAllZero is false, it is to controll and say to the program that we are now scanning and have to end when all is on the end zero.
			if (!isAllZero)
			{
				endZero = true;
			}

			if (m_bUSRIO[0] == 83)
			{
				//takeDataThread.Abort();
			}
			else if (isAllZero && fromStart && endZero)
			{
				fromStart = false;

				if (ScannerHandle != (IntPtr)null)
				{
					ScannerHandle = EthernetScanner_Disconnect(ScannerHandle);
					buttonConnect.IsEnabled = true;
					buttonDisconnect.IsEnabled = true;
					stateTxt.Background = Brushes.LightSalmon;
					stateTxt.Text = string.Format("Scan finished and disconnected");
				}

				processDataThread = new Thread(ThProcessData);
				//showProcDataThread = new Thread(ThShowProcessingData);
				processDataThread.Start();
				//showProcDataThread.Start();

				takeDataThread.Abort();
			}
		}

		private void LoadPointCloud()
		{
			if (!string.IsNullOrEmpty(selectedFile) && !string.IsNullOrEmpty(destinyFile))
			{
				switch (Path.GetExtension(selectedFile))
				{
					case ".xyzi":
						LoadTxt();
						break;
					case ".csv":
						LoadCsv();
						break;
					default:
						break;
				}
			}
			else
			{
				System.Windows.MessageBox.Show("Select a File and the destiny files path", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void LoadCsv()
		{
			try
			{
				startTakeDataTime = DateTime.Now;

				string file = selectedFile;
				float x = 0, y = 0, z = 0;
				int c = 0;

				int counter = 0;
				int added = 0;
				// To read a text file line by line
				if (File.Exists(file))
				{
					List<double> Xs = new List<double>();
					List<double> Zs = new List<double>();
					List<int> Cs = new List<int>();

					var lines = File.ReadLines(file);
					foreach (var line in lines)
					{
						x = 0;
						if (counter % 34 == 0)
						{
							foreach (var prof in line.Split(';'))
							{
								if (!prof.Contains("99,9999"))
								{
									y = counter;
									z = float.Parse(prof.Replace(',', '.'), CultureInfo.InvariantCulture);

									Xs.Add(x);
									Zs.Add(z);
									Cs.Add(c);

									x = x + (float)0.05;
								}
							}

							double[] xList = new double[Xs.Count()];
							double[] zList = new double[Zs.Count()];
							int[] cList = new int[Cs.Count()];
							Xs.CopyTo(xList);
							Zs.CopyTo(zList);
							Cs.CopyTo(cList);

							var transform = new RotateTransform() { Angle = 207, CenterX = 0, CenterY = 0 };
							List<double> px = new List<double>();
							List<double> py = new List<double>();

							for (int i = 0; i < xList.Count(); i++)
							{
								var transformedPoint = transform.Transform(new Point(xList[i], zList[i]));
								px.Add(transformedPoint.X);
								py.Add(transformedPoint.Y);
							}

							//profiles.Add(new Profil(float.Parse(prev_y.ToString()), xList.ToList(), zList.ToList(), cList.ToList(), counter, Xs.Count()));
							lock (profiles)
							{
								profiles.Add(new Profil(profiles.Count(), px, py, cList.ToList(), profiles.Count(), Xs.Count()));
							}

							
							Xs.Clear();
							Zs.Clear();
							Cs.Clear();
						}

						counter++;
					}
				}

				DateTime stopTakeDataTime = DateTime.Now;

				TimeSpan totalTimeTakedData = stopTakeDataTime - startTakeDataTime;

				if (!Dispatcher.CheckAccess())
				{
					Dispatcher.BeginInvoke(new Action(() =>
					{
						takeDataInfoTxt.Text = string.Format("{0} {1:hh\\:mm\\:ss}", Resources.MergedDictionaries.First()["mainTimeTakingDataTxt"], totalTimeTakedData);
					}
					));
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("Error in LoadCsv file: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LoadTxt()
		{
			float prev_y = -1;

			try
			{
				startTakeDataTime = DateTime.Now;

				string file = selectedFile;
				float x = 0, y = 0, z = 0;
				int c = 0;

				int counter = 0;

				// To read a text file line by line
				if (File.Exists(file))
				{
					List<double> Xs = new List<double>();
					List<double> Zs = new List<double>();
					List<int> Cs = new List<int>();

					var lines = File.ReadLines(file);
					foreach (var line in lines)
					{
						var prof = line.Split(' ');

						x = float.Parse(prof[0], CultureInfo.InvariantCulture);
						y = float.Parse(prof[3], CultureInfo.InvariantCulture);
						z = float.Parse(prof[6], CultureInfo.InvariantCulture);
						if (prof.Length > 7)
						{
							c = int.Parse(prof[9]);
						}
						else
						{
							c = 0;
						}


						if (prev_y == -1) prev_y = y;
						//To improve Kensly Algo... here is not taken the last profil
						if (y != prev_y)
						{
							double[] xList = new double[Xs.Count()];
							double[] zList = new double[Zs.Count()];
							int[] cList = new int[Cs.Count()];
							Xs.CopyTo(xList);
							Zs.CopyTo(zList);
							Cs.CopyTo(cList);

							var transform = new RotateTransform() { Angle = 0, CenterX = 0, CenterY = 0 };
							List<double> px = new List<double>();
							List<double> py = new List<double>();

							for (int i = 0; i < xList.Count(); i++)
							{
								var transformedPoint = transform.Transform(new Point(xList[i], zList[i]));
								px.Add(transformedPoint.X);
								py.Add(transformedPoint.Y);
							}

							//profiles.Add(new Profil(float.Parse(prev_y.ToString()), xList.ToList(), zList.ToList(), cList.ToList(), counter, Xs.Count()));
							lock (profiles)
							{
								profiles.Add(new Profil(float.Parse(prev_y.ToString()), px, py, cList.ToList(), counter, Xs.Count()));
							}

							counter++;
							prev_y = y;
							Xs.Clear();
							Zs.Clear();
							Cs.Clear();
						}
						else
						{
							Xs.Add(x);
							Zs.Add(z);
							Cs.Add(c);
						}
					}
				}

				DateTime stopTakeDataTime = DateTime.Now;

				TimeSpan totalTimeTakedData = stopTakeDataTime - startTakeDataTime;

				if (!Dispatcher.CheckAccess())
				{
					Dispatcher.BeginInvoke(new Action(() =>
					{
						takeDataInfoTxt.Text = string.Format("{0} {1:hh\\:mm\\:ss}", Resources.MergedDictionaries.First()["mainTimeTakingDataTxt"], totalTimeTakedData);
					}
					));
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("Error in LoadTxt file: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ProcessProfiles()
		{
			try
			{
				StringBuilder pathDestinyFiles;

				startProcessDataTime = DateTime.Now;

				string keyPoints = string.Empty;
				string fittedLine = string.Empty;

				double x = 0, y = 0, z = 0, c = 0;
				double previous_y = -1;

				timeSavedFile = DateTime.Now;
				pathFiles = Path.Combine(destinyFile, string.Format("RobAH Insight {0}", timeSavedFile.ToString("dd-MM-yyyy HH-mm-ss")));

				if (!Directory.Exists(pathFiles))
				{
					Directory.CreateDirectory(pathFiles);
				}

				foreach (var prof in profiles)
				{
					//if not contains points
					if (prof.XPoint.Count() < 1 || prof.ZPoint.Count() < 1)
					{
						skipedProfilLst.Add(prof.orderRead);
						continue;
					}

					Profil profil = new Profil(prof);
					y = profil.profilNumber;
					currentProfileNumber = y;
					List<float> profilList1 = new List<float>();

					//polyRegresion Library
					#region
					// Let's retrieve the input and output data:
					double[] inputs = profil.XPoint.ToArray();  // X
					double[] outputs = profil.ZPoint.ToArray(); // Y

					// We can create a learning algorithm
					var ls = new PolynomialLeastSquares()
					{
						Degree = 30
					};

					// Now, we can use the algorithm to learn a polynomial
					PolynomialRegression poly = ls.Learn(inputs, outputs);
					polyRegList.Add(new Tuple<int, PolynomialRegression>((int)profil.profilNumber, poly));

					for (int i = 0; i < inputs.Length; i++)
					{

						double result = 0;// result -> f(x)
						int pow = poly.Weights.Length;

						for (int j = 0; j < poly.Weights.Length; j++)
						{
							result = result + (poly.Weights[j] * Math.Pow(inputs[i], pow));
							pow = pow - 1;
						}

						result = result + poly.Intercept;

						var distance = outputs[i] - result;

						if (distance > 0.1)
						{
							int index = profil.XPoint.IndexOf(inputs[i]);
							profil.XPoint.RemoveAt(index);
							profil.ZPoint.RemoveAt(index);
						}
						else if (distance < -0.3)
						{
							int index = profil.XPoint.IndexOf(inputs[i]);
							profil.XPoint.RemoveAt(index);
							profil.ZPoint.RemoveAt(index);
						}
					}
					#endregion
					//polyRegresionCreated
					#region
					//List<Tuple<int, double>> xs = new List<Tuple<int, double>>();
					//List<Tuple<int, double>> zs = new List<Tuple<int, double>>();

					//for (int i = 0; i < profil.XPoint.Count(); i++)
					//{
					//	if (profil.XPoint[i] > 10 && profil.XPoint[i] < 15)
					//	{
					//		xs.Add(new Tuple<int, double>(profil.XPoint.IndexOf(profil.XPoint[i]), profil.XPoint[i]));
					//		zs.Add(new Tuple<int, double>(profil.ZPoint.IndexOf(profil.ZPoint[i]), profil.ZPoint[i]));
					//	}
					//}
					//int grad = FilterProfil(xs, zs, 4);

					//PolynomialRegressionCreated polynomialRegression = new PolynomialRegressionCreated();
					//var polyRegresion = polynomialRegression.GetPolynomialRegresion(xs.Select(t => t.Item2).ToList(),
					//	zs.Select(t => t.Item2).ToList(), grad);

					////evaluate X coor. in the polyRegresional function to filtrate points that are generating noise
					////create a new y array with values evaluated in the regresion function
					////create a new y array with the filtered points to be calculate
					//List<int> indexs = new List<int>();//regeresion results

					//for (int i = 0; i < xs.Count(); i++)
					//{
					//	var number = xs[i].Item2;

					//	double result = 0;// result -> f(x)

					//	for (int j = 0; j < polyRegresion.Length; j++)
					//	{
					//		result = result + (polyRegresion[j] * Math.Pow(number, j));
					//	}

					//	var distance = zs[i].Item2 - result;//Math.Abs(zs[i].Item2 - result);

					//	if (distance > 0.1)
					//	{
					//		int index = profil.XPoint.IndexOf(number);
					//		profil.XPoint.RemoveAt(index);
					//		profil.ZPoint.RemoveAt(index);
					//	}
					//	else if (distance < -0.3)
					//	{
					//		int index = profil.XPoint.IndexOf(number);
					//		profil.XPoint.RemoveAt(index);
					//		profil.ZPoint.RemoveAt(index);
					//	}
					//}
					//endofpolyregresion
					#endregion

					for (int i = 0; i < profil.XPoint.Count; i++)
					{
						x = profil.XPoint[i];
						z = profil.ZPoint[i];

						profilList1.Add((float)x);
						profilList1.Add((float)y);
						profilList1.Add((float)z);
					}

					pathDestinyFiles = new StringBuilder(pathFiles);

					StringBuilder resultStr = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));
					StringBuilder dentStr = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));
					StringBuilder ecCircleStr = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));
					StringBuilder fittedLineStr = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));
					StringBuilder keyPointStr = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));

					processProfile(profilList1.ToArray().Length, (float)y, profilList1.ToArray(), minWidth, maxWidth, minDepth, maxDepth,
						calculatedWidth, minTransition, maxTransition, pathDestinyFiles, pathDestinyFiles.Length,
						isNewObject, minGroupPointSize, minGroupSizeOfValidPtsFitLine, convertedMinDistanceConstant,
						minDepthOfDeepestPointConstant, resultStr, dentStr, ecCircleStr, fittedLineStr, keyPointStr);

					//Add dent
					if (!string.IsNullOrWhiteSpace(dentStr.ToString()))
					{
						foreach (var item in dentStr.ToString().Split('\n'))
						{
							var dp = new DentPoint();
							dp.profilNumber = float.Parse(item.Split(' ')[1]);
							dp.dentPoint = new Point(float.Parse(item.Split(' ')[0]), float.Parse(item.Split(' ')[2]));
							lock (dentPointLst)
							{
								dentPointLst.Add(dp);
							}
						}
					}

					//add Circle
					if (!string.IsNullOrWhiteSpace(ecCircleStr.ToString()))
					{
						var ecc = new EcCircle();
						ecc.profilNumber = float.Parse(ecCircleStr.ToString().Split(' ')[0]);
						ecc.xc = float.Parse(ecCircleStr.ToString().Split(' ')[1]);
						ecc.radius = float.Parse(ecCircleStr.ToString().Split(' ')[2]);
						ecc.zc = float.Parse(ecCircleStr.ToString().Split(' ')[3]);

						lock (ecCircleLst)
						{
							ecCircleLst.Add(ecc);
						}
					}

					//add base plate
					if (!string.IsNullOrWhiteSpace(fittedLineStr.ToString()))
					{
						foreach (var item in fittedLineStr.ToString().Split('\n'))
						{
							var basePlate = new BasePlate();
							basePlate.profilNumber = float.Parse(item.Split(' ')[1]);
							basePlate.point = new Point(double.Parse(item.Split(' ')[0]), double.Parse(item.Split(' ')[2]));

							lock (basePlateLst)
							{
								basePlateLst.Add(basePlate);
							}
						}
					}

					//add keyPoints
					if (!string.IsNullOrWhiteSpace(keyPointStr.ToString()))
					{
						foreach (var item in keyPointStr.ToString().Split('\n'))
						{
							if (!string.IsNullOrEmpty(item))
							{
								var keyPoint = new KeyPoint();
								keyPoint.profilNumber = float.Parse(item.Split(' ')[1]);
								keyPoint.keyPoint = new Point(double.Parse(item.Split(' ')[0]), double.Parse(item.Split(' ')[2]));

								lock (keyPointLst)
								{
									keyPointLst.Add(keyPoint);
								}
							}
						}
					}

					profilList1.Clear();
					previous_y = y;
					isNewObject = 0;
					isNewObject = 0;
					processingProfiles = profil.orderRead;
					processingProfilesLst.Add(processingProfiles);

					if (!Dispatcher.CheckAccess())
					{
						Dispatcher.BeginInvoke(new Action(() =>
						{
							progressBar1.Value = processingProfiles * 100 / profiles.Count();
							processingLbl.Content = string.Format("{0} {1}%", Resources.MergedDictionaries.First()["mainProcessingInfoTxt"], processingProfiles * 100 / profiles.Count());
						}
						));
					}
				}

				if (!Dispatcher.CheckAccess())
				{
					Dispatcher.BeginInvoke(new Action(() =>
					{
						progressBar1.Value = 100;
						processingLbl.Content = $"{Resources.MergedDictionaries.First()["mainProcessingCompletInfoTxt"]}";
					}
					));
				}

				DateTime stopProcessingDataTime = DateTime.Now;
				TimeSpan totalTimeProcessingData = stopProcessingDataTime - startProcessDataTime;

				if (!Dispatcher.CheckAccess())
				{
					Dispatcher.BeginInvoke(new Action(() =>
					{
						processDataInfoTxt.Text = string.Format("{0} {1:hh\\:mm\\:ss}", Resources.MergedDictionaries.First()["mainTotalTimeProcessingTxt"], totalTimeProcessingData);
					}
					));
				}

				System.Windows.MessageBox.Show($"{Resources.MergedDictionaries.First()["mainMessageProcessFinished"]}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("Error in ProcessProfiles for profil Number {0}\n{1}", currentProfileNumber, ex.Message), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void ThTakeData()
		{
			try
			{
				//if the scanner was disconnected try to restart the sending of the data
				//Boolean bSendDataTransferEnableAlreadyDone = false;
				int iRetBuf = 128;
				StringBuilder strRetBuf = new StringBuilder(new String(' ', iRetBuf));

				DateTime timeGetInfoTemp = DateTime.Now;
				TimeSpan timeDiff = new TimeSpan();

				while (true)
				{
					//current state of the connection
					EthernetScanner_GetConnectStatus(ScannerHandle, iConnectionStatus);
					if (iConnectionStatus[0] == iETHERNETSCANNER_TCPSCANNERCONNECTED)
					{
						//EthernetScanner_GetXZIExtended: the Data are linearized (every call is a profil)
						m_iScannerDataLen = EthernetScanner_GetXZIExtended(
																			ScannerHandle,
																			m_doX,
																			m_doZ,
																			m_iIntensity,
																			m_iPeakWidth,
																			iETHERNETSCANNER_PEAKSPERCMOSSCANLINEMAX * iETHERNETSCANNER_SCANXMAX,
																			m_iEncoder,
																			m_bUSRIO,
																			timeBetweenProfils,
																			null,
																			0,
																			m_iPicCnt);
						m_iProfilesAvail = EthernetScanner_GetDllFiFoState(ScannerHandle);
						//if the scan data was received: do anything
						if (m_iScannerDataLen > 0)
						{
							//decrease or disable the view of the scans if the scanner frequency go high!
							timeDiff = DateTime.Now - timeGetInfoTemp;
							if (timeDiff.TotalMilliseconds > 100)
							{
								timeGetInfoTemp = DateTime.Now;
								UpdateProfiles();
							}
						}
					}
				}
			}
			catch (ThreadAbortException ex)
			{
				Console.WriteLine("Exception message in ThTakeData: {0}", ex.Message);
				Thread.ResetAbort();
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("Error in ThTakeData: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
				//throw ex;
			}
		}

		private void UpdateProfiles()
		{
			//bool again is defined to prevent lost profiles while this is blocked
			bool again = true;

			while (again)
			{
				if (!block)
				{
					block = true;
					again = false;
					//ea -> [0]EA1, [1]EA2, [2]EA3, [3]EA4
					string[] ea = { Convert.ToString(m_bUSRIO[0], 2).Substring(6, 1), Convert.ToString(m_bUSRIO[0], 2).Substring(5, 1), Convert.ToString(m_bUSRIO[0], 2).Substring(4, 1), Convert.ToString(m_bUSRIO[0], 2).Substring(3, 1) };
					//if EA3 on
					if (ea[2] == "1" || ea[2] == "0")
					{
						List<double> m_doZList = new List<double>();
						List<double> m_doXlist = new List<double>();
						List<int> intensityList = new List<int>();

						for (int i = 0; i < m_iScannerDataLen; i++)
						{
							if (m_doZ[i] != 0 && m_doX[i] != 0 && m_iIntensity[i] != 0)
							{
								m_doZList.Add(Math.Round(m_doZ[i], 4));
								m_doXlist.Add(Math.Round(m_doX[i], 4));
								intensityList.Add(m_iIntensity[i]);
							}
						}

						profiles.Add(new Profil(m_iEncoder[0], m_doXlist, m_doZList, intensityList, countAddedProfil, m_iScannerDataLen));
						CheckFinishScanByIntensity(m_iIntensity);
						countAddedProfil++;
					}

					block = false;
				}
			}
		}

		private void ThShowData()
		{

			while (true)
			{
				if (!Dispatcher.CheckAccess())
				{
					Dispatcher.BeginInvoke(
						new Action(() =>
						{
							UpdateChart();
							UpdatePanel();
						}
					));
				}
				//if the thread that take data from sensor had finished and all profiles are showed, break loop
				//to finish this thread
				if (!takeDataThread.IsAlive && countShowProfil >= profiles.Count())
					break;
			}

			try
			{
				showDataThread.Abort();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		private void UpdateChart()
		{
			try
			{
				if (!block)
				{
					block = true;

					//List<Profil> newList = new List<Profil>(profiles);
					var newList = profiles.ToArray();

					var profilToShow = newList.Where(x => x.orderRead == countShowProfil).FirstOrDefault();

					if (profilToShow != null)
					{
						List<Point3D> point3Ds = new List<Point3D>();

						for (int i = 0; i < profilToShow.XPoint.Count(); i++)
						{
							point3Ds.Add(new Point3D(profilToShow.XPoint[i], profilToShow.ZPoint[i], profilToShow.profilNumber));
						}

						ScatterPlotVisual3D sc = new ScatterPlotVisual3D();
						ModelVisual3D visualProfil = new ModelVisual3D();
						visualProfil.Content = sc.CreateModel(point3Ds.ToArray(), Materials.Blue, 0.06, 2, 2);
						ViewPort3D.Children.Add(visualProfil);
						countShowProfil++;

					}
					block = false;
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("UpdateChart Error: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void UpdatePanel()
		{
			if (m_bUSRIO[0] != 0)
			{
				//ea -> [0]EA1, [1]EA2, [2]EA3, [3]EA4
				string[] ea = { Convert.ToString(m_bUSRIO[0], 2).Substring(6, 1), Convert.ToString(m_bUSRIO[0], 2).Substring(5, 1), Convert.ToString(m_bUSRIO[0], 2).Substring(4, 1), Convert.ToString(m_bUSRIO[0], 2).Substring(3, 1) };
				//if EA3 on
				eaInfo1.Content = ea[0];
				eaInfo2.Content = ea[1];
				eaInfo3.Content = ea[2];
				eaInfo4.Content = ea[3];
			}
		}

		private void ThProcessData()
		{
			try
			{
				startProcessDataTime = DateTime.Now;
				timeSavedFile = DateTime.Now;
				string pathFiles = Path.Combine(destinyFile, string.Format("RobAH Insight {0}", timeSavedFile.ToString("dd-MM-yyyy HH-mm-ss")));
				float previousY = 0;

				if (!Directory.Exists(pathFiles))
				{
					Directory.CreateDirectory(pathFiles);
				}

				while (true)
				{
					try
					{
						var profilToProcess = profiles.Where(x => x.orderRead == countProcessData).FirstOrDefault();
						List<float> profilList = new List<float>();
						StringBuilder pathDestinyFiles;
						string keyPoints = string.Empty;
						string fittedLine = string.Empty;

						if (profilToProcess != null)
						{
							if (countProcessData == 0) previousY = profilToProcess.profilNumber;

							//polyRegresion Library
							#region
							// Let's retrieve the input and output data:
							double[] inputs = profilToProcess.XPoint.ToArray();  // X
							double[] outputs = profilToProcess.ZPoint.ToArray(); // Y

							// We can create a learning algorithm
							var ls = new PolynomialLeastSquares()
							{
								Degree = 30
							};

							// Now, we can use the algorithm to learn a polynomial
							PolynomialRegression poly = ls.Learn(inputs, outputs);
							polyRegList.Add(new Tuple<int, PolynomialRegression>((int)profilToProcess.profilNumber, poly));

							for (int i = 0; i < inputs.Length; i++)
							{

								double result = 0;// result -> f(x)
								int pow = poly.Weights.Length;

								for (int j = 0; j < poly.Weights.Length; j++)
								{
									result = result + (poly.Weights[j] * Math.Pow(inputs[i], pow));
									pow = pow - 1;
								}

								result = result + poly.Intercept;

								var distance = outputs[i] - result;

								if (distance > 0.1)
								{
									int index = profilToProcess.XPoint.IndexOf(inputs[i]);
									profilToProcess.XPoint.RemoveAt(index);
									profilToProcess.ZPoint.RemoveAt(index);
								}
								else if (distance < -0.3)
								{
									int index = profilToProcess.XPoint.IndexOf(inputs[i]);
									profilToProcess.XPoint.RemoveAt(index);
									profilToProcess.ZPoint.RemoveAt(index);
								}
							}
							#endregion

							for (int i = 0; i < profilToProcess.XPoint.Count(); i++)
							{
								profilList.Add((float)profilToProcess.XPoint[i]);
								profilList.Add(profilToProcess.profilNumber);
								profilList.Add((float)profilToProcess.ZPoint[i]);
							}

							pathDestinyFiles = new StringBuilder(pathFiles);

							var resultString = new StringBuilder(string.Empty);

							StringBuilder resultStr = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));
							StringBuilder dentStr = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));
							StringBuilder ecCircleStr = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));
							StringBuilder fittedLineStr = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));
							StringBuilder keyPointStr = new StringBuilder(new String(' ', iETHERNETSCANNER_GETINFOSIZEMAX));

							processProfile(profilList.ToArray().Length, (float)profilToProcess.profilNumber, profilList.ToArray(), minWidth, maxWidth, minDepth, maxDepth,
								calculatedWidth, minTransition, maxTransition, pathDestinyFiles, pathDestinyFiles.Length,
								isNewObject, minGroupPointSize, minGroupSizeOfValidPtsFitLine, convertedMinDistanceConstant,
								minDepthOfDeepestPointConstant, resultStr, dentStr, ecCircleStr, fittedLineStr, keyPointStr);

							profilList.Clear();
							isNewObject = 0;

							if (!Dispatcher.CheckAccess())
							{
								Dispatcher.BeginInvoke(new Action(() =>
								{
									progressBar1.Value = (countProcessData * 100) / profiles.Count();
									processingLbl.Content = string.Format("{0} {1}%", Resources.MergedDictionaries.First()["mainProcessingInfoTxt"], (processingProfiles * 100) / profiles.Count());
								}
								));
							}

							countProcessData++;
						}
						else if (profiles.Count == countProcessData)
						{
							break;
						}
					}
					catch (Exception ex)
					{
						System.Windows.MessageBox.Show(string.Format("Error in ThProcessData: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception message: {0}", ex.Message);
			}

			try
			{
				processDataThread.Abort();
			}
			catch (ThreadAbortException ex)
			{
				Console.WriteLine("Exception message for processDataThread.Abort(): {0}", ex.Message);
				Thread.ResetAbort();
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("Error in ThProcessData: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			try
			{
				showProcDataThread.Abort();
				showProcDataThread.Join();
			}
			catch (ThreadAbortException ex)
			{
				Console.WriteLine("Exception message: {0}", ex.Message);
				Thread.ResetAbort();
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("Error in ThProcessData: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.BeginInvoke(new Action(() =>
					{
						progressBar1.Value = 100;
						processingLbl.Content = $"{Resources.MergedDictionaries.First()["mainProcessingCompletInfoTxt"]}";
					}
				));
			}


			DateTime stopProcessingDataTime = DateTime.Now;
			TimeSpan totalTimeProcessingData = stopProcessingDataTime - startProcessDataTime;

			System.Windows.MessageBox.Show($"{Resources.MergedDictionaries.First()["mainMessageProcessFinished"]}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void ThShowProcessingData()
		{
			bool flag = true;
			/*do
            {
                if (!Dispatcher.CheckAccess()) // CheckAccess returns true if you're on the dispatcher thread
                {
                    if (((processingProfiles * 100) / profiles.Count()) > 97)
                    {

                        flag = false;
                    }
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        progressBar1.Value = ((processingProfiles * 100) / profiles.Count());
                        processingLbl.Content = string.Format("Processing ... {0}%", ((processingProfiles * 100) / profiles.Count()));
                    }
                    ));
                }
            } while (flag);*/
		}

		private void testFileBtn_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();

			if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				//string filePath = openFileDialog.FileName;
				selectedFileTxt.Text = openFileDialog.FileName;
				selectedFile = openFileDialog.FileName;
			}
		}

		private void savePointsBtn_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (profiles.Count() != 0)
				{
					if (!String.IsNullOrEmpty(destinyFileTxt.Text))
					{
						string strTemp = string.Empty;

						string pathFiles = Path.Combine(destinyFileTxt.Text, string.Format("RobAH Insight {0}", timeSavedFile.ToString("dd-MM-yyyy HH-mm-ss")));

						var filePath = Path.Combine(pathFiles, string.Format("Points_{0}.xyzi", timeSavedFile.ToString("dd-MM-yyyy HH-mm-ss")));

						if (!Directory.Exists(pathFiles)) Directory.CreateDirectory(pathFiles);

						using (StreamWriter sw = new StreamWriter(filePath))
						{
							foreach (var profil in profiles)
							{
								for (int k = 0; k < profil.XPoint.Count(); k++)
								{
									strTemp = String.Format("{0}   {1}   {2}   {3}",
															profil.XPoint[k].ToString().Replace(',', '.'), profil.profilNumber, profil.ZPoint[k].ToString().Replace(',', '.'), profil.Intensity[k].ToString());
									sw.WriteLine(strTemp);
								}
							}
						}

						//profiles.Clear();

						System.Windows.MessageBox.Show(string.Format("File was saved in '{0}'", destinyFileTxt.Text), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
					}
					else
					{
						System.Windows.MessageBox.Show("Please choose a destination path", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
					}
				}
				else
				{
					System.Windows.MessageBox.Show("Cannot save file. Process not yet started", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			catch (Exception ex)
			{
				System.Windows.MessageBox.Show(string.Format("savePointsBtn_Click Error: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			parameterDetails.Close();

			if (showDataThread != null)
			{
				showDataThread.Abort();
				showDataThread.Join();
			}

			if (processDataThread != null)
			{
				processDataThread.Abort();
			}

			if (takeDataThread != null)
			{
				takeDataThread.Abort();
				takeDataThread.Join();

			}

			if (profiles.Count() > 0)
			{
				if (System.Windows.MessageBox.Show("Do you want to close the app without having stored the points?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					e.Cancel = true;
				}
			}
		}

		private void resetEncoderBtn_Click(object sender, EventArgs e)
		{
			IntPtr ScannerHandleReset = (IntPtr)null;

			String strIPAddressReset = textBoxIPAddress.Text;
			string strPortReset = "32001";
			int iTimeOutReset = Int32.Parse(string.Format("1000"));
			int[] iConnectionStatusReset = new int[1];

			if (ScannerHandleReset != (IntPtr)null)
			{
				System.Windows.MessageBox.Show(string.Format("Connection Error"), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			//start the connection to the Scanner
			ScannerHandleReset = EthernetScanner_Connect(strIPAddressReset, strPortReset, iTimeOutReset);

			//check the connection state with timeout 3000 ms
			DateTime startConnectTime = DateTime.Now;
			TimeSpan connectTime = new TimeSpan();
			do
			{
				if (connectTime.TotalMilliseconds > 1500)
				{
					ScannerHandleReset = EthernetScanner_Disconnect(ScannerHandleReset);
					System.Windows.MessageBox.Show("Error: No Connection!!!", "ERROR", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return;
				}
				Thread.Sleep(10);
				EthernetScanner_GetConnectStatus(ScannerHandleReset, iConnectionStatusReset);
				connectTime = DateTime.Now - startConnectTime;
			} while (iConnectionStatusReset[0] != iETHERNETSCANNER_TCPSCANNERCONNECTED);

			byte[] buffer = Encoding.ASCII.GetBytes("SetResetEncoder");
			int iRes = EthernetScanner_WriteData(ScannerHandleReset, buffer, buffer.Length);

			ScannerHandleReset = EthernetScanner_Disconnect(ScannerHandleReset);

			System.Windows.MessageBox.Show(string.Format("Encoder reset"), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (profiles.Count() != 0)
			{
				//evaluation = new Evaluation(pathFiles, profiles);
				//evaluation.Show();
			}
			else
			{
				System.Windows.Forms.MessageBox.Show("Cannot show results. Process not yet started", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void destinyFileBtn_Click_1(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();

			if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				destinyFileTxt.Text = Path.GetFullPath(dlg.SelectedPath);
				destinyFile = Path.GetFullPath(dlg.SelectedPath);
			}
		}

		private void settingsBtn_Click(object sender, RoutedEventArgs e)
		{
			parameterDetails.Show();
		}

		private async void showEvaluationBtn_Click(object sender, RoutedEventArgs e)
		{
			if (profiles.Count() != 0)
			{
				evaluation = new Evaluation(resourceDictionary);

				await evaluation.CreateEvaluation(pathFiles, profiles, distanceSearchAreaNeighborhood, numberOfNeighborhood,
					distanceToCenterOfCircle, maxWidthD5D0, minWidthD5D0, groupCircleDistance, skipStartEnd, polyRegList);

				evaluation.Show();

			}
			else
			{
				System.Windows.MessageBox.Show("Cannot show results. Process not yet started", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{

			if (parameterDetails != null)
				parameterDetails.Close();

			if (evaluation != null)
				evaluation.Close();

			if (showDataThread != null)
			{
				showDataThread.Abort();
				showDataThread.Join();
			}

			if (processDataThread != null)
				processDataThread.Abort();

			if (takeDataThread != null)
			{
				takeDataThread.Abort();
				takeDataThread.Join();
			}

			if (profiles.Count() > 0)
			{
				if (System.Windows.MessageBox.Show("Do you want to close the app without having stored the points?", "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.No)
					e.Cancel = true;
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Environment.Exit(0);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			ViewPortDefaultConfig();
		}

		private void SwitchLanguage(string lCode)
		{
			switch (lCode)
			{
				case "en-EN":
					resourceDictionary.Source = new Uri("..\\Resources\\StringResources.en.xaml", UriKind.Relative);
					break;
				case "de-DE":
					resourceDictionary.Source = new Uri("..\\Resources\\StringResources.de.xaml", UriKind.Relative);
					break;
				case "es-ES":
					resourceDictionary.Source = new Uri("..\\Resources\\StringResources.es.xaml", UriKind.Relative);
					break;
				default:
					resourceDictionary.Source = new Uri("..\\Resources\\StringResources.en.xaml", UriKind.Relative);
					lCode = "en-EN";
					break;
			}

			Thread.CurrentThread.CurrentCulture = new CultureInfo(lCode);
			Thread.CurrentThread.CurrentUICulture = new CultureInfo(lCode);
			CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(lCode);

			this.Resources.MergedDictionaries.Add(resourceDictionary);
		}

		private void enMenuItem_Click(object sender, RoutedEventArgs e)
		{
			SwitchLanguage("en-EN");
		}

		private void deMenuItem_Click(object sender, RoutedEventArgs e)
		{
			SwitchLanguage("de-DE");
		}

		private void esMenuItem_Click(object sender, RoutedEventArgs e)
		{
			SwitchLanguage("es-ES");
		}
	}

	public class ChangeColorRow : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string input = (string)value;
			switch (input)
			{
				case "Good Quality welding":
					return Brushes.GreenYellow;
				case "Middle Quality welding":
					return Brushes.Yellow;
				case "Bad Quality welding":
					return Brushes.OrangeRed;
				default:
					return DependencyProperty.UnsetValue;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
