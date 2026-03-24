using TaskStorm.Model.Entity.Masterdata;

namespace TaskStorm.Model.Request;

public record CreateMasterdataValueRequest(
    int Order, 
    string Value,
    string Code,
    MasterdataType Type,
    bool IsActive
    )  {}
