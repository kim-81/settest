using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WelcomeKorea;
using WelcomeKoreaSetup;

class Tests
{
    static int count;
    static bool HasText(Control root, string text)
    {
        if (root.Text.Contains(text)) return true;
        foreach (Control child in root.Controls) if (HasText(child, text)) return true;
        return false;
    }
    static void Check(bool value, string name) { if (!value) throw new Exception("FAIL: " + name); count++; Console.WriteLine("PASS: " + name); }
    static void LayoutTree(Control root)
    {
        root.PerformLayout(); foreach (Control child in root.Controls) LayoutTree(child);
    }
    static void CheckHomeLayout(MainWindow app, Size size)
    {
        app.ClientSize = size;
        app.Navigate("Home"); app.CreateControl();
        for (int pass = 0; pass < 3; pass++) LayoutTree(app);
        TableLayoutPanel grid = (TableLayoutPanel)app.Controls.Find("HomeCategories", true).Single();
        Control home = grid.Parent, viewport = home.Parent;
        Button[] buttons = grid.Controls.OfType<Button>().ToArray();
        Check(buttons.Length == 6 && buttons.Select(b=>b.Tag).Distinct().Count() == 6, "six distinct home categories: " + size);
        Check(buttons.All(b=>b.Width > 180 && b.Height >= 45 && b.Left >= 0 && b.Top >= 0 && b.Right <= grid.ClientSize.Width && b.Bottom <= grid.ClientSize.Height), "all six buttons have usable bounds: " + size);
        Check(home.Top + grid.Top + buttons.Max(b=>b.Bottom) <= viewport.ClientSize.Height, "all six categories fit without scrolling: " + size);
        foreach (Button first in buttons) foreach (Button second in buttons)
            if (first != second && first.Bounds.IntersectsWith(second.Bounds)) throw new Exception("Home categories overlap");
    }
    [STAThread] static int Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Check(ReadingText.Sentences("첫 문장입니다. 다음 문장입니다! 마지막인가요? 네.")=="첫 문장입니다.\n다음 문장입니다!\n마지막인가요?\n네.","Korean sentences start on new lines");
            Check(ReadingText.Sentences("First sentence. Next sentence! Why? Yes.")=="First sentence.\nNext sentence!\nWhy?\nYes.","English sentences start on new lines");
            Check(ReadingText.Sentences("1. Choose a place / 2. Choose food. Cost 3.5. Visit https://example.com/info.").StartsWith("1. Choose a place / 2. Choose food.\nCost 3.5."),"numbered steps and decimal values preserved");
            Check(ReadingText.Sentences("Ask Dr. Kim. Keep this line.\nKeep this too.")=="Ask Dr. Kim.\nKeep this line.\nKeep this too.","abbreviations and explicit newlines preserved");
            if(args.Length==1 && args[0]=="--live-places")
            {
                var cityHall=PlaceSearch.Search("Seoul City Hall");var palace=PlaceSearch.Search("덕수궁");
                Check(cityHall.Any(p=>p.Name.Contains("서울") || p.Name.Contains("시청")),"live public-place search: Seoul City Hall");
                Check(palace.Any(p=>p.Name.Contains("덕수궁")),"live public-place search: Deoksugung");
                Console.WriteLine("Live place lookup passed; no private places or route requests were used.");return 0;
            }
            var data = Catalog.Load();
            Check(RegionCatalog.Nodes.Count==20560 && RegionCatalog.Nodes.Select(n=>n.Code).Distinct().Count()==20560,"official active nationwide codes loaded uniquely");
            Check(RegionCatalog.Provinces().Length>=16,"all official province entries available");
            Check(RegionCatalog.Cities("경기도").Contains("수원시") && RegionCatalog.Districts("경기도","수원시").Contains("팔달구") && RegionCatalog.Neighborhoods("경기도","수원시","팔달구").Contains("팔달로1가"),"city and non-autonomous ward hierarchy");
            Check(RegionCatalog.Neighborhoods("강원특별자치도","춘천시","").Contains("조양동"),"smaller city neighborhood selection");
            var rural=new RegionSelection{Province="충청북도",City="단양군",Neighborhood="단양읍",Detail="도전리"};
            Check(RegionCatalog.Villages(rural).Contains("도전리"),"county town and village hierarchy");
            Check(RegionCatalog.Cities("세종특별자치시").Length==0 && RegionCatalog.Neighborhoods("세종특별자치시","","").Length>0,"Sejong skips absent city level");
            using(var picker=new RegionPicker(new Localizer(AppLanguage.Korean)))
            {
                picker.SetSelection(rural);Check(picker.Selection.Detail=="" && picker.Selection.Scope==rural.Scope,"restore rural region without removed detail filter");
                Check(picker.Controls.Find("DistrictSelect",true).Length==0 && picker.Controls.Find("DetailSelect",true).Length==0 && picker.Controls.Find("NeighborhoodSelect",true).Length==1 && picker.Height==76,"region picker has only three fields in one row");
                ((ComboBox)picker.Controls.Find("ProvinceSelect",true).Single()).SelectedItem="강원특별자치도";
                Check(picker.Selection.City=="" && picker.Selection.Neighborhood=="" && picker.Selection.Detail=="","province change clears all lower levels");
                picker.SetSelection(new RegionSelection{Province="강원특별자치도",City="춘천시",Neighborhood="조양동",Detail="춘천 명동 닭갈비골목"});
                Check(picker.Selection.Neighborhood=="조양동" && picker.Selection.Detail=="","legacy street selection discarded");
                picker.SetSelection(new RegionSelection{Province="경기도",City="수원시",District="팔달구",Neighborhood="팔달로1가"});
                Check(picker.Selection.Scope=="경기도 수원시 팔달구 팔달로1가" && ((ComboBox)picker.Controls.Find("CitySelect",true).Single()).Text=="수원시 팔달구","city and ward combined without losing neighborhoods");
                picker.SetSelection(new RegionSelection{Province="세종특별자치시"});
                Check(picker.Controls.Find("NeighborhoodSelect",true).Single().Enabled,"Sejong neighborhood dropdown usable");
            }
            Check(FoodCatalog.Types.Length==12 && FoodCatalog.Types.Select(t=>t.Key).Distinct().Count()==12,"twelve food choices including all");
            Check(FoodCatalog.Streets.Count==11 && FoodCatalog.Streets.Select(s=>s.Id).Distinct().Count()==11,"eleven distinct regional food guides");
            Check(FoodCatalog.Streets.All(s=>Links.IsSafe(s.Source) && RegionCatalog.Nodes.Any(n=>n.Name==s.LocationPath) && s.FoodTypes.All(k=>FoodCatalog.Types.Any(t=>t.Key==k))),"regional guides have sources valid locations and categories");
            Check(data.All(r=>RegionCatalog.Nodes.Any(n=>n.Name==r.LocationPath)),"stored restaurants use valid legal neighborhoods");
            Check(FoodCatalog.Filter(data,new RegionSelection{Province="서울특별시"},"soup","").Count()==2,"region and soup category intersect");
            Check(FoodCatalog.FilterStreets(new RegionSelection{Province="강원특별자치도",City="춘천시"},"stirfry","").Count()==1,"regional stir-fry guide filter");
            Check(FoodCatalog.Query(rural,"snacks","마늘")=="충청북도 단양군 단양읍 도전리 분식 맛집 마늘","full geography detail food and keyword preserved");
            Check(FoodCatalog.Query(new RegionSelection(),"all","")=="대한민국 맛집","national search has no Seoul default");
            foreach(string provider in RoutePlanner.Providers)
            {
                string url=FoodCatalog.SearchUrl(provider,rural,"snacks","마늘 & rice");
                Check(Links.IsSafe(url) && url.Contains(Uri.EscapeDataString(FoodCatalog.Query(rural,"snacks","마늘 & rice"))),"food query encoded for "+provider);
                using(var bitmap=Symbols.ProviderBitmap(provider,24))Check(bitmap.Width==24 && bitmap.GetPixel(12,12).A>0,"brand identifier renders: "+provider);
            }
            bool badFood=false;try{FoodCatalog.Type("invalid");}catch(ArgumentException){badFood=true;}Check(badFood,"unsupported food categories rejected");
            foreach(string keyword in new[]{"홍대 맛집","신촌 맛집","명동 맛집","  Hongdae food & cafe  "})foreach(string provider in RoutePlanner.Providers)
                Check(FoodCatalog.TotalSearchUrl(provider,keyword).Contains(Uri.EscapeDataString(keyword.Trim())) && !FoodCatalog.TotalSearchUrl(provider,keyword).Contains(Uri.EscapeDataString("대한민국")),"independent total search: "+provider+" / "+keyword);
            bool blankTotal=false;try{FoodCatalog.TotalSearchUrl("naver","  ");}catch(ArgumentException){blankTotal=true;}Check(blankTotal,"blank total searches rejected");
            Check(data.Count == 6 && data.Select(r=>r.Id).Distinct().Count() == 6, "six unique embedded restaurants");
            foreach (string region in Catalog.Regions) Check(Catalog.Filter(data, region, "").Count == 2, "two places in " + region);
            Check(Catalog.Filter(data, "All areas", "명동교자").Count == 1, "Korean name search");
            Check(Catalog.Filter(data, "All areas", "PIZZA").Count == 1, "case-insensitive cuisine search");
            Check(Catalog.Filter(data, "Myeongdong", "pizza").Count == 0, "region and text filters intersect");
            Check(data.All(r=>Links.IsSafe(r.Source) && r.Checked == "2026-10-28" && r.Address.Length > 0), "all sources dated and HTTPS");
            string input = "명동 & coffee #1";
            string map = Links.Maps(input);
            Check(map.Contains("api=1") && map.Contains(Uri.EscapeDataString(input)) && !map.Contains("#1"), "map query safely encoded");
            Check(!Links.Route("Seoul", "", "transit").Contains("origin="), "empty origin delegates to map");
            Check(Links.Route("Seoul", "서울역 & exit 1", "transit").Contains("origin=" + Uri.EscapeDataString("서울역 & exit 1")), "route origin safely encoded");
            Check(!Links.IsSafe("file:///C:/Windows/notepad.exe") && !Links.IsSafe("javascript:alert(1)") && !Links.IsSafe("http://example.com"), "unsafe external schemes rejected");
            foreach (TravelOption option in RoutePlanner.Options)
            {
                string route = RoutePlanner.Build(" 서울역 & Exit 1 ", " 명동 #2 ", option.Key);
                Check(route.Contains("travelmode="+option.GoogleMode) && route.Contains("origin="+Uri.EscapeDataString("서울역 & Exit 1")) && route.Contains("destination="+Uri.EscapeDataString("명동 #2")), "selected route mode and endpoints: "+option.Key);
            }
            Check(RoutePlanner.Find("taxi").GoogleMode == "driving" && RoutePlanner.Find("taxi").Note.Contains("not a taxi booking"), "taxi is disclosed as road planning");
            Check(RoutePlanner.Find("transit").Note.Contains("together"), "bus and subway combined routing disclosed");
            foreach (string missing in new[] { "origin", "destination" })
            {
                bool blocked = false;
                try { RoutePlanner.Build(missing == "origin" ? " " : "Seoul", missing == "destination" ? "" : "Myeongdong", "transit"); } catch (ArgumentException) { blocked = true; }
                Check(blocked,"missing "+missing+" blocks route request");
            }
            bool unknown = false; try { RoutePlanner.Build("Seoul","Myeongdong","helicopter"); } catch (ArgumentException) { unknown = true; }
            Check(unknown,"unsupported route method rejected");
            GeoPlace routeStart=new GeoPlace { Name="서울특별시청",Address="서울 중구",Latitude=37.5667893,Longitude=126.9784204 };
            GeoPlace routeEnd=new GeoPlace { Name="덕수궁",Address="서울 중구",Latitude=37.5661668,Longitude=126.9751703 };
            Check(RoutePlanner.Providers.SequenceEqual(new[]{"naver","kakao","google"}),"map provider order is Naver Kakao Google");
            foreach(string provider in RoutePlanner.Providers)
            foreach(TravelOption option in RoutePlanner.Options)
            {
                string route=RoutePlanner.Build(routeStart,routeEnd,option.Key,provider);
                string expectedHost=provider=="naver"?"map.naver.com":provider=="kakao"?"map.kakao.com":"www.google.com";
                string expectedMode=provider=="google"?"travelmode="+option.GoogleMode:provider=="naver"?"/-/"+(option.Key=="walking"?"walk":option.Key=="bicycling"?"bike":option.Key=="taxi"?"car":"transit")+"?":"/link/by/"+(option.Key=="walking"?"walk":option.Key=="bicycling"?"bicycle":option.Key=="taxi"?"car":"traffic")+"/";
                Check(new Uri(route).Host==expectedHost && route.Contains(expectedMode),"provider-specific transport mapping: "+provider+" / "+option.Key);
                Check(provider=="naver"?route.Contains("14135173.100") && route.Contains("4518407.139"):provider=="kakao"?route.Contains(routeStart.Coordinates):route.Contains(Uri.EscapeDataString(routeStart.Coordinates)),"confirmed coordinates carried into "+provider+" / "+option.Key);
            }
            bool samePlace=false;try{RoutePlanner.Build(routeStart,routeStart,"walking","naver");}catch(ArgumentException){samePlace=true;}Check(samePlace,"same coordinates do not open a bogus zero-length route");
            bool missingConfirm=false;try{RoutePlanner.Build(null,routeEnd,"walking","naver");}catch(ArgumentException){missingConfirm=true;}Check(missingConfirm,"unconfirmed places cannot open provider routes");
            bool badCoordinates=false;try{RoutePlanner.Build(new GeoPlace{Name="bad",Latitude=Double.NaN,Longitude=126},routeEnd,"walking","naver");}catch(ArgumentException){badCoordinates=true;}Check(badCoordinates,"invalid coordinates blocked");
            string fixture="{\"features\":[{\"geometry\":{\"type\":\"Point\",\"coordinates\":[126.9784204,37.5667893]},\"properties\":{\"name\":\"서울특별시청\",\"city\":\"서울\",\"countrycode\":\"KR\"}},{\"geometry\":{\"type\":\"Point\",\"coordinates\":[-74,40]},\"properties\":{\"name\":\"Other\",\"countrycode\":\"US\"}},{\"geometry\":{\"type\":\"Point\",\"coordinates\":[126.9784204,37.5667893]},\"properties\":{\"name\":\"서울특별시청\",\"city\":\"서울\",\"countrycode\":\"KR\"}}]}";
            var parsed=PlaceSearch.Parse(fixture,"시청");Check(parsed.Count==1 && parsed[0].Name=="서울특별시청" && parsed[0].Longitude==126.9784204,"place parsing filters countries and duplicate coordinates");
            using(PlaceConfirmation confirm=new PlaceConfirmation("서울시청","덕수궁",AppLanguage.Korean))
            {
                Check(!confirm.Controls.Find("ConfirmBothPlaces",true).Single().Enabled && confirm.StartPlace==null && confirm.EndPlace==null,"place confirmation never silently chooses a candidate");
            }
            Check(new[] {"Maps","Food","Transport","Essentials","Travel Help","Saved places"}.Select(Symbols.ForPage).Distinct().Count() == 6,"six distinct category symbols");
            foreach (string symbol in new[] {"pin","food","transit","card","chat","bookmark","walking","bicycling","taxi"})
            using (Bitmap icon = Symbols.Bitmap(symbol,32,Color.Black))
            {
                int ink = 0; for (int y=0;y<icon.Height;y++) for (int x=0;x<icon.Width;x++) if (icon.GetPixel(x,y).A>0) ink++;
                Check(ink>30,"vector symbol renders: "+symbol);
            }
            string sandbox = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-sandbox-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(sandbox); string prefsPath = Path.Combine(sandbox, "preferences.json");
            LocalStore store = new LocalStore(prefsPath); Preferences prefs = store.Load();
            Check(prefs.Region == "Myeongdong" && prefs.Saved.Count == 0, "first run defaults");
            prefs.Region = "Insadong"; prefs.Saved.Add(data[0].Id); store.Save(prefs);
            Check(store.Load().Region == "Insadong" && store.Load().Saved.Contains(data[0].Id), "saved places persist");
            prefs.FoodRegion=rural;prefs.FoodCategory="snacks";store.Save(prefs);
            Check(store.Load().FoodRegion.Detail=="도전리" && store.Load().FoodCategory=="snacks","food region and category persist independently");
            prefs.Saved.Clear(); store.Save(prefs); Check(store.Load().Saved.Count == 0, "atomic preference replacement");
            File.WriteAllText(prefsPath, "not-json", Encoding.UTF8); Check(store.Load().Saved.Count == 0 && store.LastError.Length > 0, "corrupt preferences recover with message");
            File.WriteAllText(prefsPath, "{\"Region\":\"invalid\",\"Saved\":null}", Encoding.UTF8); Check(store.Load().Region == "Myeongdong" && store.Load().Saved.Count == 0, "invalid preference fields normalized");
            using (LanguageSelection chooser = new LanguageSelection())
            {
                Check(chooser.DialogResult == DialogResult.None && chooser.OptionCount == 50, "startup waits for a dropdown language choice with 50 entries");
                Check(LanguageCatalog.Options[0].DisplayName == "한국어" && LanguageCatalog.Options[1].DisplayName == "ENGLISH", "Korean and ENGLISH pinned first");
                Check(LanguageCatalog.Options.Select(o=>o.Locale).Distinct().Count() == 50, "50 unique country/locale entries");
                ComboBox dropdown = (ComboBox)chooser.Controls.Find("LanguageDropdown", true).Single();
                Check(dropdown.DropDownStyle == ComboBoxStyle.DropDownList && !dropdown.Sorted, "dropdown preserves order and prevents invalid text");
                chooser.SetSelection("ja-JP"); Check(chooser.SupportNotice.Contains("English") && chooser.SupportNotice.Contains("영어"), "pending translation disclosed before continuing");
                chooser.Choose(AppLanguage.Korean);
                Check(chooser.DialogResult == DialogResult.OK && chooser.SelectedLanguage == AppLanguage.Korean, "Korean selection completes startup");
            }
            using (LanguageSelection chooser = new LanguageSelection())
            {
                chooser.Choose(AppLanguage.English);
                Check(chooser.DialogResult == DialogResult.OK && chooser.SelectedLanguage == AppLanguage.English, "English selection completes startup");
            }
            foreach (LanguageOption option in LanguageCatalog.Options)
            using (LanguageSelection chooser = new LanguageSelection(option.Locale))
            {
                chooser.ChooseLocale(option.Locale);
                Check(chooser.SelectedLocale == option.Locale && chooser.SelectedLanguage == option.InterfaceLanguage, "country/language selection: " + option.Locale);
            }
            prefs = store.Load(); prefs.Locale = "ja-JP"; store.Save(prefs);
            Check(store.Load().Locale == "ja-JP", "selected locale persists alongside existing preferences");
            Localizer korean = new Localizer(AppLanguage.Korean), english = new Localizer(AppLanguage.English);
            Check(korean.Text("Start with Insadong") == "여행 시작: 인사동" && korean.Text("View saved places (2)") == "저장한 장소 보기 (2)", "composed labels translated without losing values");
            Check(english.Text("Search Google Maps") == "Search Google Maps", "English text preserved");
            Check(data.All(r=>korean.Text(r.Description) != r.Description && korean.Text(r.Menu) != r.Menu && korean.Text(r.Note) != r.Note), "all six restaurant descriptions menus and notes localized");
            foreach (AppLanguage chosen in new[] { AppLanguage.Korean, AppLanguage.English })
            using (MainWindow app = new MainWindow(prefsPath, chosen))
            {
                Check(app.CurrentLanguage == chosen, "selected language applied: " + chosen);
                foreach (string page in new[] { "Home", "Maps", "Food", "Transport", "Essentials", "Travel Help", "Saved places" })
                {
                    app.Navigate(page); Check(app.CurrentPage == page && app.VisibleCardCount > 0, "offline view: " + chosen + " / " + page);
                    Check(HasText(app, chosen == AppLanguage.Korean ? korean.Text(page) : page), "localized navigation: " + chosen + " / " + page);
                    Check(((SymbolBadge)app.Controls.Find("PageSymbol",true).Single()).Kind == Symbols.ForPage(page),"page symbol: "+chosen+" / "+page);
                    Check(app.Controls.Find("GlobalSeoulAreaSelector",true).Length==0 && !HasText(app,"여행할 서울 지역"),"Seoul selector removed on "+chosen+" / "+page);
                    Check(((ScenicHeader)app.Controls.Find("KoreanScenicHeader",true).Single()).Page==page,"Korean scenery follows page: "+chosen+" / "+page);
                    for(int pass=0;pass<2;pass++)LayoutTree(app);
                    Control[] pageCards=app.Controls.Find("PageContent",true).Single().Controls.OfType<FlowLayoutPanel>().Cast<Control>().ToArray();
                    Check(pageCards.Length==0 || pageCards.Select(c=>c.Width).Distinct().Count()==1,"consistent card widths: "+chosen+" / "+page);
                }
                app.Navigate("Maps");
                Check(HasText(app, chosen == AppLanguage.Korean ? "이동방법 선택 후 지도를 선택해 주세요" : "Choose a map for this travel method"), "map action matches language: " + chosen);
                Check(HasText(app,chosen == AppLanguage.Korean ? "출발지는 여기에 입력" : "Enter your starting point here") && HasText(app,chosen == AppLanguage.Korean ? "도착지는 여기에 입력" : "Enter your destination here"),"explicit origin and destination guides: "+chosen);
                Check(app.Controls.Find("OriginInput",true).Length == 1 && app.Controls.Find("DestinationInput",true).Length == 1,"separate journey input fields: "+chosen);
                Check(app.Controls.Find("TravelMethods",true).Single().Controls.Count == 4,"four transport selectors: "+chosen);
                Check(RoutePlanner.Providers.All(p=>((Button)app.Controls.Find("ShowRoute-"+p,true).Single()).Image!=null),"route providers include brand symbols: "+chosen);
                Check(app.Controls.Find("RouteProviders",true).Single().Controls.Cast<Control>().Select(c=>(string)c.Tag).SequenceEqual(new[]{"naver","kakao","google"}),"three provider buttons in requested order: "+chosen);
                Check(app.Controls.Find("ShowRoute-naver",true).Length == 1 && app.Controls.Find("RouteRequestSummary",true).Length == 1,"route action and live request summary: "+chosen);
                app.CreateControl(); for (int pass=0;pass<3;pass++) LayoutTree(app);
                Control routeCard = app.Controls.Find("RoutePlanner",true).Single();
                Control routeAction = app.Controls.Find("ShowRoute-naver",true).Single();
                int actionBottom = routeAction.Bottom; for (Control parent=routeAction.Parent;parent!=routeCard.Parent;parent=parent.Parent) actionBottom += parent.Top;
                Check(actionBottom <= routeCard.Parent.ClientSize.Height,"route action visible in initial viewport: "+chosen);
                Check(new[] {"OriginInput","DestinationInput"}.All(name=> { Control field=app.Controls.Find(name,true).Single(); return field.Bottom <= field.Parent.ClientSize.Height && field.Right <= field.Parent.ClientSize.Width; }),"both journey inputs fit their field groups: "+chosen);
                app.Navigate("Food");
                Check(FoodCatalog.Types.All(t=>((Button)app.Controls.Find("FoodType-"+t.Key,true).Single()).Image!=null),"all twelve food buttons have symbols: "+chosen);
                Check(app.Controls.Find("NationalRegionPicker",true).Length==1 && app.Controls.Find("GlobalSeoulAreaSelector",true).Length==0,"nationwide food selector without global Seoul selector: "+chosen);
                Check(RoutePlanner.Providers.All(p=>((Button)app.Controls.Find("FoodSearch-"+p,true).Single()).Image!=null),"three named food map buttons have brand symbols: "+chosen);
                Check(HasText(app, chosen == AppLanguage.Korean ? "장소 저장" : "Save place"), "food action matches language: " + chosen);
                for(int pass=0;pass<3;pass++)LayoutTree(app);
                Control foodTypes=app.Controls.Find("FoodTypes",true).Single();
                Control regionSection=app.Controls.Find("RegionalFoodSection",true).Single(),totalSection=app.Controls.Find("TotalFoodSection",true).Single();
                Check(regionSection.Parent==totalSection.Parent && regionSection.Bottom<=totalSection.Top && regionSection.BackColor!=totalSection.BackColor,"regional search above separate total search card: "+chosen);
                Check(foodTypes.Parent==regionSection && app.Controls.Find("NationalRegionPicker",true).Single().Parent==regionSection && app.Controls.Find("FoodSearchProviders",true).Single().Parent==regionSection && app.Controls.Find("TotalFoodQuery",true).Single().Parent==totalSection && app.Controls.Find("TotalFoodProviders",true).Single().Parent==totalSection,"each search keeps its own inputs and providers together: "+chosen);
                Check(regionSection.Controls[0].Text.StartsWith("①") && totalSection.Controls[0].Text.StartsWith("②") && regionSection.Controls[0].Font.Size>=17 && totalSection.Controls[0].Font.Size>=17 && app.Controls.Find("TotalFoodQuery",true).Single().Font.Size>=12,"numbered guidance and larger type: "+chosen);
                Check(foodTypes.Controls.Cast<Control>().All(b=>b.Right<=foodTypes.ClientSize.Width && b.Bottom<=foodTypes.ClientSize.Height),"all food types fit wrapping layout: "+chosen);
                var foodPicker=(RegionPicker)app.Controls.Find("NationalRegionPicker",true).Single();
                app.Show();Application.DoEvents();
                TextBox total=(TextBox)app.Controls.Find("TotalFoodQuery",true).Single();
                Check(app.Controls.Find("TotalFoodProviders",true).Single().Controls.Cast<Control>().All(b=>!b.Enabled),"empty total search buttons disabled: "+chosen);
                string beforeTotal=app.Controls.Find("FoodSearchSummary",true).Single().Text;total.Text="홍대 맛집";
                Check(app.Controls.Find("TotalFoodProviders",true).Single().Controls.Cast<Control>().All(b=>b.Enabled) && app.Controls.Find("FoodSearchSummary",true).Single().Text==beforeTotal,"total search independent from regional results: "+chosen);
                Check(app.Controls.Find("FoodVisitChecklist",true).Single().Controls.Cast<Control>().Count(c=>c.Text.StartsWith("🕒") || c.Text.StartsWith("💳") || c.Text.StartsWith("🥜"))==3,"three practical visitor highlights with emoji: "+chosen);
                total.Text="";
                foodPicker.SetSelection(new RegionSelection{Province="강원특별자치도",City="춘천시"});
                ((Button)app.Controls.Find("FoodType-stirfry",true).Single()).PerformClick();
                Check(app.Controls.Find("FoodSearchSummary",true).Single().Text.Contains("강원특별자치도 춘천시 볶음요리 맛집") && HasText(app,"춘천 명동 닭갈비골목"),"region and food clicks update results and map summary: "+chosen);
                Check(store.Load().FoodCategory=="stirfry" && store.Load().FoodRegion.City=="춘천시","UI food filters persist: "+chosen);
                foodPicker.SetSelection(new RegionSelection());((Button)app.Controls.Find("FoodType-all",true).Single()).PerformClick();
                app.Navigate("Home");
                Check(HasText(app, chosen == AppLanguage.Korean ? "한국 여행을 시작해요." : "Let's explore Korea."), "home heading matches language: " + chosen);
                foreach (Size size in new[] { new Size(1000, 650), new Size(1120, 760), new Size(1366, 768) }) CheckHomeLayout(app, size);
                app.Scale(new SizeF(1.25f, 1.25f));
                CheckHomeLayout(app, new Size(1280, 850));
            }
            string root = Path.Combine(sandbox, "Programs", "WelcomeKorea"), menu = Path.Combine(sandbox, "StartMenu");
            Installer installer = new Installer(root, menu, false); installer.Install();
            Check(File.Exists(Path.Combine(root, "WelcomeKorea.exe")) && File.Exists(Path.Combine(root, "WelcomeKoreaSetup.exe")), "installer extracts app and uninstaller");
            Check(File.Exists(Path.Combine(menu, "Welcome Korea.lnk")) && File.Exists(Path.Combine(menu, "Uninstall Welcome Korea.lnk")), "installer creates both shortcuts");
            Check(File.ReadAllBytes(Path.Combine(root, "WelcomeKorea.exe")).SequenceEqual(File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WelcomeKorea.exe"))), "installer payload matches portable app");
            installer.Install(); Check(File.Exists(Path.Combine(root, "installed.txt")), "repeat install updates successfully");
            File.WriteAllText(Path.Combine(root, "keep-user-file.txt"), "keep"); installer.Remove();
            Check(!File.Exists(Path.Combine(root, "WelcomeKorea.exe")) && !File.Exists(Path.Combine(menu, "Welcome Korea.lnk")), "uninstall removes app and shortcuts");
            Check(File.Exists(Path.Combine(root, "keep-user-file.txt")) && File.Exists(prefsPath), "uninstall preserves unrelated files and preferences");
            string foreign = Path.Combine(sandbox, "OtherApp"); Directory.CreateDirectory(foreign); File.WriteAllText(Path.Combine(foreign, "keep.txt"), "user data");
            bool rejected = false; try { new Installer(foreign, menu, false).Install(); } catch (IOException) { rejected = true; }
            Check(rejected && File.ReadAllText(Path.Combine(foreign, "keep.txt")) == "user data", "installer rejects unowned nonempty target");
            // Remove only the exact files created by this test; never recursively delete directories.
            foreach (string file in new[] { prefsPath, Path.Combine(root, "keep-user-file.txt"), Path.Combine(foreign, "keep.txt") }) File.Delete(file);
            foreach (string dir in new[] { root, Path.GetDirectoryName(root), foreign, sandbox }) if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0) Directory.Delete(dir);
            Console.WriteLine(count + " checks passed. No real user installation or network access was performed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
