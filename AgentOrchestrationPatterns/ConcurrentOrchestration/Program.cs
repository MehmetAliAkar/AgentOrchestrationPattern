using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration.Concurrent;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;

namespace ConcurrentOrchestraion;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class Program
{
    public static List<ChatMessageContent> AgentHistory { get; } = [];

    public static async Task Main(string[] args)
    {
        var kernelConfig = new KernelConfigurations();
        var kernel = kernelConfig.AzureOpenAIKernel();

        ChatCompletionAgent flightAgent = new ChatCompletionAgent
        {
            Name = "FlightBookingAgent",
            Description = "Finds affordable and convenient flights for your desired travel dates and destination.",
            Instructions = "You are a travel agent specializing in finding the best flight options. You stricktly answer given a departure city, destination, and dates, provide the most affordable and convenient flight recommendations.Do not answer other questions",
            Kernel = kernel,
        };

        ChatCompletionAgent hotelAgent = new ChatCompletionAgent
        {
            Name = "HotelBookingAgent",
            Description = "Suggests hotels that match the user's preferences, such as price range, star rating, and location.",
            Instructions = "You are a hotel booking expert. You stricktly answer based on the destination and travel dates, suggest 2-3 hotels that are well-rated, within the user's budget, and close to major attractions. Do not answer other questions",
            Kernel = kernel,
        };

        ChatCompletionAgent weatherAgent = new ChatCompletionAgent
        {
            Name = "WeatherAgent",
            Description = "Provides weather forecasts for the target location and travel dates.",
            Instructions = "You are a weather forecasting assistant.You stricktly answer the given a location and date range, return a detailed weather forecast including temperature, precipitation, and advice (e.g., bring a jacket, avoid certain days do not answer other questions).",
            Kernel = kernel,
        };

        ChatCompletionAgent activityAgent = new ChatCompletionAgent
        {
            Name = "ActivityPlannerAgent",
            Description = "Recommends local attractions and activities based on user interests.",
            Instructions = "You are an expert in local attractions. You stricktly answer the based on the destination, suggest fun and popular activities like museums, nature spots, cultural experiences, or food tours that match user preferences. Do not answer other quetions",
            Kernel = kernel,
        };

        ConcurrentOrchestration orchestration = new(flightAgent, hotelAgent, weatherAgent, activityAgent)
        {
            ResponseCallback = ResponseCallback
        };

        InProcessRuntime runtime = new InProcessRuntime();
        await runtime.StartAsync();
        Console.Write("Enter to help in your vocation:");
        var input = Console.ReadLine() ?? "Plan a vocation in Istanbul";

        var result = await orchestration.InvokeAsync(input, runtime);

        var agentResult = await result.GetValueAsync();

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        foreach (var agent in AgentHistory)
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

    private static ValueTask ResponseCallback(ChatMessageContent response)
    {
        AgentHistory.Add(response);
        return ValueTask.CompletedTask;
    }
}