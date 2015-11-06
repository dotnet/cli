// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Dnx.TestHost.Client
{
    public class TestHostWrapper : IDisposable
    {
        public DataReceivedEventHandler ConsoleOutputReceived;

        public EventHandler<Message> MessageReceived;

        public TestHostWrapper(string project)
            : this(project, TestHost.Client.DNX.FindDnx(), debug: false)
        {
        }

        public TestHostWrapper(string project, string dnx, bool debug)
        {
            Project = project;
            DNX = dnx;
            Debug = debug;

            Output = new List<Message>();
        }

        public int? ProtocolVersion { get; set; }

        public int? DTHPort { get; set; }

        private TcpClient Client { get; set; }

        private bool Debug { get; }

        private string DNX { get; }

        private int Port { get; set; }

        public Process Process { get; private set; }

        private string Project { get; }

        public IList<Message> Output { get; }

        public int? ParentProcessId { get; set; }

        public async Task StartAsync()
        {
            if (Process != null)
            {
                throw new InvalidOperationException("TestHost cannot be reused.");
            }

            var project = Project;
            if (Project.EndsWith("project.json", StringComparison.OrdinalIgnoreCase))
            {
                project = Path.GetDirectoryName(Project);
            }

            Port = FindFreePort();

            var arguments = new List<string>();
            arguments.Add("--port");
            arguments.Add(Port.ToString());

            arguments.Add("--project");
            arguments.Add(project);

            if (Debug)
            {
                arguments.Add("--debug");
            }

            if (ParentProcessId.HasValue)
            {
                arguments.Add("--parentProcessId");
                arguments.Add(ParentProcessId.Value.ToString());
            }

            var allArgs = "Microsoft.Dnx.TestHost " + string.Join(" ", arguments.Select(Quote));
            if (DTHPort != null)
            {
                allArgs = "--port " + DTHPort + " " + allArgs;
            }

            Process = new Process();
            Process.StartInfo = new ProcessStartInfo
            {
                Arguments = allArgs,
                CreateNoWindow = true,
                FileName = DNX,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = project,
            };

            Process.StartInfo.EnvironmentVariables.Add("DNX_TESTHOST_TRACE", "1");

            Process.OutputDataReceived += Process_OutputDataReceived;
            Process.ErrorDataReceived += Process_OutputDataReceived;

            Process.Start();
            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();

            var client = new TcpClient();
            for (var i = 0; i < 50; i++)
            {
                try
                {
                    await client.ConnectAsync(IPAddress.Loopback, Port);
                    break;
                }
                catch (SocketException)
                {
                    await Task.Delay(100);
                }
            }

            if (!client.Connected)
            {
                client.Close();
                throw new Exception("Unable to connect.");
            }

            Client = client;
        }

        public async Task<int> ListTestsAsync()
        {
            // This will block until the test host replies
            var payload = (object)null;

            var listener = Task.Run(() => StartCommandAndWaitForResponse("TestDiscovery.Start", payload, "TestDiscovery.Response"));

            Process.WaitForExit();

            await listener;

            return Process.ExitCode;
        }

        public async Task<int> RunTestsAsync(params string[] tests)
        {
            // This will block until the test host replies
            var payload = new RunTestsMessage();
            payload.Tests = tests == null ? new List<string>() : new List<string>(tests);

            var listener = Task.Run(() => StartCommandAndWaitForResponse("TestExecution.Start", payload, "TestExecution.Response"));

            Process.WaitForExit();

            await listener;

            return Process.ExitCode;
        }

        private void OnMessageReceived(object sender, Message e)
        {
            Output.Add(e);

            var handler = MessageReceived;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var handler = ConsoleOutputReceived;
            if (handler != null && e.Data != null)
            {
                handler(sender, e);
            }
        }

        private static string Quote(string arg)
        {
            return "\"" + arg + "\"";
        }

        private void StartCommandAndWaitForResponse(string messageType, object payload, string terminalMessageType)
        {
            try
            {
                var stream = Client.GetStream();

                using (var writer = new BinaryWriter(stream))
                using (var reader = new BinaryReader(stream))
                {
                    // If we're using a ProtocolVersion then establish that with the TestHost
                    if (ProtocolVersion.HasValue)
                    {
                        writer.Write(JsonConvert.SerializeObject(new Message()
                        {
                            MessageType = "ProtocolVersion",
                            Payload = JToken.FromObject(new ProtocolVersionMessage()
                            {
                                Version = ProtocolVersion.Value,
                            }),
                        }));

                        var message = JsonConvert.DeserializeObject<Message>(reader.ReadString());
                        OnMessageReceived(this, message);

                        if (message.MessageType == "ProtocolVersion")
                        {
                            ProtocolVersion = message.Payload?.ToObject<ProtocolVersionMessage>().Version;
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"Invalid ProtocolVersion response {message.MessageType}");
                        }
                    }

                    writer.Write(JsonConvert.SerializeObject(new Message
                    {
                        MessageType = messageType,
                        Payload = payload == null ? null : JToken.FromObject(payload),
                    }));

                    while (true)
                    {
                        var message = JsonConvert.DeserializeObject<Message>(reader.ReadString());
                        OnMessageReceived(this, message);

                        if (string.Equals(message.MessageType, terminalMessageType) ||
                            string.Equals(message.MessageType, "Error"))
                        {
                            writer.Write(JsonConvert.SerializeObject(new Message
                            {
                                MessageType = "TestHost.Acknowledge"
                            }));

                            break;
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Thrown when the socket is closed by the test process.
            }
            catch (EndOfStreamException)
            {
                // Thrown if nothing was written by the test process.
            }
        }

        private int FindFreePort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }

        public void Dispose()
        {
            if (Client != null)
            {
                Client.Close();
                Client = null;
            }
        }
    }
}
