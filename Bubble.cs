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
    public class Bubble : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Bubble class.
        /// </summary>
        public Bubble()
          : base("Bubble", "BUBL",
              "Draw a circle on each point, sized by the distance to its nearest neighbour",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The centers of the bubbles.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Factor", "F", "The radius as a factor of half the distance to the nearest point (1 = bubbles touch).", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCircleParameter("Bubbles", "B", "The bubbles, with the same tree as the points.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> points;
            double factor = 1;

            if (!DA.GetDataTree(0, out points)) return;
            DA.GetData(1, ref factor);

            if (factor <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Factor must be greater than 0.");
                return;
            }

            // Le plus proche voisin est cherché parmi tous les points de l'arbre.
            List<Point3d> all = points.AllData(true).OfType<GH_Point>().Select(p => p.Value).ToList();
            if (all.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "At least 2 points are needed.");
                return;
            }

            List<int[]> neighbours = RTree.Point3dKNeighbors(all, all, 2).ToList();
            double tolerance = DocumentTolerance();
            DataTree<Circle> result = new DataTree<Circle>();
            int k = 0;

            for (int b = 0; b < points.PathCount; b++)
            {
                GH_Path path = points.Paths[b];
                result.EnsurePath(path);
                foreach (GH_Point p in points.Branches[b])
                {
                    if (p == null) continue;
                    Point3d c = all[k];
                    // Le premier voisin est le point lui-même ; on prend l'autre.
                    double d = neighbours[k].Where(i => i != k).Select(i => c.DistanceTo(all[i])).DefaultIfEmpty(0).Min();
                    k++;
                    if (d <= tolerance) continue;
                    result.Add(new Circle(new Plane(c, Vector3d.ZAxis), factor * d / 2), path);
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
                return Cuttlefish.Properties.Resources.Bubble;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("454DDC48-A2B4-41F2-8002-5C0A4C44757D"); }
        }
    }
}
