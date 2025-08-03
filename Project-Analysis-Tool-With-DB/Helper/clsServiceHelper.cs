using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SpAnalyzerTool.Helper
{
    public class clsServiceHelper
    {

        public static void EnsureSqlServicesRunning()
        {
            TryStartService("SQLBrowser");            // خدمة SQL Server Browser
            TryStartService("MSSQLSERVER");           // النسخة الأساسية من SQL Server
            TryStartService("MSSQL$SQLEXPRESS");      // نسخة Express إن وجدت (اختياري)
        }

        private static void TryStartService(string serviceName)
        {
            try
            {
                var service = new ServiceController(serviceName);
                if (service.Status != ServiceControllerStatus.Running &&
                    service.Status != ServiceControllerStatus.StartPending)
                {
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                }
            }
            catch
            {
                // نتجاهل الخطأ بصمت، أو نسجله لاحقًا في Log داخلي
            }
        }

    }
}
