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

    [Fact]
    public void Validate_InsertAdjacentHtmlIntroducesNewClassInMarkup_ReturnsNull()
    {
        // insert_adjacent_html exists to add NEW markup. A class that appears only in the
        // inserted HTML (not yet in scope) is legitimate — it will be styled in a following
        // save_artefact on _styles.css. Only the anchor selector must already exist.
        var existingClasses = new[] { "card" };

        var error = PrototypeApplyToScopeGuard.Validate(
            scope: "screen-01-legacy",
            selector: ".card",
            operation: "insert_adjacent_html",
            value: "<span class=\"priority-badge\">High</span>",
            existingClasses: existingClasses);

        Assert.Null(error);
    }

    [Fact]
    public void Validate_InsertAdjacentHtmlWithInventedSelector_RejectsAndListsExistingClasses()
    {
        // The anchor selector must still exist — an invented selector matches no element and
        // silently no-ops, so it remains rejected even on the insert path.
        var existingClasses = new[] { "card" };

        var error = PrototypeApplyToScopeGuard.Validate(
            scope: "screen-01-legacy",
            selector: ".does-not-exist",
            operation: "insert_adjacent_html",
            value: "<span class=\"priority-badge\">High</span>",
            existingClasses: existingClasses);

        Assert.NotNull(error);
        Assert.Contains(".does-not-exist", error);
        Assert.Contains(".card", error);
    }
}
