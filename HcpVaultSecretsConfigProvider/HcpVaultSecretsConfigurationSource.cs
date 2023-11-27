using Microsoft.Extensions.Configuration;
using System;

namespace HcpVaultSecretsConfigProvider
{
    public class HcpVaultSecretsConfigurationSource : IConfigurationSource
    {
        private IConfiguration _upstreamConfig;
        public HcpVaultSecretsConfigurationSource(IConfiguration configuration)
        {
                _upstreamConfig = configuration;
        }
        public  IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new HcpVaultSecretsConfigurationProvider(_upstreamConfig);
        }
    }
}