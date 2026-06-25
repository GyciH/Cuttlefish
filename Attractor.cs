using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Shapes;

namespace Cuttlefish
{
    public class Attractor : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Attractor class.
        /// </summary>
        public Attractor()
          : base("Attractor", "ATRK",
              "Attract points with curve",
              "Cuttlefish", "Tansform")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The points to attract.", GH_ParamAccess.tree);
            pManager.AddCurveParameter("Curve", "C", "The curve to attract the points to.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Attraction Factor", "AF", "The factor of attraction.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Attraction Distance", "AD", "The distance of attraction.", GH_ParamAccess.item, 25);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The result points of the grid.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Grasshopper.Kernel.Data.GH_Structure<GH_Point> points;
            double fA = 1.0, dA = 25;
            List<Curve> curves = new List<Curve>();

            var result = new Grasshopper.DataTree<Point3d>();

            if (!DA.GetDataTree(0, out points)) return;
            DA.GetDataList(1, curves);
            DA.GetData(2, ref fA);
            DA.GetData(3, ref dA);

            var groups = points.Paths
                .Select((path, index) => new { Path = path, Index = index })
                .GroupBy(p => p.Path[0]);

            foreach (var group in groups)
            {
                var ids = group
                    .Select(x => x.Index)
                    .ToList();

                foreach (int id in ids)
                {

                    var path = points.Paths[id];
                    var branch = points.Branches[id];

                    foreach (GH_Point ghPt in branch)
                    {
                        Point3d pt = ghPt.Value;

                        Vector3d valVec = Vector3d.Zero;
                        int compteur = 0;

                        foreach (var curve in curves)
                        {
                            if (curve.ClosestPoint(pt, out double t, dA))
                            {
                                Point3d closestPt = curve.PointAt(t);
                                Vector3d vec = closestPt - pt;

                                valVec += (vec * Math.Pow(1 - Math.Pow(vec.Length / dA, 2), 2) * fA);

                                compteur++;
                            }
                        }
                        if (compteur != 0)
                        {
                            valVec /= compteur;

                        }
                        result.Add(pt + valVec, path);
                    }
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
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Cuttlefish.Properties.Resources.Attract;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6881ECC8-F1CF-4221-B689-EDC78B05B82F"); }
        }
    }
}