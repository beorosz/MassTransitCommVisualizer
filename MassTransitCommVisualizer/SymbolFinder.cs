﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace MassTransitCommVisualizer
{
    public interface ISymbolFinder
    {
        ImmutableArray<IMethodSymbol> GetMethodSymbols(ImmutableHashSet<Compilation> projectCompilations,
            string typeName, string methodName, byte methodArity = default, byte methodParameterCount = default);

        INamedTypeSymbol GetTypeSymbol(ImmutableHashSet<Compilation> projectCompilations, string typeName);

        Task<Dictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>>> FindMethodCallers
        (Solution solution, ImmutableArray<IMethodSymbol> methodSymbols, byte methodParameterCount,
            byte orderOfTypeArgumentToRetrieve);

        Task<Dictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>>> FindNamedTypeSymbolImplementations
            (Solution solution, INamedTypeSymbol interfaceTypeSymbol);
    }

    public class SymbolFinder : ISymbolFinder
    {
        public ImmutableArray<IMethodSymbol> GetMethodSymbols(ImmutableHashSet<Compilation> projectCompilations,
            string typeName, string methodName, byte methodArity, byte methodParameterCount)
        {
            foreach (var compilation in projectCompilations)
            {
                var typeSymbol = compilation.References.Select(compilation.GetAssemblyOrModuleSymbol)
                    .OfType<IAssemblySymbol>()
                    .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeName))
                    .FirstOrDefault(symbol => symbol != null);

                if (typeSymbol == null) continue;

                var methodSymbolsForGivenName = GetBaseTypeMethodsWithName(typeSymbol, methodName, methodArity, methodParameterCount);

                return methodSymbolsForGivenName
                    .ToImmutableArray();
            }

            return ImmutableArray<IMethodSymbol>.Empty;
        }

        private IEnumerable<IMethodSymbol> GetBaseTypeMethodsWithName(INamedTypeSymbol typeSymbol, string methodName, byte methodArity, byte methodParameterCount)
        {
            List<IMethodSymbol> methodSymbols = new List<IMethodSymbol>();

            methodSymbols.AddRange(typeSymbol.GetMembers(methodName)
                .OfType<IMethodSymbol>()
                .Where(methodSymbol => methodSymbol.Arity == methodArity)
                .Where(methodSymbol => methodParameterCount == default || methodSymbol.Parameters.Length == methodParameterCount));

            if (typeSymbol.BaseType != null)
            {
                methodSymbols.AddRange(GetBaseTypeMethodsWithName(typeSymbol.BaseType, methodName, methodArity, methodParameterCount));
            }

            foreach (var baseInterfaceSymbol in typeSymbol.AllInterfaces)
            {
                methodSymbols.AddRange(GetBaseTypeMethodsWithName(baseInterfaceSymbol, methodName, methodArity, methodParameterCount));
            }

            return methodSymbols.AsEnumerable();
        }

        public INamedTypeSymbol GetTypeSymbol(ImmutableHashSet<Compilation> projectCompilations, string typeName)
        {
            foreach (var compilation in projectCompilations)
            {
                var typeSymbol = compilation.References.Select(compilation.GetAssemblyOrModuleSymbol)
                    .OfType<IAssemblySymbol>()
                    .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeName))
                    .FirstOrDefault(symbol => symbol != null);

                if (typeSymbol != null)
                    return typeSymbol;
            }

            return null;
        }

        public async Task<Dictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>>> FindMethodCallers
            (Solution solution, ImmutableArray<IMethodSymbol> methodSymbols, byte methodParameterCount, byte orderOfTypeArgumentToRetrieve)
        {
            var methodCallerTypeWithMethodParamTypesTuples = new Dictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>>();

            foreach (var methodSymbol in methodSymbols)
            {
                var processedLocations = new List<Location>();
                var methodCallers = await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindCallersAsync(methodSymbol, solution);
                foreach (var directMethodCaller in methodCallers.Where(callerInfo => callerInfo.IsDirect))
                {
                    foreach (var location in directMethodCaller.Locations)
                    {
                        // Projects with multiple target frameworks appear multiple times in the solution
                        // Therefore the method symbol is found multiple times, but at the same location
                        // Such excessive calls must be filtered out
                        if (processedLocations.Any(procLoc =>
                            procLoc.SourceTree.FilePath == location.SourceTree.FilePath && procLoc.SourceSpan == location.SourceSpan))
                        {
                            continue;
                        }
                        if (location.IsInSource)
                        {
                            var methodCallerSemanticModel = await solution.GetDocument(location.SourceTree).GetSemanticModelAsync();
                            var root = await location.SourceTree.GetRootAsync();
                            var node = root.FindToken(location.SourceSpan.Start).Parent;
                            var methodCallerSymbolInfo = methodCallerSemanticModel.GetSymbolInfo(node);
                            if (methodCallerSymbolInfo.Symbol is IMethodSymbol calledMethod && calledMethod.TypeArguments.Length == methodParameterCount)
                            {
                                var messagePayloadType = new List<ITypeSymbol> { calledMethod.TypeArguments[orderOfTypeArgumentToRetrieve - 1] };
                                methodCallerTypeWithMethodParamTypesTuples[directMethodCaller.CallingSymbol.ContainingType] =
                                    messagePayloadType.Union(GetValueOrDefault(methodCallerTypeWithMethodParamTypesTuples, directMethodCaller.CallingSymbol.ContainingType)).ToArray();
                            }
                            processedLocations.Add(location);
                        }
                    }
                }
            }

            return methodCallerTypeWithMethodParamTypesTuples;
        }

        public async Task<Dictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>>> FindNamedTypeSymbolImplementations
            (Solution solution, INamedTypeSymbol interfaceTypeSymbol)
        {
            var typeImplementorWithPayloadTypesTuples = new Dictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>>();
            var interfaceImplementations = await Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindImplementationsAsync(
                interfaceTypeSymbol, solution);

            foreach (var interfaceTypeImplementation in interfaceImplementations
                // check the solution project implementations only
                .Where(impl => solution.Projects.Any(proj => impl.ContainingAssembly.Name == proj.AssemblyName)).ToArray())
            {
                // find the given interface type within the implemented interfaces of the implementor class
                // and retrieve the first payload type from the found interface
                // i.e. Interface<PayloadType> => ImplementorClass : Interface<ConcretePayloadType> => ConcretePayloadType
                var firstGenericParamTypes = interfaceTypeImplementation.AllInterfaces
                    .Where(item => item.GetType() == interfaceTypeSymbol.GetType() && item.Arity == interfaceTypeSymbol.Arity)
                    .Select(item => item.TypeArguments[0]);

                typeImplementorWithPayloadTypesTuples[interfaceTypeImplementation] = firstGenericParamTypes.Union(GetValueOrDefault(typeImplementorWithPayloadTypesTuples, interfaceTypeImplementation)).ToArray();
            }

            return typeImplementorWithPayloadTypesTuples;
        }

        private IEnumerable<ITypeSymbol> GetValueOrDefault(Dictionary<INamedTypeSymbol, IEnumerable<ITypeSymbol>> dict, INamedTypeSymbol key)
        {
            if (dict.TryGetValue(key, out var result))
            {
                return result;
            }

            return new List<ITypeSymbol>();
        }
    }
}
