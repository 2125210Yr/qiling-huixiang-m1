using System;

namespace Resonance.App
{
    /// <summary>2D shape-matching cluster. No renderer types.</summary>
    public static class PuppetSoftMath
    {
        public static void Polar2(double axx, double axy, double ayx, double ayy, out double c, out double s)
        {
            var a = axx + ayy;
            var b = ayx - axy;
            var d = Math.Sqrt(a * a + b * b);
            if (d < 1e-12) { c = 1; s = 0; return; }
            c = a / d;
            s = b / d;
        }

        public static void Com(double[] px, double[] py, double[] mass, int n, out double cx, out double cy)
        {
            double sx = 0, sy = 0, m = 0;
            for (int i = 0; i < n; i++)
            {
                var w = mass[i];
                sx += px[i] * w;
                sy += py[i] * w;
                m += w;
            }
            if (m < 1e-12) { cx = 0; cy = 0; return; }
            cx = sx / m;
            cy = sy / m;
        }

        public static void Match(double[] px, double[] py, double[] relX, double[] relY, double[] mass, int n,
            double goalX, double goalY, double squashY, double stiffness)
        {
            if (n <= 0 || stiffness <= 0) return;
            double axx = 0, axy = 0, ayx = 0, ayy = 0, msum = 0;
            double cx, cy;
            Com(px, py, mass, n, out cx, out cy);
            for (int i = 0; i < n; i++)
            {
                var w = mass[i];
                axx += w * (px[i] - cx) * relX[i];
                axy += w * (px[i] - cx) * relY[i];
                ayx += w * (py[i] - cy) * relX[i];
                ayy += w * (py[i] - cy) * relY[i];
                msum += w;
            }
            if (msum < 1e-12) return;
            double c, s;
            Polar2(axx, axy, ayx, ayy, out c, out s);
            var q = squashY < 0.01 ? 1 : squashY;
            var k = stiffness > 1 ? 1 : stiffness;
            for (int i = 0; i < n; i++)
            {
                var gx = goalX + c * relX[i] - s * relY[i] * q;
                var gy = goalY + s * relX[i] + c * relY[i] * q;
                px[i] += (gx - px[i]) * k;
                py[i] += (gy - py[i]) * k;
            }
        }

        public static void Verlet(double[] px, double[] py, double[] prevX, double[] prevY, int n, double damp, double ax, double ay, double dt)
        {
            if (dt <= 0) return;
            var dt2 = dt * dt;
            for (int i = 0; i < n; i++)
            {
                var x = px[i];
                var y = py[i];
                px[i] = x + (x - prevX[i]) * damp + ax * dt2;
                py[i] = y + (y - prevY[i]) * damp + ay * dt2;
                prevX[i] = x;
                prevY[i] = y;
            }
        }
    }
}
