# Phase 10 - Quality and release readiness

Date: 2026-08-29

## Automated quality gates

| Gate | Result | Evidence |
|---|---|---|
| Exact decimal finance calculations | Pass | Fractional-value and linked-transfer tests |
| Empty, small, and large datasets | Pass | Empty-range tests plus 10,000-row report test |
| Database upgrade compatibility | Pass | Fresh and legacy `EnsureCreated` migration tests through all three migrations |
| Budget/report boundary cases | Pass | Income-only, expense-only, deleted category, month/year boundary, rollover, and threshold tests |
| Light/Dark/HighContrast semantic resources | Pass | Automated resource-key parity test |
| Icon-only button accessible names | Pass | Automated XAML accessibility gate |
| Reduced motion | Pass | Charts use Windows `AnimationsEnabled` and disable animation when requested |
| Responsive data actions | Pass | Settings data actions use a wrapping layout for narrow/high-scale windows |
| Large-data release budget | Pass | 10,000-row SQLite report test completed in 3 seconds; gate is 8 seconds |
| x64 Release MSIX | Pass | Manifest inspected: x64, version 1.0.0.0, 625 archive entries |
| ARM64 Release MSIX | Pass | Manifest inspected: arm64, version 1.0.0.0, 621 archive entries |

The full automated suite contains 27 tests.

## Package outputs

### x64

- Path: `AppPackages/Phase10/x64/BudgetBook_1.0.0.0_x64_Test/BudgetBook_1.0.0.0_x64.msix`
- Size: 129,404,992 bytes
- SHA-256: `842937AE5FCBAEB5768CCF92809FDE2802AC26C0D91B274FD1E6D5AE74D90547`

### ARM64

- Path: `AppPackages/Phase10/arm64/BudgetBook_1.0.0.0_arm64_Test/BudgetBook_1.0.0.0_arm64.msix`
- Size: 124,300,631 bytes
- SHA-256: `9002AD55E5D8791DCE80807AE32E376B983A9D59FA0F3D1B710B8E278CB317A2`

Both packages are intentionally unsigned validation artifacts. The missing optional
`mspdbcmf.exe` produced one warning per package, so `.msixsym` symbol packages were not
generated; it did not affect either MSIX.

## External release-operator checks still required

These checks cannot be completed on the current machine and must be performed before a
Store submission:

1. Associate the project with the reserved Microsoft Store identity. The current manifest
   still contains `Publisher="CN=AppPublisher"`.
2. Build a signed Store-upload package using the Partner Center identity and certificate.
3. Install the Windows App Certification Kit and run WACK. `appcert.exe` is not installed
   in either standard Windows Kits location on this machine.
4. Install and launch the ARM64 package on physical ARM64 Windows hardware.
5. Perform the final human assistive-technology pass with Narrator, keyboard-only input,
   Windows High Contrast, and 125%, 150%, 175%, and 200% display scaling. Automated gates
   cover resources and accessible names but cannot substitute for a human screen-reader
   and physical-display review.

## Release command examples

```powershell
dotnet test .\BudgetBook.Tests\BudgetBook.Tests.csproj -p:RuntimeIdentifier=win-x64

dotnet build .\BudgetBook.csproj -c Release -p:Platform=x64 `
  -p:GenerateAppxPackageOnBuild=true -p:UapAppxPackageBuildMode=StoreUpload

dotnet build .\BudgetBook.csproj -c Release -p:Platform=ARM64 `
  -p:RuntimeIdentifier=win-arm64 -p:GenerateAppxPackageOnBuild=true `
  -p:UapAppxPackageBuildMode=StoreUpload
```
