# 简短功能导览

沿用 WASD / 砍树引导的表现：目标区域保持明亮，四周压暗，金色边框指出当前功能。只显示一句介绍与 CLICK TO CONTINUE；点击屏幕进入下一项，右上角 SKIP 或 TAB / ESC 可以结束导览。

首次拾取后依次介绍：信封入口、信息正文、邮件翻页及已读按钮、升级入口、升级分类和列表、购买按钮、主树收集按钮、返回按钮。所需页面会自动展开。导览点击只推进介绍，不会触发购买、收集或已读操作。页面动画期间不接受连续推进，结束后关闭面板并恢复探索。

提示保留 Press Start 2P 像素字体。布局和短句在 `TutorialCoach.prefab` 中编辑；`TutorialCoachView` 的 Shades 为四周遮罩，Focus Edges 为高亮边框，Shade Opacity 为压暗程度。进度保存于本次 TownProgress，关闭面板或场景重载不重置。

邮件仍按最新未读优先打开，全部已读时打开最新邮件；收到新邮件不会打断正在阅读的正文。

`Cheese Town/Run Tutorial Asset Checks` 检查字体排版、真实遮罩面积和透明高亮区域、全屏点击与跳过绑定。Play Mode 的 Tutorial Checks 覆盖逐次点击、页面预览以及不误触发实际功能。
