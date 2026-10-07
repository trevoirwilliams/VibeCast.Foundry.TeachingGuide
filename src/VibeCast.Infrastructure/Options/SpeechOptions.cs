using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace VibeCast.Infrastructure.Options;

public sealed class SpeechOptions
{
    public const string SectionName = "Speech";

    [Required]
    [Url]
    public string Endpoint { get; init; } = string.Empty;

    public string? ApiKey { get; init; }
}
