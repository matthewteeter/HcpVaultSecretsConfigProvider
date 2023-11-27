using HcpVaultSecretsConfigProvider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IHost host = Host.CreateDefaultBuilder(args)
                       .UseEnvironment("Development")
                       .ConfigureAppConfiguration(config => config.AddHcpVaultSecretsConfiguration(config.Build()))
                       .Build(); //enable user secrets in Development
// if running locally, you can set the parameters using dotnet user-secrets. If docker, pass in via Env Vars.
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

Console.WriteLine(config["Zip"]);
