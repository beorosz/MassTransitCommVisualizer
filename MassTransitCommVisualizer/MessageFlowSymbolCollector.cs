using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MassTransitCommVisualizer.Model;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MassTransitCommVisualizer
{
    public interface IMessageFlowSymbolCollector
    {
        Task<MessageFlowSymbols> Collect(string solutionFilePath);
    }

    public class MessageFlowSymbolCollector : IMessageFlowSymbolCollector
    {
        private readonly ISymbolFinder symbolFinder;

        public MessageFlowSymbolCollector(ISymbolFinder symbolFinder)
        {
            this.symbolFinder = symbolFinder;
        }

        public async Task<MessageFlowSymbols> Collect(string solutionFilePath)
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
            var consumerInterfaceSymbol = symbolFinder.GetTypeSymbol(projectCompilations, "MassTransit.IConsumer`1");
            var consumerImplementationSymbols = await symbolFinder.FindNamedTypeSymbolImplementations(solution, consumerInterfaceSymbol);

            var getResponseMethodSymbols = symbolFinder.GetMethodSymbols(projectCompilations,
                "ETR.Backend.Common.Infrastructure.MassTransit.Extensions.ControllerRequestClientExtensions", "GetResponse", 2);
            var responseReceiverSymbols = await symbolFinder.FindMethodCallers(solution, getResponseMethodSymbols, 2, 2);
            var responseSenderSymbols = await symbolFinder.FindMethodCallers(solution, getResponseMethodSymbols, 2, 1);

            var publishMethodSymbols = symbolFinder.GetMethodSymbols(projectCompilations,
                "MassTransit.IPublishEndpoint", "Publish", 1, 2);
            var messagePublisherSymbols = await symbolFinder.FindMethodCallers(solution, publishMethodSymbols, 1, 1);

            var sendMethodSymbols = symbolFinder.GetMethodSymbols(projectCompilations,
                "MassTransit.EndpointConventionExtensions", "Send", 1, 3);
            var messageSenderSymbols = await symbolFinder.FindMethodCallers(solution, sendMethodSymbols, 1, 1);

            var respondAsyncSymbols = symbolFinder.GetMethodSymbols(projectCompilations,
                "MassTransit.ConsumeContext", "RespondAsync", 1, 1);
            var messageResponderSymbols = await symbolFinder.FindMethodCallers(solution, respondAsyncSymbols, 1, 1);

            return new MessageFlowSymbols(messagePublisherSymbols, messageResponderSymbols, messageSenderSymbols, 
                responseSenderSymbols, consumerImplementationSymbols, responseReceiverSymbols);
        }

        private void Workspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            Console.WriteLine($"[{e.Diagnostic.Kind}] {e.Diagnostic.Message}");
        }
    }
}
