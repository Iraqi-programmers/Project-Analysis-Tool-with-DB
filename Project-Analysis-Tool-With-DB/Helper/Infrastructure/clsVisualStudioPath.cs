using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpAnalyzerTool.Helper
{
    internal class clsVisualStudioPath
    {

        /// <summary>
        /// يستخدم vswhere.exe للعثور على أول نسخة من Visual Studio تحتوي على devenv.exe
        /// </summary>
        /// <returns>المسار الكامل لـ devenv.exe أو null إذا لم يُعثر عليه</returns>
        private static string? GetVisualStudioPathFromVsWhere()
        {
            // المسار المتوقع لـ vswhere.exe
            string vswherePath = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";

            if (!File.Exists(vswherePath))
                return null;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = vswherePath,
                    Arguments = "-latest -products * -requires Microsoft.Component.MSBuild -property productPath",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return null;

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string path = output.Trim();

                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return path;
            }
            catch
            {
                // إهمال الأخطاء، سنرجع null
            }

            return null;
        }

        private static string? GetVisualStudioPath()
        {
            // جرب جميع الإصدارات 
            string[] versions = { "2022", "2019", "2017" };
            string[] editions = { "Community", "Professional", "Enterprise", "BuildTools" };

            foreach (var version in versions)
            {
                foreach (var edition in editions)
                {
                    string vsPath = $@"%ProgramFiles(x86)%\Microsoft Visual Studio\{version}\{edition}\Common7\IDE\devenv.exe";
                    string fullPath = Environment.ExpandEnvironmentVariables(vsPath);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }

            return null;
        }

        private static string? GetVsCodePath()
        {
            // المسار الرسمي للتثبيت في AppData
            var codePath1 = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\Programs\Microsoft VS Code\Code.exe");
            if (File.Exists(codePath1))
                return codePath1;

            // أحياناً يكون مثبّتاً في Program Files
            var codePath2 = Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\Microsoft VS Code\Code.exe");
            if (File.Exists(codePath2))
                return codePath2;

            // أحياناً للمستخدمين الآخرين أو portable
            var codePath3 = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Local\Programs\Microsoft VS Code\Code.exe");
            if (File.Exists(codePath3))
                return codePath3;

            return null;
        }

        public static string? GetFirstAvailableEditor()
        {
            // 1. جرب vswhere أولاً
            var vsFromVsWhere = GetVisualStudioPathFromVsWhere();
            if (vsFromVsWhere != null)
                return vsFromVsWhere;

            // 2. جرب المسارات اليدوية المعروفة
            var vsPath = GetVisualStudioPath();
            if (vsPath != null)
                return vsPath;

            // 3. VS Code
            var vsCode = GetVsCodePath();
            if (vsCode != null)
                return vsCode;

            return null;
        }



    }
}
