using System.Numerics;
using Dalamud.Configuration;
using ImGuiNET;

namespace NoviceInviterReborn
{
    public class NoviceInviterConfig : IPluginConfiguration
    {
        public int Version { get; set; }

        [NonSerialized] private NoviceInviterReborn plugin;

        public bool enableInvite = false;
        public float sliderMaxInviteRange = 200.0f;
        public int sliderTimeBetweenInvites = 500;
        public bool checkBoxDoNotInvite = false;
        public bool sendInviteConfirmationOpen = false;
        public bool clearInviteConfirmationOpen = false;

        public void Init(NoviceInviterReborn plugin)
        {
            this.plugin = plugin;
        }

        public void Save()
        {
            plugin.PluginInterface.SavePluginConfig(this);
        }

        public bool DrawConfigUI()

        {
            var drawConfig = true;

            var windowFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;

            ImGui.GetBackgroundDrawList();

            ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);

            ImGui.Begin($"{plugin.Name} UI", ref drawConfig, windowFlags);

            var changed = false;

            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFAAAAAA);

            ImGui.Text("General Settings:");

            ImGui.PopStyleColor();

            ImGui.Indent(10);

            changed |= ImGui.Checkbox("Enable", ref enableInvite);

            ImGui.SameLine();

            ImGui.TextDisabled("[Enable] or [Disable] the Novice Inviter Plugin");

            ImGui.Separator();

            ImGui.Unindent(10);

            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFAAAAAA);

            ImGui.Text("Invite Settings:");

            ImGui.PopStyleColor();

            ImGui.Indent(10);

            changed |= ImGui.SliderFloat("Max invite range", ref sliderMaxInviteRange, 0.0f, 200.0f);

            if (ImGui.IsItemHovered())

            {

                ImGui.SetTooltip("Set the max range for inviting people. 0 - 200");

            }

            ImGui.Separator();

            changed |= ImGui.SliderInt("Time between invites", ref sliderTimeBetweenInvites, 500, 30000);

            if (ImGui.IsItemHovered())

            {

                ImGui.SetTooltip("Set the time between each invite. 500 - 30000");

            }

            ImGui.Separator();

            ImGui.Unindent(10);

            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFAAAAAA);

            ImGui.Text("Anti Bot Settings:");

            ImGui.PopStyleColor();

            ImGui.Indent(10);

            changed |= ImGui.Checkbox("Do you want to invite possible bots? (Not recommended)", ref checkBoxDoNotInvite);

            ImGui.Separator();
            ImGui.Text($"Total players invited: {plugin.InvitedPlayersAmount()}");
            ImGui.Text($"Total players in list: {plugin.GetPlayerSearchAmount()}");
            ImGui.Separator();

            if (changed)

            {

                Save();

            }

            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF5E5BFF);

            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF5E5BAA);

            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF5E5BDD);

            ImGui.PopStyleColor(3);

            if (ImGui.Button("Send Mass Invitation"))
            {
                sendInviteConfirmationOpen = true;
            }

            ImGui.SameLine();

            if (ImGui.Button("Clear Invitation List"))
            {
                clearInviteConfirmationOpen = true;
            }

            if (sendInviteConfirmationOpen)
            {
                ImGui.OpenPopup("SendInviteConfirmation");
            }

            if (ImGui.BeginPopupModal("SendInviteConfirmation", ref sendInviteConfirmationOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("*WARNING* This will invite hundred of sprouts at once do you wanna continue?");
                ImGui.Separator();

                if (ImGui.Button("Yes"))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            if (plugin._isActive)
                                return;

                            plugin._isActive = true;
                            plugin.PlayerSearchClearList();
                            plugin.EnableSearchNop();
                            Thread.Sleep(2000);
                            var oldPlayers = plugin.GetPlayerSearchAmount();
                            for (int i = 1; i <= 11; i++)
                            {
                                plugin.PluginLog.Information("Executing Search Area: " + i);
                                plugin.SendExecuteSearch(i);
                                while (true)
                                {
                                    Thread.Sleep(2000);
                                    plugin.PluginLog.Information("Current players: " + plugin.GetPlayerSearchAmount() + " - Old players: " + oldPlayers);
                                    if (oldPlayers == plugin.GetPlayerSearchAmount())
                                    {
                                        break;
                                    }
                                    oldPlayers = plugin.GetPlayerSearchAmount();
                                }
                            }
                            plugin.DisableSearchNop();
                            Thread.Sleep(2000);
                            plugin.SendPlayerSearchInvites();
                            plugin._isActive = false;
                        }
                        catch (Exception)
                        {
                            plugin.DisableSearchNop();
                            plugin._isActive = false;
                        }
                    });
                    ImGui.CloseCurrentPopup();
                    sendInviteConfirmationOpen = false;
                }
                ImGui.SetItemDefaultFocus();
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                    sendInviteConfirmationOpen = false;
                }
                ImGui.EndPopup();
            }

            if (clearInviteConfirmationOpen)
            {
                ImGui.OpenPopup("ClearInviteConfirmation");
            }

            if (ImGui.BeginPopupModal("ClearInviteConfirmation", ref clearInviteConfirmationOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Are you sure you want to clear the invite list?");
                ImGui.Separator();

                if (ImGui.Button("Yes"))
                {
                    plugin.PlayerSearchClearList();
                    ImGui.CloseCurrentPopup();
                    clearInviteConfirmationOpen = false;
                }
                ImGui.SetItemDefaultFocus();
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                    clearInviteConfirmationOpen = false;
                }
                ImGui.EndPopup();
            }

            ImGui.End();

            return drawConfig;

        }

    }
}
