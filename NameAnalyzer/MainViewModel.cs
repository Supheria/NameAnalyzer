using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using ABI.System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Parser.Data.TokenTypes;
using Parser.Helper;
using Utilities;
using WinUI3Utilities;

namespace NameAnalyzer;

public record NameCorrectness(string Name, bool IsError);

public record NameOnLevel(string Name, int Level);

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

    public NameOnLevel NameInfoToShow
    {
        get => _nameInfoToShow;
        set
        {
            if (_nameInfoToShow == value)
                return;
            NameInfoBlocks = new();
            if (value.Level is -1)
                return;

            _nameInfoToShow = value;
            OnPropertyChanged();

            var infos = LevelMap[(uint)_nameInfoToShow.Level][_nameInfoToShow.Name].ToArray();

            if (infos.Length is not 0)
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
                    ErrorMessage = ErrorSet.Append($"\"{infos[0].Name}\" in level {_nameInfoToShow.Level} has different types.");
            }

            OnPropertyChanged(nameof(NameInfoLabelsSource));
            if(SelectedNameInfoLabel!.Type is NameInfoLabelType.None)
                SelectedNameInfoLabel = new(NameInfoLabelType.Type);
            OnPropertyChanged(nameof(SelectedNameInfoLabel));
        }
    }

    public InlineCollection NameInfoBlockSource { get; set; } = null!;

    public NameInfoLabel? SelectedNameInfoLabel
    {
        get => _selectedNameInfoLabel;
        set
        {
            NameInfoBlockSource.Clear();

            if (value is null || value.Type is NameInfoLabelType.None || !NameInfoBlocks.ContainsKey(value.Type))
                return;

            _selectedNameInfoLabel = value;

            foreach (var run in NameInfoBlocks[_selectedNameInfoLabel.Type].Select(text =>
                         new Run { Text = text }))
            {
                if (_selectedNameInfoLabel.Type is NameInfoLabelType.SourceFile)
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
        NameInfoToShow = new(NamePickerSource[SelectedNameIndex], SelectedLevel);
    }

    public void SetPropertyNameInfo(string propertyName)
    {
        NameInfoToShow = new(propertyName, SelectedLevel + 1);
    }

    /// <summary>
    /// 仅用来调用，自身不绑定
    /// </summary>
    public Dictionary<string, List<TokenInfo>> NameDictionary => LevelMap.TryGetValue((uint)SelectedLevel, out var value) ? value : new ();


    /// <summary>
    /// 给NamePicker的<see cref="ComboBox"/>用
    /// </summary>
    public List<string> NamePickerSource => NameDictionary.Keys.ToList();

    /// <summary>
    /// 给显示Scope.Property的<see cref="ListView"/>用
    /// </summary>
    public NameCorrectness[] PropertyNamesSource
    {
        get
        {
            if (SelectedNameIndex >= NamePickerSource.Count)
                return Array.Empty<NameCorrectness>();

            var propertyNames = new Dictionary<string, Token?>();
            if (SelectedLevel is -1)
            {
                foreach (var name in LevelMap[0])
                {
                    foreach (var info in name.Value)
                    {
                        if (propertyNames.TryGetValue(name.Key, out var token))
                        {
                            if (token?.GetType() != info.Token.GetType())
                                // 为null时表示几处定义类型不一样
                                propertyNames[name.Key] = null;
                        }
                        else
                            _ = propertyNames[name.Key] = info.Token;
                    }
                }
            }
            else
            {
                if (SelectedNameIndex is -1)
                    return Array.Empty<NameCorrectness>();
                foreach (var info in NameDictionary[NamePickerSource[SelectedNameIndex]])
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
            }

            return propertyNames.Select(t => new NameCorrectness(t.Key, t.Value is null)).ToArray();
        }
    }

    private NameOnLevel _nameInfoToShow = new("", -1);
    private int _selectedLevel;
    private int _selectedNameIndex;
    private NameInfoLabel _selectedNameInfoLabel = new(NameInfoLabelType.None);
    private Dictionary<uint, Dictionary<string, List<TokenInfo>>> _levelMap = new();
}
