using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace WelcomeKorea
{
    public sealed class FoodType
    {
        public string Key,Label,Symbol,SearchTerm;
    }
    public sealed class FoodStreet
    {
        public string Id,Name,EnglishName,LocationPath,Description,DescriptionKo,Source;
        public string[] FoodTypes;
    }
    public static class FoodCatalog
    {
        public static readonly FoodType[] Types={
            new FoodType{Key="all",Label="All food",Symbol="food",SearchTerm=""},
            new FoodType{Key="soup",Label="Soups & stews",Symbol="soup",SearchTerm="국물요리"},
            new FoodType{Key="stirfry",Label="Stir-fries",Symbol="wok",SearchTerm="볶음요리"},
            new FoodType{Key="snacks",Label="Korean snacks",Symbol="snacks",SearchTerm="분식"},
            new FoodType{Key="grill",Label="Grilled food",Symbol="grill",SearchTerm="고기구이"},
            new FoodType{Key="noodles",Label="Noodles",Symbol="noodles",SearchTerm="면요리"},
            new FoodType{Key="rice",Label="Rice & set meals",Symbol="rice",SearchTerm="백반"},
            new FoodType{Key="seafood",Label="Seafood",Symbol="fish",SearchTerm="해산물"},
            new FoodType{Key="braised",Label="Braised dishes",Symbol="pot",SearchTerm="찜 조림"},
            new FoodType{Key="western",Label="Western food",Symbol="pizza",SearchTerm="양식"},
            new FoodType{Key="cafe",Label="Cafes & desserts",Symbol="coffee",SearchTerm="카페 디저트"},
            new FoodType{Key="vegetarian",Label="Vegetarian",Symbol="leaf",SearchTerm="채식"}
        };
        public static readonly List<FoodStreet> Streets=LoadStreets();
        static List<FoodStreet> LoadStreets()
        {
            using(Stream stream=typeof(FoodCatalog).Assembly.GetManifestResourceStream("food-streets.json"))
            using(StreamReader reader=new StreamReader(stream,Encoding.UTF8))return new JavaScriptSerializer().Deserialize<List<FoodStreet>>(reader.ReadToEnd());
        }
        public static FoodType Type(string key) { FoodType type=Types.FirstOrDefault(t=>t.Key==key);if(type==null)throw new ArgumentException("Unknown food category");return type; }
        public static string Query(RegionSelection selection,string foodKey,string keyword)
        {
            FoodType type=Type(foodKey);
            return String.Join(" ",new[]{selection.Scope.Length==0?"대한민국":selection.Scope,selection.Detail,type.SearchTerm,"맛집",(keyword??"").Trim()}.Where(s=>!String.IsNullOrWhiteSpace(s)));
        }
        public static string SearchUrl(string provider,RegionSelection selection,string foodKey,string keyword)
        {
            return TotalSearchUrl(provider,Query(selection,foodKey,keyword));
        }
        public static string TotalSearchUrl(string provider,string keyword)
        {
            if(String.IsNullOrWhiteSpace(keyword))throw new ArgumentException("Enter a search term");
            string query=Uri.EscapeDataString(keyword.Trim());
            switch(provider){case "naver":return "https://map.naver.com/p/search/"+query;case "kakao":return "https://map.kakao.com/link/search/"+query;case "google":return "https://www.google.com/maps/search/?api=1&query="+query;default:throw new ArgumentException("Unknown map provider");}
        }
        public static IEnumerable<Restaurant> Filter(IEnumerable<Restaurant> items,RegionSelection selection,string foodKey,string keyword)
        {
            Type(foodKey);string query=(keyword??"").Trim();
            return items.Where(r=>selection.Contains(r.LocationPath??"") && (selection.Detail.Length==0 || ((r.LocationPath??"")+" "+r.Name+" "+r.Korean).Contains(selection.Detail)) && (foodKey=="all" || (r.FoodTypes??new string[0]).Contains(foodKey)) && (query.Length==0 || (r.Name+" "+r.Korean+" "+r.Cuisine+" "+r.Menu).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0));
        }
        public static IEnumerable<FoodStreet> FilterStreets(RegionSelection selection,string foodKey,string keyword)
        {
            Type(foodKey);string query=(keyword??"").Trim();
            return Streets.Where(s=>selection.Contains(s.LocationPath) && (selection.Detail.Length==0 || s.Name==selection.Detail || s.LocationPath.EndsWith(" "+selection.Detail)) && (foodKey=="all" || s.FoodTypes.Contains(foodKey)) && (query.Length==0 || (s.Name+" "+s.EnglishName+" "+s.Description+" "+s.DescriptionKo).IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0));
        }
    }
}
