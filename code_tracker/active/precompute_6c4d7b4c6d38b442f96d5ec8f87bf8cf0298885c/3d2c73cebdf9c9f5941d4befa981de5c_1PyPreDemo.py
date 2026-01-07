�X### 获取配置并组装(只需要做一次)

# 使用  precompute\data\1\1001 目录下的json文件作为数据源；然后整理出需要的numpy数组，以加速并行计算；
# 举例：
# <!-- TODO: agent
# 这一部分准备在ipynb中一步步摸索，然后把案例整理到这里
# -->
# * 下面我们一步步的找数据，进行探索和整合：
# * 对于 D:\fishinggame\ExportedData\Fishing_1006001_Dense_20260107_154037\Fishing_1006001_Global.npy，
# 先截取中其中的1006001，也就是scene id，然后到D:\fishinggame\precompute\data\1\1001\map_scene.json中，找到对应的map id

import numpy as np
import json
import re
from pathlib import Path

# 配置路径
DATA_ROOT = Path(r'D:\fishinggame\precompute\data\1\1001')
EXPORTED_DATA_ROOT = Path(r'D:\fishinggame\ExportedData')

# 加载 map_scene.json
with open(DATA_ROOT / 'map_scene.json', 'r', encoding='utf-8') as f:
    map_scene = json.load(f)

# 建立 assetId -> map_id 的反向索引
asset_to_map = {info['assetId']: int(map_id) for map_id, info in map_scene.items() if info.get('assetId')}
print(f'已加载 {len(map_scene)} 个地图配置，其中 {len(asset_to_map)} 个有 assetId')
print(f'assetId -> map_id 映射: {asset_to_map}')

def get_scene_id_from_path(npy_path: str) -> str:
    """从npy文件路径中提取scene_id (如 Fishing_1006001_Global.npy -> '1006001')"""
    match = re.search(r'Fishing_(\d+)', str(npy_path))
    if not match:
        raise ValueError(f'无法从路径中提取scene_id: {npy_path}')
    return match.group(1)

def get_map_id_from_scene_id(scene_id: str) -> int:
    """根据scene_id查找对应的map_id"""
    if scene_id in asset_to_map:
        return asset_to_map[scene_id]
    raise ValueError(f'找不到scene_id {scene_id} 对应的map_id')

def get_map_id_from_npy_path(npy_path: str) -> int:
    """从npy文件路径直接获取map_id"""
    scene_id = get_scene_id_from_path(npy_path)
    return get_map_id_from_scene_id(scene_id)

# 测试示例
test_path = r'D:\fishinggame\ExportedData\Fishing_1006001_Dense_20260107_154037\Fishing_1006001_Global.npy'
scene_id = get_scene_id_from_path(test_path)
map_id = get_map_id_from_scene_id(scene_id)
print(f'文件路径: {test_path}')
print(f'提取的 scene_id: {scene_id}')
print(f'对应的 map_id: {map_id}')
print(f'地图信息: {map_scene[str(map_id)]}')

# ============================================================
# 第二步：加载更多配置表并建立索引
# ============================================================

# 加载 fish_stock.json (鱼塘/Stock 配置)
with open(DATA_ROOT / 'fish_stock.json', 'r', encoding='utf-8') as f:
    fish_stock = json.load(f)
print(f'已加载 {len(fish_stock)} 个 fish_stock 配置')

# 加载 stock_release.json (Stock 中的鱼种投放配置)
with open(DATA_ROOT / 'stock_release.json', 'r', encoding='utf-8') as f:
    stock_release = json.load(f)
print(f'已加载 {len(stock_release)} 个 stock_release 配置')

# 建立 stockId -> [release_info] 的索引
stock_to_releases: dict[int, list] = {}
for release_id, release_info in stock_release.items():
    stock_id = release_info['stockId']
    if stock_id not in stock_to_releases:
        stock_to_releases[stock_id] = []
    stock_to_releases[stock_id].append(release_info)
