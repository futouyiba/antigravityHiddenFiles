��using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using NumSharp;
using UnityEditor;
using System;
using System.Linq;

[CreateAssetMenu(fileName = "MapExportGlobalConfig", menuName = "Export/Map Global Config", order = 1)]
public class EnvTileExportCfg : SerializedScriptableObject
{
    public string samplingVolumeTag = "Exporter";
    public string filePrefix = "MapExport_";

    [FolderPath]
    public string exportFolder = "D:/ExportedData";

    [TableList]
    public List<EnvTileExportCfgPerMap> perMapConfigs = new List<EnvTileExportCfgPerMap>();

    public Transform theSamplingVolume;
    public float extraSafeDepth = 1.5f;

    // ================== Bit Mask Definitions ==================
    private const int FLAG_WATER = 1 << 0;
    private const int FLAG_WATER_GRASS = 1 << 1;
    private const int FLAG_STONE = 1 << 2;
    private const int FLAG_DRIFTWOOD = 1 << 3;
    private const int FLAG_PIER = 1 << 4;
    private const int FLAG_DEEP_PIT = 1 << 5;
    private const int FLAG_RIDGE = 1 << 6;
    private const int FLAG_FAULT = 1 << 7;
    private const int FLAG_ROCK_SHELF = 1 << 8;
    private const int FLAG_BAY = 1 << 9;
    private const int FLAG_MUD = 1 << 10;
    private const int FLAG_GRAVEL = 1 << 11;

    private QHTerrain _qhTerrainInstance;
    protected QHTerrain QHTerrainCached {
        get{
            if (_qhTerrainInstance == null){
                _qhTerrainInstance = GameObject.FindObjectOfType<QHTerrain>();
            }
            return _qhTerrainInstance;
        }
    }

    [Button("显示采样区域")]
    public void PlaceActualScopeTriggerBox()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        var sceneName = Path.GetFileNameWithoutExtension(currentScene.path);
        
        EnvTileExportCfgPerMap cfgForCurrScene = GetSceneConfig(sceneName);
        if (cfgForCurrScene == null)
        {
            cfgForCurrScene = this.CreateDefaultCfgForCurrScene();
        }

        GameObject samplingVolume = GameObject.FindWithTag(samplingVolumeTag);
        if (samplingVolume != null)
        {
            samplingVolume.SetActive(true);
            var samplingVolumeSize = samplingVolume.GetComponent<BoxCollider>().size;
            var samplingVolumeCenter = samplingVolume.transform.position;

            if (cfgForCurrScene != null)
            {
                if (EditorUtility.DisplayDialog("提示", "是否要将找到的采样区域信息保存到地图配置中？", "是", "否"))
                {
                    cfgForCurrScene.scopeTriggerBoxSize = samplingVolumeSize;
                    cfgForCurrScene.scopeTriggerBoxCenter = samplingVolumeCenter;
                }
            }
            theSamplingVolume = samplingVolume.transform;
            return;
        }
        Debug.Log($"未找到 Tag 为 {samplingVolumeTag} 的 GameObject，开始创建...");

        GameObject scopeTriggerBox = new GameObject("EnvTileExportScopeTriggerBox");
        scopeTriggerBox.tag = samplingVolumeTag;
        scopeTriggerBox.transform.position = cfgForCurrScene.scopeTriggerBoxCenter;
        BoxCollider boxCollider = scopeTriggerBox.AddComponent<BoxCollider>();
        boxCollider.size = cfgForCurrScene.scopeTriggerBoxSize;
        boxCollider.isTrigger = true;

