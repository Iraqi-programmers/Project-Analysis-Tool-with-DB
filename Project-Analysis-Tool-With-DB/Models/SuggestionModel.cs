using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpAnalyzerTool.Models
{
    /// <summary>
    /// مودل يعبئ من خلال ملف json
    /// </summary>
    public class SuggestionModel
    {
        public List<string>? keywords { get; set; }
        public List<string>? procedures { get; set; }
        public List<string>? tables { get; set; }

        public List<string> GetAllSuggestions()
        {
            return (keywords ?? new())
                .Concat(procedures ?? new())
                .Concat(tables ?? new())
                .Distinct()
                .ToList();
        }
    }
}
