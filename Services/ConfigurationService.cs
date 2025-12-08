using Microsoft.Extensions.Configuration;

namespace BilderbergImport.Services;

public class ConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }

    public string GetConnectionString(string name = "DefaultConnection")
    {
        return _configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' not found.");
    }

    public string GetSetting(string key)
    {
        return _configuration[key];
    }
}
