using Microsoft.SemanticKernel;

namespace GroupChat;

public class KernelConfigurations
{
    private readonly AzureOpenAIConfig _config;

    public KernelConfigurations()
    {
        _config = new AzureOpenAIConfig();
    }

    public Kernel AzureOpenAIKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddAzureOpenAIChatCompletion(_config.ModelId, _config.Endpoint, _config.ApiKey);
        return builder.Build();
    }
}