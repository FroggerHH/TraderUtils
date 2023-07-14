using System;
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

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.FillList)), HarmonyPrefix, HarmonyWrapSafe]
        internal static bool StoreGui_FillList(StoreGui __instance)
        {
            List<CustomTrade> source;
            bool result;
            if (CustomTrade.traders.TryGetValue(__instance.m_trader.GetPrefabName<Trader>(), out source))
            {
                int num = __instance.GetSelectedItemIndex();
                foreach (var obj in __instance.m_itemList)
                {
                    Object.Destroy(obj);
                }

                __instance.m_itemList.Clear();
                List<CustomTrade> list = (from x in source
                    where x.enabled
                    select x).ToList<CustomTrade>();
                __instance.m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    Mathf.Max(__instance.m_itemlistBaseSize, (float)list.Count * __instance.m_itemSpacing));
                for (int i = 0; i < list.Count; i++)
                {
                    CustomTrade customTrade = list[i];
                    int num2 = Player.m_localPlayer.GetInventory()
                        .CountItems(customTrade.moneyItem.m_itemData.m_shared.m_name, -1, true);
                    GameObject tradeeElement =
                        UnityEngine.Object.Instantiate<GameObject>(__instance.m_listElement, __instance.m_listRoot);
                    RectTransform rectTransform = tradeeElement.transform as RectTransform;
                    tradeeElement.SetActive(true);
                    rectTransform.anchoredPosition = new Vector2(0f, (float)i * -__instance.m_itemSpacing);
                    bool flag2 = customTrade.price <= num2;
                    Image component = tradeeElement.transform.Find("icon").GetComponent<Image>();
                    component.sprite = customTrade.prefab.m_itemData.GetIcon();
                    component.color = (flag2 ? Color.white : new Color(1f, 0f, 1f, 0f));
                    string text = Localization.instance.Localize(customTrade.prefab.m_itemData.m_shared.m_name);
                    bool flag3 = customTrade.stack > 1;
                    if (flag3)
                    {
                        text = text + " x" + customTrade.stack.ToString();
                    }

                    Text component2 = tradeeElement.transform.Find("name").GetComponent<Text>();
                    component2.text = text;
                    component2.color = (flag2 ? Color.white : Color.grey);
                    UITooltip component3 = tradeeElement.GetComponent<UITooltip>();
                    component3.m_topic = customTrade.prefab.m_itemData.m_shared.m_name;
                    component3.m_text = customTrade.prefab.m_itemData.GetTooltip();
                    Text component4 = Utils.FindChild(tradeeElement.transform, "price").GetComponent<Text>();
                    component4.text = customTrade.price.ToString();
                    bool flag4 = !flag2;
                    if (flag4)
                    {
                        component4.color = Color.grey;
                    }

                    tradeeElement.GetComponent<Button>().onClick.AddListener(delegate()
                    {
                        __instance.OnSelectedItem(tradeeElement);
                    });
                    __instance.m_itemList.Add(tradeeElement);
                    var component5 = Utils.FindChild(tradeeElement.transform, "coin icon").GetComponent<Image>();
                    component5.sprite = customTrade.moneyItem.m_itemData.GetIcon();
                }

                bool flag5 = num < 0;
                if (flag5)
                {
                    num = 0;
                }

                __instance.SelectItem(num, false);
                result = false;
            }
            else
            {
                result = true;
            }

            return result;
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.SelectItem)), HarmonyPostfix, HarmonyWrapSafe]
        internal static void StoreGui_SelectItem(StoreGui __instance)
        {
            if (__instance.m_selectedItem == null || CustomTrade.all == null) return;

            current = CustomTrade.all.Find(x =>
                x.price == __instance.m_selectedItem.m_price &&
                x.stack == __instance.m_selectedItem.m_stack &&
                x.prefab == __instance.m_selectedItem.m_prefab);

            if (current == null)
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

        private static CustomTrade current;
        private static Image moneyIcon;
        internal static ItemDrop coinPrefab;
    }
}