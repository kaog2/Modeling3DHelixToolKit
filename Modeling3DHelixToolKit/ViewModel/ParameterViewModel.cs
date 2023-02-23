using System.Text.RegularExpressions;

namespace Modeling3DHelixToolKit.ViewModel
{
	public class ParameterViewModel : ObservableObject
	{
		private string minWidth;
		private string maxWidth;
		private string minDepth;
		private string maxDepth;
		private string minTransition;
		private string maxTransition;
		private string minGroupPointSize;
		private string minGroupSizeOfValidPtsFitLine;
		private string convertedMinDistanceConstant;
		private string minDepthOfDeepestPointConstant;
		private string distanceSearchAreaNeighborhood;
		private string numberOfNeighborhood;
		private string distanceToCenterOfCircle;
		private string minWidthD5D0;
		private string maxWidthD5D0;
		private string groupCircleDistance;
		public bool skipStartEnd;

		public string MinWidth
		{
			get { return minWidth; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref minWidth, value);
			}
		}

		public string MaxWidth
		{
			get { return maxWidth; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref maxWidth, value);
			}
		}

		public string MinWidthD5D0
		{
			get { return minWidthD5D0; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref minWidthD5D0, value);
			}
		}

		public string MaxWidthD5D0
		{
			get { return maxWidthD5D0; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref maxWidthD5D0, value);
			}
		}

		public string MinDepth
		{
			get { return minDepth; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref minDepth, value);
			}
		}

		public string MaxDepth
		{
			get { return maxDepth; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref maxDepth, value);
			}
		}

		public string MinTransition
		{
			get { return minTransition; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref minTransition, value);
			}
		}

		public string MaxTransition
		{
			get { return maxTransition; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref maxTransition, value);
			}
		}

		public string MinGroupPointSize
		{
			get { return minGroupPointSize; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref minGroupPointSize, value);
			}
		}

		public string MinGroupSizeOfValidPtsFitLine
		{
			get { return minGroupSizeOfValidPtsFitLine; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref minGroupSizeOfValidPtsFitLine, value);
			}
		}

		public string ConvertedMinDistanceConstant
		{
			get { return convertedMinDistanceConstant; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref convertedMinDistanceConstant, value);
			}
		}

		public string MinDepthOfDeepestPointConstant
		{
			get { return minDepthOfDeepestPointConstant; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref minDepthOfDeepestPointConstant, value);
			}
		}

		public string DistanceSearchAreaNeighborhood
		{
			get { return distanceSearchAreaNeighborhood; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref distanceSearchAreaNeighborhood, value);
			}
		}

		public string NumberOfNeighborhood
		{
			get { return numberOfNeighborhood; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref numberOfNeighborhood, value);
			}
		}

		public string DistanceToCenterOfCircle
		{
			get { return distanceToCenterOfCircle; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref distanceToCenterOfCircle, value);
			}
		}
		
		public string GroupCircleDistance
		{
			get { return groupCircleDistance; }
			set
			{
				if (!AllowOnlyNumberAndPoint(value))
					return;

				OnPropertyChanged(ref groupCircleDistance, value);
			}
		}

		public bool SkipStartEnd
		{
			get { return skipStartEnd; }
			set
			{
				OnPropertyChanged(ref skipStartEnd, value);
			}
		}


		public ParameterViewModel(string minWidth, string maxWidth, string minDepth, string maxDepth, string minTransition, string maxTransition,
									string minGroupPointSize, string minGroupSizeOfValidPtsFitLine, string convertedMinDistanceConstant, string minDepthOfDeepestPointConstant,
									string distanceSearchAreaNeighborhood, string numberOfNeighborhood, string distanceToCenterOfCircle, string maxWidthD5D0,
									string minWidthD5D0, string groupCircleDistance, bool skipStartEnd)
		{
			MinWidth = minWidth;
			MaxWidth = maxWidth;
			MinDepth = minDepth;
			MaxDepth = maxDepth;
			MaxDepth = maxDepth;
			MinTransition = minTransition;
			MaxTransition = maxTransition;
			MinGroupPointSize = minGroupPointSize;
			MinGroupSizeOfValidPtsFitLine = minGroupSizeOfValidPtsFitLine;
			ConvertedMinDistanceConstant = convertedMinDistanceConstant;
			MinDepthOfDeepestPointConstant = minDepthOfDeepestPointConstant;
			DistanceSearchAreaNeighborhood = distanceSearchAreaNeighborhood;
			NumberOfNeighborhood = numberOfNeighborhood;
			DistanceToCenterOfCircle = distanceToCenterOfCircle;
			MaxWidthD5D0 = maxWidthD5D0;
			MinWidthD5D0 = minWidthD5D0;
			GroupCircleDistance = groupCircleDistance;
			SkipStartEnd = skipStartEnd;
		}

		private bool AllowOnlyNumberAndPoint(string number)
		{

			string pattern = @"^([0-9]+$)|(\.[0-9]+$)";
			Regex rgx = new Regex(pattern);

			if (rgx.IsMatch(number) || string.IsNullOrWhiteSpace(number))
			{
				return true;
			}

			return false;
		}
	}
}
