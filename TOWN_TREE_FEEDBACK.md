# Town tree harvest feedback

The town tree now drops small cheese icons when a production batch matures. Each starts at a scattered canopy position and falls straight down under constant gravity, keeping its horizontal position through a small, energy-losing bounce. Analytical motion prevents floor penetration during long frames. Resting icons remain near the roots as a visual indication of stored cheese. Collecting sends a staggered stream of icons, pixel trails and sparks to the tablet header's cheese counter. The destination and counter pulse as icons arrive, accompanied by the existing wild pickup sound.

The original production curve, storage capacity, purchase rules and collection amount are unchanged. `TownProgress.BatchProduced` reports only cheese actually added to storage, after the capacity clamp, once per production update. A large elapsed-time jump produces one bounded visual burst, not one callback or object per cheese. Full storage and the dormant tree emit nothing.

`TryHarvest` still transfers inventory immediately. The feedback holds back only the displayed header count until the flight credits arrive; animation callbacks never award currency. Repeated collections merge into the fixed flight pool when necessary. Empty collection, closing, changing pages, disabling the effect, scene reload and the ending cannot duplicate or discard income. The UI settles to the real wallet whenever presentation is interrupted.

Open `Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/TownTreeHarvestFeedback.prefab` in Prefab Mode to adjust:

- Drop artwork and size, visual pile limit (default 32) and burst limit (default 12).
- Gravity (default 700 canvas units/s squared), restitution (default 0.24), stagger, canopy region and resting height range. Landing X always follows the spawn X; it is never assigned to a separate slot.
- Harvest flight pool (default 18), launch spacing and counter pulse.
- The shared `CheesePickupFlight.prefab` reference, which supplies the existing wild-pickup trail and sparkle design. Assign a separate variant if town-only flight tuning is desired.

The effect is a nested prefab inside `TabletView.prefab`. Its controller receives the existing session, tree and header target through `Bind`; layout remains editable. All graphics ignore raycasts. There are no physical pickups, colliders or currency objects. Manual harvest arrivals reuse `TownAudio.PlayGroundPickup`, including its shared 0.06-second minimum sound interval. Large harvests play a rhythmic sequence instead of thousands of overlapping clips. Production itself, restored stock and cancelled flights do not play pickup sounds. Returning to the page restores a representative pile from current stock without replaying missed bursts.

The pool is created once per UI instance, bounded to at most 48 drop images and 24 flights by runtime clamps. A visible icon may stand for many actual cheeses; the stock label always shows the real quantity. The effect uses unscaled time, so UI motion stays responsive while gameplay is paused.

Validation: run `TownTreeHarvestChecks.RunBatch` in a test copy. It checks vertical-only trajectories, gravitational acceleration, rebound and settling; verifies multiple real-time AudioSource pickup triggers with silent production and cancellation; runs the existing economy data checks; merges an 8,180-cheese payout; checks interruption and reload behavior; and writes native Unity renders to `Logs/TownTreeHarvest`.

Rollback: stop Play and use the project-adjacent `town-tree-motion-audio-backup-20261006/Restore.ps1 -CheckOnly`, then the script without `-CheckOnly`, to undo the vertical motion and sound fixes. To remove the original harvest feature afterward, use `town-tree-feedback-backup-20261006/Restore.ps1` in the same way. Each script validates every changed file before restoring and refuses to overwrite later edits. Restore these layers before earlier UI backups. No commit or push is performed automatically.
