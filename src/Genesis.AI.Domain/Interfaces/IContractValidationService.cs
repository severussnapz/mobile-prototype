namespace Genesis.AI.Domain.Interfaces;

public sealed record ContractValidationResult(
    bool IsValid,
    IReadOnlyList<string> Violations);

public interface IContractValidationService
{
    ContractValidationResult ValidatePipeline01(string reqContent);
    ContractValidationResult ValidatePipeline03(string reqContent);
    ContractValidationResult ValidatePipeline04(string reqContent);
    ContractValidationResult ValidatePipeline05(string reqContent);
    ContractValidationResult ValidatePipeline06(string reqContent);
    ContractValidationResult ValidatePipeline07(string reqContent);
    ContractValidationResult ValidatePipeline08(string reqContent);
}
