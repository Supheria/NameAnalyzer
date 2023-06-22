using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Parser.Data.TokenTypes;
using Parser.Helper;
using Utilities;
using WinUI3Utilities;

namespace NameAnalyzer;

public record SubItem(string Name, bool IsError);

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _messageDialogText = "";

    private (int Level, string Name) _infoShowing = (-1, "");

    public (int Level, string Name) InfoShowing
    {
        get => _infoShowing;
        set
        {
            if (_infoShowing == value)
                return;
            if (value.Level is not -1)
            {
                _infoShowing = value;

                OnPropertyChanged();
                var infos = LevelMap[(uint)_infoShowing.Level][_infoShowing.Name].ToArray();

                if (infos.Length is 0)
                    return;
                var probableType = new HashSet<string>();
                var propertyNames = new HashSet<string>();
                var values = new HashSet<string>();
                var sourceFiles = new HashSet<string>();
                foreach (var info in infos)
                {
                    switch (info.Token)
                    {
                        case Scope scope:
                            foreach (var token in scope.Property)
                                _ = propertyNames.Add(token.Name.Text);
                            break;
                        case TaggedValue or ValueArray or TagArray:
                            _ = values.Add(info.Token.ToString());
                            break;
                        default:
                            break;
                    }

                    _ = probableType.Add(info.Token.GetType().Name);
                    _ = sourceFiles.Add(info.FilePath);
                }

                if (probableType.Count > 1)
                {
                    // TODO
                    ErrorMessage =
                        ErrorSet.Append($"\"{infos[0].Name}\" in level {_infoShowing.Level} has different types.");
                    OnPropertyChanged(nameof(ErrorMessageVisibility));
                }

                if (probableType.Count > 0)
                    NameInfoType = probableType.Aggregate("", (current, s) => current + s + " ");
                if (propertyNames.Count > 0)
                    NameInfoPropertyNames = propertyNames.Aggregate("", (current, s) => current + s + "\n");
                if (values.Count > 0)
                    NameInfoValue = values.Aggregate("", (current, s) => current + s + "\n");
                if (sourceFiles.Count > 0)
                {
                    NameInfoSourceFile.Clear();
                    foreach (var hyperlink in sourceFiles.Select(sourceFile => new Hyperlink { Inlines = { new Run { Text = sourceFile } } }))
                    {
                        hyperlink.Click += (sender, _) => PathTool.OpenFileOrFolderInShell(sender.Inlines[0].To<Run>().Text);
                        NameInfoSourceFile.Add(hyperlink);
                        NameInfoSourceFile.Add(new LineBreak());
                    }
                }
            }
            else
            {
                NameInfoType = NameInfoPropertyNames = NameInfoValue = "";
                NameInfoSourceFile.Clear();
            }
            OnPropertyChanged(nameof(NameInfoVisibility));
        }
    }

    [ObservableProperty] private string _nameInfoType = "";
    [ObservableProperty] private string _nameInfoPropertyNames = "";
    [ObservableProperty] private string _nameInfoValue = "";
    public InlineCollection NameInfoSourceFile { get; set; } = null!;
    public Visibility NameInfoVisibility => NameInfoType + NameInfoPropertyNames + NameInfoValue is "" && NameInfoSourceFile.Count is 0 ? Visibility.Collapsed : Visibility.Visible;

    public Visibility ParseExceptionVisibility => MessageDialogText is "" ? Visibility.Collapsed : Visibility.Visible;

    [ObservableProperty]
    private string _errorMessage = "";

    public bool ErrorMessageVisibility => ErrorMessage is not "";

    /// <summary>
    /// 最大的Level值，即<see cref="NumberBox"/>LevelPicker最大值
    /// </summary>
    public int MaxLevel => LevelMap.Count - 1;

    /// <summary>
    /// 仅用来提示其他属性更新，自身不绑定
    /// </summary>
    public Dictionary<uint, Dictionary<string, List<TokenInfo>>>/*todo Scope*/ LevelMap
    {
        get => _levelMap;
        set
        {
            if (Equals(_levelMap, value))
                return;
            ErrorMessage = "";
            ErrorSet = new();
            _levelMap = value;
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
        if (SelectedNameIndex is -1)
            return;
        InfoShowing = (SelectedLevel, NamePickerSource[SelectedNameIndex]);
    }

    public void SetPropertyNameInfo(string propertyName)
    {
        InfoShowing = (SelectedLevel + 1, propertyName);
    }

    /// <summary>
    /// 仅用来调用，自身不绑定
    /// </summary>
    public Dictionary<string, List<TokenInfo>> NameDictionary => LevelMap[(uint)SelectedLevel];

    /// <summary>
    /// 给NamePicker的<see cref="ComboBox"/>用
    /// </summary>
    public List<string> NamePickerSource => NameDictionary.Keys.ToList();

    /// <summary>
    /// 给显示Scope.Property的<see cref="ListView"/>用
    /// </summary>
    public IEnumerable<SubItem> PropertyNamesSource
    {
        get
        {
            if (SelectedNameIndex is -1 || SelectedNameIndex > NamePickerSource.Count)
                return Array.Empty<SubItem>();

            var infoList = NameDictionary[NamePickerSource[SelectedNameIndex]];
            var propertyNames = new Dictionary<string, Token?>();
            foreach (var info in infoList)
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
            return propertyNames.Select(t => new SubItem(t.Key, t.Value is null));
        }
    }

    private int _selectedLevel;

    private int _selectedNameIndex;

    private Dictionary<uint, Dictionary<string, List<TokenInfo>>> _levelMap = new() { [0] = new() { [""] = new() } };

    public ErrorSet ErrorSet { get; private set; } = new();
}
