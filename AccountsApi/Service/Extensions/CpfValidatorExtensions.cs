using AccountsApi.Service.Validator;
using FluentValidation;

namespace AccountsApi.Service.Extensions;


public static class CpfValidatorExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeValidCpf<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(cpf => CpfValidator.IsValid(cpf))
            .WithMessage("CPF inválido.");
    }
}