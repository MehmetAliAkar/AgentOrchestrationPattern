using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcurrentOrchestraion;

public class AzureOpenAIConfig
{
    public string ModelId { get; } = "gpt-4o";
    public string Endpoint { get; } = "*";
    public string ApiKey { get; } = "*";
}