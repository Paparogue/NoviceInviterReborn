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
using System.Reflection;
using Lumina.Excel;
using static NoviceInviterReborn.PlayerSearch;
using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System.Runtime.Intrinsics.X86;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI;
using Dalamud;

#pragma warning disable CA1816
#pragma warning disable CS8602

namespace NoviceInviterReborn;

public class NoviceInviterReborn : IDalamudPlugin
{
    public string Name => "NoviceInviterReborn";
    public NoviceInviterConfig PluginConfig { get; private set; }
    private bool drawConfigWindow;

    private delegate char NoviceInviteDelegate(IntPtr unknownFunction, IntPtr unknownFunction2, short worldID, IntPtr playerName, byte always0x8);
    private delegate char ExecuteSearchDelegate(IntPtr agent, IntPtr agent_plus_0x48, byte always0x0);
    private delegate void PlayerSearchDelegate(IntPtr unknownFunction, IntPtr playerArray, uint always0xA);

    private NoviceInviteDelegate _noviceInvite;
    private ExecuteSearchDelegate _executeSearch;

    private Hook<PlayerSearchDelegate> _playerSearchHook = null!;
    private readonly List<String> _playerSearchList = [];

    private readonly List<string> _alreadyInvitedPlayers = [];
    private readonly string _invitedPlayersPath;

    private DateTime? _minWaitToCheck = DateTime.UtcNow;
    private DateTime? _minWaitToSave = DateTime.UtcNow;

    private IntPtr _socialPanel = IntPtr.Zero;
    private byte[] _originalPatchBytes = null!;
    private bool _isPatchApplied = false;
    public bool _isActive = false;

    public IDalamudPluginInterface PluginInterface { get; init; }
    public IClientState Client { get; init; }
    public ISigScanner SigScanner { get; init; }
    public IObjectTable Objects { get; init; }
    public ICommandManager CommandManager { get; init; }
    public ICondition Condition { get; init; }
    public IGameInteropProvider DalamudHook { get; init; }

    public IPluginLog PluginLog { get; init; }

    public unsafe NoviceInviterReborn(
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
        _invitedPlayersPath = Path.Combine(PluginInterface.GetPluginConfigDirectory(), "player.inv");
        SetupCommands();
        LoadInvitedPlayers();
        InitHooknSigs();
        PluginInterface.UiBuilder.Draw += BuildUI;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += OpenConfigUi;
        PluginInterface.UiBuilder.Draw += OnUpdate;
    }

    public void Dispose()
    {
        _playerSearchHook.Disable();
        _playerSearchHook.Dispose();
        if (_isPatchApplied)
            DisableSearchNop();
        PluginInterface.UiBuilder.Draw -= BuildUI;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= OpenConfigUi;
        PluginInterface.UiBuilder.Draw -= OnUpdate;
        RemoveCommands();
    }

