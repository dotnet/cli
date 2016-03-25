// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.Tasks;
using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Microsoft.DotNet.InstallScripts.Tests
{
    public class TestServer : IDisposable
    {
        private Dictionary<string, RequestDelegate> _pathsMappings;
        private Counter<string> _requestCounts;
        public int PageNotFoundHits { get; private set; }
        private IWebHost _host;

        public RequestDelegate PathNotFoundHandler { get; set; }
        public string Url { get; private set; }

        public TestServer()
        {
            _pathsMappings = new Dictionary<string, RequestDelegate>();
            _requestCounts = new Counter<string>();
            RequestCounts = new RequestCountsNormalizer(_requestCounts);
            PathNotFoundHandler = DefaultPathNotFoundHandler;
        }

        public RequestDelegate this[string path]
        {
            get
            {
                RequestDelegate requestHandler;
                _pathsMappings.TryGetValue(NormalizeServerPath(path), out requestHandler);
                return requestHandler;
            }
            set
            {
                _pathsMappings[NormalizeServerPath(path)] = value;
            }
        }
        
        public RequestCountsNormalizer RequestCounts { get; private set; }

        public Task DefaultPathNotFoundHandler(HttpContext context)
        {
            context.Response.StatusCode = 404;
            return context.Response.WriteAsync($"404 Path {context.Request.Path} not found!");
        }

        private RequestDelegate ExceptionPrintingHandlerWrapper(RequestDelegate other)
        {
            return async (context) => {
                try
                {
                    await other(context);
                }
                catch (Exception e)
                {
                    context.Response.StatusCode = 500;
                    await SendText(e.ToString())(context);
                }
            };
        }
        
        private Task PathHandlerOrDefault(HttpContext context)
        {
            if (_pathsMappings == null)
            {
                return MappingsNotSetHandler(context);
            }

            string path = NormalizeServerPath(context.Request.Path);
            _requestCounts.Increment(path);

            RequestDelegate handler;
            if (_pathsMappings.TryGetValue(path, out handler))
            {
                return handler(context);
            }
            else
            {
                PageNotFoundHits++;
                return PathNotFoundHandler(context);
            }
        }

        public static RequestDelegate SendFile(string filePath, string contentType = "application/octet-stream")
        {
            return (context) =>
            {
                context.Response.ContentType = contentType;
                return context.Response.SendFileAsync(filePath);
            };
        }
        
        public static RequestDelegate SendStream(Stream stream, string contentType = "application/octet-stream")
        {
            return (context) =>
            {
                context.Response.ContentType = contentType;
                return stream.CopyToAsync(context.Response.Body);
            };
        }

        public static RequestDelegate SendText(string text)
        {
            return context => context.Response.WriteAsync(text);
        }

        private Task MappingsNotSetHandler(HttpContext context)
        {
            context.Response.StatusCode = 500;
            return context.Response.WriteAsync($"500 Server path mappings are set to null!");
        }

        public void Configure(IApplicationBuilder app)
        {
            app.ServerFeatures.Set<TestServer>(this);
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.All
            });

            app.Run(ExceptionPrintingHandlerWrapper(PathHandlerOrDefault));
        }

        public static string NormalizeServerPath(string path)
        {
            path = path.ToLower();
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            return path;
        }

        public static TestServer Create()
        {
            Exception lastException = null;
            // Try few times just in case different process takes it in between the call
            for (int creationAttempt = 0; creationAttempt < 10; creationAttempt++)
            {
                IWebHost host = null;
                try
                {
                    TestServer ret = null;

                    var hostBuilder = new WebHostBuilder();
                    hostBuilder.UseServer("Microsoft.AspNetCore.Server.Kestrel");
                    hostBuilder.UseContentRoot(Directory.GetCurrentDirectory());
                    var cb = new ConfigurationBuilder();
                    hostBuilder.UseConfiguration(cb.Build());
                    hostBuilder.UseStartup<TestServer>();
                    string url;
                    lock (_freePortLock)
                    {
                        url = $"http://localhost:{GetFreePort()}";
                        hostBuilder.UseUrls(url);

                        host = hostBuilder.Build();
                        host.Start();
                    }

                    ret = host.ServerFeatures.Get<TestServer>();
                    ret._host = host;
                    ret.Url = url;

                    return ret;
                }
                catch (Exception e)
                {
                    lastException = e;
                    host?.Dispose();
                }
            }

            throw lastException;
        }

        public static TestServer Create(Dictionary<string, RequestDelegate> mappings)
        {
            TestServer ret = Create();
            foreach (var mapping in mappings)
            {
                ret[mapping.Key] = mapping.Value;
            }

            return ret;
        }

        public void Dispose()
        {
            _host?.Dispose();
            _host = null;
        }

        private static object _freePortLock = new object();
        private static int GetFreePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
        
        public class RequestCountsNormalizer
        {
            public Counter<string> Counter { get; private set; }
            
            public RequestCountsNormalizer(Counter<string> counter)
            {
                Counter = counter;
            }
            
            public int this[string path]
            {
                get
                {
                    return Counter[TestServer.NormalizeServerPath(path)];
                }
            }
        }
    }
}
