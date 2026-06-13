using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SpAnalyzerTool.Helper
{
    public static class MyCertificateHelper
    {
        public static X509Certificate2 CreateSelfSignedCertificate(string certName)
        {
            using var ecdsa = System.Security.Cryptography.ECDsa.Create();
            var req = new CertificateRequest($"cn={certName}", ecdsa, System.Security.Cryptography.HashAlgorithmName.SHA256);

            req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
            req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));

            var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));

            return new X509Certificate2(cert.Export(X509ContentType.Pfx));
        }


        public static void InstallCertificate(X509Certificate2 certificate)
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            // تحقق من وجود الشهادة مسبقًا
            var existingCerts = store.Certificates.Find(X509FindType.FindBySubjectName, certificate.SubjectName.Name, false);
            if (existingCerts.Count == 0)
            {
                store.Add(certificate);
            }

            store.Close();
        }

    }
}
