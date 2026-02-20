# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 1.0.50 / 2026-02-20
### Fixed
- Fixed codegen to not add 'Mutable' prefix to built-in types in generic collections (#66)
- Fixed CI build by removing orphaned System.Formats.Asn1 reference (#45)
- Fixed OctoVersion.Tool runtime mismatch for .NET 10 (#67)

### Changed
- Updated NUnit from 4.3.2 to 4.5.0
- Updated NUnit3TestAdapter from 4.6.0 to 5.2.0
- Updated Roslynator.* from 4.12.10 to 4.15.0
- Updated Snapshooter.Xunit from 0.14.1 to 0.15.0
- Updated Spectre.Console from 0.49.1 to 0.54.0
- Updated FluentAssertions.Analyzers from 0.33.0 to 0.34.1
- Updated DotNet.ReproducibleBuilds from 1.2.25 to 1.2.39
- Updated Microsoft.CodeCoverage and Microsoft.NET.Test.Sdk to 17.14.1
- Updated coverlet.collector from 6.0.3 to 6.0.4
- Updated GitHub Actions: actions/checkout to v6, actions/cache to v5
- Updated GitHub Actions: actions/configure-pages to v5, actions/upload-pages-artifact to v4

## 1.0.* / 2025-04-10
### Added
- Added CHANGELOG
- Initial Release of the package