using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using TaskStorm.Data;
using TaskStorm.Exception;
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

    public async Task<MasterdataAllTypesWithAllValuesDto> GetAllMasterdata()
    {
        var labels = await GetAllLabelsAsync();

        return new MasterdataAllTypesWithAllValuesDto( new() { labels }
        );
    }

    public async Task<MasterdataSingleTypeWithValuesDto> SetMasterdataValue(MasterdataValueRequest req)
    {
        int orderValidated = req.Order == null ? 0 : (int)req.Order;
        bool deleteValidated = req.Delete == null ? false : (bool)req.Delete;
        bool isActiveValidated = req.IsActive == null ? false : (bool)req.IsActive;
        l.LogDebug($"SetMasterdataValue req: ${req}");

        if (string.IsNullOrWhiteSpace(req.Code))
        {
            l.LogError($"Empty code in request, throwing exception");
            throw new BadRequestException("Code cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(req.Value))
        {
            l.LogError($"Empty Value in request, throwing exception");
            throw new BadRequestException("Value cannot be empty");
        }

        MasterdataType typeEnum;

        if (!Enum.IsDefined(typeof(MasterdataType), req.Type))
        {
            l.LogError($"Invalid MasterData type in request: {req.Type}");
            throw new BadRequestException("Invalid MasterData type in request");
        }
        else
        {
            typeEnum = req.Type;
        }

        // check duplicates
        var exists = await _db.MasterdataValues.AnyAsync(v => v.Value == req.Value && v.Type == req.Type || v.Code == req.Code && v.Type == req.Type || v.Value == req.Value && v.Type == req.Type);

        l.LogDebug($"exists: {exists}");
        l.LogDebug($"req.Value: {req.Value}");
        l.LogDebug($"req.Type, req.Type: {req.Type}, {req.Type}");
        l.LogDebug($"req.Delete: {req.Delete}");
        l.LogDebug($"exists && req.Delete: {exists && deleteValidated}");
        l.LogDebug($"exists && !req.Delete: {exists && !deleteValidated}");
        l.LogDebug($"!exists && req.Delete: {!exists && deleteValidated}");


        if (exists && deleteValidated)
        {
            var existingValue = await _db.MasterdataValues.FirstOrDefaultAsync(v => v.Value == req.Value && v.Type == req.Type ||  v.Code == req.Code && v.Type == req.Type || v.Value == req.Value && v.Type == req.Type);
            l.LogDebug("existingValue: {@existingValue}", existingValue);
            if (existingValue != null)
            {
                l.LogDebug($"Detected Delete request for Masterdata existingValue: ${existingValue.Value}, ${existingValue.Type}");
                _db.MasterdataValues.Remove(existingValue);
                await _db.SaveChangesAsync();
                l.LogInformation($"Removed MasterdataValue: ${existingValue.Value}, ${existingValue.Type}");
                return await GetMasterdataValuesForType(req.Type);
            }
        }
        else if (exists && !deleteValidated) 
        {
            l.LogError($"Type, code or value pair already exists: {req.Type}, {req.Value}, {req.Code}");
            throw new BadRequestException($"Type, code or value pair already exists: {req.Type}, {req.Value}, {req.Code}");
        }
        else if (!exists && deleteValidated)
        {
            l.LogError("Cannot delete not existing masterdata value");
            throw new BadRequestException("Cannot delete not existing masterdata value");
        }

        // Validate Order
        int maxOrder = await _db.MasterdataValues.Where(v => v.Type == req.Type).MaxAsync(v => (int?)v.Order) ?? 0;
        l.LogDebug($"Maxorder : {maxOrder}");

        orderValidated = maxOrder+1;

        l.LogDebug($"orderValidated : {orderValidated}");

        var value = new MasterdataValue()
        {
            Order = orderValidated,
            Code = req.Code,
            Type = req.Type,
            Value = req.Value,
            IsActive = isActiveValidated
        };

        await _db.MasterdataValues.AddAsync(value);
        await _db.SaveChangesAsync();

        l.LogDebug($"MasterdataValue: ${value}");

        var valueDao = await _db.MasterdataValues.AnyAsync(v => v.Value == req.Value && v.Type == req.Type );
        if (valueDao)
        {
            l.LogDebug($"Found valueDao in DB: ${valueDao}");
            return await GetMasterdataValuesForType(req.Type);
        }
        else
        {
            l.LogError("valueDao not found in DB, throwing ArgumentException");
            throw new ArgumentException($"Failed to save masterdata value: {req.Value}");
        }
    }

    public async Task<MasterdataSingleTypeWithValuesDto> GetMasterdataValuesForType(MasterdataType type)
    {
        l.LogDebug($"GetMasterdataValuesForType {type}");
        return type switch
        {
            MasterdataType.ISSUE_LABEL =>   await GetAllLabelsAsync(),

            _ =>  throw new ArgumentException("Invalid masterdata type"),  
        };

    }

    private async Task<MasterdataSingleTypeWithValuesDto> GetAllLabelsAsync()
    {
        var values = await _db.MasterdataValues.Where(v => v.Type == MasterdataType.ISSUE_LABEL).ToListAsync();
        var dto = new MasterdataSingleTypeWithValuesDto(MasterdataType.ISSUE_LABEL, values);
        l.LogDebug($"Returning GetAllLabels: {dto}");
        return dto;
    }
}
