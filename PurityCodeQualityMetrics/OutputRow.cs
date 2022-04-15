namespace PurityCodeQualityMetrics.Git
{
    class OutputRow
    {
        public string ClassName { get; set; }
        public double Faulty { get; set; } = 0;

        public double CyclomaticComplexity { get; set; }
        public double SourceLinesOfCode { get; set; }
        public double CommentDensity { get; set; }

        public double WeightedMethodsPerClass { get; set; }
        public double DepthOfInheritance { get; set; }
        public double NumberOfChildren { get; set; }
        public double CouplingBetweenObjects { get; set; }
        public double ResponseForAClass { get; set; }
        public double LackOfCohesionOfMethods { get; set; }

        public double LambdaScore { get; set; }
        public double LambdaCount { get; set; }
        public double SourceLinesOfLambda { get; set; }
        public double LambdaUsed { get; set; }
        public double LambdaFieldVariableUsageCount { get; set; }
        public double LambdaFieldVariableUsed { get; set; }
        public double LambdaLocalVariableUsageCount { get; set; }
        public double LambdaLocalVariableUsed { get; set; }
        public double UnterminatedCollections { get; set; }
        public double LambdaSideEffectCount { get; set; }
        
        public double Purity { get; set; }

        public OutputRow(string className, IReadOnlyDictionary<Measure, double> metricResults)
        {
            ClassName = className;

            CyclomaticComplexity = metricResults[Measure.CyclomaticComplexity];
            SourceLinesOfCode = metricResults[Measure.SourceLinesOfCode];
            CommentDensity = metricResults[Measure.CommentDensity];

            WeightedMethodsPerClass = metricResults[Measure.WeightedMethodsPerClass];
            DepthOfInheritance= metricResults[Measure.DepthOfInheritanceTree];
            NumberOfChildren = metricResults[Measure.NumberOfChildren];
            CouplingBetweenObjects = metricResults[Measure.CouplingBetweenObjects];
            ResponseForAClass = metricResults[Measure.ResponseForAClass];
            LackOfCohesionOfMethods = metricResults[Measure.LackOfCohesionOfMethods];

            SourceLinesOfLambda = metricResults[Measure.SourceLinesOfLambda];
            LambdaScore = metricResults[Measure.LambdaScore];
//            LambdaUsed = metricResults[Measure.LambdaCount].LimitToOne();
            LambdaCount = metricResults[Measure.LambdaCount]; 
//            LambdaFieldVariableUsed = metricResults[Measure.LambdaFieldVariableUsageCount].LimitToOne(); 
            LambdaFieldVariableUsageCount = metricResults[Measure.LambdaFieldVariableUsageCount];
//            LambdaLocalVariableUsed = metricResults[Measure.LambdaLocalVariableUsageCount].LimitToOne(); 
            LambdaLocalVariableUsageCount = metricResults[Measure.LambdaLocalVariableUsageCount];
            LambdaSideEffectCount = metricResults[Measure.LambdaSideEffectCount];
            UnterminatedCollections = metricResults[Measure.UnterminatedCollections]; 
            Purity = metricResults[Measure.Purity]; 
        }

        public void SetFaulty() => Faulty = 1;
    }
}