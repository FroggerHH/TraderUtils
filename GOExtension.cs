using UnityEngine;

namespace TraderUtils;

internal static class GOExtension
{
    internal static string GetPrefabName<T>(this T gameObject) where T : MonoBehaviour
    {
        string prefabName = Utils.GetPrefabName(gameObject.gameObject);
        for (int index = 0; index < 80; ++index)
            prefabName = prefabName.Replace(string.Format(" ({0})", (object)index), "");
        return prefabName;
    }
    internal static string GetPrefabName(this GameObject gameObject)
    {
        string prefabName = Utils.GetPrefabName(gameObject);
        for (int index = 0; index < 80; ++index)
            prefabName = prefabName.Replace(string.Format(" ({0})", (object)index), "");
        return prefabName;
    }
}