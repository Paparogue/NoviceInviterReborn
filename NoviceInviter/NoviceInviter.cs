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
using ECommons.GameHelpers;
using ECommons.Logging;

#pragma warning disable CA1816
#pragma warning disable CS8602

namespace NoviceInviter;

public class NoviceInviter : IDalamudPlugin
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
                _playerSearchList.Add(playerData.PlayerName.Trim());
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
                if (_invitedPlayers.Contains(player.Trim()) || IsABot(player))
                    continue;

                var playerAddress = Client.LocalPlayer.Address+0x2268;
                var playerWorld = Marshal.ReadInt16(playerAddress);
                SendNoviceInvite(player, playerWorld);
                //SendNoviceInvite(player, (short)402); //402 is alpha und 403 is raiden
                _invitedPlayers.Add(player.Trim());
                Thread.Sleep(100);
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
        //SetupAssemblyResolving();
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
        //ECommonsMain.Init(pluginInterface, this, null);
        //chatties = new Chat();

        //var noviceSigPtr = SigScanner.ScanText("C6 44 24 20 08 45 0F B7 C6 48 8B D6 48 8B CF E8")+0xF;
        var noviceSigPtr = SigScanner.ScanText("E8 ?? ?? ?? ?? EB 40 41 B1 09");
        //var noviceSigPtrx = SigScanner.ScanText("E8 ?? ?? ?? ?? BA 0C 00 00 00 48 8D 0D");
        //PluginLog.Warning($"noviceInviteSig: 0x{noviceSigPtr:X}");
        //PluginLog.Warning($"Test: 0x{noviceSigPtrx:X}");
        _noviceInvite = Marshal.GetDelegateForFunctionPointer<NoviceInviteDelegate>(noviceSigPtr);

        var playerSearchSigPtr = SigScanner.ScanText("40 56 57 41 54 41 55 41 56 48 83 EC 40");
        if (playerSearchSigPtr != IntPtr.Zero)
        {
            PlayerSearchHook = DalamudHook.HookFromAddress<PlayerSearchDelegate>(playerSearchSigPtr, PlayerSearchDetour);
            PlayerSearchHook.Enable();
        }
        string modelPath = @"C:\Users\AdminPC\source\repos\NoviceInviter2\NoviceInviter\model.zip";
        mlContext = new MLContext();
        using (var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            trainedModel = mlContext.Model.Load(fileStream, out modelSchema);
        }
        predictionEngine = mlContext.Model.CreatePredictionEngine<NameData, NamePrediction>(trainedModel);
        PluginInterface.UiBuilder.Draw += BuildUI;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        PluginInterface.UiBuilder.Draw += OnUpdate;
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

    public void Dispose()
    {
        //ECommonsMain.Dispose();
        PlayerSearchHook.Disable();
        PlayerSearchHook.Dispose();
        PluginInterface.UiBuilder.Draw -= BuildUI;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        PluginInterface.UiBuilder.Draw -= OnUpdate;
        RemoveCommands();
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
            if (CheckIfMinimumTimeHasPassed(ref _minWaitToSave, 15000))
            {
                SaveInvitedPlayers();
            }
            /*
            if (!PluginConfig.enableInvite) return;
            if (Client == null || Condition == null || Objects == null) return;
            if (Client is not { IsLoggedIn: true } || Condition[ConditionFlag.BoundByDuty]) return;

            for (var i = 0; i < Objects.Length; i++)
            {
                var gameObject = Objects.CreateObjectReference(Objects.GetObjectAddress(i));
                if (gameObject is null || !gameObject.IsValid() || gameObject.ObjectKind != ObjectKind.Player) continue;
                IPlayerCharacter? player = gameObject as IPlayerCharacter;
                if (player == null)
                    continue;

                string worldName = player.CurrentWorld.GameData.Name.ToString();
                var jobName = player.ClassJob.GameData.NameEnglish.ToString();
                bool botty;
                try
                {
                    botty = IsABot(player.Name.TextValue);
                }
                catch (Exception ex)
                {
                    continue; // Skip this player and continue with the next one
                }

                if (botty && chatties != null && (jobName.EqualsIgnoreCase("Archer") || jobName.EqualsIgnoreCase("Lancer") || jobName.EqualsIgnoreCase("Bard") || jobName.EqualsIgnoreCase("Marauder")))
                {
                    if (!HandlePlayerData(player.Name.TextValue.Trim(), worldName) && chatties != null)
                    {
                        chatties.SendMessage("/void " + player.Name.TextValue.Trim() + " " + worldName + " Bot detected by ML");
                        HandlePlayerData(player.Name.TextValue.Trim(), worldName, true);
                    }
                }

                if (botty) continue;
                if (player.HomeWorld.Id != Client.LocalPlayer.HomeWorld.Id) continue;
                if (player.OnlineStatus.Id != 32) continue;
                if (_invitedPlayers is null || _invitedPlayers.Contains(player.Name.TextValue.Trim())) continue;
                if (!IsPlayerWithinDistance(gameObject, PluginConfig.sliderMaxInviteRange)) continue;
                if (!CheckIfMinimumTimeHasPassed(ref _minWaitToCheck, PluginConfig.sliderTimeBetweenInvites)) continue;
                SendNoviceInvite(player.Name.TextValue, (short)player.HomeWorld.Id);
                _invitedPlayers.Add(player.Name.TextValue.Trim());
        }*/
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