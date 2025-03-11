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
using Lumina.Excel;
using static NoviceInviterReborn.PlayerSearch;

#pragma warning disable CA1816
#pragma warning disable CS8602

namespace NoviceInviterReborn;

public class NoviceInviterReborn : IDalamudPlugin
{
    public string Name => "NoviceInviter";
    public NoviceInviterConfig PluginConfig { get; private set; }

    private delegate char NoviceInviteDelegate(IntPtr a1, IntPtr a2, short worldID, IntPtr playerName, byte a3);

    private readonly NoviceInviteDelegate _noviceInvite;

    private bool drawConfigWindow;
    private readonly List<string> _invitedPlayers = new();
    private DateTime? _minWaitToCheck = DateTime.UtcNow;
    private DateTime? _minWaitToSave = DateTime.UtcNow;
    private const string invitedPath = "C:\\Users\\Public\\player.inv";

    public IDalamudPluginInterface PluginInterface { get; init; }
    public IClientState Client { get; init; }
    public ISigScanner SigScanner { get; init; }
    public IObjectTable Objects { get; init; }
    public ICommandManager CommandManager { get; init; }
    public ICondition Condition { get; init; }
    public IGameInteropProvider DalamudHook { get; init; }

    public IPluginLog PluginLog { get; init; }

    public delegate void PlayerSearchDelegate(IntPtr globalFunction, IntPtr playerArray, uint always0xA);
    private Hook<PlayerSearchDelegate> PlayerSearchHook = null;
    private List<String> _playerSearchList = new List<String>();
    private static readonly object predictionLock = new object();
    MLContext mlContext;
    ITransformer trainedModel;
    private bool _isActive = false;
    private DataViewSchema modelSchema;
    PredictionEngine<NameData, NamePrediction> predictionEngine;

    public NoviceInviterReborn(
        IDalamudPluginInterface pluginInterface,
        IClientState client,
        ISigScanner sigScanner,
        IObjectTable objects,
        ICommandManager commandManager,
        IGameInteropProvider dalamudHook,
        ICondition condition,
        IPluginLog pluginLog
    )
    {
        PluginInterface = pluginInterface;
        Client = client;
        DalamudHook = dalamudHook;
        SigScanner = sigScanner;
        Objects = objects;
        CommandManager = commandManager;
        Condition = condition;
        PluginLog = pluginLog;
        PluginConfig = PluginInterface.GetPluginConfig() as NoviceInviterConfig ?? new NoviceInviterConfig();
        PluginConfig.Init(this);
        SetupCommands();
        LoadInvitedPlayers();
        var noviceSigPtr = SigScanner.ScanText("E8 ?? ?? ?? ?? EB 40 41 B1 09");
        //var OLD_noviceSigPtrx = SigScanner.ScanText("E8 ?? ?? ?? ?? BA 0C 00 00 00 48 8D 0D");
        _noviceInvite = Marshal.GetDelegateForFunctionPointer<NoviceInviteDelegate>(noviceSigPtr);

        var playerSearchSigPtr = SigScanner.ScanText("E8 ?? ?? ?? ?? 49 8B 4F ?? 48 8B 01 FF 50 ?? 41 0F B6 97");
        if (playerSearchSigPtr != IntPtr.Zero)
        {
            PlayerSearchHook = DalamudHook.HookFromAddress<PlayerSearchDelegate>(playerSearchSigPtr, PlayerSearchDetour);
            PlayerSearchHook.Enable();
        }
        /*string modelPath = @"C:\Users\AdminPC\source\repos\NoviceInviter2\NoviceInviter\model.zip";
        mlContext = new MLContext();
        using (var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            trainedModel = mlContext.Model.Load(fileStream, out modelSchema);
        }*/
        //predictionEngine = mlContext.Model.CreatePredictionEngine<NameData, NamePrediction>(trainedModel);
        PluginInterface.UiBuilder.Draw += BuildUI;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += OpenConfigUi;
        PluginInterface.UiBuilder.Draw += OnUpdate;
    }