        theSamplingVolume = scopeTriggerBox.transform;
    }

    [Button("清除采样区域")]
    public void RemoveActualScopeTriggerBox()
    {
        GameObject samplingVolume = GameObject.FindWithTag(samplingVolumeTag);
        if (samplingVolume != null)
        {
            samplingVolume.SetActive(false);
            DestroyImmediate(samplingVolume);
            theSamplingVolume = null;
        }
    }

    [Button("采样导出")]
    public void ExportVoxelData()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        var sceneName = Path.GetFileNameWithoutExtension(currentScene.path);

        if (GameObject.FindObjectOfType<Camera>() == null)
        {
            GameObject dummyCameraObj = new GameObject("DummyCamera");
            dummyCameraObj.transform.position = Vector3.zero;
            dummyCameraObj.AddComponent<Camera>();
        }
        var mapCtrl = GameObject.FindObjectOfType<MapCtrl>();
        if (mapCtrl != null) mapCtrl.isTest = true;

        if (!EditorApplication.isPlaying)
        {
            EditorApplication.EnterPlaymode();
            EditorUtility.DisplayDialog("提示", "等待Unity进入PlayMode，然后重新点击采样导出。", "确定");   
            return;
        }

        GenerateTerrainColliders();
        EnsureTerrainInitialized();

        EnvTileExportCfgPerMap cfgForCurrScene = GetSceneConfig(sceneName);
        if (cfgForCurrScene == null)
        {
            cfgForCurrScene = CreateDefaultCfgForCurrScene();
            return;
        }

        if (theSamplingVolume == null)
        {
            PlaceActualScopeTriggerBox();
            if (!EditorUtility.DisplayDialog("提示", "在场景中放置了范围框，继续导出吗？", "继续导出", "取消导出，先调整采样区域"))
                return;
        }

        var samplingVolumeIdx = theSamplingVolume.gameObject;
        var globalSize = samplingVolumeIdx.GetComponent<BoxCollider>().size;
        var globalCenter = samplingVolumeIdx.transform.position;
        Bounds globalBounds = new Bounds(globalCenter, globalSize);

        var mapIDStr = sceneName.Substring(sceneName.LastIndexOf('_') + 1);
        int mapID = int.Parse(mapIDStr);
        var structurePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Res/Base/Prefab/level/Structure_{mapIDStr}.prefab");
        GameObject structureInst = null;
        if (structurePrefab != null) {
            structureInst = GameObject.Instantiate(structurePrefab);
        }

        float maxDepth = CalculateMaxDepth(globalBounds, cfgForCurrScene);
        if (!EditorUtility.DisplayDialog("提示", $"计算得到的最大深度为 {maxDepth}，请确认是否正确。", "确定", "不对，取消导出"))
        {
            if(structureInst) GameObject.Destroy(structureInst);
            return;
        }
        
        // 准备导出路径: {SceneName}_{Mode}_{timestamp}
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string modeStr = cfgForCurrScene.exportMode.ToString();
        string subFolderName = $"{sceneName}_{modeStr}_{timestamp}";
        string subFolderPath = Path.Combine(exportFolder, subFolderName);
        if (!Directory.Exists(subFolderPath)) Directory.CreateDirectory(subFolderPath);

        // Core Export Data
        MapExportData exportData = new MapExportData();
        exportData.mapID = mapID;
        exportData.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // -------------------------------------------------------------
        // Step A: 导出 Global Map
        // -------------------------------------------------------------
        float globalMinY = -maxDepth - extraSafeDepth;
        float globalMaxY = cfgForCurrScene.raycastStartY;
        Bounds globalSamplingBounds = new Bounds(globalBounds.center, globalBounds.size);
        globalSamplingBounds.min = new Vector3(globalBounds.min.x, globalMinY, globalBounds.min.z);
        globalSamplingBounds.max = new Vector3(globalBounds.max.x, globalMaxY, globalBounds.max.z);

        Debug.Log($"开始采样 Global Map: {globalSamplingBounds}");
        var globalVoxelData = SampleVoxelGrid(globalSamplingBounds, cfgForCurrScene.samplingStep, out int[] globalDims, null);
        
        string globalBaseName = $"{sceneName}_Global";
        MapLayerData globalLayerData = ExportLayerByMode(globalVoxelData, subFolderPath, globalBaseName, cfgForCurrScene);
        
        globalLayerData.name = "Global";
        globalLayerData.type = "Global";
        globalLayerData.origin = new float[] { globalSamplingBounds.min.x, globalSamplingBounds.min.y, globalSamplingBounds.min.z };
        globalLayerData.step = new float[] { cfgForCurrScene.samplingStep.x, cfgForCurrScene.samplingStep.y, cfgForCurrScene.samplingStep.z };
        globalLayerData.dim = globalDims;
        exportData.global = globalLayerData;

        // -------------------------------------------------------------
        // Step B: 导出 Local Stocks (局部池)
        // -------------------------------------------------------------
        var allTags = GameObject.FindObjectsOfType<QHSyncColliderTag>();
        var localStockTags = allTags.Where(t => t.AreaType == QHColliderAreaType.LocalStock).ToList();
        
        Debug.Log($"发现 {localStockTags.Count} 个局部池 (LocalStock)");

        foreach (var stock in localStockTags)
        {
            Bounds stockBounds = new Bounds(stock.transform.position, Vector3.zero);
            var cols = stock.GetComponentsInChildren<Collider>();
            if (cols.Length > 0)
            {
                stockBounds = cols[0].bounds;
                for (int i = 1; i < cols.Length; i++) stockBounds.Encapsulate(cols[i].bounds);
            }
            else
            {
                 Debug.LogWarning($"LocalStock {stock.name} 没有 Collider，包围盒可能不准确(Zero)。");
            }

            Vector3 localStep = new Vector3(cfgForCurrScene.localStockStepXZ, cfgForCurrScene.samplingStep.y, cfgForCurrScene.localStockStepXZ);
            
            float localMinY = GetMinYInBounds(stockBounds, cfgForCurrScene); 
            localMinY -= extraSafeDepth;
            
            Bounds localSamplingBounds = new Bounds(stockBounds.center, stockBounds.size);
            localSamplingBounds.min = new Vector3(localSamplingBounds.min.x, localMinY, localSamplingBounds.min.z);
            localSamplingBounds.max = new Vector3(localSamplingBounds.max.x, cfgForCurrScene.raycastStartY, localSamplingBounds.max.z);

            Debug.Log($"开始采样 LocalStock [{stock.name}]: Bounds={localSamplingBounds} Step={localStep}");

            var localVoxelData = SampleVoxelGrid(localSamplingBounds, localStep, out int[] localDims, stock);

            string localBaseName = $"{sceneName}_{stock.name}_{stock.GetInstanceID()}";
            MapLayerData localLayerData = ExportLayerByMode(localVoxelData, subFolderPath, localBaseName, cfgForCurrScene);

            localLayerData.name = string.IsNullOrEmpty(stock.stockName) ? stock.name : stock.stockName;
            localLayerData.type = "LocalStock";
            localLayerData.origin = new float[] { localSamplingBounds.min.x, localSamplingBounds.min.y, localSamplingBounds.min.z };
            localLayerData.step = new float[] { localStep.x, localStep.y, localStep.z };
            localLayerData.dim = localDims;
            
            exportData.locals.Add(localLayerData);
        }

        // -------------------------------------------------------------
        // Step C: 导出 Protobuf 描述文件
        // -------------------------------------------------------------
        string jsonMeta = JsonUtility.ToJson(exportData, true);
        File.WriteAllText(Path.Combine(subFolderPath, "map_data.json"), jsonMeta);
        Debug.Log($"Map {mapID} 导出完成！\n路径: {subFolderPath}");

        if(structureInst) GameObject.Destroy(structureInst);
    }

    private MapLayerData ExportLayerByMode(int[,,] voxelData, string folder, string baseName, EnvTileExportCfgPerMap cfg)
    {
        MapLayerData layerData = new MapLayerData();
        
        switch (cfg.exportMode)
        {
            case ExportMode.BlockSparse:
                return ExportAsBlockSparse(voxelData, folder, baseName, cfg.blockSize);

            case ExportMode.SparseCOO:
                string sparseFile = $"{baseName}.npy";
                ExportVoxelDataAsSparseCOO(voxelData, Path.Combine(folder, sparseFile));
                layerData.format = "sparse_coo";
                layerData.dataFile = sparseFile;
                break;

            case ExportMode.Dense:
            default:
                string denseFile = $"{baseName}.npy";
                ExportVoxelDataAsDense(voxelData, Path.Combine(folder, denseFile));
                layerData.format = "dense";
                layerData.dataFile = denseFile;
                break;
        }
        return layerData;
    }

    // ================== Block Sparse Export ==================
    private MapLayerData ExportAsBlockSparse(int[,,] voxelData, string folder, string baseName, int blockSize)
    {
        int dimX = voxelData.GetLength(0);
        int dimY = voxelData.GetLength(1);
        int dimZ = voxelData.GetLength(2);
        
        int blocksX = Mathf.CeilToInt((float)dimX / blockSize);
        int blocksY = Mathf.CeilToInt((float)dimY / blockSize);
        int blocksZ = Mathf.CeilToInt((float)dimZ / blockSize);
        
        List<int[]> blockDataList = new List<int[]>();
        List<int[]> blockIndexList = new List<int[]>();
        
        int blockVolume = blockSize * blockSize * blockSize;

        for (int bx = 0; bx < blocksX; bx++)
        {
            for (int by = 0; by < blocksY; by++)
            {
                for (int bz = 0; bz < blocksZ; bz++)
                {
                    int startX = bx * blockSize;
                    int startY = by * blockSize;
                    int startZ = bz * blockSize;
                    
                    int[] blockData = new int[blockVolume];
                    bool hasData = false;
                    int idx = 0;
                    
                    for (int lx = 0; lx < blockSize; lx++)
                    {
                        for (int ly = 0; ly < blockSize; ly++)
                        {
                            for (int lz = 0; lz < blockSize; lz++)
                            {
                                int gx = startX + lx;
                                int gy = startY + ly;
                                int gz = startZ + lz;
                                
                                int val = 0;
                                if (gx < dimX && gy < dimY && gz < dimZ)
                                {
                                    val = voxelData[gx, gy, gz];
                                }
                                blockData[idx++] = val;
                                if (val != 0) hasData = true;
                            }
                        }
                    }
                    
                    if (hasData)
                    {
                        blockDataList.Add(blockData);
                        blockIndexList.Add(new int[] { bx, by, bz });
                    }
                }
            }
        }
        
        int numBlocks = blockDataList.Count;
        
        // Save Data: [N, blockSize, blockSize, blockSize]
        int[] flatData = new int[numBlocks * blockVolume];
        for (int i = 0; i < numBlocks; i++)
        {
            Array.Copy(blockDataList[i], 0, flatData, i * blockVolume, blockVolume);
        }
        NDArray npData = np.array(flatData).reshape(new int[] { numBlocks, blockSize, blockSize, blockSize });
        string dataName = $"{baseName}_Data.npy";
        np.save(Path.Combine(folder, dataName), npData);
        
        // Save Index: [N, 3]
        int[] flatIndex = new int[numBlocks * 3];
        for (int i = 0; i < numBlocks; i++)
        {
            flatIndex[i * 3 + 0] = blockIndexList[i][0];
            flatIndex[i * 3 + 1] = blockIndexList[i][1];
            flatIndex[i * 3 + 2] = blockIndexList[i][2];
        }
        NDArray npIndex = np.array(flatIndex).reshape(new int[] { numBlocks, 3 });
        string indexName = $"{baseName}_Index.npy";
        np.save(Path.Combine(folder, indexName), npIndex);
        
        MapLayerData layerData = new MapLayerData();
        layerData.format = "block_sparse";
        layerData.blockSize = blockSize;
        layerData.dataFile = dataName;
        layerData.indexFile = indexName;
        layerData.blockCount = numBlocks;
        
        return layerData;
    }

    // ================== Dense & Sparse COO Export ==================

    private void ExportVoxelDataAsDense(int[,,] voxelMask, string filePath)
    {
        int dimX = voxelMask.GetLength(0);
        int dimY = voxelMask.GetLength(1);
        int dimZ = voxelMask.GetLength(2);
        
        int total = dimX * dimY * dimZ;
        int[] flatArray = new int[total];
        int index = 0;
        
        // Optimized flattening
        for (int x = 0; x < dimX; x++)
        for (int y = 0; y < dimY; y++)
        for (int z = 0; z < dimZ; z++)
            flatArray[index++] = voxelMask[x, y, z];
        
        NDArray npArray = np.array(flatArray).reshape(new int[] { dimX, dimY, dimZ });
        np.save(filePath, npArray);
        Debug.Log($"   -> Saved Dense {Path.GetFileName(filePath)}");
    }

    private void ExportVoxelDataAsSparseCOO(int[,,] voxelMask, string filePath)
    {
        int dimX = voxelMask.GetLength(0);
        int dimY = voxelMask.GetLength(1);
        int dimZ = voxelMask.GetLength(2);
        
        List<int> sparseList = new List<int>();
        int count = 0;

        for (int x = 0; x < dimX; x++)
        {
            for (int y = 0; y < dimY; y++)
            {
                for (int z = 0; z < dimZ; z++)
                {
                    int val = voxelMask[x, y, z];
                    if (val != 0)
                    {
                        sparseList.Add(x);
                        sparseList.Add(y);
                        sparseList.Add(z);
                        sparseList.Add(val);
                        count++;
                    }
                }
            }
        }
        
        var flatArray = sparseList.ToArray();
        // N行 4列
        NDArray npArray = np.array(flatArray).reshape(new int[] { count, 4 });
        np.save(filePath, npArray);
        Debug.Log($"   -> Saved SparseCOO {Path.GetFileName(filePath)} (Count={count})");
    }


    // ================== Core Sampling Logic ==================

    private int[,,] SampleVoxelGrid(Bounds bounds, Vector3 step, out int[] dims, QHSyncColliderTag filterTag)
    {
        float raycastStartY = bounds.max.y;
        float actualMinY = bounds.min.y;

        int resX = Mathf.CeilToInt((bounds.max.x - bounds.min.x) / step.x);
        int resZ = Mathf.CeilToInt((bounds.max.z - bounds.min.z) / step.z);
        float heightY = raycastStartY - actualMinY;
        int resY = Mathf.CeilToInt(heightY / step.y);
        
        dims = new int[] { resX, resY, resZ };
        int[,,] voxelData = new int[resX, resY, resZ];

        float minX = bounds.min.x;
        float minY = actualMinY; 
        float minZ = bounds.min.z;
        
        int waterLayerMask = 1 << LayerMask.NameToLayer("Water");
        float sphereRadius = Mathf.Min(step.x, Mathf.Min(step.y, step.z)) * 0.45f;
        var terrain = QHTerrainCached;

        for (int x = 0; x < resX; x++)
        {
            float sampleX = minX + x * step.x;
            for (int z = 0; z < resZ; z++)
            {
                float sampleZ = minZ + z * step.z;
                
                Vector3 colOrigin = new Vector3(sampleX, raycastStartY + 10f, sampleZ);
                Ray colRay = new Ray(colOrigin, Vector3.down);
                
                float terrainHeight = -9999f;
                bool hasWater = Physics.Raycast(colRay, Mathf.Infinity, waterLayerMask, QueryTriggerInteraction.Collide);
                
                if (hasWater)
                {
                    terrainHeight = terrain.GetHeight(new Vector3(sampleX, 0, sampleZ));
                }

                for (int y = 0; y < resY; y++)
                {
                    float sampleY = minY + y * step.y;
                    Vector3 samplePos = new Vector3(sampleX, sampleY, sampleZ);

                    if (filterTag != null)
                    {
                        if (!filterTag.IsHit(samplePos)) continue;
                    }

                    if (hasWater && sampleY < 0 && sampleY > terrainHeight)
                    {
                        voxelData[x, y, z] |= FLAG_WATER;
                    }
                    
                    Collider[] colliders = Physics.OverlapSphere(samplePos, sphereRadius, ~0, QueryTriggerInteraction.Collide);
                    foreach (var col in colliders)
                    {
                        var tagComp = col.GetComponent<QHSyncColliderTag>();
                        if (tagComp != null)
                        {
                            voxelData[x, y, z] |= GetFlagFromAreaType(tagComp.AreaType);
                        }
                    }
                }
            }
        }
        return voxelData;
    }

    private int GetFlagFromAreaType(QHColliderAreaType type)
    {
        switch (type)
        {
            case QHColliderAreaType.WaterGrass: return FLAG_WATER_GRASS;
            case QHColliderAreaType.Stone:      return FLAG_STONE;
            case QHColliderAreaType.Driftwood:  return FLAG_DRIFTWOOD;
            case QHColliderAreaType.PIER:       return FLAG_PIER;
            case QHColliderAreaType.DeepPit:    return FLAG_DEEP_PIT;
            case QHColliderAreaType.Ridge:      return FLAG_RIDGE;
            case QHColliderAreaType.Fault:      return FLAG_FAULT;
            case QHColliderAreaType.RockShelf:  return FLAG_ROCK_SHELF;
            case QHColliderAreaType.Bay:        return FLAG_BAY;
            case QHColliderAreaType.Mud:        return FLAG_MUD;
            case QHColliderAreaType.Gravel:     return FLAG_GRAVEL;
            default: return 0;
        }
    }

    private void GenerateTerrainColliders()
    {
        GameObject patchObj = GameObject.Find("patch");
        if (patchObj == null ||
            patchObj.layer != LayerMask.NameToLayer("Terrain") ||
            patchObj.transform.parent == null ||
            patchObj.transform.parent.name != "Terrain Collider")
        {
            var terrain = GameObject.FindObjectOfType<QHTerrain>();
            if (terrain != null) terrain.CreateTerrainCollider();
            else Debug.LogError("未找到 QHTerrain 脚本，无法生成 Terrain Collider。");
        }
    }

    private EnvTileExportCfgPerMap GetSceneConfig(string sceneName)
    {
        foreach (var mapCfg in perMapConfigs)
        {
            if (mapCfg.sceneAsset == null) continue;
            if (mapCfg.sceneAsset.name == sceneName) return mapCfg;
        }
        return null;
    }

    private float CalculateMaxDepth(Bounds bounds, EnvTileExportCfgPerMap cfg)
    {
        QHTerrain terrain = GameObject.FindObjectOfType<QHTerrain>();
        if (terrain == null) return 0;

        float minX = bounds.min.x;
        float minZ = bounds.min.z;
        float sizeX = bounds.size.x;
        float sizeZ = bounds.size.z;
        
        int resX = (int)(sizeX / cfg.samplingStep.x);
        int resZ = (int)(sizeZ / cfg.samplingStep.z);
        float maxDepth = 0;

        for (int x = 0; x < resX; x += 5)
        {
            for (int z = 0; z < resZ; z += 5)
            {
                Vector3 samplePos = new Vector3(minX + x * cfg.samplingStep.x, 0, minZ + z * cfg.samplingStep.z);
                var height = terrain.GetHeight(samplePos); 
                if (height < 0 && -height > maxDepth)
                {
                    maxDepth = -height;
                }
            }
        }
        return maxDepth;
    }
    
    private float GetMinYInBounds(Bounds bounds, EnvTileExportCfgPerMap cfg)
    {
         return -CalculateMaxDepth(bounds, cfg);
    }

    private void EnsureTerrainInitialized()
    {
        var terrain = QHTerrainCached;
        if (terrain != null)
        {
            if (terrain.transform.Find("collider") == null)
            {
                Debug.Log("Export: 检测到 QHTerrain 未初始化，正在手动调用 Run()...");
                Camera cam = GameObject.FindObjectOfType<Camera>();
                if (cam != null) terrain.Run(cam);
            }
        }
    }

    EnvTileExportCfgPerMap CreateDefaultCfgForCurrScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        var sceneName = Path.GetFileNameWithoutExtension(activeScene.path);
        var mapIDStr = sceneName.Substring(sceneName.LastIndexOf('_') + 1);
        int mapID = 0;
        int.TryParse(mapIDStr, out mapID);

        var defaultCfg = new EnvTileExportCfgPerMap();
        defaultCfg.sceneAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(activeScene.path);
        defaultCfg.mapID = mapID;
        perMapConfigs.Add(defaultCfg);
        return defaultCfg;
    }
}

