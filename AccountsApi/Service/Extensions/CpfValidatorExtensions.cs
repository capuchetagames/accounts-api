using AccountsApi.Service.Validator;
using FluentValidation;

namespace AccountsApi.Service.Extensions;


/// <summary>
/// 
/// </summary>
public static class CpfValidatorExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ruleBuilder"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IRuleBuilderOptions<T, string> MustBeValidCpf<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(cpf => CpfValidator.IsValid(cpf))
            .WithMessage("CPF inválido.");
    }
}