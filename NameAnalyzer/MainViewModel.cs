using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Windows.Management.Policies;
using ABI.System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Parser.Data.TokenTypes;
using Parser.Helper;
using Utilities;
using WinUI3Utilities;
using System.Xml.Linq;

namespace NameAnalyzer;

public record NameItem(int Level, string Name, bool IsError);

public record NameInfoLabel(NameInfoLabelType Type);

public enum NameInfoLabelType
{
    None,
    Type,
    PropertyName,
    Value,
    SourceFile
}

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _messageDialogText = "";

    [ObservableProperty]
    private string _errorMessage = "";

    public NameInfoLabel[] NameInfoLabelsSource => NameInfoBlocks.Select(block => new NameInfoLabel(block.Key)).ToArray();

    public Dictionary<NameInfoLabelType, HashSet<string>> NameInfoBlocks { get; set; } = new();

    public ErrorSet ErrorSet { get; private set; } = new();

    public InlineCollection NameInfoBlockSource { get; set; } = null!;

    public NameItem SelectedName
    {
        get => _selectedName;
        set
        {
            if (value == _selectedName)
                return;
            _selectedName = value;
            OnPropertyChanged();

            NameInfoBlocks = new();
            if (!LevelMap.TryGetValue((uint)value.Level, out var names))
                return;
            if (!names.TryGetValue(value.Name, out var infos))
                return;
            if (infos.Count is not 0)
            {
                NameInfoBlocks = new()
                {
                    [NameInfoLabelType.Type] = new(),
                    [NameInfoLabelType.SourceFile] = new()
                };
                foreach (var info in infos)
                {
                    switch (info.Token)
                    {
                        case Scope scope:
                            if (!NameInfoBlocks.ContainsKey(NameInfoLabelType.PropertyName))
                                NameInfoBlocks[NameInfoLabelType.PropertyName] = new();
                            foreach (var token in scope.Property)
                                _ = NameInfoBlocks[NameInfoLabelType.PropertyName].Add(token.Name.Text);
                            break;
                        case TaggedValue or ValueArray or TagArray:
                            if (!NameInfoBlocks.ContainsKey(NameInfoLabelType.Value))
                                NameInfoBlocks[NameInfoLabelType.Value] = new();
                            _ = NameInfoBlocks[NameInfoLabelType.Value].Add(info.Token.ToString());
                            break;
                        default:
                            break;
                    }

                    _ = NameInfoBlocks[NameInfoLabelType.Type].Add(info.Token.GetType().Name);
                    _ = NameInfoBlocks[NameInfoLabelType.SourceFile].Add(info.FilePath);
                }

                if (NameInfoBlocks[NameInfoLabelType.Type].Count > 1)
                    ErrorMessage = ErrorSet.Append($"\"{infos[0].Name}\" in level {value.Level} has different types.");
            }

            OnPropertyChanged(nameof(NameInfoLabelsSource));
            if (SelectedNameInfoLabel!.Type is NameInfoLabelType.None)
                SelectedNameInfoLabel = new(NameInfoLabelType.Type);
            OnPropertyChanged(nameof(SelectedNameInfoLabel));
            SetNameInfoBlockSource();
        }
    }

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
        }
    }

    private void SetNameInfoBlockSource()
    {
        NameInfoBlockSource.Clear();
        if (!NameInfoBlocks.TryGetValue(SelectedNameInfoLabel!.Type, out var labels))
            return;
        foreach (var run in labels.Select(text =>
                     new Run { Text = text }))
        {
            if (SelectedNameInfoLabel.Type is NameInfoLabelType.SourceFile)
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

    /// <summary>
    /// 最大的Level值，即<see cref="NumberBox"/>LevelPicker最大值
    /// </summary>
    public int MaxLevel => LevelMap.Count is 0 ? 0 : LevelMap.Count - 1;

    public int MinLevel => LevelMap.Count is 0 ? 0 : -1;

    /// <summary>
    /// 仅用来提示其他属性更新，自身不绑定
    /// </summary>
    public Dictionary<uint, Dictionary<string, List<TokenInfo>>> LevelMap
    {
        get => _levelMap;
        set
        {
            if (value.Count is 0 || Equals(_levelMap, value))
                return;
            ErrorMessage = "";
            ErrorSet = new();
            _levelMap = value;
            OnPropertyChanged(nameof(MinLevel));
            OnPropertyChanged(nameof(MaxLevel));
            // 并且刷新NamePickerSource和PropertyNamesSource
            SelectedLevel = 0;
            SelectedNameIndex = 0;
        }
    }
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
            OnPropertyChanged(nameof(NamePickerSource));
            // 并且刷新PropertyNamesSource
            SelectedNameIndex = 0;
        }
    }

    /// <summary>
    /// <see cref="ComboBox"/>NamePicker正选择的项
    /// </summary>
    public int SelectedNameIndex
    {
        get => _selectedNameIndex;
        set
        {
            // 不判断是否相等就赋值，是为了不论何时都刷新Source
            _selectedNameIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PropertyNamesSource));
        }
    }

    public void SetNameInfo()
    {
        if (SelectedLevel is -1 || SelectedNameIndex is -1)
            return;
        SelectedName = NamePickerSource[SelectedNameIndex];
    }

    public void SetPropertyNameInfo(NameItem propertyNameItem)
    {
        SelectedName = propertyNameItem;
    }

    /// <summary>
    /// 仅用来调用，自身不绑定
    /// </summary>
    public Dictionary<string, List<TokenInfo>> NameDictionary => LevelMap.TryGetValue((uint)SelectedLevel, out var value) ? value : new ();

    private List<NameItem> GetNameList(int level)
    {
        if (!LevelMap.TryGetValue((uint)level, out var nameList))
            return new();
        var names = new Dictionary<string, Token?>();
        foreach (var name in nameList)
        {
            foreach (var info in name.Value)
            {
                if (names.TryGetValue(name.Key, out var token))
                {
                    if (token?.GetType() != info.Token.GetType())
                        // 为null时表示几处定义类型不一样
                        names[name.Key] = null;
                }
                else
                    _ = names[name.Key] = info.Token;
            }
        }
        return names.Select(t => new NameItem(level, t.Key, t.Value is null)).ToList();
    }

    /// <summary>
    /// 给NamePicker的<see cref="ComboBox"/>用
    /// </summary>
    public List<NameItem> NamePickerSource => GetNameList(SelectedLevel);

    /// <summary>
    /// 给显示Scope.Property的<see cref="ListView"/>用
    /// </summary>
    public List<NameItem> PropertyNamesSource
    {
        get
        {
            if (SelectedNameIndex >= NamePickerSource.Count)
                return new();

            var propertyNames = new Dictionary<string, Token?>();
            if (SelectedLevel is -1)
                return GetNameList(0);
            if (SelectedNameIndex is -1)
                return new();
            foreach (var info in NameDictionary[NamePickerSource[SelectedNameIndex].Name])
            {
                if (info.Token is not Scope scope)
                    continue;
                foreach (var property in scope.Property)
                {
                    var name = property.Name.Text;
                    if (propertyNames.TryGetValue(name, out var token))
                    {
                        if (token?.GetType() != property.GetType())
                            // 为null时表示几处定义类型不一样
                            propertyNames[name] = null;
                    }
                    else
                        _ = propertyNames[name] = property;
                }
            }

            return propertyNames.Select(t => new NameItem(0, t.Key, t.Value is null)).ToList();
        }
    }

    private int _selectedLevel;
    private int _selectedNameIndex;
    private NameItem _selectedName = new(-1, "", false);
    private NameInfoLabel _selectedNameInfoLabel = new(NameInfoLabelType.None);
    private Dictionary<uint, Dictionary<string, List<TokenInfo>>> _levelMap = new();
}