    private void InitHooknSigs()
    {
        var noviceSigPtr = SigScanner.ScanText("E8 ?? ?? ?? ?? EB 40 41 B1 09");
        _noviceInvite = Marshal.GetDelegateForFunctionPointer<NoviceInviteDelegate>(noviceSigPtr);

        var playerSearchSigPtr = SigScanner.ScanText("E8 ?? ?? ?? ?? 49 8B 4F ?? 48 8B 01 FF 50 ?? 41 0F B6 97");
        if (playerSearchSigPtr != IntPtr.Zero)
        {
            _playerSearchHook = DalamudHook.HookFromAddress<PlayerSearchDelegate>(playerSearchSigPtr, PlayerSearchDetour);
            _playerSearchHook.Enable();
        }

        var executeSearchPtr = SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 41 C7 07 ?? ?? ?? ?? 41 C6 47");
        _executeSearch = Marshal.GetDelegateForFunctionPointer<ExecuteSearchDelegate>(executeSearchPtr);

        try
        {
            var openSocialPanelPtr = "?? E8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 0F 85 ?? ?? ?? ?? 48 8B 4B";
            _socialPanel = SigScanner.ScanText(openSocialPanelPtr);
            _socialPanel += 0x1;

            if (_socialPanel != IntPtr.Zero)
            {
                PluginLog.Information($"Found patch signature at 0x{_socialPanel.ToInt64():X}");
                _originalPatchBytes = new byte[5];
                Marshal.Copy(_socialPanel, _originalPatchBytes, 0, 5);

                PluginLog.Debug($"Original bytes: {BitConverter.ToString(_originalPatchBytes)}");
            }
            else
            {
                PluginLog.Error("Failed to find patch signature");
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Error scanning for patch signature");
        }
    }

    public int InvitedPlayersAmount()
    {
        return _alreadyInvitedPlayers.Count;
    }

    public int GetPlayerSearchAmount()
    {
        return _playerSearchList.Count;
    }

    public void PlayerSearchClearList()
    {
        _playerSearchList.Clear();
    }

    private void PlayerSearchDetour(IntPtr globalFunction, IntPtr playerArray, uint always0xA)
    {
        IntPtr playerArrayBeginning = playerArray + 0x44;
        for (int i = 0; i < 10; i++)
        {
            var playerData = Marshal.PtrToStructure<PlayerSearch.PlayerData>(playerArrayBeginning);
            if (IsValidPlayerName(playerData.PlayerName) && _isActive)
            {
                _playerSearchList.Add(playerData.PlayerName.Trim());
            }
            else
            {
                break;
            }

            playerArrayBeginning += 0x70;
        }

        _playerSearchHook.Original(globalFunction, playerArray, always0xA);
    }

    public void EnableSearchNop()
    {
        if (_socialPanel == IntPtr.Zero || _originalPatchBytes == null || _isPatchApplied)
        {
            PluginLog.Warning("Cannot apply patch: address not found, original bytes not saved, or patch already applied");
            return;
        }

        try
        {
            byte[] nopPatch = new byte[] { 0x90, 0x90, 0x90, 0x90, 0x90 };
            SafeMemory.WriteBytes(_socialPanel, nopPatch);

            PluginLog.Information($"Applied NOP patch at 0x{_socialPanel.ToInt64():X}");
            _isPatchApplied = true;
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Error applying patch");
        }
    }

    public void DisableSearchNop()
    {
        if (_socialPanel == IntPtr.Zero || _originalPatchBytes == null || !_isPatchApplied)
        {
            PluginLog.Warning("Cannot restore patch: address not found, original bytes not saved, or patch not applied");
            return;
        }

        try
        {
            SafeMemory.WriteBytes(_socialPanel, _originalPatchBytes);
            PluginLog.Information($"Restored original bytes at 0x{_socialPanel.ToInt64():X}");
            _isPatchApplied = false;
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Error restoring original bytes");
        }
    }
    public void SendPlayerSearchInvites()
    {
        foreach (var player in _playerSearchList)
        {
            string worldName = Client.LocalPlayer.CurrentWorld.Value.Name.ToString();
            if (_alreadyInvitedPlayers.Contains(player.Trim() + "-" + worldName))
                continue;
            SendNoviceInvite(player, (short)Client.LocalPlayer.CurrentWorld.RowId);
            _alreadyInvitedPlayers.Add(player.Trim() + "-" + worldName);
            Thread.Sleep(200);
        }
        _playerSearchList.Clear();
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

    public unsafe void SendExecuteSearch(int region)
    {
        var moduleInstance = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentModule.Instance();
        IntPtr agent = (IntPtr)moduleInstance->GetAgentByInternalId(AgentId.Search);
        var backupData = AgentSearchExtensions.BackupValues(agent);

        try
        {
            var agentStruct = (AgentSearch*)agent;
            agentStruct->OnlineStatusLeft = 0; // always 0
            agentStruct->OnlineStatusRight = 3; // sprouts + returner
            if(!PluginConfig.checkBoxDoNotInvite)
            {
                agentStruct->ClassSearchRow1 = 223; //223
                agentStruct->ClassSearchRow3 = 107; //107
            } 
            else
            {
                agentStruct->ClassSearchRow1 = 255;
                agentStruct->ClassSearchRow3 = 255;
            }
            agentStruct->ClassSearchRow2 = 255;
            agentStruct->ClassSearchRow4 = 255; //255
            agentStruct->ClassSearchRow5 = 255; //255
            agentStruct->ClassSearchRow6 = 3; //3
            agentStruct->Language = 15; // include all languages
            agentStruct->Company = 0; // include all players regardless of company
            if (!PluginConfig.checkBoxDoNotInvite)
                agentStruct->MinLevel = 5; // too many bots here
            else
                agentStruct->MinLevel = 1;
            agentStruct->MaxLevel = 100;
            AgentSearchExtensions.SetOnlyOneRegion(agent, region);
            _executeSearch(agent, agent + 0x48, 0);
            PluginLog.Information($"Agent: 0x{((IntPtr)agent).ToInt64():X}");
        }
        finally
        {
            AgentSearchExtensions.RestoreValues(agent, backupData);
        }
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
        if (!File.Exists(_invitedPlayersPath))
            try
            {
                File.Create(_invitedPlayersPath).Close();
            }
            catch (Exception)
            {
                PluginLog.Warning("Could not create invited player file");
            }

        try
        {
            using var reader = new StreamReader(_invitedPlayersPath);
            while (reader.ReadLine() is { } line) _alreadyInvitedPlayers.Add(line);
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
            File.WriteAllLines(_invitedPlayersPath, _alreadyInvitedPlayers);
        }
        catch (Exception)
        {
            PluginLog.Warning("Could not save invited players file");
        }
    }


    private unsafe void OnUpdate()
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

                //too many bots and these people will level up fast
                if (player.Level < 5) continue;

                if (!PluginConfig.checkBoxDoNotInvite)
                    if (jobName.Equals("Archer") || jobName.Equals("Lancer") || jobName.Equals("Bard") || jobName.Equals("Marauder"))
                        continue;

                //is from different world
                if (player.CurrentWorld.Value.RowId != Client.LocalPlayer.CurrentWorld.RowId) continue;
                //is a sprout
                if (player.OnlineStatus.RowId != 32) continue;
                //was not already invited
                if (_alreadyInvitedPlayers is null || _alreadyInvitedPlayers.Contains(player.Name.TextValue.Trim() + "-" + worldName)) continue;
                //is within distance
                if (!IsPlayerWithinDistance(o, PluginConfig.sliderMaxInviteRange)) continue;
                //enough time passed between invite
                if (!CheckIfMinimumTimeHasPassed(ref _minWaitToCheck, PluginConfig.sliderTimeBetweenInvites)) continue;
                //invite player
                SendNoviceInvite(player.Name.TextValue, (short)player.HomeWorld.RowId);
                //dont invite them twice
                _alreadyInvitedPlayers.Add(player.Name.TextValue.Trim() + "-" + worldName);
            }
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "Error during OnUpdate.");
        }
    }


    public void SetupCommands()
    {
        CommandManager.AddHandler("/nir", new CommandInfo(OnConfigCommandHandler)
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
        CommandManager.RemoveHandler("/nir");
    }

    private void BuildUI()
    {
        drawConfigWindow = drawConfigWindow && PluginConfig.DrawConfigUI();
    }
}