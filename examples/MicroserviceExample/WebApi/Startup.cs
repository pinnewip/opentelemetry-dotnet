// <copyright file="Startup.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
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
// </copyright>

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Utils.Messaging;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton<MessageSender>();

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            services.AddControllers();

            services.AddOpenTelemetryTracing((builder) => builder
                .AddAspNetCoreInstrumentation()
                .SetSampler(new AlwaysOnSampler())
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Webapi test api"))
                .AddSource(nameof(MessageSender))
                .AddConsoleExporter(c =>
                {
                    c.Targets = OpenTelemetry.Exporter.ConsoleExporterOutputTargets.Console;
                })
                .AddOtlpExporter(otlpOptions =>
                {
                    var apmHostName = Environment.GetEnvironmentVariable("APM_HOSTNAME") ?? "localhost";
                    otlpOptions.Endpoint = new Uri($"http://{apmHostName}:8200");
                })
                .AddZipkinExporter(b =>
                {
                    var zipkinHostName = Environment.GetEnvironmentVariable("ZIPKIN_HOSTNAME") ?? "localhost";
                    b.Endpoint = new Uri($"http://{zipkinHostName}:9411/api/v2/spans");
                }));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
