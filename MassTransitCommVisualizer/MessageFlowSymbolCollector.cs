using System;
using System.Collections.Generic;
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
            var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

            Console.WriteLine($"Opening solution {solutionFilePath}...");
            var solution = await workspace.OpenSolutionAsync(solutionFilePath);

            Console.WriteLine("Compiling solution projects...");
            var projectCompilations = solution.Projects
                .Select(project => project.GetCompilationAsync().Result)
                .ToImmutableHashSet();

            Console.WriteLine("Finding all message producers and consumers in solution...");
            var consumerInterfaceSymbol = symbolFinder.GetTypeSymbol(projectCompilations, "MassTransit.IConsumer`1");
            var consumerImplementationSymbols =
                await symbolFinder.FindNamedTypeSymbolImplementations(solution, consumerInterfaceSymbol);

            var getResponseMethodSymbols = symbolFinder.GetMethodSymbols(projectCompilations,
                "ETR.Backend.Common.Infrastructure.MassTransit.Extensions.ControllerRequestClientExtensions",
                "GetResponse", 2);
            var responseReceiverSymbols =
                await symbolFinder.FindMethodCallers(solution, getResponseMethodSymbols, 2, 2);
            var responseSenderSymbols = await symbolFinder.FindMethodCallers(solution, getResponseMethodSymbols, 2, 1);

            var publishMethodSymbols = symbolFinder.GetMethodSymbols(projectCompilations,
                "MassTransit.IPublishEndpoint", "Publish", 1, 2);
            var messagePublisherSymbols = await symbolFinder.FindMethodCallers(solution, publishMethodSymbols, 1, 1);
            
            var publishExtensionMethodSymbols = symbolFinder.GetMethodSymbols(projectCompilations,
                "MassTransit.PublishContextExecuteExtensions", "Publish", 1, 4);
            var messageExtensionPublisherSymbols =
                await symbolFinder.FindMethodCallers(solution, publishExtensionMethodSymbols, 1, 1);

            var sendMethodSymbols = symbolFinder.GetMethodSymbols(projectCompilations,
                "MassTransit.EndpointConventionExtensions", "Send", 1, 3);
            var messageSenderSymbols = await symbolFinder.FindMethodCallers(solution, sendMethodSymbols, 1, 1);

            var respondAsyncSymbols = symbolFinder.GetMethodSymbols(projectCompilations,
                "MassTransit.ConsumeContext", "RespondAsync", 1, 1);
            var messageResponderSymbols = await symbolFinder.FindMethodCallers(solution, respondAsyncSymbols, 1, 1);


            var messageProducerSymbolCollection = MergeDictionaries(
                    new[]
                    {
                        messagePublisherSymbols,                        
                        messageExtensionPublisherSymbols,
                        messageResponderSymbols,
                        messageSenderSymbols,
                        responseSenderSymbols
                    })
                .ToDictionary(x => x.Key, x => x.Value);

            var messageConsumerSymbolCollection = MergeDictionaries(
                    new[]
                    {
                        consumerImplementationSymbols,
                        responseReceiverSymbols
                    })
                .ToDictionary(x => x.Key, x => x.Value);

            return new MessageFlowSymbols(messageProducerSymbolCollection, messageConsumerSymbolCollection);
        }

        private void Workspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            Console.WriteLine($"[{e.Diagnostic.Kind}] {e.Diagnostic.Message}");
        }

        private IDictionary<TMessageHandlerType, IEnumerable<TMessageType>>
            MergeDictionaries<TMessageHandlerType, TMessageType>(IEnumerable<IDictionary<TMessageHandlerType, IEnumerable<TMessageType>>> handlersWithMessages)
        {
            var result = new Dictionary<TMessageHandlerType, IEnumerable<TMessageType>>();
            foreach (var classWithMessagesDictionary in handlersWithMessages)
            {
                foreach (var classWithMessages in classWithMessagesDictionary)
                {
                    var currentMessagesList = result.ContainsKey(classWithMessages.Key) ?
                        result[classWithMessages.Key].ToList() :
                        new List<TMessageType>();
                    currentMessagesList.AddRange(classWithMessages.Value);
                    result[classWithMessages.Key] = currentMessagesList.Distinct().ToArray();
                }
            }

            return result;
        }
    }
}
