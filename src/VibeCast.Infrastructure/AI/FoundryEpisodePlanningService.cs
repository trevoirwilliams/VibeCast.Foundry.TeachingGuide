using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using VibeCast.Application.Episodes;
using VibeCast.Application.Validation;

namespace VibeCast.Infrastructure.AI;

public class FoundryEpisodePlanningService(
    IChatClient chatClient,
    IValidator<EpisodePlan> planValidator,
    ILogger<FoundryEpisodePlanningService> logger) : CommonEpisodePlanningMethods, IEpisodePlanningService
{
    public Task<EpisodePlanningResult> GenerateAsync(GenerateEpisodePlanRequest request, CancellationToken cancellationToken = default)
    {
        // PRACTICE S05-03: Generate and validate a plan. Return valid output; otherwise attempt one repair, validate again and stop on failure. Preserve prompt-version metadata.
        // Completion criteria and optional hints: docs/practice/README.md.
        throw new NotImplementedException("S05-03: implement GenerateAsync.");
    }

    private async Task<EpisodePlan> RequestTypedPlanAsync(ChatMessage[] initialMessages, string operationName, CancellationToken cancellationToken)
    {
        // PRACTICE S05-01: Request a typed episode plan with the existing schema contract. Reject unusable completion states and missing results; preserve cancellation.
        // Completion criteria and optional hints: docs/practice/README.md.
        throw new NotImplementedException("S05-01: implement RequestTypedPlanAsync.");
    }

    
}
