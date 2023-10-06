using BepInEx.Configuration;
using UnityEngine.Events;

namespace TraderUtils;

[Serializable]
public class CustomTrade
{
    private static bool patched;
    internal static int ID_counter;
    internal static UnityAction onUpdateAllValues;
    internal static Dictionary<string, List<CustomTrade>> traders = new();
    internal TradeConfig config;
    internal bool configurable = true;
    internal int ID;
    internal ItemDrop moneyItem;

    internal ItemDrop prefab;
    internal string requiredGlobalKey = "";

    public CustomTrade()
    {
        all.Add(this);
        ID = ID_counter;
        ID_counter++;
        if (!patched)
        {
            var harmony = new Harmony("org.bepinex.helpers.TraderUtils");
            harmony.PatchAll(typeof(Patch));
            patched = true;
        }
    }

    private static string s => $"[{_plugin.Info.Metadata.Name}] ";
    public static List<CustomTrade> all { get; } = new();
    public string prefabName { get; private set; }
    public string moneyItemName { get; private set; } = "Coins";
    public int price { get; private set; } = 1;
    public int stack { get; private set; } = 1;
    public string m_traderName { get; private set; }
    public bool enabled { get; private set; } = true;

    private void AddToTradersDictionary()
    {
        if (traders.ContainsKey(m_traderName)) traders[m_traderName].Add(this);
        else traders.Add(m_traderName, new List<CustomTrade> { this });
    }

    public CustomTrade SetTrader(string traderName)
    {
        m_traderName = traderName;
        AddToTradersDictionary();
        return this;
    }

    public CustomTrade SetItem(string itemName)
    {
        prefabName = itemName;
        return this;
    }

    public CustomTrade SetPrice(int itemPrice)
    {
        price = itemPrice;
        return this;
    }

    public CustomTrade SetStack(int count)
    {
        stack = count;
        return this;
    }

    public CustomTrade SetMoneyItem(string itemName)
    {
        moneyItemName = itemName;
        return this;
    }

    public CustomTrade SetRequiredGlobalKey(string globalKey)
    {
        requiredGlobalKey = globalKey;
        return this;
    }

    public CustomTrade SetConfigurable(bool flag)
    {
        configurable = flag;
        return this;
    }

    public CustomTrade SetEnabled(bool flag)
    {
        enabled = flag;
        return this;
    }

    public override string ToString() =>
        $"Configurable: {configurable}, prefabName: {prefabName}, moneyItemName: {moneyItemName}, price: {price}, stack: {stack}, m_traderName: {m_traderName}, enabled: {enabled}";

    internal static void UpdateAllValues()
    {
        foreach (var customTrade in all) customTrade.SetReferences();

        foreach (var trader in traders)
        {
            var traderOrig = ZNetScene.instance.GetPrefab(trader.Key)?.GetComponent<Trader>();
            if (!traderOrig)
            {
                LogError($"{s}Can't find trader prefab with name \"{trader.Key}\". " +
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
            if (config == null) BindConfig(this);
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
            LogError(s +
                     $" Can't find item prefab with name {moneyItemName}.");
        }

        if (!prefab)
            LogError(s +
                     $"Can't find item prefab with name {prefabName}.");
    }

    internal class TradeConfig
    {
        internal ConfigEntry<bool> enabled;
        internal ConfigEntry<string> moneyItemName;
        internal ConfigEntry<string> prefabName;
        internal ConfigEntry<int> price;
        internal ConfigEntry<string> requiredGlobalKey;
        internal ConfigEntry<int> stack;

        public override string ToString()
        {
            return
                $"Enabled: {enabled}, MoneyItemName: {moneyItemName}, PrefabName: {prefabName}, Price: {price}, RequiredGlobalKey: {requiredGlobalKey}, Stack: {stack}";
        }
    }
}