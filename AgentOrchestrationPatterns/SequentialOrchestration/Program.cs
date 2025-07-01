using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Sequential;

public class Program
{
    public static async Task Main(string[] args)
    {
        var kernelConfig = new KernelConfigurations();
        var kernel = kernelConfig.AzureOpenAIKernel();

        ChatCompletionAgent analystAgent = new ChatCompletionAgent
        {
            Name = "Analyst",
            Description = "Analyzes feature requests and converts them into structured business requirements.",
            Instructions = """
            You are a business analyst working on software feature definitions.

            Your tasks:
            - Understand the user's feature request.
            - Identify the business goal behind it.
            - Write **user stories** (e.g., "As a user, I want to ...").
            - Extract **business rules**, **acceptance criteria**, and **edge cases**.
            - Format your output clearly for developers.

            Output structure:
            1. Feature Summary
            2. User Stories
            3. Business Rules
            4. Edge Cases

            Only proceed with clear and structured business language.
            """,
            Kernel = kernel,
        };

        ChatCompletionAgent softwareDeveloperAgent = new ChatCompletionAgent
        {
            Name = "SoftwareDeveloper",
            Description = "Creates technical implementation plan and example code from business requirements.",
            Instructions = "Break down the user stories into frontend/backend tasks and provide example code or pseudocode where needed.",
            Kernel = kernel,
        };

        ChatCompletionAgent TesterAgent = new ChatCompletionAgent
        {
            Name = "Tester",
            Description = "Generates test scenarios and quality risks based on the developer's technical design.",
            Instructions = "Analyze the developer's technical tasks and code samples, then write Gherkin-style test cases to cover expected behaviors, edge cases, and error scenarios. Also include a short risk analysis.",
            Kernel = kernel,
        };

        ChatHistory history = [];
        ValueTask responseCallback(ChatMessageContent response)
        {
            history.Add(response);
            return ValueTask.CompletedTask;
        }

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        SequentialOrchestration orchestration = new(analystAgent, softwareDeveloperAgent, TesterAgent)
        {
            ResponseCallback = responseCallback,
        };

        InProcessRuntime runtime = new InProcessRuntime();
        await runtime.StartAsync();

        var input = Console.ReadLine();

        var result = await orchestration.InvokeAsync(input, runtime);

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