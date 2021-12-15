using ArchiSteamFarm;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Storage;
using ArchiSteamFarm.Storage;
using DynamicData;
using ReactiveUI;
using System;
using System.Application.Settings;
using System.Application.UI.Resx;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace System.Application.Services
{
    public sealed class ASFService : ReactiveObject
    {
        static ASFService? mCurrent;
        public static ASFService Current => mCurrent ?? new();

        readonly IArchiSteamFarmService archiSteamFarmService = IArchiSteamFarmService.Instance;

        string? _IPCUrl;
        public string? IPCUrl
        {
            get => _IPCUrl;
            set => this.RaiseAndSetIfChanged(ref _IPCUrl, value);
        }

        string? _ConsoleLogText;
        public string? ConsoleLogText
        {
            get => _ConsoleLogText;
            set => this.RaiseAndSetIfChanged(ref _ConsoleLogText, value);
        }

        public IConsoleBuilder ConsoleLogBuilder { get; } = new ConsoleBuilder();

        public SourceCache<Bot, string> SteamBotsSourceList;

        public bool IsASFRuning => archiSteamFarmService.StartTime != null;

        GlobalConfig? _GlobalConfig;
        public GlobalConfig? GlobalConfig
        {
            get => _GlobalConfig;
            set => this.RaiseAndSetIfChanged(ref _GlobalConfig, value);
        }

        public ASFService()
        {
            mCurrent = this;

            SteamBotsSourceList = new SourceCache<Bot, string>(t => t.BotName);

            archiSteamFarmService.OnConsoleWirteLine += OnConsoleWirteLine;

            ASFSettings.ConsoleMaxLine.Subscribe(x =>
            {
                var line = x;
                if (x < ASFSettings.MinRangeConsoleMaxLine) line = ASFSettings.MinRangeConsoleMaxLine;
                if (x > ASFSettings.MaxRangeConsoleMaxLine) line = ASFSettings.MaxRangeConsoleMaxLine;
                ConsoleLogBuilder.MaxLine = line;
            });
        }

        void OnConsoleWirteLine(string message)
        {
            MainThread2.InvokeOnMainThreadAsync(() =>
            {
                ConsoleLogBuilder.AppendLine(message);
                var text = ConsoleLogBuilder.ToString();
                ConsoleLogText = text;
            });
        }

        /// <summary>
        /// 是否正在启动或停止中
        /// </summary>
        bool IsASFRunOrStoping;

        public Task InitASF() => InitASFCore(true);

        public async Task InitASFCore(bool showToast)
        {
            if (IsASFRunOrStoping) return;

            if (showToast) Toast.Show(AppResources.ASF_Starting, ToastLength.Short);

            IsASFRunOrStoping = true;

            await archiSteamFarmService.Start();

            RefreshBots();

            IPCUrl = archiSteamFarmService.GetIPCUrl();

            MainThread2.BeginInvokeOnMainThread(() =>
            {
                this.RaisePropertyChanged(nameof(IsASFRuning));
            });

            IsASFRunOrStoping = false;

            if (showToast) Toast.Show(AppResources.ASF_Started, ToastLength.Short);
        }

        public Task StopASF() => StopASFCore(true);

        public async Task StopASFCore(bool showToast)
        {
            if (IsASFRunOrStoping) return;

            if (showToast) Toast.Show(AppResources.ASF_Stoping, ToastLength.Short);

            IsASFRunOrStoping = true;

            await archiSteamFarmService.Stop();

            MainThread2.BeginInvokeOnMainThread(() =>
            {
                this.RaisePropertyChanged(nameof(IsASFRuning));
            });

            IsASFRunOrStoping = false;

            if (showToast) Toast.Show(AppResources.ASF_Stoped, ToastLength.Short);
        }

        public void RefreshBots()
        {
            var bots = archiSteamFarmService.GetReadOnlyAllBots();
            if (bots.Any_Nullable())
            {
                SteamBotsSourceList.AddOrUpdate(bots!.Values);
            }
        }

        public void RefreshConfig()
        {
            GlobalConfig = archiSteamFarmService.GetGlobalConfig();
        }

        public async void ImportBotFiles(IEnumerable<string> files)
        {
            var num = 0;
            foreach (var filename in files)
            {
                var file = new FileInfo(filename);
                if (file.Exists)
                {
                    var bot = await BotConfig.Load(file.FullName).ConfigureAwait(false);
                    if (bot.BotConfig != null)
                    {
                        try
                        {
                            file.CopyTo(Path.Combine(SharedInfo.ConfigDirectory, file.Name), true);
                            num++;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(nameof(ASFService), ex, nameof(ImportBotFiles));
                        }
                    }
                }
            }
            Toast.Show(string.Format(AppResources.LocalAuth_ImportSuccessTip, num));
        }
    }
}