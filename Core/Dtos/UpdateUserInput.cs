namespace Core.Dtos;

/// <summary>
/// DTO para atualização de usuário.
/// </summary>
public class UpdateUserInput :UserInput
{
    public bool IsActive { get; set; }
}