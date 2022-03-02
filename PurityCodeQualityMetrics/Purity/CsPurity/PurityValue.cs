namespace PurityCodeQualityMetrics.Purity.CsPurity;

public enum PurityValue
{
    Pure = 6,
    ThrowsException = 5,
    Undeterministic = 4,
    ParametricallyImpure = 2,
    Impure = 2,
    Unknown = 1,
}