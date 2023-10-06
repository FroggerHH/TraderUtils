using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using JetBrains.Annotations;

namespace TraderUtils;

[PublicAPI]
public static class TradesConfiguration
{
    internal static BaseUnityPlugin? _plugin;

    private static bool hasConfigSync = true;
    private static object? _configSync;


    internal static List<TradeConfig> _configs = new();

    internal static BaseUnityPlugin plugin
    {
        get
        {
            if (_plugin is not null) return _plugin;
            IEnumerable<TypeInfo> types;
            try
            {
                types = Assembly.GetExecutingAssembly().DefinedTypes.ToList();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
            }

            _plugin = (BaseUnityPlugin)Chainloader.ManagerObject.GetComponent(types.First(t =>
                t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));

            return _plugin;
        }
    }

    private static object? configSync
    {
        get
        {
            if (_configSync != null || !hasConfigSync) return _configSync;
            if (Assembly.GetExecutingAssembly().GetType("ServerSync.ConfigSync") is { } configSyncType)
            {
                _configSync = Activator.CreateInstance(configSyncType, plugin.Info.Metadata.GUID + " PieceManager");
                configSyncType.GetField("CurrentVersion")
                    .SetValue(_configSync, plugin.Info.Metadata.Version.ToString());
                configSyncType.GetProperty("IsLocked")!.SetValue(_configSync, true);
            } else
            {
                hasConfigSync = false;
            }

            return _configSync;
        }
    }

    private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
    {
        var configEntry = plugin.Config.Bind(group, name, value, description);

        configSync?.GetType().GetMethod("AddConfigEntry")!.MakeGenericMethod(typeof(T))
            .Invoke(configSync, new object[] { configEntry });

        return configEntry;
    }

    private static ConfigEntry<T> config<T>(string group, string name, T value, string description)
    {
        return config(group, name, value, new ConfigDescription(description));
    }

    internal static TradeConfig BindConfig(CustomTrade trade)
    {
        var SaveOnConfigSet = plugin.Config.SaveOnConfigSet;
        plugin.Config.SaveOnConfigSet = false;

        var result = new TradeConfig();
        var group = $"Trades of {trade.m_traderName} - trade {trade.ID}";
        result.prefabName = config(group, "Item to buy", trade.prefabName, "");
        result.moneyItemName = config(group, "Money item", trade.moneyItemName, "");
        result.price = config(group, "Price", trade.price, "");
        result.requiredGlobalKey = config(group, "RequiredGlobalKey", trade.requiredGlobalKey, "");
        result.stack = config(group, "Stack", trade.stack, "");
        result.enabled = config(group, "Enabled", trade.enabled, "");
        trade.config = result;
        result.prefabName.SettingChanged += (_, _) => UpdateAllValues();
        result.moneyItemName.SettingChanged += (_, _) => UpdateAllValues();
        result.price.SettingChanged += (_, _) => UpdateAllValues();
        result.requiredGlobalKey.SettingChanged += (_, _) => UpdateAllValues();
        result.stack.SettingChanged += (_, _) => UpdateAllValues();
        result.enabled.SettingChanged += (_, _) => UpdateAllValues();

        if (SaveOnConfigSet)
        {
            plugin.Config.SaveOnConfigSet = true;
            plugin.Config.Save();
        }

        return result;
    }
}