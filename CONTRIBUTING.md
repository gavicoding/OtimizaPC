# Contributing to OtimizaPC

Thank you for contributing to **OtimizaPC**!

## Development Setup

- **OS:** Windows 10 / 11
- **SDK:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Architecture:** WPF + MVVM (`Views` ↔ `ViewModels` ↔ `Services`) using `CommunityToolkit.Mvvm`

```bash
git clone https://github.com/gavicoding/OtimizaPC.git
cd OtimizaPC
dotnet restore OtimizaPC.sln
dotnet build OtimizaPC.sln -c Release
```

## Guidelines

1. Branch from `main` using descriptive branch names (`feat/...`, `fix/...`, `docs/...`).
2. **Safety first:** Never implement silent or unconfirmed deletions. Every cleanup, duplicate removal, or residue action must present exact paths/byte totals for explicit user approval.
3. Preserve standard Windows non-admin fallback behavior and surface elevation prompts cleanly via `ElevationHelper`.
4. Ensure `dotnet build OtimizaPC.sln -c Release` passes with zero errors before submitting a Pull Request.
