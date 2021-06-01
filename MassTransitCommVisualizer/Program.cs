using System;
using System.Threading.Tasks;
using FluentArgs;
using Microsoft.Extensions.DependencyInjection;

namespace MassTransitCommVisualizer
{
    class Program
    {
        private static ServiceProvider serviceProvider;

        static Task Main(string[] args)
        {
            ConfigureServices();

            return FluentArgsBuilder.New()
                .DefaultConfigsWithAppDescription(
                    "This application opens the given VS solution file, analyzes its message communication and outputs it in an SVG file.")
                // Parameter "solution": if defined, app opens and compiles the given VS solution
                // and retrieves the messages, the message senders and producers
                .Parameter<string>("-s", "--solution")
                .WithDescription("VS solution file name with path for analysis")
                .WithExamples("path\\to\\solution\\file.sln")
                .IsOptional()

                .Parameter<string>("-idf", "--inputdatafile")
                .WithDescription("Data file name with path to use for diagram generation (VS solution not compiled)")
                .WithExamples("path\\to\\visualizer\\data\\file")
                .IsOptionalWithDefault("messageflowdata.dat")

                .Parameter<string>("-so", "--svgoutput")
                .WithDescription("output SVG file name with path")
                .WithExamples("path\\to\\output\\file.svg")
                .IsRequired()

                .Parameter<string>("-sp", "--starting-producer")
                .WithDescription("starts walking the graph from this point and follow out edges only")
                .WithExamples("-sp ETR.Backend.JET.UiCommandConsumers.Resource.ProcessResourcesChanges.ProcessResourcesChangesConsumer")
                .IsOptional()
                .Call(startingProducer => svgOutputFilePath => inputDataFilePath => async solutionFilePath =>
                {
                    var app = serviceProvider.GetService<IApp>();
                    if (app == null)
                    {
                        throw new ApplicationException("App not found in service provider!");
                    }

                    await app.Run(solutionFilePath, inputDataFilePath, svgOutputFilePath, startingProducer);
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
