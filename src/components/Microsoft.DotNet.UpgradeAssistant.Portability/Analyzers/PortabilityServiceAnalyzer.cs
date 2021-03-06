﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.UpgradeAssistant.Portability.Service;

namespace Microsoft.DotNet.UpgradeAssistant.Portability.Analyzers
{
    internal class PortabilityServiceAnalyzer : IPortabilityAnalyzer
    {
        private readonly IPortabilityService _service;

        public PortabilityServiceAnalyzer(IPortabilityService service)
        {
            _service = service;
        }

        public async IAsyncEnumerable<PortabilityResult> Analyze(Compilation compilation, [EnumeratorCancellation] CancellationToken token)
        {
            var contains = await GetMemberAccessSymbols(compilation, token)
                .Select(s => s?.GetDocumentationCommentId())
                .Where(s => s is not null)
                .Distinct()
                .ToListAsync(token).ConfigureAwait(false);

            await foreach (var api in _service.GetApiInformation(contains!, token))
            {
                if (!api.IsSupported())
                {
                    yield return new PortabilityResult(ApiType.Method, api.Definition.FullName, api.RecommendedChanges);
                }
            }
        }

        private static async IAsyncEnumerable<ISymbol> GetMemberAccessSymbols(Compilation compilation, [EnumeratorCancellation] CancellationToken token)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var semantic = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync(token).ConfigureAwait(false);
                var symbols = root.DescendantNodes(_ => true)
                    .Where(node => node.IsKind(CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression))
                    .Select(s => semantic.GetSymbolInfo(s).Symbol);

                foreach (var s in symbols)
                {
                    if (s is not null)
                    {
                        yield return s;
                    }
                }
            }
        }
    }
}
