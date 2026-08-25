#!/usr/bin/env python3
import argparse
import datetime
import os
import shutil
import subprocess
import sys
import zipfile

def check_last_command(result, step_name):
    if result.returncode != 0:
        print(f"\n[ERROR] {step_name} failed with exit code {result.returncode}. Build aborted.", file=sys.stderr)
        sys.exit(result.returncode)

def check_prerequisites():
    print("[0/4] Checking environment prerequisites...", flush=True)
    
    # 1. Check for Windows SDK (required for XamlCompiler)
    win_sdk_path = r"C:\Program Files (x86)\Windows Kits\10"
    if not os.path.exists(win_sdk_path):
        print("\n[ERROR] Windows 10/11 SDK is missing!", file=sys.stderr)
        print("The WinUI 3 XamlCompiler requires the Windows SDK to satisfy system types and WinRT metadata.", file=sys.stderr)
        print("\n🚀 QUICK INSTALL via winget:", file=sys.stderr)
        print("  winget source update", file=sys.stderr)
        print("  winget install Microsoft.WindowsSDK.10.0.19041", file=sys.stderr)
        print("  winget install Microsoft.VisualStudio.Community --override \"--add Microsoft.VisualStudio.Workload.NetDesktop --add Microsoft.VisualStudio.Workload.Azure --add Microsoft.VisualStudio.Component.Windows10SDK.19041 --add Microsoft.VisualStudio.Component.VC.UniversalWindowsPlatform --passive --norestart\"", file=sys.stderr)
        sys.exit(1)

    print("Environment: OK", flush=True)

def main():
    parser = argparse.ArgumentParser(description="Home Assistant Windows Companion - Local Build Script")
    parser.add_argument("--version", default="", help="Version string to use for the build.")
    args = parser.parse_args()

    project_root = os.path.dirname(os.path.abspath(__file__))
    project_dir = os.path.join(project_root, "src", "HAWindowsCompanion.App")
    publish_dir = os.path.join(project_root, "publish_local")
    solution_file = os.path.join(project_root, "HAWindowsCompanion.sln")

    version = args.version
    if not version:
        date_suffix = datetime.datetime.now().strftime("%Y%m%d")
        version = f"2026.3.0.dev{date_suffix}"

    zip_name = f"HAWindowsCompanion_v{version}.zip"
    zip_path = os.path.join(project_root, zip_name)

    print(f"\n--- Starting Build Process for Home Assistant Windows Companion ---")
    print(f"Target Version: {version}")
    print(f"Developer: FaserF")

    check_prerequisites()

    # 2. Clean
    print(f"\n[1/4] Cleaning previous builds...")
    if os.path.exists(publish_dir):
        shutil.rmtree(publish_dir)
    
    clean_res = subprocess.run(["dotnet", "clean", solution_file, "-c", "Release", "-p:Platform=x64"])
    check_last_command(clean_res, "Clean")

    # 3. Restore
    print(f"\n[2/4] Restoring dependencies...")
    restore_res = subprocess.run(["dotnet", "restore", solution_file, "-p:Platform=x64"])
    check_last_command(restore_res, "Restore")

    # 4. Publish
    print(f"\n[3/4] Publishing single-file executable...")
    
    # Parse numeric part for AssemblyVersion (2026.3.0.0)
    numeric_version = re.sub(r'\.dev.*$', '', version)
    numeric_version = re.sub(r'b.*$', '', numeric_version)
    if re.match(r'^\d+\.\d+\.\d+$', numeric_version):
        numeric_version += ".0"

    print(f"Assembly Version: {numeric_version}")
    print(f"Informational Version: {version}")

    publish_cmd = [
        "dotnet", "publish", project_dir,
        "-c", "Release",
        "-r", "win-x64",
        "-p:Platform=x64",
        "--output", publish_dir,
        "-p:PublishSingleFile=false",
        "-p:SelfContained=true",
        "-p:WindowsPackageType=None",
        "-p:WindowsAppSDKSelfContained=true",
        f"-p:AssemblyVersion={numeric_version}",
        f"-p:FileVersion={numeric_version}",
        f"-p:InformationalVersion={version}",
        f"-p:Version={numeric_version}"
    ]
    publish_res = subprocess.run(publish_cmd)
    check_last_command(publish_res, "Publish")

    # 5. ZIP
    print(f"\n[4/4] Packaging into {zip_name}...")
    if os.path.exists(zip_path):
        os.remove(zip_path)

    with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zip_file:
        for root, dirs, files in os.walk(publish_dir):
            for file in files:
                file_path = os.path.join(root, file)
                arc_name = os.path.relpath(file_path, publish_dir)
                zip_file.write(file_path, arc_name)

    print(f"\n--- BUILD SUCCESSFUL ---")
    print(f"Target: {version}")
    print(f"Location: {zip_path}")
    print(f"Published files (uncompressed): {publish_dir}")

if __name__ == "__main__":
    main()
