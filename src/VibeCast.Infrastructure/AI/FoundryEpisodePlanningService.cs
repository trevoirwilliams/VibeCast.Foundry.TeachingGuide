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
        // PRACTICE S05-03: GenerateAsync
        // 1. Validate the request and use the supplied helpers to build initial messages.
        // 2. Request and validate the first plan; return it if valid.
        // 3. Otherwise build repair messages from the first plan and its validation errors.
        // 4. Request and validate one repair; reject a second invalid plan.
        // 5. Return the accepted plan with accurate prompt and repair metadata.
        // Optional API hints and checks: docs/practice/README.md#s05-03-generateasync
        throw new NotImplementedException("S05-03: implement GenerateAsync.");
    }

    private Task<EpisodePlan> RequestTypedPlanAsync(ChatMessage[] initialMessages, string operationName, CancellationToken cancellationToken)
    {
        // PRACTICE S05-01: RequestTypedPlanAsync
        // 1. Use the supplied messages and JSON options to request a typed episode plan.
        // 2. Set the output limit and pass the caller token.
        // 3. Check the completion state before extracting the typed result.
        // 4. Reject a missing result; return the EpisodePlan rather than response text.
        // Optional API hints and checks: docs/practice/README.md#s05-01-requesttypedplanasync
        throw new NotImplementedException("S05-01: implement RequestTypedPlanAsync.");
    }

    
}
