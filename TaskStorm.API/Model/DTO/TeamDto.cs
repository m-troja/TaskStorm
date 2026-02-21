namespace TaskStorm.Model.DTO;

public record TeamDto(
    int Id, 
    string Name, 
    List<int> Issues, 
    List<int> users)
{
}
