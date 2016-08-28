// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Configurer;
using Microsoft.DotNet.PlatformAbstractions;

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

        public bool Enabled { get; }

        public Telemetry () : this(null) { }

        public Telemetry(INuGetCacheSentinel sentinel)
        {
            Enabled = !Env.GetEnvironmentVariableAsBool(TelemetryOptout) && PermissionExists(sentinel);

            if (!Enabled)
            {
                return;
            }

            //initialize in task to offload to parallel thread
            _trackEventTask = Task.Factory.StartNew(() => InitializeTelemetry());
        }

        private bool PermissionExists(INuGetCacheSentinel sentinel)
        {
            if (sentinel == null)
            {
                return false;
            }

            return sentinel.Exists();
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
    }
}
