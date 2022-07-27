using Lokad.Secrets;

var resolve = LokadSecrets.Resolve("secret:myvault/mysecret");

Console.WriteLine(resolve.ToString());
Console.WriteLine("Value: '{0}'", resolve.Value);
