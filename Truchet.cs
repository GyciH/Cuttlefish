using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuttlefish
{
    public class Truchet : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Truchet class.
        /// </summary>
        public Truchet()
          : base("Truchet", "TRUCH",
              "Draw Truchet tiles on a grid of points",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The grid of points, one branch per row.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Type", "T", "0 = quarter arcs, 1 = diagonals.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Seed", "S", "The seed of the random draw.", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "The result curves, joined into continuous paths.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> points;
            int type = 0;
            int seed = 0;

            if (!DA.GetDataTree(0, out points)) return;
            DA.GetData(1, ref type);
            DA.GetData(2, ref seed);

            Random rand = new Random(seed);
            double tolerance = DocumentTolerance();
            List<Curve> curves = new List<Curve>();

            foreach (var rows in Outils.Groupes(points))
            {
                List<Curve> tiles = new List<Curve>();
                for (int i = 0; i < rows.Count - 1; i++)
                {
                    int count = Math.Min(rows[i].Count, rows[i + 1].Count);
                    for (int j = 0; j < count - 1; j++)
                    {
                        // Coins de la case : c00 et c11 opposés, c10 et c01 opposés.
                        Point3d c00 = rows[i][j], c10 = rows[i + 1][j];
                        Point3d c11 = rows[i + 1][j + 1], c01 = rows[i][j + 1];
                        bool flip = rand.Next(2) == 1;

                        if (type == 1)
                        {
                            tiles.Add(flip ? new LineCurve(c10, c01) : new LineCurve(c00, c11));
                            continue;
                        }

                        Point3d m0 = Outils.Milieu(c00, c10), m1 = Outils.Milieu(c10, c11);
                        Point3d m2 = Outils.Milieu(c11, c01), m3 = Outils.Milieu(c01, c00);
                        if (flip)
                        {
                            tiles.Add(QuarterArc(m0, c10, m1));
                            tiles.Add(QuarterArc(m2, c01, m3));
                        }
                        else
                        {
                            tiles.Add(QuarterArc(m3, c00, m0));
                            tiles.Add(QuarterArc(m1, c11, m2));
                        }
                    }
                }
                if (tiles.Count > 0)
                    curves.AddRange(Curve.JoinCurves(tiles, tolerance));
            }

            DA.SetDataList(0, curves);
        }

        /// <summary>
        /// Arc rationnel de degré 2 centré sur un coin : quart de cercle exact sur une case carrée,
        /// arc lisse sur une case déformée. Le point de contrôle est le sommet opposé au coin
        /// dans le parallélogramme (start, corner, end), là où se croisent les tangentes.
        /// </summary>
        static Curve QuarterArc(Point3d start, Point3d corner, Point3d end)
        {
            Point3d control = start + (end - corner);
            NurbsCurve arc = NurbsCurve.Create(false, 2, new[] { start, control, end });
            arc.Points.SetPoint(1, control, Math.Sqrt(0.5));
            return arc;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.Truchet;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A0FF062D-2EE6-4E76-8C65-02C37CA7CF17"); }
        }
    }
}
