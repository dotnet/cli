using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.PlatformAbstractions;
using System.Diagnostics;

namespace Microsoft.DotNet.Cli
{
    public class Telemetry : ITelemetry
    {
        public const double TimeoutInMilliseconds = 100;

        private bool _isInitialized = false;
        private TelemetryClient _client = null;

        private Dictionary<string, string> _commonProperties = null;
        private Dictionary<string, double> _commonMeasurements = null;
        private Task _trackEventTask = null;

        private const int ReciprocalSampleRateValue = 5;
        private const int ReciprocalSampleRateValueForCI = 1000;

        private static bool _shouldLogTelemetry = (Environment.TickCount % ReciprocalSampleRateValue == 0);
        private const string InstrumentationKey = "74cc1c9e-3e6e-4d05-b3fc-dde9101d0254";
        private const string TelemetryOptout = "DOTNET_CLI_TELEMETRY_OPTOUT";
        private const string ContinousIntegrationFlag = "CI_TEST_MACHINE";
        private const string CITestMachine = "CI Test Machine";
        private const string OSVersion = "OS Version";
        private const string OSPlatform = "OS Platform";
        private const string RuntimeId = "Runtime Id";
        private const string ProductVersion = "Product Version";
        private const string ReciprocalSampleRate = "Reciprocal SampleRate";

        public Telemetry()
        {
            bool optout = Env.GetEnvironmentVariableAsBool(TelemetryOptout);

            if (optout)
            {
                return;
            }

            int sampleRate = ReciprocalSampleRateValue;
            bool isCITestMachine = Env.GetEnvironmentVariableAsBool(ContinousIntegrationFlag);

			if(isCITestMachine)
            {
                sampleRate = ReciprocalSampleRateValueForCI;
                _shouldLogTelemetry = (Environment.TickCount % ReciprocalSampleRateValue == 0);    
            }
            if(!_shouldLogTelemetry)
            {
                return;
            }

            try
            {
                _client = new TelemetryClient();
                _client.InstrumentationKey = InstrumentationKey;
                _client.Context.Session.Id = Guid.NewGuid().ToString();

                
                var runtimeEnvironment = PlatformServices.Default.Runtime;
                _client.Context.Device.OperatingSystem = runtimeEnvironment.OperatingSystem;

                _commonProperties = new Dictionary<string, string>();
                _commonProperties.Add(OSVersion, runtimeEnvironment.OperatingSystemVersion);
                _commonProperties.Add(OSPlatform, runtimeEnvironment.OperatingSystemPlatform.ToString());
                _commonProperties.Add(RuntimeId, runtimeEnvironment.GetRuntimeIdentifier());
                _commonProperties.Add(ProductVersion, Product.Version);
                _commonProperties.Add(CITestMachine, isCITestMachine.ToString());
                _commonProperties.Add(ReciprocalSampleRate, sampleRate.ToString());
                _commonMeasurements = new Dictionary<string, double>();

                _isInitialized = true;
            }
            catch (Exception)
            {
                // we dont want to fail the tool if telemetry fais. We should be able to detect abnormalities from data 
                // at the server end
                Debug.Fail("Exception during telemetry initialization");
            }
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
        {
            if (!_isInitialized)
            {
                return;
            }
            if(!_shouldLogTelemetry)
            {
                return;
            }                

            _trackEventTask = Task.Factory.StartNew(
                () => TrackEventTask(eventName, properties, measurements)
            );
        }
        
        private void TrackEventTask(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
        {
            Dictionary<string, double> eventMeasurements = GetEventMeasures(measurements);
            Dictionary<string, string> eventProperties = GetEventProperties(properties);

            try
            {
                _client.TrackEvent(eventName, eventProperties, eventMeasurements);
                _client.Flush();
            }
            catch (Exception) 
            {
                Debug.Fail("Exception during TrackEvent");
            }
        }

        public void Finish()
        {
            if(_trackEventTask != null)
            {
                _trackEventTask.Wait(TimeSpan.FromMilliseconds(TimeoutInMilliseconds));
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
