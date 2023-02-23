using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeling3DHelixToolKit.Models
{
	class ProfilResult
	{
        public int GoodPerfil { get; set; }

        public int BadPerfil { get; set; }

        public int TotalPerfil { get; set; }

        public string EvaluationResult { get; set; }

        public ProfilResult(int GoodPerfil, int BadPerfil, int TotalPerfil, string EvaluationResult)
        {
            this.GoodPerfil = GoodPerfil;
            this.BadPerfil = BadPerfil;
            this.TotalPerfil = TotalPerfil;
            this.EvaluationResult = EvaluationResult;
        }
    }
}
