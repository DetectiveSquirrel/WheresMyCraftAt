﻿using ExileCore;
using ExileCore.Shared;
using ExileCore.Shared.Nodes;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using WheresMyCraftAt.CraftingSequence;
using WheresMyCraftAt.Handlers;
using static WheresMyCraftAt.CraftingSequence.CraftingSequence;
using Vector2N = System.Numerics.Vector2;

namespace WheresMyCraftAt
{
    public partial class WheresMyCraftAt : BaseSettingsPlugin<WheresMyCraftAtSettings>
    {
        public readonly Dictionary<SpecialSlot, Vector2N> specialSlotDimensionMap = new()
        {
            { SpecialSlot.CurrencyTab, new Vector2N(126f, 252f) },
            { SpecialSlot.EssenceTab, new Vector2N(127.2f, 254.4f) }
        };

        private CancellationTokenSource _operationCts;
        public SyncTask<bool> _currentOperation;
        public Vector2 ClickWindowOffset;
        public int ServerLatency;
        public List<CraftingStep> SelectedCraftingSteps = [];
        public static WheresMyCraftAt Main;

        public WheresMyCraftAt()
        {
            Name = "Wheres My Craft At";
            _operationCts = new CancellationTokenSource();
        }

        public override bool Initialise()
        {
            Main = this;
            RegisterHotkey(Settings.RunButton);

            return true;
        }

        private static void RegisterHotkey(HotkeyNode hotkey)
        {
            Input.RegisterKey(hotkey);
            hotkey.OnValueChanged += () => Input.RegisterKey(hotkey);
        }

        public override Job Tick()
        {
            ClickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            ServerLatency = GameController.IngameState.ServerData.Latency;

            if (!GameHandler.IsInGameCondition())
            {
                Stop();
                return null;
            }

            if (Settings.RunButton.PressedOnce())
            {
                if (_currentOperation is not null)
                {
                    // Imediate cancelation called, release all buttons.
                    // TODO: Get some help on how the hell this works.
                    Stop();
                }
                else
                {
                    Logging.Add($"{Name}: Attempting to Start New Operation.", LogMessageType.Info);
                    ResetCancellationTokenSource();
                    _currentOperation = AsyncStart(_operationCts.Token);
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

                Logging.Add($"{Name}: Stop() has been ran.", LogMessageType.Warning);
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

        private async SyncTask<bool> AsyncStart(CancellationToken token)
        {
            if (!GameHandler.IsInGameCondition())
            {
                Logging.Add($"{Name}: Not in game, operation will be terminated.", LogMessageType.Error);
                return false;
            }

            try
            {
                bool isInvOpen = await InventoryHandler.AsyncWaitForInventoryOpen(token);
                bool isStashOpen = await StashHandler.AsyncWaitForStashOpen(token);

                if (!isStashOpen || !isInvOpen)
                    return false;

                var giveItems = new CraftingSequenceExecutor(SelectedCraftingSteps);

                if (!await giveItems.Execute(_operationCts.Token))
                    return false;

                Logging.Add($"{Name}: AsyncStart() Completed.", LogMessageType.Success);
            }
            catch (OperationCanceledException)
            {
                Stop();
                return false;
            }

            return true;
        }

        public override void DrawSettings()
        {
            base.DrawSettings();

            CraftingSequenceMenu.Draw();
        }

        public override void Render()
        {
            base.Render();
            Logging.Render();
        }
    }
}