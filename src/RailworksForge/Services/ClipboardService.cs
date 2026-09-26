using System;
using System.Threading.Tasks;

using Avalonia.Input.Platform;

using RailworksForge.Util;

namespace RailworksForge.Services;

public class ClipboardService
{
    public Task SetText(string text)
    {
        var clipboard = Utils.GetApplicationWindow().Clipboard ?? throw new Exception("unable to get clipboard instance");

        return clipboard.SetTextAsync(text);
    }
}
