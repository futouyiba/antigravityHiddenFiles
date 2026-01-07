�4using System;
using System.Collections.Generic;
using System.Linq;
using QHFramework.AOT;
using QHFramework.AOT.Localization;
using QHFramework.Runtime;
using UnityEngine;
using World.Const;

namespace World.Data.Config
{
    [Serializable]
    public class PondList : ScriptableObject
    {
        public const string Path = "fish_pond_list";
        /*
         "id": 301020000,
        "name": "yinchuan_1",
        "pondName": 14000062,
        "pondImg": "ro01_2D",
        "spotImg": "ro01_area_2D",
        "mapId": 106,
        "fishStockId": 301030000,
        "entryLevel": 1,
        "entryItem": 101,
        "entryFee": 100,
        "OpenSpot": [1, 2, 3],
        "mark": "金币"
         */
        //json
        public long id;
        public string name;
        public long pondName;
        public string pondImg;
        public string spotImg;
        public int isShow;
        public int mapId;
        public int fishStockId;
        public int entryLevel;
        public long[] entryItem;
        public int[] OpenSpot;
        public string mark;
        public int pondOrder;

        public bool IsShowInView => isShow > 0;
        /// <summary>
        /// 水底温度
        /// </summary>
        public int hypolimnionT;
        private static LanguageReader _languageReader;

        public static Dictionary<string, PondList> GetConfig()
        {
            QHApp.Config.TryGetDictConfig<PondList>(WorldConfigDefine.FishPondList, out var tbs);
            return tbs.Map;
        }

        public static PondList TryGetDictConfig(string id)
        {
            GetConfig().TryGetValue(id, out var item);
            return item as PondList;
        }

        /// <summary>
        /// 获取ID最小值
        /// </summary>
        /// <returns></returns>
        public static PondList GetIdAscendingOrderFirst()
        {
            //获取ID最小的值
            var list = GetConfig().Values.ToList();
            list.Sort((a, b) => ((PondList)a).id.CompareTo( ((PondList)b).id));
            return list[0] as PondList;
        }
        public static PondList GetLvAscendingOrderFirst()
        {
            //获取ID最小的值
            var list = GetConfig().Values.ToList();
            list.Sort((a, b) => ((PondList)a).entryLevel - ((PondList)b).entryLevel);
            return list[0] as PondList;
        }

        public static int GetDefaultBirthSpotIdById(long pondId)
        {
            var tb = TryGetDictConfig(pondId.ToString());
            if (tb.OpenSpot.Length <= 0)
            {
                QHLog.Error($"Pond open spot is empty. pondID: {pondId}");
                return 0;
            }

            return tb.OpenSpot[0];
        }

        
        public long GetKeyFishId()
        {
            BasicFishQuality fishQuality = null;
            StockReleaseCfg bestStockRelease = null;
            bool haveKeyFish = false;

            var stockReleases = StockReleaseCfg.GetListByStockId(fishStockId);
            if (stockReleases.Count <= 0)
            {
                QHLog.Error($"投鱼配置{fishStockId}在StockRelease没有对应的鱼配置");
            }
            foreach (var cfg in stockReleases)
            {
                var fishReleaseCfg = cfg.GetFishReleaseCfg();
                // var feaCfg = FishEnvAffinityCfg.GetFishEnvAffinityById(cfg.fishId);
                var fqCfg = BasicFishQuality.GetBasicFishQuality(cfg.fishId);

                if (fishQuality == null)
                {
                    fishQuality = fqCfg;
                    bestStockRelease = cfg;
                    continue;
                }

                if (fqCfg.IsKeyFish == "true")
                {
                    if (!haveKeyFish)
                    {
                        fishQuality = fqCfg;
                        bestStockRelease = cfg;
                        haveKeyFish = true;
                        continue;
                    }

                    if (fishReleaseCfg.probWeightIdeal > bestStockRelease.GetFishReleaseCfg().probWeightIdeal)
                    {
                        fishQuality = fqCfg;
                        bestStockRelease = cfg;
                    }
                }
                else
                {
                    if (haveKeyFish)
                        continue;

                    if (fishReleaseCfg.probWeightIdeal > bestStockRelease.GetFishReleaseCfg().probWeightIdeal)
                    {
                        fishQuality = fqCfg;
                        bestStockRelease = cfg;
                    }
                }
            }

            return fishQuality == null ? 0 : fishQuality.id;
        }


        public string GetName()
        {
            return GetName(id);
        }

        public static string GetName(long id)
        {
            return _languageReader.GetNameTextById(id);
        }

        public string GetDes()
        {
            return GetDes(id);
        }

        public List<long> GetEntryItems()
        {
            if (entryItem.Length == 1 && entryItem[0] == 0)
            {
                return new List<long>();
            }

            return entryItem.ToList();
        }

        public bool IsEnterFree()
        {
            if (entryItem.Length == 1 && entryItem[0] == 0)
            {
                return true;
            }

            return false;
        }

        public static string GetDes(long id)
        {
            return _languageReader.GetDesTextById(id);
        }

        public static void InitLanguageReader(LanguageReader reader)
        {
            _languageReader = reader;
        }

        public static List<long> GetEntryItemsById(long id)
        {
            var tb = TryGetDictConfig(id.ToString());
            if (tb == null)
            {
                return null;
            }


            return tb.GetEntryItems();
        }

        public static bool GetPondIsEnterFree(long id)
        {
            var tb = TryGetDictConfig(id.ToString());
            if (tb == null)
            {
                return true;
            }

            return tb.IsEnterFree();
        }
        
        public static List<PondList> GetShowPondList()
        {
            var cfg = GetConfig();
            if (cfg == null)
            {
                return null;
            }

            List<PondList> pondLists = new List<PondList>();
            foreach (var v in cfg.Values)
            {
                if (v.IsShowInView)
                {
                    pondLists.Add(v);
                }
            }

            pondLists.Sort((a, b) => a.pondOrder - b.pondOrder);

            return pondLists;
        }
    }
}
�4"(4500bf11159f7dd297cafd329a276bd8a9c29b7b2Cfile:///d:/fishinggame/Assets/World/Scripts/Data/Config/PondList.cs:file:///d:/fishinggame