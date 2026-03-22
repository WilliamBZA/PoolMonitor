# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PoolMonitor is a .NET nanoFramework application targeting an ESP32 microcontroller. It monitors pool water quality using a DS18B20 temperature sensor and a dfRobot ORP (Oxidation-Reduction Potential) meter, then reports readings to Home Assistant via MQTT using the `nf.HomeAssistant.MqttDiscovery` package.

## Technology Stack

- **.NET nanoFramework** — embedded .NET runtime for ESP32
- **Hardware**: ESP32, DS18B20 (temperature), dfRobot ORP meter
- **Communication**: MQTT for Home Assistant integration
- **Key package**: `nf.HomeAssistant.MqttDiscovery` for HA auto-discovery

## Build & Deploy

- Open `PoolMonitor.sln` in Visual Studio with the nanoFramework extension installed
- Build with Visual Studio or `msbuild PoolMonitor.sln`
- Deploy to ESP32 via the nanoFramework VS extension or `nanoff` CLI
- NuGet packages are managed via `packages.config` (nanoFramework convention)
- Project files use `.nfproj` extension instead of `.csproj`

## Architecture

- `PoolMonitor/Program.cs` — main entry point; reads sensors in a loop and outputs values
- ORP sensor connects to an ESP32 ADC pin; readings are averaged over multiple samples to reduce noise
- dfRobot ORP conversion formula: `ORP = ((2500 - sensorMv) / 1.037) + offset`

## nanoFramework Constraints

- Limited subset of .NET APIs — no `Task`, no `async/await`, no LINQ
- Use `Thread.Sleep` for delays and `System.Diagnostics.Debug.WriteLine` for output
- String interpolation is supported via `nanoFramework.System.Text`
