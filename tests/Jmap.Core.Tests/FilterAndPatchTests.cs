using System.Text.Json;
using Jmap.Json;

namespace Jmap.Core.Tests;

public class FilterAndPatchTests
{
    private sealed record TestCondition : JmapFilter
    {
        public string? Subject { get; init; }
        public bool? HasAttachment { get; init; }
    }

    [Fact]
    public void Filter_operators_nest_and_serialise_runtime_condition_types()
    {
        var filter = JmapFilter.And(
            new TestCondition { Subject = "raven" },
            JmapFilter.Not(new TestCondition { HasAttachment = true }));

        var json = JsonSerializer.Serialize<JmapFilter>(filter, JmapJson.Options);

        Assert.Equal(
            """{"operator":"AND","conditions":[{"subject":"raven"},{"operator":"NOT","conditions":[{"hasAttachment":true}]}]}""",
            json);
    }

    [Fact]
    public void Comparators_serialise_camel_cased()
    {
        var json = JsonSerializer.Serialize(new Comparator("receivedAt", IsAscending: false), JmapJson.Options);

        Assert.Equal("""{"property":"receivedAt","isAscending":false}""", json);
    }

    [Fact]
    public void Patch_objects_keep_pointer_paths_verbatim()
    {
        var patch = PatchObject
            .Set("keywords/$seen", true)
            .With("mailboxIds/MB2", true)
            .Without("mailboxIds/MB1");

        var json = JsonSerializer.Serialize(patch, JmapJson.Options);

        Assert.Equal("""{"keywords/$seen":true,"mailboxIds/MB2":true,"mailboxIds/MB1":null}""", json);
    }

    [Fact]
    public void Utc_dates_always_write_a_zulu_suffix()
    {
        var wrapped = new UtcHolder(new DateTimeOffset(2026, 7, 3, 15, 30, 0, TimeSpan.FromHours(2)));

        var json = JsonSerializer.Serialize(wrapped, JmapJson.Options);

        Assert.Equal("""{"at":"2026-07-03T13:30:00Z"}""", json);
        Assert.Equal(wrapped.At, JsonSerializer.Deserialize<UtcHolder>(json, JmapJson.Options)!.At);
    }

    private sealed record UtcHolder([property: System.Text.Json.Serialization.JsonConverter(typeof(UtcDateConverter))] DateTimeOffset At);
}
