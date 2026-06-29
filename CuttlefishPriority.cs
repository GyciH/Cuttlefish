using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuttlefish
{
    public class CuttlefishPriority : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.ComponentServer.AddCategoryIcon("Cuttlefish", Cuttlefish.Properties.Resources.Icone);
            Grasshopper.Instances.ComponentServer.AddCategorySymbolName("Cuttlefish", 'C');
            return GH_LoadingInstruction.Proceed;
        }
    }
}
