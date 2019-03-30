using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace OpusMagnumCoder
{
    public partial class Form1 : Form
    {
        private readonly CodeParser _codeParser = new CodeParser();
        private readonly List<(string, RichTextBox)> _macros;
        private Solution _solution;
        private readonly SolutionParser _solutionParser = new SolutionParser();
        private List<(RichTextBox, Part)> _textMappings;
        private Panel _dragging;
        private Panel _placeHolder;
        private Point _mouseDown;

        public Form1()
        {
            InitializeComponent();
            _macros = new List<(string, RichTextBox)>();
            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            exportToolStripMenuItem.Enabled = false;
            macroToolStripMenuItem.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void loadSolutionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fileDialog = new OpenFileDialog();
            Dictionary<string, string> code = null;
            _macros.Clear();
            if (fileDialog.ShowDialog() != DialogResult.OK) return;
            using (var stream = File.Open(fileDialog.FileName, FileMode.Open))
            {
                var ext = Path.GetExtension(fileDialog.FileName);
                switch (ext)
                {
                    case ".solution":
                        _solution = _solutionParser.Parse(stream);
                        break;
                    case ".solutionx":
                        var load = _solutionParser.ParseWithCode(stream);
                        _solution = load.Item1;
                        code = load.Item2;
                        break;
                    default:
                        throw new Exception("Unknown file type");
                }
            }

            panel1.Controls.Clear();
            _textMappings = new List<(RichTextBox, Part)>();
            foreach (var part in _solution.Parts.Where(p => p.Name.StartsWith("arm") || p.Name == "piston")
                .OrderBy(p => p.Number))
            {
                var (groupBox, textBox) = AddPanel();
                groupBox.Text = "Arm " + (part.Number + 1);
                if (code?["arm" + part.Number] != null)
                {
                    textBox.Text = code["arm" + part.Number];
                    code.Remove("arm" + part.Number);
                }
                else
                {
                    textBox.Text = _codeParser.ToCode(part);
                }

                _textMappings.Add((textBox, part));
            }

            if (code != null)
            {
                foreach (var m in code)
                {
                    CreateMacro(m.Key, m.Value);
                }
            }

            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            exportToolStripMenuItem.Enabled = true;
            macroToolStripMenuItem.Enabled = true;
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fileDialog = new SaveFileDialog {FileName = _solution.PuzzleName + "-generated.solutionx"};
            if (fileDialog.ShowDialog() != DialogResult.OK) return;
            using (var stream = File.Open(fileDialog.FileName, FileMode.OpenOrCreate))
            {
                _solutionParser.WriteWithCode(_solution,
                    _textMappings.Select(m => ("arm" + m.Item2.Number, m.Item1.Text))
                        .Concat(_macros.Select(m => (m.Item1, m.Item2.Text))).ToList(), stream);
            }
        }

        private void addNewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new TextDialog("macro" + (_macros.Count + 1), "Macro name", "Create");
            if (dialog.ShowDialog() != DialogResult.OK) return;
            CreateMacro(dialog.UserInput, "");
        }

        private void CreateMacro(string name, string code)
        {
            var (groupBox, textBox) = AddPanel();
            groupBox.Text = name;
            textBox.Text = code;
            groupBox.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var dialog = new TextDialog(groupBox.Text, "Macro name", "Update");
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    _macros.Remove((groupBox.Text, textBox));
                    groupBox.Text = dialog.UserInput;
                    _macros.Add((groupBox.Text, textBox));
                }
            };
            _macros.Add((groupBox.Text, textBox));
        }

        private (GroupBox, RichTextBox) AddPanel()
        {
            var textBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true
            };
            var groupBox = new GroupBox
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            var panel = new Panel
            {
                Dock = DockStyle.Left,
                Padding = new Padding(5)
            };
            groupBox.MouseDown += (sender, e) => _mouseDown = e.Location;
            groupBox.MouseMove += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    panel.Left += e.X - _mouseDown.X;
                    panel.Top += e.Y - _mouseDown.Y;
                    int index;
                    if (_dragging == null)
                    {
                        _dragging = panel;
                        index = panel1.Controls.GetChildIndex(_dragging);
                        _placeHolder = new Panel
                        {
                            Width = _dragging.Width,
                            Height = _dragging.Height,
                            Dock = DockStyle.Left
                        };
                        _dragging.Dock = DockStyle.None;
                        panel1.Controls.Add(_placeHolder);
                    }
                    else
                    {
                        _dragging.SendToBack();
                        Control replace = panel1.GetChildAtPoint(new Point(_dragging.Left + e.X, _dragging.Top + e.Y));

                        if (replace != null)
                        {
                            index = panel1.Controls.GetChildIndex(replace);
                        }
                        else
                        {
                            index = panel1.Controls.GetChildIndex(_placeHolder);
                        }
                    }

                    panel1.Controls.SetChildIndex(_placeHolder, index);
                    _dragging.BringToFront();
                }
            };
            groupBox.MouseUp += (sender, e) =>
            {
                if (_dragging != null)
                {
                    _dragging.SendToBack();
                    Control replace = panel1.GetChildAtPoint(new Point(_dragging.Left + e.X, _dragging.Top + e.Y));
                    if (replace != null)
                    {
                        panel1.Controls.SetChildIndex(_dragging, panel1.Controls.GetChildIndex(replace));
                    }

                    _dragging.Dock = DockStyle.Left;
                    panel1.Controls.Remove(_placeHolder);
                    _dragging = null;
                    _placeHolder = null;
                }
            };
            groupBox.Controls.Add(textBox);
            panel.Controls.Add(groupBox);
            panel1.Controls.Add(panel);
            panel.BringToFront();
            return (groupBox, textBox);
        }

        private void toOriginalPuzzleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Export();
        }

        private void Export()
        {
            _codeParser.Clear();
            _macros.ForEach(m => _codeParser.AddMacro(m.Item1, m.Item2.Text));
            var threads = new List<Thread>();
            foreach (var mapping in _textMappings)
            {
                var t = new Thread(() => _codeParser.FillFromCode(mapping.Item2, mapping.Item1.Text));
                threads.Add(t);
                t.Start();
            }

            threads.ForEach(t => t.Join());
            var fileDialog = new SaveFileDialog {FileName = _solution.PuzzleName + "-generated.solution"};
            if (fileDialog.ShowDialog() != DialogResult.OK) return;
            using (var stream = File.Open(fileDialog.FileName, FileMode.OpenOrCreate))
            {
                _solutionParser.Write(_solution, stream);
            }
        }

        private void toAnotherPuzzleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new TextDialog(_solution.PuzzleName, "Puzzle name", "Confirm");
            if (dialog.ShowDialog() != DialogResult.OK) return;
            _solution.PuzzleName = dialog.UserInput;
            Export();
        }
    }
}