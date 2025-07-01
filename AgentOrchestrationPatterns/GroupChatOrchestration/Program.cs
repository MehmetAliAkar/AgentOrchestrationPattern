using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GroupChat;

public class Program
{
    public static async Task Main(string[] args)
    {
        var kernelConfig = new KernelConfigurations();
        var kernel = kernelConfig.AzureOpenAIKernel();

        var contentWriter = new ChatCompletionAgent
        {
            Name = "ContentWriter",
            Description = "Creates an article draft based on the provided topic.",
            Instructions = "Write an informative and engaging article draft based on the given topic.",
            Kernel = kernel
        };

        var editor = new ChatCompletionAgent
        {
            Name = "Editor",
            Description = "Improves grammar, fluency, and structure of the article.",
            Instructions = "Review and edit the content for clarity, grammar, and consistency without changing its core meaning.",
            Kernel = kernel
        };

        var seoExpert = new ChatCompletionAgent
        {
            Name = "SEOExpert",
            Description = "Enhances the article for better search engine performance.",
            Instructions = "Optimize the article with keywords and formatting to improve search engine visibility.",
            Kernel = kernel
        };

        ChatHistory history = [];

        ValueTask responseCallback(ChatMessageContent response)
        {
            history.Add(response);
            return ValueTask.CompletedTask;
        }

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        GroupChatOrchestration orchestration = new GroupChatOrchestration(
        new RoundRobinGroupChatManager { MaximumInvocationCount = 8 },// başka manager var mı?
        contentWriter,
        editor, seoExpert)
        {
            ResponseCallback = responseCallback,
        };

        InProcessRuntime runtime = new InProcessRuntime();
        await runtime.StartAsync();

        var result = await orchestration.InvokeAsync(
        "Write a high-quality SEO-friendly article as a team.",
        runtime);

        var agentResult = await result.GetValueAsync();

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        foreach (var agent in history)
        {
            Console.WriteLine("-------------------------------------");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(agent.AuthorName);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(agent.Content);

            Console.ResetColor();
            Console.WriteLine("-------------------------------------");
        }
    }
}