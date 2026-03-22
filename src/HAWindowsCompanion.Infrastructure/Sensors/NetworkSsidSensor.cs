using System;
using System.Linq;
using System.Net.NetworkInformation;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Sensors;

public class NetworkSsidSensor : ISensorProvider
{
    public string UniqueId => "network_ssid";
    public string Name => "Network SSID";
    public bool IsEnabled { get; set; } = true;

    public SensorRegistration GetRegistration()
    {
        return new SensorRegistration
        {
            UniqueId = UniqueId,
            Name = Name,
            Type = "sensor",
            Icon = "mdi:wifi"
        };
    }

    public SensorUpdate GetCurrentState()
    {
        string ssid = "Unknown";
        try
        {
            // Simple approach for Windows: using netsh to get the SSID
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var line = output.Split('\n').FirstOrDefault(l => l.Contains(" SSID") && !l.Contains("BSSID"));
            if (line != null)
            {
                ssid = line.Split(':').Last().Trim();
            }
        }
        catch { }

        return new SensorUpdate
        {
            UniqueId = UniqueId,
            State = ssid,
            Attributes = new System.Collections.Generic.Dictionary<string, object>
            {
                { "interface_name", NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(i => i.OperationalStatus == OperationalStatus.Up)?.Name ?? "unknown" }
            }
        };
    }
}