print(f'已建立 stockId -> releases 索引，共 {len(stock_to_releases)} 个 stock')

# 建立 stock_name -> stock_id 的索引 (用于匹配 LocalStock 的 name)
stock_name_to_id = {info['name']: int(stock_id) for stock_id, info in fish_stock.items()}
print(f'stock_name -> stock_id 映射: {stock_name_to_id}')

# ============================================================
# 第三步：发现并加载 ExportedData 中的 npy 文件
# ============================================================

def find_latest_export_dir(scene_id: str) -> Path | None:
    """查找指定 scene_id 的最新导出目录 (带时间戳的 Dense 目录)"""
    pattern = f'Fishing_{scene_id}_Dense_*'
    matches = list(EXPORTED_DATA_ROOT.glob(pattern))
    if not matches:
        return None
    # 按目录名排序，取最新的 (时间戳在目录名末尾)
    return sorted(matches)[-1]

def load_map_data(export_dir: Path) -> dict:
    """加载导出目录中的 map_data.json"""
    map_data_path = export_dir / 'map_data.json'
    if not map_data_path.exists():
        raise FileNotFoundError(f'找不到 map_data.json: {map_data_path}')
    with open(map_data_path, 'r', encoding='utf-8') as f:
        return json.load(f)

def load_voxel_data(export_dir: Path, data_file: str) -> np.ndarray:
    """加载 npy 文件"""
    npy_path = export_dir / data_file
    if not npy_path.exists():
        raise FileNotFoundError(f'找不到 npy 文件: {npy_path}')
    return np.load(npy_path)

class VoxelLayer:
    """体素层数据封装，支持世界坐标与体素索引转换"""
    def __init__(self, name: str, layer_type: str, data: np.ndarray, 
                 origin: tuple[float, float, float], 
                 step: tuple[float, float, float],
                 dim: tuple[int, int, int]):
        self.name = name
        self.layer_type = layer_type  # 'Global' or 'LocalStock'
        self.data = data
        self.origin = np.array(origin)
        self.step = np.array(step)
        self.dim = np.array(dim)
        
    def world_to_voxel(self, world_pos: np.ndarray) -> np.ndarray:
        """世界坐标转体素索引 (不含边界检查)"""
        return np.floor((world_pos - self.origin) / self.step).astype(int)
    
    def voxel_to_world(self, voxel_idx: np.ndarray) -> np.ndarray:
        """体素索引转世界坐标 (体素中心)"""
        return self.origin + (voxel_idx + 0.5) * self.step
    
    def is_valid_index(self, idx: np.ndarray) -> bool:
        """检查体素索引是否在有效范围内"""
        return np.all(idx >= 0) and np.all(idx < self.dim)
    
    def get_value_at_world(self, world_pos: np.ndarray) -> int | None:
        """获取世界坐标处的体素值，越界返回 None"""
        idx = self.world_to_voxel(world_pos)
        if not self.is_valid_index(idx):
            return None
        return int(self.data[idx[0], idx[1], idx[2]])
    
    def __repr__(self):
        return f'VoxelLayer({self.name}, type={self.layer_type}, dim={tuple(self.dim)}, origin={tuple(self.origin)})'

