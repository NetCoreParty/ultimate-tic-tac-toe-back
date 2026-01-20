using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace UltimateTicTacToe.API.Extensions;

public static class SecretsConfigurationExtensions
{
    public static IConfigurationBuilder AddEnvironmentSecrets(this IConfigurationBuilder config, IHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            return config;
        }

        var secretsFile = config.Build()["Sops:SecretsFile"] ?? "secrets/appsettings.Production.secrets.json";
        config.AddJsonFile(secretsFile, optional: true, reloadOnChange: false);
        return config;
    }
}
