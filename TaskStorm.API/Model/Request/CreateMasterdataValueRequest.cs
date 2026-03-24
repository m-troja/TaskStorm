using TaskStorm.Model.Entity.Masterdata;

namespace TaskStorm.Model.Request;

public record MasterdataValueRequest(
    int Order, 
    string Value,
    string Code,
    MasterdataType Type,
    bool IsActive
    )  {}
