using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Dh.Launcher.TestRunner.WinUI
{
    /// <summary>
    /// WinForms mini UI để chọn và chạy các TestScenario từ Dh.Launcher.TestRunner.
    /// Không đụng gì tới launcher thật, chỉ thao tác trên root riêng.
    /// </summary>
    public sealed class MainForm : Form
    {
        private readonly ListBox _scenarioList;
        private readonly TextBox _descriptionBox;
        private readonly TextBox _baseUrlBox;
        private readonly TextBox _rootBox;
        private readonly Button _refreshButton;
        private readonly Button _runButton;
        private readonly Label _statusLabel;

        private List<Dh.Launcher.TestRunner.TestScenario> _scenarios;

        public MainForm()
        {
            Text = "Dh.Launcher TestRunner UI";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;

            var lblRoot = new Label { Left = 10, Top = 10, Width = 80, Text = "Root:" };
            _rootBox = new TextBox { Left = 90, Top = 8, Width = 500 };
            _rootBox.Text = Path.Combine(Environment.CurrentDirectory, "UITestRunnerRoot");

            var lblBase = new Label { Left = 10, Top = 40, Width = 80, Text = "BaseUrl:" };
            _baseUrlBox = new TextBox { Left = 90, Top = 38, Width = 500 };
            _baseUrlBox.Text = "http://localhost:3000";

            _refreshButton = new Button { Left = 610, Top = 7, Width = 120, Height = 24, Text = "Load scenarios" };
            _refreshButton.Click += (s, e) => LoadScenarios();

            _runButton = new Button { Left = 610, Top = 37, Width = 120, Height = 24, Text = "Run selected" };
            _runButton.Click += (s, e) => RunSelected();

            _statusLabel = new Label { Left = 10, Top = 70, Width = 840, Height = 20, Text = "Ready." };

            _scenarioList = new ListBox { Left = 10, Top = 100, Width = 400, Height = 430 };
            _scenarioList.SelectedIndexChanged += (s, e) => UpdateDescription();

            _descriptionBox = new TextBox
            {
                Left = 420,
                Top = 100,
                Width = 430,
                Height = 430,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            Controls.Add(lblRoot);
            Controls.Add(_rootBox);
            Controls.Add(lblBase);
            Controls.Add(_baseUrlBox);
            Controls.Add(_refreshButton);
            Controls.Add(_runButton);
            Controls.Add(_statusLabel);
            Controls.Add(_scenarioList);
            Controls.Add(_descriptionBox);

            Shown += (s, e) => LoadScenarios();
        }

        private void LoadScenarios()
        {
            try
            {
                var root = _rootBox.Text;
                if (string.IsNullOrWhiteSpace(root))
                {
                    root = Path.Combine(Environment.CurrentDirectory, "UITestRunnerRoot");
                    _rootBox.Text = root;
                }

                Directory.CreateDirectory(root);

                var baseUrl = _baseUrlBox.Text;
                _scenarios = Dh.Launcher.TestRunner.Program.BuildScenarios(root, baseUrl);

                _scenarioList.Items.Clear();
                foreach (var sc in _scenarios)
                {
                    _scenarioList.Items.Add(sc.Name);
                }

                _statusLabel.Text = $"Loaded {_scenarios.Count} scenarios.";
                if (_scenarios.Count > 0)
                    _scenarioList.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Load scenarios failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateDescription()
        {
            if (_scenarios == null || _scenarios.Count == 0)
            {
                _descriptionBox.Text = string.Empty;
                return;
            }

            var idx = _scenarioList.SelectedIndex;
            if (idx < 0 || idx >= _scenarios.Count)
            {
                _descriptionBox.Text = string.Empty;
                return;
            }

            var sc = _scenarios[idx];
            _descriptionBox.Text = sc.Name + Environment.NewLine +
                                   new string('-', 40) + Environment.NewLine +
                                   (sc.Description ?? "(no description)") + Environment.NewLine +
                                   Environment.NewLine +
                                   "Root: " + sc.RootPath;
        }

        private void RunSelected()
        {
            if (_scenarios == null || _scenarios.Count == 0)
            {
                MessageBox.Show(this, "Chưa load scenario.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var idx = _scenarioList.SelectedIndex;
            if (idx < 0 || idx >= _scenarios.Count)
            {
                MessageBox.Show(this, "Vui lòng chọn một scenario.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var sc = _scenarios[idx];
            try
            {
                _statusLabel.Text = "Running " + sc.Name + "...";
                _statusLabel.Refresh();

                var started = DateTime.UtcNow;
                sc.Setup?.Invoke();
                sc.Run?.Invoke();
                var elapsed = (DateTime.UtcNow - started).TotalSeconds;

                _statusLabel.Text = $"Scenario {sc.Name} OK in {elapsed:0.00}s.";
                MessageBox.Show(this, $"Scenario {sc.Name} completed in {elapsed:0.00}s.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _statusLabel.Text = "Scenario " + sc.Name + " FAILED.";
                MessageBox.Show(this, "Scenario FAILED: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
