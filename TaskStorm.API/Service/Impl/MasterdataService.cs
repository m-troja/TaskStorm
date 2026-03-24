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

        // validate MasterData type
        if (!Enum.IsDefined(typeof(MasterdataType) ,req.Type))
        {
            l.LogError($"Invalid MasterData type in request: {req.Type}");
            throw new BadRequestException("Invalid MasterData type in request");
        }

        // check duplicates
        var exists = await _db.MasterdataValues.AnyAsync(v => v.Value == req.Value || v.Type == req.Type);

        if (exists)
        {
            l.LogError($"Type or value already exists: {req.Type}, {req.Value}");
            throw new BadRequestException($"Type or value already exists: {req.Type}, {req.Value}");
        }

        l.LogDebug($"Exists: {exists}");
        var value = new MasterdataValue()
        {
            Order = req.Order,
            Code = req.Code,
            Type = req.Type,
            Value = req.Value,
            IsActive = req.IsActive
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
            MasterdataType.IssueLabel =>   await GetAllLabelsAsync(),

            _ =>  throw new ArgumentException("Invalid masterdata type"),  
        };

    }

    private async Task<MasterdataSingleTypeWithValuesDto> GetAllLabelsAsync()
    {
        var values = await _db.MasterdataValues.Where(v => v.Type == MasterdataType.IssueLabel).ToListAsync();
        var dto = new MasterdataSingleTypeWithValuesDto(MasterdataType.IssueLabel, values);
        l.LogDebug($"Returning GetAllLabels: {dto}");
        return dto;
    }
}
