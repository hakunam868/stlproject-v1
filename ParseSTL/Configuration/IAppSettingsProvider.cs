namespace ParseStl.Configuration;

public interface IAppSettingsProvider
{
    /// <summary>Loads and validates the settings.</summary>
    /// <exception cref="ConfigurationException">The configuration is missing, malformed or invalid.</exception>
    AppSettings Load();
}

public sealed class ConfigurationException(string message, Exception? inner = null) : Exception(message, inner);
