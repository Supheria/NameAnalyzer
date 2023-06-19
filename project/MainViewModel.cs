using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Parser;
using Parser.data;

namespace NameAnalyzer;

public class MainViewModel : ObservableObject
{
    /// <summary>
    /// 最大的Level值，即<see cref="NumberBox"/>LevelPicker最大值
    /// </summary>
    public int MaxLevel => Analyzer.LevelMap.Count - 1;

    /// <summary>
    /// 仅用来提示其他属性更新，自身不绑定
    /// </summary>
    public Analysis Analyzer
    {
        get => _analyzer;
        set
        {
            if (Equals(_analyzer, value))
                return;
            _analyzer = value;
            OnPropertyChanged(nameof(MaxLevel));
            // 并且刷新NamePickerSource和ListViewSource
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
            // 并且刷新ListViewSource
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
            OnPropertyChanged(nameof(ListViewSource));
        }
    }

    /// <summary>
    /// 仅用来调用，自身不绑定
    /// </summary>
    public Dictionary<string, List<Token>> NameDictionary => Analyzer.LevelMap[(uint)SelectedLevel];

    /// <summary>
    /// 给NamePicker的<see cref="ComboBox"/>用
    /// </summary>
    public List<string> NamePickerSource => NameDictionary.Keys.ToList();

    /// <summary>
    /// 给显示子项的<see cref="ListView"/>用
    /// </summary>
    public IEnumerable<string> ListViewSource
    {
        get
        {
            if (SelectedNameIndex is -1 || SelectedNameIndex > NamePickerSource.Count)
                return Array.Empty<string>();

            var tokens = NameDictionary[NamePickerSource[SelectedNameIndex]];
            var subs = new HashSet<string>();
            foreach (var token in tokens)
            {
                switch (token)
                {
                    case Scope scope:
                        {
                            foreach (var subToken in scope.Property)
                                _ = subs.Add(subToken.Name);
                            break;
                        }
                    case TaggedValue tagged:
                        {
                            var sb = new StringBuilder();
                            _ = sb.Append($"{tagged.Tag}(...) ");
                            foreach (var value in tagged.Value)
                                _ = sb.Append($"{value} ");

                            _ = subs.Add(sb.ToString());
                            break;
                        }
                    case ValueArray vArr:
                        {
                            foreach (var value in vArr.Value)
                            {
                                var sb = new StringBuilder();
                                foreach (var element in value)
                                    _ = sb.Append($"{element} ");
                                _ = subs.Add(sb.ToString());
                            }

                            break;
                        }
                    case TagArray tArr:
                        {
                            foreach (var value in tArr.Value)
                            {
                                var sb = new StringBuilder();
                                foreach (var pair in value)
                                {
                                    _ = sb.Append($"{pair.Key}(...) ");
                                    foreach (var element in pair.Value)
                                        _ = sb.Append($"{element} ");
                                }
                                _ = subs.Add(sb.ToString());
                            }

                            break;
                        }
                }
            }
            return subs;
        }
    }

    private Analysis _analyzer = new();

    private int _selectedLevel;

    private int _selectedNameIndex;
}
