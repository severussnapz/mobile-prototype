using Genesis.AI.Infrastructure.Services;

namespace Genesis.AI.Tests.Infrastructure;

public class PrototypeApplyToScopeGuardTests
{
    [Fact]
    public void Validate_SelectorReferencesClassAbsentFromScope_RejectsAndListsExistingClasses()
    {
        var existingClasses = new[] { "urgency-arrow", "banner" };

        var error = PrototypeApplyToScopeGuard.Validate(
            scope: "screen-01-legacy",
            selector: ".urg-arrow",
            operation: "set_node_text",
            value: "Updated",
            existingClasses: existingClasses);

        Assert.NotNull(error);
        Assert.Contains(".urgency-arrow", error);
        Assert.Contains(".banner", error);
    }

    [Fact]
    public void Validate_InsertAdjacentHtmlValueContainsCss_RejectsWithInsertHtmlOnlyMessage()
    {
        var existingClasses = new[] { "card" };

        var error = PrototypeApplyToScopeGuard.Validate(
            scope: "screen-01-legacy",
            selector: ".card",
            operation: "insert_adjacent_html",
            value: "<div class=\"card\"><style>.x{color:red;}</style></div>",
            existingClasses: existingClasses);

        Assert.NotNull(error);
        Assert.Contains("insert HTML only, reuse an existing class, do not author CSS", error);
    }

    [Fact]
    public void Validate_SelectorAndInsertedClassesExist_ReturnsNull()
    {
        var existingClasses = new[] { "card", "card-title" };

        var error = PrototypeApplyToScopeGuard.Validate(
            scope: "screen-01-legacy",
            selector: ".card",
            operation: "insert_adjacent_html",
            value: "<span class=\"card-title\">Hello</span>",
            existingClasses: existingClasses);

        Assert.Null(error);
    }
}
