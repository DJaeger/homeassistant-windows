# Development Guide

Thank you for your interest in contributing to the Home Assistant Windows Companion! This project is built on modern .NET technologies and follows industry best practices.

## 🛠️ Prerequisites

- **Windows 10/11**: Developing WinUI 3 apps requires a modern Windows environment.
- **Visual Studio 2026**: With the "Windows app development" workload installed.
- **.NET 10 SDK**: The latest cutting-edge runtime.
- **Git**: For version control.

## 🏗️ Project Architecture

The solution uses **Clean Architecture** to maintain a strict separation of concerns:

1. **`HAWindowsCompanion.Core`**:
   - Contains all business logic, domain models, and interfaces.
   - Zero dependencies on UI or platform-specific libraries.
2. **`HAWindowsCompanion.Infrastructure`**:
   - Implements data persistence, HA API communication (REST/WS), and Windows API integrations (Win32, WMI).
3. **`HAWindowsCompanion.App`**:
   - The WinUI 3 presentation layer.
   - Follows the **MVVM** pattern using `CommunityToolkit.Mvvm`.
4. **`HAWindowsCompanion.Tests`**:
   - Comprehensive unit and integration tests using `xUnit` and `Moq`.

## 🚀 Getting Started

1. Clone the repository: `git clone https://github.com/fabian-be/homeassistant-windows.git`
2. Open `HAWindowsCompanion.sln` in Visual Studio.
3. Restore NuGet packages and build the solution.
4. Run the `App` project.

## 📦 Building for Release

To generate a self-contained, optimized single-file executable:

```powershell
dotnet publish src/HAWindowsCompanion.App/HAWindowsCompanion.App.csproj `
  -c Release `
  -r win-x64 `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -p:PublishReadyToRun=true
```

## 🧪 Testing

Always run tests before submitting a Pull Request:
```powershell
dotnet test HAWindowsCompanion.sln
```
