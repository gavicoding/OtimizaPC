# Security Policy

## Reporting a Vulnerability

OtimizaPC performs privileged file cleanup, service inspection, and Windows Registry tweaks when elevated. If you discover a security flaw (such as unsafe elevation, symlink/junction traversal during cleanup, or unquoted service path issues), please **do not** disclose it in public issues.

Report privately via **GitHub Security Advisories** or email `gavicoding@gmail.com` with:

- Affected service or module (e.g. `CleanupService`, `ResidueService`, `ElevationHelper`)
- Reproduction steps and impact assessment

Reports are triaged promptly so fixes can be released safely.
