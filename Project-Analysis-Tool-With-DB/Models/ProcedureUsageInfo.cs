using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

namespace SpAnalyzerTool.Models
{
    public partial  class ProcedureUsageInfo :ObservableObject
    {
        [ObservableProperty]
        public string? procedure;

        [ObservableProperty]
        public int count;

        [ObservableProperty]
        public bool isSelectedForDelete;

        public string Status => Count > 0 ? "✅ مستخدم" : "❌ غير مستخدم";
        
        [ObservableProperty]
        public List<LocationInfo> locationPaths  = new();
     

        public string Locations => string.Join(", ", LocationPaths.Select(l => $"{l.FileName} (سطر {l.LineNumber})"));


    }

    public class LocationInfo 
    {
        public string? FullPath { get; set; }
        public int LineNumber { get; set; }

        public string? FileName => Path.GetFileName(FullPath);

    }



}
