using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Modeling3DHelixToolKit.ViewModel
{
	public class MainWindowViewModel : ObservableObject
	{
        private string _ipAddress;
        public string IpAddress
        {
            get { return _ipAddress; }
            set
            {
                if (!IsIpCharacter(value))
                    return;

                OnPropertyChanged(ref _ipAddress, value);
            }
        }

        public MainWindowViewModel(string ip)
		{
            IpAddress = ip;
		}

        private bool IsIpCharacter(string text)
		{
            string pattern = @"^([0-9]+$)|(\.[0-9]+$)|([0-9]+\.$)";
            Regex rgx = new Regex(pattern);
            
            if (rgx.IsMatch(text))
            {
                return true;
            }

            return false;
        }
    }
}
