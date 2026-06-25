using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Render.PostEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Cuttlefish
{
    public class Lining : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Meshing class.
        /// </summary>
        public Lining()
          : base("Lining", "LINE",
              "Draw lines by points",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The points to draw lines.", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Line Type", "L", "Sharp or smooth line.", GH_ParamAccess.item, true);
            pManager.AddRectangleParameter("Rectangle", "R", "The rectangle to draw lines in.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "The result curves of the points.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Grasshopper.Kernel.Data.GH_Structure<GH_Point> points;
            bool lineType = true;

            var result = new List<Curve>();

            DA.GetDataTree(0, out points);
            DA.GetData(1, ref lineType);

            var groups = points.Paths
                .Select((path, index) => new { Path = path, Index = index })
                .GroupBy(p => p.Path[0]);

            foreach (var group in groups)
            {
                var ids = group
                    .Select(x => x.Index)
                    .ToList();


                for (int i = 0; i < ids.Count - 1; i++)
                {
                    int branchIndex = ids[i];

                    Vector3d vect = new Vector3d(points[branchIndex][1].Value - points[branchIndex][0].Value);

                    points[i][points.Branches[i].Count - 2].Value = new Point3d(points[i][points.Branches[i].Count - 1].Value - vect);
                }

                for (int i = 0; i < ids.Count - 1; i++)
                {
                    var rowA = points.Branches[ids[i]];
                    var rowB = rowA;

                    int count = Math.Min(rowA.Count, rowB.Count);

                    List<Point3d> pointList = new List<Point3d>();


                    int j = i;
                    if (i % 2 == 0)
                    {
                        for (int k = 0; k < count; k += 2)
                        {

                            pointList.Add(rowA[k].Value);

                            if (k + 1 < count)
                            {
                                pointList.Add(rowB[k + 1].Value);
                            }
                        }
                    }
                    else
                    {
                        for (int k = 0; k < count; k += 2)
                        {
                            pointList.Add(rowB[k].Value);

                            if (k + 1 < count)
                                pointList.Add(rowA[k + 1].Value);
                        }
                    }

                    if (pointList.Count < 2)
                        continue;


                    if (lineType)
                    {
                        result.Add(new NurbsCurve((NurbsCurve)Curve.CreateInterpolatedCurve(pointList, 3, CurveKnotStyle.Uniform, Vector3d.YAxis, Vector3d.YAxis)));
                    }
                    if (!lineType)
                    {
                        Polyline line = new Polyline();
                        foreach (Point3d point in pointList)
                        {
                            line.Add(point);
                        }
                        result.Add(line.ToNurbsCurve());
                    }
                }
            }
            DA.SetDataList(0, result);

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
                return Properties.Resources.Lining;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("B97C4401-74CB-4560-A1F1-CE4002B9CABF"); }
        }
    }
}