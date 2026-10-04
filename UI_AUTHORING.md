# Editable tablet UI

The existing town, upgrades and mail interfaces use `TabletView.prefab`, assigned in the `TabletSettings` resource. Opening menus and the prologue are outside this change. The shared `TownProgress` still owns money, upgrade IDs, prices, effects and letters.

## Editing in Unity

1. Choose **Cheese Town > Edit Tablet UI Prefab**, or open `Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/TabletView.prefab`.
2. Select the prefab root. Its inspector has **Town / Upgrades / Mail** preview buttons. These reveal the actual child UI objects in Prefab Mode.
3. Edit child RectTransforms, Images and Text components in the Scene view/Inspector. Keep the root at **640 x 360**. The supplied 2x PNGs display at half their exported dimensions.
4. Edit `Header controls`, `Town page`, `Mayor mailbox`, `Footer clip/ Footer motion` children and `List placement` to change their layouts. The notification placement objects control feedback positions. Runtime controls the panel/footer motion offsets and list mask reveal; edit the placement objects and the graphics beneath them instead of animation offsets.
5. Adjust opening duration, page duration, slide distance and footer travel on `TabletView`. Adjust card duration, stagger and slide on the `UpgradeShopView` component of `Upgrade shop panel`. Durations use unscaled time; motion snaps to logical pixels without scaling sprites.

The world launcher has editable screen-edge insets and an unread-message gap on the root view. Disable `Anchor Launcher To Screen` to position it entirely through its RectTransform.

The root must retain its assigned component references. Moving children within the prefab does not break those references. Preview visibility is normalized at runtime. The font and colors are the serialized UI component values; the import menu does not rebuild an existing tablet or upgrade-row prefab.

## Upgrade rows and new upgrades

The nested **UpgradeRow - authoring preview** makes one row visible while editing the shop. It is hidden at runtime. Open the original `UpgradeRow.prefab` to change all live rows; overrides made only to the preview instance do not affect live rows. This change preserves the existing row asset and its adjusted font sizes, colors and spacing.

Add, remove or reorder options and price/value levels in `TabletSettings > Shop list`. Rows instantiate the configured `upgradeRowPrefab`; callbacks retain each option's ID and call the existing purchase logic. Adding another option for an existing effect requires no new view code. A genuinely new gameplay effect still needs its rule/listener implementation.

Filtering rebuilds only the list. Normal purchases refresh values without replaying the entrance or resetting scroll. The full effect description remains a passive overlay triggered only by the description text. Wheel/drag/scrollbar movement dismisses it.

## Motion and native assets

- The tablet slides and fades on open/close. World movement/chopping remains blocked until the closing panel disappears.
- The supplied frame textures are displayed as fixed UV crops. The footer crop and its readouts slide downward behind a mask when entering upgrades, then return when leaving. No PNG or source art was redrawn.
- Visible rows unfold downward from a shared upper edge, with a short stagger. Offscreen rows are already settled. Reversing navigation continues from the current page pose.
- `TabletPrefabBuilder` is a one-time migration/fallback tool. It creates a missing asset; it never replaces an existing saved prefab. The old procedural builders remain as compatibility fallback for configurations without a prefab.
- `TabletSettings.asset` gains only the reference to the new view asset. Unity YAML contains no injected comments; source additions and changed logic have short English reason comments.

### Border and purchase feedback correction

The frame's inner uprights end at exported source columns 59 and begin at 1220. The background and footer now span columns 60 through 1219 (logical x=30 through 610), restoring the missing one-pixel inner strip on each side. The footer UV crop and mask were widened together, preserving its artwork scale and control positions. An opaque interior backing sits under the page transition to prevent the world from showing through while the footer moves. Canvas pixel alignment and integer scale remain enabled.

The Buy button in `UpgradeRow.prefab` now uses the same immediate Color Tint hover and press colors as the header buttons. The existing gray disabled artwork is still selected by `UpgradeRowView.RefreshDisplay`; its disabled tint stays white to avoid dimming that asset twice. Other row layout, typography and colors remain unchanged.

The repository explicitly checks out C# and Markdown files in LF via `.gitattributes`, consistent with its Unity YAML rule. Edited source files use LF; unrelated working files were left in their existing format. No global Git configuration was changed.

## Verification

**Cheese Town > Run Pixel UI Checks** checks real Play-mode navigation, purchases and effects on player/tree components, category filtering, scroll limits, text fit, hover click-through, mail and ending behavior. It also checks intermediate animation poses, rapid reversals, closing input protection and component identity across category changes. Test output and rendered UI captures go to the ignored `Logs` directory.

Border regression checks render against a magenta backdrop at 1280x720, 1920x1080, 2560x1440 and 1601x901, sampling both inner edges and the footer junction. Pointer checks verify Buy hover, press, release, exit, refresh persistence and disabled-click protection.

**Cheese Town > Run Tablet World Checks** opens Wilderness and reuses the existing tutorial integration runner: nearest tree, actual chopping and pickup, launcher unlock, welcome letter and completion. Run from Edit mode; save your current scene first. Both suites passed in an isolated Unity **6000.6.2f1** project, with 1080p UI captures inspected. Existing tutorial rules were retained.

No additional animation artwork is required. Unassigned upgrade icons and the town-background slot continue using the current project content/placeholders and can be replaced through existing asset fields.
