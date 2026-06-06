# CCDASTRO Temma Mount Driver

## Overview
The CCDASTRO Temma Mount Driver is a modern ASCOM telescope driver for Takahashi Temma equatorial mounts.

Designed as a complete C# replacement for earlier VB6-based Temma drivers, it provides reliable mount control, GOTO slewing, tracking control, synchronization, parking support, and observatory automation compatibility while maintaining full support for the native Takahashi Temma serial protocol.

The driver utilizes a modern Local COM Server architecture which provides improved stability, multi-client support, automatic resource management, and enhanced reliability for unattended observatory operation.

## Key Features
- ASCOM Telescope Interface
- Native Temma Protocol Support
- Multiple Mount Model Support
- Tracking Control
- GOTO Slewing
- Synchronization
- Pulse Guiding
- Parking Support
- Persistent Settings
- Multi-Client Local COM Server Architecture
- Diagnostic Logging

## Supported Mount Models
- EM-11
- EM-11 Temma 2M
- EM-200
- EM-200 Temma 2M
- NJP
- EM-500

## Observatory Reliability Features
- Local COM Server Hosting
- Multi-Client Support
- Automatic Resource Management
- Automatic Cleanup and Shutdown
- Diagnostic Logging

## Requirements
Software:
- Windows
- ASCOM Platform 7 or later

Hardware:
- Takahashi Temma Mount
- Serial or USB-to-Serial Adapter

## Author
Chuck Faranda
CCD Astro Observatory Automation
https://ccdastro.net
