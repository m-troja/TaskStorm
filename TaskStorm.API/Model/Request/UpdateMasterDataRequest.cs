using TaskStorm.Model.Entity.Masterdata;

namespace TaskStorm.Model.Request;

public record UpdateMasterDataRequest(
    int IssueId, 
    MasterdataType MasterdataType,
    string MasterDataCode,
    string MasterDataValue
    )
{
}
