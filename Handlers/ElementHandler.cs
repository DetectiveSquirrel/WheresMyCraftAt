﻿using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System;
using System.Threading;
using WheresMyCraftAt.Extensions;
using static WheresMyCraftAt.Enums.WheresMyCraftAt;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class ElementHandler
{
    public static Element GetHoveredElementUiAction()
    {
        return Main.GameController?.Game?.IngameState?.UIHoverElement;
    }

    public static bool IsElementsSameCondition(Element first, Element second)
    {
        return first.Address == second.Address;
    }

    public static bool IsInGameUiElementOpenCondition(Func<IngameUIElements, Element> panelSelector)
    {
        return panelSelector(Main.GameController?.Game?.IngameState?.IngameUi)?.IsVisible ?? false;
    }

    /*
     * Note: State doesnt change if you run out of the currency you were using if holding shift
     * TODO: Must check before each click that we have that item in the stash tab when this is called (Solve performance issues at a later date for this)
     */

    public static bool TryGetCursorStateCondition(out MouseActionType cursorState)
    {
        var gameController = Main.GameController;

        if (gameController?.Game?.IngameState?.IngameUi?.Cursor != null)
        {
            cursorState = gameController.Game.IngameState.IngameUi.Cursor.Action;
            return true;
        }

        cursorState = MouseActionType.HoldItemForSell;
        return false;
    }

    public static async SyncTask<bool> AsyncExecuteNotSameItemWithCancellationHandling(long itemToChange, int timeoutS,
        CancellationToken token)
    {
        using var ctsTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        ctsTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutS));

        try
        {
            while (!ctsTimeout.Token.IsCancellationRequested)
            {
                await GameHandler.AsyncWait(HelperHandler.GetRandomTimeInRange(Main.Settings.MinMaxRandomDelay), token);
                var hoveredEntity = GetHoveredElementUiAction().Entity.Address;

                if (HelperHandler.IsAddressSameCondition(itemToChange, hoveredEntity))
                {
                    Logging.Logging.Add(
                        "AsyncExecuteNotSameItemWithCancellationHandling: Item address is the same. Waiting for change.",
                        LogMessageType.Info
                    );

                    continue;
                }

                Logging.Logging.Add(
                    "AsyncExecuteNotSameItemWithCancellationHandling: Item address has changed.",
                    LogMessageType.Info
                );

                return true;
            }

            Logging.Logging.Add(
                "AsyncExecuteNotSameItemWithCancellationHandling: Timeout occurred.",
                LogMessageType.Warning
            );

            return false;
        }
        catch (OperationCanceledException)
        {
            Logging.Logging.Add(
                "AsyncExecuteNotSameItemWithCancellationHandling: Operation canceled.",
                LogMessageType.Info
            );

            return false;
        }
    }

    public static async SyncTask<bool> AsyncTryApplyOrb(this NormalInventoryItem item, string currencyName,
        CancellationToken token)
    {
        try
        {
            var (item1, orbItem) = await StashHandler.AsyncTryGetItemInStash(currencyName, token);

            if (!item1)
            {
                Logging.Logging.Add("AsyncTryApplyOrb: Required orb item not found in stash.", LogMessageType.Error);
                Main.Stop();
                return false;
            }

            var storeAddressOfItem = item.Item.Address;

            Logging.Logging.Add(
                $"AsyncTryApplyOrb: Store address of item is {storeAddressOfItem:X}.",
                LogMessageType.Info
            );

            if (!await orbItem.AsyncTryClick(true, token))
            {
                Logging.Logging.Add("AsyncTryApplyOrb: Failed to click orb item.", LogMessageType.Error);
                Main.Stop();
                return false;
            }

            if (!await item.AsyncTryClick(false, token))
            {
                Logging.Logging.Add("AsyncTryApplyOrb: Failed to click target item.", LogMessageType.Error);
                Main.Stop();
                return false;
            }

            if (!await AsyncExecuteNotSameItemWithCancellationHandling(
                    storeAddressOfItem,
                    Main.Settings.ActionTimeoutInSeconds,
                    token
                ))
            {
                Logging.Logging.Add("AsyncTryApplyOrb: Item did not change after applying orb.", LogMessageType.Error);
                Main.Stop();
                return false;
            }

            Logging.Logging.Add("AsyncTryApplyOrb: Orb successfully applied to item.", LogMessageType.Info);
            return true;
        }
        catch (OperationCanceledException)
        {
            Logging.Logging.Add("AsyncTryApplyOrb: Operation canceled.", LogMessageType.Info);
            return false;
        }
    }
}