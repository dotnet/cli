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

        private bool _isCollectingTelemetry = false;
        
        
        private TelemetryClient _client = null;

        private Dictionary<string, string> _commonProperties = null;
        private Dictionary<string, double> _commonMeasurements = null;
        private Task _trackEventTask = null;

        private int _sampleRate = 1;
        private bool _isTestMachine = false;

        private const int ReciprocalSampleRateValue = 1;
        private const int ReciprocalSampleRateValueForTest = 1;        
        private const string InstrumentationKey = "74cc1c9e-3e6e-4d05-b3fc-dde9101d0254";
        private const string TelemetryOptout = "DOTNET_CLI_TELEMETRY_OPTOUT";
        private const string TestMachineFlag = "TEST_MACHINE";
        private const string TestMachine = "Test Machine";
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

            _sampleRate = ReciprocalSampleRateValue;
            _isTestMachine = Env.GetEnvironmentVariableAsBool(TestMachineFlag);

            if(_isTestMachine)
            {
                _sampleRate = ReciprocalSampleRateValueForTest;
            }
            
            bool shouldLogTelemetry = Environment.TickCount % _sampleRate == 0;

            if(!shouldLogTelemetry)
            {
                return;
            }

            _isCollectingTelemetry = true;
            try
            {
                //initialize in task to offload to parallel thread
                _trackEventTask = Task.Factory.StartNew(() => InitializeTelemetry());
            }
            catch(Exception)
            {
                Debug.Fail("Exception during telemetry task initialization");
            }
        }
        
        public void TrackEvent(string eventName, IList<string> properties, IDictionary<string, double> measurements)
        {
            if (!_isCollectingTelemetry)
            {
                return;
            }
            
            try
            {
                _trackEventTask = _trackEventTask.ContinueWith(
                    () => TrackEventTask(eventName, 
                        properties, 
                        measurements)
                );
            }
            catch(Exception)
            {
                Debug.Fail("Exception during telemetry task continuation");
            }
        }
        
        private void InitializeTelemetry()
        {
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
                _commonProperties.Add(TestMachine, _isTestMachine.ToString());
                _commonProperties.Add(ReciprocalSampleRate, _sampleRate.ToString());
                _commonMeasurements = new Dictionary<string, double>();

            }
            catch (Exception)
            {
                _isCollectingTelemetry = false;
                // we dont want to fail the tool if telemetry fais. We should be able to detect abnormalities from data 
                // at the server end
                Debug.Fail("Exception during telemetry initialization");
                return;
            }
        }
        
        private void TrackEventTask(string eventName, IList<string> properties, IDictionary<string, double> measurements)
        {
            if (!_isCollectingTelemetry)
            {
                return;
            }

            var hashedargs = string.Join(",", HashHelper.Sha256HashList(properties));
            _commonProperties.Add("HashedArgs", hashedargs);

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
