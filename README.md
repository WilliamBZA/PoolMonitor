# PoolMonitor

A .NET nanoFramework application for ESP32 that monitors pool water quality and reports readings to Home Assistant via MQTT.

## Sensors

- **DS18B20** — Water temperature (1-Wire, GPIO 16/17)
- **dfRobot ORP Meter** — Oxidation-Reduction Potential (ADC channel 6)

Readings are taken every 5 minutes, averaged over 100 samples (ORP) to reduce noise, and published to Home Assistant using MQTT auto-discovery.

## Hardware

| Component | Connection |
|-----------|-----------|
| ESP32 | Microcontroller |
| DS18B20 | 1-Wire bus on GPIO 16 (RX) / GPIO 17 (TX) |
| dfRobot ORP sensor | ADC channel 6 (GPIO 34) |

## Prerequisites

- Visual Studio with the [nanoFramework extension](https://marketplace.visualstudio.com/items?itemName=nanoframework.nanoFramework-VS2022-Extension)
- An ESP32 flashed with the nanoFramework firmware
- An MQTT broker accessible on your network
- Home Assistant with MQTT integration enabled

## Configuration

Edit the constants in `PoolMonitor/Program.cs` to match your setup:

```csharp
private const int OrpAdcChannel = 6;           // ADC channel for ORP sensor
private const double OrpOffset = 0;             // ORP calibration offset (mV)
private const int ReadIntervalMs = 60000 * 5;   // Reading interval (5 min)

private const string MqttBrokerIp = "192.168.88.172";
private const int MqttBrokerPort = 1883;
private const string MqttUsername = "";
private const string MqttPassword = "";
```

WiFi credentials are configured on the ESP32 device itself via the nanoFramework tooling.

## Build & Deploy

1. Open `PoolMonitor.sln` in Visual Studio
2. Restore NuGet packages (managed via `packages.config`)
3. Build the solution
4. Connect your ESP32 via USB
5. Deploy using the nanoFramework VS extension or the `nanoff` CLI

## Home Assistant

The device registers automatically via MQTT discovery as **Pool Monitor** with two sensors:

- **Water Temperature** (°C)
- **ORP** (mV)
