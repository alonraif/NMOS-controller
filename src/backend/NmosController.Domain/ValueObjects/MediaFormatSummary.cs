namespace NmosController.Domain.ValueObjects;

public sealed record MediaFormatSummary(
    string Format,
    string? MediaType,
    string? GrainRate,
    string? FrameWidth,
    string? FrameHeight,
    string? SampleRate);
