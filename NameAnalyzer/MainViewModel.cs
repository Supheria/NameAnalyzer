using System;
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
    Warning,
    SourceFile,
    Type,
    From,
    PropertyName,
    Value,
}

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _messageDialogText = "";

    [ObservableProperty] private int _itemNumber;

    public NameInfoLabel[] NameInfoLabelsSource => NameInfoBlocks.Select(block => new NameInfoLabel(block.Key)).ToArray();

    public Dictionary<NameInfoLabelType, HashSet<string>> NameInfoBlocks { get; set; } = new();

    public InlineCollection NameInfoBlockSource { get; set; } = null!;

    /// <summary>
    /// 最大的Level值，即<see cref="NumberBox"/>LevelPicker最大值
    /// </summary>
    public int MaxLevel => LevelMap.Count is 0 ? 0 : LevelMap.Count - 1;
    private const string AllTypes = "all";
    private const int MaxItemNumber = 300;

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
            SelectedTypeIndex = 0;
            OnlyShowWarnedItems = false;
            OnPropertyChanged(nameof(SelectionShowText));
            StringBuilder warningMessage = new();
            foreach (var infos in value.Values.SelectMany(nameList => nameList.Values))
            {
                if (!CheckWarning(infos, out var warnings))
                    continue;
                _ = warningMessage.AppendLine(warnings).AppendLine();
            }
            WarningMessage = warningMessage.ToString();
        }
    }
    private Dictionary<uint, Dictionary<string, List<TokenInfo>>> _levelMap = new();

    public bool OnlyShowWarnedItems
    {
        get => _onlyShowWarnedItems;
        set
        {
            _onlyShowWarnedItems = value;
            OnPropertyChanged();
            SetNameListSource();
            SetTypeListSource();
            SelectedNameIndex = 0;
            SelectedTypeIndex = 0;
        }
    }

    private bool _onlyShowWarnedItems = false;

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
            SetNameListSource();
            SetTypeListSource();
            SelectedNameIndex = 0;
            SelectedTypeIndex = 0;
        }
    }
    private int _selectedLevel;

    public List<string> TypeListSource { get; set; } = new();

    private void SetTypeListSource()
    {
        TypeListSource = new();
        if (!LevelMap.TryGetValue((uint)SelectedLevel, out var nameList))
        {
            OnPropertyChanged(nameof(TypeListSource));
            return;
        }

        var types = new HashSet<string>();
        if (OnlyShowWarnedItems)
            foreach (var info in nameList.Values.Where(infos => CheckWarning(infos, out _)).SelectMany(infos => infos))
                _ = types.Add(info.Token.GetType().Name);
        else
            foreach (var info in nameList.Values.SelectMany(infos => infos))
                _ = types.Add(info.Token.GetType().Name);

        if (types.Count > 0)
            TypeListSource.Add(AllTypes);
        TypeListSource.AddRange(types);

        OnPropertyChanged(nameof(TypeListSource));
    }

    public int SelectedTypeIndex
    {
        get => _selectedTypeIndex;
        set
        {
            // 不判断是否相等就赋值，是为了不论何时都刷新Source
            _selectedTypeIndex = value;
            OnPropertyChanged();
            SetNameListSource();
            SelectedNameIndex = 0;
        }
    }
    private int _selectedTypeIndex;

    private string SelectedType => SelectedTypeIndex is not -1 && SelectedTypeIndex < TypeListSource.Count
        ? TypeListSource[SelectedTypeIndex]
        : AllTypes;


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
        var fromNames = new HashSet<string>();
        var fromPositions = new HashSet<TextPosition>();

        var pickedInfoListByType = infos;
        if (SelectedType is not AllTypes)
            pickedInfoListByType = infos.Where(info => info.Token.GetType().Name == SelectedType).ToList();

        foreach (var info in pickedInfoListByType)
        {
            var token = info.Token;
            _ = types.Add(token.GetType().Name);
            _ = sources.Add(info.FilePath);

            switch (token)
            {
                case Scope scope:
                    foreach (var t in scope.Property)
                        _ = properties.Add(t.Name.Text);
                    break;
                case TaggedValue or ValueArray or TagArray:
                    _ = values.Add(token.ValueToString());
                    break;
                default:
                    break;
            }

            _ = fromNames.Add(token.From?.ToString() ?? "file root");
            if (token.From is null)
                continue;
            _ = fromPositions.Add(token.From.Name.Position);
        }

        if (CheckWarning(infos, out var warning))
            NameInfoBlocks[NameInfoLabelType.Warning] = new() { warning };
        if (sources.Count > 0)
            NameInfoBlocks[NameInfoLabelType.SourceFile] = sources;
        if (types.Count > 0)
            NameInfoBlocks[NameInfoLabelType.Type] = types;
        if (fromNames.Count > 0)
        {
            NameInfoBlocks[NameInfoLabelType.From] =
                new() { new StringBuilder().AppendJoin('\n', fromNames).ToString() };
            var sortedFromPositions = fromPositions.ToList();
            sortedFromPositions.Sort();
            var sb = new StringBuilder();
            foreach (var position in sortedFromPositions)
                _ = sb.AppendLine(position.ToString());
            _ = NameInfoBlocks[NameInfoLabelType.From].Add(sb.ToString());
        }
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

    public List<NameItem> NameListSource { get; set; } = new();

    private void SetNameListSource()
    {
        NameListSource = new();
        if (!LevelMap.TryGetValue((uint)SelectedLevel, out var nameList))
        {
            OnPropertyChanged(nameof(NameListSource));
            return;
        }

        var pickedNamesByType = nameList;
        if (SelectedType is not AllTypes)
        {
            pickedNamesByType = new();
            foreach (var name in nameList.Where(name =>
                         name.Value.Any(info => info.Token.GetType().Name == SelectedType)))
                pickedNamesByType[name.Key] = name.Value;
        }
        var namesCorrectness = new Dictionary<string, bool>();
        foreach (var name in pickedNamesByType)
            namesCorrectness[name.Key] = CheckWarning(name.Value, out _);
        if (OnlyShowWarnedItems)
        {
            NameListSource.AddRange(from item in namesCorrectness where item.Value select new NameItem(SelectedLevel, item.Key, item.Value));
            ItemNumber = NameListSource.Count;
        }
        else
        {
            var itemNumber = 0;
            foreach (var item in namesCorrectness)
            {
                if (++itemNumber > MaxItemNumber)
                {
                    NameListSource.Add(new(SelectedLevel, "...", false));
                    break;
                }

                NameListSource.Add(new(SelectedLevel, item.Key, item.Value));
            }

            ItemNumber = namesCorrectness.Count;
        }
        OnPropertyChanged(nameof(NameListSource));
    }

    private static bool CheckWarning(List<TokenInfo> sameNameInfos, out string warning)
    {
        warning = "";
        if (sameNameInfos.Count is 0)
            return false;

        var multiTypes = new Dictionary<string, Dictionary<Word, Token?>>();
        var multiAssignments = new Dictionary<string, Dictionary<Word, List<Token>>>();

        var tokenToString = sameNameInfos.First().Token.ToString();
        foreach (var info in sameNameInfos)
        {
            if (info.Token.ToString() != tokenToString)
            {
                warning = $"inner error: name of {tokenToString} is inconsistent in CheckWarning.";
                return true;
            }
            var token = info.Token;
            var from = token.From?.Name ?? new Word();
            var path = info.FilePath;

            if (!multiTypes.ContainsKey(path))
                multiTypes[path] = new();
            if (!multiTypes[path].ContainsKey(from))
                multiTypes[path][from] = token;
            if (token.GetType() != multiTypes[path][from]?.GetType())
                multiTypes[path][from] = null;

            if (token is not (TaggedValue or ValueArray or TagArray))
                continue;
            if (!multiAssignments.ContainsKey(path))
                multiAssignments[path] = new();
            if (!multiAssignments[path].ContainsKey(from))
                multiAssignments[path][from] = new();
            multiAssignments[path][from].Add(token);
        }

        var summery = new StringBuilder();
        var warningSb = new StringBuilder();
        if (ComposeWarning(multiTypes, t => t is null, out var type))
        {
            summery.Append("multi-types ");
            _ = warningSb.Append($"\tMulti-Types\n{type}");
        }

        if (ComposeWarning(multiAssignments, list => list.Count > 1, out var assign))
        {
            summery.Append("multi-assignments ");
            _ = warningSb.Append($"\tMulti-Assignments\n{assign}");
        }
        if (warningSb.Length > 0)
            warning = $"{tokenToString} : {summery}\n{warningSb}";

        return warning.Length > 0;
    }

    private static bool ComposeWarning<T>(Dictionary<string, Dictionary<Word, T>> multiAssemble, Func<T, bool> warningFilter,
        out string part)
    {
        part = "";
        var partSb = new StringBuilder();
        foreach (var file in multiAssemble)
        {
            var fromList = file.Value.Where(pair => warningFilter(pair.Value)).Select(pair => pair.Key).ToList();
            var sb = new StringBuilder();
            foreach (var from in fromList)
                _ = sb.AppendLine($"\t\t\t{from.ToString()}");
            if (sb.Length > 0)
                _ = partSb.Append($"\t\t{file.Key}\n\t\t\tFROM\n{sb}");
        }
        if (partSb.Length > 0)
            part = $"\t\tIN\n{partSb}";
        return part.Length > 0;
    }
}
