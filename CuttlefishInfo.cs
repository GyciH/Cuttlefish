using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace Cuttlefish
{
    public class CuttlefishInfo : GH_AssemblyInfo
    {
        public override string Name => "Cuttlefish";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Cuttlefish.Properties.Resources.Icone;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("4f000575-ea0d-45f7-bb20-2ca3c21a5379");

        //Return a string identifying you or your company.
        public override string AuthorName => "Carolus Jérémy";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "www.cj-developpement.fr";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}