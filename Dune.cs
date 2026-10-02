using GH_IO.Types;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Cuttlefish
{
    public class Dune : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Dune class.
        /// </summary>
        public Dune()
          : base("Dune", "DUNE",
              "Generate dune design from points  ",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Points to use in deisgn.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Radius", "R", "Radius to apply in curves.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Result dune design.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Grasshopper.Kernel.Data.GH_Structure<GH_Point> points;
            double radius = 0;
            DA.GetDataTree(0, out points);
            DA.GetData(1, ref radius);

            List<int> ids = new List<int> { -1, 0, 0, 1 };
            List<Curve> curves = new List<Curve>();

            Random rand = new Random();

            for (int i = 1; i < points.PathCount; i++)
            {
                List<Curve> lines = new List<Curve>();
                List<Point3d> pointList = new List<Point3d>();
                for (int j = 0; j < points.Branches[i].Count; j++)
                {
                    int offset = ids[rand.Next(0, ids.Count - 1)];
                    pointList.Add( points.Branches[i + offset][j].Value);
                }

                for (int j = 0; j < points.Branches[i].Count - 1; j++)
                {
                    lines.Add(new LineCurve( pointList[j], pointList[j + 1]));
                }
                
                Curve poly = PolylineCurve.JoinCurves(lines)[0];



                curves.Add(Curve.CreateFilletCornersCurve(poly, radius, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAngleToleranceDegrees));
            }


            DA.SetDataList(0, curves);
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
                return Cuttlefish.Properties.Resources.Dune;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("47D25FBE-95D8-402F-A907-11402AE1C809"); }
        }
    }
}