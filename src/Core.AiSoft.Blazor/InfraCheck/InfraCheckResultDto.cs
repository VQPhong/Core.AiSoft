using System.Collections.Generic;
using System.Linq;

namespace Core.AiSoft.Blazor.InfraCheck;

/// <summary>
/// Outcome of one infra check. Failures are reported as data (HTTP 200), not as exceptions.
/// </summary>
public class InfraCheckResultDto
{
    public string Target { get; set; } = default!;

    public bool Success => Steps.Count > 0 && Steps.All(step => step.Success);

    public List<InfraCheckStepDto> Steps { get; set; } = new();
}

public class InfraCheckStepDto
{
    public string Name { get; set; } = default!;

    public bool Success { get; set; }

    public string? Detail { get; set; }
}
