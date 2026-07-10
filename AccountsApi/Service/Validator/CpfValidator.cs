using System.Text.RegularExpressions;

namespace AccountsApi.Service.Validator;


public static class CpfValidator
{
    public static string Normalize(string? cpf)
    {
        return string.IsNullOrWhiteSpace(cpf) ? string.Empty : Regex.Replace(cpf, @"[^\d]", "");
    }

    public static string Format(string? cpf)
    {
        var digits = Normalize(cpf);

        if (digits.Length != 11)
            return cpf ?? string.Empty;

        return $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}";
    }
    
    public static bool IsValid(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        cpf = Normalize(cpf);

        if (cpf.Length != 11)
            return false;

        // Rejeita CPFs com todos os dígitos iguais (ex: 111.111.111-11)
        if (cpf.Distinct().Count() == 1)
            return false;

        var numbers = cpf.Select(c => int.Parse(c.ToString())).ToArray();

        // Calcula o 1º dígito verificador
        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += numbers[i] * (10 - i);

        var remainder = sum % 11;
        var firstNumber = remainder < 2 ? 0 : 11 - remainder;

        if (numbers[9] != firstNumber)
            return false;

        // Calcula o 2º dígito verificador
        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += numbers[i] * (11 - i);

        remainder = sum % 11;
        var secondNumber = remainder < 2 ? 0 : 11 - remainder;

        return numbers[10] == secondNumber;
    }

    
}