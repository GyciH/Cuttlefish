using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuttlefish
{
    public class Wander : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Wander class.
        /// </summary>
        public Wander()
          : base("Wander", "WAND",
              "Draw filleted polylines wandering randomly between neighbouring rows of points",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The points to wander on, one branch per row.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Radius", "R", "The fillet radius at the corners.", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "The result wandering curves.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> points;
            double rayon = 1;

            if (!DA.GetDataTree(0, out points)) return;
            if (!DA.GetData(1, ref rayon)) return;

            List<List<Point3d>> rows = points.Branches
                .Select(branch => branch.Where(p => p != null).Select(p => p.Value).ToList())
                .ToList();

            List<int> ids = new List<int> { -1, 0, 0, 1 };
            List<Curve> curves = new List<Curve>();
            Random rand = new Random();
            double tolerance = DocumentTolerance();
            bool shortNeighbour = false;

            for (int i = 1; i < rows.Count; i++)
            {
                if (rows[i].Count < 2)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A row with less than 2 points was skipped.");
                    continue;
                }

                List<Point3d> pointList = new List<Point3d>();
                for (int j = 0; j < rows[i].Count; j++)
                {
                    int offset = ids[rand.Next(0, ids.Count)];
                    int row = Math.Max(0, Math.Min(rows.Count - 1, i + offset));

                    // Rangée voisine trop courte : on reste sur la rangée courante.
                    if (j >= rows[row].Count)
                    {
                        row = i;
                        shortNeighbour = true;
                    }
                    pointList.Add(rows[row][j]);
                }

                Polyline poly = new Polyline(pointList);
                poly.DeleteShortSegments(tolerance);
                if (poly.Count < 2) continue;

                Curve fillet = Curve.CreateFilletCornersCurve(poly.ToPolylineCurve(), rayon, tolerance, DocumentAngleTolerance());
                if (fillet != null)
                    curves.Add(fillet);
            }

            if (shortNeighbour)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some neighbour rows are shorter: their missing points were taken from the current row.");

            DA.SetDataList(0, curves);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.Wander;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("E66124D9-94B1-4BC5-BE1E-EA9BB03B6156"); }
        }
    }
}
