using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Cuttlefish
{
    public class SurfaceRectangle : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SurfaceRectangle class.
        /// </summary>
        public SurfaceRectangle()
          : base("SurfaceRectangle", "SRECT",
              "Create a flat rectangle with the mean size of a surface, to draw a pattern before MapToSurface",
              "Cuttlefish", "Points")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "The surface the pattern will be mapped on.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddRectangleParameter("Rectangle", "R", "A rectangle in the XY plane: its width follows U and its height follows V.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Length U", "LU", "The mean length of the surface along U.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Length V", "LV", "The mean length of the surface along V.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Surface surface = null;
            if (!DA.GetData(0, ref surface) || surface == null) return;

            double longueurU = Moyenne(surface, 0);
            double longueurV = Moyenne(surface, 1);
            if (longueurU <= 0 || longueurV <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The surface has no length along U or V.");
                return;
            }

            DA.SetData(0, new Rectangle3d(Plane.WorldXY, longueurU, longueurV));
            DA.SetData(1, longueurU);
            DA.SetData(2, longueurV);
        }

        /// <summary>
        /// Longueur moyenne de 9 isocourbes : direction 0 = U varie, 1 = V varie.
        /// </summary>
        private static double Moyenne(Surface surface, int direction)
        {
            Interval autre = surface.Domain(1 - direction);
            double somme = 0;
            for (int k = 0; k < 9; k++)
            {
                Curve iso = surface.IsoCurve(direction, autre.ParameterAt(k / 8.0));
                if (iso != null) somme += iso.GetLength();
            }
            return somme / 9;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.SurfaceRectangle;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("81CF04EB-61EB-44D8-A03E-62C3E2830146"); }
        }
    }
}
