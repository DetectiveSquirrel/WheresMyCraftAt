﻿using ExileCore;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using static WheresMyCraftAt.WheresMyCraftAt;
using Color = SharpDX.Color;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace WheresMyCraftAt.Handlers
{
    public static class Logging
    {
        private class DebugMsgDescription
        {
            public string Msg { get; init; }
            public DateTime Time { get; init; }
            public Vector4 ColorV4 { get; init; }
            public Color Color { get; init; }
        }

        private static readonly object locker = new();
        private static List<DebugMsgDescription> MessagesList;
        private static Vector2 position;

        public static void Init()
        {
            MessagesList = new List<DebugMsgDescription>(24);
            Main.Graphics.InitImage("menu-background.png");
        }

        public static void Render()
        {
            if (!Main.Settings.ShowLogWindow)
                return;

            using var fontPush = Main.Graphics.UseCurrentFont();

            ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.Once);
            ImGui.SetNextWindowSize(new Vector2(600, 1000), ImGuiCond.Once);
            ImGui.Begin("WheresMyCraftAt Logs");

            foreach (var msg in MessagesList)
            {
                if (msg == null) continue;
                ImGui.PushStyleColor(ImGuiCol.Text, msg.ColorV4);
                ImGui.TextUnformatted($"{msg.Time.ToLongTimeString()}: {msg.Msg}");
                ImGui.PopStyleColor();
            }

            ImGui.End();
        }

        public static void Add(string msg, LogMessageType messageType)
        {
            try
            {
                Color color = Main._logMessageColors[messageType];
                var time = Main.Settings.DebugPrintLingerTime;

                if (Main.Settings.DebugPrint)
                    DebugWindow.LogMsg(msg, Main.Settings.DebugPrintLingerTime, color);

                var debugMsgDescription = new DebugMsgDescription
                {
                    Msg = msg,
                    Time = DateTime.Now,
                    ColorV4 = color.ToImguiVec4(),
                    Color = color
                };

                lock (locker)
                {
                    MessagesList.Add(debugMsgDescription);
                }
            }
            catch (Exception e)
            {
                DebugWindow.LogError($"{nameof(DebugWindow)} -> {e}");
            }
        }
    }
}