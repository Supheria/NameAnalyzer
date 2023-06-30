using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using NameAnalyzer.Converters;
using Parser.Data;
using Parser.Data.TokenTypes;
using Parser.Helper;
using Utilities;
using WinUI3Utilities;

namespace NameAnalyzer;

public record NameItem(int Level, string Name, bool IsError);

public record NameInfoLabel(NameInfoLabelType Type);

public enum NameInfoLabelType
{
    None,
    Type,
    PropertyName,
    Value,
    SourceFile,
    Warning
}

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _messageDialogText = "";

    public NameInfoLabel[] NameInfoLabelsSource => NameInfoBlocks.Select(block => new NameInfoLabel(block.Key)).ToArray();

    public Dictionary<NameInfoLabelType, HashSet<string>> NameInfoBlocks { get; set; } = new();

    public InlineCollection NameInfoBlockSource { get; set; } = null!;

    /// <summary>
    /// 最大的Level值，即<see cref="NumberBox"/>LevelPicker最大值
    /// </summary>
    public int MaxLevel => LevelMap.Count is 0 ? 0 : LevelMap.Count - 1;

    public string WarningMessage
    {
        get => _warningMessage;
        set
        {
            _warningMessage = value;
            OnPropertyChanged();
        }
    }
    private string _warningMessage = "";

    /// <summary>
    /// 仅用来提示其他属性更新，自身不绑定
    /// </summary>
    public Dictionary<uint, Dictionary<string, List<TokenInfo>>> LevelMap
    {
        get => _levelMap;
        set
        {
            _levelMap = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MaxLevel));

            // 并且刷新NamePickerSource和PropertyNamesSource
            SelectedLevel = 0;
            SelectedNameIndex = 0;
            OnPropertyChanged(nameof(SelectionShowText));
            StringBuilder warningMessage = new();
            foreach (var infos in value.Values.SelectMany(nameList => nameList.Values))
            {
                if (!CheckWarning(infos, out var warnings))
                    continue;
                foreach (var warning in warnings)
                {
                    _ = warningMessage.AppendLine(warning).AppendLine();
                }
            }
            WarningMessage = warningMessage.ToString();
        }
    }
    private Dictionary<uint, Dictionary<string, List<TokenInfo>>> _levelMap = new();

    /// <summary>
    /// <see cref="NumberBox"/>LevelPicker正选择的项
    /// </summary>
    public int SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            // 不判断是否相等就赋值，是为了不论何时都刷新Source
            _selectedLevel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NameListSource));
            // 并且刷新PropertyNamesSource
            SelectedNameIndex = 0;
        }
    }
    private int _selectedLevel;

    public int SelectedNameIndex
    {
        get => _selectedNameIndex;
        set
        {
            _selectedNameIndex = value;
            OnPropertyChanged();

            var selectedName = SelectedName;
            if (!LevelMap.TryGetValue((uint)SelectedName.Level, out var names) ||
                !names.TryGetValue(selectedName.Name, out var infos))
                SetNameInfoBlock(new());
            else
                SetNameInfoBlock(infos);

            OnPropertyChanged(nameof(SelectedName));
            OnPropertyChanged(nameof(SelectionShowText));
        }
    }
    private int _selectedNameIndex;

    private void SetNameInfoBlock(List<TokenInfo> infos)
    {
        NameInfoBlocks = new();
        var types = new HashSet<string>();
        var sources = new HashSet<string>();
        var properties = new HashSet<string>();
        var values = new HashSet<string>();
        foreach (var info in infos)
        {
            _ = types.Add(info.Token.GetType().Name);
            _ = sources.Add(info.FilePath);

            switch (info.Token)
            {
                case Scope scope:
                    foreach (var token in scope.Property)
                        _ = properties.Add(token.Name.Text);
                    break;
                case TaggedValue or ValueArray or TagArray:
                    _ = values.Add(info.Token.ValueToString());
                    break;
                default:
                    break;
            }
        }

        if (CheckWarning(infos, out var warnings))
            NameInfoBlocks[NameInfoLabelType.Warning] = warnings;
        if (sources.Count > 0)
            NameInfoBlocks[NameInfoLabelType.SourceFile] = sources;
        if (types.Count > 0)
            NameInfoBlocks[NameInfoLabelType.Type] = types;
        if (properties.Count > 0)
            NameInfoBlocks[NameInfoLabelType.PropertyName] = properties;
        if (values.Count > 0)
            NameInfoBlocks[NameInfoLabelType.Value] = values;
        NameInfoBlocks[NameInfoLabelType.None] = new();

        OnPropertyChanged(nameof(NameInfoLabelsSource));
        SetNameInfoBlockSource();
    }

    public NameItem SelectedName => SelectedNameIndex is not -1 && SelectedNameIndex < NameListSource.Count
        ? NameListSource[SelectedNameIndex]
        : new(-1, "", false);

    public NameInfoLabel? SelectedNameInfoLabel
    {
        get => _selectedNameInfoLabel;
        set
        {
            if (value is null)
                return;
            _selectedNameInfoLabel = value;
            OnPropertyChanged();
            SetNameInfoBlockSource();
            OnPropertyChanged(nameof(SelectionShowText));
        }
    }
    private NameInfoLabel _selectedNameInfoLabel = new(NameInfoLabelType.None);

    public string SelectionShowText
    {
        get
        {
            if (SelectedName.Level is -1)
                return "";
            var tooLong = SelectedName.Name.Length > 10;
            return
                $"{SelectedName.Name[..(tooLong ? 10 : SelectedName.Name.Length)]}{(tooLong ? "..." : "")}({SelectedName.Level})\n\n{NameInfoLabelTypeToStringConverter.Convert(GetRealNameInfoLabelType())}";
        }
    }

    private void SetNameInfoBlockSource()
    {
        NameInfoBlockSource.Clear();

        var type = GetRealNameInfoLabelType();
        if (type is NameInfoLabelType.None)
            return;
        foreach (var run in NameInfoBlocks[type].Select(text =>
                     new Run { Text = text }))
        {
            if (type is NameInfoLabelType.SourceFile)
            {
                var hyperlink = new Hyperlink { Inlines = { run } };
                hyperlink.Click += (s, _) => PathTool.OpenFileOrFolderInShell(s.Inlines[0].To<Run>().Text);
                NameInfoBlockSource.Add(hyperlink);
                NameInfoBlockSource.Add(new LineBreak());
                NameInfoBlockSource.Add(new LineBreak());
            }
            else
            {
                NameInfoBlockSource.Add(run);
                NameInfoBlockSource.Add(new LineBreak());
                NameInfoBlockSource.Add(new LineBreak());
            }
        }
    }

    private NameInfoLabelType GetRealNameInfoLabelType()
    {
        var type = SelectedNameInfoLabel!.Type;
        if (!NameInfoBlocks.ContainsKey(type))
        {
            type = type switch
            {
                NameInfoLabelType.Value => NameInfoLabelType.PropertyName,
                NameInfoLabelType.PropertyName => NameInfoLabelType.Value,
                _ => type
            };
        }

        return !NameInfoBlocks.ContainsKey(type) ? NameInfoLabelType.None : type;
    }

    private const int MaxItemNumber = 150;

    public List<NameItem> NameListSource
    {
        get
        {
            if (!LevelMap.TryGetValue((uint)SelectedLevel, out var nameList))
                return new();

            var namesCorrectness = new Dictionary<string, bool>();
            uint itemNumber = 1;
            foreach (var name in nameList)
            {
                if (itemNumber > MaxItemNumber)
                {
                    namesCorrectness["..."] = false;
                    break;
                }
                namesCorrectness[name.Key] = CheckWarning(name.Value, out _);
                itemNumber++;
            }
            return namesCorrectness.Select(name => new NameItem(SelectedLevel, name.Key, name.Value)).ToList();
        }
    }

    private static bool CheckWarning(List<TokenInfo> infos, out HashSet<string> warnings)
    {
        warnings = new();
        if (infos.Count is 0)
            return false;

        var duplicateType = false;
        var duplicateAssignment = new Dictionary<string, Dictionary<Word, List<Token>>>();
        var token = infos.First().Token;
        foreach (var info in infos)
        {
            if (info.Token.GetType() != token.GetType())
                duplicateType = true;
            if (info.Token is not (TaggedValue or ValueArray or TagArray))
                continue;
            if (!duplicateAssignment.ContainsKey(info.FilePath))
                duplicateAssignment[info.FilePath] = new();
            var nullWord = new Word();
            if (!duplicateAssignment[info.FilePath].ContainsKey(info.Token.From?.Name ?? nullWord))
                duplicateAssignment[info.FilePath][info.Token.From?.Name ?? nullWord] = new();
            duplicateAssignment[info.FilePath][info.Token.From?.Name ?? nullWord].Add(info.Token);
        }

        if (duplicateType)
            _ = warnings.Add($"{token} has different types.");

        foreach (var file in duplicateAssignment)
        {
            var fromList = file.Value.Where(pair => pair.Value.Count > 1).Select(pair => pair.Key).ToList();
            var sb = new StringBuilder();
            foreach (var word in fromList)
                _ = sb.AppendLine($"{word}");
            if (sb.Length > 0)
                _ = warnings.Add($"{token} in {file.Key} is duplicated from\n{sb}");
        }

        return warnings.Count > 0;
    }
}
