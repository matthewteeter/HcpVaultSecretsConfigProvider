using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HcpVaultSecretsConfigProvider
{
    public static class HcpVaultSecretsExtensions
    {
        public static IConfigurationBuilder AddHcpVaultSecretsConfiguration(this IConfigurationBuilder builder, IConfiguration upstreamConfig)
        {
            builder.Add(new HcpVaultSecretsConfigurationSource(upstreamConfig));
            return builder;
        }
    }
}
