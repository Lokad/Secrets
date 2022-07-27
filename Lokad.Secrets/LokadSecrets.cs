using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Lokad.Secrets
{
    public static class LokadSecrets
    {
        /// <summary>
        ///     Synchronously retrieve the secret that corresponds to a configuration 
        ///     entry to be resolved. 
        /// </summary>
        /// <remarks>
        ///     Provided for convenience. 
        /// </remarks>
        public static SecretString Resolve(string toResolve) =>
            ResolveAsync(toResolve, default).GetAwaiter().GetResult();

        /// <summary>
        ///     Asynchronously retrieve the secret that corresponds to a configuration 
        ///     entry to be resolved. 
        /// </summary>
        public static async Task<SecretString> ResolveAsync(string toResolve, CancellationToken cancel)
        {
            const string SecretPrefix = "secret:";

            if (!toResolve.StartsWith(SecretPrefix))
                return new SecretString("[HIDDEN]", toResolve);

            var segments = toResolve[SecretPrefix.Length..].Split('/');
            var validation = new Regex(@"^[0-9a-zA-Z\-]+$");
            if (segments.Length != 2 ||
                segments[0].Length > 24 ||
                !validation.Match(segments[0]).Success ||
                segments[1].Length > 127 ||
                !validation.Match(segments[1]).Success)
            {
                throw new ArgumentException(
                    // Malformed entry may actually be a secret that was misparsed as a
                    // reference, so do not include it in the exception message
                    $"Entry starts with '{SecretPrefix}' but is not in proper 'vault/secret' format.",
                    nameof(toResolve));
            }

            var vault = segments[0];
            var secretKey = segments[1];

            // Attempt overrides first, then KeyVault

            return TryUserSecretsResolve(vault, secretKey) 
                ?? TryFileSystemResolve(vault, secretKey)
                ?? await KeyVaultResolve(vault, secretKey, cancel);
        }

        /// <summary>
        ///     Returns a copy of a configuration object, where every value is passed
        ///     through <see cref="LokadSecrets.Resolve"/> before being returned.
        /// </summary>
        public static IConfiguration Resolve(IConfiguration configuration) =>
            new OverriddenConfiguration(configuration);

        /// <summary>
        ///     Returns a copy of a configuration object, where every value is passed
        ///     through <see cref="LokadSecrets.Resolve"/> before being returned.
        /// </summary>
        public static IConfigurationSection Resolve(IConfigurationSection section) =>
            new OverriddenConfigurationSection(section);

        /// <summary> Attempt to resolve a secret from dotnet user secrets. </summary>
        /// <remarks>
        ///     The user secret should be named '{vault}/{secretKey}'
        /// </remarks>
        private static SecretString? TryUserSecretsResolve(string vault, string secretKey)
        {
            try
            {
                var assemblyKey = Assembly.GetEntryAssembly().GetCustomAttribute<UserSecretsIdAttribute>();
                if (assemblyKey == null) return null;

                var config = new ConfigurationBuilder()
                    .AddUserSecrets(assemblyKey.UserSecretsId)
                    .Build();

                var value = config.GetChildren()
                    .FirstOrDefault(cs => cs.Key == $"{vault}/{secretKey}")
                    ?.Value;

                if (value == null) 
                    return null;

                return new SecretString(
                    key: $"secret:{vault}/{secretKey}",
                    value: value,
                    SecretSource.UserSecrets,
                    assemblyKey.UserSecretsId);
            }
            catch
            {
                return null;
            }
        }

        /// <summary> Attempt to resolve a secret from the file system. </summary>
        /// <remarks>
        ///     The function will look, in order, at: 
        ///      - a 'secrets/' in the current directory
        ///      - a 'C:\LokadData\config\secrets'
        ///      - a '/etc/lokad/secrets'
        ///      
        ///     In all three cases, the file will be named {vault}/{secretKey}.txt
        /// </remarks>
        private static SecretString? TryFileSystemResolve(string vault, string secretKey)
        {
            var dirs = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "secrets"),
                @"C:\LokadData\config\secrets",
                "/etc/lokad/secrets"
            };

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;

                var path = Path.Combine(dir, vault, secretKey);
                try
                {
                    return new SecretString(
                        key: $"secret:{vault}/{secretKey}",
                        value: File.ReadAllText(path),
                        SecretSource.File,
                        File.GetLastWriteTimeUtc(path).ToString("O"));
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

        /// <summary> Resolve a secret from the specified vault. </summary>
        private static async Task<SecretString> KeyVaultResolve(
            string vault, 
            string secretKey,
            CancellationToken cancel)
        {
            SecretBundle bundle;
            try
            {
                bundle = await KeyVaultClient.GetSecretAsync(
                    $"https://{vault}.vault.azure.net/",
                    secretKey,
                    cancel);
            }
            catch (Exception ex)
            {
                throw new SecretNotResolvedException(
                    $"Lokad.Secrets: Failed to retrieve secret {secretKey} from https://{vault}.vault.azure.net/",
                    ex);
            }

            if (bundle.Value == null)
                throw new SecretNotResolvedException(
                    $"Lokad.Secrets: Secret {secretKey} from https://{vault}.vault.azure.net/ has no associated value");

            // secret found, retrieve its identity
            var identityString = bundle.Id;
            var identity = identityString[(identityString.LastIndexOf('/') + 1)..];

            return new SecretString(
                key: $"secret:{vault}/{secretKey}",
                value: bundle.Value,
                SecretSource.Vault,
                identity);
        }

        private static KeyVaultClient KeyVaultClient { get; } = KeyVaultConnector.Connect();
    }
}
