
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeling3DHelixToolKit.Models
{
    public class ProfilAnalisys
    {
        public int Perfil { get; set; }

        public string Width { get; set; }

        public string WidthRatio { get; set; }

        public string Depth { get; set; }

        public string DepthScale { get; set; }

        public string TransitionPointDistance { get; set; }

        public string TransitionPointScale { get; set; }

        public string ProfileGap { get; set; }

        public string WidthOk { get; set; }

        public string DepthOk { get; set; }

        public string TransitionPointOk { get; set; }

        public string HadCircle{ get; set; }

        public string ProcessEscapeTypeName { get; set; }
        
        public string ProcessEscapeTypeId { get; set; }

        public string WidthD0D5 { get; set; }

        public string WidthD0D5Ok { get; set; }

        public string WidthRelative { get; set; }

        public string PositionMM { get; set; }

        public ProfilAnalisys()
        {
            
        }
    }
}
