using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using static GH_IO.VersionNumber;

namespace Cuttlefish
{
    public class Worm : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Worm class.
        /// </summary>
        public Worm()
          : base("Worm", "WORM",
              "Generate random curve by Voronoi cells",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Cells", "C", "The cells to generate worm.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Head", "H", "The curves to start & end worms.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Worms", "W", "The result curves of the worms.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> cellCurves = new List<Curve>();
            List<Curve> head = new List<Curve>();

            if (!DA.GetDataList(0, cellCurves)) return;
            if (!DA.GetDataList(1, head)) return;

            // Les cellules arrivent en Curve : GH ne sait pas les caster directement en PolylineCurve.
            List<PolylineCurve> cells = new List<PolylineCurve>();
            foreach (Curve crv in cellCurves)
            {
                if (crv != null && crv.TryGetPolyline(out Polyline pl))
                    cells.Add(new PolylineCurve(pl));
                else
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A cell is not a polyline and was skipped.");
            }

            int branch1 = 0;

            Random rand = new Random();
            DataTree<Curve> curves = new DataTree<Curve>();

                foreach ( PolylineCurve cell in cells)
                {

                    List<Curve> edges = new List<Curve>();
                    List<Point3d> mids = new List<Point3d>();
                    List<Vector3d> vects = new List<Vector3d>();

                    Point3d center = new Point3d();

                    edges = cell.DuplicateSegments().ToList();
                    center = cell.ToPolyline().CenterPoint();
                    List<int> index = new List<int>(Enumerable.Range(0, edges.Count));
                    Shuffle(index, rand);

                    for (int i = 0; i < edges.Count; i++)
                    {
                        mids.Add(new Point3d());
                        vects.Add(new Vector3d());
                        mids[i] = edges[i].PointAtLength(edges[i].GetLength() / 2);

                        Vector3d tangent = new Vector3d(edges[i].TangentAtStart);
                        vects[i] = new Vector3d(-tangent.Y, tangent.X, 0);
                    }

                    List<Vector3d> currentVect = new List<Vector3d>();
                    List<Point3d> currentPt = new List<Point3d>();

                    List<Curve> result = new List<Curve>();

                    int startIndex = 0;
                    int branchIndex = 0;
                    for (int i = 0; i < vects.Count; i++)
                    {
                        currentVect.Add(vects[index[i]]);
                        currentPt.Add(mids[index[i]]);
                    }
                    if (currentVect.Count % 2 == 1)
                    {
                        List<Curve> movedPin = MoveCurve(head, currentPt[0], currentVect[0]);

                        foreach (Curve c in movedPin)
                        {
                            int[] path = { branch1, branchIndex };
                            curves.Add(c, new GH_Path(path));
                        }
                        startIndex = 1;
                        branchIndex++;
                    }
                    for (int j = startIndex; j < currentVect.Count - 1; j += 2)
                    {
                        //curves.Add(new List<Curve>());
                        Curve c = CreateNurb(currentVect[j], currentVect[j + 1], currentPt[j], currentPt[j + 1], center);
                        int[] path = { branch1, branchIndex };

                        curves.Add(c, new GH_Path(path));
                        branchIndex++;
                    }

                    branch1++;
                }
            DA.SetDataTree(0, curves);


            void Shuffle<T>(List<T> list, Random rng)
            {
                int n = list.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    T value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
            }

            //Point3d Intersection(Vector3d aV, Vector3d bV, Point3d aPt, Point3d bPt)
            //{
            //    Point3d intersection;
            //    Line aL = new Line(aPt, aPt + aV);
            //    Line bL = new Line(bPt, bPt + bV);
            //    Rhino.Geometry.Intersect.Intersection.LineLine(aL, bL, out double a, out double b);
            //    intersection = aL.PointAt(a);

            //    return intersection;
            //}

            NurbsCurve CreateNurb(Vector3d aV, Vector3d bV, Point3d aPt, Point3d bPt, Point3d center)
            {
                List<Point3d> points = new List<Point3d>();
                points.Add(aPt);
                points.Add(bPt);
                NurbsCurve curve = new NurbsCurve((NurbsCurve)Curve.CreateInterpolatedCurve(points, 3, CurveKnotStyle.Uniform, aV, -bV));
                return curve;
            }
            List<Curve> MoveCurve(List<Curve> pin, Point3d targetPt, Vector3d targetVect)
            {
                List<Curve> resultCurve = new List<Curve>();
                foreach (Curve curve in pin)
                {
                    Curve copy = curve.DuplicateCurve();
                    copy.Translate(new Vector3d(targetPt - new Point3d(0, 0, 0)));
                    copy.Transform(Transform.Rotation(Vector3d.YAxis, targetVect, targetPt));

                    resultCurve.Add(copy);
                }

                return resultCurve;
            }

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
                return Properties.Resources.Worm;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("58E49FC4-2E4D-4FEE-9656-AFE22DCA1108"); }
        }
    }
}