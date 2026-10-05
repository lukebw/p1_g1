# 地图块与树木布置

Wilderness 现在使用预先制作的 5×5 大地图：先拼地面，再跨地图块统一生成树木，最后手工微调并保存 prefab。生成只在编辑器中点击按钮时执行；进入 Play、隐藏再显示地图都不会重新随机或补树。地图对象数量相同时，随机生成不会自动减少运行时树木的开销。

## 当前游戏大地图（优先编辑这里）

双击 `Assets/Prefab/Maps/MapWorld_Large.prefab`。它已经接入 `Assets/Scenes/Wilderness.unity`，包含七种草地组成的 25 块地面，总尺寸 256×256 世界单位。原来的 16 棵树及其位置保留在 Fixed Trees，玩家起点和教程逻辑保留。场景中的旧 Background 仅停用，便于参考。

- `Terrain Blocks`：各地块及池塘、岩石等避让区域。大地图保存了源地块的独立快照，可在这里改地面 Sprite 和区域；修改七张源 MapBlock prefab 不会自动改变此大地图。每块是 ManualOnly，树木从大地图根节点统一生成。
- `Fixed Trees - original tutorial grove`：保留位置的可砍树。把手工调整后想保留的树移入这里，重新生成会避让它们。
- `Generated Trees - editable`：初始新增 280 棵，普通树和 02 树共用全图最小间距，02 的生成概率为 12%。Generated Tree Count 指**额外生成数量**，不包含 Fixed Trees；点击 Keep generated trees as fixed 后，如果不想继续新增，可将此数设为 0。
- `Border Forest - decorative tree03`：三排共 664 棵普通装饰树，引用 `Assets/Prefab/Tree_03_Border.prefab`。它只有 SpriteRenderer，不挂 Tree、掉落、升级、粒子或逐帧逻辑，不能砍掉边界。
- `Invisible Walls`：四面实体 BoxCollider2D 空气墙，内边界对应 WorldBoundary 的 Walkable。
- `Boundary Darken - below gameplay HUD`：可编辑的 UI Canvas/Image，靠近边界时渐黑，返回内部时恢复。排序低于 HUD/平板，且不拦截鼠标输入。

根节点 `LargeMapAuthoring` 的生成按钮分别只替换可砍树或外围树，不会清空固定树、地形或其他组；支持 Undo。Seed 控制可复现布局。Minimum Spacing 控制全图树距；Harvest Tree Edge Inset 只在世界外围为树冠和掉落保留空间。地图内部接缝不再留空，也不再套用每块相同的十字通道。全图分布带有疏密变化，仍保留每块图案的禁生成区域和出生点空地。

根节点 `WorldBoundary` 可调整 Walkable、Player Padding、Darken Distance、Maximum Darkness 和 Fade Speed。默认可行走区域 224×222，南侧额外内收以避让向上伸展的树冠；角色在墙内额外留 0.8 单位，距离边界 14 单位内渐暗，最大黑色遮罩透明度 0.42。更改 Walkable 后点击 `Rebuild walls from boundary settings` 同步物理墙。若更改地形尺寸，需要同时调整 Terrain、实际地面布置和外围树林；它们不是运行时自动扩展系统。世界根节点保持零旋转、单位缩放。

PlayerController 的移动接口执行边界限制，因此直接坐标移动也不会穿墙；PlayerCamera 在 LateUpdate 跟随并限制视野在地面内。两者在场景中通过 World Boundary 字段引用同一个组件。其他场景未指定边界时继续使用原来的自由移动行为。

`Cheese Town > Maps > Create Large Wilderness (once)` 只用于首次安装，有大地图 prefab 时不会覆盖现有布局。后续在 Prefab Mode 里调整并保存即可。

## 原有地图块素材库与小地图示例

- `Assets/Prefab/Maps/MapBlock_Base.prefab`：公共模板。
- `Assets/Prefab/Maps/MapBlock_01.prefab` 至 `MapBlock_07.prefab`：对应七张草地的 prefab variants，保存各自的树木布局和区域。
- `Assets/Settings/MapBlockDatabase.asset`：地图配置库。每项引用一个完整地图块 prefab，Weight 决定抽取权重；0 表示不参与抽取。
- `Assets/Prefab/Maps/MapWorld_3x3.prefab`：已拼接好的 3×3 示例，能够整体拖入场景。选块时尽量避免上下左右相邻的地图素材相同。

