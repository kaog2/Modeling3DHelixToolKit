using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Modeling3DHelixToolKit.ViewModel;

namespace Modeling3DHelixToolKit
{
    /// <summary>
    /// Interaction logic for ParameterDetails.xaml
    /// </summary>
    public partial class ParameterDetails : Window
    {
        private MainWindow wenglorUserInterface = null;
        private bool enableEntryCalculatedParameter = false; //this is for the function S = Width

        public ParameterDetails(MainWindow form, ResourceDictionary resourceDictionary)
        {
            wenglorUserInterface = form;
            InitializeComponent();

            this.DataContext = new ParameterViewModel(wenglorUserInterface.minWidth.ToString("0.000", CultureInfo.InvariantCulture), 
                                                        wenglorUserInterface.maxWidth.ToString("0.000", CultureInfo.InvariantCulture),
                                                        wenglorUserInterface.minDepth.ToString("0.000", CultureInfo.InvariantCulture), 
                                                        wenglorUserInterface.maxDepth.ToString("0.000", CultureInfo.InvariantCulture), 
                                                        wenglorUserInterface.minTransition.ToString("0.000", CultureInfo.InvariantCulture),
                                                        wenglorUserInterface.maxTransition.ToString("0.000", CultureInfo.InvariantCulture),
                                                        wenglorUserInterface.minGroupPointSize.ToString(),
                                                        wenglorUserInterface.minGroupSizeOfValidPtsFitLine.ToString(),
                                                        wenglorUserInterface.convertedMinDistanceConstant.ToString("0.000", CultureInfo.InvariantCulture),
                                                        wenglorUserInterface.minDepthOfDeepestPointConstant.ToString("0.000", CultureInfo.InvariantCulture),
                                                        wenglorUserInterface.distanceSearchAreaNeighborhood.ToString("0.000", CultureInfo.InvariantCulture),
                                                        wenglorUserInterface.numberOfNeighborhood.ToString(),
                                                        wenglorUserInterface.distanceToCenterOfCircle.ToString("0.000", CultureInfo.InvariantCulture),
                                                        wenglorUserInterface.maxWidthD5D0.ToString("0.000", CultureInfo.InvariantCulture),
                                                        wenglorUserInterface.minWidthD5D0.ToString("0.000", CultureInfo.InvariantCulture),
                                                        wenglorUserInterface.groupCircleDistance.ToString(),
                                                        wenglorUserInterface.skipStartEnd
                                                    );

            this.Resources.MergedDictionaries.Add(resourceDictionary);
        }

        private void applyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (RulesTextBoxes())
            {
                wenglorUserInterface.minDepth = float.Parse(depthMinTxt.Text);
                wenglorUserInterface.maxDepth = float.Parse(depthMaxTxt.Text);
                wenglorUserInterface.minWidth = float.Parse(widthMinTxt.Text);
                wenglorUserInterface.maxWidth = float.Parse(widthMaxTxt.Text);
                wenglorUserInterface.minTransition = float.Parse(transitionMinTxt.Text);
                wenglorUserInterface.maxTransition = float.Parse(transitionMaxTxt.Text);
                wenglorUserInterface.minGroupPointSize = int.Parse(minGroupPointSizeTxt.Text);
                wenglorUserInterface.minGroupSizeOfValidPtsFitLine = int.Parse(minGroupSizeOfValidPtsFitLineTxt.Text);
                wenglorUserInterface.convertedMinDistanceConstant = float.Parse(convertedMinDistanceConstantTxt.Text);
                wenglorUserInterface.minDepthOfDeepestPointConstant = float.Parse(minDepthOfDeepestPointConstantTxt.Text);
                wenglorUserInterface.distanceSearchAreaNeighborhood = float.Parse(distanceSearchAreaNeighborhoodTxt.Text);
                wenglorUserInterface.numberOfNeighborhood = int.Parse(numberOfNeighborhoodTxt.Text);
                wenglorUserInterface.distanceToCenterOfCircle = float.Parse(distanceToCenterOfCircleTxt.Text);
                wenglorUserInterface.minWidthD5D0 = float.Parse(minWidthD5D0Txt.Text);
                wenglorUserInterface.maxWidthD5D0 = float.Parse(maxD5D0TxtTxt.Text);
                wenglorUserInterface.groupCircleDistance = int.Parse(groupCircleDistanceTxt.Text);
                wenglorUserInterface.skipStartEnd = skipStartEndChk.IsChecked.Value;

                if (enableEntryCalculatedParameter)
                {
                    wenglorUserInterface.calculatedWidthH = float.Parse(parameterHTxt.Text);
                    wenglorUserInterface.calculatedWidthR = float.Parse(parameterRTxt.Text);
                    wenglorUserInterface.enableEntryCalculatedParameter = true;
                }

                Hide();
            }
        }

        private bool RulesTextBoxes()
        {
            if ((float.Parse(depthMinTxt.Text) >= float.Parse(depthMaxTxt.Text))
                || (float.Parse(widthMinTxt.Text, CultureInfo.InvariantCulture) >= float.Parse(widthMaxTxt.Text))
                || (float.Parse(transitionMinTxt.Text) >= float.Parse(transitionMaxTxt.Text))
                )
            {
                MessageBox.Show(string.Format("the min. value cannot be less than the max. value"), "INFORMATION", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }

            return true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //the window form should be active all the time but if neccessary unvisible
           e.Cancel = true;
           Hide();
            
        }

        private void calculatedWidthChb_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;

            bool? isChecked = checkBox.IsChecked;

            if (isChecked == true)
            {
                parameterHTxt.IsEnabled = true;
                parameterRTxt.IsEnabled = true;
                enableEntryCalculatedParameter = true;
            }
        }

		private void calculatedWidthChb_Unchecked(object sender, RoutedEventArgs e)
		{
            var checkBox = sender as CheckBox;

            bool? isChecked = checkBox.IsChecked;

            if (isChecked == false)
            {
                parameterHTxt.IsEnabled = false;
                parameterRTxt.IsEnabled = false;
                enableEntryCalculatedParameter = false;
            }
        }
	}
}
