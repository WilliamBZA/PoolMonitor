using nanoFramework.Device.OneWire;
using nanoFramework.Hardware.Esp32;
using nanoFramework.HomeAssistant.MqttDiscovery;
using nanoFramework.HomeAssistant.MqttDiscovery.Items;
using nanoFramework.Networking;
using nanoFramework.Runtime.Native;
using System;
using System.Device.Adc;
using System.Net.NetworkInformation;
using System.Threading;

namespace PoolMonitor
{
    public class Program
    {
        // dfRobot ORP sensor is connected to an ADC pin on the ESP32.
        // Adjust this to match your wiring (e.g., GPIO 34 = ADC channel 6 on ESP32).
        private const int OrpAdcChannel = 6;

        // Reading interval in milliseconds
        private const int ReadIntervalMs = 60000 * 5;

        // DS18B20 commands
        private const byte SkipRomCommand = 0xCC;
        private const byte ConvertTemperatureCommand = 0x44;
        private const byte ReadScratchpadCommand = 0xBE;

        // MQTT broker settings - adjust to match your setup
        private const string MqttBrokerIp = "192.168.88.172";
        private const int MqttBrokerPort = 1883;
        private const string MqttUsername = "";
        private const string MqttPassword = "";

        public static void Main()
        {
            Debug("PoolMonitor starting...");

            ConnectToWiFi();

            // ORP sensor setup
            AdcController adcController = new AdcController();
            AdcChannel orpChannel = adcController.OpenChannel(OrpAdcChannel);

            int adcMaxValue = (int)Math.Pow(2, adcController.ResolutionInBits) - 1;
            Debug($"ADC resolution: {adcController.ResolutionInBits} bits (max value: {adcMaxValue})");

            // DS18B20 temperature sensor setup
            Configuration.SetPinFunction(16, DeviceFunction.COM3_RX);
            Configuration.SetPinFunction(17, DeviceFunction.COM3_TX);
            OneWireHost oneWire = new OneWireHost();

            // Home Assistant MQTT discovery setup
            HomeAssistant ha = new HomeAssistant(
                "Pool Monitor",
                MqttBrokerIp,
                MqttBrokerPort,
                MqttUsername,
                MqttPassword);

            Sensor temperatureSensor = ha.AddSensor("Water Temperature", "°C", "0", DeviceClass.Temperature);
            Sensor orpSensor = ha.AddSensor("ORP", "mV", "0", DeviceClass.Voltage);

            Debug("Connecting to MQTT broker...");
            ha.Connect();
            Debug("Connected to MQTT broker. Device registered with Home Assistant.");

            var rebootTimer = new Timer((state) =>
            {
                Power.RebootDevice(nanoFramework.Runtime.Native.RebootOption.NormalReboot);
            }, null, 1000 * 60 * 60, 1000 * 60 * 60); // Reboot device every hour

            while (true)
            {
                double orpMv = ReadOrpMillivolts(orpChannel);
                double temperatureC = ReadTemperature(oneWire);

                Debug($"ORP Voltage: {orpMv:F1} mV - Temp: {temperatureC:F2}C");

                orpSensor.UpdateValue(orpMv.ToString("F1"));

                if (temperatureC != double.MinValue)
                {
                    temperatureSensor.UpdateValue(temperatureC.ToString("F2"));
                }

                Thread.Sleep(ReadIntervalMs);
            }
        }

        /// <summary>
        /// Reads the raw ADC value from the ORP sensor and converts to millivolts.
        /// Takes multiple samples and averages to reduce noise.
        /// </summary>
        private static double ReadOrpMillivolts(AdcChannel channel)
        {
            const int sampleCount = 100;
            int total = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                total += channel.ReadValue();
                Thread.Sleep(10);
            }

            double averageRaw = total / (double)sampleCount;

            return averageRaw;
        }

        /// <summary>
        /// Reads the temperature from a DS18B20 sensor on the 1-Wire bus.
        /// Assumes a single DS18B20 is connected (uses Skip ROM).
        /// </summary>
        private static double ReadTemperature(OneWireHost oneWire)
        {
            if (!oneWire.TouchReset())
            {
                Debug("DS18B20: No device found on 1-Wire bus");
                return double.MinValue;
            }

            // Skip ROM (only one device on the bus) and start temperature conversion
            oneWire.WriteByte(SkipRomCommand);
            oneWire.WriteByte(ConvertTemperatureCommand);

            // Wait for conversion to complete (750ms for 12-bit resolution)
            Thread.Sleep(750);

            // Read the scratchpad
            oneWire.TouchReset();
            oneWire.WriteByte(SkipRomCommand);
            oneWire.WriteByte(ReadScratchpadCommand);

            byte lsb = (byte)oneWire.ReadByte();
            byte msb = (byte)oneWire.ReadByte();

            // Convert raw value to temperature in Celsius
            // DS18B20 returns a 16-bit signed value in 1/16 degree increments
            int raw = (msb << 8) | lsb;

            // Handle negative temperatures (two's complement)
            if ((raw & 0x8000) != 0)
            {
                raw = (int)((uint)raw | 0xFFFF0000);
            }

            return raw / 16.0;
        }

        private static void Debug(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        private static void ConnectToWiFi()
        {
            var connected = WifiNetworkHelper.Reconnect(requiresDateTime: true);
            if (connected)
            {
                var ipAddress = IPGlobalProperties.GetIPAddress().ToString();
                Console.WriteLine($"Connected {ipAddress}");
            }
        }
    }
}
