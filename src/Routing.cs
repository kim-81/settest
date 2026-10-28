using System;
using System.Globalization;

namespace WelcomeKorea
{
    public sealed class TravelOption
    {
        public string Key, Label, GoogleMode, Symbol, Note;
    }
    public static class RoutePlanner
    {
        public static readonly string[] Providers = { "naver", "kakao", "google" };
        public static readonly TravelOption[] Options = {
            new TravelOption { Key="transit", Label="Public transit", GoogleMode="transit", Symbol="transit", Note="Bus and subway routes are shown together. Set any bus-only or subway-only preference in your selected map." },
            new TravelOption { Key="taxi", Label="Taxi", GoogleMode="driving", Symbol="taxi", Note="Shows a road route for planning a taxi trip. This is not a taxi booking or a live fare estimate." },
            new TravelOption { Key="walking", Label="Walking", GoogleMode="walking", Symbol="walking", Note="Requests a walking route. If unavailable for this area, use the alternative map links below." },
            new TravelOption { Key="bicycling", Label="Bicycle", GoogleMode="bicycling", Symbol="bicycling", Note="Requests a bicycle route. Bicycle routing may be unavailable for this area; check the map before traveling." }
        };
        public static TravelOption Find(string key)
        {
            foreach (TravelOption option in Options) if (option.Key == key) return option;
            throw new ArgumentException("Unknown transport option");
        }
        public static string Build(string origin, string destination, string key)
        {
            if (String.IsNullOrWhiteSpace(origin)) throw new ArgumentException("Enter your starting point here");
            if (String.IsNullOrWhiteSpace(destination)) throw new ArgumentException("Enter your destination here");
            string url = Links.Route(destination.Trim(),origin.Trim(),Find(key).GoogleMode);
            if (url.Length > 2048) throw new ArgumentException("Please use shorter place names or addresses.");
            return url;
        }
        public static string ProviderName(string provider)
        {
            switch(provider) { case "naver": return "Naver Map";case "kakao": return "Kakao Map";case "google": return "Google Maps";default:throw new ArgumentException("Unknown map provider"); }
        }
        static string NaverPoint(GeoPlace place)
        {
            double x=place.Longitude*20037508.342789244/180;
            double y=Math.Log(Math.Tan(Math.PI/4+place.Latitude*Math.PI/360))*6378137;
            return x.ToString("0.#######",CultureInfo.InvariantCulture)+","+y.ToString("0.#######",CultureInfo.InvariantCulture)+","+Uri.EscapeDataString(place.Name)+",,SIMPLE_POI";
        }
        static string KakaoPoint(GeoPlace place) { return Uri.EscapeDataString(place.Name)+","+place.Coordinates; }
        public static string Build(GeoPlace origin,GeoPlace destination,string key,string provider)
        {
            if(origin==null || destination==null) throw new ArgumentException("Confirm both places before opening directions.");
            origin.Validate();destination.Validate();Find(key);ProviderName(provider);
            if(Math.Abs(origin.Latitude-destination.Latitude)<0.000001 && Math.Abs(origin.Longitude-destination.Longitude)<0.000001) throw new ArgumentException("Choose two different places.");
            string mode=key=="taxi"?"car":key=="walking"?"walk":key=="bicycling"?"bike":"transit";
            if(provider=="naver") return "https://map.naver.com/p/directions/"+NaverPoint(origin)+"/"+NaverPoint(destination)+"/-/"+mode+"?c=16.00,0,0,0,dh";
            if(provider=="kakao")
            {
                string kakaoMode=key=="transit"?"traffic":key=="bicycling"?"bicycle":mode;
                return "https://map.kakao.com/link/by/"+kakaoMode+"/"+KakaoPoint(origin)+"/"+KakaoPoint(destination);
            }
            return Links.Route(destination.Coordinates,origin.Coordinates,Find(key).GoogleMode);
        }
    }
}
