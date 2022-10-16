using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleTables;
using PurityCodeQualityMetrics;
using PurityCodeQualityMetrics.CodeMetrics;
using PurityCodeQualityMetrics.DataProcessing;
using PurityCodeQualityMetrics.GitCrawler;
using PurityCodeQualityMetrics.Purity;


GenerateConstruct();

void GenerateConstruct()
{
    var lst = new List<MethodType>
    {
        MethodType.Lambda,
        MethodType.Local,
        MethodType.Method,
    };
    Score.Mode = 2;

    foreach (var project in TargetProject.GetTargetProjects())
        for (int m = 0; m < 4; m++)
        {
            Score.Mode = m;
            var p = project.RepositoryName;
            for (int i = 0; i < 3; i++)
            {
                var mt = lst[i];
                var model = ModelType.Purity;

                var data = Data.GetFinalData(p).Where(x => x.MethodType == mt)
                    .Select(x => Regression.Generate(x, false, model))
                    .Concat(Data.GetData(p).Where(x => x.Before != null).Select(x => x.Before)
                        .Where(x => x.MethodType == mt)
                        .Select(x => Regression.Generate(x, true, model)))
                    .ToArray();

                File.WriteAllLines(Path.Combine(Regression.Path, "constructs", $"regression-{p}-{m}-{mt}.csv"),
                    Regression.ToLines(data));
            }
        }
}


void GenerateMetrics()
{
    foreach (var project in TargetProject.GetTargetProjects())
    {
        var p = project.RepositoryName;
        for (int i = 0; i < 5; i++)
        {
            Score.Mode = i;
            var model = ModelType.Purity;

            var data = Data.GetFinalData(p).Select(x => Regression.Generate(x, false, model))
                .Concat(Data.GetData(p).Where(x => x.Before != null).Select(x => x.Before)
                    .Select(x => Regression.Generate(x, true, model)))
                .ToArray();

            File.WriteAllLines(Path.Combine(Regression.Path, "purity_metric", $"regression-{p}-{i}.csv"),
                Regression.ToLines(data));
        }
    }
}


void GenerateModelsCombintations()
{
    Score.Mode = 2;

    var lst = new List<(ModelType, ModelType)>
    {
        (ModelType.BaselineOOP, ModelType.BaselineFP),
        (ModelType.BaselineOOP, ModelType.Purity),
        (ModelType.Purity, ModelType.BaselineFP),
    };


    foreach (var project in TargetProject.GetTargetProjects())
   {
       var p = project.RepositoryName;
      //  for (int i = 0; i < lst.Count; i++)
        {
       //     var model = lst[i].Item1;
      //      var model2 = lst[i].Item2;

            var data = Data.GetFinalData(p).Select(x => Regression.Generate(x, false))
                .Concat(Data.GetData(p).Where(x => x.Before != null).Select(x => x.Before)
                    .Select(x => Regression.Generate(x, true)))
                .ToArray();

            File.WriteAllLines(
                Path.Combine(Regression.Path, "combinations", $"regression-{p}-{ModelType.Purity.ToStr()}-{ModelType.BaselineFP.ToStr()}-{ModelType.BaselineOOP.ToStr()}.csv"),
                Regression.ToLines(data));
        }
    }
}


void GenerateModels()
{
    Score.Mode = 2;

    var lst = new List<ModelType>
    {
        ModelType.BaselineOOP,
        ModelType.BaselineFP,
        ModelType.Purity,
    };


    //  foreach (var project in TargetProject.GetTargetProjects())
    {
        var p = ""; //project.RepositoryName;
        for (int i = 0; i < 3; i++)
        {
            var model = lst[i];

            var data = Data.GetFinalData(p).Select(x => Regression.Generate(x, false, model))
                .Concat(Data.GetData(p).Where(x => x.Before != null).Select(x => x.Before)
                    .Select(x => Regression.Generate(x, true, model)))
                .ToArray();

            File.WriteAllLines(Path.Combine(Regression.Path, "models", $"regression-{model.ToStr()}.csv"),
                Regression.ToLines(data));
        }
    }
}


//
// var data2 = Data.GetFinalData(p).Where(x => x.HasAllMetrics()).Select(x => Regression.GenereOld(x, false))
//     .Concat(Data.GetData(p).Where(x => x.Before != null).Select(x => x.Before)
//         .Where(x => x.HasAllMetrics()).Select(x => Regression.GenereOld(x, true)))
//     .ToArray();
//
// File.WriteAllLines(Path.Combine(Regression.Path, $"old-metrics-{p}-{i}.csv"), Regression.ToLines(data2));