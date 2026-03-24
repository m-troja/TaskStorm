    using TaskStorm.Model.Entity.Masterdata;

    namespace TaskStorm.Model.Request;

    using System.Text.Json.Serialization;

    public record MasterdataValueRequest(
        int? Order,
        string Value,
        string Code,
        [property: JsonConverter(typeof(JsonStringEnumConverter))]
        MasterdataType Type,
        bool? IsActive,
        bool? Delete
    );
