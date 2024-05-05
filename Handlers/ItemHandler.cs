﻿using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using static WheresMyCraftAt.WheresMyCraftAt;

namespace WheresMyCraftAt.Handlers;

public static class ItemHandler
{
    public static async SyncTask<bool> AsyncTryApplyOrbToSpecialSlot(Enums.WheresMyCraftAt.SpecialSlot slot, string orbName, CancellationToken token)
    {
        Logging.Logging.Add($"Attempting to apply orb '{orbName}' to slot '{slot}'.", Enums.WheresMyCraftAt.LogMessageType.Info);

        var asyncResult = await StashHandler.AsyncTryGetStashSpecialSlot(slot, token);

        if (!asyncResult.Item1)
        {
            Logging.Logging.Add($"Failed to get stash slot '{slot}' for orb '{orbName}'.", Enums.WheresMyCraftAt.LogMessageType.Error);

            return false;
        }

        Logging.Logging.Add($"Stash slot '{slot}' retrieved successfully. Applying orb '{orbName}'.", Enums.WheresMyCraftAt.LogMessageType.Info);

        return await asyncResult.Item2.AsyncTryApplyOrb(orbName, token);
    }
    public static async SyncTask<bool> AsyncTryApplyOrbToSlot(Vector2 invSlot, string orbName, CancellationToken token)
    {
        Logging.Logging.Add($"Attempting to apply orb '{orbName}' to slot '{invSlot}'.", Enums.WheresMyCraftAt.LogMessageType.Info);

        var asyncResult = await InventoryHandler.AsyncTryGetInventoryItemFromSlot(invSlot, token);

        if (!asyncResult.Item1)
        {
            Logging.Logging.Add($"Failed to get inventory slot '{invSlot}' for orb '{orbName}'.", Enums.WheresMyCraftAt.LogMessageType.Error);

            return false;
        }

        Logging.Logging.Add($"Inventory slot '{invSlot}' retrieved successfully. Applying orb '{orbName}'.", Enums.WheresMyCraftAt.LogMessageType.Info);

        return await asyncResult.Item2.AsyncTryApplyOrb(orbName, token);
    }

    public static async SyncTask<bool> AsyncWaitForItemOnCursor(CancellationToken token, int timeout = 2)
    {
        Logging.Logging.Add("Waiting for an item to be on the cursor.", Enums.WheresMyCraftAt.LogMessageType.Info);

        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(InventoryHandler.IsAnItemPickedUpCondition, timeout,
            HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token);
    }

    public static async SyncTask<bool> AsyncWaitForNoItemOnCursor(CancellationToken token, int timeout = 2)
    {
        Logging.Logging.Add("Waiting for no item to be on the cursor.", Enums.WheresMyCraftAt.LogMessageType.Info);

        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(IsCursorFree, timeout, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token);
    }

    public static async SyncTask<bool> AsyncWaitForRightClickedItemOnCursor(CancellationToken token, int timeout = 2)
    {
        Logging.Logging.Add("Waiting for a right-clicked item to be on the cursor.", Enums.WheresMyCraftAt.LogMessageType.Info);

        return await ExecuteHandler.AsyncExecuteWithCancellationHandling(IsItemRightClickedCondition, timeout, HelperHandler.GetRandomTimeInRange(Main.Settings.DelayOptions.MinMaxRandomDelayMS), token);
    }

    public static List<string> GetHumanModListFromItem(Entity item) =>
        item.TryGetComponent<Mods>(out var modsComp) && modsComp.HumanStats.Count != 0 ? modsComp.HumanStats : [];

    public static void PrintHumanModListFromItem(Entity item)
    {
        Logging.Logging.Add($"-- Items Mods for: [{item.Path}] --", Enums.WheresMyCraftAt.LogMessageType.ItemData);
        Logging.Logging.Add($"-- Item Address: [{item.Address:X}] --", Enums.WheresMyCraftAt.LogMessageType.ItemData);
        Logging.Logging.Add("", Enums.WheresMyCraftAt.LogMessageType.ItemData);
        item.TryGetComponent<Mods>(out var modsComponent);

        if (modsComponent != null)
        {
            Logging.Logging.Add($"Rarity: {modsComponent.ItemRarity}", Enums.WheresMyCraftAt.LogMessageType.ItemData);
            Logging.Logging.Add("", Enums.WheresMyCraftAt.LogMessageType.ItemData);
        }
        else
        {
            Logging.Logging.Add($"Could not find <Mods> Component.", Enums.WheresMyCraftAt.LogMessageType.Error);
        }

        var modsList = GetHumanModListFromItem(item);

        if (modsList.Count != 0)
        {
            foreach (var itemMod in modsList) Logging.Logging.Add($"{itemMod}", Enums.WheresMyCraftAt.LogMessageType.ItemData);
        }
        else
        {
            Logging.Logging.Add("No mods found on the item.", Enums.WheresMyCraftAt.LogMessageType.ItemData);
        }

        Logging.Logging.Add("", Enums.WheresMyCraftAt.LogMessageType.ItemData);
        Logging.Logging.Add("--", Enums.WheresMyCraftAt.LogMessageType.ItemData);
    }

    public static void UpdateUsedItemDictionary(string currencyName)
    {
        Main.CurrentOperationUsedItemsList ??= [];

        if (Main.CurrentOperationUsedItemsList.TryGetValue(currencyName, out var currentCount))
        {
            Main.CurrentOperationUsedItemsList[currencyName] = currentCount + 1;
        }
        else
        {
            Main.CurrentOperationUsedItemsList[currencyName] = 1;
        }
    }

    public static bool HasCorrectMods(Entity item)
    {
        item.TryGetComponent<Mods>(out var modComp);
        if (modComp is not {Address: not 0})
        {
            return false;
        }

        switch (modComp.ItemRarity)
        {
            case ItemRarity.Normal:
                return modComp.ItemMods.Count < 1;
            case ItemRarity.Magic:
            case ItemRarity.Rare:
            case ItemRarity.Unique:
                return modComp.ItemMods.Count > 0;
        }

        return false;
    }

    public static string GetBaseNameFromItem(Entity item) => GetBaseNameFromPath(item?.Path);

    public static string GetBaseNameFromItem(NormalInventoryItem item) => GetBaseNameFromPath(item.Entity?.Path);

    public static string GetBaseNameFromItem(ServerInventory.InventSlotItem item) => GetBaseNameFromPath(item?.Item?.Path);

    public static string GetBaseNameFromPath(string path) =>
        Main.GameController?.Files.BaseItemTypes.Translate(path)?.BaseName ?? string.Empty;

    public static bool IsItemOnLeftClickCondition() =>
        ElementHandler.TryGetCursorStateCondition(out var cursorState) && cursorState == MouseActionType.HoldItem;

    public static bool IsItemRightClickedCondition() =>
        ElementHandler.TryGetCursorStateCondition(out var cursorState) && cursorState == MouseActionType.UseItem;

    public static bool IsCursorFree() =>
        ElementHandler.TryGetCursorStateCondition(out var cursorState) && cursorState == MouseActionType.Free;
}