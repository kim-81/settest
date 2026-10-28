using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WelcomeKorea
{
    public sealed class RegionSelection
    {
        public string Province="", City="", District="", Neighborhood="", Detail="";
        public string Scope { get { return String.Join(" ",new[]{Province,City,District,Neighborhood}.Where(v=>!String.IsNullOrWhiteSpace(v))); } }
        public bool Contains(string path) { return String.IsNullOrEmpty(Scope) || path==Scope || path.StartsWith(Scope+" ",StringComparison.Ordinal); }
    }
    public sealed class RegionNode
    {
        public string Code, Name;
        public string[] Parts;
    }
    public static class RegionCatalog
    {
        public static readonly List<RegionNode> Nodes=Load();
        static List<RegionNode> Load()
        {
            List<RegionNode> result=new List<RegionNode>();
            using(Stream stream=typeof(RegionCatalog).Assembly.GetManifestResourceStream("regions.tsv"))
            using(StreamReader reader=new StreamReader(stream,Encoding.UTF8))
            {
                string line;while((line=reader.ReadLine())!=null)
                {
                    string[] cells=line.TrimStart('\ufeff').Split('\t');
                    if(cells.Length!=2 || cells[0].Length!=10 || !cells[0].All(Char.IsDigit)) continue;
                    result.Add(new RegionNode{Code=cells[0],Name=cells[1],Parts=cells[1].Split(' ')});
                }
            }
            return result;
        }
        public static string[] Provinces() { return Nodes.Where(n=>n.Parts.Length==1).Select(n=>n.Name).Distinct().ToArray(); }
        public static string[] Cities(string province) { return Nodes.Where(n=>n.Parts[0]==province && n.Parts.Length==2 && n.Code.EndsWith("00000")).Select(n=>n.Parts[1]).Distinct().OrderBy(n=>n,StringComparer.Ordinal).ToArray(); }
        public static string[] Districts(string province,string city) { return Nodes.Where(n=>n.Parts[0]==province && n.Parts.Length==3 && n.Parts[1]==city && n.Code.EndsWith("00000")).Select(n=>n.Parts[2]).Distinct().OrderBy(n=>n,StringComparer.Ordinal).ToArray(); }
        public static string[] Neighborhoods(string province,string city,string district)
        {
            string scope=String.Join(" ",new[]{province,city,district}.Where(n=>n.Length>0));int depth=scope.Split(' ').Length+1;
            return Nodes.Where(n=>n.Name.StartsWith(scope+" ",StringComparison.Ordinal) && n.Parts.Length==depth && n.Code.EndsWith("00") && !n.Code.EndsWith("00000")).Select(n=>n.Parts.Last()).Distinct().OrderBy(n=>n,StringComparer.Ordinal).ToArray();
        }
        public static string[] Villages(RegionSelection selection)
        {
            if(selection.Neighborhood.Length==0) return new string[0];
            return Nodes.Where(n=>n.Name.StartsWith(selection.Scope+" ",StringComparison.Ordinal) && !n.Code.EndsWith("00")).Select(n=>n.Parts.Last()).Distinct().OrderBy(n=>n,StringComparer.Ordinal).ToArray();
        }
    }
    public sealed class RegionPicker : UserControl
    {
        readonly Localizer language;
        readonly ComboBox province=new ComboBox(),city=new ComboBox(),neighborhood=new ComboBox();
        bool updating;
        public event EventHandler SelectionChanged;
        public RegionSelection Selection
        {
            get { string[] area=Value(city).Split(new[]{' '},2);return new RegionSelection{Province=Value(province),City=area[0],District=area.Length>1?area[1]:"",Neighborhood=Value(neighborhood)}; }
        }
        static string Value(ComboBox box) { return box.SelectedIndex>0 ? Convert.ToString(box.SelectedItem) : ""; }
        public RegionPicker(Localizer language)
        {
            this.language=language;Name="NationalRegionPicker";Width=650;Height=76;Margin=Padding.Empty;
            TableLayoutPanel grid=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=3,RowCount=1,Margin=Padding.Empty};
            for(int i=0;i<3;i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/3));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(grid);
            AddField(grid,province,"ProvinceSelect","Province / metro",0,0);AddField(grid,city,"CitySelect","City / county",1,0);AddField(grid,neighborhood,"NeighborhoodSelect","Neighborhood / town",2,0);
            province.SelectedIndexChanged+=delegate { if(updating)return;updating=true;string p=Value(province);Fill(city,RegionCatalog.Cities(p).SelectMany(c=>new[]{c}.Concat(RegionCatalog.Districts(p,c).Select(d=>c+" "+d))));RefreshNeighborhoods();updating=false;Changed(); };
            city.SelectedIndexChanged+=delegate { if(updating)return;updating=true;RefreshNeighborhoods();updating=false;Changed(); };
            neighborhood.SelectedIndexChanged+=delegate { if(!updating) Changed(); };
            updating=true;Fill(province,RegionCatalog.Provinces());Fill(city,new string[0]);Fill(neighborhood,new string[0]);updating=false;
        }
        void AddField(TableLayoutPanel grid,ComboBox box,string name,string label,int col,int row)
        {
            TableLayoutPanel field=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=new Padding(0,0,10,7)};field.RowStyles.Add(new RowStyle(SizeType.Absolute,25));field.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            field.Controls.Add(new Label{Text=language.Text(label),Dock=DockStyle.Fill,Font=new Font("Malgun Gothic",10),ForeColor=Color.FromArgb(91,108,115)},0,0);
            box.Font=new Font("Malgun Gothic",11);box.Name=name;box.AccessibleName=language.Text(label);box.DropDownStyle=ComboBoxStyle.DropDownList;box.AutoCompleteSource=AutoCompleteSource.ListItems;box.AutoCompleteMode=AutoCompleteMode.SuggestAppend;box.Dock=DockStyle.Top;box.DropDownWidth=330;box.IntegralHeight=false;box.DropDownHeight=300;field.Controls.Add(box,0,1);grid.Controls.Add(field,col,row);
        }
        void Fill(ComboBox box,IEnumerable<string> options)
        {
            box.BeginUpdate();box.Items.Clear();box.Items.Add(language.Text("All"));foreach(string option in options.Distinct()) box.Items.Add(option);box.SelectedIndex=0;box.Enabled=box.Items.Count>1;box.EndUpdate();
        }
        void RefreshNeighborhoods()
        {
            RegionSelection selection=Selection;string p=selection.Province,c=selection.City,d=selection.District;
            bool ready=p.Length>0 && (RegionCatalog.Cities(p).Length==0 || c.Length>0) && (RegionCatalog.Districts(p,c).Length==0 || d.Length>0);
            Fill(neighborhood,ready?RegionCatalog.Neighborhoods(p,c,d):new string[0]);
        }
        void Changed() { if(SelectionChanged!=null) SelectionChanged(this,EventArgs.Empty); }
        public void SetSelection(RegionSelection s)
        {
            SelectValue(province,s.Province);SelectValue(city,String.Join(" ",new[]{s.City,s.District}.Where(v=>!String.IsNullOrEmpty(v))));SelectValue(neighborhood,s.Neighborhood);
        }
        static void SelectValue(ComboBox box,string value) { if(String.IsNullOrEmpty(value) || !box.Items.Contains(value))box.SelectedIndex=0;else box.SelectedItem=value; }
    }
}
