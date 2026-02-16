using System.Collections.Generic;

namespace MyApp.Common.Models;

public class ItemModel
{
    public int Id { get; set; }
    public string? Name { get; set; }

    // Transport type: USB, Ethernet/GigE, Integrated, etc.
    public string? Transport { get; set; }

    // Optional manufacturer and PNP id for diagnostics
    public string? Manufacturer { get; set; }
    public string? PnpDeviceId { get; set; }

    // Friendly display used by the UI
    public string DisplayName
    {
        get
        {
            var baseName = string.IsNullOrWhiteSpace(Transport) ? (Name ?? string.Empty) : $"{Name} ({Transport})";
            var parts = new List<string> { baseName };
            if (!string.IsNullOrWhiteSpace(Manufacturer)) parts.Add(Manufacturer!);
            if (!string.IsNullOrWhiteSpace(PnpDeviceId)) parts.Add(PnpDeviceId!);
            return string.Join(" - ", parts);
        }
    }
}
