namespace Core.Entity;

/// <summary>
/// Representa um usuário no banco de dados.
/// </summary>
public class User : EntityBase
{ 
    public required string Name  { get; set; }
    
    public required string Cpf  { get; set; }
    public required string Email  { get; set; }
    
    public required string PasswordHash  { get; set; }
    public required Role Role  { get; set; }
    
    public required bool IsActive  { get; set; }
    
    public Guid? CreatedBy { get; set; }
}