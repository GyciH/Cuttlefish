using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Cuttlefish
{
    public class Flow : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Flow class.
        /// </summary>
        public Flow()
          : base("Flow", "FLOW",
              "Draw streamlines following guide curves",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Seeds", "P", "The start points of the streamlines.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Guides", "G", "The curves the flow follows.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Step", "St", "The length of one step.", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Count", "N", "The number of steps of each streamline.", GH_ParamAccess.item, 50);
            pManager.AddRectangleParameter("Rectangle", "R", "Optional boundary: streamlines stop when they leave it.", GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "The streamlines.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> seeds = new List<Point3d>();
            List<Curve> guides = new List<Curve>();
            double step = 1;
            int count = 50;
            Rectangle3d rect = Rectangle3d.Unset;

            if (!DA.GetDataList(0, seeds)) return;
            if (!DA.GetDataList(1, guides)) return;
            DA.GetData(2, ref step);
            DA.GetData(3, ref count);
            bool bounded = DA.GetData(4, ref rect) && rect.IsValid;

            if (step <= 0 || count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Step must be greater than 0 and Count at least 1.");
                return;
            }
            guides.RemoveAll(g => g == null);
            if (guides.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "At least one guide curve is needed.");
                return;
            }

            // Direction du champ : moyenne des tangentes des guides au point le plus proche,
            // pondérée par l'inverse du carré de la distance.
            Func<Point3d, Vector3d> field = p =>
            {
                Vector3d sum = Vector3d.Zero;
                foreach (Curve g in guides)
                {
                    if (!g.ClosestPoint(p, out double t)) continue;
                    double d = p.DistanceTo(g.PointAt(t));
                    Vector3d tan = g.TangentAt(t);
                    if (d < 1e-9) return tan;
                    sum += tan / (d * d);
                }
                return sum.Unitize() ? sum : Vector3d.Zero;
            };

            List<Curve> curves = new List<Curve>();
            foreach (Point3d seed in seeds)
            {
                List<Point3d> line = new List<Point3d> { seed };
                Point3d p = seed;
                for (int i = 0; i < count; i++)
                {
                    // Pas du point milieu (Runge-Kutta d'ordre 2).
                    Vector3d v1 = field(p);
                    if (v1.IsZero) break;
                    Vector3d v2 = field(p + v1 * (step / 2));
                    if (v2.IsZero) break;
                    p += v2 * step;
                    if (bounded && rect.Contains(p) != PointContainment.Inside) break;
                    line.Add(p);
                }
                if (line.Count >= 2)
                    curves.Add(new PolylineCurve(line));
            }

            DA.SetDataList(0, curves);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.Flow;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("28B800B3-86CA-4B55-8F38-4E04D394D415"); }
        }
    }
}
