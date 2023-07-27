using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace TraderUtils
{
    [Serializable]
    public class CustomTrade
    {
        static string s => $"[{TradesConfiguration._plugin.Info.ToString()}] ";

        public CustomTrade()
        {
            all.Add(this);
            ID = ID_counter;
            ID_counter++;
            if (!patched)
            {
                Harmony harmony = new Harmony("org.bepinex.helpers.TraderUtils");
                harmony.PatchAll(typeof(Patch));
                patched = true;
            }
        }

        private void AddToTradersDictionary()
        {
            if (CustomTrade.traders.ContainsKey(m_traderName)) CustomTrade.traders[m_traderName].Add(this);
            else CustomTrade.traders.Add(m_traderName, new() { this });
        }

        public CustomTrade SetTrader(string traderName)
        {
            this.m_traderName = traderName;
            this.AddToTradersDictionary();
            return this;
        }

        public CustomTrade SetItem(string itemName)
        {
            this.prefabName = itemName;
            return this;
        }

        public CustomTrade SetPrice(int itemPrice)
        {
            this.price = itemPrice;
            return this;
        }

        public CustomTrade SetStack(int count)
        {
            this.stack = count;
            return this;
        }

        public CustomTrade SetMoneyItem(string itemName)
        {
            this.moneyItemName = itemName;
            return this;
        }

        public CustomTrade SetRequiredGlobalKey(string globalKey)
        {
            this.requiredGlobalKey = globalKey;
            return this;
        }

        public CustomTrade SetConfigurable(bool flag)
        {
            this.configurable = flag;
            return this;
        }

        public CustomTrade SetEnabled(bool flag)
        {
            this.enabled = flag;
            return this;
        }

        public override string ToString()
        {
            return string.Concat(new string[]
            {
                string.Format("ID: {0}, ", ID),
                "TraderName: ",
                this.m_traderName,
                ", PrefabName: ",
                this.prefabName,
                ", MoneyItemName: ",
                this.moneyItemName,
                ", ",
                string.Format("Price: {0}, ", price),
                "GlobalKey: ",
                this.requiredGlobalKey,
                ", ",
                string.Format("Stack: {0}", stack)
            });
        }

        internal static void UpdateAllValues()
        {
            foreach (var customTrade in CustomTrade.all)
            {
                customTrade.SetReferences();
            }

            foreach (var trader in CustomTrade.traders)
            {
                var traderOrig = ZNetScene.instance.GetPrefab(trader.Key)?.GetComponent<Trader>();
                if (!traderOrig)
                {
                    Debug.LogError($"[{TradesConfiguration._plugin.Info.ToString()}] " +
                                   $" Can't find trader prefab with name \"{trader.Key}\". " +
                                   $"Make sure entered name is equal to your gameobject that have Trader and ZNetView components.");
                    continue;
                }

                traderOrig.m_items = ToVanilaTrade(trader.Value);
            }

            onUpdateAllValues?.Invoke();
            if (StoreGui.instance) StoreGui.instance.FillList();
        }

        internal static List<Trader.TradeItem> ToVanilaTrade(List<CustomTrade> customTrades)
        {
            var result = new List<Trader.TradeItem>();
            foreach (var customTrade in customTrades.Where(x => x.enabled))
            {
                var item = new Trader.TradeItem
                {
                    m_prefab = customTrade.prefab,
                    m_price = customTrade.price,
                    m_stack = customTrade.stack,
                    m_requiredGlobalKey = customTrade.requiredGlobalKey
                };
                result.Add(item);
            }

            return result;
        }

        internal void SetReferences()
        {
            if (configurable)
            {
                if (config == null) TradesConfiguration.BindConfig(this);
                prefabName = config.prefabName.Value;
                moneyItemName = config.moneyItemName.Value;
                price = config.price.Value;
                requiredGlobalKey = config.requiredGlobalKey.Value;
                stack = config.stack.Value;
                enabled = config.enabled.Value;
                if (!ZNetScene.instance) return;
            }

            moneyItem = ZNetScene.instance.GetPrefab(moneyItemName)?.GetComponent<ItemDrop>();
            prefab = ZNetScene.instance.GetPrefab(prefabName)?.GetComponent<ItemDrop>();

            if (!moneyItem)
            {
                moneyItem = Patch.coinPrefab;
                Debug.LogError(s +
                               $" Can't find item prefab with name {moneyItemName}.");
            }

            if (!prefab)
                Debug.LogError(s +
                               $"Can't find item prefab with name {prefabName}.");
        }

        private static bool patched = false;
        internal static int ID_counter = 0;
        internal static UnityAction onUpdateAllValues;
        internal static List<CustomTrade> all = new();
        internal static Dictionary<string, List<CustomTrade>> traders = new();

        internal ItemDrop prefab;
        internal ItemDrop moneyItem;
        internal string prefabName;
        internal string moneyItemName = "Coins";
        internal int price = 1;
        internal string requiredGlobalKey = "";
        internal int stack = 1;
        internal string m_traderName;
        internal CustomTrade.TradeConfig config;
        internal bool configurable = true;
        internal bool enabled = true;
        internal int ID = 0;

        internal class TradeConfig
        {
            internal ConfigEntry<string> prefabName;
            internal ConfigEntry<string> moneyItemName;
            internal ConfigEntry<int> price;
            internal ConfigEntry<string> requiredGlobalKey;
            internal ConfigEntry<int> stack;
            internal ConfigEntry<bool> enabled;
        }
    }
}