public enum ExportMode
{
    Dense,       // 完整Numpy
    SparseCOO,   // 坐标列表 (N,4)
    BlockSparse  // 分块稀疏 (Data+Index)
}

[System.Serializable]
public class EnvTileExportCfgPerMap
{
    [TableColumnWidth(50)]
    public UnityEditor.SceneAsset sceneAsset;

    [ReadOnly]
    [TableColumnWidth(50)]
    public int mapID;

    [TableColumnWidth(50)]
    public float raycastStartY = 0.5f;

    public Vector3 samplingStep = new Vector3(1f, 1f, 1f);
    
    [LabelText("局部池采样步长(XZ)")]
    public float localStockStepXZ = 0.5f; 

    [LabelText("导出模式")]
    public ExportMode exportMode = ExportMode.BlockSparse;

    [LabelText("Block Size")]
    [ShowIf("exportMode", ExportMode.BlockSparse)]
    public int blockSize = 16;

    public Vector3 scopeTriggerBoxSize = new Vector3(100f, 20f, 100f);
    public Vector3 scopeTriggerBoxCenter = new Vector3(0f, -9f, 0f);
}

// ================== Metadata Structs ==================
[System.Serializable]
public class MapExportData
{
    public int mapID;
    public string timestamp;
    public MapLayerData global;
    public List<MapLayerData> locals = new List<MapLayerData>();
}

[System.Serializable]
public class MapLayerData
{
    public string name;
    public string type;
    public string format;       // "dense", "sparse_coo", "block_sparse"
    public int blockSize;
    public string dataFile;
    public string indexFile;
    public int blockCount;
    public float[] origin; 
    public float[] step;
    public int[] dim;
}
��"(4500bf11159f7dd297cafd329a276bd8a9c29b7b2Dfile:///d:/fishinggame/Assets/EditorTools/Editor/EnvTileExportCfg.cs:file:///d:/fishinggame