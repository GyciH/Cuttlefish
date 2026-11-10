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

        /// <summary>
        /// Reporte sur l'avant-dernier point de chaque rangÃ©e (sauf la derniÃ¨re) l'Ã©cart des deux
        /// premiers : la courbe finit comme elle commence. Travaille sur les listes de Groupes,
        /// des copies : les points d'entrÃ©e du composant ne sont pas touchÃ©s.
        /// </summary>
        public static void AjusterFinRangees(List<List<Point3d>> rows)
        {
            for (int i = 0; i < rows.Count - 1; i++)
            {
                List<Point3d> r = rows[i];
                if (r.Count < 2) continue;
                r[r.Count - 2] = r[r.Count - 1] - (r[1] - r[0]);
            }
        }

        /// <summary>
        /// Direction gÃ©nÃ©rale des rangÃ©es d'un groupe, utilisÃ©e comme tangente aux extrÃ©mitÃ©s :
        /// elle suit une grille tournÃ©e. Axe Y si les rangÃ©es n'ont pas de direction.
        /// </summary>
        public static Vector3d DirectionRangees(List<List<Point3d>> rows)
        {
            Vector3d somme = Vector3d.Zero;
            foreach (List<Point3d> r in rows)
                if (r.Count >= 2) somme += r[r.Count - 1] - r[0];
            return somme.Unitize() ? somme : Vector3d.YAxis;
        }

        public static Point3d Milieu(Point3d a, Point3d b)
        {
            return new Point3d((a.X + b.X) / 2, (a.Y + b.Y) / 2, (a.Z + b.Z) / 2);
        }
    }
}
