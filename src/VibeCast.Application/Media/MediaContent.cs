using System;
using System.Collections.Generic;
using System.Text;

namespace VibeCast.Application.Media;

public sealed record MediaContent(
    Stream Content,
    string ContentType,
    string OriginalFileName);
