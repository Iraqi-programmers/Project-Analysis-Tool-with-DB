using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpAnalyzerTool.Models
{
    public partial class AppSettings : ObservableObject
    {
        [ObservableProperty]
        public string? defaultConnectionString;

        [ObservableProperty]
        public string? fileSize;

    }

    public class HelpItem
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    public class ContactDetail
    {
        public string? Icon { get; set; }
        public string? Text { get; set; }
        public string? Link { get; set; } // رابط قابل للنقر
    }

    public class ContactInfo
    {
        public string? Title { get; set; }
        public List<ContactDetail>? Details { get; set; }
    }

    public class HelpContentModel
    {
        public string? Title { get; set; }
        public string? Version { get; set; }
        public List<HelpItem>? Items { get; set; }
        public ContactInfo? Contact { get; set; }
    }





}
