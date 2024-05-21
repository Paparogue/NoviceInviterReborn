using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Command;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Hooking;
using System.Text;
using Microsoft.ML;
using System.Reflection;
using ECommons;
using ECommons.Automation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using ECommons.GameHelpers;

#pragma warning disable CA1816
#pragma warning disable CS8602

namespace NoviceInviter;

public class NoviceInviter : IDalamudPlugin
{
    public string Name => "NoviceInviter";
    public NoviceInviterConfig PluginConfig { get; private set; }

    private delegate char NoviceInviteDelegate(long a1, long a2, short worldID, IntPtr playerName, byte a3);

    private readonly NoviceInviteDelegate _noviceInvite;

    private bool drawConfigWindow;
    private readonly List<string> _invitedPlayers = new();
    private DateTime? _minWaitToCheck = DateTime.UtcNow;
    private DateTime? _minWaitToSave = DateTime.UtcNow;
    private const string invitedPath = "C:\\Users\\Public\\player.inv";

    public DalamudPluginInterface PluginInterface { get; init; }
    public IClientState Client { get; init; }
    public ISigScanner SigScanner { get; init; }
    public IObjectTable Objects { get; init; }
    public ICommandManager CommandManager { get; init; }
    public ICondition Condition { get; init; }
    public IGameInteropProvider DalamudHook { get; init; }

    private Chat chatties;
    public delegate void PlayerSearchDelegate(IntPtr globalFunction, IntPtr playerArray, uint always0xA);
    private Hook<PlayerSearchDelegate> PlayerSearchHook = null;
    private List<String> _playerSearchList = new List<String>();
    private static readonly object predictionLock = new object();
    MLContext mlContext;
    ITransformer trainedModel;
    private bool _isActive = false;
    private DataViewSchema modelSchema;
    PredictionEngine<NameData, NamePrediction> predictionEngine;

    public int InvitedPlayersAmount()
    {
        return _invitedPlayers.Count;
    }

    public int PlayerSearchAmount()
    {
        return _playerSearchList.Count;
    }

    public void PlayerSearchClear()
    {
        StringBuilder csvContent = new StringBuilder();
        foreach (var player in _playerSearchList)
        {
            csvContent.AppendLine($"{player},0");
        }
        System.Windows.Forms.Clipboard.SetText(csvContent.ToString());
        _playerSearchList.Clear();
    }

    private void PlayerSearchDetour(IntPtr globalFunction, IntPtr playerArray, uint always0xA)
    {
        IntPtr playerArrayBeginning = playerArray + 0x3C;
        for (int i = 0; i < 10; i++)
        {
            var playerData = Marshal.PtrToStructure<PlayerSearch.PlayerData>(playerArrayBeginning);

            if (IsValidPlayerName(playerData.PlayerName))
            {
                _playerSearchList.Add(playerData.PlayerName.Trim());
            }
            else
            {
                break;
            }

            playerArrayBeginning += 0x68;
        }

        PlayerSearchHook.Original(globalFunction, playerArray, always0xA);
    }

    public void SendPlayerSearchInvites()
    {
        if (_isActive)
        {
            return;
        }
        _isActive = true;
        try
        {
            foreach (var player in _playerSearchList)
            {
                if (_invitedPlayers.Contains(player.Trim()) || MLBotDetection(player))
                {
                    continue;
                }
                SendNoviceInvite(player, (short)Client.LocalPlayer.CurrentWorld.Id);
                _invitedPlayers.Add(player.Trim());
                Thread.Sleep(200);
            }

            _playerSearchList.Clear();
        }
        finally
        {
            _isActive = false;
        }
    }

