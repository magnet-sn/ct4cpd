using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ct4cpd;

internal sealed partial class ct4cpdPage : ListPage
{
    private readonly Action? _refreshCallback;

    public ct4cpdPage(Action? refreshCallback = null)
    {
        _refreshCallback = refreshCallback;
        Icon = IconHelpers.FromRelativePath(@"Assets\StoreLogo.png");
        Title = "Core Temp CPU 温度一覧";
        Name = "Open";
    }

    public override IListItem[] GetItems()
    {
        float[]? temps = CoreTempReader.GetCpuTemps();
        if (temps == null || temps.Length == 0)
        {
            string reason = string.IsNullOrEmpty(CoreTempReader.LastError)
                ? "Core Temp が起動していません。"
                : $"エラー: {CoreTempReader.LastError}";

            return
            [
                new ListItem(new AnonymousCommand(() => _refreshCallback?.Invoke()))
                {
                    Title = "Core Temp からデータを取得できません",
                    Subtitle = $"{reason} (クリックして再試行)",
                    Icon = IconHelpers.FromRelativePath(@"Assets\StoreLogo.png")
                },
                new ListItem(new NoOpCommand())
                {
                    Title = "対処方法",
                    Subtitle = "Core Temp を起動し、[オプション] -> [設定] -> [共有メモリを有効にする] がオンになっていることを確認してください。"
                }
            ];
        }

        double max = Math.Round(temps.Max(), 1);
        double min = Math.Round(temps.Min(), 1);
        double avg = Math.Round(temps.Average(), 1);

        var items = new List<IListItem>
        {
            new ListItem(new AnonymousCommand(() => _refreshCallback?.Invoke()))
            {
                Title = $"最高: {max} °C  /  平均: {avg} °C  /  最低: {min} °C",
                Subtitle = $"検出コア数: {temps.Length} (クリックして今すぐ更新)",
                Icon = IconHelpers.FromRelativePath(@"Assets\StoreLogo.png")
            }
        };

        for (int i = 0; i < temps.Length; i++)
        {
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = $"Core #{i}: {Math.Round(temps[i], 1)} °C",
                Subtitle = (temps[i] == max) ? "最高温度コア" : (temps[i] == min ? "最低温度コア" : "")
            });
        }

        return items.ToArray();
    }
}
