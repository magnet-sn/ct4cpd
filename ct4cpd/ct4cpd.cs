using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ct4cpd;

[Guid("d2541718-87a7-4bbc-aec5-063cfa836158")]
[ComVisible(true)]
public sealed partial class ct4cpd : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly ct4cpdProvider _provider;

    public ct4cpd(ManualResetEvent extensionDisposedEvent)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
        _provider = new ct4cpdProvider();
    }

    public object? GetProvider(ProviderType providerType)
    {
        return providerType == ProviderType.Commands ? _provider : null;
    }

    public void Dispose()
    {
        _provider?.Dispose();
        _extensionDisposedEvent?.Set();
        GC.SuppressFinalize(this);
    }
}

public partial class ct4cpdProvider : CommandProvider
{
    private readonly List<CommandItem> _dockItems = new();
    private readonly System.Timers.Timer _timer;
    private readonly Ct4cpdSettings _settingsManager;
    private readonly ct4cpdPage _detailPage;
    private int _isUpdating;

    public ct4cpdProvider()
    {
        Id = "com.ct4cpd.provider";
        DisplayName = "Core Temp Monitor (ct4cpd)";
        Icon = IconHelpers.FromRelativePath(@"Assets\StoreLogo.png");

        _settingsManager = new Ct4cpdSettings();
        Settings = _settingsManager.Settings;
        _detailPage = new ct4cpdPage(() => UpdateTemperature(null, null));

        // 初期ブロック数の決定（未起動時は論理プロセッサ数または20をベースにする）
        float[]? initialTemps = CoreTempReader.GetCpuTemps();
        int initialCoreCount = initialTemps?.Length ?? Math.Max(Environment.ProcessorCount, 20);
        int numBlocks = CalculateRequiredBlocks(initialCoreCount, _settingsManager.GetLayoutMode());

        EnsureDockItemCount(numBlocks);

        _settingsManager.Settings.SettingsChanged += OnSettingsChanged;

        UpdateTemperature(null, null);

        double interval = _settingsManager.GetIntervalMilliseconds();
        _timer = new System.Timers.Timer(interval);
        _timer.Elapsed += UpdateTemperature;
        _timer.Start();
    }

    public override ICommandItem[] TopLevelCommands() =>
    [
        new CommandItem(_detailPage)
        {
            Title = "CPU 温度詳細 (Core Temp)",
            Subtitle = "各コアのリアルタイム温度一覧と統計情報を表示します",
            Icon = IconHelpers.FromRelativePath(@"Assets\StoreLogo.png")
        }
    ];

    public override ICommandItem[] GetDockBands() => _dockItems.Cast<ICommandItem>().ToArray();

    private void OnSettingsChanged(object sender, Settings args)
    {
        double newInterval = _settingsManager.GetIntervalMilliseconds();
        if (Math.Abs(_timer.Interval - newInterval) > 10)
        {
            _timer.Interval = newInterval;
        }

        UpdateTemperature(null, null);
    }

    private static int CalculateRequiredBlocks(int coreCount, DockLayoutMode mode)
    {
        if (mode == DockLayoutMode.Summary) return 1;

        int coresPerBlock = mode switch
        {
            DockLayoutMode.Per2Cores => 2,
            DockLayoutMode.Per1Core  => 1,
            DockLayoutMode.Per4Cores => 4,
            _ => 2
        };

        return Math.Max(1, (int)Math.Ceiling(coreCount / (double)coresPerBlock));
    }

    private void EnsureDockItemCount(int requiredBlocks)
    {
        bool changed = false;
        while (_dockItems.Count < requiredBlocks)
        {
            int idx = _dockItems.Count;
            var cmd = new AnonymousCommand(() => UpdateTemperature(null, null))
            {
                Id = $"com.ct4cpd.temp{idx}",
                Name = "更新"
            };
            _dockItems.Add(new CommandItem(cmd)
            {
                Title = (idx == 0) ? "読込中..." : "",
                Subtitle = "" // 下段（灰色）は使用しない
            });
            changed = true;
        }

        if (changed)
        {
            try
            {
                RaiseItemsChanged(_dockItems.Count);
            }
            catch
            {
                // ホスト未登録・未初期化時の例外防止
            }
        }
    }

    private void ClearRemainingDockItems(int startIndex)
    {
        for (int i = startIndex; i < _dockItems.Count; i++)
        {
            _dockItems[i].Title = "";
            _dockItems[i].Subtitle = "";
        }
    }