    public void Dispose()
    {
        PlayerSearchHook.Disable();
        PlayerSearchHook.Dispose();
        PluginInterface.UiBuilder.Draw -= BuildUI;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= OpenConfigUi;
        PluginInterface.UiBuilder.Draw -= OnUpdate;
        RemoveCommands();
    }

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
        /*
        StringBuilder csvContent = new StringBuilder();
        foreach (var player in _playerSearchList)
        {
            csvContent.AppendLine($"{player},0");
        }
        System.Windows.Forms.Clipboard.SetText(csvContent.ToString());
        */
        _playerSearchList.Clear();
    }

    private void PlayerSearchDetour(IntPtr globalFunction, IntPtr playerArray, uint always0xA)
    {
        IntPtr playerArrayBeginning = playerArray + 0x44;
        for (int i = 0; i < 10; i++)
        {
            var playerData = Marshal.PtrToStructure<PlayerSearch.PlayerData>(playerArrayBeginning);
            if (IsValidPlayerName(playerData.PlayerName))
            {
                PluginLog.Warning(playerData.PlayerName);
                //_playerSearchList.Add(playerData.PlayerName.Trim());
            }
            else
            {
                break;
            }

            playerArrayBeginning += 0x70;
        }

        PlayerSearchHook.Original(globalFunction, playerArray, always0xA);
    }

    public void SendPlayerSearchInvites()
    {
        if (_isActive || Client.LocalPlayer is null)
            return;

        _isActive = true;

        try
        {
            foreach (var player in _playerSearchList)
            {
                string worldName = Client.LocalPlayer.CurrentWorld.Value.Name.ToString();
                if (_invitedPlayers.Contains(player.Trim() + "-" + worldName) || IsABot(player))
                    continue;

                //var playerAddress = Client.LocalPlayer.Address + 0x2268;
                //var playerWorld = Marshal.ReadInt16(playerAddress);
                //SendNoviceInvite(player, playerWorld);
                SendNoviceInvite(player, (short)Client.LocalPlayer.CurrentWorld.RowId);
                _invitedPlayers.Add(player.Trim() + "-" + worldName);
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

    private unsafe void SendNoviceInvite(string playerName, short playerWorldID)
    {
        try
        {
            PluginLog.Debug($"Attempting to send novice invite to {playerName} (World ID: {playerWorldID})");

            if (_noviceInvite == null)
            {
                PluginLog.Error("NoviceInvite function pointer is null");
                return;
            }

            byte[] playerNameBytes = Encoding.UTF8.GetBytes(playerName + "\0");
            fixed (byte* playerNamePtr = playerNameBytes)
            {
                PluginLog.Debug($"Calling _noviceInvite function");
                char result = _noviceInvite(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    playerWorldID,
                    (IntPtr)playerNamePtr,
                    8);
                PluginLog.Debug($"_noviceInvite function returned: {result}");
            }

            PluginLog.Debug($"Successfully sent novice invite to {playerName}");
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Error in SendNoviceInvite for player {playerName}");
        }
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

    private bool IsPlayerWithinDistance(IGameObject player, float distance)
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

    public bool IsABot(string PlayerName)
    {
        if (string.IsNullOrEmpty(PlayerName))
        {
            throw new ArgumentNullException(nameof(PlayerName), "PlayerName is null or empty in IsABot.");
        }

        if (predictionEngine == null)
        {
            throw new InvalidOperationException("predictionEngine is not initialized in IsABot.");
        }

        var PlayerNameData = new NameData { Name = PlayerName.Trim() };

        NamePrediction prediction;
        lock (predictionLock)
        {
            prediction = predictionEngine.Predict(PlayerNameData);
        }
        return prediction.PredictedLabel;
    }


    private void OnUpdate()
    {
        try
        {
            if (Client == null || Condition == null || Objects == null) return;

            if (CheckIfMinimumTimeHasPassed(ref _minWaitToSave, 15000))
                SaveInvitedPlayers();
            
            if (!PluginConfig.enableInvite) return;
            if (Client is not { IsLoggedIn: true } || Condition[ConditionFlag.BoundByDuty]) return;

            foreach (var o in Objects)
            {
                if (o is null || !o.IsValid() || o.ObjectKind != ObjectKind.Player) continue;
                IPlayerCharacter? player = o as IPlayerCharacter;
                string worldName = player.CurrentWorld.Value.Name.ToString();
                var jobName = player.ClassJob.Value.NameEnglish.ToString();
                bool botty = false;
                /*try
                {
                    botty = IsABot(player.Name.TextValue);
                }
                catch (Exception ex)
                {
                    continue; // Skip this player and continue with the next one
                }

                if (jobName.EqualsIgnoreCase("Archer") || jobName.EqualsIgnoreCase("Lancer") || jobName.EqualsIgnoreCase("Bard") || jobName.EqualsIgnoreCase("Marauder")))
                */

                //is considered a bot
                if (botty) continue;
                //is from different world
                if (player.CurrentWorld.Value.RowId != Client.LocalPlayer.CurrentWorld.RowId) continue;
                //is a sprout
                if (player.OnlineStatus.RowId != 32) continue;
                //was not already invited
                if (_invitedPlayers is null || _invitedPlayers.Contains(player.Name.TextValue.Trim() + "-" + worldName)) continue;
                //is within distance
                if (!IsPlayerWithinDistance(o, PluginConfig.sliderMaxInviteRange)) continue;
                //enough time passed between invite
                if (!CheckIfMinimumTimeHasPassed(ref _minWaitToCheck, PluginConfig.sliderTimeBetweenInvites)) continue;
                //invite player
                SendNoviceInvite(player.Name.TextValue, (short)player.HomeWorld.RowId);
                //dont invite them twice
                _invitedPlayers.Add(player.Name.TextValue.Trim() + "-" + worldName);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Error during OnUpdate.");
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