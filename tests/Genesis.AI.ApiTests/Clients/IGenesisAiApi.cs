using ApiAutomationCore;
using ApiAutomationCore.Generated;
using Emis.X.Scopes;
using Refit;

namespace Genesis.AI.ApiTests.Clients;

[Headers("Accept: application/json")]
public interface IGenesisAiApi : IApi
{
    #region Health Endpoints

    [ExcludeFromCorsTest, ProtectedBy(Scope.None)]
    [Get("/health")]
    Task<ApiResponse<string>> GetHealthAsync();

    [ExcludeFromCorsTest, ProtectedBy(Scope.None)]
    [Get("/health/ready")]
    Task<ApiResponse<string>> GetHealthReadyAsync();

    #endregion

    #region Projects Endpoints

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Get("/api/v1/projects")]
    Task<ApiResponse<HttpResponseMessage>> GetProjectsAsync([Authorize] string token);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Get("/api/v1/projects")]
    Task<ApiResponse<HttpResponseMessage>> GetProjectsAsync([Authorize] string token, [Query] string? status = null);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Get("/api/v1/projects/{id}")]
    Task<ApiResponse<HttpResponseMessage>> GetProjectAsync([Authorize] string token, Guid id);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Post("/api/v1/projects")]
    Task<ApiResponse<HttpResponseMessage>> CreateProjectAsync([Authorize] string token, [Body] object body);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Delete("/api/v1/projects/{id}")]
    Task<ApiResponse<HttpResponseMessage>> DeleteProjectAsync([Authorize] string token, Guid id);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Get("/api/v1/projects/{projectId}/parking-lot")]
    Task<ApiResponse<HttpResponseMessage>> GetProjectParkingLotAsync([Authorize] string token, Guid projectId);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Get("/api/v1/projects/{projectId}/export")]
    Task<ApiResponse<HttpResponseMessage>> ExportProjectAsync([Authorize] string token, Guid projectId);

    #endregion

    #region Artefacts Endpoints

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Get("/api/v1/projects/{projectId}/artefacts")]
    Task<ApiResponse<HttpResponseMessage>> GetArtefactsByProjectAsync([Authorize] string token, Guid projectId);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Get("/api/v1/projects/{projectId}/artefacts/{artefactId}")]
    Task<ApiResponse<HttpResponseMessage>> GetArtefactByIdAsync([Authorize] string token, Guid projectId, Guid artefactId);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Post("/api/v1/projects/{projectId}/artefacts")]
    Task<ApiResponse<HttpResponseMessage>> CreateArtefactsAsync([Authorize] string token, Guid projectId, [Body] object body);

    #endregion

    #region Conversations Endpoints

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Post("/api/v1/conversations")]
    Task<ApiResponse<HttpResponseMessage>> CreateConversationAsync([Authorize] string token, [Body] object body);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Get("/api/v1/conversations/{id}")]
    Task<ApiResponse<HttpResponseMessage>> GetConversationAsync([Authorize] string token, Guid id);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Get("/api/v1/conversations/by-stage/{stageId}")]
    Task<ApiResponse<HttpResponseMessage>> GetConversationsByStageAsync([Authorize] string token, Guid stageId);

    #endregion

    #region Conversation State Endpoints

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Get("/api/v1/conversations/{conversationId}/progress")]
    Task<ApiResponse<HttpResponseMessage>> GetConversationProgressAsync([Authorize] string token, Guid conversationId);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Post("/api/v1/conversations/{conversationId}/advance-phase")]
    Task<ApiResponse<HttpResponseMessage>> AdvancePhaseAsync([Authorize] string token, Guid conversationId);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Patch("/api/v1/conversations/{conversationId}/phase")]
    Task<ApiResponse<HttpResponseMessage>> SetPhaseAsync([Authorize] string token, Guid conversationId, [Body] object body);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqRead, Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Get("/api/v1/conversations/{conversationId}/parking-lot")]
    Task<ApiResponse<HttpResponseMessage>> GetConversationParkingLotAsync([Authorize] string token, Guid conversationId);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Post("/api/v1/conversations/{conversationId}/parking-lot")]
    Task<ApiResponse<HttpResponseMessage>> AddParkingLotItemAsync([Authorize] string token, Guid conversationId, [Body] object body);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Post("/api/v1/conversations/{conversationId}/parking-lot/{itemId}/resolve")]
    Task<ApiResponse<HttpResponseMessage>> ResolveParkingLotItemAsync([Authorize] string token, Guid conversationId, Guid itemId);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Post("/api/v1/conversations/{conversationId}/parking-lot/{itemId}/defer")]
    Task<ApiResponse<HttpResponseMessage>> DeferParkingLotItemAsync([Authorize] string token, Guid conversationId, Guid itemId);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin, Scope.GenaiReqArch, Scope.GenaiReqPxd, Scope.GenaiReqClin)]
    [Delete("/api/v1/conversations/{conversationId}/parking-lot/{itemId}")]
    Task<ApiResponse<HttpResponseMessage>> DeleteParkingLotItemAsync([Authorize] string token, Guid conversationId, Guid itemId);

    #endregion

    #region Pipeline Stages Endpoints

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Post("/api/v1/stages/{stageId}/complete")]
    Task<ApiResponse<HttpResponseMessage>> CompleteStageAsync([Authorize] string token, Guid stageId);

    [ExcludeFromScopeTest, ProtectedBy(Scope.GenaiReqWrite, Scope.GenaiReqAdmin)]
    [Post("/api/v1/stages/{stageId}/skip")]
    Task<ApiResponse<HttpResponseMessage>> SkipStageAsync([Authorize] string token, Guid stageId);

    #endregion
}