    private void UpdateTemperature(object? sender, ElapsedEventArgs? e)
    {
        if (Interlocked.CompareExchange(ref _isUpdating, 1, 0) != 0)
            return;

        try
        {
            float[]? temps = CoreTempReader.GetCpuTemps();
            if (temps != null && temps.Length > 0)
            {
                var mode = _settingsManager.GetLayoutMode();
                string unit = _settingsManager.ShowUnit ? "°C" : "";
                string sep = _settingsManager.GetSeparator();
                bool showCoreIndex = _settingsManager.ShowCoreIndex;

                int requiredBlocks = CalculateRequiredBlocks(temps.Length, mode);
                EnsureDockItemCount(requiredBlocks);

                if (mode == DockLayoutMode.Summary)
                {
                    // 1枠に最高温度と平均温度をTitle（メイン文字色）のみで表示
                    double max = Math.Round(temps.Max());
                    double avg = Math.Round(temps.Average());
                    _dockItems[0].Title = $"MAX {max}{unit}  AVG {avg}{unit}";
                    _dockItems[0].Subtitle = "";
                    ClearRemainingDockItems(1);
                }
                else
                {
                    int groupSize = mode switch
                    {
                        DockLayoutMode.Per2Cores => 2,
                        DockLayoutMode.Per1Core  => 1,
                        DockLayoutMode.Per4Cores => 4,
                        _ => 2
                    };

                    int itemIndex = 0;
                    for (int i = 0; i < temps.Length && itemIndex < _dockItems.Count; i += groupSize)
                    {
                        int count = Math.Min(groupSize, temps.Length - i);
                        var parts = new List<string>(count);

                        for (int j = 0; j < count; j++)
                        {
                            double val = Math.Round(temps[i + j]);
                            parts.Add($"{val}{unit}");
                        }

                        string text = string.Join(sep, parts);

                        // コア番号プレフィックス（オプション）
                        if (showCoreIndex)
                        {
                            if (groupSize == 1)
                            {
                                text = $"#{i}: {text}";
                            }
                            else
                            {
                                int endIdx = i + count - 1;
                                text = $"{i}-{endIdx}: {text}";
                            }
                        }

                        // すべてTitle（メイン文字色）のみに設定。Subtitle（灰色）は常に空文字で統一
                        _dockItems[itemIndex].Title = text;
                        _dockItems[itemIndex].Subtitle = "";
                        itemIndex++;
                    }

                    ClearRemainingDockItems(itemIndex);
                }
            }
            else if (_dockItems.Count > 0)
            {
                string err = string.IsNullOrEmpty(CoreTempReader.LastError) ? "未発見" : CoreTempReader.LastError;
                _dockItems[0].Title = $"待機: {err}";
                _dockItems[0].Subtitle = "";
                ClearRemainingDockItems(1); // 2ブロック目以降の残像を確実に消去
            }
        }
        catch (Exception ex)
        {
            if (_dockItems.Count > 0)
            {
                _dockItems[0].Title = $"ｴﾗｰ: {ex.GetType().Name}";
                _dockItems[0].Subtitle = "";
                ClearRemainingDockItems(1);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isUpdating, 0);
        }
    }

    public override void Dispose()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Elapsed -= UpdateTemperature;
            _timer.Dispose();
        }
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}

public static class CoreTempReader
{
    public static string LastError { get; set; } = "";

    public static float[]? GetCpuTemps()
    {
        try
        {
            LastError = "";
            MemoryMappedFile? mmf = null;
            try
            {
                mmf = MemoryMappedFile.OpenExisting("CoreTempMappingObject", MemoryMappedFileRights.Read);
            }
            catch (FileNotFoundException)
            {
                mmf = MemoryMappedFile.OpenExisting(@"Global\CoreTempMappingObject", MemoryMappedFileRights.Read);
            }

            using (mmf)
            {
                using var accessor = mmf.CreateViewAccessor(0L, 0L, MemoryMappedFileAccess.Read);

                int sizeToRead = (int)Math.Min(4096L, accessor.Capacity);
                byte[] mem = new byte[sizeToRead];
                accessor.ReadArray(0L, mem, 0, sizeToRead);

                if (mem.Length < 2568)
                {
                    LastError = "データ不足";
                    return null;
                }

                uint coreCount = BitConverter.ToUInt32(mem, 1536);
                if (coreCount == 0 || coreCount > 256)
                {
                    LastError = "読込エラー";
                    return null;
                }

                float[] temps = new float[coreCount];
                for (int i = 0; i < coreCount; i++)
                {
                    temps[i] = BitConverter.ToSingle(mem, 1544 + (i * 4));
                }

                return temps;
            }
        }
        catch (FileNotFoundException)
        {
            LastError = "未発見";
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            LastError = "権限拒否";
            return null;
        }
        catch (Exception ex)
        {
            LastError = ex.GetType().Name;
            return null;
        }
    }
}
