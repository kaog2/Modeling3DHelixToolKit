using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeling3DHelixToolKit.Models
{
    public class Profil
    {
        public float profilNumber { get; set; }
        public int orderRead { get; set; }
        public int scannedData { get; set; }
        private List<double> xPoint = new List<double>();
        private List<double> zPoint = new List<double>();
        private List<int> intensity = new List<int>();
        public List<double> XPoint { get => xPoint; set => xPoint = value; }
        public List<double> ZPoint { get => zPoint; set => zPoint = value; }
        public List<int> Intensity { get => intensity; set => intensity = value; }

        public Profil(float profilNumber, List<double> xPoint, List<double> zPoint, List<int> intensity, int orderRead, int scannedData)
        {
            this.profilNumber = profilNumber;
            this.xPoint = xPoint;
            this.zPoint = zPoint;
            this.intensity = intensity;
            this.orderRead = orderRead;
            this.scannedData = scannedData;
        }

        public Profil(Profil profil)
		{
            this.profilNumber = profil.profilNumber;
            this.xPoint = profil.xPoint;
            this.zPoint = profil.zPoint;
            this.intensity = profil.intensity;
            this.orderRead = profil.orderRead;
            this.scannedData = profil.scannedData;
        }
    }
}
