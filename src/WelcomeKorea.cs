using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("Welcome Korea")]
[assembly: AssemblyProduct("Welcome Korea")]
[assembly: AssemblyVersion("1.8.0.0")]

namespace WelcomeKorea
{
    public sealed class Restaurant
    {
        public string Id, Region, Name, Korean, Cuisine, Description, Menu, Address, Source, SourceName, Note, Checked;
        public string LocationPath;
        public string[] FoodTypes;
    }
    public static class Catalog
    {
        public static readonly string[] Regions = { "Myeongdong", "Insadong", "Hongdae / Yeonnam" };
        public static List<Restaurant> Load()
        {
            using (Stream stream = typeof(Catalog).Assembly.GetManifestResourceStream("restaurants.json"))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                return new JavaScriptSerializer().Deserialize<List<Restaurant>>(reader.ReadToEnd());
        }
        public static List<Restaurant> Filter(IEnumerable<Restaurant> items, string region, string query)
        {
            query = (query ?? "").Trim();
            return items.Where(r => (region == "All areas" || r.Region == region) &&
                (query.Length == 0 || (r.Name + " " + r.Korean + " " + r.Cuisine + " " + r.Menu).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
        }
    }
    public static class Links
    {
        public const string Tourism = "https://english.visitkorea.or.kr/";
        public const string Cards = "https://english.visitkorea.or.kr/svc/contents/infoBscView.do?menuSn=479&vcontsId=140663";
        public const string Help = "https://english.visitkorea.or.kr/svc/contents/infoHtmlView.do?menuSn=454&vcontsId=140632";
        public const string Airport = "https://www.airport.kr/ap_en/index.do";
        public const string Money = "https://english1.visitkorea.or.kr/enu/TRV/TV_ENG_1_3.jsp";
        public const string Sim = "https://m.roaming.kt.com/eng/esim";
        public static string Search(string query) { return "https://www.google.com/search?q=" + Uri.EscapeDataString(query); }
        public static string Maps(string query) { return "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(query); }
        public static string Route(string destination, string origin, string mode)
        {
            if (mode != "transit" && mode != "walking" && mode != "driving" && mode != "bicycling") throw new ArgumentException("Invalid travel mode");
            return "https://www.google.com/maps/dir/?api=1&destination=" + Uri.EscapeDataString(destination) + "&travelmode=" + mode +
                (String.IsNullOrWhiteSpace(origin) ? "" : "&origin=" + Uri.EscapeDataString(origin.Trim()));
        }
        public static bool IsSafe(string url)
        {
            Uri uri;
            return Uri.TryCreate(url, UriKind.Absolute, out uri) && uri.Scheme == Uri.UriSchemeHttps && !String.IsNullOrEmpty(uri.Host);
        }
    }
    public sealed class Preferences
    {
        public string Region = "Myeongdong";
        public string Locale = "ko-KR";
        public RegionSelection FoodRegion = new RegionSelection();
        public string FoodCategory = "all";
        public List<string> Saved = new List<string>();
    }
    public sealed class LocalStore
    {
        readonly string path;
        public string LastError = "";
        public LocalStore(string file) { path = file; }
        public Preferences Load()
        {
            try
            {
                if (!File.Exists(path)) return new Preferences();
                Preferences p = new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(path, Encoding.UTF8));
                if (p == null) throw new InvalidDataException("Empty settings");
                if (!Catalog.Regions.Contains(p.Region)) p.Region = Catalog.Regions[0];
                if (p.Saved == null) p.Saved = new List<string>();
                p.Saved = p.Saved.Distinct().ToList();
                if (!LanguageCatalog.Options.Any(o => o.Locale == p.Locale)) p.Locale = "ko-KR";
                if(p.FoodRegion==null)p.FoodRegion=new RegionSelection();
                p.FoodRegion.Province=p.FoodRegion.Province??"";p.FoodRegion.City=p.FoodRegion.City??"";p.FoodRegion.District=p.FoodRegion.District??"";p.FoodRegion.Neighborhood=p.FoodRegion.Neighborhood??"";p.FoodRegion.Detail=p.FoodRegion.Detail??"";
                if(!FoodCatalog.Types.Any(t=>t.Key==p.FoodCategory))p.FoodCategory="all";
                return p;
            }
            catch (Exception ex) { LastError = "Could not read saved places: " + ex.Message; return new Preferences(); }
        }
        public void Save(Preferences prefs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, new JavaScriptSerializer().Serialize(prefs), Encoding.UTF8);
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
    }
    public sealed class Banner : Panel
    {
        readonly Localizer language;
        public Banner(Localizer language){this.language=language;DoubleBuffered=true;Height=180;}
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);KoreanScenery.Draw(e.Graphics,ClientRectangle,"Home",true);
            using(Font title=new Font("Malgun Gothic",23,FontStyle.Bold))
            using(Font body=new Font("Malgun Gothic",11))
            using(Brush white=new SolidBrush(Color.FromArgb(255,249,234)))
            using(Brush light=new SolidBrush(Color.FromArgb(214,231,218)))
            {
                e.Graphics.DrawString(language.Text("Your first day, made easier."),title,white,new RectangleF(22,18,Width*.77f,55));
                e.Graphics.DrawString(ReadingText.Sentences(language.Text("Find your way. Find your next meal. Feel at home.")),body,light,new RectangleF(25,77,Width*.65f,Height-80));
            }
        }
    }
    public sealed class MainWindow : Form
    {
        static readonly Color Ink = Color.FromArgb(24, 49, 58), Teal = Color.FromArgb(18, 112, 104), Muted = Color.FromArgb(91, 108, 115);
        readonly List<Restaurant> restaurants = Catalog.Load();
        readonly LocalStore store;
        readonly Preferences prefs;
        readonly Localizer language;
        public AppLanguage? RequestedLanguage { get; private set; }
        public string RequestedLocale { get; private set; }
        readonly string selectedLocale;
        readonly ScenicSurface content = new ScenicSurface();
        readonly ScenicHeader top = new ScenicHeader();
        readonly Label feedback = new Label(), pageTitle = new Label();
        readonly SymbolBadge pageSymbol = new SymbolBadge();
        readonly ComboBox region = new ComboBox();
        readonly Dictionary<string, Button> nav = new Dictionary<string, Button>();
        string active = "Home";
        string foodSearchText="";
        public MainWindow(string settingsPath) : this(settingsPath, AppLanguage.English) { }
        public MainWindow(string settingsPath, AppLanguage selectedLanguage) : this(settingsPath, selectedLanguage, selectedLanguage == AppLanguage.Korean ? "ko-KR" : "en-US") { }
        public MainWindow(string settingsPath, AppLanguage selectedLanguage, string locale)
        {
            selectedLocale = LanguageCatalog.Find(locale).Locale;
            language = new Localizer(selectedLanguage);
            store = new LocalStore(settingsPath); prefs = store.Load();
            Text = "Welcome Korea"; Font = new Font(selectedLanguage == AppLanguage.Korean ? "Malgun Gothic" : "Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(1120, 760); MinimumSize = new Size(1000, 690);
            StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(244, 247, 246);
            TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210)); shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Controls.Add(shell);
            Panel side = new Panel { Dock = DockStyle.Fill, BackColor = Ink, Margin = Padding.Empty }; shell.Controls.Add(side, 0, 0);
            Label brand = new Label { Text = "WELCOME\nKOREA", ForeColor = Color.White, Font = new Font("Segoe UI", 18, FontStyle.Bold), Location = new Point(22, 26), Size = new Size(185, 100) }; side.Controls.Add(brand);
            side.Controls.Add(new Label { Text = "A little local confidence.", ForeColor = Color.FromArgb(165, 198, 194), Location = new Point(24, 136), Size = new Size(180, 30), Font = new Font("Segoe UI", 9) });
            string[] pages = { "Home", "Maps", "Food", "Transport", "Essentials", "Travel Help", "Saved places" };
            for (int i = 0; i < pages.Length; i++)
            {
                string page = pages[i];
                Button b = new Button { Text = T(page), Font=new Font(Font.FontFamily,11), TextAlign = ContentAlignment.MiddleLeft, ImageAlign = ContentAlignment.MiddleLeft, TextImageRelation = TextImageRelation.ImageBeforeText, Image = Symbols.Bitmap(Symbols.ForPage(page),22,Color.FromArgb(192,218,207)), Padding = new Padding(10, 0, 0, 0), Location = new Point(14, 195 + i * 49), Size = new Size(182, 43), FlatStyle = FlatStyle.Flat, BackColor = Ink, ForeColor = Color.White, Cursor = Cursors.Hand };
                b.Disposed += delegate { if (b.Image != null) b.Image.Dispose(); };
                b.FlatAppearance.BorderSize = 0; b.Click += delegate { Navigate(page); }; side.Controls.Add(b); nav[page] = b;
            }
            Button changeLanguage = new Button { Text = "언어 / Language", Location = new Point(14, 550), Size = new Size(182, 40), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Teal };
            changeLanguage.Click += delegate
            {
                using (LanguageSelection chooser = new LanguageSelection(selectedLocale))
                {
                    if (chooser.ShowDialog(this) == DialogResult.OK) { RequestedLanguage = chooser.SelectedLanguage; RequestedLocale = chooser.SelectedLocale; prefs.Locale = chooser.SelectedLocale; Persist(); Close(); }
                }
            };
            side.Controls.Add(changeLanguage);
            Label edition = new Label { Text = selectedLanguage == AppLanguage.Korean ? "한국어 / " + selectedLocale + "\nWindows 테스트 버전 1.8\n자료 확인일 2026-10-28" : "ENGLISH / " + selectedLocale + "\nWindows preview 1.8\nGuides checked 28 OCT 2026", ForeColor = Color.FromArgb(165, 198, 194), Dock = DockStyle.Bottom, Height = 85, Padding = new Padding(23, 10, 0, 0), Font = new Font("Segoe UI", 8) }; side.Controls.Add(edition);
            TableLayoutPanel main = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(26, 0, 24, 0), Margin = Padding.Empty };
            main.RowStyles.Add(new RowStyle(SizeType.Absolute, 92)); main.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); main.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); shell.Controls.Add(main, 1, 0);
            top.Dock=DockStyle.Fill;top.Margin=Padding.Empty;top.Name="KoreanScenicHeader";
            pageSymbol.Name = "PageSymbol"; pageSymbol.SetBounds(0,27,44,44); top.Controls.Add(pageSymbol);
            pageTitle.BackColor=Color.Transparent;pageTitle.Font = new Font(Font.FontFamily, 20, FontStyle.Bold); pageTitle.ForeColor = Ink; pageTitle.SetBounds(56, 26, 560, 48); top.Controls.Add(pageTitle);
            main.Controls.Add(top, 0, 0);
            content.Name="PageContent";content.Dock = DockStyle.Fill; content.AutoScroll = true; content.WrapContents = false; content.FlowDirection = FlowDirection.TopDown; content.Margin = Padding.Empty; content.Padding = new Padding(0, 0, 8, 10);
            content.SizeChanged += delegate { FitCards(); }; main.Controls.Add(content, 0, 1);
            feedback.Dock = DockStyle.Fill; feedback.TextAlign = ContentAlignment.MiddleLeft; feedback.ForeColor = Muted; feedback.Font = new Font("Segoe UI", 9); main.Controls.Add(feedback, 0, 2);
            language.TranslateTree(shell);
            Navigate("Home"); if (store.LastError.Length > 0) feedback.Text = T("Could not read saved places.");
        }
        public AppLanguage CurrentLanguage { get { return language.Language; } }
        string T(string text) { return language.Text(text); }
        public string CurrentPage { get { return active; } }
        public int VisibleCardCount { get { return content.Controls.Count; } }
        void Persist()
        {
            try { store.Save(prefs); }
            catch (Exception ex) { MessageBox.Show(this, T("Your change could not be saved on this computer.") + "\n" + ex.Message, T("Save failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        void FitCards()
        {
            int width = Math.Max(400, content.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 16);
            foreach (Control c in content.Controls)
            {
                if(c is FlowLayoutPanel){c.MinimumSize=new Size(width,0);c.MaximumSize=new Size(width,0);}
                c.Width = width;
                if (c.Name == "HomeSurface") c.Height = Math.Max(300, content.ClientSize.Height - 12);
                foreach(Control child in c.Controls)if(child.Name=="ReadingCard"){child.MinimumSize=new Size(width,0);child.MaximumSize=new Size(width,0);child.Width=width;}
            }
        }
        public void Navigate(string page)
        {
            if (!nav.ContainsKey(page)) throw new ArgumentException("Unknown page");
            active = page; pageTitle.Text = T(page == "Home" ? "Let's explore Korea." : page);
            top.Page=page;content.Page=page;top.Invalidate();content.Invalidate();
            pageTitle.ForeColor=KoreanScenery.Accent(page);pageSymbol.Kind = Symbols.ForPage(page); pageSymbol.AccessibleName = T(page); pageSymbol.Invalidate();
            foreach (var item in nav) item.Value.BackColor = item.Key == page ? Teal : Ink;
            content.SuspendLayout();
            while (content.Controls.Count > 0) { Control old = content.Controls[0]; content.Controls.RemoveAt(0); old.Dispose(); }
            feedback.Text = LanguageCatalog.Find(selectedLocale).UsesEnglishPreview ? "Selected: " + selectedLocale + " · Translation pending; English preview" : T("Offline guides available  ·  Map and web links open in your browser");
            if (page == "Home") Home();
            else if (page == "Maps") Maps();
            else if (page == "Food") Food(false);
            else if (page == "Saved places") Food(true);
            else if (page == "Transport") Transport();
            else if (page == "Essentials") Essentials();
            else Help();
            language.TranslateTree(content);
            FitCards(); content.ResumeLayout(true); content.AutoScrollPosition = Point.Empty;
        }
        void Add(Control control) { control.Margin = new Padding(0, 0, 0, 14); content.Controls.Add(control); }
        Label TextBlock(string text, float size, Color color, bool bold)
        {
            return new Label { Text = ReadingText.Sentences(T(text)), AutoSize = true, MaximumSize = new Size(660, 0), Margin = new Padding(0, 0, 0, 10), ForeColor = color, Font = new Font(Font.FontFamily, size<10?10:size<12?11.5f:size, bold ? FontStyle.Bold : FontStyle.Regular) };
        }
        FlowLayoutPanel Card(string title, string text)
        {
            FlowLayoutPanel box = new FlowLayoutPanel { Name="ReadingCard", BackColor = Color.White, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(400, 0), FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20, 16, 20, 14) };
            if (title.Length > 0) { string icon=active=="Transport"?(title.IndexOf("airport",StringComparison.OrdinalIgnoreCase)>=0?"🛬 ":"🚆 "):active=="Essentials"?(title.Contains("SIM")?"📶 ":"💳 "):active=="Travel Help"?"💬 ":"";box.Controls.Add(TextBlock(icon+T(title),16,Ink,true)); }
            if (text.Length > 0) box.Controls.Add(TextBlock(text, 10, Muted, false));
            return box;
        }
        Button ActionButton(string text, Action action, bool primary)
        {
            Button b = new Button { Text = ReadingText.Sentences(T(text)), AutoSize = true, Font=new Font(Font.FontFamily,11), MinimumSize = new Size(130, 42), Padding = new Padding(10, 5, 10, 5), Margin = new Padding(0, 0, 9, 6), FlatStyle = FlatStyle.Flat, BackColor = primary ? Teal : Color.FromArgb(235, 242, 240), ForeColor = primary ? Color.White : Ink, Cursor = Cursors.Hand };
            string provider=text=="Naver Map"?"naver":text=="Kakao Map"?"kakao":text=="Google Maps"?"google":null;
            if(provider!=null){b.Image=Symbols.ProviderBitmap(provider,24);b.TextImageRelation=TextImageRelation.ImageBeforeText;b.ImageAlign=ContentAlignment.MiddleLeft;b.Disposed+=delegate{if(b.Image!=null)b.Image.Dispose();};}
            b.FlatAppearance.BorderSize = 0; b.Click += delegate { action(); }; return b;
        }
        void Actions(FlowLayoutPanel card, params Button[] buttons)
        {
            FlowLayoutPanel row = new FlowLayoutPanel { AutoSize = true, MaximumSize = new Size(680, 0), WrapContents = true, FlowDirection = FlowDirection.LeftToRight, Margin = Padding.Empty };
            row.Controls.AddRange(buttons); card.Controls.Add(row);
        }
        void Open(string url)
        {
            if (!Links.IsSafe(url)) { feedback.Text = T("This link is not supported."); return; }
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); feedback.Text = T("Opened in your browser. An internet connection is needed."); }
            catch (Exception ex) { MessageBox.Show(this, T("Could not open your browser.") + "\n" + ex.Message, T("Open link"), MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }
        void Copy(string text)
        {
            try { Clipboard.SetText(text); feedback.Text = T("Copied. You can paste this into a map, message or translation app."); }
            catch (Exception ex) { feedback.Text = T("Could not copy:") + " " + ex.Message; }
        }
        void Home()
        {
            // All rows have explicit percentages. Default auto-sized table rows previously
            // consumed the grid height unevenly, hiding the first category buttons.
            TableLayoutPanel home = new TableLayoutPanel { Name = "HomeSurface", ColumnCount = 1, RowCount = 3, Margin = Padding.Empty, Padding = Padding.Empty, Height = 540 };
            home.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            home.RowStyles.Add(new RowStyle(SizeType.Percent, 24));
            home.RowStyles.Add(new RowStyle(SizeType.Percent, 16));
            home.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
            Banner banner = new Banner(language) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) }; home.Controls.Add(banner, 0, 0);
            Label intro = new Label { Text = ReadingText.Sentences(T("🌿 A gentle start to your Korean journey.") + "\n" + T("Six categories for a smoother visit. No sign-up needed.")), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Ink, Font = new Font(Font.FontFamily, 11), Margin = new Padding(0, 0, 0, 6) }; home.Controls.Add(intro, 0, 1);
            TableLayoutPanel grid = new TableLayoutPanel { Name = "HomeCategories", ColumnCount = 2, RowCount = 3, Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = Padding.Empty, GrowStyle = TableLayoutPanelGrowStyle.FixedSize };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int row = 0; row < 3; row++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 3f));
            string[] titles = { "01  " + T("Maps"), "02  " + T("Food"), "03  " + T("Transport"), "04  " + T("Essentials"), "05  " + T("Travel Help"), "06  " + T("Saved places") };
            string[] details = { "Search places & open Google Maps", "Discover local meals & save places", "Airport, subway & transit cards", "Payments, SIM & digital access", "Useful Korean phrases & travel help", "Your saved restaurants, available offline" };
            string[] routes = { "Maps", "Food", "Transport", "Essentials", "Travel Help", "Saved places" };
            for (int i = 0; i < titles.Length; i++)
            {
                string route = routes[i];
                Button b = new CategoryTile { SymbolKind = Symbols.ForPage(route), Name = "HomeCategory" + (i + 1), Tag = route, Font=new Font(Font.FontFamily,11), Text = titles[i] + "\n" + T(details[i]), Dock = DockStyle.Fill, AutoSize = false, BackColor = Color.White, ForeColor = Ink, Margin = new Padding(0, 0, 10, 10), AccessibleName = T(route) };
                b.FlatAppearance.BorderSize = 0; b.Click += delegate { Navigate(route); }; grid.Controls.Add(b, i % 2, i / 2);
            }
            home.Controls.Add(grid, 0, 2); Add(home);
        }
        void Maps()
        {
            FlowLayoutPanel card = Card("Plan your journey", "1. Enter your starting point  /  2. Enter your destination  /  3. Choose how to travel");
            card.Name = "RoutePlanner";
            FlowLayoutPanel fields = new FlowLayoutPanel { Name = "JourneyInputs", Width = 640, Height = 132, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0,3,0,0) };
            FlowLayoutPanel from = new FlowLayoutPanel { Width = 305, Height = 128, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0,0,20,0) };
            FlowLayoutPanel to = new FlowLayoutPanel { Width = 305, Height = 128, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
            Label fromLabel = TextBlock("Enter your starting point here",12,Ink,true); fromLabel.MaximumSize = new Size(300,0); from.Controls.Add(fromLabel);
            Label fromTip = TextBlock("Example: Seoul Station or your hotel",9,Muted,false); fromTip.MaximumSize = new Size(300,0); from.Controls.Add(fromTip);
            TextBox origin = new TextBox { Name = "OriginInput", Font=new Font(Font.FontFamily,12), AccessibleName = T("Enter your starting point here"), Width = 300, MaxLength = 220, Margin = Padding.Empty }; from.Controls.Add(origin);
            Label toLabel = TextBlock("Enter your destination here",12,Ink,true); toLabel.MaximumSize = new Size(300,0); to.Controls.Add(toLabel);
            Label toTip = TextBlock("Example: Gyeongbokgung or Myeongdong",9,Muted,false); toTip.MaximumSize = new Size(300,0); to.Controls.Add(toTip);
            TextBox target = new TextBox { Name = "DestinationInput", Font=new Font(Font.FontFamily,12), AccessibleName = T("Enter your destination here"), Width = 300, MaxLength = 220, Margin = Padding.Empty }; to.Controls.Add(target);
            fields.Controls.Add(from); fields.Controls.Add(to); card.Controls.Add(fields);
            Actions(card, ActionButton("Swap starting point and destination", delegate { string previous = origin.Text; origin.Text = target.Text; target.Text = previous; }, false));
            card.Controls.Add(TextBlock("Choose a travel method", 12, Ink, true));
            FlowLayoutPanel modes = new FlowLayoutPanel { Name = "TravelMethods", FlowDirection = FlowDirection.LeftToRight, WrapContents = true, AutoSize = true, MaximumSize = new Size(650,0), Margin = new Padding(0,0,0,10) };
            string selected = "transit";
            GeoPlace confirmedOrigin=null, confirmedDestination=null;
            Label note = TextBlock("", 9, Muted, false); note.Name = "TravelMethodNote";
            Label summary = TextBlock("", 10, Ink, false); summary.Name = "RouteRequestSummary";
            Label error = TextBlock("", 10, Color.FromArgb(158,65,51), false); error.Name = "RouteValidation"; error.Visible = false;
            Action<string> reportError = delegate(string text) { error.Text = T(text); error.Visible = true; };
            Func<string,string> placePreview = delegate(string text) { string value = text.Trim(); return value.Length>28 ? value.Substring(0,28)+"…" : value; };
            Action refresh = delegate
            {
                TravelOption choice = RoutePlanner.Find(selected);
                foreach (Button b in modes.Controls) { bool chosen = (string)b.Tag == selected; b.BackColor = chosen ? Teal : Color.FromArgb(236,242,238); b.ForeColor = chosen ? Color.White : Ink; b.FlatAppearance.BorderColor = chosen ? Teal : Color.FromArgb(214,228,219); b.AccessibleDescription = chosen ? T("Selected") : ""; }
                note.Text = ReadingText.Sentences(T(choice.Note));
                summary.Text = T(confirmedOrigin==null?"Route to search":"Confirmed places") + " · " + T(choice.Label) + "\nA  " + (confirmedOrigin!=null?placePreview(confirmedOrigin.Name):String.IsNullOrWhiteSpace(origin.Text)?T("Starting point"):placePreview(origin.Text)) + "  →  B  " + (confirmedDestination!=null?placePreview(confirmedDestination.Name):String.IsNullOrWhiteSpace(target.Text)?T("Destination"):placePreview(target.Text));
                error.Text = ""; error.Visible = false;
            };
            foreach (TravelOption option in RoutePlanner.Options)
            {
                TravelOption choice = option;
                Button button = new Button { Name = "RouteMode-" + choice.Key, Tag = choice.Key, Text = T(choice.Label).Replace(" (","\n("), AccessibleName = T(choice.Label), Width = 150, Height = 78, Margin = new Padding(0,0,10,5), FlatStyle = FlatStyle.Flat, TextImageRelation = TextImageRelation.ImageAboveText, Image = Symbols.Bitmap(choice.Symbol,23,Color.FromArgb(172,197,178)), Font = new Font(Font.FontFamily,10.5f,FontStyle.Bold), Cursor = Cursors.Hand };
                button.Disposed += delegate { if (button.Image != null) button.Image.Dispose(); };
                button.Click += delegate { selected = choice.Key; refresh(); };
                modes.Controls.Add(button);
            }
            card.Controls.Add(modes); card.Controls.Add(note); card.Controls.Add(summary); card.Controls.Add(error);
            Action<string> showRoute = delegate(string provider)
            {
                try
                {
                    if(String.IsNullOrWhiteSpace(origin.Text)) throw new ArgumentException("Enter your starting point here");
                    if(String.IsNullOrWhiteSpace(target.Text)) throw new ArgumentException("Enter your destination here");
                    if(confirmedOrigin==null || confirmedDestination==null)
                    {
                        using(PlaceConfirmation confirmation=new PlaceConfirmation(origin.Text.Trim(),target.Text.Trim(),language.Language))
                        {
                            if(confirmation.ShowDialog(this)!=DialogResult.OK) return;
                            confirmedOrigin=confirmation.StartPlace;confirmedDestination=confirmation.EndPlace;
                        }
                        refresh();
                    }
                    Open(RoutePlanner.Build(confirmedOrigin,confirmedDestination,selected,provider));
                }
                catch (ArgumentException ex) { reportError(ex.Message); if (String.IsNullOrWhiteSpace(origin.Text)) origin.Focus(); else target.Focus(); }
            };
            card.Controls.Add(TextBlock("Choose a map for this travel method",12,Ink,true));
            FlowLayoutPanel providers=new FlowLayoutPanel { Name="RouteProviders",FlowDirection=FlowDirection.LeftToRight,WrapContents=true,AutoSize=true,MaximumSize=new Size(650,0),Margin=Padding.Empty };
            foreach(string providerKey in RoutePlanner.Providers)
            {
                string provider=providerKey;
                Button routeButton=ActionButton(RoutePlanner.ProviderName(provider),delegate { showRoute(provider); },provider=="naver");routeButton.Name="ShowRoute-"+provider;routeButton.Tag=provider;routeButton.MinimumSize=new Size(160,40);providers.Controls.Add(routeButton);
            }
            card.Controls.Add(providers);
            card.Controls.Add(TextBlock("Confirm the place addresses once, then open the same journey in any map above.",9,Muted,false));
            origin.TextChanged += delegate { confirmedOrigin=null;confirmedDestination=null;refresh(); }; target.TextChanged += delegate { confirmedOrigin=null;confirmedDestination=null;refresh(); };
            target.KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; providers.Controls[0].Focus(); feedback.Text=T("Choose Naver Map, Kakao Map or Google Maps to continue."); } };
            refresh(); Add(card);
            FlowLayoutPanel alternative = Card("Walking in Korea", "For walking or cycling in Korea, try Naver Map or Kakao Map first. If a map cannot provide a route, choose another map above; your confirmed places and travel method stay the same.");
            alternative.Controls.Add(TextBlock("Place search: Photon / OpenStreetMap contributors. Search only when requested; no automatic location tracking.",9,Muted,false));
            Actions(alternative,ActionButton("Copy journey",delegate { if(!String.IsNullOrWhiteSpace(origin.Text) && !String.IsNullOrWhiteSpace(target.Text)) Copy(origin.Text.Trim()+" → "+target.Text.Trim()+"\n"+T(RoutePlanner.Find(selected).Label));else reportError("Enter both places before copying your journey."); },false),ActionButton("Map data attribution",delegate { Open("https://www.openstreetmap.org/copyright"); },false),ActionButton("Place-search service",delegate { Open("https://photon.komoot.io/"); },false));Add(alternative);
        }
        void Food(bool saved)
        {
            FlowLayoutPanel filters=Card(saved?"Your saved food stops":"① Choose an area and food",saved?"Filter your saved restaurants by area and food type.":"Choose your province, city and neighborhood, then pick a food type and a map to search.");
            filters.Name="RegionalFoodSection";((Label)filters.Controls[0]).Font=new Font(Font.FontFamily,17,FontStyle.Bold);((Label)filters.Controls[1]).Font=new Font(Font.FontFamily,11);
            RegionPicker picker=new RegionPicker(language);picker.SetSelection(prefs.FoodRegion);filters.Controls.Add(picker);
            filters.Controls.Add(TextBlock("Choose a food type",12,Ink,true));
            FlowLayoutPanel types=new FlowLayoutPanel{Name="FoodTypes",AutoSize=true,WrapContents=true,FlowDirection=FlowDirection.LeftToRight,MaximumSize=new Size(650,0),Margin=new Padding(0,0,0,6)};
            string foodKey=prefs.FoodCategory;
            filters.Controls.Add(types);
            Label current=TextBlock("",11,Muted,false);current.Name="FoodSearchSummary";filters.Controls.Add(current);
            FlowLayoutPanel searchProviders=new FlowLayoutPanel{Name="FoodSearchProviders",AutoSize=true,WrapContents=true,MaximumSize=new Size(650,0),Margin=Padding.Empty};
            foreach(string providerKey in RoutePlanner.Providers)
            {
                string provider=providerKey;Button search=ActionButton(RoutePlanner.ProviderName(provider),delegate{Open(FoodCatalog.SearchUrl(provider,picker.Selection,foodKey,""));},provider=="naver");search.Name="FoodSearch-"+provider;search.Tag=provider;search.Font=new Font(Font.FontFamily,11,FontStyle.Bold);search.MinimumSize=new Size(165,44);searchProviders.Controls.Add(search);
            }
            filters.Controls.Add(searchProviders);
            Add(filters);
            FlowLayoutPanel totalSection=Card("② Total search","Enter any area or restaurant name, then choose a map. This search is independent of section 1.");
            totalSection.Name="TotalFoodSection";totalSection.BackColor=Color.FromArgb(237,246,246);((Label)totalSection.Controls[0]).Font=new Font(Font.FontFamily,17,FontStyle.Bold);((Label)totalSection.Controls[1]).Font=new Font(Font.FontFamily,11);

            totalSection.Controls.Add(TextBlock("Example: Hongdae restaurants, Sinchon restaurants, Myeongdong restaurants",11,Muted,false));
            TextBox totalQuery=new TextBox{Name="TotalFoodQuery",AccessibleName=T("Total search"),Text=foodSearchText,Font=new Font(Font.FontFamily,12),Width=620,MaxLength=160,Margin=new Padding(0,0,0,7)};totalSection.Controls.Add(totalQuery);
            FlowLayoutPanel totalProviders=new FlowLayoutPanel{Name="TotalFoodProviders",AutoSize=true,WrapContents=true,MaximumSize=new Size(650,0),Margin=Padding.Empty};
            foreach(string providerKey in RoutePlanner.Providers)
            {
                string provider=providerKey;Button button=ActionButton(RoutePlanner.ProviderName(provider),delegate{Open(FoodCatalog.TotalSearchUrl(provider,totalQuery.Text));},provider=="naver");button.Name="TotalFood-"+provider;button.Tag=provider;button.Font=new Font(Font.FontFamily,11,FontStyle.Bold);button.MinimumSize=new Size(165,44);button.Enabled=!String.IsNullOrWhiteSpace(totalQuery.Text);totalProviders.Controls.Add(button);
            }
            totalQuery.TextChanged+=delegate{foodSearchText=totalQuery.Text;foreach(Control button in totalProviders.Controls)button.Enabled=!String.IsNullOrWhiteSpace(totalQuery.Text);};
            totalQuery.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;if(totalProviders.Controls[0].Enabled)totalProviders.Controls[0].Focus();}};
            totalSection.Controls.Add(totalProviders);


            Add(totalSection);
            FlowLayoutPanel results = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
            Action render = delegate
            {
                results.SuspendLayout(); while (results.Controls.Count > 0) { Control old = results.Controls[0]; results.Controls.RemoveAt(0); old.Dispose(); }
                IEnumerable<Restaurant> pool = saved ? restaurants.Where(r => prefs.Saved.Contains(r.Id)) : restaurants;
                RegionSelection selection=picker.Selection;
                List<Restaurant> matches=FoodCatalog.Filter(pool,selection,foodKey,"").ToList();
                List<FoodStreet> streets=saved?new List<FoodStreet>():FoodCatalog.FilterStreets(selection,foodKey,"").ToList();
                foreach(Button button in types.Controls)
                {
                    bool selected=(string)button.Tag==foodKey;button.BackColor=selected?Teal:Color.FromArgb(235,242,240);button.ForeColor=selected?Color.White:Ink;button.AccessibleDescription=selected?T("Selected"):"";
                    if(button.Image!=null)button.Image.Dispose();button.Image=Symbols.Bitmap(FoodCatalog.Type((string)button.Tag).Symbol,22,selected?Color.White:Teal);
                }
                current.Text=T("Current search")+": "+FoodCatalog.Query(selection,foodKey,"");
                if(matches.Count==0 && streets.Count==0)results.Controls.Add(Card("No researched entries for these filters yet",saved?"Save a restaurant from Food to build your list, or try another filter.":"Use the map buttons above to find current venues for the selected area and food type. An empty preview list does not mean there are no restaurants."));
                if(streets.Count>0)results.Controls.Add(Card("Food streets & local market areas",T("Researched area guides")+": "+streets.Count));
                foreach(FoodStreet street in streets){FlowLayoutPanel c=FoodStreetCard(street);c.Width=Math.Max(400,content.ClientSize.Width-50);c.Margin=new Padding(0,0,0,14);results.Controls.Add(c);}
                if(matches.Count>0)results.Controls.Add(Card(saved?"Your saved food stops":"Researched restaurant introductions",T("Matching restaurants")+": "+matches.Count));
                foreach (Restaurant restaurant in matches) { FlowLayoutPanel c = RestaurantCard(restaurant, saved); c.Width = Math.Max(400, content.ClientSize.Width - 50); c.Margin = new Padding(0, 0, 0, 14); results.Controls.Add(c); }
                results.ResumeLayout(true); FitCards();
            };
            foreach(FoodType food in FoodCatalog.Types)
            {
                FoodType choice=food;Button button=new Button{Name="FoodType-"+choice.Key,Tag=choice.Key,Text=T(choice.Label),Width=151,Height=52,FlatStyle=FlatStyle.Flat,Margin=new Padding(0,0,9,8),TextImageRelation=TextImageRelation.ImageBeforeText,Font=new Font(Font.FontFamily,10.5f),Cursor=Cursors.Hand};button.FlatAppearance.BorderSize=0;button.Disposed+=delegate{if(button.Image!=null)button.Image.Dispose();};button.Click+=delegate{foodKey=choice.Key;prefs.FoodCategory=choice.Key;Persist();render();};types.Controls.Add(button);
            }
            picker.SelectionChanged+=delegate{prefs.FoodRegion=picker.Selection;Persist();render();};
            FlowLayoutPanel visit=Card("Before your visit","");visit.Name="FoodVisitChecklist";
            visit.Controls.Add(TextBlock("🕒 Hours & holidays: check today's opening hours, breaks and last order.",11,Teal,true));
            visit.Controls.Add(TextBlock("💳 Prices & payment: confirm menu prices, minimum orders and foreign-card acceptance.",11,Ink,true));
            visit.Controls.Add(TextBlock("🥜 Allergies & diets: ask about ingredients, cross-contact and vegetarian or halal options.",11,Ink,true));
            Add(visit);Add(results);render();
        }
        FlowLayoutPanel FoodStreetCard(FoodStreet street)
        {
            FlowLayoutPanel card=Card(street.Name+" / "+street.EnglishName,language.Language==AppLanguage.Korean?street.DescriptionKo:street.Description);
            card.Controls.Add(TextBlock(street.LocationPath+"\n"+String.Join(" · ",street.FoodTypes.Select(key=>T(FoodCatalog.Type(key).Label))),9,Muted,false));
            Actions(card,ActionButton("Naver Map",delegate{Open("https://map.naver.com/p/search/"+Uri.EscapeDataString(street.LocationPath+" "+street.Name));},true),ActionButton("Kakao Map",delegate{Open("https://map.kakao.com/link/search/"+Uri.EscapeDataString(street.LocationPath+" "+street.Name));},false),ActionButton("Google Maps",delegate{Open(Links.Maps(street.LocationPath+" "+street.Name));},false),ActionButton("Read source",delegate{Open(street.Source);},false));
            card.Controls.Add(TextBlock(T("Source:")+" "+new Uri(street.Source).Host+" · 2026-10-28",8,Muted,false));return card;
        }
        FlowLayoutPanel RestaurantCard(Restaurant r, bool saved)
        {
            FlowLayoutPanel card = Card(r.Name + "  /  " + r.Korean, T(r.Cuisine) + "  ·  " + T(r.Region) + "\n\n" + T(r.Description));
            card.Controls.Add(TextBlock("Try: " + r.Menu + "\n" + r.Address, 10, Ink, false));
            card.Controls.Add(TextBlock(r.Note + "\nSource: " + r.SourceName + "  ·  Checked " + r.Checked, 9, Muted, false));
            bool isSaved = prefs.Saved.Contains(r.Id);
            Actions(card,ActionButton("Naver Map",delegate{Open("https://map.naver.com/p/search/"+Uri.EscapeDataString(r.Korean+" "+r.Address));},true),ActionButton("Kakao Map",delegate{Open("https://map.kakao.com/link/search/"+Uri.EscapeDataString(r.Korean+" "+r.Address));},false),ActionButton("Google Maps",delegate{Open(Links.Maps(r.Korean+" "+r.Address));},false));
            Actions(card, ActionButton("Read source", delegate { Open(r.Source); }, false), ActionButton("Copy address", delegate { Copy(r.Korean + "\n" + r.Address); }, false), ActionButton(isSaved ? "Remove saved" : "Save place", delegate
            {
                List<string> before = new List<string>(prefs.Saved);
                if (prefs.Saved.Contains(r.Id)) prefs.Saved.Remove(r.Id); else prefs.Saved.Add(r.Id);
                try { store.Save(prefs); Navigate(saved ? "Saved places" : "Food"); feedback.Text = T("Saved places updated on this computer."); }
                catch (Exception ex) { prefs.Saved = before; MessageBox.Show(this, ex.Message, T("Could not save")); }
            }, false));
            return card;
        }
        void Guide(string title, string text, string label, string url)
        {
            FlowLayoutPanel c = Card(title, text); c.Controls.Add(TextBlock("Guide reviewed 2026-10-28. Check the linked source for updates.", 8, Muted, false)); Actions(c, ActionButton(label, delegate { Open(url); }, false)); Add(c);
        }
        void Transport()
        {
            Guide("01 / From the airport", "Start by checking your arrival terminal and destination. Compare the airport rail and bus options through the airport's official transport pages. Use current operator information for fares and last departures.", "Incheon Airport guide", Links.Airport);
            Guide("02 / Transit cards", "Tmoney and EZL cards are sold at convenience stores. Buy a card and add travel credit separately. Bring Korean-won cash for conventional card top-ups; payment options vary by card and recharge channel. Compare cards before buying.", "Official transit-card guide", Links.Cards);
            FlowLayoutPanel c = Card("03 / Plan the next leg", "Keep the Korean destination name and station exit number handy. Choose your start, destination and travel method on the Maps page."); Actions(c,ActionButton("Maps",delegate{Navigate("Maps");},true));Add(c);
        }
        void Essentials()
        {
            Guide("01 / Paying with an overseas card", "Credit cards are widely used in Korea, but acceptance of a particular overseas card is not guaranteed. Before ordering, ask whether your card can be used. Carry a backup payment method and check your issuer's overseas-use settings if a payment fails.", "Official money guide", Links.Money);
            Guide("02 / SIM and eSIM", "Check device compatibility, network-unlock status, data allowance and whether calls or SMS are included before purchasing. A data connection alone does not establish eligibility for Korean identity verification. Confirm authentication requirements with the service you want to use.", "KT visitor eSIM information", Links.Sim);
            FlowLayoutPanel c = Card("03 / When a sign-up asks for a Korean number", "Look for a foreign-visitor or guest option on the official service. Ask the provider whether it supports your number and identity documents. If you get stuck, use the tourism help service for guidance rather than entering someone else's identity."); Actions(c, ActionButton("1330 travel help", delegate { Open(Links.Help); }, false)); Add(c);
            Guide("04 / Keep transport credit separate", "A travel payment card may have a separate transit balance. Read the card's instructions before topping up and check which balance is used for each journey or purchase.", "Compare transport cards", Links.Cards);
        }
        void Help()
        {
            Guide("Travel information & interpretation", "The Korea Tourism Organization's 1330 service offers travel information, interpretation support and tourist complaint channels. Open the official page for online chat and calling options. This button opens the page; it does not place a call.", "Open official 1330 page", Links.Help);
            string[,] phrases = {
                {"Could I see an English menu?", "영어 메뉴가 있나요?"},
                {"Can I pay with a foreign credit card?", "해외 신용카드로 결제할 수 있나요?"},
                {"Please take me to this address.", "이 주소로 가 주세요."},
                {"Where can I top up my transit card?", "교통카드는 어디에서 충전할 수 있나요?"},
                {"Does this contain peanuts?", "이 음식에 땅콩이 들어 있나요?"},
                {"Please help me.", "도와주세요."}
            };
            for (int i = 0; i < phrases.GetLength(0); i++)
            {
                string en = phrases[i,0], ko = phrases[i,1]; FlowLayoutPanel c = Card(language.Language == AppLanguage.Korean ? "여행 회화 · " + en : en, ""); c.Controls.Add(TextBlock(ko, 21, Ink, true)); Actions(c, ActionButton("Copy Korean", delegate { Copy(ko); }, false), ActionButton("Show large card", delegate { using (PhraseWindow window = new PhraseWindow(en, ko)) { window.Text = T("Show this card"); window.ShowDialog(this); } }, true)); Add(c);
            }
            Add(Card("About this preview", "Choose Korean or English at startup. Korean place names and phrase cards are included. No automatic translation or emergency dispatch. Saved places and area preference are stored locally in your Windows user profile. No login, analytics or background location tracking. External sites have their own terms and privacy policies."));
        }
    }
    sealed class PhraseWindow : Form
    {
        public PhraseWindow(string en, string ko)
        {
            Text = "Show this card"; ClientSize = new Size(760, 290); MinimumSize = new Size(600, 250); StartPosition = FormStartPosition.CenterParent; BackColor = Color.White; AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(new Label { Text = ko, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Malgun Gothic", 29, FontStyle.Bold), Padding = new Padding(20) });
            Controls.Add(new Label { Text = en, Dock = DockStyle.Bottom, Height = 60, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 13) });
        }
    }
    static class Program
    {
        [STAThread] static void Main()
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WelcomeKorea", "preferences.json");
            try
            {
                AppLanguage selected;
                string selectedLocale;
                LocalStore startupStore = new LocalStore(path);
                using (LanguageSelection chooser = new LanguageSelection(startupStore.Load().Locale))
                {
                    if (chooser.ShowDialog() != DialogResult.OK) return;
                    selected = chooser.SelectedLanguage;
                    selectedLocale = chooser.SelectedLocale;
                    Preferences prefs = startupStore.Load(); prefs.Locale = selectedLocale;
                    try { startupStore.Save(prefs); } catch (Exception ex) { MessageBox.Show("Could not save language preference. / 언어 설정을 저장하지 못했습니다.\n" + ex.Message, "Welcome Korea"); }
                }
                while (true)
                {
                    using (MainWindow window = new MainWindow(path, selected, selectedLocale))
                    {
                        Application.Run(window);
                        if (!window.RequestedLanguage.HasValue) break;
                        selected = window.RequestedLanguage.Value;
                        selectedLocale = window.RequestedLocale;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Welcome Korea could not start.\n" + ex.Message, "Welcome Korea", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
