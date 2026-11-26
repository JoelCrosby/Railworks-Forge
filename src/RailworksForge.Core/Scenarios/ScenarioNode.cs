using AngleSharp.Dom;

using RailworksForge.Core.Serialization;

namespace RailworksForge.Core.Scenarios;

// A lightweight copy of part of a scenario, holding only element names, ids and text. Consists are listed from these
// rather than from the scenario's XML document, which takes seconds to parse for a large scenario. They are read
// straight from Scenario.bin when a scenario opens, and copied from the document once it has been edited.
public sealed class ScenarioNode
{
    private readonly List<ScenarioNode> _children = [];
    private readonly string _text;

    private ScenarioNode(string name, string? id, string text, ScenarioNode? parent)
    {
        Name = name;
        Id = id;
        _text = text;
        Parent = parent;
        parent?._children.Add(this);
    }

    public string Name { get; }

    // The d:id attribute.
    public string? Id { get; }

    public ScenarioNode? Parent { get; }

    public IReadOnlyList<ScenarioNode> Children => _children;

    public ScenarioNode? FirstChild => _children.Count > 0 ? _children[0] : null;

    public string TextContent => _children.Count == 0 ? _text : string.Concat(_children.Select(child => child.TextContent));

    // Every cConsist in the scenario, in document order.
    public static List<ScenarioNode> ReadConsists(byte[] scenarioBinary, CancellationToken cancellationToken)
    {
        var reader = new SerzReader(scenarioBinary);
        var consists = new List<ScenarioNode>();
        var current = default(ScenarioNode);

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (current is null)
            {
                var opensConsist = reader.Kind is SerzNodeKind.Open && SerzInternal.XmlName(reader.Name) == "cConsist";

                if (opensConsist)
                {
                    current = new ScenarioNode("cConsist", FormatId(reader.Id), string.Empty, null);
                    consists.Add(current);
                }

                continue;
            }

            switch (reader.Kind)
            {
                case SerzNodeKind.Open:
                    var name = SerzInternal.XmlName(reader.Name);
                    current = new ScenarioNode(name, FormatId(reader.Id), string.Empty, current);

                    // Listed like the document's QuerySelectorAll("cConsist"), which includes nested consists.
                    if (name == "cConsist")
                    {
                        consists.Add(current);
                    }

                    break;
                case SerzNodeKind.Close:
                    current = current.Parent;
                    break;
                default:
                    _ = new ScenarioNode(GetLeafName(reader), null, GetLeafText(reader), current);
                    break;
            }
        }

        return consists;
    }

    // Every cConsist in an edited scenario document, in document order.
    public static List<ScenarioNode> ReadConsists(IDocument document)
    {
        return document
            .QuerySelectorAll("cConsist")
            .Select(element => FromElement(element, null))
            .ToList();
    }

    private static ScenarioNode FromElement(IElement element, ScenarioNode? parent)
    {
        var hasChildElements = element.FirstElementChild is not null;
        var text = hasChildElements ? string.Empty : element.TextContent;
        var node = new ScenarioNode(element.NodeName, element.GetAttribute("d:id"), text, parent);

        for (var child = element.FirstElementChild; child is not null; child = child.NextElementSibling)
        {
            FromElement(child, node);
        }

        return node;
    }

    private static string? FormatId(int id)
    {
        return id == 0 ? null : id.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string GetLeafName(SerzReader reader) => reader.Kind switch
    {
        SerzNodeKind.Nil => "d:nil",
        SerzNodeKind.Blob => "d:blob",
        _ => SerzInternal.XmlName(reader.Name),
    };

    private static string GetLeafText(SerzReader reader) => reader.Kind switch
    {
        SerzNodeKind.Value when reader.IsFloat => SerzReader.FormatFloat(reader.FloatValue, "G6"),
        SerzNodeKind.Value or SerzNodeKind.Reference => reader.Value,
        SerzNodeKind.Array => string.Join(" ", reader.Values),
        _ => string.Empty,
    };

    // Supports type selectors joined by descendant combinators, e.g. "Driver ServiceName Key", matched as in CSS:
    // ancestors of a match may lie outside this node.
    public ScenarioNode? QuerySelector(string selector)
    {
        var names = selector.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return Descendants().FirstOrDefault(node => node.Matches(names));
    }

    public IEnumerable<ScenarioNode> QuerySelectorAll(string selector)
    {
        var names = selector.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return Descendants().Where(node => node.Matches(names));
    }

    public string SelectTextContent(string selector)
    {
        return QuerySelector(selector)?.TextContent.Trim().ReplaceLineEndings(string.Empty) ?? string.Empty;
    }

    public string SelectLocalisedStringContent(string selector)
    {
        var localisation = QuerySelector($"{selector} Localisation-cUserLocalisedString");

        return localisation?.Children
            .FirstOrDefault(child => !string.IsNullOrWhiteSpace(child.TextContent))?
            .TextContent
            .Trim()
            .ReplaceLineEndings(string.Empty) ?? string.Empty;
    }

    private IEnumerable<ScenarioNode> Descendants()
    {
        var pending = new Stack<ScenarioNode>();

        PushChildren(pending, this);

        while (pending.TryPop(out var node))
        {
            yield return node;

            PushChildren(pending, node);
        }
    }

    private static void PushChildren(Stack<ScenarioNode> pending, ScenarioNode node)
    {
        for (var i = node._children.Count - 1; i >= 0; i--)
        {
            pending.Push(node._children[i]);
        }
    }

    // For a chain of descendant combinators, matching each name against the nearest ancestor that has it is exact.
    private bool Matches(string[] names)
    {
        if (Name != names[^1])
        {
            return false;
        }

        var ancestor = Parent;

        for (var i = names.Length - 2; i >= 0; i--)
        {
            while (ancestor is not null && ancestor.Name != names[i])
            {
                ancestor = ancestor.Parent;
            }

            if (ancestor is null)
            {
                return false;
            }

            ancestor = ancestor.Parent;
        }

        return true;
    }
}
