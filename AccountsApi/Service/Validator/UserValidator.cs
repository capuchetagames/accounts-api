using Core.Dtos;
using FluentValidation;

namespace UsersApi.Service.Validator;

public class UserValidator : AbstractValidator<BaseUserDto>
{
    public UserValidator()
    {
        RuleFor(user => user.Name)
            .NotEmpty().WithMessage("O Nome é Obrigatório.")
            .Length(2, 50).WithMessage("O Nome deve ter entre 2 e 50 caracteres.");

        RuleFor(user => user.Email)
            .NotEmpty().WithMessage("O Email é Obrigatório.")
            .EmailAddress().WithMessage("O formato do Email não é válido.");

        RuleFor(user => user.Password)
            .NotEmpty().WithMessage("A Senha é obrigatória.")
            .Matches(@"[A-Z]").WithMessage("A Senha deve conter pelo menos 1 letra maiúscula.")
            .Matches(@"[a-z]").WithMessage("A Senha deve conter pelo menos 1 letra minúscula.")
            .Matches(@"[0-9]").WithMessage("A Senha deve conter pelo menos 1 número.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("A Senha deve conter pelo menos 1 caractere especial.")
            .MinimumLength(8).WithMessage("A Senha deve ter pelo menos 8 caracteres.");
        
        RuleFor(user => user.ConfirmPassword)
            .Equal(user => user.Password).WithMessage("A confirmação de Senha não confere.");
        
      
    }
    
}