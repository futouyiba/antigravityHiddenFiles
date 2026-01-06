�+using System.Collections.Generic;
using ObjectModel;
using SharedLib.Game;
using UnityEngine;

public class AssetsCache
{
	private Dictionary<int, string> _fishAssets = new Dictionary<int, string>();

	private Dictionary<int, ItemAssetInfo> _itemAssets = new Dictionary<int, ItemAssetInfo>();

	private ExitGames.Client.Photon.Hashtable _globalVariables;

	private bool FGameIgnore = true; //[EASTMALLI][TODO]物品配置解析，暂时不用并且解析太耗时，先屏蔽

	public bool AllAssetsChachesInited
	{
		get
		{
			//[EASTMALLI][TODO]物品配置解析，暂时不用并且解析太耗时，先屏蔽
			if (FGameIgnore)
			{
				return true;
			}
			
			return _fishAssets != null && _itemAssets != null && _globalVariables != null;
		}
	}

	public void Init()
	{
		//[EASTMALLI][TODO]物品配置解析，暂时不用并且解析太耗时，先屏蔽
		// InitInventoryItemCache();
	}

	private void InitInventoryItemCache()
	{
        TextAsset ta = Resources.Load<TextAsset>("config/itemAssets");
        var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ItemAssetInfo>>(ta.text, SerializationHelper.JsonSerializerSettings);
        Instance_OnGotInventoryItemAssets(list);
    }

    private void Debug_Instance_OnGotFishAssets()
    {
        TextAsset ta = Resources.Load<TextAsset>("config/FishAssets");
        var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<FishAssetInfo>>(ta.text, SerializationHelper.JsonSerializerSettings);

        foreach (FishAssetInfo fishAsset in list)
        {
            _fishAssets[fishAsset.FishId] = fishAsset.Asset;
        }
        InitGlobalVariablesCache();
        Debug.Log($"LoadFishAssets finished: {list.Count}");
    }

	private void InitGlobalVariablesCache()
	{
        var ta = Resources.Load<TextAsset>("config/GlobalVariables");

        var hashTable = Newtonsoft.Json.JsonConvert.DeserializeObject<ExitGames.Client.Photon.Hashtable>(ta.text);

        // 创建一个新的字典来存储修改后的值
        var newHashTable = new ExitGames.Client.Photon.Hashtable();

        foreach (var k in hashTable)
        {
            if (k.Value == null)
            {
                continue;
            }
            if (k.Value.GetType() == typeof(System.Int64))
            {
                newHashTable[k.Key] = (int)(System.Int64)k.Value;
            }
            else if (k.Value.GetType() == typeof(System.Decimal))
            {
                newHashTable[k.Key] = (float)(System.Decimal)k.Value;
            }
            else if (k.Value.GetType() == typeof(System.Double))
            {
                newHashTable[k.Key] = (float)(System.Double)k.Value;
            }
            else
            {
                newHashTable[k.Key] = k.Value;  // 对于其他类型，直接复制
            }
        }

        Instance_OnGotGlobalVariables(newHashTable);
    }

	private void Instance_OnGotInventoryItemAssets(IEnumerable<ItemAssetInfo> itemAssets)
	{
		foreach (ItemAssetInfo itemAsset in itemAssets)
		{
			_itemAssets[itemAsset.ItemId] = itemAsset;
		}
		InitFishCache();
		Debug.Log($"LoadItemAssets finished: {_itemAssets.Count}");
	}

	private void InitFishCache()
	{
        Debug_Instance_OnGotFishAssets();
    }

	private void Instance_OnGotGlobalVariables(ExitGames.Client.Photon.Hashtable vars)
	{
		_globalVariables = vars;
		if (vars.ContainsKey("IsRetail"))
		{
			Inventory.IsRetail = (bool)vars["IsRetail"];
		}
		if (vars.ContainsKey("ExchangeRate"))
		{
			Inventory.ExchangeRate = () => (float)vars["ExchangeRate"];
		}
		if (vars.ContainsKey("PondDayStart"))
		{
            InGameTimeHelper.Init((int)vars["PondDayStart"]);
		}
		if (vars.ContainsKey("DefaultInventoryCapacity"))
		{
			Inventory.DefaultInventoryCapacity = (int)vars["DefaultInventoryCapacity"];
		}
		if (vars.ContainsKey("MaxInventoryCapacity"))
		{
			Inventory.MaxInventoryCapacity = (int)vars["MaxInventoryCapacity"];
		}
		if (vars.ContainsKey("DefaultRodSetupCapacity"))
		{
			Inventory.DefaultRodSetupCapacity = (int)vars["DefaultRodSetupCapacity"];
		}
		if (vars.ContainsKey("MaxRodSetupCapacity"))
		{
			Inventory.MaxRodSetupCapacity = (int)vars["MaxRodSetupCapacity"];
		}
		if (vars.ContainsKey("DefaultBuoyCapacity"))
		{
			Inventory.DefaultBuoyCapacity = (int)vars["DefaultBuoyCapacity"];
		}
		if (vars.ContainsKey("MaxBuoyCapacity"))
		{
			Inventory.MaxBuoyCapacity = (int)vars["MaxBuoyCapacity"];
		}
		if (vars.ContainsKey("MixBasePossiblePercentageMin"))
		{
            Inventory.MixBasePossiblePercentageMin = (float)vars["MixBasePossiblePercentageMin"];
		}
		if (vars.ContainsKey("MixAromaPossiblePercentage"))
		{
			Inventory.MixAromaPossiblePercentage = (float)vars["MixAromaPossiblePercentage"];
		}
		if (vars.ContainsKey("MixParticlePossiblePercentage"))
		{
			Inventory.MixParticlePossiblePercentage = (float)vars["MixParticlePossiblePercentage"];
		}
		if (vars.ContainsKey("SilverRepairRate"))
		{
			Inventory.SilverRepairRate = (float)vars["SilverRepairRate"];
		}
		if (vars.ContainsKey("GoldRepairRate"))
		{
			Inventory.GoldRepairRate = (float)vars["GoldRepairRate"];
		}
		if (vars.ContainsKey("LevelCap"))
		{
			Profile.LevelCap = (int)vars["LevelCap"];
		}
		Debug.Log($"LoadGlobalVariables finished");
	}

	public string GetFishAssetPath(int fishId)
	{
		return (!_fishAssets.ContainsKey(fishId)) ? null : _fishAssets[fishId];
	}

	public ItemAssetInfo GetItemAssetPath(int itemId)
	{
		return (!_itemAssets.ContainsKey(itemId)) ? null : _itemAssets[itemId];
	}
}
�+2Pfile:///e:/DocsHDD/fishingplanet2022/Assets/Scripts/FPWorld/Asset/AssetsCache.cs