namespace TaskStorm.Model.Entity.Masterdata;

public class IssueLabel 
{
    public MasterdataType Type { get; set; }
    
    public IssueLabel()
    {
        this.Type = MasterdataType.IssueLabel;
    }

}
