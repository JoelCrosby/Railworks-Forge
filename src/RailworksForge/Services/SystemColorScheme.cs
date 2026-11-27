using System;
using System.Threading.Tasks;

using Avalonia.Styling;

using Serilog;

using Tmds.DBus.Protocol;

namespace RailworksForge.Services;

// Avalonia reads the desktop's colour scheme from the freedesktop portal asynchronously and reports light until the
// reply arrives, so a dark desktop briefly shows a light window. Reading it here, before the window opens, avoids that.
public static class SystemColorScheme
{
    private const uint PrefersDark = 1;
    private const uint PrefersLight = 2;

    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(300);

    public static ThemeVariant? ReadFreedesktopPreference()
    {
        var sessionAddress = DBusAddress.Session;

        if (!OperatingSystem.IsLinux() || sessionAddress is null)
        {
            return null;
        }

        try
        {
            var read = Task.Run(() => ReadColorScheme(sessionAddress));
            var hasReply = read.Wait(Timeout);

            return hasReply ? ToThemeVariant(read.Result) : null;
        }
        catch (Exception e)
        {
            Log.Debug(e, "Could not read the desktop colour scheme");

            return null;
        }
    }

    private static async Task<uint> ReadColorScheme(string sessionAddress)
    {
        using var connection = new DBusConnection(sessionAddress);

        await connection.ConnectAsync();

        var message = CreateReadMessage(connection);

        return await connection.CallMethodAsync(message, (reply, _) => ReadUInt32(reply));
    }

    private static MessageBuffer CreateReadMessage(DBusConnection connection)
    {
        using var writer = connection.GetMessageWriter();

        writer.WriteMethodCallHeader(
            "org.freedesktop.portal.Desktop",
            "/org/freedesktop/portal/desktop",
            "org.freedesktop.portal.Settings",
            "Read",
            "ss");
        writer.WriteString("org.freedesktop.appearance");
        writer.WriteString("color-scheme");

        return writer.CreateMessage();
    }

    // Older portals wrap the value in a second variant.
    private static uint ReadUInt32(Message reply)
    {
        var reader = reply.GetBodyReader();
        var value = reader.ReadVariantValue();

        while (value.Type == VariantValueType.Variant)
        {
            value = value.GetVariantValue();
        }

        return value.GetUInt32();
    }

    private static ThemeVariant? ToThemeVariant(uint colorScheme)
    {
        return colorScheme switch
        {
            PrefersDark => ThemeVariant.Dark,
            PrefersLight => ThemeVariant.Light,
            _ => null,
        };
    }
}
