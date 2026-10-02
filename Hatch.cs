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
    public class Hatch : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Hatch class.
        /// </summary>
        public Hatch()
          : base("Hatch", "HATCH",
              "Fill each cell with parallel lines at a random angle",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Cells", "C", "The closed planar cells to hatch.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Spacing", "Sp", "The space between two hatch lines.", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("Seed", "S", "The seed of the random angles.", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Lines", "L", "The hatch lines, one branch per cell.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> cells = new List<Curve>();
            double spacing = 2;
            int seed = 0;

            if (!DA.GetDataList(0, cells)) return;
            DA.GetData(1, ref spacing);
            DA.GetData(2, ref seed);

            if (spacing <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Spacing must be greater than 0.");
                return;
            }

            Random rand = new Random(seed);
            double tolerance = DocumentTolerance();
            DataTree<Curve> result = new DataTree<Curve>();
            int lost = 0;

            for (int c = 0; c < cells.Count; c++)
            {
                Curve cell = cells[c];
                // Un angle par cellule, tiré même si la cellule est ignorée, pour que le Seed reste stable.
                double angle = rand.NextDouble() * Math.PI;
                GH_Path path = new GH_Path(c);
                result.EnsurePath(path);

                if (cell == null || !cell.IsClosed || !cell.TryGetPlane(out Plane plane, tolerance))
                {
                    lost++;
                    continue;
                }

                plane.Origin = AreaMassProperties.Compute(cell)?.Centroid ?? plane.Origin;
                Vector3d dir = Math.Cos(angle) * plane.XAxis + Math.Sin(angle) * plane.YAxis;
                Vector3d normal = Vector3d.CrossProduct(plane.ZAxis, dir);
                double reach = cell.GetBoundingBox(false).Diagonal.Length;

                for (double t = -reach; t <= reach; t += spacing)
                {
                    Point3d mid = plane.Origin + normal * t;
                    LineCurve line = new LineCurve(mid - dir * reach, mid + dir * reach);
                    var events = Intersection.CurveCurve(line, cell, tolerance, tolerance);
                    if (events == null || events.Count < 2) continue;

                    // Pair-impair : les morceaux entre deux intersections successives sont dans la cellule.
                    List<double> ts = events.Select(e => e.ParameterA).OrderBy(x => x).ToList();
                    for (int k = 0; k + 1 < ts.Count; k += 2)
                    {
                        Point3d a = line.PointAt(ts[k]), b = line.PointAt(ts[k + 1]);
                        if (a.DistanceTo(b) > tolerance)
                            result.Add(new LineCurve(a, b), path);
                    }
                }
            }

            if (lost > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, lost + " cell(s) skipped: not closed and planar.");

            DA.SetDataTree(0, result);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.Hatch;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("377E92C4-D3F3-4515-89F2-B33E4803FFC3"); }
        }
    }
}
