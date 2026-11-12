using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ct4cpd;

public enum DockLayoutMode
{
    Per2Cores,   // 縦ドック推奨: 1枠に2コア (Titleのみ、均一で最も統一感が高い)
    Per1Core,    // 縦ドック細幅: 1枠に1コア (Titleのみ、完全均一)
    Per4Cores,   // 横ドック向け: 1枠に4コア (Titleのみ、横置き時に最適)
    Summary      // サマリー: 1枠に最高温度と平均温度をTitleのみで表示
}

public class Ct4cpdSettings : JsonSettingsManager
{
    private readonly ChoiceSetSetting _layoutMode = new(
        key: "layoutMode",
        label: "ドック表示レイアウト",
        description: "ドックの向きや幅に合わせて1枠あたりのコア数を選択します。すべてTitle（上段メイン色）で統一され、灰色の下段は使用しません。",
        choices: new List<ChoiceSetSetting.Choice>
        {
            new ChoiceSetSetting.Choice("縦ドック推奨: 2コア / 枠 (Titleのみ・均一整列)", nameof(DockLayoutMode.Per2Cores)),
            new ChoiceSetSetting.Choice("縦ドック細幅: 1コア / 枠 (Titleのみ・完全均一)", nameof(DockLayoutMode.Per1Core)),
            new ChoiceSetSetting.Choice("横ドック向け: 4コア / 枠 (Titleのみ・横一列)", nameof(DockLayoutMode.Per4Cores)),
            new ChoiceSetSetting.Choice("サマリー表示: 1枠のみ (Max/AvgをTitle表示)", nameof(DockLayoutMode.Summary)),
        })
    {
        Value = nameof(DockLayoutMode.Per2Cores)
    };

    private readonly ToggleSetting _showCoreIndex = new(
        key: "showCoreIndex",
        label: "コア番号を表示する",
        description: "各枠の先頭にコア番号（例: '0-1: 45 48' や '#0: 45°'）を表示します。",
        defaultValue: false);

    private readonly ToggleSetting _showUnit = new(
        key: "showUnit",
        label: "単位 (°C) を表示する",
        description: "温度の後ろに °C を付加します。",
        defaultValue: false);

    private readonly ChoiceSetSetting _separatorSetting = new(
        key: "separator",
        label: "温度間の間隔",
        description: "複数コア表示時の数字の区切り文字を設定します。",
        choices: new List<ChoiceSetSetting.Choice>
        {
            new ChoiceSetSetting.Choice("全角スペース (広め)", "wide"),
            new ChoiceSetSetting.Choice("半角スペース2つ (標準)", "normal"),
            new ChoiceSetSetting.Choice("半角スペース1つ (狭め)", "narrow"),
        })
    {
        Value = "normal"
    };

    private readonly ChoiceSetSetting _intervalSetting = new(
        key: "updateInterval",
        label: "更新間隔",
        description: "CPU温度を読み取り・更新する間隔を設定します。",
        choices: new List<ChoiceSetSetting.Choice>
        {
            new ChoiceSetSetting.Choice("1秒", "1000"),
            new ChoiceSetSetting.Choice("2秒 (標準)", "2000"),
            new ChoiceSetSetting.Choice("3秒", "3000"),
            new ChoiceSetSetting.Choice("5秒", "5000"),
        })
    {
        Value = "2000"
    };

    public Ct4cpdSettings()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dir = Path.Combine(appData, "ct4cpd");
        Directory.CreateDirectory(dir);
        FilePath = Path.Combine(dir, "settings.json");

        Settings.Add(_layoutMode);
        Settings.Add(_showCoreIndex);
        Settings.Add(_showUnit);
        Settings.Add(_separatorSetting);
        Settings.Add(_intervalSetting);
        LoadSettings();

        Settings.SettingsChanged += (s, e) => SaveSettings();
    }

    public DockLayoutMode GetLayoutMode()
    {
        if (Enum.TryParse<DockLayoutMode>(_layoutMode.Value, out var mode))
        {
            return mode;
        }
        return DockLayoutMode.Per2Cores;
    }

    public bool ShowCoreIndex => _showCoreIndex.Value;

    public bool ShowUnit => _showUnit.Value;

    public string GetSeparator()
    {
        return _separatorSetting.Value switch
        {
            "wide"   => "　",
            "narrow" => " ",
            _        => "  "
        };
    }

    public double GetIntervalMilliseconds()
    {
        if (double.TryParse(_intervalSetting.Value, out var ms) && ms >= 500)
        {
            return ms;
        }
        return 2000.0;
    }
}
