using Core.Dtos;
using FluentValidation;

namespace UsersApi.Service.Validator;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(user => user.Name)
            .NotEmpty().WithMessage("O Nome é Obrigatório.");

        RuleFor(user => user.Password)
            .NotEmpty().WithMessage("A Senha é obrigatória.");
    }
    
}