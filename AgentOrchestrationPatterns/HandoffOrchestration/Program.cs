using Azure;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Handoff;

public class Program
{
    public static async Task Main(string[] args)
    {
        var kernelConfig = new KernelConfigurations();
        var kernel = kernelConfig.AzureOpenAIKernel();

        ChatCompletionAgent triageAgent = new ChatCompletionAgent
        {
            Name = "TriageAgent",
            Description = "Handle customer requests.",
            Instructions = "A customer support agent that triages issues.",
            Kernel = kernel,
        };

        ChatCompletionAgent statusAgent = new ChatCompletionAgent
        {
            Name = "OrderStatusAgent",
            Description = "A customer support agent that checks order status.",
            Instructions = "Handle order status requests.",
            Kernel = kernel,
        };
        statusAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new OrderStatusPlugin()));

        ChatCompletionAgent returnAgent = new ChatCompletionAgent
        {
            Name = "OrderReturnAgent",
            Description = "A customer support agent that handles order returns.",
            Instructions = "Handle order return requests.",
            Kernel = kernel,
        };
        returnAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new OrderReturnPlugin()));

        ChatCompletionAgent refundAgent = new ChatCompletionAgent
        {
            Name = "OrderRefundAgent",
            Description = "A customer support agent that handles order refund.",
            Instructions = "Handle order refund requests.",
            Kernel = kernel,
        };
        refundAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new OrderRefundPlugin()));

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var handoffs = OrchestrationHandoffs
        .StartWith(triageAgent)
        .Add(triageAgent, statusAgent, returnAgent, refundAgent)
        .Add(statusAgent, triageAgent, "Transfer to this agent if the issue is not status related")
        .Add(returnAgent, triageAgent, "Transfer to this agent if the issue is not return related")
        .Add(refundAgent, triageAgent, "Transfer to this agent if the issue is not refund related");

        ChatHistory history = [];

        ValueTask responseCallback(ChatMessageContent response)
        {
            history.Add(response);
            return ValueTask.CompletedTask;
        }

        Queue<string> responses = new();
        responses.Enqueue("I'd like to track the status of my order");
        responses.Enqueue("My order ID is 123");
        responses.Enqueue("I want to return another order of mine");
        responses.Enqueue("Order ID 321");
        responses.Enqueue("Broken item");
        responses.Enqueue("No, bye");

        ValueTask<ChatMessageContent> InteractiveCallback()
        {
            string input = responses.Dequeue();
            Console.WriteLine($"\n# INPUT: {input}\n");
            return ValueTask.FromResult(new ChatMessageContent(AuthorRole.User, input));
        }

        //ValueTask<ChatMessageContent> InteractiveCallback()
        //{
        //    Console.ForegroundColor = ConsoleColor.Yellow;

        //    Console.WriteLine("Interactive callback triggered. Please provide your input: ");
        //    string userInput = Console.ReadLine() ?? string.Empty;
        //    ChatMessageContent userMessage = new ChatMessageContent(AuthorRole.User, userInput);

        //    return ValueTask.FromResult(userMessage);
        //}

        HandoffOrchestration orchestration = new HandoffOrchestration(
            handoffs,
            triageAgent,
            statusAgent,
            returnAgent,
            refundAgent)
        {
            InteractiveCallback = InteractiveCallback,
            ResponseCallback = responseCallback,
        };

        InProcessRuntime runtime = new InProcessRuntime();
        await runtime.StartAsync();

        string task = "I am a customer that needs help with my orders";
        var result = await orchestration.InvokeAsync(task, runtime);

        var agentResults = await result.GetValueAsync();

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

public sealed class OrderStatusPlugin
{
    [KernelFunction]
    public string CheckOrderStatus(string orderId) => $"Order {orderId} is shipped and will arrive in 2-3 days.";
}

public sealed class OrderReturnPlugin
{
    [KernelFunction]
    public string ProcessReturn(string orderId, string reason) => $"Return for order {orderId} has been processed successfully.";
}

public sealed class OrderRefundPlugin
{
    [KernelFunction]
    public string ProcessReturn(string orderId, string reason) => $"Refund for order {orderId} has been processed successfully.";
}