using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Modeling3DHelixToolKit.Classes
{
	public class ShapeSearch
	{

		public ShapeSearch()
		{

		}
		public void SearchShape(List<Point> points)
		{
			var orderedList = points.OrderByDescending(x => x.X).ToList();
		}
	}
}
