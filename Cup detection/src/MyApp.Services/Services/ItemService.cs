using System.Collections.Generic;
using System.Management;
using MyApp.Common.Models;
using DirectShowLib;

namespace MyApp.Services.Services;

public class ItemService : IItemService
{
    public IEnumerable<ItemModel> GetItems()
    {
        var list = new List<ItemModel>();
        var index = 0;

        // First try DirectShow enumeration for video input devices (more reliable for webcams)
        try
        {
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            foreach (var d in devices)
            {
                var name = d.Name ?? d.DevicePath ?? "Video Device";
                var mon = d.DevicePath ?? string.Empty;
                var lmon = mon.ToLowerInvariant();
                string transport = "Unknown";
                if (lmon.Contains("usb") || lmon.Contains("vid_") || lmon.Contains("ven_")) transport = "USB";
                else if (lmon.Contains("gige") || lmon.Contains("ethernet") || lmon.Contains("rj45") || lmon.Contains("tcpip")) transport = "Ethernet";
                else transport = "Integrated"; // assume integrated if not explicit

                list.Add(new ItemModel { Id = ++index, Name = name, Transport = transport, Manufacturer = null, PnpDeviceId = mon });
            }

            // If DirectShow found any video input devices, return them only (show only webcams)
            if (list.Count > 0)
                return list;
        }
        catch
        {
            // ignore and fallback to WMI below
        }
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");
            foreach (ManagementObject device in searcher.Get())
            {
                var nameObj = device["Name"];
                if (nameObj == null) continue;
                var name = nameObj.ToString();
                var lname = name.ToLowerInvariant();

                var pnp = device["PNPDeviceID"]?.ToString() ?? string.Empty;
                var manuf = device["Manufacturer"]?.ToString() ?? string.Empty;
                var pnpClass = device["PNPClass"]?.ToString() ?? string.Empty;
                var lclass = pnpClass.ToLowerInvariant();

                // Keywords in multiple languages (English/Portuguese) and variations
                var keywords = new[] { "camera", "câmera", "camerá", "webcam", "video", "vídeo", "imagem", "image", "integrated camera", "integrated", "built-in", "internal", "camera front", "rear camera" };

                bool looksLikeCamera = false;
                foreach (var kw in keywords)
                {
                    if (lname.Contains(kw)) { looksLikeCamera = true; break; }
                }

                if (!looksLikeCamera)
                {
                    // check PNP class for imaging devices
                    if (lclass == "image" || lclass == "camera" || lclass == "imaging")
                        looksLikeCamera = true;
                }

                // also check service/hardware id for common camera drivers
                var service = device["Service"]?.ToString() ?? string.Empty;
                var lservice = service.ToLowerInvariant();
                var deviceIdLower = pnp.ToLowerInvariant();
                if (!looksLikeCamera && (lservice.Contains("usbvideo") || deviceIdLower.Contains("usb\\") || deviceIdLower.Contains("video")))
                    looksLikeCamera = true;

                if (!looksLikeCamera)
                    continue;

                // Determine transport type using heuristics
                string transport;
                if (deviceIdLower.Contains("usb") || deviceIdLower.Contains("vid_") || deviceIdLower.Contains("ven_"))
                    transport = "USB";
                else if (lname.Contains("gige") || lname.Contains("gigabit") || lname.Contains("ethernet") || lname.Contains("rj45") || lname.Contains("ip camera") || lname.Contains("network") || manuf.ToLowerInvariant().Contains("gige") || deviceIdLower.Contains("tcpip") || deviceIdLower.Contains("net"))
                    transport = "Ethernet";
                else if (deviceIdLower.Contains("acpi") || lname.Contains("integrated") || lname.Contains("built-in") || lname.Contains("internal") || lclass == "image")
                    transport = "Integrated";
                else
                    transport = "Unknown";

                // only add if not already present (avoid duplicates from DirectShow)
                // try to merge with existing by name
                var existing = list.Find(i => string.Equals(i.Name, name, System.StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    if (string.IsNullOrWhiteSpace(existing.Transport) || existing.Transport == "Unknown") existing.Transport = transport;
                    if (string.IsNullOrWhiteSpace(existing.Manufacturer) && !string.IsNullOrWhiteSpace(manuf)) existing.Manufacturer = manuf;
                    if (string.IsNullOrWhiteSpace(existing.PnpDeviceId) && !string.IsNullOrWhiteSpace(pnp)) existing.PnpDeviceId = pnp;
                }
                else
                {
                    list.Add(new ItemModel { Id = ++index, Name = name, Transport = transport, Manufacturer = manuf, PnpDeviceId = pnp });
                }
            }
        }
        catch
        {
            // If WMI fails, fall back to an empty list
        }

        // If none found even after WMI, add a placeholder
        if (list.Count == 0)
        {
            list.Add(new ItemModel { Id = 1, Name = "(no cameras found)" });
        }

        return list;
    }
}
