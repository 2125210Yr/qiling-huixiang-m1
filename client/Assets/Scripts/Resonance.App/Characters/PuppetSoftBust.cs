using System.Collections.Generic;
using UnityEngine;

namespace Resonance.App
{
    /// <summary>
    /// Two independent 2D shape-matching globes, pinned to the torso still.
    /// </summary>
    public sealed class PuppetSoftBust
    {
        sealed class Cluster
        {
            public int[] Index;
            public double[] RelX, RelY, Mass, Pin;
            public double[] Px, Py, PrevX, PrevY;
            public Vector3 RestCom;
        }

        Cluster _left, _right;
        float _canvasW, _canvasH;
        bool _ready;

        public bool Bind(Vector3[] rest, Vector2[] chest, Vector2[] outer, float canvasW, float canvasH)
        {
            _ready = false;
            _left = _right = null;
            if (rest == null || chest == null || outer == null) return false;
            _canvasW = canvasW;
            _canvasH = canvasH;
            var left = new List<int>();
            var right = new List<int>();
            for (int i = 0; i < rest.Length; i++)
            {
                var u = rest[i].x / Mathf.Max(0.001f, canvasW) + 0.5f;
                var v = rest[i].y / Mathf.Max(0.001f, canvasH);
                if (v < 0.655f || v > 0.782f || u < 0.408f || u > 0.702f) continue;
                if (u < 0.515f) left.Add(i);
                else right.Add(i);
            }
            _left = Make(rest, left);
            _right = Make(rest, right);
            _ready = _left != null || _right != null;
            return _ready;
        }

        Cluster Make(Vector3[] rest, List<int> ids)
        {
            if (ids == null || ids.Count < 8) return null;
            var n = ids.Count;
            var c = new Cluster();
            c.Index = ids.ToArray();
            c.RelX = new double[n];
            c.RelY = new double[n];
            c.Mass = new double[n];
            c.Pin = new double[n];
            c.Px = new double[n];
            c.Py = new double[n];
            c.PrevX = new double[n];
            c.PrevY = new double[n];
            double sx = 0, sy = 0, m = 0;
            for (int k = 0; k < n; k++)
            {
                var i = c.Index[k];
                var u = rest[i].x / Mathf.Max(0.001f, _canvasW) + 0.5f;
                var v = rest[i].y / Mathf.Max(0.001f, _canvasH);
                var pin = 0f;
                pin = Mathf.Max(pin, 1f - Mathf.SmoothStep(0.408f, 0.448f, u));
                pin = Mathf.Max(pin, Mathf.SmoothStep(0.662f, 0.702f, u));
                pin = Mathf.Max(pin, 1f - Mathf.SmoothStep(0.655f, 0.688f, v));
                pin = Mathf.Max(pin, Mathf.SmoothStep(0.752f, 0.782f, v));
                if (u > 0.486f && u < 0.544f)
                    pin = Mathf.Max(pin, Mathf.Min(Mathf.SmoothStep(0.486f, 0.500f, u), 1f - Mathf.SmoothStep(0.530f, 0.544f, u)));
                c.Pin[k] = pin;
                var w = 1f - pin;
                if (w < 0.05f) w = 0.05f;
                c.Mass[k] = w;
                sx += rest[i].x * w;
                sy += rest[i].y * w;
                m += w;
            }
            if (m < 1e-6) return null;
            c.RestCom = new Vector3((float)(sx / m), (float)(sy / m), 0f);
            for (int k = 0; k < n; k++)
            {
                var i = c.Index[k];
                c.RelX[k] = rest[i].x - c.RestCom.x;
                c.RelY[k] = rest[i].y - c.RestCom.y;
                c.Px[k] = rest[i].x;
                c.Py[k] = rest[i].y;
                c.PrevX[k] = rest[i].x;
                c.PrevY[k] = rest[i].y;
            }
            return c;
        }

        public void Step(Vector3[] verts, float chestL, float chestR, float dt)
        {
            if (!_ready || verts == null) return;
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            var pending = Mathf.Min(dt, 0.08f);
            const float tick = 1f / 120f;
            while (pending > 1e-5f)
            {
                var step = Mathf.Min(tick, pending);
                pending -= step;
                Integrate(_left, verts, chestL, step);
                Integrate(_right, verts, chestR, step);
            }
        }

        void Integrate(Cluster c, Vector3[] verts, float spring, float dt)
        {
            if (c == null) return;
            var n = c.Index.Length;
            double sx = 0, sy = 0, m = 0;
            for (int k = 0; k < n; k++)
            {
                var g = verts[c.Index[k]];
                var w = c.Mass[k];
                sx += g.x * w;
                sy += g.y * w;
                m += w;
            }
            if (m < 1e-6) return;
            var drivenX = sx / m;
            var drivenY = sy / m;
            var goalX = drivenX + spring * _canvasW * 0.95;
            var goalY = drivenY + spring * _canvasH * 0.35;
            var squash = 1.0 - System.Math.Min(0.08, System.Math.Abs(spring) * 10.0);
            PuppetSoftMath.Verlet(c.Px, c.Py, c.PrevX, c.PrevY, n, 0.93, 0, 0, dt);
            var stiff = 1.0 - System.Math.Exp(-12.0 * dt);
            PuppetSoftMath.Match(c.Px, c.Py, c.RelX, c.RelY, c.Mass, n, goalX, goalY, squash, stiff);
            for (int k = 0; k < n; k++)
            {
                var g = verts[c.Index[k]];
                var pin = c.Pin[k];
                var x = c.Px[k] + (g.x - c.Px[k]) * pin;
                var y = c.Py[k] + (g.y - c.Py[k]) * pin;
                c.Px[k] = x;
                c.Py[k] = y;
                verts[c.Index[k]] = new Vector3((float)x, (float)y, g.z);
            }
        }
    }
}
