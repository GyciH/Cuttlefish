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
            pManager.AddNumberParameter("Density by circle", "X", "Space betwen too points.", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("Density of circle", "Y", "Space betwen too circles.", GH_ParamAccess.item, 10);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Grid result", GH_ParamAccess.list);
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

            DA.GetDataList(0, circle);
            DA.GetData(1, ref ecartX);
            DA.GetData(2, ref ecartY);

            Grasshopper.DataTree<Point3d> pointsTree = new Grasshopper.DataTree<Point3d>();

            for (int p = 0; p < circle.Count; p++)
            {
                List<List<Point3d>> points = new List<List<Point3d>>();
                int divisionY = (int)circle[p].Radius/ (int)ecartY;
                points.Add(new List<Point3d>());

                points[0].Add(circle[p].Center);
                Circle previousCircle = circle[p];

                for (int i = 1; i < divisionY; i++)
                {
                    Circle currentCircle = new Circle(previousCircle.Center, previousCircle.Radius + ecartY);
                    List<double> param = new List<double>();
                    bool boucle = false;
                    double range = currentCircle.Circumference / ecartX;

                    while (boucle)
                    {                        
                        param.Add(range);
                        range += ecartX;
                        boucle = range < currentCircle.Circumference;
                    }

                    points.Add(new List<Point3d>());
                    foreach (var par in param)
                    {
                        points[i].Add(currentCircle.PointAt(par));
                    }
                    pointsTree.AddRange(points[i]);
                    
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
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
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