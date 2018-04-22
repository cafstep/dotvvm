﻿using DotVVM.Tracing.ApplicationInsights;
using DotVVM.Tracing.ApplicationInsights.Owin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DotVVM.Framework.Configuration
{
    public static class TracingBuilderExtensions
    {
        /// <summary>
        /// Registers ApplicationInsightsTracer
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IDotvvmServiceCollection AddApplicationInsightsTracing(this IDotvvmServiceCollection services)
        {
            services.AddApplicationInsightsTracingInternal();
            services.Services.AddTransient<IConfigureOptions<DotvvmConfiguration>, ApplicationInsightSetup>();

            return services;
        }
    }

    internal class ApplicationInsightSetup : IConfigureOptions<DotvvmConfiguration>
    {
        public void Configure(DotvvmConfiguration options)
        {
            options.Markup.AddCodeControls("dot", typeof(ApplicationInsightsJavascript));
        }
    }
}
