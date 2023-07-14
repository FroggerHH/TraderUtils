using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using static TraderUtils.Patch;

namespace TraderUtils;

[PublicAPI]
public static class TradesConfiguration
{
    internal static BaseUnityPlugin? _plugin = null!;

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

            _plugin = (BaseUnityPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(types.First(t =>
                t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));

            return _plugin;
        }
    }

    private static bool hasConfigSync = true;
    private static object? _configSync;

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
            }
            else
            {
                hasConfigSync = false;
            }

            return _configSync;
        }
    }

    private static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
    {
        ConfigEntry<T> configEntry = plugin.Config.Bind(group, name, value, description);

        configSync?.GetType().GetMethod("AddConfigEntry")!.MakeGenericMethod(typeof(T))
            .Invoke(configSync, new object[] { configEntry });

        return configEntry;
    }

    private static ConfigEntry<T> config<T>(string group, string name, T value, string description) =>
        config(group, name, value, new ConfigDescription(description));


    internal static List<CustomTrade.TradeConfig> _configs = new();

    internal static CustomTrade.TradeConfig BindConfig(CustomTrade trade)
    {
        bool SaveOnConfigSet = plugin.Config.SaveOnConfigSet;
        plugin.Config.SaveOnConfigSet = false;

        var result = new CustomTrade.TradeConfig();
        var group = $"Trades of {trade.m_traderName} - trade {trade.ID}";
        result.prefabName = config(group, "Item to buy", trade.prefabName, "");
        result.moneyItemName = config(group, "Money item", trade.moneyItemName, "");
        result.price = config(group, "Price", trade.price, "");
        result.requiredGlobalKey = config(group, "RequiredGlobalKey", trade.requiredGlobalKey, "");
        result.stack = config(group, "Stack", trade.stack, "");
        result.enabled = config(group, "Enabled", trade.enabled, "");
        trade.config = result;
        result.prefabName.SettingChanged += (_, _) => CustomTrade.UpdateAllValues();
        result.moneyItemName.SettingChanged += (_, _) => CustomTrade.UpdateAllValues();
        result.price.SettingChanged += (_, _) => CustomTrade.UpdateAllValues();
        result.requiredGlobalKey.SettingChanged += (_, _) => CustomTrade.UpdateAllValues();
        result.stack.SettingChanged += (_, _) => CustomTrade.UpdateAllValues();
        result.enabled.SettingChanged += (_, _) => CustomTrade.UpdateAllValues();

        if (SaveOnConfigSet)
        {
            plugin.Config.SaveOnConfigSet = true;
            plugin.Config.Save();
        }

        return result;
    }
}