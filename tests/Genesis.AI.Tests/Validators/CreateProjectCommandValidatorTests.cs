using Genesis.AI.Domain.Commands.CreateProject;
using Genesis.AI.Domain.Enums;

namespace Genesis.AI.Tests.Validators;

public class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = new CreateProjectCommand("DOC", "Documents Management", "A description", "TS001", ComplianceDomain.ClinicalUk, "user-1");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        var command = new CreateProjectCommand("DOC", "", null, "TS001", ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NameTooLong_ShouldFail()
    {
        var command = new CreateProjectCommand("DOC", new string('x', 201), null, "TS001", ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_EmptyCode_ShouldFail()
    {
        var command = new CreateProjectCommand("", "Name", null, "TS001", ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_CodeTooShort_ShouldFail()
    {
        var command = new CreateProjectCommand("AB", "Name", null, "TS001", ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_CodeTooLong_ShouldFail()
    {
        var command = new CreateProjectCommand("ABCDEFGHIJK", "Name", null, "TS001", ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_CodeContainsLowercase_ShouldFail()
    {
        var command = new CreateProjectCommand("Doc", "Name", null, "TS001", ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_CodeContainsNumbers_ShouldFail()
    {
        var command = new CreateProjectCommand("DOC1", "Name", null, "TS001", ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_DescriptionTooLong_ShouldFail()
    {
        var command = new CreateProjectCommand("DOC", "Name", new string('x', 2001), "TS001", ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }

    [Fact]
    public void Validate_NullDescription_ShouldPass()
    {
        var command = new CreateProjectCommand("DOC", "Name", null, "TS001", ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyTimeSheetCode_ShouldFail()
    {
        var command = new CreateProjectCommand("DOC", "Name", null, "", ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TimeSheetCode");
    }

    [Fact]
    public void Validate_TimeSheetCodeTooLong_ShouldFail()
    {
        var command = new CreateProjectCommand("DOC", "Name", null, new string('x', 51), ComplianceDomain.Generic, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TimeSheetCode");
    }

    [Fact]
    public void Validate_EmptyCreatedBy_ShouldFail()
    {
        var command = new CreateProjectCommand("DOC", "Name", null, "TS001", ComplianceDomain.Generic, "");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CreatedBy");
    }

    [Fact]
    public void Validate_InvalidComplianceDomain_ShouldFail()
    {
        var command = new CreateProjectCommand("DOC", "Name", null, "TS001", (ComplianceDomain)99, "user-1");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ComplianceDomain");
    }
}
