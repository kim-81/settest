using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WelcomeKorea
{
    public static class ReadingText
    {
        public static string Sentences(string text)
        {
            if(String.IsNullOrEmpty(text))return text;
            return Regex.Replace(text,@"(?<=[.!?。！？])[ \t]+",delegate(Match match)
            {
                int end=match.Index;char mark=text[end-1];
                if(mark=='.')
                {
                    if(end>=2 && Char.IsDigit(text[end-2]))return match.Value;
                    string before=text.Substring(0,end);
                    if(Regex.IsMatch(before,@"(?:\b(?:Mr|Mrs|Ms|Dr|St|vs|etc)|e\.g|i\.e|U\.S)\.$",RegexOptions.IgnoreCase))return match.Value;
                }
                return "\n";
            });
        }
    }
    public static class KoreanScenery
    {
        public static Color Accent(string page)
        {
            switch(page){case "Food":return Color.FromArgb(157,84,58);case "Transport":return Color.FromArgb(55,91,132);case "Essentials":return Color.FromArgb(103,88,135);case "Travel Help":return Color.FromArgb(149,85,107);case "Saved places":return Color.FromArgb(148,111,49);default:return Color.FromArgb(38,105,94);}
        }
        public static void Draw(Graphics g,Rectangle bounds,string page,bool dark)
        {
            if(bounds.Width<1 || bounds.Height<1)return;
            GraphicsState saved=g.Save();g.SetClip(bounds);g.SmoothingMode=SmoothingMode.AntiAlias;
            Color accent=Accent(page);
            using(var sky=new LinearGradientBrush(bounds,dark?Color.FromArgb(21,49,55):Color.FromArgb(250,248,240),dark?Color.FromArgb(40,76,77):Color.FromArgb(231,239,232),90))g.FillRectangle(sky,bounds);
            float x=bounds.X,y=bounds.Y,w=bounds.Width,h=bounds.Height;
            using(var moon=new SolidBrush(dark?Color.FromArgb(240,203,143):Color.FromArgb(235,202,145)))g.FillEllipse(moon,x+w*.79f,y+h*.12f,h*.27f,h*.27f);
            for(int layer=0;layer<3;layer++)
            {
                float baseline=y+h*(.60f+layer*.14f);
                PointF[] points={new PointF(x+w*.42f,y+h),new PointF(x+w*.42f,baseline+h*.12f),new PointF(x+w*.55f,baseline-h*.15f),new PointF(x+w*.66f,baseline+h*.07f),new PointF(x+w*.77f,baseline-h*.23f),new PointF(x+w*.90f,baseline),new PointF(x+w,baseline-h*.17f),new PointF(x+w,y+h)};
                using(var hill=new SolidBrush(Color.FromArgb(dark?55+layer*25:22+layer*17,accent)))g.FillClosedCurve(hill,points,FillMode.Winding,.12f);
            }
            // Curved tiled roof and wooden pillars, drawn in the quiet right-hand area.
            float hx=x+w*.78f,hy=y+h*.69f,s=Math.Min(w*.16f,h*.8f);
            using(var roof=new Pen(dark?Color.FromArgb(188,214,201):Color.FromArgb(110,accent),Math.Max(1,s*.025f)))
            {
                g.DrawBezier(roof,hx-s*.55f,hy-s*.06f,hx-s*.22f,hy+s*.07f,hx-s*.13f,hy-s*.20f,hx,hy-s*.23f);
                g.DrawBezier(roof,hx,hy-s*.23f,hx+s*.13f,hy-s*.20f,hx+s*.22f,hy+s*.07f,hx+s*.55f,hy-s*.06f);
                g.DrawLine(roof,hx-s*.38f,hy+s*.02f,hx+s*.38f,hy+s*.02f);
                g.DrawLine(roof,hx-s*.29f,hy+s*.02f,hx-s*.29f,hy+s*.30f);g.DrawLine(roof,hx+s*.29f,hy+s*.02f,hx+s*.29f,hy+s*.30f);
                g.DrawLine(roof,hx,hy+s*.02f,hx,hy+s*.30f);g.DrawLine(roof,hx-s*.38f,hy+s*.30f,hx+s*.38f,hy+s*.30f);
            }
            using(var branch=new Pen(dark?Color.FromArgb(100,180,198,173):Color.FromArgb(75,accent),1.5f))
            {
                g.DrawBezier(branch,x+w*.96f,y+h*.22f,x+w*.92f,y+h*.20f,x+w*.94f,y+h*.45f,x+w*.89f,y+h*.49f);
                for(int i=0;i<4;i++)using(var blossom=new SolidBrush(dark?Color.FromArgb(200,227,185,176):Color.FromArgb(160,221,177,167)))g.FillEllipse(blossom,x+w*(.915f+i*.008f),y+h*(.23f+i*.055f),5,5);
            }
            g.Restore(saved);
        }
    }
    public sealed class ScenicHeader : Panel
    {
        public string Page="Home";
        public ScenicHeader(){DoubleBuffered=true;}
        protected override void OnPaintBackground(PaintEventArgs e){KoreanScenery.Draw(e.Graphics,ClientRectangle,Page,false);}
    }
    public sealed class ScenicSurface : FlowLayoutPanel
    {
        public string Page="Home";
        public ScenicSurface(){DoubleBuffered=true;}
        protected override void OnPaintBackground(PaintEventArgs e){KoreanScenery.Draw(e.Graphics,ClientRectangle,Page,false);}
    }
}
