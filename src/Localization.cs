using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WelcomeKorea
{
    public enum AppLanguage { Korean, English }

    public sealed class LanguageOption
    {
        public string Locale, DisplayName, Country;
        public AppLanguage InterfaceLanguage;
        public bool UsesEnglishPreview;
        public override string ToString() { return DisplayName; }
    }

    public static class LanguageCatalog
    {
        static LanguageOption Option(string locale, string label, string country)
        {
            bool korean = locale == "ko-KR", english = locale.StartsWith("en-", StringComparison.Ordinal);
            return new LanguageOption { Locale = locale, DisplayName = label, Country = country, InterfaceLanguage = korean ? AppLanguage.Korean : AppLanguage.English, UsesEnglishPreview = !korean && !english };
        }
        // A curated set of 50 country/region language entries, not a visitor ranking.
        // Countries sharing a language are kept separate so the country choice is explicit.
        public static readonly LanguageOption[] Options = {
            Option("ko-KR", "한국어", "대한민국 / South Korea"),
            Option("en-US", "ENGLISH", "United States"),
            Option("ja-JP", "日本語 — Japanese / Japan", "Japan"),
            Option("zh-CN", "简体中文 — Chinese / China", "China"),
            Option("zh-TW", "繁體中文 — Chinese / Taiwan", "Taiwan"),
            Option("zh-HK", "繁體中文 — Chinese / Hong Kong", "Hong Kong"),
            Option("en-GB", "English — United Kingdom", "United Kingdom"),
            Option("fr-FR", "Français — French / France", "France"),
            Option("de-DE", "Deutsch — German / Germany", "Germany"),
            Option("es-ES", "Español — Spanish / Spain", "Spain"),
            Option("it-IT", "Italiano — Italian / Italy", "Italy"),
            Option("pt-PT", "Português — Portuguese / Portugal", "Portugal"),
            Option("pt-BR", "Português — Portuguese / Brazil", "Brazil"),
            Option("ru-RU", "Русский — Russian / Russia", "Russia"),
            Option("uk-UA", "Українська — Ukrainian / Ukraine", "Ukraine"),
            Option("pl-PL", "Polski — Polish / Poland", "Poland"),
            Option("cs-CZ", "Čeština — Czech / Czechia", "Czechia"),
            Option("hu-HU", "Magyar — Hungarian / Hungary", "Hungary"),
            Option("ro-RO", "Română — Romanian / Romania", "Romania"),
            Option("el-GR", "Ελληνικά — Greek / Greece", "Greece"),
            Option("tr-TR", "Türkçe — Turkish / Türkiye", "Türkiye"),
            Option("ar-SA", "العربية — Arabic / Saudi Arabia", "Saudi Arabia"),
            Option("ar-AE", "العربية — Arabic / United Arab Emirates", "United Arab Emirates"),
            Option("he-IL", "עברית — Hebrew / Israel", "Israel"),
            Option("fa-IR", "فارسی — Persian / Iran", "Iran"),
            Option("hi-IN", "हिन्दी — Hindi / India", "India"),
            Option("bn-BD", "বাংলা — Bengali / Bangladesh", "Bangladesh"),
            Option("ur-PK", "اردو — Urdu / Pakistan", "Pakistan"),
            Option("ne-NP", "नेपाली — Nepali / Nepal", "Nepal"),
            Option("th-TH", "ไทย — Thai / Thailand", "Thailand"),
            Option("vi-VN", "Tiếng Việt — Vietnamese / Vietnam", "Vietnam"),
            Option("id-ID", "Bahasa Indonesia — Indonesian / Indonesia", "Indonesia"),
            Option("ms-MY", "Bahasa Melayu — Malay / Malaysia", "Malaysia"),
            Option("fil-PH", "Filipino — Philippines", "Philippines"),
            Option("en-SG", "English — Singapore", "Singapore"),
            Option("km-KH", "ខ្មែរ — Khmer / Cambodia", "Cambodia"),
            Option("lo-LA", "ລາວ — Lao / Laos", "Laos"),
            Option("my-MM", "မြန်မာ — Burmese / Myanmar", "Myanmar"),
            Option("mn-MN", "Монгол — Mongolian / Mongolia", "Mongolia"),
            Option("kk-KZ", "Қазақша — Kazakh / Kazakhstan", "Kazakhstan"),
            Option("uz-UZ", "O‘zbekcha — Uzbek / Uzbekistan", "Uzbekistan"),
            Option("nl-NL", "Nederlands — Dutch / Netherlands", "Netherlands"),
            Option("nl-BE", "Nederlands — Dutch / Belgium", "Belgium"),
            Option("de-CH", "Deutsch — German / Switzerland", "Switzerland"),
            Option("de-AT", "Deutsch — German / Austria", "Austria"),
            Option("sv-SE", "Svenska — Swedish / Sweden", "Sweden"),
            Option("nb-NO", "Norsk bokmål — Norwegian / Norway", "Norway"),
            Option("da-DK", "Dansk — Danish / Denmark", "Denmark"),
            Option("fi-FI", "Suomi — Finnish / Finland", "Finland"),
            Option("en-AU", "English — Australia", "Australia")
        };
        public static LanguageOption Find(string locale)
        {
            foreach (LanguageOption option in Options) if (option.Locale == locale) return option;
            throw new ArgumentException("Unknown language/country selection: " + locale);
        }
    }

    public sealed class Localizer
    {
        readonly Dictionary<string, string> translations;
        public readonly AppLanguage Language;
        public Localizer(AppLanguage language)
        {
            Language = language;
            using (Stream stream = typeof(Localizer).Assembly.GetManifestResourceStream("ko.json"))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                translations = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
        }
        public string Text(string text)
        {
            if (Language != AppLanguage.Korean || String.IsNullOrEmpty(text)) return text;
            string translated;
            if (translations.TryGetValue(text, out translated)) return translated;
            // Translate composed labels one line at a time, preserving names and addresses.
            string[] lines = text.Split('\n');
            if (lines.Length > 1) { for (int i = 0; i < lines.Length; i++) lines[i] = Text(lines[i]); return String.Join("\n", lines); }
            foreach (string prefix in new[] { "Start with ", "View saved places (", "Transit to ", "Try: ", "Source: " })
                if (text.StartsWith(prefix, StringComparison.Ordinal)) return Text(prefix) + Text(text.Substring(prefix.Length));
            string[] segments = text.Split(new[] { "  ·  ", "  |  " }, StringSplitOptions.None);
            if (segments.Length > 1) { for (int i = 0; i < segments.Length; i++) segments[i] = Text(segments[i]); return String.Join("  ·  ", segments); }
            if (text.StartsWith("Checked ", StringComparison.Ordinal)) return "확인일 " + text.Substring(8);
            return text;
        }
        public void TranslateTree(Control root)
        {
            // User input and combo values remain unchanged, so search keys stay stable.
            if (!(root is TextBox) && !(root is ComboBox)) root.Text = Text(root.Text);
            foreach (Control child in root.Controls) TranslateTree(child);
        }
    }

    public sealed class LanguageSelection : Form
    {
        readonly ComboBox languages = new ComboBox();
        readonly Label support = new Label();
        public AppLanguage SelectedLanguage { get; private set; }
        public string SelectedLocale { get; private set; }
        public int OptionCount { get { return languages.Items.Count; } }
        public string SupportNotice { get { return support.Text; } }
        public LanguageSelection() : this("ko-KR") { }
        public LanguageSelection(string currentLocale)
        {
            Text = "Welcome Korea · 언어 선택 / Language";
            DoubleBuffered=true;Paint+=delegate(object sender,PaintEventArgs e){KoreanScenery.Draw(e.Graphics,ClientRectangle,"Home",false);};
            Font = new Font("Malgun Gothic", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(720, 490);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(244, 247, 246);
            Controls.Add(new Label { Text = "WELCOME KOREA", ForeColor = Color.FromArgb(24,49,58), Font = new Font("Segoe UI", 29, FontStyle.Bold), Location = new Point(30, 25), Size = new Size(660, 65), TextAlign = ContentAlignment.MiddleCenter });
            Controls.Add(new Label { Text = "언어를 선택해 주세요 · 50개 국가·지역\nChoose a language / country from the list.", ForeColor = Color.FromArgb(91,108,115), Location = new Point(30, 105), Size = new Size(660, 68), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Malgun Gothic", 13) });
            languages.Name = "LanguageDropdown"; languages.AccessibleName = "언어 / Language";
            languages.DropDownStyle = ComboBoxStyle.DropDownList; languages.Sorted = false;
            languages.SetBounds(50, 195, 620, 38); languages.Font = new Font("Malgun Gothic", 13);
            languages.DropDownWidth = 620; languages.IntegralHeight = false; languages.DropDownHeight = 320;
            languages.Items.AddRange(LanguageCatalog.Options);
            languages.SelectedIndexChanged += delegate { RefreshNotice(); };
            Controls.Add(languages); support.SetBounds(50, 249, 620, 88); support.ForeColor = Color.FromArgb(91,108,115); Controls.Add(support);
            SetSelection(currentLocale);
            Button start = new Button { Name = "ContinueButton", Text = "시작하기 / Continue", Location = new Point(50, 349), Size = new Size(620, 57), BackColor = Color.FromArgb(18,112,104), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Malgun Gothic", 14, FontStyle.Bold), Cursor = Cursors.Hand };
            start.FlatAppearance.BorderSize = 0; start.Click += delegate { ChooseLocale(((LanguageOption)languages.SelectedItem).Locale); }; Controls.Add(start); AcceptButton = start;
            Controls.Add(new Label { Text = "국가별 대표 언어를 담은 테스트 목록입니다.\nTest list of representative languages; countries may share a language.", ForeColor = Color.FromArgb(91,108,115), Location = new Point(30, 425), Size = new Size(660, 50), TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Malgun Gothic", 9) });
            foreach(Control child in Controls)if(child is Label)child.BackColor=Color.Transparent;
        }
        public void SetSelection(string locale)
        {
            languages.SelectedItem = LanguageCatalog.Find(locale);
        }
        void RefreshNotice()
        {
            LanguageOption option = (LanguageOption)languages.SelectedItem;
            if (option == null) return;
            support.Text = option.Country + " · " + option.Locale + "\n" + (option.UsesEnglishPreview
                ? "이 언어의 번역은 준비 중입니다. 이번 테스트에서는 영어 화면으로 열립니다.\nTranslation pending. This preview opens the interface in English."
                : option.InterfaceLanguage == AppLanguage.Korean ? "한국어 화면으로 시작합니다.\nThe interface will open in Korean." : "영어 화면으로 시작합니다.\nThe interface will open in English.");
        }
        public void Choose(AppLanguage language)
        {
            ChooseLocale(language == AppLanguage.Korean ? "ko-KR" : "en-US");
        }
        public void ChooseLocale(string locale)
        {
            LanguageOption option = LanguageCatalog.Find(locale);
            SelectedLocale = option.Locale;
            SelectedLanguage = option.InterfaceLanguage;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
