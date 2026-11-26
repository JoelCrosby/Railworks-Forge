using System.Threading.Tasks;

namespace RailworksForge.Services;

public interface IClipboardService
{
    Task SetText(string text);
}
