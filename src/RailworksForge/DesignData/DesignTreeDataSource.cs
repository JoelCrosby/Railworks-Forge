using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

using RailworksForge.Core.Models;

namespace RailworksForge.DesignData;

public static partial class DesignData
{
    public static ITreeDataGridSource ServicesSource = new FlatTreeDataGridSource<Consist>(Scenario.Consists)
    {
        Columns =
        {
            new TextColumn<Consist, AcquisitionState>("Locomotive State", c => c.AcquisitionState),
            new TextColumn<Consist, AcquisitionState>("Consist State", c => c.ConsistAcquisitionState),
            new TextColumn<Consist, string>("Locomotive Author", c => c.LocoAuthor),
            new TextColumn<Consist, bool>("Is Player Driver", c => c.PlayerDriver),
            new TextColumn<Consist, string>("Locomotive Name", c => c.LocomotiveName),
            new TextColumn<Consist, LocoClass?>("Locomotive Class", c => c.LocoClass),
            new TextColumn<Consist, string>("Service Name", c => c.ServiceName),
        },
    };
}
