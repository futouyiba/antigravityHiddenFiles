��using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using NumSharp;
using UnityEditor;
using System;

[CreateAssetMenu(fileName = "MapExportGlobalConfig", menuName = "Export/Map Global Config", order = 1)]
public class EnvTileExportCfg : SerializedScriptableObject
{
    // [LabelText("采样体 Tag")]
    public string samplingVolumeTag = "Exporter";

    // [LabelText("导出文件名前缀")]
    public string filePrefix = "MapExport_";

    // [LabelText("输出路径")]
    [FolderPath]
    public string exportFolder = "D:/ExportedData";

    // [LabelText("地图配置项")]
    [TableList]
    public List<EnvTileExportCfgPerMap> perMapConfigs = new List<EnvTileExportCfgPerMap>();

    public Transform theSamplingVolume;
    // 在最大深度基础上加一个保险值，保证底部不突出去
    public float extraSafeDepth = 1.5f;

    // 添加 bit mask 常量定义：
    private const int WATER_FLAG = 1 << 0;
    private const int WATER_GRASS_FLAG = 1 << 1;
    private const int STONE_FLAG = 1 << 2;
    private const int DRIFTWOOD_FLAG = 1 << 3;

    private QHTerrain _qhTerrainInstance;
    protected QHTerrain QHTerrainCached {
        get{
            if (_qhTerrainInstance == null){
                _qhTerrainInstance = GameObject.FindObjectOfType<QHTerrain>();
            }
            return _qhTerrainInstance;
        }
    }

