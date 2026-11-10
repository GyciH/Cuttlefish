using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuttlefish
{
    public class MapToSurface : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MapToSurface class.
        /// </summary>
        public MapToSurface()
          : base("MapToSurface", "MAP",
              "Map points and curves drawn in a rectangle onto a surface: X follows U and Y follows V",
              "Cuttlefish", "Transform")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "The points and curves to map.", GH_ParamAccess.tree);
            pManager.AddRectangleParameter("Rectangle", "R", "The rectangle the geometry was drawn in.", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Surface", "S", "The surface to map onto.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("By Length", "L", "True: U and V are spread by the real length of the surface. False: they follow its parameters.", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "G", "The mapped points and curves, with the same tree as the input.", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_GeometricGoo> geometrie;
            Rectangle3d rectangle = Rectangle3d.Unset;
            Surface surface = null;
            bool parLongueur = true;

            if (!DA.GetDataTree(0, out geometrie)) return;
            if (!DA.GetData(1, ref rectangle)) return;
            if (!DA.GetData(2, ref surface) || surface == null) return;
            DA.GetData(3, ref parLongueur);

            if (!rectangle.IsValid || Math.Abs(rectangle.X.Length) < 1e-9 || Math.Abs(rectangle.Y.Length) < 1e-9)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The rectangle must have a width and a height.");
                return;
            }

            var report = new ReportSurface(surface, rectangle, parLongueur, DocumentTolerance());
            var result = new DataTree<object>();
            int ignores = 0;

            for (int b = 0; b < geometrie.PathCount; b++)
            {
                GH_Path path = geometrie.Paths[b];
                result.EnsurePath(path);
                foreach (IGH_GeometricGoo goo in geometrie.Branches[b])
                {
                    if (goo == null) continue;

                    // La courbe d'abord : un cercle se convertit aussi en point, son centre.
                    Point3d point = Point3d.Unset;
                    Curve courbe = null;
                    if (GH_Convert.ToCurve(goo, ref courbe, GH_Conversion.Both))
                        foreach (Curve c in report.Courbe(courbe)) result.Add(c, path);
                    else if (GH_Convert.ToPoint3d(goo, ref point, GH_Conversion.Both))
                        result.Add(report.Point(point), path);
                    else
                        ignores++;
                }
            }

            if (ignores > 0)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ignores + " object(s) skipped: only points and curves can be mapped.");
            if (report.Deborde)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some geometry was outside the rectangle: it was moved to the edge of the surface.");

            DA.SetDataTree(0, result);
        }

        /// <summary>
        /// Reporte un point du rectangle sur la surface. Une direction fermée de la surface se
        /// referme d'elle-même ; sur une direction ouverte, ce qui dépasse est ramené au bord.
        /// </summary>
        private class ReportSurface
        {
            private const int Stations = 64;

            private readonly Surface surface;
            private readonly Rectangle3d rectangle;
            private readonly bool parLongueur;
            private readonly double tolerance;
            private readonly bool fermeU, fermeV;
            private readonly double[] tableU, tableV;
            private readonly double pas;

            public bool Deborde { get; private set; }

            public ReportSurface(Surface surface, Rectangle3d rectangle, bool parLongueur, double tolerance)
            {
                this.surface = surface;
                this.rectangle = rectangle;
                this.parLongueur = parLongueur;
                this.tolerance = tolerance;
                fermeU = surface.IsClosed(0);
                fermeV = surface.IsClosed(1);
                tableU = Table(0);
                tableV = Table(1);
                pas = (Math.Abs(rectangle.X.Length) + Math.Abs(rectangle.Y.Length)) / 2 / 150;
            }

            // Longueur cumulée moyenne (de 0 à 1) en fonction du paramètre, sur 9 isocourbes.
            private double[] Table(int direction)
            {
                Interval domaine = surface.Domain(direction), autre = surface.Domain(1 - direction);
                var moyenne = new double[Stations + 1];
                for (int k = 0; k < 9; k++)
                {
                    double c = autre.ParameterAt(k / 8.0);
                    var cumul = new double[Stations + 1];
                    Point3d precedent = Evaluer(direction, domaine.Min, c);
                    for (int i = 1; i <= Stations; i++)
                    {
                        Point3d p = Evaluer(direction, domaine.ParameterAt((double)i / Stations), c);
                        cumul[i] = cumul[i - 1] + p.DistanceTo(precedent);
                        precedent = p;
                    }
                    double total = cumul[Stations] > 0 ? cumul[Stations] : 1;
                    for (int i = 0; i <= Stations; i++) moyenne[i] += cumul[i] / total / 9;
                }
                return moyenne;
            }

            private Point3d Evaluer(int direction, double t, double c)
            {
                return direction == 0 ? surface.PointAt(t, c) : surface.PointAt(c, t);
            }

            private double Parametre(double x, Interval domaine, bool ferme, double[] table)
            {
                if (ferme) x -= Math.Floor(x);
                else
                {
                    if (x < -1e-9 || x > 1 + 1e-9) Deborde = true;
                    x = Math.Max(0, Math.Min(1, x));
                }
                if (!parLongueur) return domaine.ParameterAt(x);

                int i = Array.BinarySearch(table, x);
                if (i >= 0) return domaine.ParameterAt((double)i / Stations);
                i = ~i;
                if (i <= 0) return domaine.Min;
                if (i > Stations) return domaine.Max;
                double f = (x - table[i - 1]) / Math.Max(1e-12, table[i] - table[i - 1]);
                return domaine.ParameterAt((i - 1 + f) / Stations);
            }

            public Point3d Point(Point3d q)
            {
                rectangle.Plane.RemapToPlaneSpace(q, out Point3d l);
                double u = Parametre(rectangle.X.NormalizedParameterAt(l.X), surface.Domain(0), fermeU, tableU);
                double v = Parametre(rectangle.Y.NormalizedParameterAt(l.Y), surface.Domain(1), fermeV, tableV);
                return surface.PointAt(u, v);
            }

            /// <summary>
            /// Une courbe interpolée par portion lisse : les angles vifs restent vifs.
            /// </summary>
            public List<Curve> Courbe(Curve c)
            {
                var coupes = new List<double> { c.Domain.T0 };
                double t = c.Domain.T0;
                while (c.GetNextDiscontinuity(Continuity.G1_continuous, t, c.Domain.T1, out double suivant))
                {
                    coupes.Add(suivant);
                    t = suivant;
                }
                coupes.Add(c.Domain.T1);

                bool periodique = c.IsClosed && coupes.Count == 2;
                var morceaux = new List<Curve>();
                for (int i = 0; i + 1 < coupes.Count; i++)
                {
                    Curve m = Morceau(c, coupes[i], coupes[i + 1], periodique);
                    if (m != null) morceaux.Add(m);
                }
                if (morceaux.Count < 2) return morceaux;

                Curve[] joint = Curve.JoinCurves(morceaux, Math.Max(tolerance, 0.01));
                return joint != null && joint.Length > 0 ? joint.ToList() : morceaux;
            }

            private Curve Morceau(Curve c, double a, double b, bool periodique)
            {
                int n = Math.Max(8, (int)Math.Ceiling(c.GetLength(new Interval(a, b)) / pas));
                Curve portion = c.Trim(a, b) ?? c;
                double[] ts = portion.DivideByCount(n, true);
                if (ts == null) return null;

                var pts = new List<Point3d>();
                foreach (double x in ts)
                    Ajouter(pts, Point(portion.PointAt(x)));

                if (periodique && pts.Count >= 3)
                {
                    if (pts[0].DistanceTo(pts[pts.Count - 1]) < 1e-6) pts.RemoveAt(pts.Count - 1);
                    Curve fermee = Curve.CreateInterpolatedCurve(pts, 3, CurveKnotStyle.ChordPeriodic);
                    if (fermee != null) return fermee;
                }

                Ajouter(pts, Point(portion.PointAtEnd));
                if (pts.Count < 2) return null;

                // Tangentes imposées aux extrémités : deux courbes qui se rejoignent gardent la même direction.
                double e = pas / 20;
                Vector3d t0 = Point(portion.PointAtStart + portion.TangentAtStart * e) - Point(portion.PointAtStart);
                Vector3d t1 = Point(portion.PointAtEnd) - Point(portion.PointAtEnd - portion.TangentAtEnd * e);
                Curve interpolee = pts.Count >= 3 ? Curve.CreateInterpolatedCurve(pts, 3, CurveKnotStyle.Chord, t0, t1) : null;
                return interpolee ?? new PolylineCurve(pts);
            }

            private static void Ajouter(List<Point3d> pts, Point3d p)
            {
                if (pts.Count == 0 || pts[pts.Count - 1].DistanceTo(p) > 1e-6) pts.Add(p);
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.MapToSurface;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("FB56E7DC-B74C-44CD-A3D8-28CEA9B64F5D"); }
        }
    }
}
