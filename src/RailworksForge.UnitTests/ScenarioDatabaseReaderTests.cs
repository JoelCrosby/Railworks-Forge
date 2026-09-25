using System.Buffers.Binary;
using System.Text;

using RailworksForge.Core.External;

namespace RailworksForge.UnitTests;

public class ScenarioDatabaseReaderTests
{
    [Fact]
    public void Read_ExtractsPlayerInfoForEachScenario()
    {
        var data = new SerzBuilder()
            .Open("cScenarioDatabase")
            .Open("RouteID").Open("cGUID").String("DevString", "route-id").Close("cGUID").Close("RouteID")
            .Scenario("21CB2A69-1CEA-49A2-8199-32D1B827C455", 120, "CompletedSuccessfully", 3)
            .Scenario("scenario-two", 0, "NotCompleted", 0)
            .Close("cScenarioDatabase")
            .Build();

        var scenarios = ScenarioDatabaseReader.Read(data);

        Assert.Equal(2, scenarios.Count);

        var completed = scenarios["21cb2a69-1cea-49a2-8199-32d1b827c455"];

        Assert.Equal("21CB2A69-1CEA-49A2-8199-32D1B827C455", completed.ScenarioId);
        Assert.Equal(120, completed.Score);
        Assert.Equal("CompletedSuccessfully", completed.Completion);
        Assert.Equal(3, completed.MedalsAwarded);
        Assert.Equal("NotCompleted", scenarios["scenario-two"].Completion);
    }

    [Fact]
    public void Read_IgnoresRouteIdAndKeepsFirstDuplicate()
    {
        var data = new SerzBuilder()
            .Open("cScenarioDatabase")
            .Open("sSDScenario")
            .Open("RouteID").Open("cGUID").String("DevString", "route-id").Close("cGUID").Close("RouteID")
            .Open("ScenarioID").Open("cGUID").String("DevString", "scenario-id").Close("cGUID").Close("ScenarioID")
            .String("Completion", "CompletedSuccessfully")
            .Close("sSDScenario")
            .Scenario("scenario-id", 0, "NotCompleted", 0)
            .Close("cScenarioDatabase")
            .Build();

        var scenarios = ScenarioDatabaseReader.Read(data);
        var scenario = Assert.Single(scenarios.Values);

        Assert.Equal("scenario-id", scenario.ScenarioId);
        Assert.Equal("CompletedSuccessfully", scenario.Completion);
    }

    [Fact]
    public void Read_ToleratesNullCharactersInText()
    {
        var data = new SerzBuilder()
            .Open("cScenarioDatabase")
            .Open("sSDScenario")
            .Open("ScenarioID").Open("cGUID").String("DevString", "scenario-id").Close("cGUID").Close("ScenarioID")
            .String("ScenarioDesc", "broken\0description")
            .String("Completion", "CompletedSuccessfully")
            .Close("sSDScenario")
            .Close("cScenarioDatabase")
            .Build();

        var scenarios = ScenarioDatabaseReader.Read(data);

        Assert.Equal("CompletedSuccessfully", scenarios["scenario-id"].Completion);
    }

    [Fact]
    public void Read_RejectsTruncatedInput()
    {
        var data = new SerzBuilder()
            .Open("cScenarioDatabase")
            .Scenario("scenario-id", 0, "NotCompleted", 0)
            .Close("cScenarioDatabase")
            .Build();

        var truncated = data[..^3];

        Assert.Throws<InvalidDataException>(() => ScenarioDatabaseReader.Read(truncated));
    }

    private sealed class SerzBuilder
    {
        private readonly List<byte> _bytes = [.. "SERZ\0\0\x01\0"u8];

        public SerzBuilder Scenario(string id, uint score, string completion, byte medals)
        {
            return Open("sSDScenario")
                .Open("ScenarioID").Open("cGUID").String("DevString", id).Close("cGUID").Close("ScenarioID")
                .UInt32("Score", score)
                .String("Completion", completion)
                .Open("QuickDriveInfo").UInt32("Score", 999).Close("QuickDriveInfo")
                .UInt8("MedalsAwarded", medals)
                .Close("sSDScenario");
        }

        public SerzBuilder Open(string name)
        {
            Chunk('P', name);
            Int32(0);
            Int32(0);

            return this;
        }

        public SerzBuilder Close(string name)
        {
            Chunk('p', name);

            return this;
        }

        public SerzBuilder String(string name, string value)
        {
            Chunk('V', name, "cDeltaString");
            Text(value);

            return this;
        }

        public SerzBuilder UInt32(string name, uint value)
        {
            Chunk('V', name, "sUInt32");
            Int32((int)value);

            return this;
        }

        public SerzBuilder UInt8(string name, byte value)
        {
            Chunk('V', name, "sUInt8");
            _bytes.Add(value);

            return this;
        }

        public byte[] Build()
        {
            return [.. _bytes];
        }

        private void Chunk(char kind, string name, string? type = null)
        {
            _bytes.Add(byte.MaxValue);
            _bytes.Add((byte)kind);
            Text(name);

            if (type is not null)
            {
                Text(type);
            }
        }

        private void Text(string value)
        {
            _bytes.AddRange([byte.MaxValue, byte.MaxValue]);
            Int32(value.Length);
            _bytes.AddRange(Encoding.UTF8.GetBytes(value));
        }

        private void Int32(int value)
        {
            var buffer = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            _bytes.AddRange(buffer);
        }
    }
}
