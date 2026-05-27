using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Microsoft.CommandPalette.Extensions; 
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ct4cpd
{
    [Guid("d2541718-87a7-4bbc-aec5-063cfa836158")]
    [ComVisible(true)]
    public partial class ct4cpd : IExtension, IDisposable
    {
        private ManualResetEvent _extensionDisposedEvent;
        private ct4cpdProvider _provider;

        public ct4cpd(ManualResetEvent extensionDisposedEvent)
        {
            _extensionDisposedEvent = extensionDisposedEvent;
            _provider = new ct4cpdProvider();
        }

        public object GetProvider(ProviderType providerType)
        {
            if (providerType == ProviderType.Commands) return _provider;
            return null;
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
        private List<CommandItem> _dockItems = new List<CommandItem>();
        private System.Timers.Timer _timer;

        public ct4cpdProvider()
        {
            // 初回読み込みでコア数を判定し、必要なブロック数を計算（最大20コア想定）
            float[] initialTemps = CoreTempReader.GetCpuTemps();
            int coreCount = initialTemps != null ? initialTemps.Length : 20; 
            
            // 4コアごとに1つのブロックに分割
            int numBlocks = (int)Math.Ceiling(coreCount / 4.0);
            if (numBlocks == 0) numBlocks = 1;

            for (int i = 0; i < numBlocks; i++)
            {
                var dummyCmd = new AnonymousCommand(() => {});
                dummyCmd.Id = $"com.ct4cpd.temp{i}";
                _dockItems.Add(new CommandItem(dummyCmd) { Title = (i == 0) ? "読込中..." : "" });
            }

            UpdateTemperature(null, null);

            _timer = new System.Timers.Timer(2000);
            _timer.Elapsed += UpdateTemperature;
            _timer.Start();
        }

        public override ICommandItem[] TopLevelCommands() => [];
        public override ICommandItem[] GetDockBands() => _dockItems.ToArray();

        private void UpdateTemperature(object? sender, ElapsedEventArgs e)
        {
            try
            {
                float[] temps = CoreTempReader.GetCpuTemps();
                if (temps != null)
                {
                    int itemIndex = 0;
                    // 4コアずつ切り出して、それぞれのブロックに割り当てる
                    for (int i = 0; i < temps.Length; i += 4)
                    {
                        if (itemIndex >= _dockItems.Count) break;

                        int count = Math.Min(4, temps.Length - i);
                        List<string> chunk = new List<string>();
                        for (int j = 0; j < count; j++)
                        {
                            chunk.Add(Math.Round(temps[i + j]).ToString());
                        }

                        // ご要望通り、全角スペースで間隔を開ける
                        _dockItems[itemIndex].Title = string.Join("　", chunk);
                        _dockItems[itemIndex].Subtitle = "";
                        itemIndex++;
                    }

                    // 余ったブロックは非表示にする
                    while (itemIndex < _dockItems.Count)
                    {
                        _dockItems[itemIndex].Title = "";
                        itemIndex++;
                    }
                }
                else
                {
                    if (_dockItems.Count > 0) _dockItems[0].Title = $"待機: {CoreTempReader.LastError}";
                }
            }
            catch (Exception ex)
            {
                if (_dockItems.Count > 0) _dockItems[0].Title = $"ｴﾗｰ: {ex.GetType().Name}";
            }
        }

        public new void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public static class CoreTempReader
    {
        public static string LastError { get; set; } = "";

        public static float[] GetCpuTemps()
        {
            try
            {
                LastError = "";
                MemoryMappedFile mmf = null;
                try
                {
                    mmf = MemoryMappedFile.OpenExisting("CoreTempMappingObject", MemoryMappedFileRights.Read);
                }
                catch (System.IO.FileNotFoundException)
                {
                    mmf = MemoryMappedFile.OpenExisting("Global\\CoreTempMappingObject", MemoryMappedFileRights.Read);
                }

                using (mmf)
                {
                    using var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                    
                    int sizeToRead = (int)Math.Min(4096, accessor.Capacity);
                    byte[] mem = new byte[sizeToRead];
                    accessor.ReadArray(0, mem, 0, sizeToRead);

                    if (mem.Length < 2568) return null;

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
            catch (System.IO.FileNotFoundException)
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
}