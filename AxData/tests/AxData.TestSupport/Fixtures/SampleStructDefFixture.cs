using AxData.Application.Ports.Axpert;
using AxData.Domain.Lifecycle;
using AxData.Domain.Model;

namespace AxData.TestSupport.Fixtures;

public static class SampleStructDefFixture
{
    public const string TenantId = "sample-tenant";
    public const string TransId = "sample-order";
    public const string Version = "1";

    public static StructDefHandle Definition { get; } = new(
        TenantId,
        TransId,
        Version,
        [
            new DataContainerDefinition(
                "dc1",
                false,
                [new FieldDefinition("customer")]),
            new DataContainerDefinition(
                "dc2",
                true,
                [
                    new FieldDefinition("item"),
                    new FieldDefinition("qty"),
                    new FieldDefinition("rate"),
                    new FieldDefinition("amount")
                ],
                ["item", "rate", "qty", "amount"])
        ],
        [new FieldDependency("dc2", "qty", "dc2", "amount", "qty-times-rate")],
        [new FillGridDefinition("fill-lines", "dc2", ["customer"], FillGridMode.Replace)]);

    public static AxDataState CreateState()
    {
        var state = new AxDataState(
            "object-1",
            TenantId,
            TransId,
            "owner",
            "owner",
            Version,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        state.DataContainers.Add(
            "dc1",
            new AxDataContainer(
                "dc1",
                false,
                [new AxDataRow("row_header", AxRowState.Unchanged, 1, new Dictionary<string, AxFieldValue>
                {
                    ["customer"] = new("ACME")
                })]));
        state.DataContainers.Add(
            "dc2",
            new AxDataContainer(
                "dc2",
                true,
                [new AxDataRow("row_new", AxRowState.New, fields: new Dictionary<string, AxFieldValue>
                {
                    ["item"] = new("widget"),
                    ["qty"] = new(1m),
                    ["rate"] = new(12.50m),
                    ["amount"] = new(12.50m)
                })]));

        return state;
    }
}
