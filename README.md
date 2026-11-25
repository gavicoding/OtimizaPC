**🇺🇸 English** | [🇧🇷 Português](README.pt-BR.md)

# ⚡ OtimizaPC

[![Windows Build](https://github.com/gavicoding/OtimizaPC/actions/workflows/build.yml/badge.svg)](https://github.com/gavicoding/OtimizaPC/actions/workflows/build.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF%20(MVVM)-0078D4.svg)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![C#](https://img.shields.io/badge/Language-C%23-239120.svg)](https://learn.microsoft.com/dotnet/csharp/)
[![License: Apache-2.0](https://img.shields.io/badge/License-Apache_2.0-green.svg)](LICENSE)

A native, dark-mode **Windows desktop system optimizer and disk utility** built with **.NET 8 & WPF (MVVM)**. Designed around strict safety guardrails: nothing is ever deleted without explicit user review, every background scan runs asynchronously off the UI thread, and System Restore Points can be created in one click before applying system tweaks.

---

## In plain words

Most "PC optimizers" delete things without saying what, change hidden settings and sometimes break Windows.

OtimizaPC works the other way around: **nothing is deleted before you see it**, and you can create a restore point with one click before any change.

**What you gain**

- **Disk space back without fear:** every cleanup shows the list and exact size of what will go.
- **An easy way back:** Windows restore point in one click.
- **More than cleanup:** real duplicates by content, leftovers from uninstalled programs and a map of what takes up space.

---

## Core Modules

### 📊 Real-Time Health Dashboard & Restore Points
- Live CPU load, RAM usage, drive capacity overview, and **S.M.A.R.T. disk health status** (when running elevated).
- One-click **Windows System Restore Point** creation before making any system-level modifications.

### 🧹 Safe Multi-Profile Junk Cleanup
Scans and previews exact byte sizes before deletion across:
- User & Windows temporary files, thumbnail caches, and crash/error reports
- Browser caches (Chrome, Edge, Firefox) - **never touches passwords, history, or bookmarks**
- Prefetch, Windows Update cache, system logs, and legacy Windows upgrade leftovers
- Recycle Bin across all drives
- **Temporary files across other Windows user profiles** on the same workstation (multi-user awareness)

### 🔍 Content-Hash Duplicate Finder
Finds byte-identical files via cryptographic content hashing (not just filenames) in a target directory or across the entire machine, keeping the oldest file selected by default and letting you review before deleting.

### 🗑️ 90-Day Orphan Software Residue Scanner
Cross-references installation directories (`Program Files`, `AppData`, `ProgramData`, including other local user profiles) against Windows' installed applications registry. Applies a **90-day inactivity safety threshold** so active developer toolchains (`npm` caches, IDEs, SDKs) are never flagged as leftovers.

### 🚀 Performance Tuning
- Power plan switcher (Balanced / High Performance / Power Saver)
- Visual effects toggle (Best Appearance vs. Best Performance)
- Startup programs manager (enable/disable in one click)
- Curated background services panel with plain-language safety descriptions
- Advanced tweaks: foreground application priority, hardware-accelerated GPU scheduling, hibernation toggle

### 💾 Instant Disk Space Analyzer
Scans an entire drive once in the background (with live progress and cancellation), caching the tree so drilling into subfolders is **instantaneous** (WinDirStat-style) for locating and cleaning large space consumers.

---

## Architecture & Design Principles

- **Zero External Infrastructure**: Built purely on native Windows APIs (Registry, WMI, `powercfg`, Service Control Manager).
- **Clean MVVM Separation**: `Views` (XAML) ↔ `ViewModels` ([CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)) ↔ `Services` (`CleanupService`, `DuplicateFinderService`, `DiskSpaceScanService`, `ResidueService`, etc.).
- **Smart UAC Elevation**: Automatically detects operations requiring administrator privileges (`Windows\Temp`, Prefetch, services, cross-profile cleanup) and offers one-click elevated restart.
- **Responsive UX**: All heavy I/O runs asynchronously with native Windows toast notifications on completion and animated sidebar indicators for active background tasks.

---

## Build & Publish

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows

### Run from Source

```bash
dotnet build OtimizaPC.sln
dotnet run --project src/OtimizaPC
```

### Publish Self-Contained Single-File Executable

```bash
dotnet publish src/OtimizaPC -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

---

## Disclaimer

This utility modifies system files, Windows Registry settings, and Windows services. Although it always presents a detailed preview before deleting or changing anything, use it at your own risk and maintain backups of critical data. Distributed "AS IS", without warranty of any kind. See [LICENSE](LICENSE) for full terms.
