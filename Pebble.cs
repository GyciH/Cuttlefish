using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Cuttlefish
{
    public class Pebble : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Pebble class.
        /// </summary>
        public Pebble()
          : base("Pebble", "PEBL",
              "Shrink cells and round their corners into pebbles",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Cells", "C", "The closed planar cells to turn into pebbles.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Offset", "O", "The inward offset distance.", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Radius", "R", "The fillet radius at the corners.", GH_ParamAccess.item, 2);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Pebbles", "P", "The result pebbles.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> cells = new List<Curve>();
            double offset = 1;
            double radius = 2;

            if (!DA.GetDataList(0, cells)) return;
            DA.GetData(1, ref offset);
            DA.GetData(2, ref radius);

            if (offset < 0 || radius < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Offset and Radius must be positive.");
                return;
            }

            double tolerance = DocumentTolerance();
            double angle = DocumentAngleTolerance();
            List<Curve> pebbles = new List<Curve>();
            int lost = 0, sharp = 0;

            foreach (Curve cell in cells)
            {
                if (cell == null || !cell.IsClosed || !cell.TryGetPlane(out Plane plane, tolerance))
                {
                    lost++;
                    continue;
                }

                Curve shrunk = offset > 0 ? Inward(cell, plane, offset, tolerance) : cell.DuplicateCurve();
                if (shrunk == null) { lost++; continue; }

                Curve rounded = radius > 0 ? Curve.CreateFilletCornersCurve(shrunk, radius, tolerance, angle) : null;
                if (radius > 0 && rounded == null) sharp++;
                pebbles.Add(rounded ?? shrunk);
            }

            if (sharp > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, sharp + " pebble(s) kept sharp: the radius is too large for them.");
            if (lost > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, lost + " cell(s) skipped: not closed and planar, or too small for the offset.");

            DA.SetDataList(0, pebbles);
        }

        /// <summary>
        /// Décalage vers l'intérieur : on garde la courbe décalée dont l'aire est la plus petite.
        /// </summary>
        static Curve Inward(Curve cell, Plane plane, double distance, double tolerance)
        {
            double area = Area(cell);
            Curve best = null;
            double bestArea = area;
            foreach (double d in new[] { distance, -distance })
            {
                Curve[] off = cell.Offset(plane, d, tolerance, CurveOffsetCornerStyle.Sharp);
                if (off == null || off.Length != 1 || !off[0].IsClosed) continue;
                double a = Area(off[0]);
                if (a > 0 && a < bestArea) { best = off[0]; bestArea = a; }
            }
            return best;
        }

        static double Area(Curve c)
        {
            var amp = AreaMassProperties.Compute(c);
            return amp != null ? amp.Area : 0;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.Pebble;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6EB4AF91-AE5D-447D-8193-0D07FC0AA8C0"); }
        }
    }
}
