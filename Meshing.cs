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
    public class Meshing : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Meshing class.
        /// </summary>
        public Meshing()
          : base("Meshing", "NET",
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
            // Jamais lue par le composant : facultative, et conservÃ©e pour que les .gh dÃ©jÃ  cÃ¢blÃ©s gardent leurs fils.
            pManager.AddRectangleParameter("Rectangle", "R", "Not used. Kept so that existing definitions stay wired.", GH_ParamAccess.list);
            pManager[2].Optional = true;
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

            if (!DA.GetDataTree(0, out points)) return;
            DA.GetData(1, ref lineType);

            foreach (var rows in Outils.Groupes(points))
            {
                Outils.AjusterFinRangees(rows);
                Vector3d direction = Outils.DirectionRangees(rows);

                for (int i = 0; i < rows.Count - 1; i++)
                {
                    var rowA = rows[i];
                    var rowB = rows[i + 1];

                    int count = Math.Min(rowA.Count, rowB.Count);

                    // Zigzag entre les deux rangÃ©es, en sens alternÃ© d'une paire Ã  l'autre.
                    List<Point3d> pointList = new List<Point3d>();
                    var pairs = i % 2 == 0 ? rowA : rowB;
                    var impairs = i % 2 == 0 ? rowB : rowA;
                    for (int k = 0; k < count; k++)
                    {
                        pointList.Add(k % 2 == 0 ? pairs[k] : impairs[k]);
                    }

                    if (pointList.Count < 2)
                        continue;

                    if (lineType)
                    {
                        Curve courbe = Curve.CreateInterpolatedCurve(pointList, 3, CurveKnotStyle.Uniform, direction, direction);
                        if (courbe != null) result.Add(courbe);
                    }
                    else
                    {
                        result.Add(new Polyline(pointList).ToNurbsCurve());
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
                return Cuttlefish.Properties.Resources.Meshing;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7847624B-226F-4287-9D2F-9408587BE5EE"); }
        }
    }
}