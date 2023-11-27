# HCP Vault Secrets Config Provider for .NET

## Purpose
This library integrates in with .NET's configuration system to allow you to easily inject secrets from HCP Vault Secrets.

Note - HCP Vault Secrets is a new service that released in 2023, with a different API from the original HCP Vault.

## How To Install
Get the Nuget package :-)

## How To Configure

#### Configuration for HCP
Add the following block to your configuration (this could be via appsettings.json, env vars, or any other .NET-supported config source your app is using):
```
"HcpVaultSecrets": {
	"OrgId":"your_org_id (note: ID, not name)",
	"ProjectId":"your_project_id (note: ID, not name)",
	"AppName":"your_app",
}
```
You can obtain the org + project IDs from the HCP Web UI.

#### Startup Code
In your application's startup code, call AddHcpVaultSecretsConfiguration() and pass in your existing IConfiguration up to that point.
This allows the library to obtain HCP Org/Project/App using well-known config keys, allowing you to pass them in via file, command line, env var, etc.
```cs
using IHost host = Host.CreateDefaultBuilder(args)
                       .UseEnvironment("Development")
                       .ConfigureAppConfiguration(config => config.AddHcpVaultSecretsConfiguration(config.Build()))
                       .Build();
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();
```

#### Env Vars for HCP Identity
Finally, this library REQUIRES you to pass it the HCP client ID and secret as env vars, namely:
* HCP_CLIENT_ID
* HCP_CLIENT_SECRET

It is assumed these will always be passed as env vars since we wouldn't want them directly in a config file, as they give access to all the other secrets.

## How it Works
This library calls the [OpenAppSecrets](https://developer.hashicorp.com/hcp/api-docs/vault-secrets#OpenAppSecrets) API to obtain all secrets in the specified HCP App, then overlays those on top of any existing config items that have a matching Secret Name.

For example, if your config file had local testing values such as:
|Key|Value|
|----|----|
|ApiKey|LocalSecret|
|OtherKey|OtherValue|

and your vault had a password named 'ApiKey' with value "ActualSecret", the app would retrieve "ActualSecret" when querying IConfiguration for key 'ApiKey'.


#### Turning up Logging
Add the following to your appsettings.json (or merge with any existing Logging block) to enable more verbose logging of what the library is doing:
```
"Logging": {
	"LogLevel": {
		"HcpVaultSecretsConfigProvider.HcpVaultSecretsConfigurationProvider": "Debug"
	}
}
```