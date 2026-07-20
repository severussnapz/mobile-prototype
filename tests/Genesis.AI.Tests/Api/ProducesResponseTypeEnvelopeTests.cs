using System.Reflection;
using Genesis.AI.Api.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Genesis.AI.Tests.Api;

/// <summary>
/// Verifies that every controller action which declares a 2xx ProducesResponseType
/// attribute wraps its payload in <see cref="ApiResponse{T}"/> — the honest-spec
/// requirement from design decision §1b.
///
/// All tests are RED until ProducesResponseType annotations are fixed across all
/// controllers to use typeof(ApiResponse&lt;T&gt;) for JSON-returning 2xx responses.
///
/// Known legitimate exclusions (file-download endpoints that do not return an
/// ApiResponse envelope) are declared in <see cref="FileDownloadControllers"/>.
/// </summary>
public class ProducesResponseTypeEnvelopeTests
{
    /// <summary>
    /// Controllers whose actions legitimately return file content (binary downloads)
    /// rather than the <see cref="ApiResponse{T}"/> JSON envelope.
    /// SSE streaming endpoints are excluded via the lack of a typed annotation.
    /// </summary>
    private static readonly HashSet<string> FileDownloadControllers =
    [
        "ProjectExportController",
        "HazardLogController",
        "SecurityReviewReportController",
        "DataProtectionImpactAssessmentController",
    ];

    private static readonly Assembly ApiAssembly =
        typeof(Genesis.AI.Api.Program).Assembly;

    private static readonly Type ApiResponseOpenGeneric = typeof(ApiResponse<>);

    // ─── Envelope annotation correctness ──────────────────────────────────────

    [Fact]
    public void ControllerActions_With2xxProducesResponseType_AllUseApiResponseTEnvelope()
    {
        // Collect every action on every non-file controller that declares a typed
        // 2xx ProducesResponseType. Every such type MUST be ApiResponse<T>.
        var violations = new List<string>();

        var controllerTypes = ApiAssembly
            .GetTypes()
            .Where(type =>
                typeof(ControllerBase).IsAssignableFrom(type)
                && !type.IsAbstract
                && !FileDownloadControllers.Contains(type.Name));

        foreach (var controllerType in controllerTypes)
        {
            foreach (var method in controllerType.GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var attributes = method
                    .GetCustomAttributes<ProducesResponseTypeAttribute>(inherit: true)
                    .Where(attribute =>
                        attribute.StatusCode is >= 200 and < 300
                        && attribute.Type != null
                        && attribute.Type != typeof(void));

                foreach (var attribute in attributes)
                {
                    var responseType = attribute.Type!;

                    var isWrapped =
                        responseType.IsGenericType
                        && responseType.GetGenericTypeDefinition() == ApiResponseOpenGeneric;

                    if (!isWrapped)
                    {
                        violations.Add(
                            $"{controllerType.Name}.{method.Name}: " +
                            $"[ProducesResponseType(typeof({responseType.Name}), {attribute.StatusCode})] " +
                            $"must be [ProducesResponseType(typeof(ApiResponse<{responseType.Name}>), {attribute.StatusCode})]");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void FileDownloadControllers_DoNotDeclareApiResponseTAnnotations()
    {
        // File-download controllers must only declare typed annotations for the
        // actual binary content types (application/zip, application/vnd.*).
        // If one mistakenly wraps in ApiResponse<T> the download is broken.
        var violations = new List<string>();

        var controllerTypes = ApiAssembly
            .GetTypes()
            .Where(type =>
                typeof(ControllerBase).IsAssignableFrom(type)
                && !type.IsAbstract
                && FileDownloadControllers.Contains(type.Name));

        foreach (var controllerType in controllerTypes)
        {
            foreach (var method in controllerType.GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var attributes = method
                    .GetCustomAttributes<ProducesResponseTypeAttribute>(inherit: true)
                    .Where(attribute =>
                        attribute.StatusCode is >= 200 and < 300
                        && attribute.Type != null);

                foreach (var attribute in attributes)
                {
                    var responseType = attribute.Type!;

                    var isApiResponse =
                        responseType.IsGenericType
                        && responseType.GetGenericTypeDefinition() == ApiResponseOpenGeneric;

                    if (isApiResponse)
                    {
                        violations.Add(
                            $"{controllerType.Name}.{method.Name}: " +
                            $"file-download endpoint must not use ApiResponse<T> annotation.");
                    }
                }
            }
        }

        Assert.Empty(violations);
    }
}
