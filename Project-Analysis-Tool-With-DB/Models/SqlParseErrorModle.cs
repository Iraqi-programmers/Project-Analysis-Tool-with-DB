using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpAnalyzerTool.Models
{
    public partial class SqlParseErrorModle : ObservableObject
    {
       
        [ObservableProperty]
        public string? suggestedText;

        [ObservableProperty]
        public int line;

        [ObservableProperty]
        public string? level;  // مثلاً Error, Warning
        
        [ObservableProperty]
        public string? message;

        [ObservableProperty]
        public bool isTableError;
    }
}
