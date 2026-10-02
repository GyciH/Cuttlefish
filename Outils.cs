using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace Cuttlefish
{
    /// <summary>
    /// Fonctions partagées par les composants de dessin.
    /// </summary>
    internal static class Outils
    {
        /// <summary>
        /// Regroupe les branches d'un arbre de points par le premier indice de leur chemin
        /// (une grille par rectangle ou par cercle d'entrée, comme Voronoi), dans l'ordre de l'arbre.
        /// Chaque groupe est une liste de rangées, chaque rangée une liste de points.
        /// </summary>
        public static List<List<List<Point3d>>> Groupes(GH_Structure<GH_Point> arbre)
        {
            var groupes = new List<List<List<Point3d>>>();
            var index = new Dictionary<int, int>();
            for (int b = 0; b < arbre.PathCount; b++)
            {
                int cle = arbre.Paths[b].Length > 0 ? arbre.Paths[b][0] : 0;
                if (!index.TryGetValue(cle, out int g))
                {
                    g = groupes.Count;
                    index[cle] = g;
                    groupes.Add(new List<List<Point3d>>());
                }
                groupes[g].Add(arbre.Branches[b].Where(p => p != null).Select(p => p.Value).ToList());
            }
            return groupes;
        }

        public static Point3d Milieu(Point3d a, Point3d b)
        {
            return new Point3d((a.X + b.X) / 2, (a.Y + b.Y) / 2, (a.Z + b.Z) / 2);
        }
    }
}
