using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Geometry;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuttlefish
{
    public class Delaunay : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Delaunay class.
        /// </summary>
        public Delaunay()
          : base("Delaunay", "DELA",
              "Draw the Delaunay triangles between points",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The points to triangulate, one triangulation per first path index.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Triangles", "T", "The closed triangles, one branch per triangulation.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> points;
            if (!DA.GetDataTree(0, out points)) return;

            DataTree<Curve> result = new DataTree<Curve>();
            var groupes = Outils.Groupes(points);

            for (int g = 0; g < groupes.Count; g++)
            {
                List<Point3d> pts = groupes[g].SelectMany(r => r).ToList();
                if (pts.Count < 3)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A group with less than 3 points was skipped.");
                    continue;
                }

                // Triangulation dans le plan XY ; les triangles gardent le Z des points.
                Node2List nodes = new Node2List();
                foreach (Point3d p in pts) nodes.Append(new Node2(p.X, p.Y));
                var faces = Grasshopper.Kernel.Geometry.Delaunay.Solver.Solve_Faces(nodes, 1);

                GH_Path path = new GH_Path(g);
                foreach (var f in faces)
                    result.Add(new PolylineCurve(new[] { pts[f.A], pts[f.B], pts[f.C], pts[f.A] }), path);
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
                return Cuttlefish.Properties.Resources.Delaunay;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9BB208B4-987A-4AE1-AC9E-53F5C63D6F71"); }
        }
    }
}
