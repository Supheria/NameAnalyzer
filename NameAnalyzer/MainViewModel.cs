using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Parser.data;
using Parser.helper;
using Windows.Foundation.Collections;

namespace NameAnalyzer;

public partial class MainViewModel : ObservableObject
{
    private string _messageDialogText = "";

    public string MessageDialogText
    {
        get => _messageDialogText;
        set
        {
            // if (_messageDialogText == value)
            //   return;
            _messageDialogText = value;
            OnPropertyChanged();
            _messageDialogShow();
        }
    }

    private readonly Action _messageDialogShow;

    public MainViewModel(Action messageDialogShow) => _messageDialogShow = messageDialogShow;

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
                            probableType.Add("Scope");
                            foreach (var token in scope.Property)
                                propertyNames.Add(token.ToString());
                            break;
                        case TaggedValue taggedValue:
                            probableType.Add("TaggedValue");
                            values.Add(taggedValue.ToString());
                            break;
                        case ValueArray valueArray:
                            probableType.Add("ValueArray");
                            values.Add(valueArray.ToString());
                            break;
                        case TagArray tagArray:
                            probableType.Add("TagArray");
                            values.Add(tagArray.ToString());
                            break;
                        default:
                            probableType.Add("Token");
                            break;
                    }

                    sourceFiles.Add(info.FilePath);
                }

                if (probableType.Count > 1)
                    ErrorMessage =
                        ErrorSet.Append($"\n\"{infos[0].Name}\" in level {_infoShowing.Level} has different types.");

                if (probableType.Count > 0)
                    NameInfoType = probableType.Aggregate("", (current, s) => current + s + " ");
                if (propertyNames.Count > 0)
                    NameInfoPropertyNames = propertyNames.Aggregate("", (current, s) => current + s + "\n");
                if (values.Count > 0)
                    NameInfoValue = values.Aggregate("", (current, s) => current + s + "\n");
                if (sourceFiles.Count > 0)
                    NameInfoSourceFile = sourceFiles.Aggregate("", (current, s) => current + s + "\n");
            }
            else
                NameInfoType = NameInfoPropertyNames = NameInfoValue = NameInfoSourceFile = "";
            OnPropertyChanged(nameof(NameInfoVisibility));
        }
    }

    public string ParseException
    {
        get => _parseException;
        set
        {
            if (value == _parseException)
                return;
            _parseException = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ParseExceptionVisibility));
            MessageDialogText = value;
        }
    }

    private string _parseException = "";

    public Visibility ParseExceptionVisibility => ParseException is "" ? Visibility.Collapsed : Visibility.Visible;

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (value == _errorMessage)
                return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ErrorMessageVisibility));
            MessageDialogText = value;
        }
    }
    private string _errorMessage = "";
    public Visibility ErrorMessageVisibility => ErrorMessage is "" ? Visibility.Collapsed : Visibility.Visible;

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

    [ObservableProperty] private string _nameInfoType = "";
    [ObservableProperty] private string _nameInfoPropertyNames = "";
    [ObservableProperty] private string _nameInfoValue = "";
    [ObservableProperty] private string _nameInfoSourceFile = "";
    public Visibility NameInfoVisibility => string.IsNullOrEmpty(NameInfoType + NameInfoPropertyNames + NameInfoValue + NameInfoSourceFile) ? Visibility.Collapsed : Visibility.Visible;

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
    public IEnumerable<string> PropertyNamesSource
    {
        get
        {
            if (SelectedNameIndex is -1 || SelectedNameIndex > NamePickerSource.Count)
                return Array.Empty<string>();

            var infoList = NameDictionary[NamePickerSource[SelectedNameIndex]];
            var propertyNames = new HashSet<string>();
            foreach (var info in infoList)
            {
                if (info.Token is not Scope scope)
                    continue;
                foreach (var property in scope.Property)
                {
                    _ = propertyNames.Add(property.ToString());
                }
            }
            return propertyNames;
        }
    }

    private int _selectedLevel;

    private int _selectedNameIndex;

    private Dictionary<uint, Dictionary<string, List<TokenInfo>>> _levelMap = new() { [0] = new() { [""] = new() } };

    public ErrorSet ErrorSet { get; private set; } = new();
}
