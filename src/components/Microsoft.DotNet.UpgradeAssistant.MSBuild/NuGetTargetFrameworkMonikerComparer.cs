﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;

namespace Microsoft.DotNet.UpgradeAssistant.MSBuild
{
    public class NuGetTargetFrameworkMonikerComparer : ITargetFrameworkMonikerComparer
    {
        private readonly ILogger<NuGetTargetFrameworkMonikerComparer> _logger;

        public NuGetTargetFrameworkMonikerComparer(ILogger<NuGetTargetFrameworkMonikerComparer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int Compare(TargetFrameworkMoniker? x, TargetFrameworkMoniker? y)
        {
            if (x is null && y is null)
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var dependent = NuGetFramework.Parse(x.Name);
            if (dependent.IsUnsupported)
            {
                _logger.LogWarning("Unrecognized TFM: {TFM}", x.Name);
            }

            var dependency = NuGetFramework.Parse(y.Name);
            if (dependency.IsUnsupported)
            {
                _logger.LogWarning("Unrecognized TFM: {TFM}", y.Name);
            }

            if (dependent.Equals(dependency))
            {
                return 0;
            }

            if (y.IsNetStandard && x.IsFramework)
            {
                return -1;
            }

            var dependentToDependency = DefaultCompatibilityProvider.Instance.IsCompatible(dependent, dependency);
            var dependencyToDependent = DefaultCompatibilityProvider.Instance.IsCompatible(dependency, dependent);

            return (dependentToDependency, dependencyToDependent) switch
            {
                (true, true) => 0,
                (false, true) => -1,
                (true, false) => 1,
                (false, false) => -1,
            };
        }

        public bool IsCompatible(TargetFrameworkMoniker tfm, TargetFrameworkMoniker other)
        {
            if (tfm is null)
            {
                throw new ArgumentNullException(nameof(tfm));
            }

            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return Compare(tfm, other) >= 0;
        }
    }
}
