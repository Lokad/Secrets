# Lokad.Secrets

**Opinionated .NET library for resolving secrets in application configuration.** By default, this library uses a locally-available certificate to access an Azure KeyVault from which it pulls the values of the secrets. It supports local overrides by writing the secrets to local files, or with `dotnet user-secrets`. 

### Usage 

Install NuGet package [Lokad.Secrets](https://www.nuget.org/packages/Lokad.Secrets) and use: 

```csharp
using Lokad.Secrets;

SecretString secret = LokadSecret.Resolve("secret:mykeyvault/mysecretid");
```

Functions `LokadSecret.Resolve` and `LokadSecret.ResolveAsync` will resolve the provided secret by attempting all of the following cases, in this order:

1. If the provided value does not begin with `secret:` then it is considered to be a verbatim secret, and will be returned as-is.

2. If the application has a [.NET user secret](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) with the name `"mykeyvault/mysecretid"`, the value of that user secret is returned. The user secret should be associated to the assembly returned by [Assembly.GetEntryAssembly()](https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly.getentryassembly). 

3. If a readable file at path `./secrets/mykeyvault/mysecretid` exists (relative to the working directory of the program), its contents are returned as the secret. 

4. If a readable file at absolute path `C:\LokadData\config\secrets\mykeyvault\mysecretid` (on Windows) or `/etc/lokad/secrets/mykeyvault/mysecretid` (on Linux) exists, its contents are returned as the secret.

5. The Azure KeyVault vault `https://mykeyvault.vault.azure.net` is opened, and the secret with key `mysecretid` is accessed and returned. 

6. If all of the previous steps failed, throw a `SecretNotResolvedException`. 

#### Return Type

The `SecretString` has four properties: 

 - `Value` is the value of secret that was resolved. 
 - `Key` is the original key, in the form `"secret:mykeyvault/mysecretid"` (if the original key was a verbatim secret, this will be redacted instead, usually as `"[HIDDEN]"`). This property should always be safe to display, since it is not in itself a secret. 
 - `Source` indicates how it was resolved: `SecretSource.Verbatim` if the input was returned verbatim, `SecretSource.UserSecrets` if the secret was found in the .NET user secrets, `SecretSource.File` if it was loaded from the file system, and `SecretSource.Vault` if it was loaded from Azure KeyVault.
 - `Identity` is an additional piece of information to help with debugging. The objective is to confirm whether the secret held by a given application is the one currently present in the source, or if it has been changed since it was resolved by the application. For `SecretSource.File`, this will the modification data of the file from which the secret was taken. For `SecretSource.Vault`, this will be the version identifier of the secret on Azure KeyVault. 

In addition, calling `SecretString.ToString()`, as well as serializing the object to JSON using JSON.NET or System.Text.Json, will never leak the secret value, and will instead display a combination of the key, source and identity to aid in debugging. 

#### Configuring KeyVault Access

In order to access KeyVault, an application must be registered in Azure Active Directory, using a certificate for authentication. [You can follow the detailed documentation on docs.microsoft.com](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal). After following this procedure, you should create three files: 

 - `client_id.txt` should contain the **Application (client) ID** of the application that was created. It will be a GUID. 
 - `certificate.pfx` should be the certificate that was registered for authentication. This is a password-protected file. 
 - `certificate_pwd.txt` should contain the password required to open the certificate file `certificate.pfx`. 

These files should be placed in `C:\LokadData\config\principal` on Windows systems, and in `/etc/lokad/principal` on Linux systems, and they should be made readable to the application.  

### Philosophy

This package is based on two major principles:  

  1. **All configuration should be committed to the repository**. That is, the settings and configuration of an application, in Production, should be entirely controlled by the `appsettings.json` and `appsettings.Production.json` files present in version control. These files should not be modified by the build or deployment systems, nor should they be overridden by the execution environment. 

  2. **Secrets should never be committed to the repository**. That is, neither `appsettings.json` nor `appsettings.Production.json` should contain secrets (nor should those secrets appear elsewhere in the code).

The configuration files (committed to repository) do not contain secrets, but instead contain secret _identifiers_. Loading the configuration does not yield the secret values, instead secrets are resolved from their identifiers after the configuration has been loaded. 
