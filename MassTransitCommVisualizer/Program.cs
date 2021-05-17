using System.Threading.Tasks;
using FluentArgs;

namespace MassTransitCommVisualizer
{
    class Program
    {
        static Task Main(string[] args)
        {
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
                .WithExamples("path\\to\\visualizer\\data\\file.json")
                .IsOptionalWithDefault("messageflowdata.dat")

                .Parameter<string>("-so", "--svgoutput")
                .WithDescription("output SVG file name with path")
                .WithExamples("path\\to\\output\\file.svg")
                .IsRequired()
                
                .Parameter<bool>("-imco", "--inter-module-comm-only")
                .WithDescription("show the inter-module messages only")
                .IsOptionalWithDefault(false)
                .Call(interModuleCommOnly => svgOutputFilePath => inputDataFilePath => async solutionFilePath =>
                {
                    await App.Run(solutionFilePath, inputDataFilePath,  svgOutputFilePath, interModuleCommOnly);
                })
                .ParseAsync(args);
        }
    }
}