    // 新增按钮，根据当前场景对应的采样区域参数，生成采样区域的trigger box
    [Button("显示采样区域")]
    public void PlaceActualScopeTriggerBox()
    {
        // operate at the current scene within the editor
        Scene currentScene = SceneManager.GetActiveScene();
        // if (currentScene.isDirty)
        // {
        //     Debug.LogError("请先保存当前场景！");
        //     return;
        // }
        // get scene name, and get the suffix number as map ID, and get structure
        var sceneName = Path.GetFileNameWithoutExtension(currentScene.path);
        // get the map ID from the scene name, which is the substring after the last underscore
        var mapIDStr = sceneName.Substring(sceneName.LastIndexOf('_') + 1);

        EnvTileExportCfgPerMap cfgForCurrScene = null;
        foreach (var mapCfg in perMapConfigs)
        {
            // find the config for the map,
            if (mapCfg.sceneAsset == null)
            {
                Debug.LogError($"地图 {mapCfg.mapID} 的 Scene 未设置！");
            }
            // check if the scene asset is the same as the current scene
            if (mapCfg.sceneAsset.name == sceneName)
            {
                Debug.Log($"地图 {mapCfg.mapID} 的 Scene 与当前 Scene 一致,使用此设置");
                cfgForCurrScene = mapCfg;
                break;
            }
        }

        // if null, create a default cfg, remind user and continue with the new configuration.
        if (cfgForCurrScene == null)
        {
            cfgForCurrScene = this.CreateDefaultCfgForCurrScene();
        }

        // find the game object with the tag, and activate it if found; finally, deactivate it after sampling
        GameObject samplingVolume = GameObject.FindWithTag(samplingVolumeTag);
        if (samplingVolume != null)
        {
            samplingVolume.SetActive(true);
            // get size x, z axis of the sampling volume
            var samplingVolumeSize = samplingVolume.GetComponent<BoxCollider>().size;
            // get the center of the sampling volume
            var samplingVolumeCenter = samplingVolume.transform.position;

            // 询问用户，是否要将这个找到的trigger box的拓扑信息保存到对应的地图cfg中，覆盖之前的配置
            if (cfgForCurrScene != null)
            {
                if (EditorUtility.DisplayDialog("提示", "是否要将找到的采样区域信息保存到地图配置中？", "是", "否"))
                {
                    cfgForCurrScene.scopeTriggerBoxSize = samplingVolumeSize;
                    cfgForCurrScene.scopeTriggerBoxCenter = samplingVolumeCenter;
                }
            }
            else
            {
                Debug.LogWarning("已根据场景中的采样区域拓扑信息创建了新的地图配置项.");
            }

            theSamplingVolume = samplingVolume.transform;
            return;
        }
        Debug.Log($"未找到 Tag 为 {samplingVolumeTag} 的 GameObject，开始创建...");

        // create a trigger box with the same size as the sampling volume
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
            // TODO 后续加逻辑，询问用户是否要保存当前的采样区域信息到地图配置中
            samplingVolume.SetActive(false);
            DestroyImmediate(samplingVolume);
            theSamplingVolume = null;
        }
    }

    // 新增按钮，点击后执行采样导出
    [Button("采样导出")]
    public void ExportVoxelData()
    {
        // EditorApplication.EnterPlaymode();
        // TODO enter playmode add callback to get
        // operate at the current scene within the editor
        Scene currentScene = SceneManager.GetActiveScene();
        var sceneName = Path.GetFileNameWithoutExtension(currentScene.path);

        // check if there is no cam within the scene, create a dummy one...
        if ( GameObject.FindObjectOfType<Camera>() == null)
        {        // place a dummy camera in the scene origin place
            GameObject dummyCameraObj = new GameObject("DummyCamera");
            dummyCameraObj.transform.position = Vector3.zero;
            dummyCameraObj.AddComponent<Camera>();
        }
        GameObject.FindObjectOfType<MapCtrl>().isTest = true;

        // if unity is not in play mode, push it to play mode, show a dialog, and return
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.EnterPlaymode();
            EditorUtility.DisplayDialog("提示", "等待Unity进入PlayMode，然后重新点击采样导出。", "确定");   
            return;
        }

        // 生成 Terrain Collider（已抽取成独立函数）
        GenerateTerrainColliders();

        // 确保 Terrain 运行时数据已初始化 (MTLoader ready)
        EnsureTerrainInitialized();

        if (theSamplingVolume == null)
        {
            PlaceActualScopeTriggerBox();
            if (!EditorUtility.DisplayDialog("提示", "在场景中放置了范围框，继续导出吗？", "继续导出", "取消导出，先调整采样区域"))
            {
                return;
            }
        }

        var samplingVolume = theSamplingVolume.gameObject;
        var samplingVolumeSize = samplingVolume.GetComponent<BoxCollider>().size;
        var samplingVolumeCenter = samplingVolume.transform.position;
        var minX = samplingVolumeCenter.x - samplingVolumeSize.x / 2;
        var minZ = samplingVolumeCenter.z - samplingVolumeSize.z / 2;

        EnvTileExportCfgPerMap cfgForCurrScene = GetSceneConfig(sceneName);
        if (cfgForCurrScene == null)
        {
            cfgForCurrScene = CreateDefaultCfgForCurrScene();
            return;
        }

        // 根据采样区域参数计算最大深度（逻辑从主函数中抽取）
        float maxDepth = CalculateMaxDepth(minX, minZ, cfgForCurrScene, samplingVolumeSize);
        if (EditorUtility.DisplayDialog("提示", $"计算得到的最大深度为 {maxDepth}，请确认是否正确。", "确定", "不对，取消导出"))
        {
            Debug.Log("开始导出体素采样数据...");
        }
        else
        {
            return;
        }

        int resY = (int)((maxDepth + extraSafeDepth) / cfgForCurrScene.samplingStep.y);

        int resX = (int)(samplingVolumeSize.x / cfgForCurrScene.samplingStep.x);
        int resZ = (int)(samplingVolumeSize.z / cfgForCurrScene.samplingStep.z);

        int[,,] voxelMask = new int[resX, resY, resZ];


        // 假定每个体素间隔为 1 单位，网格居中
        float offsetX = resX / 2.0f;
        float offsetZ = resZ / 2.0f;
        float sphereRadius = 0.1f;

        int waterLayer = LayerMask.NameToLayer("Water");
        int waterLayerMask = 1 << waterLayer;
        // int terrainLayer = LayerMask.NameToLayer("Terrain");

        var mapIDStr = sceneName.Substring(sceneName.LastIndexOf('_') + 1);
        var mapID = int.Parse(mapIDStr);
        
        var structurePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Res/Base/Prefab/level/Structure_{mapIDStr}.prefab");
        if (structurePrefab != null) {
            GameObject.Instantiate(structurePrefab);
        }

        // 遍历所有采样点，采样点在世界坐标中的计算采用采样体积左下角作为起点
        for (int x = 0; x < resX; x++)
        {
            // 横向世界坐标
            float sampleX = minX + x * cfgForCurrScene.samplingStep.x;
            for (int z = 0; z < resZ; z++)
            {
                float waterDepth = 1f;
                // 纵向世界坐标
                float sampleZ = minZ + z * cfgForCurrScene.samplingStep.z;
                // 针对当前列 (x,z) ，先做一次列射线检测，获得水层区域
                Vector3 colOrigin = new Vector3(sampleX, cfgForCurrScene.raycastStartY, sampleZ);
                Ray colRay = new Ray(colOrigin, Vector3.down);

                var waterFound = Physics.Raycast(colRay, Mathf.Infinity, waterLayerMask, QueryTriggerInteraction.Collide);

                if (waterFound){
                    waterDepth = QHTerrainCached.GetHeight(colOrigin);
                    // TODO
                }

                // 在当前列内，对每个 y 进行采样
                for (int y = 0; y < resY; y++)
                {
                    // 垂直方向的采样使用采样步长计算
                    float sampleY = -0.5f - y * cfgForCurrScene.samplingStep.y;
                    // 如果sampleY小于0，且sampleY大于水深的负值，就应该在水中，应该设置水标志位
                    if (sampleY < 0 && sampleY > waterDepth)
                    {
                        voxelMask[x, y, z] |= WATER_FLAG;
                    }

                    var samplePos = new Vector3(sampleX, sampleY, sampleZ);

                    // 利用小球检测获得水草、石头、沉木等状态，根据情况设置对应的bit
                    Collider[] colliders = Physics.OverlapSphere(samplePos, sphereRadius, ~0, QueryTriggerInteraction.Collide);
                    foreach (var col in colliders)
                    {
                        var tagComp = col.GetComponent<QHSyncColliderTag>();
                        if (tagComp != null)
                        {
                            switch (tagComp.AreaType)
                            {
                                case QHColliderAreaType.WaterGrass:
                                    voxelMask[x, y, z] |= WATER_GRASS_FLAG;
                                    break;
                                case QHColliderAreaType.Stone:
                                    voxelMask[x, y, z] |= STONE_FLAG;
                                    break;
                                case QHColliderAreaType.Driftwood:
                                    voxelMask[x, y, z] |= DRIFTWOOD_FLAG;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }

        // 确保导出文件夹存在
        if (!Directory.Exists(exportFolder))
            Directory.CreateDirectory(exportFolder);

        string prefix = filePrefix + cfgForCurrScene.mapID + "_" + DateTime.Now.ToString("yyyyMMddHHmmss_");
        // Save3DArrayAsCSV(voxelMask, Path.Combine(exportFolder, prefix + "voxelMask.csv"));
        ExportVoxelDataAsNumpy(voxelMask, prefix + "voxelMask.npy");

        Debug.Log($"Map {cfgForCurrScene.mapID} 的体素采样导出完成！");
        // EditorApplication.ExitPlaymode();
    }

    private void GenerateTerrainColliders()
    {
        // 将生成 terrain collider 的逻辑抽取为一个独立函数
        GameObject patchObj = GameObject.Find("patch");
        if (patchObj == null ||
            patchObj.layer != LayerMask.NameToLayer("Terrain") ||
            patchObj.transform.parent == null ||
            patchObj.transform.parent.name != "Terrain Collider")
        {
            var terrain = GameObject.FindObjectOfType<QHTerrain>();
            if (terrain != null)
            {
                terrain.CreateTerrainCollider();
            }
            else
            {
                Debug.LogError("未找到 QHTerrain 脚本，无法生成 Terrain Collider。");
            }
        }
    }

    private EnvTileExportCfgPerMap GetSceneConfig(string sceneName)
    {
        foreach (var mapCfg in perMapConfigs)
        {
            if (mapCfg.sceneAsset == null)
                continue;
            if (mapCfg.sceneAsset.name == sceneName)
            {
                Debug.Log($"地图 {mapCfg.mapID} 的 Scene 与当前 Scene 一致,使用此设置");
                return mapCfg;
            }
        }
        return null;
    }

    private float CalculateMaxDepth(float minX, float minZ, EnvTileExportCfgPerMap cfgForCurrScene, Vector3 samplingVolumeSize)
    {
        QHTerrain qHTerrain = GameObject.FindObjectOfType<QHTerrain>();
        if (qHTerrain == null)
        {
            Debug.LogError("未找到 QHTerrain 脚本，无法计算最大深度。");
            return 0;
        }
    
        Vector3 theDeepestPoint = new Vector3(0, 0, 0);
        int resX = (int)(samplingVolumeSize.x / cfgForCurrScene.samplingStep.x);
        int resZ = (int)(samplingVolumeSize.z / cfgForCurrScene.samplingStep.z);
        float maxDepth = 0;
    
        // Initialize progress bar
        for (int x = 0; x < resX; x++)
        {
            float progress = (float)x / resX;
            if (EditorUtility.DisplayCancelableProgressBar("计算最大深度", $"处理 {x + 1}/{resX} 列...", progress))
            {
                Debug.LogWarning("计算最大深度操作已取消。");
                EditorUtility.ClearProgressBar();
                return maxDepth;
            }
    
            for (int z = 0; z < resZ; z++)
            {
                Vector3 samplePos = new Vector3(minX + x * cfgForCurrScene.samplingStep.x, cfgForCurrScene.raycastStartY, minZ + z * cfgForCurrScene.samplingStep.z);
                var height = qHTerrain.GetHeight(samplePos);
                if (height != 0)
                {
                    Debug.Log($"Height at {samplePos} is {height}");
                }
                var depth = -height;
                if (depth > maxDepth)
                {
                    maxDepth = depth;
                    theDeepestPoint = samplePos;
                }
            }
        }
        
        EditorUtility.ClearProgressBar();
        Debug.Log($"最深点坐标：{theDeepestPoint}");
        return maxDepth;
    }

    /// <summary>
    /// 将 3D 整型数组保存为 CSV 文件，第一行写入维度信息，后续按 Y 层输出 X*Z 网格数据。
    /// </summary>
    private void Save3DArrayAsCSV(int[,,] array, string filePath)
    {
        int dimX = array.GetLength(0);
        int dimY = array.GetLength(1);
        int dimZ = array.GetLength(2);

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine($"{dimX},{dimY},{dimZ}");
            for (int y = 0; y < dimY; y++)
            {
                for (int x = 0; x < dimX; x++)
                {
                    string line = "";
                    for (int z = 0; z < dimZ; z++)
                    {
                        line += array[x, y, z];
                        if (z < dimZ - 1)
                            line += ",";
                    }
                    writer.WriteLine(line);
                }
            }
        }
    }

    EnvTileExportCfgPerMap CreateDefaultCfgForCurrScene()
    {
        // if already has a cfg for current scene, show a reminder window and return
        if (perMapConfigs.Exists(cfg => cfg.sceneAsset.name == SceneManager.GetActiveScene().name))
        {
            EditorUtility.DisplayDialog("配置提示", $"尝试创建新配置，但地图 {SceneManager.GetActiveScene().name} 的配置已存在！", "确定");
            return perMapConfigs.Find(cfg => cfg.sceneAsset.name == SceneManager.GetActiveScene().name);
        }

        // get current scene asset, and get the map ID from the scene name
        var activeScene = SceneManager.GetActiveScene();
        var scenePath = activeScene.path;
        var sceneName = Path.GetFileNameWithoutExtension(scenePath);
        var mapIDStr = sceneName.Substring(sceneName.LastIndexOf('_') + 1);
        var mapID = int.Parse(mapIDStr);

        // create a default cfg, add into map
        var defaultCfg = new EnvTileExportCfgPerMap();
        defaultCfg.sceneAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(scenePath);
        defaultCfg.mapID = mapID;
        perMapConfigs.Add(defaultCfg);

        // show a reminder window, and return;
        EditorUtility.DisplayDialog("配置提示", $"未找到地图 {mapID} 的配置，已创建默认配置，\n请在编辑器中设置后再次导出！", "确定");
        return defaultCfg;
    }

    static int GetIndexOfY(float y, float startY, float stepY)
    {
        return (int)((startY - y) / stepY);
    }

    public void ExportVoxelDataAsNumpy(int[,,] voxelMask, string fileName)
    {
        // 获取尺寸
        int dimX = voxelMask.GetLength(0);
        int dimY = voxelMask.GetLength(1);
        int dimZ = voxelMask.GetLength(2);
        
        // 拉平数组（也可以用 LINQ：voxelMask.Cast<int>().ToArray()）
        int total = dimX * dimY * dimZ;
        int[] flatArray = new int[total];
        int index = 0;
        for (int x = 0; x < dimX; x++)
        {
            for (int y = 0; y < dimY; y++)
            {
                for (int z = 0; z < dimZ; z++)
                {
                    flatArray[index++] = voxelMask[x, y, z];
                }
            }
        }
        
        // 创建 NumSharp 的 NDArray，假设它可以接受 int[] 和 shape 数组
        NDArray npArray = np.array(flatArray).reshape(new int[] { dimX, dimY, dimZ });
        
        // 使用传入的文件名构造完整路径
        string filePath = Path.Combine(exportFolder, fileName);
        np.save(filePath, npArray);
        Debug.Log("导出 numpy 数组：" + filePath);
    }

    private void EnsureTerrainInitialized()
    {
        var terrain = QHTerrainCached;
        if (terrain != null)
        {
            // 通过检查子节点 "collider" 是否存在来判断是否已经 Run 过
            // (Run 方法会创建名为 "collider" 的子节点)
            if (terrain.transform.Find("collider") == null)
            {
                Debug.Log("Export: 检测到 QHTerrain 未初始化，正在手动调用 Run()...");
                Camera cam = GameObject.FindObjectOfType<Camera>();
                if (cam == null)
                {
                    // 前面代码已保证场景有 Camera (DummyCamera)，这里再次获取
                    Debug.LogWarning("Export: QHTerrain Run 需要相机，但未找到，尝试使用 DummyCamera 或报错。");
                    return;
                }
                terrain.Run(cam);
            }
        }
    }
}

[System.Serializable]
public class EnvTileExportCfgPerMap
{
    // [LabelText("地图 Scene (拖拽 SceneAsset)")]
    [TableColumnWidth(50)]
    public UnityEditor.SceneAsset sceneAsset;

    // [LabelText("地图 ID")]
    [ReadOnly]
    [TableColumnWidth(50)]
    public int mapID;

    // [LabelText("射线起始点 (Y 坐标)")]
    [TableColumnWidth(50)]
    public float raycastStartY = 0.5f;

    /// <summary>
    /// 采样时，各方向上，每间隔多少距离采样一次
    /// </summary>
    // [LabelText("X/Y/Z 采样间隔")]
    public Vector3 samplingStep = new Vector3(1f, 1f, 1f);

    // 范围盒大小。用范围盒的大小和中心点来确定、存储范围。只有x&z方向的数值有效。
    public Vector3 scopeTriggerBoxSize = new Vector3(100f, 20f, 100f);
    // 范围盒中心点。用范围盒的大小和中心点来确定、存储范围。
    public Vector3 scopeTriggerBoxCenter = new Vector3(0f, -9f, 0f);
}

�5 �5�6*cascade08�6�� ��ȟ*cascade08ȟ�� "(7799193779ce0b657e1ba9b0027d5ebb9563c9492Dfile:///d:/fishinggame/Assets/EditorTools/Editor/EnvTileExportCfg.cs:file:///d:/fishinggame