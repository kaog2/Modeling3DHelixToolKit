using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modeling3DHelixToolKit.Models
{
    public class ProcessEscapeType
    {
        public int ProcessEscapeTypeId { get; set; }
        public int ProcessEscapeTypeOrder { get; set; }
        public string ProcessEscapeTypeName { get; set; }

        public ProcessEscapeType()
		{

		}

        public ProcessEscapeType(int ProcessEscapeTypeId, int ProcessEscapeTypeOrder, string ProcessEscapeTypeName)
        {
            this.ProcessEscapeTypeId = ProcessEscapeTypeId;
            this.ProcessEscapeTypeOrder = ProcessEscapeTypeOrder;
            this.ProcessEscapeTypeName = ProcessEscapeTypeName;
        }

        public List<ProcessEscapeType> GetProcessEscapeTypes()
		{
            List<ProcessEscapeType> processEscapeTypes = new List<ProcessEscapeType>();

            processEscapeTypes.Add(new ProcessEscapeType(0, 0, string.Format("not a T joint or noisy profile")));
            processEscapeTypes.Add(new ProcessEscapeType(1, 0, string.Format("The weld is bad")));
            //ProcessEscapeTypes.Add(new ProcessEscapeType( 1, 0, string.Format("More or less found Key Points") ) );
            //ProcessEscapeTypes.Add(new ProcessEscapeType( 2, 0, string.Format("The weld is bad, differences of K1 and K2 is small") ) );
            //ProcessEscapeTypes.Add(new ProcessEscapeType( 3, 0, string.Format("The weld is bad, differences of K1 and K2 is small") ) );
            processEscapeTypes.Add(new ProcessEscapeType(2, 0, string.Format("no line was fitted")));
            processEscapeTypes.Add(new ProcessEscapeType(3, 0, string.Format("there are not enough points below the baseplate")));
            processEscapeTypes.Add(new ProcessEscapeType(4, 0, string.Format("there are not enough points below the baseplate")));
            processEscapeTypes.Add(new ProcessEscapeType(5, 0, string.Format("m_dent.fittedCircle.xc != m_dent.fittedCircle.xc /*nan test*/")));
            processEscapeTypes.Add(new ProcessEscapeType(6, 0, string.Format("circle found but very small, there are not enough points below the baseplate")));

            return processEscapeTypes;
        }
    }
}
