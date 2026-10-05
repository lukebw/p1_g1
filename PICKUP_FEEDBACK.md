# 芝士拾取反馈

落地芝士被拾取时，世界对象立即退出碰撞并入账；UI 从拾取点显示芝士图标，弹起后沿弧线加速飞向左上角。起飞时出现像素粒子，飞行中有残影拖尾，抵达时计数器回弹并出现另一组粒子。

## 编辑 prefab

打开 `Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/CheesePickupFlight.prefab`。

- Duration：飞行总时间，默认 0.62 秒，包含 0.12 秒弹起。
- Pop Height / Arc Height：弹起高度和弧度，单位为逻辑 UI 像素。
- Arrival Duration：抵达后粒子的消散时间，默认 0.18 秒。
- Trail Spacing / Trail Color：残影间隔、颜色与透明度。初始有 6 个残影 Image。
- Particle Spread / Particle Color：粒子散开范围和颜色。初始 12 个粒子，前半用于起飞，后半用于抵达。
- Flying cheese：调整图标尺寸；拾取时使用对应地面芝士的 sprite。

打开 `TabletView.prefab`，选择 `Pickup Feedback - nonblocking UI`。

- Flight Prefab：引用上面的独立特效 prefab。
- Target Icon：拖入 HUD 的芝士图标，可随 UI 排版移动。
- Pool Size：默认最多 12 个同时播放的实例，额外拾取合并到已有飞行中；实际入账不减少。修改后重新进入 Play。
- Pulse Duration / Pulse Scale：计数器回弹时间和幅度。

所有特效 Image 和 CanvasGroup 都不拦截鼠标。管理器与飞行实例使用 UI 坐标及 unscaled time，角色移动不会拖走已飞出的图标；终点每帧跟随 HUD 布局。无需新美术素材或第三方粒子插件。

## 数量与中断

经济数据仍由原来的 `TownProgress.CollectWorld` 立即更新。特效只在 HUD 上暂缓显示尚未抵达的收益，商店余额、教程和任务条件使用真实值。飞到终点后再显示该批收益，双倍价值升级和余额上限均按实际入账量处理。

打开信息/升级界面、进入结局或禁用特效时，会回收飞行图标并同步真实数字。自动收集等没有地面拾取位置的收入继续正常刷新，不凭空生成地面飞行。动画不会再次发放收益。

## 查看与验证

打开 Wilderness 并进入 Play，砍树后走近落地芝士即可体验。重要修改附近保留了 BEGIN ADDED / CHANGED 注释。

`Cheese Town > UI > Check Pickup Feedback and Render Preview` 检查拾取重复回调、真实余额立即到账、HUD 延迟显示、40 次连续拾取合并、页面切换、双倍价值、禁用组件和余额上限，并将 48 帧实际 Unity 渲染保存到 `Logs/PickupFrames`。此检查在临时 Play 会话操作，不保存游戏进度；结果为 `Logs/pickup-feedback-checks.txt`。

`Cheese Town > Run Tablet World Checks` 验证实际物理掉落拾取与原有教程衔接。`Cheese Town > UI > Install Pickup Feedback` 仅安装缺失的 prefab 和引用，不覆盖已经编辑的特效或重建整套 UI。
