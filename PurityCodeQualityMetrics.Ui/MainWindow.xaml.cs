using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;
using PurityCodeQualityMetrics.Purity;
using PurityCodeQualityMetrics.Purity.Storage;
using PurityCodeQualityMetrics.Purity.Util;

namespace PurityCodeQualityMetrics.Ui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PurityAnalyser _analyser;
        private readonly PurityCalculator _calculator;

        private readonly ILoggerFactory _loggerFactory =
            LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));

        private readonly IPurityReportRepo _repo = new InMemoryReportRepo();

        private int UnknownIndex = 0;

        private readonly IDictionary<Key, CheckBox> _shortcuts = new Dictionary<Key, CheckBox>();
        private readonly IDictionary<CheckBox, PurityViolation> _violationsMap = new Dictionary<CheckBox, PurityViolation>();

        private List<MethodDependency> Unknowns = new ();

        public MainWindow()
        {
            _analyser = new PurityAnalyser(_loggerFactory.CreateLogger<PurityAnalyser>());
            _calculator = new PurityCalculator(_loggerFactory.CreateLogger<PurityCalculator>());
            InitializeComponent();
            LoadUnknown();

            _shortcuts[Key.D1] = CbThrowsException;
            _shortcuts[Key.D2] = CbModifiesLocal;
            _shortcuts[Key.D3] = CbReadLocal;
            _shortcuts[Key.D4] = CbModifiesGlobal;
            _shortcuts[Key.D5] = CbReadsGlobal;
            _shortcuts[Key.D6] = CbModifiesParameters;

            _violationsMap[CbThrowsException] = PurityViolation.ThrowsException;
            _violationsMap[CbModifiesLocal] = PurityViolation.ModifiesLocalState;
            _violationsMap[CbReadLocal] = PurityViolation.ReadsLocalState;
            _violationsMap[CbModifiesGlobal] = PurityViolation.ModifiesGlobalState;
            _violationsMap[CbReadsGlobal] = PurityViolation.ReadsGlobalState;
            _violationsMap[CbModifiesParameters] = PurityViolation.ModifiesParameter;
        }

        private async void LoadProject_Click(object sender, RoutedEventArgs e)
        {
            Title = "Compiling project...";
            var path = ProjectInput.Text;
            var reports = await _analyser.GeneratePurityReports(path);
            _repo.AddRange(reports);

            Title = "Done!";
            LoadUnknown();
        }

        private void LoadUnknown()
        {
            Unknowns = _repo.GetAllReports().GetAllUnkownMethods();

            UnkownMethodText.Text = $"Unknown methods: {Unknowns.Count}";

            if (UnknownIndex >= Unknowns.Count)
                UnknownIndex = Unknowns.Count - 1;
            else if (UnknownIndex < 0)
                UnknownIndex = 0;

            if (Unknowns.Any())
                UnknownMethodName.Text = $"[{UnknownIndex}]: {Unknowns[UnknownIndex].FullName}";
        }

        public void Save()
        {
            if(!Unknowns.Any()) return;
            
            var current = Unknowns[UnknownIndex];
            var report = new PurityReport
            {
                ReturnType = current.ReturnType,
                Dependencies = new List<MethodDependency>(),
                Name = current.Name,
                Namespace = current.Namespace,
                FullName = current.Namespace + "." + current.Name,
                MethodType = MethodType.Method,
                ParameterTypes = current.ParameterTypes,
                IsMarkedByHand = true,
                ReturnValueIsFresh = false,
                Violations = GetViolationsFromUi()
            };
            
            _repo.AddRange(new []{report});
            Unknowns.RemoveAt(UnknownIndex);
            if (Unknowns.Any())
                UnknownMethodName.Text = $"[{UnknownIndex}]: {Unknowns[UnknownIndex].FullName}";
        }

        public List<PurityViolation> GetViolationsFromUi()
        {
            return _violationsMap.Keys.Where(x => x.IsChecked ?? false).Select(x => _violationsMap[x]).ToList();
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (_shortcuts.TryGetValue(e.Key, out var cb))
            {
                cb.IsChecked = !cb.IsChecked;
            }

            switch (e.Key)
            {
                case Key.Enter:
                    Save();
                    break;
                case Key.Right:
                    UnknownIndex++;
                    LoadUnknown();
                    break;
                case Key.Left:
                    UnknownIndex--;
                    LoadUnknown();
                    break;
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            UnknownIndex--;
            LoadUnknown();
        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {
            UnknownIndex++;
            LoadUnknown();
        }
    }
}