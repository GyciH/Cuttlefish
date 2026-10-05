using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Cuttlefish.Properties;
using Grasshopper.Kernel.Types;
using System.Linq;

namespace Cuttlefish
{
    public class Blur : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Blur class.
        /// </summary>
        public Blur()
          : base("Blur", "BLR",
              "Randomly move points",
              "Cuttlefish", "Transform")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The points to blur.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("BlurX", "Bx", "Factor X of blur.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("BlurY", "By", "Factor Y of blur.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("BlurZ", "Bz", "Factor Z of blur.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Seed", "S", "The seed of the random draw.", GH_ParamAccess.item, 0);
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
            double x = 1.0, y = 1.0, z = 0;

            if(!DA.GetDataTree(0, out points)) return;
            DA.GetData(1, ref x);
            DA.GetData(2, ref y);
            DA.GetData(3, ref z);
            int seed = 0;
            DA.GetData(4, ref seed);

            var groups = points.Paths
                .Select((path, index) => new { Path = path, Index = index })
                .GroupBy(p => p.Path[0]);

            var result = new Grasshopper.DataTree<Point3d>();
            Random rand = new Random(seed);

            foreach (var group in groups)
            {
                var ids = group
                    .Select(id => id.Index)
                    .ToList();                    
                    
                Vector3d vect = new Vector3d(points[ids[ids.Count - 1]][0].Value - points[ids[0]][0].Value);

                List<double> randJoin = new List<double> { 2, 31, 76, 98, 7, 97, 56, 94, 71, 1, 36, 2, 76, 35, 63, 61, 52, 18, 38, 16 };
                for (int i = 0; i < randJoin.Count; i++)
                {
                    randJoin[i] = randJoin[i] / 100;
                }


                    
                int manque = Math.Max(points.PathCount, points.Branches.Count) * 2 - randJoin.Count;
                manque = (manque + 5) * 2;
                for (int i = 0; i < manque; i++)
                {
                    randJoin.Add(randJoin[i]);
                }

                for (int i = 0; i < ids.Count; i++)
                {
                    var path = points.Paths[ids[i]];
                    var branch = points.Branches[ids[i]];

                    if (i == ids.Count - 1)
                    {
                        for (int j = 0; j < branch.Count; j++)
                        {
                            result.Add(new Point3d(result.Branches[ids[0]][j] + vect), path);
                        }
                        continue;
                    }
                    else
                    {
                        for (int j = 0; j < branch.Count; j++)
                        {
                            Point3d pt = branch[j].Value;
                            Point3d newPt;

                            if (i != 0 && i != points.PathCount - 1 && j != 0 && j != branch.Count - 1)
                            {
                                newPt = new Point3d(
                                    pt.X + ((rand.NextDouble() - 0.5) * x),
                                    pt.Y + ((rand.NextDouble() - 0.5) * y),
                                    pt.Z + ((rand.NextDouble() - 0.5) * z)
                                    );
                            }
                            else if (i == 0 || i == points.PathCount - 1)
                            {
                                // Le dernier point de la colonne reprend le décalage du premier :
                                // la première et la dernière ligne restent identiques à la hauteur près.
                                int k = (j == branch.Count - 1) ? 0 : j;
                                newPt = new Point3d(
                                    pt.X + ((randJoin[k] - 0.5) * x),
                                    pt.Y + ((randJoin[k + 1] - 0.5) * y),
                                    pt.Z + ((randJoin[k + 2] - 0.5) * z)
                                    );
                            }
                            else if (j == 0 || j == branch.Count - 1)
                            {
                                newPt = new Point3d(
                                    pt.X + ((randJoin[i] - 0.5) * x),
                                    pt.Y + ((randJoin[i + 1] - 0.5) * y),
                                    pt.Z + ((randJoin[i + 2] - 0.5) * z)
                                    );
                            }
                            else continue;

                            result.Add(newPt, path);
                        }

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
                return Cuttlefish.Properties.Resources.Blur;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0F5E1E26-1C6C-44F8-A508-6061492C9546"); }
        }
    }
}