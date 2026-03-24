using TaskStorm.Model.Entity.Masterdata;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;

namespace TaskStorm.Service;

public interface IMasterdataService
{
    Task<Masterdata> GetMasterdata();
    Task<MasterdataValue> CreateMasterdataValue(CreateMasterdataValueRequest req);

}