    private bool IsValidPlayerName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName))
            return false;

        foreach (char c in playerName)
        {
            if (c < 32 || c > 126)
                return false;
        }
        return true;
    }

    public NoviceInviter(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] IClientState client,
        [RequiredVersion("1.0")] ISigScanner sigScanner,
        [RequiredVersion("1.0")] IObjectTable objects,
        [RequiredVersion("1.0")] ICommandManager commandManager,
        [RequiredVersion("1.0")] IGameInteropProvider dalamudHook,
        [RequiredVersion("1.0")] ICondition condition
    )
    {
        //SetupAssemblyResolving();
        PluginInterface = pluginInterface;
        Client = client;
        DalamudHook = dalamudHook;
        SigScanner = sigScanner;
        Objects = objects;
        CommandManager = commandManager;
        Condition = condition;

        PluginConfig = PluginInterface.GetPluginConfig() as NoviceInviterConfig ?? new NoviceInviterConfig();
        PluginConfig.Init(this);
        SetupCommands();
        LoadInvitedPlayers();
        ECommonsMain.Init(PluginInterface, this);
        chatties = new Chat();

        var noviceSigPtr = SigScanner.ScanText("E8 ?? ?? ?? ?? EB A8 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 48 89 5C 24");
        _noviceInvite = Marshal.GetDelegateForFunctionPointer<NoviceInviteDelegate>(noviceSigPtr);

        var playerSearchSigPtr = SigScanner.ScanText("40 56 57 41 54 41 55 41 57 48 83 EC 30");
        if (playerSearchSigPtr != IntPtr.Zero)
        {
            PlayerSearchHook = DalamudHook.HookFromAddress<PlayerSearchDelegate>(playerSearchSigPtr, PlayerSearchDetour);
            PlayerSearchHook.Enable();
        }
        PluginInterface.UiBuilder.Draw += BuildUI;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        PluginInterface.UiBuilder.Draw += OnUpdate;
        string modelPath = @"C:\Users\AdminPC\source\repos\NoviceInviter2\NoviceInviter\model.zip";
        mlContext = new MLContext();
        using (var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            trainedModel = mlContext.Model.Load(fileStream, out modelSchema);
        }
        predictionEngine = mlContext.Model.CreatePredictionEngine<NameData, NamePrediction>(trainedModel);

    }

    private void SendNoviceInvite(string playerName, short playerWorldID)
    {
        var playerNamePtr = Marshal.StringToHGlobalAnsi(playerName);
        _noviceInvite(
            0x0,
            0x0,
            playerWorldID,
            playerNamePtr,
            0x8);

        Marshal.FreeHGlobal(playerNamePtr);
    }

    private bool CheckIfMinimumTimeHasPassed(ref DateTime? lastTimeCalled, int timeBetween)
    {
        var now = DateTime.UtcNow;
        if (lastTimeCalled == null || (now - lastTimeCalled.Value).TotalMilliseconds >= timeBetween)
        {
            lastTimeCalled = now;
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        ECommonsMain.Dispose();
        PlayerSearchHook.Disable();
        PlayerSearchHook.Dispose();
        PluginInterface.UiBuilder.Draw -= BuildUI;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        PluginInterface.UiBuilder.Draw -= OnUpdate;
        RemoveCommands();
    }

    private bool IsPlayerWithinDistance(GameObject player, float distance)
    {
        var localPlayer = Client.LocalPlayer.Position;
        var playerPosition = player.Position;
        var deltaDist = Math.Abs(Vector3.Distance(localPlayer, playerPosition));
        return deltaDist <= distance;
    }

    private void LoadInvitedPlayers()
    {
        if (!File.Exists(invitedPath))
            try
            {
                File.Create(invitedPath).Close();
            }
            catch (Exception)
            {
                PluginLog.Warning("Could not create invited player file");
            }

        try
        {
            using var reader = new StreamReader(invitedPath);
            while (reader.ReadLine() is { } line) _invitedPlayers.Add(line);
        }
        catch (Exception)
        {
            PluginLog.Warning("Could not load invited players file");
        }
    }

    private void SaveInvitedPlayers()
    {
        try
        {
            File.WriteAllLines(invitedPath, _invitedPlayers);
        }
        catch (Exception)
        {
            PluginLog.Warning("Could not save invited players file");
        }
    }

    public static bool HandlePlayerData(string playerName, string server, bool saveToFile = false)
    {
        string filePath = @"C:\Users\Public\bots.txt";
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
        }
        List<string> lines = new List<string>(File.ReadAllLines(filePath));
        string newEntry = $"{playerName},{server}";

        if (saveToFile)
        {
            if (!lines.Contains(newEntry))
            {
                File.AppendAllText(filePath, newEntry + Environment.NewLine);
                return false;
            }
            return true;
        }
        else
        {
            return lines.Contains(newEntry);
        }
    }


    public Boolean MLBotDetection(String PlayerName)
    {
        var sampleName = new NameData { Name = PlayerName.Trim() };
        NamePrediction prediction;

        lock (predictionLock)
        {
            prediction = predictionEngine.Predict(sampleName);
        }
        return prediction.PredictedLabel;
    }

    private void OnUpdate()
    {
        if(CheckIfMinimumTimeHasPassed(ref _minWaitToSave, 15000))
        {
            SaveInvitedPlayers();
        }
        if (!PluginConfig.enableInvite) return;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Client is not { IsLoggedIn: true } || Condition[ConditionFlag.BoundByDuty]) return;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Objects is null) return;

        for (var i = 0; i < Objects.Length; i++)
        {
            var gameObject = Objects.CreateObjectReference(Objects.GetObjectAddress(i));
            if (gameObject is null || !gameObject.IsValid() || gameObject.ObjectKind != ObjectKind.Player) continue;
            var player = gameObject as PlayerCharacter;
            string worldName = player.CurrentWorld.GameData.Name.ToString();
            var jobName = player.ClassJob.GameData.NameEnglish.ToString().Trim();
            if (MLBotDetection(player.Name.TextValue) && (jobName.EqualsIgnoreCase("Archer") || jobName.EqualsIgnoreCase("Lancer") || jobName.EqualsIgnoreCase("Bard") || jobName.EqualsIgnoreCase("Marauder")))
            {
                if (!HandlePlayerData(player.Name.TextValue.Trim(), worldName) && chatties != null)
                {
                    chatties.SendMessage("/void " + player.Name.TextValue.Trim() + " " + worldName + " Bot detected by ML");
                    HandlePlayerData(player.Name.TextValue.Trim(), worldName, true);
                }
                continue;
            }
            //Don't invite people that are not from this World
            if (player.HomeWorld.Id != Client.LocalPlayer.HomeWorld.Id) continue;
            //Don't invite bots Archer/Bard/Unclassed
            //if (!PluginConfig.checkBoxBardInvite)
            //Don't invite people that are not sprouts
            if (player.OnlineStatus.Id != 32) continue;
            //Don't annoy people with double invites
            if (_invitedPlayers.Contains(player.Name.TextValue.Trim())) continue;
            //Only invite people in a certain range
            if (!IsPlayerWithinDistance(gameObject, PluginConfig.sliderMaxInviteRange)) continue;
            if (!CheckIfMinimumTimeHasPassed(ref _minWaitToCheck, PluginConfig.sliderTimeBetweenInvites)) continue;
            SendNoviceInvite(player.Name.TextValue, (short)player.HomeWorld.Id);
            _invitedPlayers.Add(player.Name.TextValue.Trim());
        }
    }

    public void SetupCommands()
    {
        CommandManager.AddHandler("/noviceinviter", new CommandInfo(OnConfigCommandHandler)
        {
            HelpMessage = $"Opens the config window for {Name}.",
            ShowInHelp = true
        });
    }

    private void OpenConfigUi()
    {
        OnConfigCommandHandler(null, null);
    }

    public void OnConfigCommandHandler(string? command, string? args)
    {
        drawConfigWindow = true;
    }

    public void RemoveCommands()
    {
        CommandManager.RemoveHandler("/noviceinviter");
    }

    private void BuildUI()
    {
        drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();
    }
}