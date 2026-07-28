using System;
using System.Collections.Generic;
using System.Text;

namespace VibeCast.Application.Episodes;

public sealed record EpisodePlan(
    string Summary,
    int TargetDurationMinutes,
    EpisodePlanSegment[] Segments,
    string[] KeyMessages,
    string[] EvidenceRequirements,
    string[] MediaRequirements,
    string[] EditorialRisks);

public sealed record EpisodePlanSegment(
    int Sequence,
    string Title,
    string Purpose,
    int DurationMinutes,
    string[] TalkingPoints);
