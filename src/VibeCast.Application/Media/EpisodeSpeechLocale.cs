using System;
using System.Collections.Generic;
using System.Text;

namespace VibeCast.Application.Media;

public static class EpisodeSpeechLocale
{
    public static string Resolve(string language) =>
        language switch
        {
            "English (United States)" => "en-US",
            "English (United Kingdom)" => "en-GB",
            "Spanish" => "es-ES",
            "French" => "fr-FR",
            _ => throw new InvalidOperationException(
                $"Speech processing is not configured for '{language}'.")
        };
}
