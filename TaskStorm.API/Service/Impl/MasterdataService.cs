using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using TaskStorm.Data;
using TaskStorm.Exception.ProjectException;
using TaskStorm.Log;
using TaskStorm.Model.Entity;
using TaskStorm.Model.Entity.Masterdata;
using TaskStorm.Model.IssueFolder;
using TaskStorm.Model.Request;
namespace TaskStorm.Service.Impl;

public class MasterdataService : IMasterdataService
{
    private readonly PostgresqlDbContext _db ;
    private readonly ILogger<MasterdataService> l;
  

    public MasterdataService(PostgresqlDbContext db, ILogger<MasterdataService> l)
    {
        _db = db;
        this.l = l;
    }

    public Task<MasterdataValue> CreateMasterdataValue(CreateMasterdataValueRequest req)
    {
        throw new NotImplementedException();
    }

    public Task<Masterdata> GetMasterdata()
    {
        throw new NotImplementedException();
    }
}
