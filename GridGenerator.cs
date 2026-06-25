using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cuttlefish
{
    internal class GridGenerator
    {
        public List<List<Point3d>> GenerateGrid(Rectangle3d rectIn, double echXIn, double echYIn, double echZIn, double ecartXIn, double ecartYIn, List<double> randJoin)
        {
            List<List<Point3d>> points = new List<List<Point3d>>();

            int divisionX = (int)(rectIn.Width / ecartXIn);
            int divisionY = (int)(rectIn.Height / ecartYIn);
            if (divisionX % 2 != 0)
            {
                divisionX++;
            }
            if (divisionY % 2 != 0)
            {
                divisionY++;
            }
            int manque = Math.Max(divisionX, divisionY) - randJoin.Count;
            manque += 5;

            for (int i = 0; i < manque; i++)
            {
                randJoin.Add(randJoin[i]);
            }

            points = CreateGrid(points, rectIn, ecartXIn, ecartYIn, divisionX, divisionY);
            points = AlignEdge(points, rectIn, randJoin, echXIn, echYIn, echZIn, divisionX, divisionY, ecartYIn);
            points = BlurPoint(points, echXIn, echYIn, echZIn, divisionX, divisionY);

            return points;
        }
        public List<List<Point3d>> CreateGrid(List<List<Point3d>> points, Rectangle3d rectIn, double ecartXIn, double ecartYIn, int divisionX, int divisionY)
        {

            for (int i = 0; i < divisionX + 1; i++)
            {
                points.Add(new List<Point3d>());
                for (int j = 0; j < divisionY + 1; j++)
                {
                    points[i].Add(new Point3d((((rectIn.Width / divisionX) * i) + rectIn.Corner(0).X), (((rectIn.Height / divisionY) * j) + rectIn.Corner(0).Y), 0));
                }
            }
            return points;
        }

        public List<List<Point3d>> AlignEdge(List<List<Point3d>> points, Rectangle3d rectIn, List<double> randJoin, double echXIn, double echYIn, double echZIn, int divisionX, int divisionY, double ecartYIn)
        {
            //Modifie la première ligne et la première colonne avec des valeurs fixes
            for (int i = 0; i <= divisionX; i++)
            {
                points[i][0] = new Point3d(
                    points[i][0].X + ((randJoin[i] - 0.5) * echXIn),
                    points[i][0].Y + ((randJoin[i + 1] - 0.5) * echYIn),
                    points[i][0].Z + ((randJoin[i + 2] + 0.1) * echZIn)
                        );
            }
            for (int j = 1; j <= divisionY; j++)
            {

                points[0][j] = new Point3d(
                    points[0][j].X + ((randJoin[j - 1] - 0.5) * echXIn),
                    points[0][j].Y + ((randJoin[j] - 0.5) * echYIn),
                    points[0][j].Z + ((randJoin[j + 1] + 0.1) * echZIn)
                    );
            }
            //Copie la première ligne sur la dernière
            for (int i = 0; i <= divisionX; i++)
            {
                points[i][divisionY] = new Point3d(points[i][0].X, points[i][0].Y + rectIn.Height, points[i][0].Z);
                points[i][divisionY - 1] = new Point3d(points[i][1].X, (points[i][1].Y + (rectIn.Height - 2 * ecartYIn)), points[i][1].Z);
            }

            //Copie le premier point sur le dernier de chaque ligne
            for (int j = 0; j <= divisionY; j++)
            {
                points[divisionX][j] = new Point3d(points[0][j].X + rectIn.Width, points[0][j].Y, points[0][j].Z);
            }
            return points;
        }
        public List<List<Point3d>> BlurPoint(List<List<Point3d>> points, double echXIn, double echYIn, double echZIn, int divisionX, int divisionY)
        {
            Random rand = new Random();
            for (int i = 0; i <= divisionX; i++)
            {
                if (i != 0 && i != divisionX)
                {
                    for (int k = 1; k < divisionY; k++)
                    {
                        points[i][k] = new Point3d(
                            points[i][k].X + ((rand.NextDouble() - 0.5) * echXIn),
                            points[i][k].Y + ((rand.NextDouble() - 0.5) * echYIn),
                            points[i][k].Z + ((rand.NextDouble() - 0.5) * echZIn)
                            );
                    }
                }
            }
            return points;
        }
    }
}
