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
                if (EstDegenere(pts))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A group whose points are aligned or merged in XY was skipped.");
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
        /// Vrai quand les points, vus en plan XY, n'ont pas trois positions distinctes non alignees :
        /// le solveur ne peut alors produire aucun triangle valable.
        /// </summary>
        private static bool EstDegenere(List<Point3d> pts)
        {
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (Point3d p in pts)
            {
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            }
            double etendue = Math.Max(maxX - minX, maxY - minY);
            if (etendue <= 0) return true;
            double tol = etendue * 1e-9; // tolerance relative a la taille du nuage

            Point3d a = pts[0];
            int iB = -1;
            for (int i = 1; i < pts.Count; i++)
                if (Math.Abs(pts[i].X - a.X) > tol || Math.Abs(pts[i].Y - a.Y) > tol) { iB = i; break; }
            if (iB < 0) return true;

            Point3d b = pts[iB];
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double longueur = Math.Sqrt(dx * dx + dy * dy);
            for (int i = iB + 1; i < pts.Count; i++)
            {
                // Distance du point a la droite (a, b) en plan XY.
                double d = Math.Abs(dx * (pts[i].Y - a.Y) - dy * (pts[i].X - a.X)) / longueur;
                if (d > tol) return false;
            }
            return true;
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
