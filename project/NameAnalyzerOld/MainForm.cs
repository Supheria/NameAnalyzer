using Parser;
using Parser.data;
using System.Text;

namespace NameAnalyzer
{
    public partial class MainForm : Form
    {
        private Analysis Analyzer { get; set; } = new();

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainMenu_Parse_Folder_Click(object sender, EventArgs e)
        {
            using var folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == DialogResult.Cancel)
                return;
            Analyzer = new(folderBrowser.SelectedPath);
            UpdateComponent();
        }

        private void MainMenu_Parse_SingleFile_Click(object sender, EventArgs e)
        {
            using var openFile = new OpenFileDialog();
            if (openFile.ShowDialog() == DialogResult.Cancel)
                return;
            Analyzer = new(openFile.FileName);
            UpdateComponent();
        }

        private void UpdateComponent()
        {
            LevelPicker.Maximum = Analyzer.LevelMap.Count - 1;
            LevelPicker.Value = 0;
            SetLevel(0);
        }

        private void SetLevel(uint level)
        {
            if (!Analyzer.LevelMap.ContainsKey(level)) { return; }
            NamePicker.Items.Clear();
            foreach (var name in Analyzer.LevelMap[level].Keys)
            {
                NamePicker.Items.Add(name);
            }
            NamePicker.SelectedIndex = 0;
            SetName(Analyzer.LevelMap[level][NamePicker.SelectedItem.ToString()]);
        }

        private void SetName(List<TokenAPI> tokens)
        {
            HashSet<string> subs = new();
            foreach (var token in tokens)
            {
                if (token is ScopeAPI scope)
                {
                    foreach (var subToken in scope.Property)
                    {
                        subs.Add(subToken.Name);
                    }
                }
                else if (token is TaggedValueAPI tagged)
                {
                    var sb = new StringBuilder();
                    sb.Append($"{tagged.Tag}(...) ");
                    foreach (var value in tagged.Value)
                    {
                        sb.Append($"{value} ");
                    }

                    subs.Add(sb.ToString());
                }
                else if (token is ValueArrayAPI varr)
                {
                    foreach (var value in varr.Value)
                    {
                        var sb = new StringBuilder();
                        foreach (var element in value)
                        {
                            sb.Append($"{element} ");
                        }
                        subs.Add(sb.ToString());
                    }
                }
                else if (token is TagArrayAPI tarr)
                {
                    foreach (var value in tarr.Value)
                    {
                        var sb = new StringBuilder();
                        foreach (var pair in value)
                        {
                            sb.Append($"{pair.Key}(...) ");
                            foreach (var element in pair.Value)
                            {
                                sb.Append($"{element} ");
                            }
                        }
                        subs.Add(sb.ToString());
                    }
                }
            }
            SubList.Items.Clear();
            foreach (var sub in subs)
            {
                SubList.Items.Add(sub);
            }
        }

        private void LevelPicker_ValueChanged(object sender, EventArgs e)
        {
            SetLevel((uint)LevelPicker.Value);
        }

        private void NamePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetName(Analyzer.LevelMap[(uint)LevelPicker.Value][NamePicker.SelectedItem.ToString()]);
        }
    }
}
