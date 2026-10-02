using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuttlefish
{
    public class Weave : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Weave class.
        /// </summary>
        public Weave()
          : base("Weave", "WEAVE",
              "Weave two sets of curves over and under each other",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves A", "A", "The first set of strands.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curves B", "B", "The second set of strands, crossing the first one.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Gap", "G", "The length cut from the strand passing under.", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Strands", "S", "The cut strands: {0; i} for A, {1; j} for B.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> a = new List<Curve>();
            List<Curve> b = new List<Curve>();
            double gap = 1;

            if (!DA.GetDataList(0, a)) return;
            if (!DA.GetDataList(1, b)) return;
            DA.GetData(2, ref gap);

            if (gap <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Gap must be greater than 0.");
                return;
            }

            double tolerance = DocumentTolerance();
            // Longueurs (le long de chaque brin) où le brin passe dessous.
            var underA = a.Select(_ => new List<double>()).ToList();
            var underB = b.Select(_ => new List<double>()).ToList();

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] == null) continue;
                for (int j = 0; j < b.Count; j++)
                {
                    if (b[j] == null) continue;
                    var events = Intersection.CurveCurve(a[i], b[j], tolerance, tolerance);
                    if (events == null) continue;
                    foreach (var e in events.Where(e => e.IsPoint))
                    {
                        // Damier : A passe dessus quand i + j est pair, dessous sinon.
                        if ((i + j) % 2 == 0)
                            underB[j].Add(b[j].GetLength(new Interval(b[j].Domain.Min, e.ParameterB)));
                        else
                            underA[i].Add(a[i].GetLength(new Interval(a[i].Domain.Min, e.ParameterA)));
                    }
                }
            }

            DataTree<Curve> result = new DataTree<Curve>();
            for (int i = 0; i < a.Count; i++)
                if (a[i] != null) result.AddRange(Cut(a[i], underA[i], gap), new GH_Path(0, i));
            for (int j = 0; j < b.Count; j++)
                if (b[j] != null) result.AddRange(Cut(b[j], underB[j], gap), new GH_Path(1, j));

            DA.SetDataTree(0, result);
        }

        /// <summary>
        /// Retire du brin un morceau de longueur gap centré sur chaque croisement où il passe dessous.
        /// </summary>
        static List<Curve> Cut(Curve strand, List<double> crossings, double gap)
        {
            double total = strand.GetLength();
            var keep = new List<Curve>();
            double from = 0;
            foreach (double s in crossings.OrderBy(x => x))
            {
                double to = Math.Max(from, s - gap / 2);
                if (to - from > 1e-6) keep.Add(Piece(strand, from, to));
                from = Math.Min(total, s + gap / 2);
            }
            if (total - from > 1e-6) keep.Add(Piece(strand, from, total));
            return keep.Where(c => c != null).ToList();
        }

        static Curve Piece(Curve strand, double from, double to)
        {
            if (!strand.LengthParameter(from, out double t0) || !strand.LengthParameter(to, out double t1)) return null;
            return strand.Trim(t0, t1);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.Weave;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D9AF71E3-BF3D-42E9-813E-8A22CF585571"); }
        }
    }
}
