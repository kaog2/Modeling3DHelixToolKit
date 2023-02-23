using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Modeling3DHelixToolKit.Models
{
    public class ProfilAnalisysViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ProfilAnalisys> profilAnalisys { get; set; }
        private DataTable infoResultTable;
        public string pathFiles;
        public string PathFiles { get => pathFiles; set => pathFiles = value; }

        public ProfilAnalisysViewModel(string pathFiles)
        {
            if (!string.IsNullOrEmpty(pathFiles))
            {
                PathFiles = pathFiles;
                FillList();
            }
            
        }

        public void FillList()
        {
            try
            {
                infoResultTable = GenerateDataTable("result.csv");

                if (profilAnalisys == null)
                    profilAnalisys = new ObservableCollection<ProfilAnalisys>();

                foreach (DataRow dr in infoResultTable.Rows)
                {
                    profilAnalisys.Add(new ProfilAnalisys
                    {
                        //id = Convert.ToInt32(dr[0].ToString()),
                        //country = dr[1].ToString(),
                        //ondate = Convert.ToDateTime(dr[2].ToString())
                    });
                }
            }
            catch (Exception ex)
            {

            }
            
        }

        private DataTable GenerateDataTable(string fileName, bool firstRowContainsFieldNames = true)
        {
            DataTable result = new DataTable();

            try
            {
                string delimiters = ";";
                string extension = System.IO.Path.GetExtension(System.IO.Path.Combine(PathFiles, fileName));
                int readType = 0;

                switch (extension.ToLower())
                {
                    case "txt":
                        delimiters = "\t";
                        break;
                    case "csv":
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
                MessageBox.Show(ex.Message, "ERROR", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return new DataTable();
            }

        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}

