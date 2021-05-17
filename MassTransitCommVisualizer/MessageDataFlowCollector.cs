using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MassTransitCommVisualizer
{
    public class MessageDataFlowCollector
    {
        public static async Task<MessageFlowSymbols> Generate(string solutionFilePath)
        {
            Console.WriteLine("Looking for Visual Studio instance with major version 16 (VS2019)...");
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.First(vsInstance => vsInstance.Version.Major == 16);
            MSBuildLocator.RegisterInstance(instance);
            Console.WriteLine($"Instance '{instance.Name}' found and registered.");
            var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

            Console.WriteLine($"Opening solution {solutionFilePath}...");
            var solution = await workspace.OpenSolutionAsync(solutionFilePath);

            Console.WriteLine("Compiling solution projects...");
            var projectCompilations = solution.Projects.Select(project => project.GetCompilationAsync().Result).ToImmutableHashSet();

            Console.WriteLine("Finding all message producers and consumers in solution...");
            var consumerInterfaceSymbol = SymbolFinder.GetTypeSymbol(projectCompilations, "MassTransit.IConsumer`1");
            var consumerImplementationSymbols = await SymbolFinder.FindNamedTypeSymbolImplementations(solution, consumerInterfaceSymbol);

            var getResponseMethodSymbols = SymbolFinder.GetMethodSymbols(projectCompilations,
                "ETR.Backend.Common.Infrastructure.MassTransit.Extensions.ControllerRequestClientExtensions", "GetResponse", 2);
            var responseReceiverSymbols = await SymbolFinder.FindMethodCallers(solution, getResponseMethodSymbols, 2, 2);
            var responseSenderSymbols = await SymbolFinder.FindMethodCallers(solution, getResponseMethodSymbols, 2, 1);

            var publishMethodSymbols = SymbolFinder.GetMethodSymbols(projectCompilations,
                "MassTransit.IPublishEndpoint", "Publish", 1, 2);
            var messagePublisherSymbols = await SymbolFinder.FindMethodCallers(solution, publishMethodSymbols, 1, 1);

            var sendMethodSymbols = SymbolFinder.GetMethodSymbols(projectCompilations,
                "MassTransit.EndpointConventionExtensions", "Send", 1, 3);
            var messageSenderSymbols = await SymbolFinder.FindMethodCallers(solution, sendMethodSymbols, 1, 1);

            var respondAsyncSymbols = SymbolFinder.GetMethodSymbols(projectCompilations,
                "MassTransit.ConsumeContext", "RespondAsync", 1, 1);
            var messageResponderSymbols = await SymbolFinder.FindMethodCallers(solution, respondAsyncSymbols, 1, 1);

            return new MessageFlowSymbols(messagePublisherSymbols, messageResponderSymbols, messageSenderSymbols, 
                responseSenderSymbols, consumerImplementationSymbols, responseReceiverSymbols);
        }

        private static void Workspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            Console.WriteLine($"[{e.Diagnostic.Kind}] {e.Diagnostic.Message}");
        }
    }
}
