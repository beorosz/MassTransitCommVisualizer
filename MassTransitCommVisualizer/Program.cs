using System;
using System.Threading.Tasks;
using FluentArgs;
using FluentArgs.Help;
using Microsoft.Extensions.DependencyInjection;

namespace MassTransitCommVisualizer
{
    class Program
    {
        private static ServiceProvider serviceProvider;

        static Task Main(string[] args)
        {
            ConfigureServices();

            var inputDataFileDefaultValue = "messageflowdata.dat";
            var app = serviceProvider.GetService<IApp>();
            if (app == null)
            {
                throw new ApplicationException("App not found in service provider!");
            }


            return FluentArgsBuilder.New()
                .DefaultConfigsWithAppDescription(
                    "The application opens the given VS solution file, " +
                    "analyzes its message flow and can either output process entry information to console or the given flow fragment into an SVG file.")
                .RegisterHelpPrinter(new SimpleHelpPrinter(Console.Error))

                .Parameter<string>("-s", "--solution")
                .WithDescription("VS solution file name with path for analysis. If defined, the solution will be compiled, message flow information is generated and saved into an input file.")
                .WithExamples("path\\to\\solution\\file.sln")
                .IsOptional()

                .Parameter<string>("-idf", "--input-data-file")
                .WithDescription("Data file name with path to use for diagram generation.")
                .WithExamples("path\\to\\visualizer\\data\\file")
                .IsOptionalWithDefault(inputDataFileDefaultValue)

                .Parameter<string>("-o", "--output")
                .WithDescription("Output SVG file name with path.")
                .WithExamples("path\\to\\output\\file.svg")
                .IsOptional()

                .Parameter<string>("-sp", "--starting-producer")
                .WithDescription("Starts walking the message flow graph from the given producer and follows out edges only.")
                .WithExamples("ETR.Backend.JET.UiCommandConsumers.Resource.ProcessResourcesChanges.ProcessResourcesChangesConsumer")
                .IsOptional()

                
                .Flag("-lbep", "--list-business-entry-points")
                .WithDescription("List all defined business process entry points, " +
                                 "defined in message handlers' XML comments (businessProcessEntryPointDescription). They can be used as starting producers.")
                
                .Call(listEntryPoints => startingProducer => svgOutputFilePath => inputDataFilePath => async solutionFilePath =>
                {
                    await app.Run(solutionFilePath, inputDataFilePath, svgOutputFilePath, startingProducer, listEntryPoints);
                })
                .ParseAsync(args);
        }

        private static void ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IApp, App>();
            serviceCollection.AddSingleton<ISymbolFinder, SymbolFinder>();
            serviceCollection.AddSingleton<IMessageFlowSymbolCollector, MessageFlowSymbolCollector>();
            serviceCollection.AddSingleton<IMessageFlowSymbolConverter, MessageFlowSymbolConverter>();
            serviceCollection.AddSingleton<IGraphWalkerAlgorithms, GraphWalkerAlgorithms>();
            serviceCollection.AddSingleton<IGraphvizDotDiagramGenerator, GraphvizDotDiagramGenerator>();

            serviceProvider = serviceCollection.BuildServiceProvider();
        }
    }
}
