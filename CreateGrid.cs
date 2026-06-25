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

            DA.GetDataList(0, rectangles);
            DA.GetData(1, ref ecartX);
            DA.GetData(2, ref ecartY);

            Grasshopper.DataTree<Point3d> pointsTree = new Grasshopper.DataTree<Point3d>();


            for (int p = 0; p < rectangles.Count; p++)
            {
                List<List<Point3d>> points = new List<List<Point3d>>();
                int divisionX = (int)(rectangles[p].Width / ecartX);
                int divisionY = (int)(rectangles[p].Height / ecartY);

                if (divisionX % 2 != 0)
                {
                    divisionX++;
                }
                if (divisionY % 2 != 0)
                {
                    divisionY++;
                }

                for (int i = 0; i < divisionX + 1; i++)
                {
                    points.Add(new List<Rhino.Geometry.Point3d>());
                    GH_Path path = new GH_Path(p, i);
                    for (int j = 0; j < divisionY + 1; j++)
                    {
                        points[i].Add(new Rhino.Geometry.Point3d((((rectangles[p].Width / divisionX) * i) + rectangles[p].Corner(0).X), (((rectangles[p].Height / divisionY) * j) + rectangles[p].Corner(0).Y), 0));
                    }
                    pointsTree.AddRange(points[i], path);
                }
                points.Clear();
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