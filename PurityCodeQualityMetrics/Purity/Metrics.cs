﻿namespace PurityCodeQualityMetrics.Purity;

public static class Metrics
{
    public static double Metric1(this PurityScore score)
    {
        return(score.Violations.Where(x => x.Violation != PurityViolation.UnknownMethod).Select(v => 1d / (v.Distance + 1)).Sum() + 1) / (score.DependencyCount + 1);
    }
}