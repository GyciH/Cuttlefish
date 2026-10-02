using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuttlefish
{
    public class Contour : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Contour class.
        /// </summary>
        public Contour()
          : base("Contour", "CONT",
              "Draw contour lines of the distance to attractors over a grid of points",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The grid of points used to compute the field, one branch per row.", GH_ParamAccess.tree);
            pManager.AddGeometryParameter("Attractors", "A", "The curves or points the distance is measured to.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Levels", "L", "The number of contour lines.", GH_ParamAccess.item, 10);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "The contour lines, one branch per grid and level.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> points;
            List<IGH_GeometricGoo> attractors = new List<IGH_GeometricGoo>();
            int levels = 10;

            if (!DA.GetDataTree(0, out points)) return;
            if (!DA.GetDataList(1, attractors)) return;
            DA.GetData(2, ref levels);

            if (levels < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Levels must be at least 1.");
                return;
            }

            List<Curve> curves = new List<Curve>();
            List<Point3d> pts = new List<Point3d>();
            foreach (var goo in attractors)
            {
                Curve crv = null;
                Point3d pt = Point3d.Unset;
                if (GH_Convert.ToCurve(goo, ref crv, GH_Conversion.Both)) curves.Add(crv);
                else if (GH_Convert.ToPoint3d(goo, ref pt, GH_Conversion.Both)) pts.Add(pt);
            }
            if (curves.Count + pts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Attractors must be curves or points.");
                return;
            }

            Func<Point3d, double> field = p =>
            {
                double d = double.MaxValue;
                foreach (Curve c in curves)
                    if (c.ClosestPoint(p, out double t)) d = Math.Min(d, p.DistanceTo(c.PointAt(t)));
                foreach (Point3d q in pts)
                    d = Math.Min(d, p.DistanceTo(q));
                return d;
            };

            double tolerance = DocumentTolerance();
            DataTree<Curve> result = new DataTree<Curve>();
            var groupes = Outils.Groupes(points);

            for (int g = 0; g < groupes.Count; g++)
            {
                var rows = groupes[g];
                if (rows.Count < 2) continue;
                int count = rows.Min(r => r.Count);
                if (count < 2) continue;

                double[,] v = new double[rows.Count, count];
                double min = double.MaxValue, max = double.MinValue;
                for (int i = 0; i < rows.Count; i++)
                    for (int j = 0; j < count; j++)
                    {
                        v[i, j] = field(rows[i][j]);
                        min = Math.Min(min, v[i, j]);
                        max = Math.Max(max, v[i, j]);
                    }

                for (int l = 0; l < levels; l++)
                {
                    double level = min + (max - min) * (l + 1) / (levels + 1);
                    List<Curve> segments = new List<Curve>();

                    // Marching squares : sur chaque case, points de passage du niveau sur les arêtes.
                    for (int i = 0; i < rows.Count - 1; i++)
                        for (int j = 0; j < count - 1; j++)
                        {
                            int[,] c = { { i, j }, { i + 1, j }, { i + 1, j + 1 }, { i, j + 1 } };
                            List<Point3d> cross = new List<Point3d>();
                            for (int e = 0; e < 4; e++)
                            {
                                int a0 = c[e, 0], a1 = c[e, 1], b0 = c[(e + 1) % 4, 0], b1 = c[(e + 1) % 4, 1];
                                double va = v[a0, a1], vb = v[b0, b1];
                                if ((va < level) != (vb < level))
                                {
                                    double t = (level - va) / (vb - va);
                                    Point3d pa = rows[a0][a1], pb = rows[b0][b1];
                                    cross.Add(pa + t * (pb - pa));
                                }
                            }
                            for (int k = 0; k + 1 < cross.Count; k += 2)
                                segments.Add(new LineCurve(cross[k], cross[k + 1]));
                        }

                    if (segments.Count > 0)
                        result.AddRange(Curve.JoinCurves(segments, tolerance), new GH_Path(g, l));
                }
            }

            DA.SetDataTree(0, result);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.Contour;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7A15E828-2AC6-4A64-99CB-56077686E269"); }
        }
    }
}
