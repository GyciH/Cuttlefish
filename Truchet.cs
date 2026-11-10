using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuttlefish
{
    public class Truchet : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Truchet class.
        /// </summary>
        public Truchet()
          : base("Truchet", "TRUCH",
              "Draw Truchet tiles on a grid of points",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The grid of points, one branch per row.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Type", "T", "0 = quarter arcs, 1 = diagonals.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Seed", "S", "The seed of the random draw.", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "The result curves, joined into continuous paths.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> points;
            int type = 0;
            int seed = 0;

            if (!DA.GetDataTree(0, out points)) return;
            DA.GetData(1, ref type);
            DA.GetData(2, ref seed);

            Random rand = new Random(seed);
            double tolerance = DocumentTolerance();
            List<Curve> curves = new List<Curve>();

            foreach (var rows in Outils.Groupes(points))
            {
                List<Curve> tiles = new List<Curve>();
                Vector3d normale = Normale(rows);
                for (int i = 0; i < rows.Count - 1; i++)
                {
                    int count = Math.Min(rows[i].Count, rows[i + 1].Count);
                    for (int j = 0; j < count - 1; j++)
                    {
                        // Coins de la case : c00 et c11 opposés, c10 et c01 opposés.
                        Point3d c00 = rows[i][j], c10 = rows[i + 1][j];
                        Point3d c11 = rows[i + 1][j + 1], c01 = rows[i][j + 1];
                        bool flip = rand.Next(2) == 1;

                        if (type == 1)
                        {
                            tiles.Add(flip ? new LineCurve(c10, c01) : new LineCurve(c00, c11));
                            continue;
                        }

                        Point3d m0 = Outils.Milieu(c00, c10), m1 = Outils.Milieu(c10, c11);
                        Point3d m2 = Outils.Milieu(c11, c01), m3 = Outils.Milieu(c01, c00);
                        Point3d centre = Outils.Milieu(m0, m2);
                        if (flip)
                        {
                            tiles.Add(QuarterArc(m0, c10, m1, centre, normale));
                            tiles.Add(QuarterArc(m2, c01, m3, centre, normale));
                        }
                        else
                        {
                            tiles.Add(QuarterArc(m3, c00, m0, centre, normale));
                            tiles.Add(QuarterArc(m1, c11, m2, centre, normale));
                        }
                    }
                }
                if (tiles.Count > 0)
                    curves.AddRange(Curve.JoinCurves(tiles, tolerance));
            }

            DA.SetDataList(0, curves);
        }

        /// <summary>
        /// Normale moyenne de la grille, à partir des diagonales de chaque case. Axe Z si elle est dégénérée.
        /// </summary>
        static Vector3d Normale(List<List<Point3d>> rows)
        {
            Vector3d somme = Vector3d.Zero;
            for (int i = 0; i < rows.Count - 1; i++)
            {
                int count = Math.Min(rows[i].Count, rows[i + 1].Count);
                for (int j = 0; j < count - 1; j++)
                    somme += Vector3d.CrossProduct(rows[i + 1][j + 1] - rows[i][j], rows[i][j + 1] - rows[i + 1][j]);
            }
            return somme.Unitize() ? somme : Vector3d.ZAxis;
        }

        /// <summary>
        /// Arc autour d'un coin, de start à end, les milieux de deux arêtes de la case.
        /// À chaque extrémité, la courbe part perpendiculairement à son arête : cette direction ne
        /// dépend que de l'arête, donc les deux cases qui la partagent (même sur deux grilles
        /// voisines) s'y raccordent sans cassure.
        /// Arc rationnel de degré 2 quand les deux tangentes se croisent à distance raisonnable
        /// (quart de cercle exact sur une case carrée), courbe de Bézier cubique sinon.
        /// </summary>
        static Curve QuarterArc(Point3d start, Point3d corner, Point3d end, Point3d centre, Vector3d normale)
        {
            // start est sur l'arête qui arrive au coin, end sur celle qui en repart, en tournant toujours
            // dans le même sens : n × arête pointe dans la case. Le sens vient de la grille et non de la
            // géométrie, donc les deux cases d'une arête ont des tangentes opposées même si le flou la plie.
            Vector3d t0 = Vector3d.CrossProduct(normale, corner - start);
            Vector3d t1 = Vector3d.CrossProduct(normale, end - corner);
            Vector3d corde = end - start;
            double longueur = corde.Length;

            if (!t0.Unitize() || !t1.Unitize() || longueur < 1e-9)
            {
                Point3d sommet = start + (end - corner);
                NurbsCurve repli = NurbsCurve.Create(false, 2, new[] { start, sommet, end });
                repli.Points.SetPoint(1, sommet, Math.Sqrt(0.5));
                return repli;
            }

            // Hauteur disponible au-dessus de la corde : l'arc opposé de la case est de l'autre côté de
            // la parallèle à la corde passant par le centre. Rester sous cette hauteur évite tout croisement.
            Vector3d haut = Vector3d.CrossProduct(normale, corde);
            haut.Unitize();
            if (haut * (centre - start) < 0) haut = -haut;
            double hauteur = 0.95 * (haut * (centre - start));

            // Arc rationnel si les tangentes se croisent assez près et si les extrémités sont à la même
            // cote : sa flèche vaut poids / (1 + poids) de la hauteur du point de contrôle.
            double det = Vector3d.CrossProduct(t0, t1) * normale;
            if (Math.Abs(det) > 1e-9 && Math.Abs(corde * normale) < 1e-9)
            {
                double a = Vector3d.CrossProduct(corde, t1) * normale / det;
                double b = Vector3d.CrossProduct(corde, t0) * normale / det;
                double limite = 1.2 * longueur;
                Point3d control = start + t0 * a;
                double poids = Math.Sqrt(0.5);
                if (a > 0 && b > 0 && a <= limite && b <= limite
                    && (control - start) * haut * poids / (1 + poids) < hauteur)
                {
                    NurbsCurve arc = NurbsCurve.Create(false, 2, new[] { start, control, end });
                    arc.Points.SetPoint(1, control, poids);
                    return arc;
                }
            }

            // Bézier cubique : la flèche ne dépasse pas 3/4 de la hauteur des poignées, raccourcies au besoin.
            double penteMax = Math.Max(Math.Abs(t0 * haut), Math.Abs(t1 * haut));
            double poignee = 0.39 * longueur;
            if (penteMax > 1e-9) poignee = Math.Min(poignee, hauteur / (0.75 * penteMax));
            poignee = Math.Max(poignee, 0.1 * longueur);
            return NurbsCurve.Create(false, 3, new[] { start, start + t0 * poignee, end + t1 * poignee, end });
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.Truchet;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A0FF062D-2EE6-4E76-8C65-02C37CA7CF17"); }
        }
    }
}
