using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace VibeCast.Infrastructure.AI;

public sealed class ChatResponseLoggingClient(
    IChatClient innerClient,
    ILogger<ChatResponseLoggingClient> logger) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        long startedAt = Stopwatch.GetTimestamp();

        try
        {
            ChatResponse response =
                await base.GetResponseAsync(
                    messages,
                    options,
                    cancellationToken);

            LogCompletedResponse(
                response,
                Stopwatch.GetElapsedTime(startedAt));

            return response;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Chat request was cancelled by the caller after " +
                "{ElapsedMilliseconds} ms.",
                Stopwatch
                    .GetElapsedTime(startedAt)
                    .TotalMilliseconds);

            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "Chat request ended because the provider operation " +
                "timed out after {ElapsedMilliseconds} ms.",
                Stopwatch
                    .GetElapsedTime(startedAt)
                    .TotalMilliseconds);

            throw;
        }
        catch (ClientResultException exception)
        {
            LogProviderFailure(
                exception,
                Stopwatch.GetElapsedTime(startedAt),
                operationType: "non-streaming");

            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unexpected chat-client failure after " +
                "{ElapsedMilliseconds} ms.",
                Stopwatch
                    .GetElapsedTime(startedAt)
                    .TotalMilliseconds);

            throw;
        }
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return InspectStreamingResponseAsync(
            messages,
            options,
            cancellationToken);
    }

    private async IAsyncEnumerable<ChatResponseUpdate> InspectStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
    {
        long startedAt = Stopwatch.GetTimestamp();

        int updateCount = 0;
        int characterCount = 0;

        string? responseId = null;
        string? modelId = null;

        ChatFinishReason? finishReason = null;

        await using IAsyncEnumerator<ChatResponseUpdate> enumerator =
           base.GetStreamingResponseAsync(
                   messages,
                   options,
                   cancellationToken)
               .GetAsyncEnumerator(cancellationToken);

        while (await MoveNextStreamingUpdateAsync(
                   enumerator,
                   startedAt,
                   updateCount,
                   characterCount,
                   cancellationToken))
        {
            ChatResponseUpdate update = enumerator.Current;

            updateCount++;
            characterCount += update.Text?.Length ?? 0;

            responseId ??= update.ResponseId;
            modelId ??= update.ModelId;

            if (update.FinishReason is not null)
            {
                finishReason = update.FinishReason;
            }

            yield return update;
        }

        LogStreamingCompletion(
            finishReason,
            responseId,
            modelId,
            updateCount,
            characterCount,
            Stopwatch.GetElapsedTime(startedAt));
    }

    private void LogStreamingCompletion(ChatFinishReason? finishReason, string? responseId, string? modelId, int updateCount, int characterCount, TimeSpan timeSpan)
    {
        ChatCompletionOutcome outcome =
            ChatCompletionOutcomeClassifier.Classify(
                finishReason);

        string finishReasonText =
            finishReason?.ToString()
            ?? "unspecified";

        if (outcome is ChatCompletionOutcome.Completed or
            ChatCompletionOutcome.ToolCallRequested)
        {
            logger.LogDebug(
                "Streaming chat response completed. Outcome: " +
                "{Outcome}; ResponseId: {ResponseId}; ModelId: " +
                "{ModelId}; FinishReason: {FinishReason}; Updates: " +
                "{UpdateCount}; Characters: {CharacterCount}; " +
                "ElapsedMilliseconds: {ElapsedMilliseconds}.",
                outcome,
                responseId,
                modelId,
                finishReasonText,
                updateCount,
                characterCount,
                timeSpan.TotalMilliseconds);

            return;
        }

        logger.LogWarning(
            "Streaming chat response ended without a normal " +
            "completion. Outcome: {Outcome}; ResponseId: " +
            "{ResponseId}; ModelId: {ModelId}; FinishReason: " +
            "{FinishReason}; Updates: {UpdateCount}; Characters: " +
            "{CharacterCount}; ElapsedMilliseconds: " +
            "{ElapsedMilliseconds}.",
            outcome,
            responseId,
            modelId,
            finishReasonText,
            updateCount,
            characterCount,
            timeSpan.TotalMilliseconds);
    }

    private async Task<bool> MoveNextStreamingUpdateAsync(IAsyncEnumerator<ChatResponseUpdate> enumerator, long startedAt, int updateCount, int characterCount, CancellationToken cancellationToken)
    {
        try
        {
            return await enumerator.MoveNextAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Streaming chat request was cancelled by the caller " +
                "after {ElapsedMilliseconds} ms. Updates: " +
                "{UpdateCount}; Characters: {CharacterCount}.",
                Stopwatch
                    .GetElapsedTime(startedAt)
                    .TotalMilliseconds,
                updateCount,
                characterCount);

            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "Streaming chat request timed out after " +
                "{ElapsedMilliseconds} ms. Updates: {UpdateCount}; " +
                "Characters: {CharacterCount}.",
                Stopwatch
                    .GetElapsedTime(startedAt)
                    .TotalMilliseconds,
                updateCount,
                characterCount);

            throw;
        }
        catch (ClientResultException exception)
        {
            LogProviderFailure(
                exception,
                Stopwatch.GetElapsedTime(startedAt),
                operationType: "streaming");

            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unexpected streaming chat-client failure after " +
                "{ElapsedMilliseconds} ms. Updates: {UpdateCount}; " +
                "Characters: {CharacterCount}.",
                Stopwatch
                    .GetElapsedTime(startedAt)
                    .TotalMilliseconds,
                updateCount,
                characterCount);

            throw;
        }
    }

    private void LogProviderFailure(ClientResultException exception, TimeSpan timeSpan, string operationType)
    {
        if (exception.Status == 429 || exception.Status >= 500)
        {
            logger.LogWarning(
                "Chat provider {OperationType} request did not " +
                "succeed after the configured provider retry policy. " +
                "HttpStatus: {HttpStatus}; ExceptionType: " +
                "{ExceptionType}; ElapsedMilliseconds: " +
                "{ElapsedMilliseconds}.",
                operationType,
                exception.Status,
                exception.GetType().Name,
                timeSpan.TotalMilliseconds);

            return;
        }

        logger.LogError(
            "Chat provider {OperationType} request failed. " +
            "HttpStatus: {HttpStatus}; ExceptionType: " +
            "{ExceptionType}; ElapsedMilliseconds: " +
            "{ElapsedMilliseconds}.",
            operationType,
            exception.Status,
            exception.GetType().Name,
            timeSpan.TotalMilliseconds);
    }

    private void LogCompletedResponse(ChatResponse response, TimeSpan elapsed)
    {
        ChatCompletionOutcome outcome =
            ChatCompletionOutcomeClassifier.Classify(
                response.FinishReason);

        string finishReason =
            response.FinishReason?.ToString()
            ?? "unspecified";

        switch (outcome)
        {
            case ChatCompletionOutcome.Completed:
                logger.LogDebug(
                    "Chat response completed normally. ResponseId: " +
                    "{ResponseId}; ModelId: {ModelId}; FinishReason: " +
                    "{FinishReason}; ElapsedMilliseconds: " +
                    "{ElapsedMilliseconds}.",
                    response.ResponseId,
                    response.ModelId,
                    finishReason,
                    elapsed.TotalMilliseconds);
                break;

            case ChatCompletionOutcome.ToolCallRequested:
                logger.LogDebug(
                    "Chat response requested a tool. ResponseId: " +
                    "{ResponseId}; ModelId: {ModelId}; FinishReason: " +
                    "{FinishReason}; ElapsedMilliseconds: " +
                    "{ElapsedMilliseconds}.",
                    response.ResponseId,
                    response.ModelId,
                    finishReason,
                    elapsed.TotalMilliseconds);
                break;

            case ChatCompletionOutcome.Truncated:
            case ChatCompletionOutcome.ContentFiltered:
                logger.LogWarning(
                    "Chat response ended abnormally. Outcome: " +
                    "{Outcome}; ResponseId: {ResponseId}; ModelId: " +
                    "{ModelId}; FinishReason: {FinishReason}; " +
                    "ElapsedMilliseconds: {ElapsedMilliseconds}.",
                    outcome,
                    response.ResponseId,
                    response.ModelId,
                    finishReason,
                    elapsed.TotalMilliseconds);
                break;

            case ChatCompletionOutcome.Unspecified:
            case ChatCompletionOutcome.ProviderSpecific:
                logger.LogWarning(
                    "Chat response completed without a recognized " +
                    "normalized finish reason. Outcome: {Outcome}; " +
                    "ResponseId: {ResponseId}; ModelId: {ModelId}; " +
                    "FinishReason: {FinishReason}; " +
                    "ElapsedMilliseconds: {ElapsedMilliseconds}.",
                    outcome,
                    response.ResponseId,
                    response.ModelId,
                    finishReason,
                    elapsed.TotalMilliseconds);
                break;
        }
    }
}
