# Génère le jeu d'icônes Cuttlefish (PNG 24x24, fond transparent).
# Usage : powershell -File icones.ps1 [-Out <dossier>] [-Preview <fichier.png>]
# Couleur d'accent par sous-catégorie : Points = bleu, Transform = orange, Draw = sarcelle.
param(
    [string]$Out = $PSScriptRoot,
    [string]$Preview = ""
)

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class CuttlefishIcons
{
    static readonly Color Ink       = Color.FromArgb(45, 45, 48);
    static readonly Color Muted     = Color.FromArgb(160, 160, 165);
    static readonly Color Points    = Color.FromArgb(33, 118, 214);
    static readonly Color Transform = Color.FromArgb(232, 118, 36);
    static readonly Color Draw      = Color.FromArgb(20, 150, 138);

    public static Dictionary<string, Bitmap> All()
    {
        var d = new Dictionary<string, Bitmap>();
        d["GridCreator"]  = Make(CreateGrid);
        d["CircularGrid"] = Make(CircularGrid);
        d["Attract"]      = Make(Attractor);
        d["Blur"]         = Make(Blur);
        d["Voronoi"]      = Make(Voronoi);
        d["Worm"]         = Make(Worm);
        d["Wander"]       = Make(Wander);
        d["Dune"]         = Make(Dune);
        d["Lining"]       = Make(Lining);
        d["Meshing"]      = Make(Meshing);
        d["Icone"]        = Make(Plugin);
        return d;
    }

    static Bitmap Make(Action<Graphics> draw)
    {
        var bmp = new Bitmap(24, 24, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            draw(g);
        }
        return bmp;
    }

    // ---------- primitives ----------
    static Pen P(Color c, float w)
    {
        var p = new Pen(c, w);
        p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
        return p;
    }
    static void Dot(Graphics g, Color c, float x, float y, float r = 1.4f)
    {
        using (var b = new SolidBrush(c)) g.FillEllipse(b, x - r, y - r, 2 * r, 2 * r);
    }
    static void Lines(Graphics g, Pen p, params float[] xy)
    {
        var pts = new PointF[xy.Length / 2];
        for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(xy[2 * i], xy[2 * i + 1]);
        g.DrawLines(p, pts);
    }
    static void Curve(Graphics g, Pen p, float tension, params float[] xy)
    {
        var pts = new PointF[xy.Length / 2];
        for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(xy[2 * i], xy[2 * i + 1]);
        g.DrawCurve(p, pts, tension);
    }

    // Cellules de Voronoi partagées par Voronoi et Worm.
    static void VoronoiEdges(Graphics g, Pen p)
    {
        g.DrawRectangle(p, 2.5f, 2.5f, 19f, 19f);
        Lines(g, p, 2.5f, 9f, 8f, 10f, 11f, 2.5f);
        Lines(g, p, 8f, 10f, 10f, 16f, 2.5f, 18f);
        Lines(g, p, 10f, 16f, 15f, 14f, 21.5f, 17f);
        Lines(g, p, 15f, 14f, 14.5f, 8f, 21.5f, 6f);
        Lines(g, p, 14.5f, 8f, 8f, 10f);
        Lines(g, p, 10f, 16f, 12f, 21.5f);
    }

    // ---------- Points ----------
    static void CreateGrid(Graphics g)
    {
        using (var p = P(Ink, 1f)) g.DrawRectangle(p, 3f, 3f, 18f, 18f);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                Dot(g, Points, 3f + 6f * i, 3f + 6f * j, 1.6f);
    }

    static void CircularGrid(Graphics g)
    {
        using (var p = P(Muted, 0.8f))
        {
            g.DrawEllipse(p, 12f - 5f, 12f - 5f, 10f, 10f);
            g.DrawEllipse(p, 12f - 9.5f, 12f - 9.5f, 19f, 19f);
        }
        Dot(g, Points, 12f, 12f, 1.6f);
        for (int k = 0; k < 6; k++)
        {
            double a = Math.PI * 2 * k / 6;
            Dot(g, Points, 12f + 5f * (float)Math.Cos(a), 12f + 5f * (float)Math.Sin(a));
        }
        for (int k = 0; k < 12; k++)
        {
            double a = Math.PI * 2 * k / 12;
            Dot(g, Points, 12f + 9.5f * (float)Math.Cos(a), 12f + 9.5f * (float)Math.Sin(a));
        }
    }

    // ---------- Transform ----------
    static void Attractor(Graphics g)
    {
        using (var p = P(Ink, 1.6f)) Curve(g, p, 0.6f, 19f, 2f, 21f, 8f, 18f, 15f, 20f, 22f);
        // Colonnes de plus en plus serrées vers la courbe, points grossissant à l'approche.
        float[] xs = { 2.5f, 8.5f, 13f, 16f };
        float[] rs = { 1.1f, 1.3f, 1.5f, 1.7f };
        float[] ys = { 4f, 9.5f, 15f, 20.5f };
        foreach (float y in ys)
            for (int i = 0; i < xs.Length; i++)
                Dot(g, Transform, xs[i], y, rs[i]);
    }

    static void Blur(Graphics g)
    {
        var rnd = new Random(7);
        using (var p = P(Muted, 0.8f))
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    float x = 3.5f + 5.6f * i, y = 3.5f + 5.6f * j;
                    float dx = (float)(rnd.NextDouble() * 4 - 2), dy = (float)(rnd.NextDouble() * 4 - 2);
                    Dot(g, Muted, x, y, 0.9f);
                    g.DrawLine(p, x, y, x + dx, y + dy);
                    Dot(g, Transform, x + dx, y + dy);
                }
    }

    // ---------- Draw ----------
    static void Voronoi(Graphics g)
    {
        using (var p = P(Ink, 1f)) VoronoiEdges(g, p);
        float[] sites = { 6f, 6f, 5f, 13.5f, 18f, 4.5f, 18.5f, 11f, 16.5f, 18.5f, 6f, 20f, 11.5f, 12f };
        for (int i = 0; i < sites.Length; i += 2) Dot(g, Draw, sites[i], sites[i + 1], 1.3f);
    }

    static void Worm(Graphics g)
    {
        using (var p = P(Muted, 0.8f)) VoronoiEdges(g, p);
        // Un ver qui traverse les cellules par le milieu de leurs arêtes.
        using (var p = P(Draw, 2.2f))
            Curve(g, p, 0.8f, 4.5f, 19.5f, 10.5f, 18f, 8.5f, 11.5f, 14f, 7.5f, 18.5f, 10.5f, 18f, 5f);
        Dot(g, Draw, 18f, 5f, 2f);
    }

    static void Wander(Graphics g)
    {
        float[] ys = { 5f, 12f, 19f };
        foreach (float y in ys)
            for (int i = 0; i < 5; i++) Dot(g, Muted, 3f + 4.5f * i, y, 1.1f);
        using (var p = P(Draw, 2f))
            Curve(g, p, 0.35f, 3f, 12f, 7.5f, 5f, 12f, 12f, 16.5f, 19f, 21f, 12f);
    }

    static void Dune(Graphics g)
    {
        using (var p = P(Draw, 1.6f))
            for (int k = 0; k < 3; k++)
            {
                var pts = new List<PointF>();
                for (float x = 2.5f; x <= 21.6f; x += 0.5f)
                    pts.Add(new PointF(x, 6f + 6f * k + 2.2f * (float)Math.Sin((x + 3f * k) * 0.55f)));
                g.DrawLines(p, pts.ToArray());
            }
    }

    static void Lining(Graphics g)
    {
        float[] off = { 0f, 1.2f, -1f, 0.6f };
        using (var p = P(Draw, 1.4f))
            for (int i = 0; i < 4; i++)
            {
                float x = 4f + 5.3f * i;
                Lines(g, p, x, 2.5f, x + off[i], 9f, x - off[i], 15f, x + off[(i + 1) % 4], 21.5f);
            }
        for (int i = 0; i < 4; i++)
        {
            float x = 4f + 5.3f * i;
            Dot(g, Ink, x + off[i], 9f, 1.2f);
            Dot(g, Ink, x - off[i], 15f, 1.2f);
        }
    }

    static void Meshing(Graphics g)
    {
        using (var p = P(Draw, 1.2f))
        {
            for (int k = -3; k <= 3; k++)
            {
                float s = 7f * k;
                g.DrawLine(p, 2.5f + s, 2.5f, 21.5f + s, 21.5f);
                g.DrawLine(p, 21.5f - s, 2.5f, 2.5f - s, 21.5f);
            }
        }
        // Effacer ce qui dépasse le carré de 24 px n'est pas nécessaire : le bitmap le coupe.
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                if ((i + j) % 2 == 0) Dot(g, Ink, 2.5f + 6.33f * i, 2.5f + 6.33f * j, 1.1f);
    }

    // ---------- Plugin ----------
    static void Plugin(Graphics g)
    {
        var body = Color.FromArgb(33, 110, 200);
        var fin  = Color.FromArgb(120, 175, 235);
        using (var b = new SolidBrush(fin)) g.FillEllipse(b, 4.5f, 1.5f, 15f, 16f);
        using (var b = new SolidBrush(body)) g.FillEllipse(b, 7f, 2f, 10f, 15f);
        using (var p = P(body, 1.5f))
        {
            Curve(g, p, 0.6f, 9f, 15f, 7.5f, 19f, 5f, 22f);
            Curve(g, p, 0.6f, 11f, 16f, 10.5f, 20f, 9.5f, 22.5f);
            Curve(g, p, 0.6f, 13f, 16f, 13.5f, 20f, 14.5f, 22.5f);
            Curve(g, p, 0.6f, 15f, 15f, 16.5f, 19f, 19f, 22f);
        }
        Dot(g, Color.White, 9.8f, 12.5f, 1.5f);
        Dot(g, Color.White, 14.2f, 12.5f, 1.5f);
        Dot(g, Ink, 9.8f, 12.8f, 0.7f);
        Dot(g, Ink, 14.2f, 12.8f, 0.7f);
    }

    // Planche : chaque icône en 4x (pixels nets) puis en taille réelle sur fond clair et sombre.
    public static void Sheet(Dictionary<string, Bitmap> icons, string path)
    {
        int cell = 112, n = icons.Count;
        using (var sheet = new Bitmap(cell * n, 160))
        using (var g = Graphics.FromImage(sheet))
        using (var font = new Font("Segoe UI", 8f))
        {
            g.Clear(Color.FromArgb(212, 208, 200));
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            int x = 0;
            foreach (var kv in icons)
            {
                using (var w = new SolidBrush(Color.White)) g.FillRectangle(w, x + 8, 4, 96, 96);
                g.DrawImage(kv.Value, x + 8, 4, 96, 96);
                g.DrawImage(kv.Value, x + 20, 108, 24, 24);
                using (var dk = new SolidBrush(Color.FromArgb(40, 40, 40))) g.FillRectangle(dk, x + 60, 104, 32, 32);
                g.DrawImage(kv.Value, x + 64, 108, 24, 24);
                g.DrawString(kv.Key, font, Brushes.Black, x + 8, 140);
                x += cell;
            }
            sheet.Save(path, ImageFormat.Png);
        }
    }
}
'@

$icons = [CuttlefishIcons]::All()
foreach ($k in $icons.Keys) {
    $icons[$k].Save((Join-Path $Out "$k.png"), [System.Drawing.Imaging.ImageFormat]::Png)
}
if ($Preview) { [CuttlefishIcons]::Sheet($icons, $Preview) }
"$($icons.Count) icons -> $Out"
