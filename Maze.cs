using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cuttlefish
{
    public class Maze : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Maze class.
        /// </summary>
        public Maze()
          : base("Maze", "MAZE",
              "Draw the walls of a random maze on a grid of points",
              "Cuttlefish", "Draw")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "The grid of points, one branch per row.", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Seed", "S", "The seed of the random draw.", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Walls", "W", "The walls of the maze.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> points;
            int seed = 0;

            if (!DA.GetDataTree(0, out points)) return;
            DA.GetData(1, ref seed);

            Random rand = new Random(seed);
            double tolerance = DocumentTolerance();
            List<Curve> walls = new List<Curve>();

            foreach (var rows in Outils.Groupes(points))
            {
                // Cases (i, j) entre les points rows[i][j] et rows[i+1][j+1].
                int nx = rows.Count - 1;
                int ny = rows.Count > 0 ? rows.Min(r => r.Count) - 1 : 0;
                if (nx < 1 || ny < 1) continue;

                // Murs encore debout : à droite de (i, j) et au-dessus de (i, j).
                bool[,] right = new bool[nx, ny];
                bool[,] up = new bool[nx, ny];
                bool[,] seen = new bool[nx, ny];
                for (int i = 0; i < nx; i++)
                    for (int j = 0; j < ny; j++) { right[i, j] = true; up[i, j] = true; }

                // Parcours en profondeur aléatoire : un arbre couvrant, donc un seul chemin entre deux cases.
                var stack = new Stack<int[]>();
                stack.Push(new[] { 0, 0 });
                seen[0, 0] = true;
                int[][] moves = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
                while (stack.Count > 0)
                {
                    int[] cell = stack.Peek();
                    var next = moves
                        .Select(m => new[] { cell[0] + m[0], cell[1] + m[1], m[0], m[1] })
                        .Where(c => c[0] >= 0 && c[1] >= 0 && c[0] < nx && c[1] < ny && !seen[c[0], c[1]])
                        .ToList();
                    if (next.Count == 0) { stack.Pop(); continue; }

                    int[] n = next[rand.Next(next.Count)];
                    if (n[2] == 1) right[cell[0], cell[1]] = false;
                    if (n[2] == -1) right[n[0], n[1]] = false;
                    if (n[3] == 1) up[cell[0], cell[1]] = false;
                    if (n[3] == -1) up[n[0], n[1]] = false;
                    seen[n[0], n[1]] = true;
                    stack.Push(new[] { n[0], n[1] });
                }

                List<Curve> lines = new List<Curve>();
                for (int i = 0; i < nx; i++)
                    for (int j = 0; j < ny; j++)
                    {
                        // Le bord droit de la dernière colonne et le bord haut de la dernière rangée
                        // font partie du contour ; la sortie est en haut de la dernière case.
                        bool exit = i == nx - 1 && j == ny - 1;
                        if (right[i, j]) lines.Add(new LineCurve(rows[i + 1][j], rows[i + 1][j + 1]));
                        if (up[i, j] && !exit) lines.Add(new LineCurve(rows[i][j + 1], rows[i + 1][j + 1]));
                    }
                // Contour gauche et bas, entrée en bas de la première case.
                for (int j = 0; j < ny; j++) lines.Add(new LineCurve(rows[0][j], rows[0][j + 1]));
                for (int i = 1; i < nx; i++) lines.Add(new LineCurve(rows[i][0], rows[i + 1][0]));

                walls.AddRange(Curve.JoinCurves(lines, tolerance));
            }

            DA.SetDataList(0, walls);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Cuttlefish.Properties.Resources.Maze;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1FE6B5E4-8D58-48D0-A367-1B5F9C0F9A5B"); }
        }
    }
}
