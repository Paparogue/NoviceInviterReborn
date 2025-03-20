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
        public bool sendInviteBool { get; set; }

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

            changed |= ImGui.Checkbox("Do you want to invite possible Bot Jobs? (Not recommended)", ref checkBoxDoNotInvite);

            ImGui.Separator();
            ImGui.Text($"Total players invited: {plugin.InvitedPlayersAmount()}");
            ImGui.Text($"Total players in list: {plugin.PlayerSearchAmount()}");
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
                ImGui.Text("Are you sure you want to send the invites?");
                ImGui.Separator();

                if (ImGui.Button("Yes"))
                {
                    Task.Run(async () => {
                        try
                        {
                            plugin.PlayerSearchClearList();
                            plugin.EnableSearchNop();
                            for (int i = 1; i <= 11; i++)
                            {
                                plugin.SendExecuteSearch(i);

                                if (i < 11)
                                    await Task.Delay(5000);
                            }
                            plugin.DisableSearchNop();
                            //plugin.SendPlayerSearchInvites();
                        }
                        catch (Exception ex)
                        {
                            plugin.DisableSearchNop();
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
