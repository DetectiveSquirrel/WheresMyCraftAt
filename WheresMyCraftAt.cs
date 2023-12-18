﻿using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Nodes;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using WheresMyCraftAt.Handlers;
using Vector2N = System.Numerics.Vector2;

namespace WheresMyCraftAt
{
    public partial class WheresMyCraftAt : BaseSettingsPlugin<WheresMyCraftAtSettings>
    {
        public readonly Dictionary<LogMessageType, Color> _logMessageColors = new()
        {
            { LogMessageType.Info, Color.White },
            { LogMessageType.Warning, Color.Yellow },
            { LogMessageType.Error, Color.Red },
            { LogMessageType.Success, Color.Green },
            { LogMessageType.Cancelled, Color.Orange },
            { LogMessageType.Timeout, Color.Gray }
        };

        public readonly Dictionary<SpecialSlot, Vector2N> specialSlotDimensionMap = new()
        {
            { SpecialSlot.CurrencyTab, new Vector2N(126f, 252f) },
            { SpecialSlot.EssenceTab, new Vector2N(127.2f, 254.4f) }
        };

        private CancellationTokenSource _operationCts;
        private SyncTask<bool> _currentOperation;
        public Vector2 _clickWindowOffset;
        public int ServerLatency;

        public WheresMyCraftAt()
        {
            Name = "Wheres My Craft At";
            _operationCts = new CancellationTokenSource();
        }

        public override bool Initialise()
        {
            RegisterHotkey(Settings.TestButton1);

            ElementHandler.Initialize(this);
            ExecuteHandler.Initialize(this);
            GameHandler.Initialize(this);
            InventoryHandler.Initialize(this);
            ItemHandler.Initialize(this);
            KeyHandler.Initialize(this);
            MouseHandler.Initialize(this);
            StashHandler.Initialize(this);

            return true;
        }

        private static void RegisterHotkey(HotkeyNode hotkey)
        {
            Input.RegisterKey(hotkey);
            hotkey.OnValueChanged += () => Input.RegisterKey(hotkey);
        }

        public override void Render()
        {
            if (StashHandler.TryGetStashSpecialSlot(SpecialSlot.CurrencyTab, out var foundItem))
            {
                Graphics.DrawText(ItemHandler.GetBaseNameFromItem(foundItem), new Vector2N(800, 600));
            }
            else
            {
                Graphics.DrawText("None", new Vector2N(800, 600));
            }
        }

        public override Job Tick()
        {
            _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            ServerLatency = GameController.IngameState.ServerData.Latency;

            if (!GameHandler.IsInGameCondition())
            {
                Stop();
                return null;
            }

            if (Settings.TestButton1.PressedOnce())
            {
                if (_currentOperation is not null)
                {
                    // Imediate cancelation called, release all buttons.
                    Stop();
                }
                else
                {
                    DebugPrint($"{Name}: Attempting to Start New Operation.", LogMessageType.Info);
                    ResetCancellationTokenSource();
                    _currentOperation = AsyncTestButton1Main(_operationCts.Token);
                }
            }

            if (_currentOperation is not null)
                TaskUtils.RunOrRestart(ref _currentOperation, () => null);

            return null;
        }

        private void Stop()
        {
            if (_currentOperation is not null)
            {
                _currentOperation = null;

                var keysToRelease = new List<Keys>
                {
                    Keys.LControlKey,
                    Keys.ShiftKey,
                    Keys.LButton,
                    Keys.RButton
                };

                foreach (var key in keysToRelease)
                    if (Input.GetKeyState(key))
                        Input.KeyUp(key);

                if (ItemHandler.IsItemRightClickedCondition())
                    Input.KeyPressRelease(Keys.Escape);
            }
        }

        private void ResetCancellationTokenSource()
        {
            if (_operationCts != null)
            {
                if (!_operationCts.IsCancellationRequested)
                {
                    _operationCts.Cancel();
                }
                _operationCts.Dispose();
            }
            _operationCts = new CancellationTokenSource();
        }

        private async SyncTask<bool> AsyncTestButton1Main(CancellationToken token)
        {
            if (!GameHandler.IsInGameCondition())
            {
                DebugPrint($"{Name}: Not in game, operation will be terminated.", LogMessageType.Error);
                return false;
            }

            try
            {
                bool isInvOpen = await InventoryHandler.AsyncWaitForInventoryOpen(token);
                bool isStashOpen = await StashHandler.AsyncWaitForStashOpen(token);

                if (!isStashOpen || !isInvOpen)
                    return false;

                if (!await MouseHandler.AsyncMoveMouse(new Vector2N(Settings.MouseMoveX, Settings.MouseMoveY), token))
                    return false;

                if (!await KeyHandler.AsyncButtonPress(Keys.RButton, token))
                    return false;

                if (!await ItemHandler.AsyncWaitForRightClickedItemOnCursor(token))
                    return false;

                if (!await KeyHandler.AsyncButtonPress(Keys.RButton, token))
                    return false;

                if (!await ItemHandler.AsyncWaitForRightClickedItemOffCursor(token))
                {
                    await KeyHandler.AsyncButtonPress(Keys.Escape, token);
                    await GameHandler.AsyncWaitServerLatency(token);
                    return false;
                }

                DebugPrint($"{Name}: AsyncTestButton1Main() Completed.", LogMessageType.Success);
            }
            catch (OperationCanceledException)
            {
                Stop();
                return false;
            }

            return true;
        }

        public void DebugPrint(string printString, LogMessageType messageType)
        {
            if (Settings.DebugPrint)
            {
                Color messageColor = _logMessageColors[messageType];
                DebugWindow.LogMsg(printString, Settings.DebugPrintLingerTime, messageColor);
            }
        }
    }
}