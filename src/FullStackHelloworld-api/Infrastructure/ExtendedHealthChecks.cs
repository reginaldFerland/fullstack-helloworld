﻿using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FullStackHelloworld_api.Infrastructure;

public class ExtendedHealthChecks : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
                HealthCheckResult.Healthy("A healthy result."));
    }
}
