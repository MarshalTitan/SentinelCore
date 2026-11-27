using System.Numerics;
using Dalamud.Bindings.ImGui;
using SentinelCore.UI;

internal static class NavigationRenderTests
{
    public static void Run()
    {
        foreach (var scale in new[] { 0.8f, 1f, 1.5f, 2f })
        foreach (var fontScale in new[] { 1f, 1.4f })
        foreach (var logicalSize in new[] { new Vector2(480, 520), new Vector2(680, 720), new Vector2(1100, 1000) })
            Render(scale, fontScale, logicalSize);
    }

    private static unsafe void Render(float scale, float fontScale, Vector2 logicalSize)
    {
        var context = ImGui.CreateContext();
        try
        {
            var io = ImGui.GetIO();
            io.IniFilename = null;
            io.LogFilename = null;
            io.DisplaySize = new Vector2(3200, 2400);
            io.DeltaTime = 1f / 60f;
            io.Fonts.AddFontDefault();
            if (!io.Fonts.Build()) throw new Exception("Font atlas failed.");
            io.FontGlobalScale = fontScale * scale;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard | ImGuiConfigFlags.NavEnableGamepad;
            io.BackendFlags |= ImGuiBackendFlags.HasGamepad;
            using var style = new SentinelModernStyleScope();
            using var state = new SentinelModernAppShellState();
            var selected = "Main";
            var minimized = false;
            var closes = 0;
            SentinelModernNavItem[] items = [new("Main", "M", "Main"), new("Plugins", "P", "Plugins"), new("Distances", "D", "Distance Profiles")];
            void Frame()
            {
                ImGui.NewFrame();
                style.PushAppShell(scale);
                ImGui.SetNextWindowPos(new Vector2(20), ImGuiCond.Always);
                ImGui.SetNextWindowSize(logicalSize * scale, ImGuiCond.Always);
                ImGui.Begin("Navigation rendering", SentinelModernWindowChrome.UseCustomHeader(ImGuiWindowFlags.NoSavedSettings));
                try
                {
                    SentinelModernAppShell.Draw(new("render", "S Rank Sentinel", selected) {
                        Scale = scale, PluginGlyph = "S", ReducedMotion = true, IsCollapsed = minimized,
                        Status = new("ENABLED", SentinelModernPillTone.Enabled),
                        RequestCollapse = () => minimized = !minimized, RequestClose = () => closes++,
                    }, state, items, id => selected = id, () => ImGui.TextUnformatted("Content"));
                }
                finally { ImGui.End(); style.Pop(); }
                ImGui.Render();
                if (ImGui.GetDrawData().TotalVtxCount == 0) throw new Exception("Empty navigation geometry.");
            }
            Frame(); Frame();
            var origin = new Vector2(20);
            var header = SentinelModernAppLayoutOptions.Default.HeaderHeight * scale;
            var rail = SentinelModernAppLayoutOptions.Default.RailWidth * scale;
            var button = SentinelModernAppLayoutOptions.Default.RailButtonSize * scale;
            var pluginsCenter = origin + new Vector2(rail * 0.5f, header + 10f * scale + button * 1.5f + 8f * scale);
            io.AddMousePosEvent(pluginsCenter.X, pluginsCenter.Y); Frame(); Frame();
            io.AddMouseButtonEvent(0, true); Frame();
            io.AddMouseButtonEvent(0, false); Frame();
            if (selected != "Plugins") throw new Exception("Rail click did not select Plugins.");
            io.AddMousePosEvent(-100, -100); Frame();
            if (selected != "Plugins") throw new Exception("Selection depended on hover.");
            var closeCenter = origin + new Vector2(logicalSize.X * scale - 29f * scale, header * 0.5f);
            var minimizeCenter = closeCenter - new Vector2(36f * scale, 0);
            void Click(Vector2 point)
            {
                io.AddMousePosEvent(point.X, point.Y); Frame();
                io.AddMouseButtonEvent(0, true); Frame();
                io.AddMouseButtonEvent(0, false); Frame();
            }
            Click(minimizeCenter);
            if (!minimized || closes != 0) throw new Exception("Minimize action changed.");
            Click(minimizeCenter);
            if (minimized) throw new Exception("Restore action changed.");
            Click(closeCenter);
            if (closes != 1) throw new Exception("Close action changed.");
            // The same public bubble renderer must clamp a long label at the viewport edge.
            ImGui.NewFrame();
            ImGui.SetNextWindowPos(new Vector2(3070, 2320));
            ImGui.SetNextWindowSize(new Vector2(120, 70));
            ImGui.Begin("Tooltip edge");
            ImGui.Button("P");
            SentinelModernNavigationPresentation.Tooltip("Plugins - Distance Profiles", scale, true);
            ImGui.End();
            ImGui.Render();
            var ctx = ImGui.GetCurrentContext();
            for (var i = 0; i < ctx.Windows.Size; i++)
            {
                var window = ctx.Windows[i];
                if (!window.Active || !window.Flags.HasFlag(ImGuiWindowFlags.Tooltip)) continue;
                if (window.Pos.X < 0 || window.Pos.Y < 0 ||
                    window.Pos.X + window.Size.X > io.DisplaySize.X + 1 ||
                    window.Pos.Y + window.Size.Y > io.DisplaySize.Y + 1)
                    throw new Exception("Tooltip extends beyond the viewport.");
            }
        }
        finally { ImGui.DestroyContext(context); }
    }
}
