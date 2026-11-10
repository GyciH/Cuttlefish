using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Cuttlefish
{
    public class CreateGrid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CreateGrid class.
        /// </summary>
        public CreateGrid()
          : base("CreateGrid", "Grid",
              "Generate a grid of point",
              "Cuttlefish", "Points")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddRectangleParameter("Rectangle", "R", "The rectangle to create the grid in.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Cell Max Size X", "X", "The Max size X of each cell in the grid.", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("Cell Max Size Y", "Y", "The Max size Y of each cell in the grid.", GH_ParamAccess.item, 10);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The points of the grid.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Rectangle3d> rectangles = new List<Rectangle3d>();
            double ecartX = 10;
            double ecartY = 10;

            if (!DA.GetDataList(0, rectangles)) return;
            DA.GetData(1, ref ecartX);
            DA.GetData(2, ref ecartY);

            if (ecartX <= 0 || ecartY <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "X and Y must be greater than 0.");
                return;
            }

            Grasshopper.DataTree<Point3d> pointsTree = new Grasshopper.DataTree<Point3d>();

            for (int p = 0; p < rectangles.Count; p++)
            {
                Rectangle3d rect = rectangles[p];
                // Width et Height sont négatifs si le rectangle a un intervalle inversé.
                double colonnes = Math.Floor(Math.Abs(rect.Width) / ecartX);
                double rangees = Math.Floor(Math.Abs(rect.Height) / ecartY);

                // Une cellule très petite demanderait des milliards de points et figerait Rhino.
                if (colonnes * rangees > MaxCellules)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "X and Y are too small for this rectangle: more than " + MaxCellules + " cells.");
                    continue;
                }

                int divisionX = PairSuperieur((int)colonnes);
                int divisionY = PairSuperieur((int)rangees);

                // Pas de la grille le long des côtés du rectangle : la grille suit son plan,
                // qu'il soit tourné ou hors du plan XY.
                Point3d origine = rect.Corner(0);
                Vector3d pasX = (rect.Corner(1) - origine) / divisionX;
                Vector3d pasY = (rect.Corner(3) - origine) / divisionY;

                for (int i = 0; i < divisionX + 1; i++)
                {
                    GH_Path path = new GH_Path(p, i);
                    pointsTree.EnsurePath(path);
                    for (int j = 0; j < divisionY + 1; j++)
                    {
                        pointsTree.Add(origine + pasX * i + pasY * j, path);
                    }
                }
            }
            DA.SetDataTree(0, pointsTree);
        }

        // Nombre de divisions pair et non nul : une cellule plus grande que le côté donne 2 divisions.
        private static int PairSuperieur(int n)
        {
            if (n < 2) return 2;
            return n % 2 != 0 ? n + 1 : n;
        }

        private const double MaxCellules = 4000000;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Cuttlefish.Properties.Resources.GridGenerator;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("D95A69C5-7398-4734-9904-E17C102E6D06"); }
        }
    }
}