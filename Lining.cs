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

                // Une courbe par rangÃ©e ; la derniÃ¨re rangÃ©e n'est pas tracÃ©e.
                for (int i = 0; i < rows.Count - 1; i++)
                {
                    List<Point3d> pointList = rows[i];
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