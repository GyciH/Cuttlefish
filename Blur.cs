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
            pManager.AddNumberParameter("BlurX", "Bx", "Maximum displacement along X, in model units (each point moves by up to half of it either way). The default of 1 depends on the model scale.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("BlurY", "By", "Maximum displacement along Y, in model units (each point moves by up to half of it either way). The default of 1 depends on the model scale.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("BlurZ", "Bz", "Maximum displacement along Z, in model units (each point moves by up to half of it either way).", GH_ParamAccess.item, 0);
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

            // Décalages fixes des bords, identiques quel que soit le seed.
            // Valeurs entre 25 et 75, centrées sur 50, pour que les jonctions se voient moins.
            // La liste se répète : le nombre de points par colonne n'est pas limité.
            const double amplitudeBord = 1;
            double[] valeursBord = { 26, 41, 63, 74, 29, 74, 53, 72, 61, 26, 43, 26, 63, 43, 57, 56, 51, 34, 44, 33 };
            Func<int, double> bord = n => (valeursBord[n % valeursBord.Length] / 100 - 0.5) * amplitudeBord;
            bool irregulier = false;

            foreach (var group in groups)
            {
                var ids = group
                    .Select(id => id.Index)
                    .ToList();

                // Le raccord des bords n'a de sens que sur une grille complète. Sinon (CircularGrid,
                // branche unique, point vide) chaque point bouge au hasard et les vides sont ignorés.
                if (!EstGrille(points, ids))
                {
                    irregulier = true;
                    foreach (int id in ids)
                    {
                        result.EnsurePath(points.Paths[id]);
                        foreach (GH_Point ghPt in points.Branches[id])
                        {
                            if (ghPt == null) continue;
                            Point3d pt = ghPt.Value;
                            result.Add(new Point3d(
                                pt.X + ((rand.NextDouble() - 0.5) * x),
                                pt.Y + ((rand.NextDouble() - 0.5) * y),
                                pt.Z + ((rand.NextDouble() - 0.5) * z)
                                ), points.Paths[id]);
                        }
                    }
                    continue;
                }

                Vector3d vect = new Vector3d(points[ids[ids.Count - 1]][0].Value - points[ids[0]][0].Value);
                List<Point3d> premiereColonne = new List<Point3d>();

                for (int i = 0; i < ids.Count; i++)
                {
                    var path = points.Paths[ids[i]];
                    var branch = points.Branches[ids[i]];

                    // La dernière colonne est la copie de la première, à la largeur près.
                    if (i == ids.Count - 1)
                    {
                        foreach (Point3d pt in premiereColonne)
                        {
                            result.Add(pt + vect, path);
                        }
                        continue;
                    }

                    for (int j = 0; j < branch.Count; j++)
                    {
                        Point3d pt = branch[j].Value;
                        Point3d newPt;

                        if (i != 0 && j != 0 && j != branch.Count - 1)
                        {
                            newPt = new Point3d(
                                pt.X + ((rand.NextDouble() - 0.5) * x),
                                pt.Y + ((rand.NextDouble() - 0.5) * y),
                                pt.Z + ((rand.NextDouble() - 0.5) * z)
                                );
                        }
                        else
                        {
                            // La première colonne suit j, et son dernier point reprend le décalage du
                            // premier ; les autres colonnes suivent i, en bas comme en haut. La première
                            // et la dernière ligne restent ainsi identiques à la hauteur près.
                            int k = i != 0 ? i : (j == branch.Count - 1 ? 0 : j);
                            newPt = new Point3d(
                                pt.X + (bord(k) * x),
                                pt.Y + (bord(k + 1) * y),
                                pt.Z + (bord(k + 2) * z)
                                );
                        }

                        if (i == 0) premiereColonne.Add(newPt);
                        result.Add(newPt, path);
                    }
                }
            }

            if (irregulier)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Not a complete grid: the points were moved at random, without matching edges.");

            DA.SetDataTree(0, result);
        }

        // Vrai quand le groupe forme une grille complète : au moins 2 colonnes, autant de points
        // dans chacune, au moins 1 point et aucun point vide.
        private static bool EstGrille(Grasshopper.Kernel.Data.GH_Structure<GH_Point> points, List<int> ids)
        {
            if (ids.Count < 2) return false;
            int nb = points.Branches[ids[0]].Count;
            if (nb == 0) return false;
            return ids.All(id => points.Branches[id].Count == nb && points.Branches[id].All(p => p != null));
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