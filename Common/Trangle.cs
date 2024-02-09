using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MazeGame.Common
{
    public  class Trangle
    {
        private static (Vector2, Vector2, Vector2) SortVerticesAscendingByY(Vector2 vt1, Vector2 vt2, Vector2 vt3)
        {
            Vector2 vTmp;

            if (vt1.Y > vt2.Y)
            {
                vTmp = vt1;
                vt1 = vt2;
                vt2 = vTmp;
            }
            /* here v1.y <= v2.y */
            if (vt1.Y > vt3.Y)
            {
                vTmp = vt1;
                vt1 = vt3;
                vt3 = vTmp;
            }
            /* here v1.y <= v2.y and v1.y <= v3.y so test v2 vs. v3 */
            if (vt2.Y > vt3.Y)
            {
                vTmp = vt2;
                vt2 = vt3;
                vt3 = vTmp;
            }

            return (vt1, vt2, vt3);
        }

        private static (Vector2, Vector2, Vector2) SortVerticesAscendingByX(Vector2 vt1, Vector2 vt2, Vector2 vt3)
        {
            Vector2 vTmp;

            if (vt1.X > vt2.X)
            {
                vTmp = vt1;
                vt1 = vt2;
                vt2 = vTmp;
            }
            if (vt1.X > vt3.X)
            {
                vTmp = vt1;
                vt1 = vt3;
                vt3 = vTmp;
            }
            if (vt2.X > vt3.X)
            {
                vTmp = vt2;
                vt2 = vt3;
                vt3 = vTmp;
            }

            return (vt1, vt2, vt3);
        }


        private static void FillBottomFlatTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Action<int, int, int> drawScanline)
        {
            float invslope1 = (v2.X - v1.X) / (v2.Y - v1.Y);
            float invslope2 = (v3.X - v1.X) / (v3.Y - v1.Y);

            float curx1 = v1.X;
            float curx2 = v1.X;

            for (int scanlineY = (int)v1.Y; scanlineY <= (int)MathF.Ceiling(v2.Y); scanlineY++)
            {
                drawScanline((int)curx1, (int)MathF.Ceiling(curx2), scanlineY);
                curx1 += invslope1;
                curx2 += invslope2;
            }
        }

        private static void FillTopFlatTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Action<int, int, int> drawScanline)
        {
            float invslope1 = (v3.X - v1.X) / (v3.Y - v1.Y);
            float invslope2 = (v3.X - v2.X) / (v3.Y - v2.Y);

            float curx1 = v3.X;
            float curx2 = v3.X;

            for (int scanlineY = (int)MathF.Ceiling(v3.Y); scanlineY > (int)v1.Y; scanlineY--)
            {
                drawScanline((int)MathF.Ceiling(curx1), (int)curx2, scanlineY);
                curx1 -= invslope1;
                curx2 -= invslope2;
            }
        }

        public static void DrawTriangle(Vector2 v1, Vector2 v2, Vector2 v3,Action<int,int,int> drawScanline)
        {
            /* at first sort the three vertices by y-coordinate ascending so v1 is the topmost vertice */
            (v1,v2,v3) = SortVerticesAscendingByY(v1,v2,v3);

            /* here we know that v1.y <= v2.y <= v3.y */
            /* check for trivial case of bottom-flat triangle */
            if (Math.Abs(v2.Y - v3.Y) < Constants.TOLERANCE)
            {
                FillBottomFlatTriangle(v1, v2, v3, drawScanline);
            }
            /* check for trivial case of top-flat triangle */
            else if (Math.Abs(v1.Y - v2.Y) < Constants.TOLERANCE)
            {
                FillTopFlatTriangle(v1, v2, v3, drawScanline);
            }
            else
            {
                Vector2 v4 = v2 with { X = v1.X + ((v2.Y - v1.Y) / (v3.Y - v1.Y)) * (v3.X - v1.X) };
                FillBottomFlatTriangle( v1, v2, v4, drawScanline);
                FillTopFlatTriangle( v2, v4, v3, drawScanline);
            }
        }

    }
}
