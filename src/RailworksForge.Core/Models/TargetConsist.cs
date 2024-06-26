namespace RailworksForge.Core.Models;

public class TargetConsist
{
    private List<Consist> _consists = [];

    public TargetConsist(Consist consist)
    {
        _consists.Add(consist);
    }

    public TargetConsist(IEnumerable<Consist> consists)
    {
        _consists.AddRange(consists);
    }

    public List<Consist> GetConsists()
    {
        return _consists;
    }
}
