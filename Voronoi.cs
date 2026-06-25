using GH_IO.Types;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters.Hints;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Render.PostEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Cuttlefish
{
    public class Voronoi : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Voronoi class.
        /// </summary>
        public Voronoi()
          : base("Voronoi", "VORO",
              "Draw voronoi Cells from points",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddRectangleParameter("Rectangle", "R", "The rectangle to draw voronoi cells in.", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "P", "The points to draw voronoi cells from.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "The result curves of the voronoi cells.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<Rectangle3d> rectangles = new List<Rectangle3d>();
            Grasshopper.Kernel.Data.GH_Structure<GH_Point> points;
            List<Polyline> poliCells = new List<Polyline>();
            List<List<Polyline>> poliCellsTree = new List<List<Polyline>>();

            DA.GetDataList(0, rectangles);
            DA.GetDataTree(1, out points);

            

            var groups = points.Paths
                .Select((path, index) => new { Path = path, Index = index })
                .GroupBy(p => p.Path[0]);
            int idRect = 0;

            foreach (var group in groups)
            {
                var ids = group
                    .Select(x => x.Index)
                    .ToList();

                List<Point3d> currentPoints = new List<Point3d>();
                for (int i = 0; i < ids.Count; i++)
                {
                    for (int j = 0; j < points[ids[i]].Count; j ++)
                    {
                        currentPoints.Add(points[ids[i]][j].Value);
                    }
                    
                }

                Rectangle3d rect = rectangles[idRect];

                idRect ++;

                List<Point3d> vertex = new List<Point3d>();

                for (int i = 0; i < 4; i++)
                {
                    vertex.Add(rect.Corner(i));
                }

                Line bisector;
                List<Point3d> sites = new List<Point3d>(ids.Count);
                List<List<Point3d>> cells = new List<List<Point3d>>();


                for (int i = 0; i < currentPoints.Count - 1; i++)
                {
                    List<Point3d> currentCell = new List<Point3d>(vertex);
                    for (int j = 0; j < currentPoints.Count - 1; j++)
                    {

                        if (i == j) { continue; }
                        else
                        {
                            bisector = CreateBisector(currentPoints[i], currentPoints[j]);
                            double referenceSide = ProdVectoriel(currentPoints[i], bisector);

                            currentCell = ClipPolygon(currentCell, bisector, referenceSide);
                        }
                    }

                    cells.Add(currentCell);

                }
                
                foreach (List<Point3d> ver in cells)
                {
                    if (ver.Count > 2)
                    {
                        if (!ver[0].EpsilonEquals(ver[ver.Count - 1], 1e-6))
                            ver.Add(ver[0]);
                    }
                    Polyline poli = new Polyline(ver);
                    poliCells.Add(poli);
                }
                poliCellsTree.Add(poliCells);
            }

            DA.SetDataList(0, poliCells);
        

        Point3d MidOf(Point3d A, Point3d B)
        {
            Point3d mid = new Point3d();
            mid = (A + B) / 2;
            return mid;
        }
        Vector3d PerpVect(Point3d A, Point3d B)
        {
            Vector3d perpVect = new Vector3d();
            perpVect = B - A;
            perpVect = new Vector3d(-perpVect.Y, perpVect.X, 0);
            return perpVect;
        }
        Line CreateBisector(Point3d A, Point3d B, int k = 1000)
        {
            Point3d mid = MidOf(A, B);
            Vector3d perp = PerpVect(A, B);
            Point3d p1 = mid - perp * k;
            Point3d p2 = mid + perp * k;
            Line bisector = new Line(p1, p2);
            return bisector;
        }
        double ProdVectoriel(Point3d q, Line line)
        {
            Vector3d a = line.To - line.From;
            Vector3d b = q - line.From;
            return Vector3d.CrossProduct(a, b).Z;
        }
        Point3d Intersection(Line bisector, Point3d a, Point3d b)
        {
            Point3d intersection;
            Line line = new Line(a, b);
            Rhino.Geometry.Intersect.Intersection.LineLine(bisector, line, out double aPoint, out double bPoint);
            intersection = bisector.PointAt(aPoint);

            return intersection;
        }
        List<Point3d> ClipPolygon(List<Point3d> polygon, Line bisector, double referenceSide)
        {
            List<Point3d> newCell = new List<Point3d>();

            int nbVertex = polygon.Count;
            for (int k = 0; k < nbVertex; k++)
            {
                double current = ProdVectoriel(polygon[k % nbVertex], bisector);
                double next = ProdVectoriel(polygon[(k + 1) % nbVertex], bisector);
                bool currentInside = current * referenceSide > 0;
                bool nextInside = next * referenceSide > 0;

                if (currentInside && nextInside)
                {
                    newCell.Add(polygon[(k + 1) % nbVertex]);
                }
                else if (!currentInside && nextInside)
                {
                    newCell.Add(Intersection(bisector, polygon[k % nbVertex], polygon[(k + 1) % nbVertex]));
                    newCell.Add(polygon[(k + 1) % nbVertex]);
                }
                else if (!currentInside && !nextInside)
                {
                    continue;
                }
                else if (currentInside && !nextInside)
                {
                    newCell.Add(Intersection(bisector, polygon[k % nbVertex], polygon[(k + 1) % nbVertex]));
                }
            }

            return newCell;

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
                return Cuttlefish.Properties.Resources.Voronoi;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("23D73EA1-D44E-4536-BB9A-36E414430D9C"); }
        }
    }
}