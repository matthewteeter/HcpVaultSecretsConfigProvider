using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;

namespace HcpVaultSecretsConfigProvider
{
    public class HcpVaultSecretsConfigurationProvider : ConfigurationProvider
    {
        IConfiguration _upstream;
        private static ILogger<HcpVaultSecretsConfigurationProvider> _logger;
        private readonly HttpClient client = new HttpClient();
        public HcpVaultSecretsConfigurationProvider(IConfiguration upstream) 
        {
            _upstream = upstream;
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConfiguration(upstream.GetSection("Logging"))
                .AddConsole();
            });
            _logger = loggerFactory.CreateLogger<HcpVaultSecretsConfigurationProvider>();
        }

        private bool ShouldLoadSecrets()
        {
            string[] keysToCheck = ["HcpVaultSecrets:OrgId", "HcpVaultSecrets:ProjectId", "HcpVaultSecrets:AppName"];
            bool foundAll = true;
            foreach (var item in keysToCheck)
            {
                bool foundOne = _upstream.AsEnumerable().Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Key == item
                && !string.IsNullOrWhiteSpace(kv.Value)).FirstOrDefault().Value != null;
                foundAll &= foundOne;
            }
            return foundAll;
        }

        private string GetFromConfig(string key) =>
            _upstream.AsEnumerable().Where(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Key == key).FirstOrDefault().Value!;

        private bool CanAuthenticateToHcp() => _upstream.AsEnumerable().Where(kv => kv.Key == "HCP_CLIENT_ID" || kv.Key == "HCP_CLIENT_SECRET").Count() == 2;

        public override void Load()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        { 
            if (!ShouldLoadSecrets())
            {
                _logger.LogWarning("OrgId, ProjectId, or AppName missing/blank values in config, so won't inject secrets from HCP Vault Secrets.");
                return;
            }
            if(!CanAuthenticateToHcp())
            {
                _logger.LogWarning("Missing either HCP_CLIENT_ID or HCP_CLIENT_SECRET env var, so won't inject secrets from HCP Vault Secrets.");
                return;
            }
            //authenticate to HCP vault using env vars HCP_CLIENT_ID and HCP_CLIENT_SECRET
            string clientId = _upstream.AsEnumerable().FirstOrDefault(kv => kv.Key == "HCP_CLIENT_ID").Value ?? string.Empty;
            string clientSecret = _upstream.AsEnumerable().FirstOrDefault(kv => kv.Key == "HCP_CLIENT_SECRET").Value ?? string.Empty;
            try
            {
                var response = await AuthenticateToHcp(clientId, clientSecret);
                if (response == null || !response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Unable to authenticate to HCP! {response?.StatusCode} {response?.Content}");
                    return;
                }
                var jsonResponse = await response.Content.ReadAsStringAsync();
                TokenResponse? parsed = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);
                if (parsed?.access_token == null)
                {
                    _logger.LogError("Response from HCP token endpoint was empty!");
                    return;
                }
                string org = GetFromConfig("HcpVaultSecrets:OrgId");
                string project = GetFromConfig("HcpVaultSecrets:ProjectId");
                string app = GetFromConfig("HcpVaultSecrets:AppName");
                _logger.LogDebug($"Found org {org}, proj {project}, app {app}");
                //get all secrets for this app and overlay onto existing config if keys match
                var secretsMap = await GetSecrets(org, project, app, parsed.access_token!);
                foreach (KeyValuePair<string, string> kv in secretsMap)
                {
                    Set(kv.Key, kv.Value);//TODO: will this break if a key doesn't exist?
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }

        private async Task<HttpResponseMessage> AuthenticateToHcp(string clientId, string clientSecret)
        {
            string url = "https://auth.hashicorp.com/oauth/token";
            HttpResponseMessage response = null;
            var retryPolicy = Policy.Handle<HttpRequestException>()
                                .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                (exception, timeSpan, context) => //executed before each retry
                                {
                                    _logger.LogWarning($"Couldn't authenticate: {exception}");
                                });
            await retryPolicy.Execute(async () =>
            {
                _logger.LogDebug($"POSTing {url}...");
                response = await client.PostAsync(url, new StringContent($$"""
                            {"audience": "https://api.hashicorp.cloud",
                             "grant_type": "client_credentials",
                             "client_id": "{{clientId}}",
                             "client_secret": "{{clientSecret}}"}
                            """, Encoding.UTF8, "application/json"));
            });
            return response;
        }

        private async Task<Dictionary<string, string>> GetSecrets(string org, string project, string app, string token) 
        {
            string json = await CallHcpVaultSecretsApi(org, project, app, token);
            OpenResponse? resp = JsonSerializer.Deserialize<OpenResponse>(json);
            Dictionary<string, string> toRet = new Dictionary<string, string>();
            if (resp != null)
            {
                toRet = resp.secrets.ToDictionary(s => s.name, s => s.version.value);
            }
            return toRet;
        }

        private async Task<string> CallHcpVaultSecretsApi(string org, string project, string app, string token)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            string url = $"https://api.cloud.hashicorp.com/secrets/2023-06-13/organizations/{org}/projects/{project}/apps/{app}/open";
            _logger.LogDebug($"GETing {url}...");
            HttpResponseMessage response = null;
            var retryPolicy = Policy.Handle<HttpRequestException>()
                                .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                (exception, timeSpan, context) => //executed before each retry
                                {
                                    _logger.LogWarning($"Couldn't retrieve secrets: {exception}");
                                });
            await retryPolicy.Execute(async () =>
            {
                response = await client.GetAsync(url);
            });
            if (!response.IsSuccessStatusCode)
            {
                string error = $"Error retrieving secrets: {response.StatusCode} {await response.Content.ReadAsStringAsync()}";
                _logger.LogError(error);
                throw new ApplicationException(error);
            }
            return await response.Content.ReadAsStringAsync();
        }
    }
}
