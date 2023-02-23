using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Modeling3DHelixToolKit.Classes
{
	public class DataSource
	{
		public string pathFiles;
		public string PathFiles { get => pathFiles; set => pathFiles = value; }

		public DataSource(string pathFiles)
		{
			PathFiles = pathFiles;
		}

		public DataTable GenerateDataTable(string fileName, bool firstRowContainsFieldNames = true)
		{
			DataTable result = new DataTable();

			try
			{
				string delimiters = ";";
				string extension = System.IO.Path.GetExtension(System.IO.Path.Combine(PathFiles, fileName));

				switch (extension.ToLower())
				{
					case "txt":
						delimiters = "\t";
						break;
					case "csv":
						delimiters = ";";
						break;
					case ".csv":
						delimiters = ";";
						break;
					case ".xyz":
						delimiters = " ";
						break;
					case ".poly":
						delimiters = " ";
						break;
					default:
						break;
				}

				using (TextFieldParser tfp = new TextFieldParser(System.IO.Path.Combine(PathFiles, fileName)))
				{
					tfp.SetDelimiters(delimiters);

					// Get The Column Names
					if (!tfp.EndOfData)
					{
						string[] fields = tfp.ReadFields();

						for (int i = 0; i < fields.Count(); i++)
						{
							if (firstRowContainsFieldNames)
								result.Columns.Add(fields[i]);
							else
								result.Columns.Add("Col" + i);
						}

						// If first line is data then add it
						if (!firstRowContainsFieldNames)
							result.Rows.Add(fields);
					}

					while (!tfp.EndOfData)
						result.Rows.Add(tfp.ReadFields());
				}

				return result;
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format("Error in GenerateDataTable: {0}", ex.Message), "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
				return new DataTable();
			}

		}
	}
}
