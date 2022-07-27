using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Lokad.Secrets
{
    public static class KeyVaultConnector
    {
        /// <summary> Location of principal directory on Windows machine. </summary>
        private const string WindowsPrincipalDir = @"C:\LokadData\config\principal";

        /// <summary> Location of principal directory on Linux machine. </summary>
        private const string LinuxPrincipalDir = @"/etc/lokad/principal";

        /// <summary> 
        ///     Create a client connected to Azure KeyVault using the principal information
        ///     stored on the local machine.
        /// </summary>
        /// <remarks>
        ///     Note that the validity of the principal information (and the extent of 
        ///     the associated permissions) is only checked upon the first access to 
        ///     a KeyVault secret. 
        /// </remarks>
        public static KeyVaultClient Connect()
        {
            var clientAssertionCertificate = GetPrincipalCredentials();

            return new KeyVaultClient(async (string authority, string resource, string scope) =>
            {
                var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
                var result = await context.AcquireTokenAsync(resource, clientAssertionCertificate);
                return result.AccessToken;
            });
        }

        /// <summary> Load the credentials of the principal from the file system. </summary>
        /// <remarks>
        ///     Expects a directory <see cref="WindowsPrincipalDir"/> (on windows systems)
        ///     or <see cref="LinuxPrincipalDir"/> (on linux systems) or, if both are missing,
        ///     will attempt to load from the working directory. 
        ///     
        ///     That directory should contain a client_id.txt file (containing the principal
        ///     application's client-id), a certificate.pfx file (containing the certificate
        ///     used to authenticate as the principal application) and a certificate_pwd.txt
        ///     file (containing the password to decrypt the certificate). 
        /// </remarks>
        private static ClientAssertionCertificate GetPrincipalCredentials()
        {
            var principalDir =
                new DirectoryInfo(LinuxPrincipalDir).Exists ? LinuxPrincipalDir :
                new DirectoryInfo(WindowsPrincipalDir).Exists ? WindowsPrincipalDir :
                Directory.GetCurrentDirectory();

            var clientIdPath = Path.Combine(principalDir, "client_id.txt");
            string clientId;
            try
            {
                clientId = File.ReadAllText(clientIdPath).Trim();
            }
            catch (Exception inner)
            {
                throw new SecretNotResolvedException(
                    $"Lokad.Secrets: could not open '{clientIdPath}' to read client-id.",
                    inner);
            }

            var passwordPath = Path.Combine(principalDir, "certificate_pwd.txt");
            string password;
            try
            {
                password = File.ReadAllText(passwordPath).Trim();
            }
            catch (Exception inner)
            {
                throw new SecretNotResolvedException(
                    $"Lokad.Secrets: could not open '{passwordPath}' to read certificate password.",
                    inner);
            }

            var certificatePath = Path.Combine(principalDir, "certificate.pfx");
            X509Certificate2 certificate;
            try
            {
                certificate = new X509Certificate2(certificatePath, password);
            }
            catch (Exception inner)
            {
                throw new SecretNotResolvedException(
                    $"Lokad.Secrets: could not open or parse '{certificatePath}' to read certificate.",
                    inner);
            }

            return new ClientAssertionCertificate(clientId, certificate);
        }
    }
}