每块地图为 51.2×51.2 世界单位，保持现有背景的 100 PPU、4 倍显示比例。块原点在左下角，地面图由 `Fit ground to block size` 按完整 sprite bounds 对齐。地图块、树木容器保持单位缩放；不要通过缩放地图块根节点调整树林密度。

## 编辑一张地图

1. 双击对应的 `MapBlock_0X.prefab` 进入 Prefab Mode。
2. 在 `Tree Regions` 中选择区域，Scene 窗口开启 Gizmos。绿色 Allow 区域允许种树，红色 Exclude 区域禁止种树。使用移动工具调整区域位置，拖动区域边框调整大小。可以复制区域，或给新的空对象添加 `MapTreeArea`。
3. 在根节点 `MapBlock` 调整 Seed、Target Tree Count、Minimum Spacing、Edge Margin 和 Tree Palette。初始目标为每块 10 棵树，普通树与 02 树权重为 9:1；这是概率权重，不保证每块刚好一棵 02。相同设置和种子得到相同布局。
4. 点击 `Regenerate Generated Trees`。它仅替换 `Generated Trees` 的内容；`Fixed Trees` 中的树会保留，并计入目标总数、参与间距检测。可用空间不足时生成较少的树并输出警告，不会强行重叠或无限尝试。
5. 所有生成树都是正常的 Tree / Tree_02 prefab 实例，可直接移动和调整。满意后点击 `Keep all generated trees as Fixed Trees`，或只把重要树移入 `Fixed Trees`。之后重新生成不会覆盖这些固定位置。
6. 保存 prefab。选择 `ManualOnly` 后，生成按钮禁用，可完全手工布置。直接替换 Ground 的 Sprite 并点击 Fit 只改变地面，不会更改树木。

初始红色区域为通道、水塘、主要岩石和大空地留出位置，后续可以自由调整。Exclusion Padding 使树根与禁生成区额外保持距离；它不是树冠轮廓检测。树冠可以有自然遮挡。固定树的位置由你决定，移动它们时也请注意跨块边缘的间距。

## 组合地图

把完成的地图块 prefab 加入 MapBlockDatabase。也可以复制某个地图块，制作相同地面、不同树木布置的版本，并为其设置合适的 Block Id。

打开 `MapWorld_3x3.prefab`，根节点的 `MapAssembly` 可设置数据库、行列数和 Seed。点击 `Rebuild Generated Blocks` 后，完整地图块会从数据库选出并按网格拼接，树木不会在这个步骤重新随机。所有选中的块必须尺寸一致。重建会替换 `Generated Blocks` 下的地图实例，因此重要布局调整应先保存到对应地图块 prefab；操作支持 Undo。

`Cheese Town > Maps > Create Missing Map Prefabs` 只补建缺失的资源，不会覆盖已经保存的地图块、地图组装结果或重新生成手工布局。

## 当前范围与检查

当前游戏使用 MapWorld_Large；原有七张 MapBlock variant 和 MapWorld_3x3 示例继续保留，便于制作独立小地图。两套生成按钮的作用不同：3×3 示例抽取整块已摆好的树；大地图跨块统一生成可砍树。

这是有限地图的编辑器烘焙方案，尚不包含远处区块卸载、存档恢复或树木再生。已砍树桩在同一次运行中隐藏/显示后会保留状态；销毁并重新实例化地图块会从 prefab 初始状态开始。

`Cheese Town > Maps > Build and Check Map Prefabs` 验证固定种子、区域排除、固定树保留、跨块间距、无缝几何对齐、配置库抽取及实际 02 树掉落/树桩逻辑。结果和 Unity 渲染图保存到忽略版本控制的 `Logs` 目录。

`Cheese Town > Maps > Check Large Wilderness` 检查实际场景连接、25 块地面接缝、七种素材、全图树距、教程起始树、实体墙、四边和角落的实际移动限制、摄像机边界、渐暗及恢复、无运行时补树。结果为 `Logs/large-world-checks.txt`，实际 Unity 预览为 `Logs/LargeWorld-overview.png`、`Logs/LargeWorld-east-edge.png`。`Cheese Town > Run Tablet World Checks` 继续验证实际砍树、掉落、拾取与信息页面教程。

大地图初始共 296 棵可砍树，砍完后在原对象上替换为树桩，不额外叠加树对象。外围装饰树数量固定。这控制了对象数量的增长，但不代表已在目标设备完成性能压测。编辑器生成工具不参与发布后的逐帧生成。
