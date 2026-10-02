using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuttlefish
{
    public class Spiral : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Spiral class.
        /// </summary>
        public Spiral()
          : base("Spiral", "SPIR",
              "Draw a spiral through the rings of a circular grid",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The points of a CircularGrid: {circle; ring}, the center in ring 0.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Spirals", "S", "One spiral per input circle.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> points;
            if (!DA.GetDataTree(0, out points)) return;

            List<Curve> spirals = new List<Curve>();

            foreach (var rings in Outils.Groupes(points))
            {
                if (rings.Count < 2 || rings[0].Count == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A grid needs a center and at least one ring.");
                    continue;
                }

                Point3d center = rings[0][0];
                List<Point3d> path = new List<Point3d> { center };
                double previous = 0;

                // Sur l'anneau k, le j-ième point garde sa direction depuis le centre, mais son rayon
                // passe progressivement du rayon de l'anneau précédent à celui de l'anneau k.
                for (int k = 1; k < rings.Count; k++)
                {
                    List<Point3d> ring = rings[k];
                    if (ring.Count == 0) continue;
                    double radius = ring.Average(p => p.DistanceTo(center));
                    for (int j = 0; j < ring.Count; j++)
                    {
                        Vector3d dir = ring[j] - center;
                        if (!dir.Unitize()) continue;
                        double r = previous + (radius - previous) * j / ring.Count;
                        if (r > 0) path.Add(center + dir * r);
                    }
                    previous = radius;
                }
                // On referme le dernier tour sur le premier point du dernier anneau.
                List<Point3d> last = rings[rings.Count - 1];
                if (last.Count > 0) path.Add(last[0]);

                if (path.Count >= 3)
                    spirals.Add(Curve.CreateInterpolatedCurve(path, 3));
            }

            DA.SetDataList(0, spirals);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.Spiral;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("15DADA4C-B58B-489B-8C9E-EF556A9DBC32"); }
        }
    }
}
