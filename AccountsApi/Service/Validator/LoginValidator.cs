using AccountsApi.Service.Extensions;
using Core.Dtos;
using FluentValidation;

namespace AccountsApi.Service.Validator;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(user => user.Cpf)
            .NotEmpty().WithMessage("O Cpf é Obrigatório.")
            .MustBeValidCpf();
        

        RuleFor(user => user.Password)
            .NotEmpty().WithMessage("A Senha é obrigatória.");
    }
    
}