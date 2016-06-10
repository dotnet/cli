// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.InternalAbstractions;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace Microsoft.DotNet.Cli
{
    public class Telemetry : ITelemetry
    {
        private TelemetryClient _client = null;

        private Dictionary<string, string> _commonProperties = null;
        private Dictionary<string, double> _commonMeasurements = null;
        private Task _trackEventTask = null;

        private const string InstrumentationKey = "74cc1c9e-3e6e-4d05-b3fc-dde9101d0254";
        private const string TelemetryOptout = "DOTNET_CLI_TELEMETRY_OPTOUT";
        private const string TelemetryProfileEnvironmentVariable = "DOTNET_CLI_TELEMETRY_PROFILE";
        private const string OSVersion = "OS Version";
        private const string OSPlatform = "OS Platform";
        private const string RuntimeId = "Runtime Id";
        private const string ProductVersion = "Product Version";
        private const string TelemetryProfile = "Telemetry Profile";
        private const string MachineID = "Machine Id";
        private const string AllMachineIDs = "All Machine Ids";

        public bool Enabled { get; }

        public Telemetry()
        {
            Enabled = !Env.GetEnvironmentVariableAsBool(TelemetryOptout);

            if (!Enabled)
            {
                return;
            }

            //initialize in task to offload to parallel thread
            _trackEventTask = Task.Factory.StartNew(() => InitializeTelemetry());
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
        {
            if (!Enabled)
            {
                return;
            }

            //continue task in existing parallel thread
            _trackEventTask = _trackEventTask.ContinueWith(
                x => TrackEventTask(eventName, properties, measurements)
            );
        }

        private void InitializeTelemetry()
        {
            try
            {
                _client = new TelemetryClient();
                _client.InstrumentationKey = InstrumentationKey;
                _client.Context.Session.Id = Guid.NewGuid().ToString();

                _client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;

                _commonProperties = new Dictionary<string, string>();
                _commonProperties.Add(OSVersion, RuntimeEnvironment.OperatingSystemVersion);
                _commonProperties.Add(OSPlatform, RuntimeEnvironment.OperatingSystemPlatform.ToString());
                _commonProperties.Add(RuntimeId, RuntimeEnvironment.GetRuntimeIdentifier());
                _commonProperties.Add(ProductVersion, Product.Version);
                _commonProperties.Add(TelemetryProfile, Environment.GetEnvironmentVariable(TelemetryProfileEnvironmentVariable));
                var hashedMacs = HashSha256(GetMacs());
                _commonProperties.Add(MachineID, hashedMacs.FirstOrDefault());
                _commonProperties.Add(AllMachineIDs, string.Join(",", hashedMacs));
                _commonMeasurements = new Dictionary<string, double>();
            }
            catch (Exception)
            {
                _client = null;
                // we dont want to fail the tool if telemetry fails.
                Debug.Fail("Exception during telemetry initialization");
            }
        }

        private void TrackEventTask(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                var eventProperties = GetEventProperties(properties);
                var eventMeasurements = GetEventMeasures(measurements);

                _client.TrackEvent(eventName, eventProperties, eventMeasurements);
                _client.Flush();
            }
            catch (Exception)
            {
                Debug.Fail("Exception during TrackEventTask");
            }
        }

        private Dictionary<string, double> GetEventMeasures(IDictionary<string, double> measurements)
        {
            Dictionary<string, double> eventMeasurements = new Dictionary<string, double>(_commonMeasurements);
            if (measurements != null)
            {
                foreach (var measurement in measurements)
                {
                    if (eventMeasurements.ContainsKey(measurement.Key))
                    {
                        eventMeasurements[measurement.Key] = measurement.Value;
                    }
                    else
                    {
                        eventMeasurements.Add(measurement.Key, measurement.Value);
                    }
                }
            }
            return eventMeasurements;
        }

        private Dictionary<string, string> GetEventProperties(IDictionary<string, string> properties)
        {
            if (properties != null)
            {
                var eventProperties = new Dictionary<string, string>(_commonProperties);
                foreach (var property in properties)
                {
                    if (eventProperties.ContainsKey(property.Key))
                    {
                        eventProperties[property.Key] = property.Value;
                    }
                    else
                    {
                        eventProperties.Add(property.Key, property.Value);
                    }
                }
                return eventProperties;
            }
            else
            {
                return _commonProperties;
            }
        }
        
        private List<string> GetMacs()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            var macs = new List<string>();
            
            if (nics == null || nics.Length < 1)
            {
                macs.Add(string.Empty);
                return macs;
            }
            
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();

                PhysicalAddress address = adapter.GetPhysicalAddress();
                byte[] bytes = address.GetAddressBytes();
                macs.Add(string.Join("-", bytes.Select(x => x.ToString("X2"))));
                if (macs.Count >= 10)
                {
                    break;
                }
            }
            return macs;
        }
        
        private List<string> HashSha256(List<string> texts)
        {
            var sha256 = SHA256.Create();
            var hashedStrings = new List<string>();
            foreach (var text in texts)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                byte[] hash = sha256.ComputeHash(bytes);
                string hashString = string.Empty;
                foreach (byte x in hash)
                {
                    hashString += String.Format("{0:x2}", x);
                }
                hashedStrings.Add(hashString);
            }
            return hashedStrings;
        }
    }
}
