﻿// Copyright 2017 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Api.Gax;
using Google.Cloud.Diagnostics.Common;
using Google.Cloud.Trace.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;

using TraceProto = Google.Cloud.Trace.V1.Trace;

namespace Google.Cloud.Diagnostics.AspNetCore
{
    /// <summary>
    ///  Uses the Google Cloud Trace Middleware.
    ///  Traces the time taken for all subsequent delegates to run.  The time taken
    ///  and metadata will be sent to the Stackdriver Trace API.  Also allows for more
    ///  finely grained manual tracing.
    /// </summary>
    /// 
    /// <example>
    /// <code>
    /// public void ConfigureServices(IServiceCollection services)
    /// {
    ///     string projectId = "[Google Cloud Platform project ID]";
    ///     services.AddGoogleTrace(projectId);
    ///     ...
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// <code>
    /// public void Configure(IApplicationBuilder app)
    /// {
    ///     // Use at the start of the request pipeline to ensure the entire
    ///     // request is traced.
    ///     app.UseGoogleTrace();
    ///     ...
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// <code>
    /// public void SomeFunction(IManagedTracer tracer)
    /// {
    ///     using (tracer.StartSpan(nameof(SomeFunction)))
    ///     {
    ///         ...
    ///         // Do work.
    ///         ...
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <remarks>
    /// Traces requests and reports them to Google Cloud Trace.
    /// Docs: https://cloud.google.com/trace/docs/
    /// </remarks>
    public static class CloudTraceExtension
    {
        /// <summary>
        /// Uses middleware that will trace time taken for all subsequent delegates to run.
        /// The time taken and metadata will be sent to the Stackdriver Trace API. To be
        /// used with <see cref="AddGoogleTrace"/>,
        /// </summary>
        public static void UseGoogleTrace(this IApplicationBuilder app)
        {
            GaxPreconditions.CheckNotNull(app, nameof(app));
            app.UseMiddleware<CloudTraceMiddleware>();
        }

        /// <summary>
        /// Adds the needed services for Google Cloud Tracing. Used with <see cref="UseGoogleTrace"/>.
        /// </summary>
        /// <param name="services">The service collection. Cannot be null.</param>
        /// <param name="setupAction">Action to set up options. Cannot be null.</param>
        /// <remarks>
        /// If <see cref="RetryOptions.ExceptionHandling"/> is set to <see cref="ExceptionHandling.Propagate"/>
        /// and the <see cref="RequestDelegate"/> executed by this middleware throws ad exception and this
        /// diagnostics library also throws an exception trying to report it and <see cref="AggregateException"/>
        /// with both exceptions will be thrown.  Otherwise only the exception from the <see cref="RequestDelegate"/>
        /// will be thrown.
        /// </remarks>
        public static void AddGoogleTrace(
            this IServiceCollection services, Action<TraceServiceOptions> setupAction)
        {
            GaxPreconditions.CheckNotNull(services, nameof(services));
            GaxPreconditions.CheckNotNull(setupAction, nameof(setupAction));

            var serviceOptions = new TraceServiceOptions();
            setupAction(serviceOptions);

            var client = serviceOptions.Client ?? TraceServiceClient.Create();
            var options = serviceOptions.Options ?? TraceOptions.Create();
            var traceFallbackPredicate = serviceOptions.TraceFallbackPredicate ?? TraceDecisionPredicate.Default;
            var projectId = Project.GetAndCheckProjectId(serviceOptions.ProjectId);

            var consumer = ManagedTracer.CreateConsumer(client, options);
            var tracerFactory = ManagedTracer.CreateTracerFactory(projectId, consumer, options);

            services.AddScoped(CreateTraceHeaderContext);
            
            services.AddSingleton<Func<TraceHeaderContext, IManagedTracer>>(tracerFactory);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton(CreateManagedTracer);
            services.AddSingleton(CreateTraceHeaderPropagatingHandler);
            services.AddSingleton(traceFallbackPredicate);
        }

        /// <summary>
        /// Creates an <see cref="TraceHeaderPropagatingHandler"/>.
        /// </summary>
        private static TraceHeaderPropagatingHandler CreateTraceHeaderPropagatingHandler(IServiceProvider provider)
        {
            var tracer = provider.GetServiceCheckNotNull<IManagedTracer>();
            return new TraceHeaderPropagatingHandler(() => tracer);
        }

        /// <summary>
        /// Creates an <see cref="TraceHeaderContext"/> based on the current <see cref="HttpContext"/>
        /// and a <see cref="TraceDecisionPredicate"/>.
        /// </summary>
        internal static TraceHeaderContext CreateTraceHeaderContext(IServiceProvider provider)
        {
            var accessor = provider.GetServiceCheckNotNull<IHttpContextAccessor>();
            var traceDecisionPredicate = provider.GetServiceCheckNotNull<TraceDecisionPredicate>();

            string header = accessor.HttpContext?.Request?.Headers[TraceHeaderContext.TraceHeader];
            Func<bool?> shouldTraceFunc = () =>
                traceDecisionPredicate.ShouldTrace(accessor.HttpContext?.Request);
            return TraceHeaderContext.FromHeader(header, shouldTraceFunc);
        }

        /// <summary>
        /// Creates a singleton <see cref="IManagedTracer"/> that is a <see cref="DelegatingTracer"/>.
        /// The <see cref="DelegatingTracer"/> will use an <see cref="IHttpContextAccessor"/> under the hood
        /// to get the current tracer which is set by the <see cref="ContextTracerManager"/>.
        /// </summary>
        internal static IManagedTracer CreateManagedTracer(IServiceProvider provider)
        {
            var accessor = provider.GetServiceCheckNotNull<IHttpContextAccessor>();
            return ManagedTracer.CreateDelegatingTracer(() => ContextTracerManager.GetCurrentTracer(accessor));
        }
    }
}
