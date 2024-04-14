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
using System.Collections;
using static Lumina.Data.Parsing.Layer.LayerCommon;
using static NoviceInviter.PlayerSearch;


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

    public delegate void PlayerSearchDelegate(IntPtr globalFunction, IntPtr playerArray, uint always0xA);
    private Hook<PlayerSearchDelegate> PlayerSearchHook = null;
    public List<String> playersToInvite = new List<String>();

    private void PlayerSearchDetour(IntPtr globalFunction, IntPtr playerArray, uint always0xA)
    {
        IntPtr playerArrayBeginning = playerArray + 0x3C;
        for (int i = 0; i < 10; i++)
        {
            var playerData = Marshal.PtrToStructure<PlayerSearch.PlayerData>(playerArrayBeginning);

            if (IsValidPlayerName(playerData.PlayerName))
            {
                //PluginLog.Warning("Player has been added: " + playerData.PlayerName.Trim());
                playersToInvite.Add(playerData.PlayerName.Trim());
            }
            else
            {
                break;
            }

            playerArrayBeginning += 0x68;
        }

        PlayerSearchHook.Original(globalFunction, playerArray, always0xA);
    }

    private bool _isActive = false;

    public void SendPlayerSearchInvites()
    {
        if (_isActive)
        {
            return;
        }
        _isActive = true;
        try
        {
            foreach (var player in playersToInvite)
            {
                if (_invitedPlayers.Contains(player.Trim())) continue;
                SendNoviceInvite(player, (short)Client.LocalPlayer.CurrentWorld.Id);
                _invitedPlayers.Add(player.Trim());
                Thread.Sleep(500);
            }

            playersToInvite.Clear();
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


    private void OnUpdate()
    {
        if (!PluginConfig.enableInvite) return;
        if(CheckIfMinimumTimeHasPassed(ref _minWaitToSave, 15000))
            SaveInvitedPlayers();
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Client is not { IsLoggedIn: true } || Condition[ConditionFlag.BoundByDuty]) return;
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Objects is null) return;

        for (var i = 0; i < Objects.Length; i++)
        {
            var gameObject = Objects.CreateObjectReference(Objects.GetObjectAddress(i));
            if (gameObject?.IsValid() != true || gameObject.ObjectKind != ObjectKind.Player) continue;
            var player = gameObject as PlayerCharacter;
            //Don't invite people that are not from your World
            if (player.HomeWorld.Id != Client.LocalPlayer.HomeWorld.Id) continue;
            //Don't invite bots Archer/Bard/Unclassed
            if (!PluginConfig.checkBoxBardInvite)
                if (player.ClassJob.Id is 23 or 0 or 5)
                    continue;
            //Anti Spam Bots
            if(player.Level < 5) continue;
            //Don't invite people that are not sprouts
            if (player.OnlineStatus.Id != 32) continue;
            //Don't annoy people with double invites
            if (_invitedPlayers.Contains(player.Name.TextValue.Trim())) continue;
            //Only invite people in a certain range
            if (!IsPlayerWithinDistance(gameObject, PluginConfig.sliderMaxInviteRange)) continue;
            if (!CheckIfMinimumTimeHasPassed(ref _minWaitToCheck, PluginConfig.sliderTimeBetweenInvites)) continue;
            PluginLog.Log("Invited " + player.Name.TextValue);
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