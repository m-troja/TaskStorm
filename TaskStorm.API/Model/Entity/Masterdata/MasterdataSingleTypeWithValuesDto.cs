namespace TaskStorm.Model.Entity.Masterdata;

public record MasterdataSingleTypeWithValuesDto
    (
    MasterdataType Type,
    List<MasterdataValue> Values
    
    )
{
}
