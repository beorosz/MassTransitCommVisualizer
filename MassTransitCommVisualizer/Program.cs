using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace MassTransitCommVisualizer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine($"Usage: {Assembly.GetExecutingAssembly().FullName} <path to solution> <output DOT file name>");
                return;
            }

            Console.WriteLine("Looking for Visual Studio instance with major version 16 (VS2019)...");
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.First(vsInstance => vsInstance.Version.Major == 16);
            MSBuildLocator.RegisterInstance(instance);
            Console.WriteLine($"Instance '{instance.Name}' found and registered.");

            var solutionFilePath = args[0];
            var outputFilePath = args[1];
            var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

            try
            {
                Console.WriteLine($"Opening solution {solutionFilePath}...");
                var solution = await workspace.OpenSolutionAsync(solutionFilePath);

                Console.WriteLine("Compiling solution projects...");
                var projectCompilations = solution.Projects.Select(project => project.GetCompilationAsync().Result).ToImmutableHashSet();

                Console.WriteLine("Finding all message consumers (MassTransit.IConsumer<T> implementations) in solution...");
                var consumerInterfaceSymbol = SymbolFinder.GetTypeSymbol(projectCompilations, "MassTransit.IConsumer`1");
                var consumerImplementationSymbols = await SymbolFinder.FindNamedTypeSymbolImplementations(solution, consumerInterfaceSymbol);

                Console.WriteLine("Finding all message producers and consumers (MassTransit.IRequestClient<T>.GetResponse extension method usages) in solution...");
                var getResponseMethodSymbols = SymbolFinder.GetMethodSymbols(projectCompilations,
                    "ETR.Backend.Common.Infrastructure.MassTransit.Extensions.ControllerRequestClientExtensions", "GetResponse", 2);
                var responseReceiverSymbols = await SymbolFinder.FindMethodCallers(solution, getResponseMethodSymbols, 2, 2);
                var responseSenderSymbols = await SymbolFinder.FindMethodCallers(solution, getResponseMethodSymbols, 2, 1);

                Console.WriteLine("Finding all message producers (MassTransit.IPublishEndpoint.Publish) in solution...");
                var publishMethodSymbols = SymbolFinder.GetMethodSymbols(projectCompilations,
                    "MassTransit.IPublishEndpoint", "Publish", 1, 2);
                var messagePublisherSymbols = await SymbolFinder.FindMethodCallers(solution, publishMethodSymbols, 1, 1);

                Console.WriteLine("Finding all message producers (MassTransit.EndpointConventionExtensions.Send) in solution...");
                var sendMethodSymbols = SymbolFinder.GetMethodSymbols(projectCompilations,
                    "MassTransit.EndpointConventionExtensions", "Send", 1, 3);
                var messageSenderSymbols = await SymbolFinder.FindMethodCallers(solution, sendMethodSymbols, 1, 1);

                Console.WriteLine("Finding all message producers (MassTransit.ConsumeContext.RespondAsync) in solution...");
                var respondAsyncSymbols = SymbolFinder.GetMethodSymbols(projectCompilations,
                    "MassTransit.ConsumeContext", "RespondAsync", 1, 1);
                var messageResponderSymbols = await SymbolFinder.FindMethodCallers(solution, respondAsyncSymbols, 1, 1);

                Console.WriteLine("Generating diagram....");
                var messagePublishers = ExtractNamesFromSymbols(messagePublisherSymbols);
                var messageResponders = ExtractNamesFromSymbols(messageResponderSymbols);
                var messageSenders = ExtractNamesFromSymbols(messageSenderSymbols);
                var responseSenders = ExtractNamesFromSymbols(responseSenderSymbols);
                var allMessageSources = MergeDictionaries(
                        new[]
                        {
                            messagePublishers,
                            messageResponders,
                            messageSenders,
                            responseSenders
                        })
                        .ToDictionary(x => x.Key, x => x.Value);

                var consumerImplementations = ExtractNamesFromSymbols(consumerImplementationSymbols);
                var responseReceivers = ExtractNamesFromSymbols(responseReceiverSymbols);
                var allMessageSinks = MergeDictionaries(
                        new[]
                        {
                            consumerImplementations,
                            responseReceivers
                        })
                    .ToDictionary(x => x.Key, x => x.Value);
                var graphRepresentation = GraphvizDotDiagramGenerator.Generate(allMessageSources, allMessageSinks);

                Console.WriteLine("Writing diagram to output file....");
                File.WriteAllText(outputFilePath, graphRepresentation);
                Console.WriteLine("done.");


                Console.WriteLine("Finished.");

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void PrintHandlerTypesAndTheirMessages(Dictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> handlersWithMessages)
        {
            Console.WriteLine();
            foreach (var handler in handlersWithMessages)
            {
                Console.WriteLine($"{handler.Key} " + $" ({string.Join(", ", handler.Value)})");
            }
        }

        private static Dictionary<string, string[]> ExtractNamesFromSymbols(Dictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> dictionary)
        {
            return dictionary
                .ToDictionary(keyValue => keyValue.Key.ToDisplayString(), keyValue =>
                    keyValue.Value.Select(value => value.ToDisplayString()).ToArray());

        }

        public static Dictionary<string, string[]>
            MergeDictionaries(IEnumerable<Dictionary<string, string[]>> classWithMessagesDictionaries)
        {
            var result = new Dictionary<string, string[]>();
            foreach (var classWithMessagesDictionary in classWithMessagesDictionaries)
            {
                foreach (var classWithMessages in classWithMessagesDictionary)
                {
                    var currentMessagesList = result.ContainsKey(classWithMessages.Key) ? result[classWithMessages.Key].ToList() : new List<string>();
                    currentMessagesList.AddRange(classWithMessages.Value);
                    result[classWithMessages.Key] = currentMessagesList.Distinct().ToArray();
                }
            }

            return result;
        }

        private static void Workspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            Console.WriteLine($"[{e.Diagnostic.Kind.ToString()}] {e.Diagnostic.Message}");
        }
    }
}
