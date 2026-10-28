using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WelcomeKorea
{
    public sealed class GeoPlace
    {
        public string Name, Address;
        public double Latitude, Longitude;
        public string Coordinates { get { return Latitude.ToString("0.#######",CultureInfo.InvariantCulture)+","+Longitude.ToString("0.#######",CultureInfo.InvariantCulture); } }
        public void Validate()
        {
            if (String.IsNullOrWhiteSpace(Name) || Double.IsNaN(Latitude) || Double.IsInfinity(Latitude) || Double.IsNaN(Longitude) || Double.IsInfinity(Longitude) || Latitude<32 || Latitude>39.5 || Longitude<124 || Longitude>132.2) throw new ArgumentException("Confirm a place in South Korea before opening directions.");
        }
    }
    public static class PlaceSearch
    {
        static readonly object Gate = new object();
        static readonly Dictionary<string,List<GeoPlace>> Cache = new Dictionary<string,List<GeoPlace>>(StringComparer.OrdinalIgnoreCase);
        static DateTime lastRequest = DateTime.MinValue;
        public static List<GeoPlace> Search(string query)
        {
            if (String.IsNullOrWhiteSpace(query)) throw new ArgumentException("Enter both places before searching.");
            query = query.Trim();
            lock (Gate)
            {
                List<GeoPlace> cached;
                if (Cache.TryGetValue(query,out cached)) return cached;
                double remaining = 1100-(DateTime.UtcNow-lastRequest).TotalMilliseconds;
                if (remaining>0) Thread.Sleep((int)remaining);
                string endpoint = Environment.GetEnvironmentVariable("WELCOME_KOREA_PHOTON_URL");
                if (String.IsNullOrWhiteSpace(endpoint)) endpoint = "https://photon.komoot.io/api/";
                Uri root;
                if (!Uri.TryCreate(endpoint,UriKind.Absolute,out root) || root.Scheme!="https" || root.Query.Length>0) throw new InvalidOperationException("The place-search server setting is invalid.");
                string url = root.AbsoluteUri+"?q="+Uri.EscapeDataString(query)+"&limit=6&countrycode=KR";
                lastRequest=DateTime.UtcNow;
                string json=ReadWithWindowsCurl(url);
                List<GeoPlace> results=Parse(json,query);
                if (Cache.Count>=50) Cache.Clear();
                Cache[query]=results;
                return results;
            }
        }
        static string ReadWithWindowsCurl(string url)
        {
            // Use the Windows-bundled HTTPS client to avoid legacy Framework TLS limits.
            // Certificate validation stays enabled. No shell or command interpreter is used.
            string curl=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"curl.exe");
            if(!File.Exists(curl)) throw new InvalidOperationException("Place search requires a current Windows HTTPS client.");
            Uri uri=new Uri(url);
            if(uri.Scheme!="https" || url.Contains("\"") || url.Contains("\r") || url.Contains("\n")) throw new InvalidOperationException("Invalid place-search URL.");
            ProcessStartInfo info=new ProcessStartInfo(curl,"--fail --silent --show-error --location --proto =https --proto-redir =https --connect-timeout 5 --max-time 15 --user-agent WelcomeKorea-LocalPreview/1.8 \""+uri.AbsoluteUri+"\"") { UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8 };
            using(Process process=Process.Start(info))
            {
                string body=process.StandardOutput.ReadToEnd();process.StandardError.ReadToEnd();process.WaitForExit();
                if(process.ExitCode!=0 || body.Length>2000000) throw new InvalidOperationException("Place search request failed.");
                return body;
            }
        }
        static string Value(Dictionary<string,object> values,string key)
        {
            object value; return values.TryGetValue(key,out value) && value!=null ? Convert.ToString(value,CultureInfo.InvariantCulture) : "";
        }
        public static List<GeoPlace> Parse(string json,string query)
        {
            List<GeoPlace> places=new List<GeoPlace>();
            Dictionary<string,object> root=new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string,object>;
            if (root==null || !root.ContainsKey("features")) throw new InvalidDataException("The place-search response was invalid.");
            object[] features=root["features"] as object[];
            if (features==null) throw new InvalidDataException("The place-search response was invalid.");
            foreach (object item in features)
            {
                try
                {
                    var feature=(Dictionary<string,object>)item;
                    var props=(Dictionary<string,object>)feature["properties"];
                    if (!String.Equals(Value(props,"countrycode"),"KR",StringComparison.OrdinalIgnoreCase)) continue;
                    var geometry=(Dictionary<string,object>)feature["geometry"];
                    if (Value(geometry,"type")!="Point") continue;
                    object[] coordinates=(object[])geometry["coordinates"];
                    if (coordinates.Length!=2) continue;
                    string name=Value(props,"name");
                    GeoPlace place=new GeoPlace { Name=String.IsNullOrWhiteSpace(name)?query:name, Longitude=Convert.ToDouble(coordinates[0],CultureInfo.InvariantCulture), Latitude=Convert.ToDouble(coordinates[1],CultureInfo.InvariantCulture), Address=String.Join(", ",new[]{Value(props,"housenumber"),Value(props,"street"),Value(props,"district"),Value(props,"city"),Value(props,"state"),Value(props,"country")}.Where(s=>s.Length>0).Distinct()) };
                    place.Validate();
                    if (!places.Any(p=>p.Name==place.Name && p.Coordinates==place.Coordinates)) places.Add(place);
                }
                catch (Exception ex) { if (!(ex is ArgumentException || ex is InvalidCastException || ex is KeyNotFoundException || ex is FormatException || ex is OverflowException)) throw; }
            }
            return places;
        }
    }
    public sealed class PlaceConfirmation : Form
    {
        readonly Localizer language;
        readonly string originQuery,destinationQuery;
        readonly ListView starts=new ListView(), ends=new ListView();
        readonly Button search=new Button(), confirm=new Button();
        readonly Label status=new Label();
        bool loading;
        public GeoPlace StartPlace { get; private set; }
        public GeoPlace EndPlace { get; private set; }
        public PlaceConfirmation(string origin,string destination,AppLanguage selectedLanguage)
        {
            language=new Localizer(selectedLanguage); originQuery=origin; destinationQuery=destination;
            Text=T("Confirm the exact places"); Font=new Font("Malgun Gothic",10); ClientSize=new Size(840,660); MinimumSize=new Size(760,600); AutoScaleMode=AutoScaleMode.Dpi; StartPosition=FormStartPosition.CenterParent;
            TableLayoutPanel layout=new TableLayoutPanel { Dock=DockStyle.Fill,Padding=new Padding(22),ColumnCount=1,RowCount=8 };
            foreach (float height in new[]{82f,44f,30f}) layout.RowStyles.Add(new RowStyle(SizeType.Absolute,height));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,50));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,30));layout.RowStyles.Add(new RowStyle(SizeType.Percent,50));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,49));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
            Controls.Add(layout);
            layout.Controls.Add(new Label { Text=T("Search sends the two place names to Photon (OpenStreetMap). Select the correct address for each place. Nothing is selected automatically."),Dock=DockStyle.Fill,ForeColor=Color.FromArgb(65,88,82) },0,0);
            search.Text=T("Search both places");search.Name="SearchBothPlaces";search.Dock=DockStyle.Left;search.Width=230; search.Click+=delegate { LoadCandidates(); };layout.Controls.Add(search,0,1);
            layout.Controls.Add(new Label { Text=T("Starting point")+": "+origin,Dock=DockStyle.Fill },0,2);ConfigureList(starts);starts.Name="StartCandidates";layout.Controls.Add(starts,0,3);
            layout.Controls.Add(new Label { Text=T("Destination")+": "+destination,Dock=DockStyle.Fill },0,4);ConfigureList(ends);ends.Name="EndCandidates";layout.Controls.Add(ends,0,5);
            confirm.Text=T("Use these two places");confirm.Name="ConfirmBothPlaces";confirm.Enabled=false;confirm.Dock=DockStyle.Left;confirm.Width=250;
            confirm.Click+=delegate { if (starts.SelectedItems.Count==1 && ends.SelectedItems.Count==1) { StartPlace=(GeoPlace)starts.SelectedItems[0].Tag;EndPlace=(GeoPlace)ends.SelectedItems[0].Tag;DialogResult=DialogResult.OK;Close(); } };layout.Controls.Add(confirm,0,6);
            status.Text=T("Click Search both places to begin. Data: OpenStreetMap contributors / Photon.");status.Dock=DockStyle.Fill;layout.Controls.Add(status,0,7);
            starts.SelectedIndexChanged+=delegate { UpdateConfirmation(); };ends.SelectedIndexChanged+=delegate { UpdateConfirmation(); };
        }
        string T(string value) { return language.Text(value); }
        void ConfigureList(ListView list)
        {
            list.Dock=DockStyle.Fill;list.View=View.Details;list.FullRowSelect=true;list.MultiSelect=false;list.HideSelection=false;list.Columns.Add(T("Place"),200);list.Columns.Add(T("Address"),400);list.Columns.Add(T("Coordinates"),160);
        }
        void UpdateConfirmation() { confirm.Enabled=!loading && starts.SelectedItems.Count==1 && ends.SelectedItems.Count==1; }
        static void Fill(ListView list,List<GeoPlace> places)
        {
            list.Items.Clear();foreach(GeoPlace place in places) { ListViewItem row=new ListViewItem(new[]{place.Name,place.Address,place.Coordinates});row.Tag=place;list.Items.Add(row); }
        }
        void LoadCandidates()
        {
            if (loading) return;loading=true;search.Enabled=false;confirm.Enabled=false;starts.Items.Clear();ends.Items.Clear();status.Text=T("Searching the two places...");
            BackgroundWorker worker=new BackgroundWorker();
            worker.DoWork+=delegate(object sender,DoWorkEventArgs e) { e.Result=new[]{PlaceSearch.Search(originQuery),PlaceSearch.Search(destinationQuery)}; };
            worker.RunWorkerCompleted+=delegate(object sender,RunWorkerCompletedEventArgs e)
            {
                worker.Dispose();if(IsDisposed) return;loading=false;search.Enabled=true;
                if(e.Error!=null) { status.Text=T("Place search failed. Check your connection or try a fuller address.");return; }
                List<GeoPlace>[] results=(List<GeoPlace>[])e.Result;Fill(starts,results[0]);Fill(ends,results[1]);
                status.Text=T(results.Any(r=>r.Count==0)?"No matching place found. Close this window and try a fuller address or Korean place name.":"Select one starting point and one destination, then confirm their addresses.");UpdateConfirmation();
            };
            worker.RunWorkerAsync();
        }
    }
}
