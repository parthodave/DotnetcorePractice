namespace DotNet8WebAPI.Services.AI;

public sealed class PendingToolApprovalResult
{
    public string Status { get; init; } = "PendingApproval";
    public string ToolName { get; init; } = string.Empty;
    public string ApprovalId { get; init; } = string.Empty;
    public string Message { get; init; } = "This action requires explicit user approval.";
}
