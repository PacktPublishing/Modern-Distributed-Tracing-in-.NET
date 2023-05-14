namespace Memes.OpenTelemetry.Common;

public enum SamplingStrategy
{
    AlwaysOff, // sample out everything, as a precaution
    Parent,
    Probability
}
