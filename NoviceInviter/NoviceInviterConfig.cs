using System.Numerics;
using Dalamud.Configuration;
using ImGuiNET;

namespace NoviceInviter
{
    public class NoviceInviterConfig : IPluginConfiguration
    {
        public int Version { get; set; }

        [NonSerialized] private NoviceInviter plugin;

        public bool enableInvite = false;
        public float sliderMaxInviteRange = 100.0f;
        public int sliderTimeBetweenInvites = 500;
        public bool checkBoxBardInvite = false;
        public bool SendInviteBool { get; set; }

        public void Init(NoviceInviter plugin)
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

            ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);
            ImGui.Begin($"{plugin.Name} UI", ref drawConfig, windowFlags);

            var changed = false;

            ImGui.Text("Novice Inviter Plugin Settings");
            ImGui.Separator();

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
            changed |= ImGui.Checkbox("Do you want to invite Archer/Bard? (Possible Bots)", ref checkBoxBardInvite);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Check to invite Archer/Bard bots");
            }
            ImGui.Unindent(10);

            ImGui.Separator();

            if (changed)
            {
                Save();
            }

            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF5E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xFF5E5BAA);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xFF5E5BDD);
            ImGui.PopStyleColor(3);

            if (ImGui.Button("Send Invite"))
            {
                Task.Run(() => plugin.SendPlayerSearchInvites());
            }

            if (ImGui.Button("Clear Invite"))
            {
                plugin.playersToInvite.Clear();
            }

            ImGui.End();

            return drawConfig;
        }

    }
}
