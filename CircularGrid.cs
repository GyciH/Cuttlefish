using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Cuttlefish
{
    public class CircularGrid : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CircularGrid class.
        /// </summary>
        public CircularGrid()
          : base("CircularGrid", "CirGrid",
              "Create a circular grid of points",
              "Cuttlefish", "Points")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCircleParameter("Circle", "C", "The Circle to create the grid in.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Density by circle", "X", "The max space between two points on a circle.", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("Density of circle", "Y", "The space between two circles.", GH_ParamAccess.item, 10);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The points of the grid, one branch per circle {input circle; ring}, the center in ring 0.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Circle> circle = new List<Circle>();
            double ecartX = 10;
            double ecartY = 10;

            if (!DA.GetDataList(0, circle)) return;
            if (!DA.GetData(1, ref ecartX)) return;
            if (!DA.GetData(2, ref ecartY)) return;

            if (ecartX <= 0 || ecartY <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "X and Y must be greater than 0.");
                return;
            }

            const double eps = 1e-9;
            Grasshopper.DataTree<Point3d> pointsTree = new Grasshopper.DataTree<Point3d>();

            for (int p = 0; p < circle.Count; p++)
            {
                // Le centre forme la rangée 0.
                pointsTree.Add(circle[p].Center, new GH_Path(p, 0));

                int divisionY = (int)Math.Floor(circle[p].Radius / ecartY + eps);

                for (int i = 1; i <= divisionY; i++)
                {
                    Circle currentCircle = new Circle(circle[p].Plane, ecartY * i);
                    int divisionX = Math.Max(1, (int)Math.Ceiling(currentCircle.Circumference / ecartX - eps));

                    List<Point3d> points = new List<Point3d>();
                    for (int j = 0; j < divisionX; j++)
                    {
                        points.Add(currentCircle.PointAt(2 * Math.PI * j / divisionX));
                    }
                    pointsTree.AddRange(points, new GH_Path(p, i));
                }
            }
            DA.SetDataTree(0, pointsTree);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.CircularGrid;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("38816210-3524-48A8-BCCB-A146B7B3EB4F"); }
        }
    }
}
