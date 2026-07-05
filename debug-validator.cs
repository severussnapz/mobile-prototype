using Genesis.AI.Domain.Interfaces;
using Genesis.AI.Infrastructure.Services;

var selected = "<button class=\"btn btn-primary\">Save</button>";
var modelOutput = "<button class=\"btn btn-danger\">Save</button>";

var result = PrototypeElementValidator.Validate(modelOutput, selected);
Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Message: {result.Message}");
if (result.Status == PrototypeElementEditStatus.Rejected)
{
    Console.WriteLine("❌ TEST FAILED: Expected Applied, got Rejected");
}
else
{
    Console.WriteLine("✅ TEST PASSED");
}
