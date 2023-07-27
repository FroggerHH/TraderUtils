﻿using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TraderUtils
{
    [HarmonyPatch]
    internal static class Patch
    {
        [HarmonyPatch(typeof(StoreGui), "Awake"), HarmonyPostfix, HarmonyWrapSafe]
        internal static void StoreGui_Awake(StoreGui __instance)
        {
            moneyIcon = __instance.transform.Find("Store/coins/coin icon").GetComponent<Image>();
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.FillList)), HarmonyPostfix, HarmonyWrapSafe]
        internal static void StoreGui_FillList(StoreGui __instance)
        {
            if (!CustomTrade.traders.TryGetValue(__instance.m_trader.GetPrefabName(), out List<CustomTrade> trades))
                return;

            trades = trades.Select(x => x).Where(x => x.enabled).ToList();

            for (var i = 0; i < __instance.m_itemList.Count; i++)
            {
                var customTrade = trades[i];
                var itemGO = __instance.m_itemList[i];
                var playerCoins = CountItems(customTrade.moneyItemName);
                bool haveEnoughMoney = customTrade.price <= playerCoins;

                var moneyIcon = Utils.FindChild(itemGO.transform, "coin icon").GetComponent<Image>();
                var iconImage = itemGO.transform.Find("icon").GetComponent<Image>();
                var nameText = itemGO.transform.Find("name").GetComponent<Text>();
                var priceText = Utils.FindChild(itemGO.transform, "price").GetComponent<Text>();

                moneyIcon.sprite = customTrade.moneyItem.m_itemData.GetIcon();
                iconImage.color = haveEnoughMoney ? Color.white : new Color(1f, 0.0f, 1f, 0.0f);
                nameText.color = haveEnoughMoney ? Color.white : Color.grey;
                priceText.color = haveEnoughMoney ? new Color(1, 0.8069f, 0, 1) : Color.grey;
            }
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.SelectItem)), HarmonyPostfix, HarmonyWrapSafe]
        internal static void StoreGui_SelectItem(StoreGui __instance)
        {
            if (__instance.m_selectedItem == null || CustomTrade.all == null) return;

            current = CustomTrade.all.Find(x =>
                x.price == __instance.m_selectedItem.m_price &&
                x.stack == __instance.m_selectedItem.m_stack &&
                x.prefab == __instance.m_selectedItem.m_prefab);

            if (current == null || current.moneyItem == null)
            {
                __instance.m_coinPrefab = coinPrefab;
                moneyIcon.sprite = coinPrefab.m_itemData.GetIcon();
            }
            else
            {
                __instance.m_coinPrefab = current.moneyItem;
                moneyIcon.sprite = current.moneyItem.m_itemData.GetIcon();
            }
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.SelectItem)), HarmonyPrefix, HarmonyWrapSafe]
        internal static void StoreGui_SellItem_Prefi(StoreGui __instance)
        {
            __instance.m_coinPrefab = Patch.coinPrefab;
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.SellItem)), HarmonyPostfix, HarmonyWrapSafe]
        internal static void StoreGui_SellItem_Postfix(StoreGui __instance)
        {
            Patch.StoreGui_SelectItem(__instance);
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.UpdateSellButton)), HarmonyPrefix, HarmonyWrapSafe]
        internal static void StoreGui_UpdateSellButton(StoreGui __instance)
        {
            __instance.m_coinPrefab = coinPrefab;
        }


        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.OnSellItem)), HarmonyPrefix, HarmonyWrapSafe]
        internal static void StoreGui_OnSellItem(StoreGui __instance)
        {
            StoreGui_SelectItem(__instance);
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.UpdateBuyButton)), HarmonyPrefix, HarmonyWrapSafe]
        internal static void StoreGui_UpdateBuyButton(StoreGui __instance)
        {
            StoreGui_SelectItem(__instance);
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.GetPlayerCoins)), HarmonyPrefix, HarmonyWrapSafe]
        internal static void StoreGui_GetPlayerCoins(StoreGui __instance)
        {
            StoreGui_SelectItem(__instance);
        }

        [HarmonyPatch(typeof(ZNetScene), nameof(StoreGui.Awake)), HarmonyPostfix, HarmonyWrapSafe]
        internal static void ZNetSceneAwake(ZNetScene __instance)
        {
            coinPrefab = __instance.GetPrefab("Coins").GetComponent<ItemDrop>();
            CustomTrade.UpdateAllValues();
        }

        [HarmonyPatch(typeof(Trader), nameof(Trader.Start)), HarmonyPostfix, HarmonyWrapSafe]
        internal static void Trader_Start(Trader __instance)
        {
            CustomTrade.onUpdateAllValues += () =>
            {
                __instance.m_items = ZNetScene.instance.GetPrefab(__instance.GetPrefabName<Trader>())
                    .GetComponent<Trader>().m_items;
            };
        }

        [HarmonyPatch(typeof(Trader), nameof(Trader.GetAvailableItems)), HarmonyPrefix, HarmonyWrapSafe]
        internal static bool Trader_GetAvailableItems(Trader __instance, ref List<Trader.TradeItem> __result)
        {
            if (CustomTrade.traders.TryGetValue(__instance.GetPrefabName(), out var result))
            {
                __result = CustomTrade.ToVanilaTrade(result);
                return false;
            }

            return true;
        }

        private static CustomTrade current;
        private static Image moneyIcon;
        internal static ItemDrop coinPrefab;

        public static int CountItems(string name)
        {
            int result = 0;
            foreach (ItemDrop.ItemData itemData in Player.m_localPlayer.GetInventory().m_inventory)
            {
                if (name == null || itemData.m_shared.m_name == name ||
                    ObjectDB.instance.GetItemPrefab(name).GetComponent<ItemDrop>().m_itemData.m_shared.m_name ==
                    itemData.m_shared.m_name)
                    result += itemData.m_stack;
            }

            return result;
        }
    }
}