using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WelcomeKorea
{
    public static class Symbols
    {
        public static string ForPage(string page)
        {
            switch (page)
            {
                case "Maps": return "pin";
                case "Food": return "food";
                case "Transport": return "transit";
                case "Essentials": return "card";
                case "Travel Help": return "chat";
                case "Saved places": return "bookmark";
                default: return "home";
            }
        }
        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath(); float d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90); path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); path.AddArc(r.X, r.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
        }
        public static Bitmap Bitmap(string kind, int size, Color color)
        {
            Bitmap bitmap = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bitmap)) { g.Clear(Color.Transparent); Draw(g, new RectangleF(1, 1, size - 2, size - 2), kind, color); }
            return bitmap;
        }
        public static Bitmap ProviderBitmap(string provider,int size)
        {
            Bitmap bitmap=new Bitmap(size,size);
            using(Graphics g=Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;
                if(provider=="naver")
                {
                    using(Brush green=new SolidBrush(Color.FromArgb(3,199,90)))using(GraphicsPath shape=Rounded(new RectangleF(1,1,size-2,size-2),5))g.FillPath(green,shape);
                    using(Font font=new Font("Segoe UI",size*.48f,FontStyle.Bold))TextRenderer.DrawText(g,"N",font,new Rectangle(0,0,size,size),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
                }
                else if(provider=="kakao")
                {
                    using(Brush yellow=new SolidBrush(Color.FromArgb(254,229,0)))using(GraphicsPath shape=Rounded(new RectangleF(1,1,size-2,size-2),5))g.FillPath(yellow,shape);
                    Draw(g,new RectangleF(size*.22f,size*.15f,size*.56f,size*.7f),"pin",Color.FromArgb(42,33,28));
                }
                else
                {
                    float w=size*.66f;RectangleF circle=new RectangleF(size*.17f,size*.17f,w,w);
                    Color[] colors={Color.FromArgb(66,133,244),Color.FromArgb(52,168,83),Color.FromArgb(251,188,5),Color.FromArgb(234,67,53)};
                    float[] starts={-10,45,115,190};float[] sweeps={55,70,75,100};
                    for(int i=0;i<4;i++)using(Pen pen=new Pen(colors[i],size*.12f))g.DrawArc(pen,circle,starts[i],sweeps[i]);
                    using(Pen pen=new Pen(colors[0],size*.12f)){g.DrawLine(pen,size*.51f,size*.5f,size*.83f,size*.5f);g.DrawLine(pen,size*.83f,size*.5f,size*.83f,size*.64f);}
                }
            }
            return bitmap;
        }
        public static void Draw(Graphics g, RectangleF bounds, string kind, Color color)
        {
            GraphicsState state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias; g.TranslateTransform(bounds.Left, bounds.Top); g.ScaleTransform(bounds.Width / 24f, bounds.Height / 24f);
            using (Pen p = new Pen(color, 1.65f))
            {
                p.StartCap = p.EndCap = LineCap.Round; p.LineJoin = LineJoin.Round;
                if (kind == "pin")
                {
                    using (GraphicsPath path = new GraphicsPath()) { path.AddBezier(12,22,2,13,4,5,7,3); path.AddBezier(7,3,20,-2,27,10,12,22); g.DrawPath(p,path); } g.DrawEllipse(p,9,6,6,6);
                }
                else if (kind == "food")
                {
                    g.DrawLine(p,5,2,5,8); g.DrawLine(p,8,2,8,8); g.DrawLine(p,11,2,11,8); g.DrawArc(p,5,5,6,7,0,180); g.DrawLine(p,8,12,8,22);
                    g.DrawLine(p,19,2,19,22); g.DrawBezier(p,19,2,13,5,14,12,19,12);
                }
                else if (kind == "transit")
                {
                    using (GraphicsPath path = Rounded(new RectangleF(4,2,16,17),3)) g.DrawPath(p,path);
                    g.DrawRectangle(p,6,6,12,6); g.DrawLine(p,12,6,12,12); g.DrawLine(p,7,19,5,22); g.DrawLine(p,17,19,19,22); g.DrawLine(p,7,21,17,21);
                    g.DrawEllipse(p,7,15,1,1); g.DrawEllipse(p,16,15,1,1);
                }
                else if (kind == "card")
                {
                    using (GraphicsPath path = Rounded(new RectangleF(2,5,20,14),3)) g.DrawPath(p,path);
                    g.DrawLine(p,2,10,22,10); g.DrawLine(p,6,15,10,15); g.DrawLine(p,16,15,18,15);
                }
                else if (kind == "chat")
                {
                    using (GraphicsPath path = new GraphicsPath()) { path.AddLines(new PointF[] {new PointF(3,4),new PointF(21,4),new PointF(21,17),new PointF(10,17),new PointF(5,22),new PointF(5,17),new PointF(3,17)}); path.CloseFigure(); g.DrawPath(p,path); }
                    g.DrawLine(p,7,9,17,9); g.DrawLine(p,7,13,13,13);
                }
                else if (kind == "bookmark")
                {
                    g.DrawLines(p,new PointF[] {new PointF(6,3),new PointF(18,3),new PointF(18,22),new PointF(12,18),new PointF(6,22),new PointF(6,3)}); g.DrawLine(p,9,8,15,8);
                }
                else if (kind == "walking")
                {
                    g.DrawEllipse(p,11,1,4,4); g.DrawLines(p,new PointF[] {new PointF(4,11),new PointF(9,7),new PointF(13,8),new PointF(17,12),new PointF(21,12)});
                    g.DrawLines(p,new PointF[] {new PointF(12,8),new PointF(10,14),new PointF(16,17),new PointF(17,22)}); g.DrawLine(p,10,14,5,22);
                }
                else if (kind == "bicycling")
                {
                    g.DrawEllipse(p,1,13,8,8); g.DrawEllipse(p,15,13,8,8); g.DrawLines(p,new PointF[] {new PointF(5,17),new PointF(9,8),new PointF(15,17),new PointF(5,17),new PointF(14,10),new PointF(19,17)});
                    g.DrawLine(p,7,8,11,8); g.DrawLines(p,new PointF[] {new PointF(14,10),new PointF(15,5),new PointF(19,5)});
                }
                else if (kind == "taxi")
                {
                    using (GraphicsPath path = Rounded(new RectangleF(2,10,20,10),2)) g.DrawPath(p,path);
                    g.DrawLines(p,new PointF[] {new PointF(5,10),new PointF(8,5),new PointF(16,5),new PointF(19,10)}); g.DrawRectangle(p,10,2,4,3);
                    g.DrawEllipse(p,5,14,2,2); g.DrawEllipse(p,17,14,2,2); g.DrawLine(p,5,20,5,22); g.DrawLine(p,19,20,19,22);
                }
                else if(kind=="soup" || kind=="rice" || kind=="noodles")
                {
                    g.DrawLine(p,3,12,21,12);g.DrawArc(p,3,5,18,16,0,180);g.DrawLine(p,8,21,16,21);
                    if(kind=="noodles"){g.DrawLine(p,9,2,21,7);g.DrawLine(p,8,5,20,10);g.DrawLine(p,13,7,13,13);g.DrawLine(p,17,9,17,14);}
                    else if(kind=="rice"){g.DrawArc(p,6,6,12,12,180,180);g.DrawLine(p,9,8,10,9);g.DrawLine(p,14,8,15,9);}
                    else {g.DrawBezier(p,7,9,10,7,4,5,7,2);g.DrawBezier(p,12,9,15,7,9,5,12,2);g.DrawBezier(p,17,9,20,7,14,5,17,2);}
                }
                else if(kind=="wok")
                {
                    g.DrawLine(p,2,11,19,11);g.DrawArc(p,2,4,17,15,0,180);g.DrawLine(p,18,13,23,7);g.DrawLine(p,5,4,15,8);g.DrawLine(p,9,2,19,6);g.DrawLine(p,8,21,15,21);
                }
                else if(kind=="snacks")
                {
                    g.DrawLine(p,5,21,19,3);for(int i=0;i<3;i++)using(GraphicsPath path=Rounded(new RectangleF(5+i*4,12-i*4,7,4),1))g.DrawPath(p,path);
                }
                else if(kind=="grill")
                {
                    using(GraphicsPath path=Rounded(new RectangleF(3,8,18,11),2))g.DrawPath(p,path);for(int i=6;i<=18;i+=4)g.DrawLine(p,i,9,i,18);g.DrawLine(p,4,13,20,13);g.DrawLine(p,6,19,5,22);g.DrawLine(p,18,19,19,22);g.DrawLine(p,7,2,7,5);g.DrawLine(p,12,1,12,5);g.DrawLine(p,17,2,17,5);
                }
                else if(kind=="fish")
                {
                    g.DrawEllipse(p,2,6,15,12);g.DrawLines(p,new PointF[]{new PointF(16,12),new PointF(22,6),new PointF(22,18),new PointF(16,12)});g.DrawEllipse(p,5,10,1,1);g.DrawArc(p,7,6,6,12,-80,160);
                }
                else if(kind=="pot")
                {
                    using(GraphicsPath path=Rounded(new RectangleF(5,9,14,12),2))g.DrawPath(p,path);g.DrawLine(p,3,9,21,9);g.DrawArc(p,8,3,8,8,180,180);g.DrawLine(p,11,3,13,3);g.DrawLine(p,2,13,5,13);g.DrawLine(p,19,13,22,13);
                }
                else if(kind=="pizza")
                {
                    g.DrawLines(p,new PointF[]{new PointF(3,5),new PointF(20,3),new PointF(12,22),new PointF(3,5)});g.DrawLine(p,5,8,18,6);g.DrawEllipse(p,9,9,3,3);g.DrawEllipse(p,12,14,2,2);
                }
                else if(kind=="coffee")
                {
                    using(GraphicsPath path=Rounded(new RectangleF(4,8,13,12),2))g.DrawPath(p,path);g.DrawArc(p,14,10,8,7,-90,180);g.DrawLine(p,2,22,20,22);g.DrawLine(p,7,3,7,5);g.DrawLine(p,12,2,12,5);
                }
                else if(kind=="leaf")
                {
                    using(GraphicsPath path=new GraphicsPath()){path.AddBezier(4,19,0,7,11,2,22,2);path.AddBezier(22,2,22,16,15,23,4,19);g.DrawPath(p,path);}g.DrawLine(p,2,22,16,8);g.DrawLine(p,8,16,8,10);g.DrawLine(p,12,12,17,12);
                }
                else
                {
                    g.DrawLines(p,new PointF[] {new PointF(2,11),new PointF(12,2),new PointF(22,11)}); g.DrawLines(p,new PointF[] {new PointF(5,9),new PointF(5,21),new PointF(19,21),new PointF(19,9)}); g.DrawRectangle(p,10,14,4,7);
                }
            }
            g.Restore(state);
        }
    }
    public sealed class SymbolBadge : Control
    {
        public string Kind = "home";
        public SymbolBadge() { DoubleBuffered = true; Size = new Size(48,48); SetStyle(ControlStyles.SupportsTransparentBackColor, true); BackColor = Color.Transparent; }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Brush b = new SolidBrush(Color.FromArgb(225,239,233))) e.Graphics.FillEllipse(b,1,1,Width-2,Height-2);
            Symbols.Draw(e.Graphics,new RectangleF(Width*.23f,Height*.23f,Width*.54f,Height*.54f),Kind,Color.FromArgb(18,112,104));
        }
    }
    public sealed class CategoryTile : Button
    {
        public string SymbolKind = "pin";
        bool hover;
        public CategoryTile() { DoubleBuffered = true; FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand; }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent == null ? Color.FromArgb(244,247,246) : Parent.BackColor);
            RectangleF r = new RectangleF(1,1,Width-3,Height-3);
            using (GraphicsPath outline = Symbols.Rounded(r,10))
            using (Brush background = new SolidBrush(hover ? Color.FromArgb(236,246,241) : Color.White))
            using (Pen border = new Pen(hover ? Color.FromArgb(83,151,132) : Color.FromArgb(222,232,225))) { e.Graphics.FillPath(background,outline); e.Graphics.DrawPath(border,outline); }
            float badge = Math.Min(44,Height-22), x = 13, y = (Height-badge)/2;
            using (Brush b = new SolidBrush(Color.FromArgb(230,241,234))) e.Graphics.FillEllipse(b,x,y,badge,badge);
            Symbols.Draw(e.Graphics,new RectangleF(x+badge*.23f,y+badge*.23f,badge*.54f,badge*.54f),SymbolKind,Color.FromArgb(18,112,104));
            string[] lines = Text.Split('\n'); int left = (int)(x+badge+12); int width = Math.Max(10,Width-left-14);
            int headingHeight = Math.Max(22,Font.Height+3), bodyHeight = Math.Max(18,Font.Height);
            int top = Math.Max(8,(Height-headingHeight-bodyHeight)/2);
            using (Font title = new Font(Font.FontFamily,Font.Size,FontStyle.Bold))
            using (Font detail = new Font(Font.FontFamily,Math.Max(8,Font.Size-1)))
            {
                TextRenderer.DrawText(e.Graphics,lines[0],title,new Rectangle(left,top,width,headingHeight),Color.FromArgb(24,49,58),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
                if (lines.Length>1) TextRenderer.DrawText(e.Graphics,lines[1],detail,new Rectangle(left,top+headingHeight,width,bodyHeight),Color.FromArgb(91,108,115),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
            }
            if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(5,5,Width-10,Height-10));
        }
    }
}
