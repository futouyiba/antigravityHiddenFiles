# 多模式导出实现

## 目标
支持三种导出模式，并按要求命名输出文件夹。

## 变化点
1.  **ExportMode 枚举**:
    *   `Dense`: 完整 Numpy 数组 `[N, M, H]`
    *   `SparseCOO`: 稀疏坐标列表 `[K, 4]` (x, y, z, val)
    *   `BlockSparse`: 分块稀疏 `Data[B, 16, 16, 16]` + `Index[B, 3]`

2.  **文件夹命名**:
    *   `ExportFolder/{SceneName}_{Mode}_{yyyyMMdd_HHmmss}`

3.  **EnvTileExportCfg.cs 修改**:
    *   `EnvTileExportCfgPerMap` 移除 `exportSparse`，增加 `exportMode`。
    *   `ExportVoxelData`:
        *   生成带时间戳的文件夹路径。
        *   根据 `exportMode` 分支调用不同的导出方法。
        *   填充 `MapLayerData.format` 字段。

## 验证
*   分别选择三种模式导出，检查文件夹名称和文件内容格式。
