using UnityEngine;

// meta-progression data shared between combat and hub, saved in PlayerPrefs
public static class PlayerProgress
{
    private const string CurrencyKey = "currency";
    private const string ShardsKey = "shards";
    private const string AbilityKeyPrefix = "ability_";

    public static int Currency => PlayerPrefs.GetInt(CurrencyKey, 0);

    // rare meta currency from elites, bosses, events and lost runs
    public static int Shards => PlayerPrefs.GetInt(ShardsKey, 0);

    public static void AddCurrency(int amount)
    {
        if (amount <= 0) return;

        PlayerPrefs.SetInt(CurrencyKey, Currency + amount);
        PlayerPrefs.Save();
    }

    public static bool SpendCurrency(int amount)
    {
        if (amount > Currency) return false;

        PlayerPrefs.SetInt(CurrencyKey, Currency - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static void AddShards(int amount)
    {
        if (amount <= 0) return;

        PlayerPrefs.SetInt(ShardsKey, Shards + amount);
        PlayerPrefs.Save();
    }

    public static bool SpendShards(int amount)
    {
        if (amount > Shards) return false;

        PlayerPrefs.SetInt(ShardsKey, Shards - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static bool IsAbilityUnlocked(AbilityData ability)
    {
        return ability.IsUnlockedByDefault || PlayerPrefs.GetInt(AbilityKeyPrefix + ability.name, 0) == 1;
    }

    public static void UnlockAbility(AbilityData ability)
    {
        PlayerPrefs.SetInt(AbilityKeyPrefix + ability.name, 1);
        PlayerPrefs.Save();
    }
}
