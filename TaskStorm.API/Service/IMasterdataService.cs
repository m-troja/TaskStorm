using TaskStorm.Model.Entity.Masterdata;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;

namespace TaskStorm.Service;

public interface IMasterdataService
{
    Task<MasterdataAllTypesWithAllValuesDto> GetAllMasterdata();
    Task<MasterdataSingleTypeWithValuesDto> SetMasterdataValue(MasterdataValueRequest req);
    Task<MasterdataSingleTypeWithValuesDto> GetMasterdataValuesForType(MasterdataType type);

}
