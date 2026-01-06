# 停用 GitHub LFS 并迁移至普通 Git

目标是移除 GitHub LFS 的依赖，停止产生存储费用，同时将所有文件内容完整地保存在普通的 Git 仓库中。由于你的 LFS 数据总量约为 2GB，且单个文件均未超过 100MB（GitHub 普通仓库限制），我们可以安全地进行转换。

## 需要用户确认

> [!IMPORTANT]
> 此过程涉及 **重写 Git 历史记录**。
> 1. 你需要对 GitHub 进行 **强制推送 (Force Push)**，这会影响其他协作者。
> 2. 为了彻底停止 GitHub 的计费，你可能需要 **删除并重新创建 GitHub 仓库**，或者联系 GitHub 客服清除后台缓存的 LFS 对象（即使客户端已删除，服务器有时仍会保留）。

> [!WARNING]
> 在开始之前，请务必 **手动备份你的本地项目文件夹**。

## 提议的变更步骤

我们将使用 `git lfs migrate` 工具将所有 LFS 跟踪的文件转回普通 Git 对象。

### 1. 识别并拉取所有 LFS 数据
确保本地拥有完整的 LFS 对象，避免迁移时丢失内容。
- `git lfs fetch --all`

### 2. 导出 LFS 对象为普通 Git 对象
针对所有分支执行迁移命令。
- `git lfs migrate export --everything --include="*"`

### 3. 清理 .gitattributes
移除 `.gitattributes` 中关于 LFS 的配置，防止新文件再次被 LFS 追踪。

### 4. 推送到 GitHub
将重写后的历史强制推送到远程。
- `git push --force --all`

## 验证计划

### 自动检查
- 检查 `.gitattributes` 是否还包含 `filter=lfs`。
- 运行 `git lfs ls-files`，确保返回列表为空。

### 手动验证
- 验证虚幻引擎 (Unreal Engine) 是否能正常打开项目，且所有资源（模型、贴图）均未损坏。
- 检查 GitHub 仓库设置中的 LFS 使用情况（可能需要一段时间才会刷新）。
