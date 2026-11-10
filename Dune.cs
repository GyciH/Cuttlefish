using GH_IO.Types;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

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
            // Ajoutée en dernier, avec une valeur par défaut : les définitions .gh déjà
            // câblées gardent leurs fils et se rouvrent sans rien changer.
            pManager.AddIntegerParameter("Seed", "S", "The seed of the random draw.", GH_ParamAccess.item, 0);
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
            if (!DA.GetDataTree(0, out points)) return;
            if (!DA.GetData(1, ref radius)) return;
            int seed = 0;
            DA.GetData(2, ref seed);

            // Un pas vers la rangée précédente une fois sur trois, sinon on reste : c'est
            // ce que Dune a toujours dessiné (le tirage Next(0, Count - 1) excluait le +1 de
            // l'ancienne liste { -1, 0, 0, 1 }). L'errance dans les deux sens est Wander.
            List<int> ids = new List<int> { -1, 0, 0 };
            List<Curve> curves = new List<Curve>();

            // Graine en entrée : le même dessin à chaque calcul, et un autre en changeant S.
            // Sans graine, chaque recalcul de la définition redessinait des dunes différentes.
            Random rand = new Random(seed);
            double tolerance = DocumentTolerance();
            double angle = DocumentAngleTolerance();

            // Une grille à la fois : une courbe ne saute jamais vers la grille d'un autre rectangle.
            var groupes = Enumerable.Range(0, points.PathCount).GroupBy(b => points.Paths[b][0]);

            foreach (var groupe in groupes)
            {
                List<int> branches = groupe.ToList();

                for (int i = 1; i < branches.Count; i++)
                {
                    var courante = points.Branches[branches[i]];
                    List<Curve> lines = new List<Curve>();
                    List<Point3d> pointList = new List<Point3d>();
                    for (int j = 0; j < courante.Count; j++)
                    {
                        int offset = ids[rand.Next(0, ids.Count)];
                        var branche = points.Branches[branches[i + offset]];
                        // Rangée précédente plus courte, ou point vide : on reste sur la rangée.
                        GH_Point pt = j < branche.Count && branche[j] != null ? branche[j] : courante[j];
                        if (pt == null) continue;
                        pointList.Add(pt.Value);
                    }

                    for (int j = 0; j < pointList.Count - 1; j++)
                    {
                        lines.Add(new LineCurve( pointList[j], pointList[j + 1]));
                    }
                    if (lines.Count == 0) continue;

                    Curve[] joined = PolylineCurve.JoinCurves(lines);
                    if (joined == null || joined.Length == 0) continue;

                    Curve fillet = Curve.CreateFilletCornersCurve(joined[0], radius, tolerance, angle);
                    if (fillet != null) curves.Add(fillet);
                }
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