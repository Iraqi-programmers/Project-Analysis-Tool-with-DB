using Microsoft.SqlServer.TransactSql.ScriptDom;
using SpAnalyzerTool.Models;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpAnalyzerTool.Helper
{
    public static class clsSqlSyntaxValidator 
    {
        /// <summary>
        /// <para> Parses the given SQL script and returns a list of syntax errors, if any.</para>
        /// التحقق من صحة بناء الجملة لـ SQL وإرجاع قائمة بالأخطاء.
        /// </summary>
        /// <param name="sqlText">The SQL code to be analyzed.</param>
        /// <returns>List of parse errors, or empty list if no errors found.</returns>
        public static List<ParseError> Validate(string sqlText)
        {
            IList<ParseError> errors; // ✅ صححنا النوع هنا

            var parser = new TSql150Parser(true); // SQL Server 2019
            using var reader = new StringReader(sqlText);
            parser.Parse(reader, out errors);

            // نحوله إلى List ليكون أسهل في التعامل
            return new List<ParseError>(errors);
        }

        /// <summary>
        /// الحصول على الأخطاء من تحليل SQL النصي.
        /// </summary>
        /// <param name="sqlText"></param>
        /// <returns></returns>
        public static List<SqlParseErrorModle> GetErrors(string sqlText)
        {
            var parser = new TSql150Parser(false);
            IList<ParseError> errors;
            parser.Parse(new StringReader(sqlText), out errors);

            return errors?.Select(e => new SqlParseErrorModle
            {
                Line = e.Line,
                Level = "خطأ",
                Message = e.Message
            }).ToList() ?? new();
        }

    }
}