class MapVoxelData:
    """地图体素数据集合，包含 Global 和所有 LocalStock"""
    def __init__(self, map_id: int, export_dir: Path):
        self.map_id = map_id
        self.export_dir = export_dir
        self.map_data = load_map_data(export_dir)
        
        # 加载 Global 层
        global_info = self.map_data['global']
        global_data = load_voxel_data(export_dir, global_info['dataFile'])
        self.global_layer = VoxelLayer(
            name=global_info['name'],
            layer_type=global_info['type'],
            data=global_data,
            origin=tuple(global_info['origin']),
            step=tuple(global_info['step']),
            dim=tuple(global_info['dim'])
        )
        print(f'已加载 Global 层: {self.global_layer}')
        
        # 加载所有 LocalStock 层
        self.local_layers: list[VoxelLayer] = []
        self.local_stock_map: dict[str, list[VoxelLayer]] = {}  # stock_name -> [layers]
        
        for local_info in self.map_data.get('locals', []):
            local_data = load_voxel_data(export_dir, local_info['dataFile'])
            layer = VoxelLayer(
                name=local_info['name'],
                layer_type=local_info['type'],
                data=local_data,
                origin=tuple(local_info['origin']),
                step=tuple(local_info['step']),
                dim=tuple(local_info['dim'])
            )
            self.local_layers.append(layer)
            
            # 按 stock_name 分组
            stock_name = local_info['name']
            if stock_name not in self.local_stock_map:
                self.local_stock_map[stock_name] = []
            self.local_stock_map[stock_name].append(layer)
            print(f'已加载 LocalStock 层: {layer}')
        
        print(f'地图 {map_id} 共加载 1 个 Global 层和 {len(self.local_layers)} 个 LocalStock 层')
    
    def query_at_world(self, world_pos: np.ndarray) -> dict:
        """查询世界坐标处的所有体素信息"""
        result = {
            'global': self.global_layer.get_value_at_world(world_pos),
            'locals': {}
        }
        for stock_name, layers in self.local_stock_map.items():
            for i, layer in enumerate(layers):
                val = layer.get_value_at_world(world_pos)
                if val is not None:
                    key = f'{stock_name}_{i}' if len(layers) > 1 else stock_name
                    result['locals'][key] = val
        return result
    
    def get_fishable_stocks_at(self, world_pos: np.ndarray) -> list[str]:
        """获取世界坐标处可钓的所有 stock_name (体素值 > 0)"""
        stocks = []
        for stock_name, layers in self.local_stock_map.items():
            for layer in layers:
                val = layer.get_value_at_world(world_pos)
                if val is not None and val > 0:
                    stocks.append(stock_name)
                    break  # 同名 stock 只需一个匹配即可
        return stocks

# ============================================================
# 测试：加载示例地图数据
# ============================================================

# 使用之前测试的 scene_id
test_export_dir = find_latest_export_dir(scene_id)
if test_export_dir:
    print(f'\n找到导出目录: {test_export_dir}')
    map_voxel_data = MapVoxelData(map_id, test_export_dir)
    
    # 测试查询 - 使用一个在 LocalStock 范围内的坐标
    # stock_sunset_zone_t4 的 origin 在 (159.34, -4.51, -57.65) 附近
    test_world_pos = np.array([165.0, -3.0, -30.0])
    query_result = map_voxel_data.query_at_world(test_world_pos)
    print(f'\n在世界坐标 {test_world_pos} 处的体素信息:')
    print(f'  Global 值: {query_result["global"]}')
    print(f'  LocalStock 值: {query_result["locals"]}')
    
    fishable_stocks = map_voxel_data.get_fishable_stocks_at(test_world_pos)
    print(f'  可钓 Stock: {fishable_stocks}')
    
    # 查询该位置可钓的鱼种
    for stock_name in fishable_stocks:
        if stock_name in stock_name_to_id:
            stock_id = stock_name_to_id[stock_name]
            releases = stock_to_releases.get(stock_id, [])
            print(f'\n  Stock "{stock_name}" (id={stock_id}) 包含 {len(releases)} 种鱼:')
            for r in releases[:5]:  # 只显示前5种
                print(f'    - fishId: {r["fishId"]}, fishEnvId: {r["fishEnvId"]}')
            if len(releases) > 5:
                print(f'    ... 还有 {len(releases) - 5} 种')
else:
    print(f'\n警告: 找不到 scene_id={scene_id} 的导出目录')

# 现在用map id去fish stock.json中找对应的fish stock
�X"(6c4d7b4c6d38b442f96d5ec8f87bf8cf0298885c2/file:///d:/fishinggame/precompute/1PyPreDemo.py:!file:///d:/fishinggame/precompute