# 升级连接与配置

所有树木相关升级仍在原来的 **TREE** 标签内。`Town Main Tree` 和 `Wild Trees` 是两个独立的升级条目，沿用同一个 UpgradeRowView prefab，不新增分类页面。

| 升级 | 实际连接 |
| --- | --- |
| Move Speed | 角色移动速度 4 → 6 → 8，同时更新快速移动动画 |
| Collect Range | 保留基础贴身拾取；购买后地面拾取半径为 2 / 4 世界单位，手动收集也使用相同范围 |
| Town Main Tree | 城镇共享库存生产效率 1 → 2 → 4 → 8 芝士/秒；不改变野外树大小、掉落范围或主树插画大小 |
| Wild Trees | 野外可砍伐树大小 1 → 1.2 → 1.45 → 1.7 倍，掉落数量范围 1 → 1.25 → 1.5 → 2 倍；不改变城镇生产效率 |
| Auto Collect | 每秒按城镇主树产量收取共享库存；不会吸取野外掉落物，也不会按野外树数量重复计算 |
| Double Cheese Value | 手动收集、自动收集和野外拾取的单块价值均翻倍，不增加实体数量 |

## 野外掉落范围

树 prefab 的 `minCheeseCount` / `maxCheeseCount` 保留基础范围，砍倒时分别乘野外升级倍率并向上取整，再在最终闭区间内随机。已掉落芝士不事后增殖，树桩不能重复发放。外围装饰树没有可砍伐组件，不受影响。

| 显示等级 | 掉落倍率 | 普通树 | 高价值 02 树 |
| --- | --- | --- | --- |
| Lv 1（初始） | 1 | 2–4 | 8–10 |
| Lv 2 | 1.25 | 3–5 | 10–13 |
| Lv 3 | 1.5 | 3–6 | 12–15 |
| Lv 4 | 2 | 4–8 | 16–20 |

两条升级目前都使用 60 / 120 / 240 的购买价格，可在 TabletSettings Inspector 中独立调整。TownTreeProduction 的 Value 是每秒产量；WildTreeGrowth 的 Value 是掉落范围倍率，Tree Scale 是体型倍率。主树旧 ID `tree-growth` 和序列化 effect 数值 2 保留；野外升级新增 ID `wild-tree-growth` 和 effect 数值 5，避免原有资源编号错位。

自动和手动收取消费同一个城镇库存。现有野外手动 E 操作也保留为共享库存的收取入口；砍树产生的实体芝士是独立的野外产出。拾取特效只延迟 HUD 显示，不延迟真实入账。

## 验证

`Cheese Town/Run Upgrade Wiring Checks` 在 Wilderness 中通过真实商店按钮逐级购买，验证两项都显示在 TREE、从 PLAYER 排除、主树升级不影响野外、野外升级不影响主树、普通树与 02 树实际掉落、树桩防重复发放、自动收益、双倍价值、实际 2D 拾取范围、场景重载继承和禁用后的恢复。

入口 `UpgradeWiringChecks.RunBatch` 同时运行数据检查；结果输出到 `Logs/upgrade-wiring-checks.txt`。为避免干扰正在编辑的场景，本次运行使用独立验证副本。
