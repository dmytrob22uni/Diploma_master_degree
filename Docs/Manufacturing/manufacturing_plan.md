# Manufacturing Plan — Device-Controlled FFF Printing

**Status:** Draft for implementation  
**Last updated:** 2026-11-17  
**Scope:** Laptop / PC / Smartphone / Tablet controlling one or more Prusa MK4S printers  
**Document type:** Living design spec + milestone checklist

---

## 1. Purpose

Add a manufacturing loop to the game:

1. The player uses a computing device (laptop, PC, smartphone, tablet) as a control console.
2. The device is bound to one or more printers by looking at a printer and clicking it.
3. The player selects an object from the `ObjectsToPrint` catalog, chooses a limited infill option, and starts a print.
4. The printer runs its own internal state machine and produces a visible, accelerated print.
5. The player can stop a print at any time. A hard stop leaves a partial print and damages the machine.
6. Consumables and wear are integrated with the existing `ItemWearState` system.

The feature is intentionally a **gameplay abstraction** of 3D printing, not a slicer or a physical simulator.

---

## 2. Scope

### In scope

- One generic device console used by all four computing prefabs.
- Device-to-printer binding through hover targeting.
- Binding highlights (translucent spheres) for bindable and already-bound printers.
- Multiple printers bound to one device, with one active printer selected in the console.
- Printer-owned state machine, independent of global game states.
- Accelerated print time with configurable multiplier.
- Job validation (parts, spool, material, filament, build volume, printer state).
- Three infill choices: `None`, `Grid`, `Gyroid`.
- Preheating, printing, cooling, finished, and hard-stop flows.
- Wear and filament consumption.
- Procedural nozzle/bed motion and shader-based layer reveal.
- Partial print left after a hard stop.
- World-space console that stays visible on the device screen, with a smooth camera focus for interaction.
- Screen-off state for the device display.
- Enclosure hooks, with enclosure gameplay delivered in a later milestone.

### Out of scope for the first implementation

- Real slicer integration or G-code parsing.
- Realistic toolpath simulation.
- Visible infill geometry.
- Print queue and multi-job scheduling.
- Resume-from-partial-print.
- Full device power simulation (battery, wall socket, wattage).
- Multiplayer, networking, or remote printing.
- Save/load persistence.
- Auto-generated print profiles from mesh volume (manual authored profiles first).
- Delta / CoreXY printer prefabs (architecture leaves room, but only MK4S is implemented).

---

## 3. Approved defaults

| Decision | Value |
|---|---|
| Console presentation | Screen-space overlay, one generic UI for all devices |
| Camera focus | Smooth Cinemachine blend to a dedicated screen-framing camera |
| Print time | Simulated time = real time × configurable multiplier; default `120×` |
| Hard stop result | Partial print stays on the bed until removed |
| Infill choices | `None`, `Grid`, `Gyroid`; affects time/filament only |
| Print settings | Layer height (`Draft 0.28` / `Normal 0.20` / `Fine 0.12` mm), speed (`Slow 40` / `Normal 80` / `Fast 140` mm/s), supports (`None` / `Auto`) |
| Nozzle diameter | Not a per-print setting; treat nozzles as hardware |
| Job queue | None; one active job per printer |
| Device-to-printer model | One device → many printers; one printer → one device |
| Printer ownership of job | Printer owns the job and keeps printing if the device is unbound |
| Screen-off behavior | Screen/display only; device and printer continue running |
| Enclosure | Basic thermal/quality effects in M3; advanced effects in M5 |
| New global game states | None; reuse `GameStateDeviceInterface` and `GameStateDeviceBind` with context |

---

## 4. Player flow

### 4.1 Opening the console

1. The player looks at any child mesh of a computing device.
2. `ItemHoverInteract` resolves the `Device` on the prefab root.
3. The hover prompt shows `Use device` with the existing `F` hint.
4. The player presses `F`.
5. The game switches to `GameStateDeviceInterface(device)`.
6. Player movement/look is disabled, the cursor is shown and unlocked.
7. A dedicated Cinemachine camera blends from the player's view to a framed view of the device screen.
8. The console is already on the device screen; it becomes interactive when the blend completes.

### 4.2 Console

The console shows:

- Device name and power state.
- Bound printer list with status indicators.
- Active printer status card.
- Job picker from the `ObjectsToPrint` catalog.
- Layer-height, print-speed, infill, supports, spool and simulation-speed selectors.
- Estimated print time and filament use.
- Validation messages and live printer status.
- `Start print`, `Place on bed`, `Pause`/`Resume`, `Hard stop`/`Confirm stop`, `Calibrate`, `Clear bed`, `Connect printer`, `Disconnect`, `Screen off`, and `Quit`.

M1 ships only the binding and power controls; the job controls arrive in M2. M2.5 adds `Place on bed` between job setup and starting. M3 adds the live run controls and calibration.

### 4.3 Binding a printer

1. Player clicks `Connect printer` in the console.
2. The console stays visible on the device screen but stops accepting pointer input; the camera blends back to the player, and movement/look is enabled.
3. The device console's header and footer are hidden. The authored `device_bind_canvas` appears together with `point_canvas`, like `parts_integrity_canvas`: a top `horizontal_stack` reading `Printer binding`, the hovered printer's name, a reason line for failed attempts, and a bottom `horizontal_stack` with `Q Quit`.
4. Bindable printers show a translucent cyan sphere; printers already bound to this device show green; printers bound to another device show red. The hovered sphere becomes slightly more opaque, never fully opaque.
5. The player looks at a printer and left-clicks.
6. Binding succeeds and the console returns to the device screen with the new printer selected; no message block is shown.
7. Hovering a printer anywhere inside its sphere shows its name in `device_bind_hover_name` and the `bind_root` key hint (constant authored text, `LMB Bind`). A failed click on a printer shows a short reason in `device_bind_reason` for `ReasonDuration` seconds, such as `Controlled by another device.` Clicking empty space is silent.

### 4.4 Disconnecting a printer

1. Player clicks `Disconnect` in the console.
2. If the active printer is printing, a confirmation modal warns that the print will continue.
3. The console stops accepting input and the camera returns to the player.
4. Bound printers show a translucent amber/orange sphere; the hovered sphere becomes slightly more opaque.
5. Player left-clicks a bound printer; that binding is removed and the console becomes interactive again.
6. The printer keeps its current job and state. The device simply stops controlling it.

### 4.5 Starting a print

1. Player selects the active printer.
2. Player selects an object, layer height, speed and infill. Estimates are shown for the default placement (centred, unrotated, scale `1`).
3. Player places the object on the bed (see 4.6). Estimates recalculate from the chosen scale.
4. Console validates the job.
5. `Start print` becomes available only when the job is valid.
5. Printer transitions `Idle → Preheating → Printing → Cooling → Finished`.
6. Console shows live state, temperatures, progress, remaining time, and filament use.
7. When finished, the object is fully revealed on the bed and can be removed.

### 4.6 Placing, rotating and scaling the object

1. Player clicks `Place on bed` in the console.
2. The console stays on the device screen but stops accepting input; the camera focuses the printer's print plate, and the cursor is hidden.
3. A `PrinterBedPlacement` ghost appears at the plate centre, at scale `1`, facing the printer front from `PrintBedAnchor`.
4. Interaction is hold-to-modify. With no modifier the mouse only moves the cursor:
   - move: hold LMB and move the pointer; the object follows the plate raycast,
   - yaw: hold `R` and move the mouse left/right,
   - scale: hold `S` and move the mouse right/up to grow and left/down to shrink; exponential. Overscale/underscale is allowed, turns the ghost to the wrong state, and blocks confirmation.
   - camera orbit: hold `RMB`; pitch is clamped to `15°–75°` above the plate,
   - zoom: wheel, with clamped distance,
   - confirm Enter, cancel `Q`. Scale/rotate happen around the object's bottom centre.
   Releasing a modifier returns to the idle state. No snap is used.
5. The ghost uses `shader_object_to_print_printer_bed_placement_mode`; valid placements use the `right` state, invalid placements switch the shader to the `wrong` state via its blackboard (`_printer_bed_placement_state` / `_PRINTER_BED_PLACEMENT_STATE_WRONG`).
6. A placement is invalid when the rotated, scaled footprint leaves the usable plate area, the height exceeds `BuildVolume.y`, or the scale leaves its dynamic min/max. Confirm is disabled while invalid.
7. Confirming stores the local position, yaw and uniform scale on the current job draft, returns to the console, and recalculates the estimates. Cancelling discards it and returns without saving; if nothing was placed, the console reports that a placement is still required.
8. Both entering and leaving placement use the same Cinemachine Brain blend as the console focus (`0.55 s`, `EaseInOut`). On confirm/cancel the placement camera stays alive as the blend source until the return blend finishes, then it is destroyed.
9. The placement UI (hints, valid/invalid message, confirm/cancel) is authored separately — the roadmap asks the project owner to create it before implementation.

### 4.7 Hard stop

1. Player clicks `Hard stop`; the button becomes `Confirm stop` for three seconds, and the second click confirms.
2. The printer immediately stops motion and turns heaters off.
3. Printing leaves a partial print on the bed; stopping during preheat leaves no partial print.
4. A one-off condition penalty is applied to the nozzle/heater/bed/plate.
5. The printer cools down and returns to `Idle` (`HasPartialPrint = true` when printing had started).
6. No new job can start until the partial print is removed with `Clear bed`.

### 4.8 Leaving the console

- `Q` quits the console (`Quit`).
- The console stops accepting pointer input and stays visible on the device screen; the camera blends back to the player, movement/look is restored, and the cursor is hidden/locked.
- Printers continue running while the player walks away.
- The screen-off state is preserved in the world.

### 4.9 Calibration

1. With an idle printer and a clear bed, click `Calibrate` in the console.
2. The nozzle visits every point of an `N × N` grid on the plate (`CalibrationGridSize`, default `5`), touching height `0` at each point. Calibration uses real time so the visual check stays readable regardless of `SIM SPEED`.
3. The grid is inset by `CalibrationPaddingMeters`.
4. Calibration completes by homing the motion. `Hard stop` cancels it.
5. Calibration does not consume a job and is blocked while a print is queued, partial or finished.

---

## 5. Answers to the open design questions

### 5.1 Should one device connect to multiple printers?

**Decision: yes, model the device as a console that can control many printers, but keep job ownership in the printer.**

Recommended binding model:

- `Device.BoundPrinters : List<Printer>`
- `Printer.BoundDevice : Device` (single owner)
- One printer can be bound to at most one device.
- One device can bind any number of printers.
- The console has an active-printer selection, but other bound printers keep running.
- Binding is a **control relationship**, not a power or job-ownership relationship.
- Unbinding does not stop a print. Rebinding restores console access and live status.

Why this is wise:

- It matches the real-world mental model of a PC or laptop controlling several printers.
- It avoids a painful data migration later from `Printer BoundPrinter` to `List<Printer>`.
- It lets the player start a long print, switch to another printer, and keep both running.
- It keeps the printer autonomous, so the simulation does not depend on the device being open.

What to avoid:

- Do not let the device own the job. If the device is unbound or destroyed, the printer should not lose its job.
- Do not implement a job queue in the first pass. One active job per printer is enough.
- Do not allow two devices to control the same printer.

UI consequence:

- The console needs a printer list and an active selection.
- M1 can ship with a simple list; a richer dashboard is M5 polish.
- If the player only ever binds one printer, the list still works as a single row.

### 5.2 Does the plan include a printer enclosure?

**Decision: basic enclosure effects arrive in M3; advanced effects remain M5.**

M3 enclosure behavior:

- `PrinterEnvironment` on the printer:
  - `bool HasEnclosure`
  - `bool DetectEnclosure`
  - `float AmbientTemperature`
  - `float ChamberTemperature`
  - `float DraftFactor`
  - `float ParticulateFactor`
- `PrinterEnclosure` marks the physical enclosure and binds the printer inside its bounds.
- `PrinterTemperatureModel` reads chamber temperature and slows cooling when enclosed.
- `PrintQualityEstimator` applies an enclosure bonus / no-enclosure penalty; raw quality caps at `90`.
- Enclosure parts join the printer wear scan, so fans, filters, thermometers and fire-suppression parts wear during a job.
- The console shows live nozzle/bed temperatures and enclosure-derived quality.

M5 enclosure effects still to add:

- Draft and particulate effects.
- Warping and long-pause adhesion risk.
- Advanced material-specific chamber targets.
- Post-process quality beyond the raw `90` ceiling.
- Optional filtration/particulate effect.
- Quality score affects `ObjectToPrintDataHolder.IsHaveQuality` and sale value.

### 5.3 How should printer motion be abstracted for future printers?

**Decision: separate “where the nozzle should be in print space” from “which transforms move.”**

Do not let job logic move `extruder_base`, `x_axis_base`, or `print_bed_holder_base` directly. Use a motion abstraction:

```
PrinterMotionBase (abstract MonoBehaviour)
  - BuildVolume (Vector3, meters)
  - Home()
  - ApplyNozzlePosition(Vector3 printSpacePosition)
  - SetBedHeight(float height)
  - GetNozzleWorldPosition()
  - GetBedAnchor()
```

MK4S implementation:

```
CartesianPrinterMotion : PrinterMotionBase
  - AxisBinding HorizontalX
  - AxisBinding HorizontalZ
  - AxisBinding VerticalY
```

`AxisBinding` contains:

- `Transform Node`
- `Vector3 LocalAxis`
- `float MinLocal`
- `float MaxLocal`
- `bool Invert`
- `float Speed`

This keeps the door open for:

- bedslinger printers
- CoreXY printers
- delta printers
- future printers with different axis construction

The job/visual layer only asks for a print-space position. A `CartesianPrinterMotion` maps it to independent axes. A future `DeltaPrinterMotion` would map the same position to three tower axes.

#### MK4S hierarchy and mapping

Confirmed hierarchy from the prefab:

- `Prusa_MK4S_printer_root`
  - `base`
    - `x_axis_base`
      - `extruder_base`
    - `print_bed_holder_base`
      - `placement_points`
        - `print_bed_location`
  - `spool_holder`
    - `placement_points`
      - `spool_location_1`
      - `spool_location_2`

Recommended mapping:

| Print-space role | Transform | Local axis | Meaning |
|---|---|---|---|
| Horizontal X | `base/x_axis_base/extruder_base` | local X | nozzle left/right (local X maps world -X) |
| Horizontal Z | `base/print_bed_holder_base` | local Y | bed front/back (local Y maps world +Z) |
| Vertical Y | `base/x_axis_base` | local Z | gantry up/down (local Z maps world +Y) |

Important calibration notes:

- The `base` node has a 180° rotation about `(0, 1, 1) / √2`, so its children's local Y and Z are swapped relative to world. Do not infer axes from node names.
- `print_bed_location` has a non-identity local rotation, but its composed world rotation is identity. A child anchor with identity local rotation is therefore Y-up. Verify it anyway.
- MK4S is a bedslinger: `Horizontal Z` moves the bed and `Vertical Y` moves the gantry. The abstraction must not assume "the bed moves down"; a dropping-bed printer maps the same role to a different node and sign.
- Moving the bed for a horizontal role inverts the print-space-to-world relationship. Calibrate `MinLocal` / `MaxLocal` (and `Invert` if convenient) so that 0.5 puts the nozzle over the bed centre.
- For the vertical role, `MinLocal` is the offset at the first layer and `MaxLocal` the offset at full build height, using signed offsets: a gantry that rises uses a positive `MaxLocal`, a bed that drops uses a negative one.
- Verify the sign of each axis in M0. Store signs in `AxisBinding.Invert` (or in the Min/Max order), not in code.
- Verify the safe min/max local offsets with a slow debug move before enabling print motion.
- The printed object is parented to the bed, so the reveal cutoff must follow the object's current base, not a fixed world height. On MK4S the base stays fixed while the gantry rises; on a dropping-bed printer the base moves down.

#### Print-bed anchor

Add a `PrintBedAnchor` transform (or runtime-created anchor) under/at `print_bed_location`:

- Position: bed center, just above the plate surface.
- Rotation: calibrated so the object's local Y is world up.
- The printed object is parented to this anchor, so it moves with the bed.
- At print start, compute the object's local bottom offset and center it on the anchor.

This avoids fighting the prefab's diagonal rotations and makes future printer prefabs easier to support.

### 5.4 Camera focus and screen-off behavior

**Camera decision: use a dedicated Cinemachine camera, not a camera detached from the player's head.**

Why:

- The player's head may be far away, at a bad angle, or behind the device.
- A dedicated camera can frame the screen consistently for laptop, PC, phone, and tablet.
- Cinemachine handles the blend in both directions.
- It avoids fighting the player look controller.

Implementation:

- Add `device_console_camera` as a `CinemachineCamera` with no procedural components; `DeviceFocusController` writes its transform directly.
- On focus:
  - Read the device's screen anchor.
  - Place the camera in front of the screen at a distance derived from the screen bounds and FOV.
  - Orient it toward the screen center using the anchor's up axis.
  - Raise the console camera priority to `100`.
- On release:
  - Lower the console camera priority to `-1` (below the player camera's implicit `0`), then let the Cinemachine Brain blend back.
- Default blend: `0.55 s` each way, `EaseInOut`.
- The console is always visible on the screen. Only the focused device's `GraphicRaycaster` is enabled, after the blend completes, with the player camera as its event camera.
- While the console is focused, player movement/look is disabled and the cursor is visible.
- While bind targeting is active, the cursor is hidden/locked and player movement/look is enabled.
- Console camera near clip is `0.01 m` so close-up laptop framing does not clip.

Edge cases:

- Laptop lid closed: do not open the console; in M5, interact opens the lid first.
- Device moved/destroyed while focused: close the console and return to free walk.
- Screen anchor missing: fall back to the `ScreenGameObject` transform and log a warning.
- Camera occluded by a wall: M1 can accept it; M5 can add a simple occlusion pull-in.

**Screen-off decision: turn off the display only, not the whole device.**

Why:

- A printer should keep printing when the controlling device screen is off.
- There is no full device power simulation yet.
- The device remains available for interaction; the player is not stranded.
- Full power-off belongs with the existing power/plug/battery systems and can be added later.

Behavior:

- `ItemComputingDataHolder.IsTurnedOn` remains the source of truth.
- `Device.SetScreenOn(bool)` updates both the logical state and the physical screen material.
- When off:
  - The physical screen switches to a black/unlit material.
  - The console shows a black screen with a `Power on` button and a hint on top of it.
  - `Q` still leaves the console; the screen remains off in the world.
- When the player next interacts, the console is focused in the off state and can be powered on.
- Printers are unaffected.

### 5.5 InputActions changes

**Decision: no new InputActions maps or actions are required for M0–M4.**

Existing inputs cover the flow:

| Input | Existing action | Use |
|---|---|---|
| `F` | `FreeWalk/InteractWithItem` | Open device console |
| `Q` | `QuitCancel/Quit` | Close console / cancel bind mode |
| Left mouse | `DeviceBind/BindDevice`, `DeviceBind/UnbindDevice` | Bind/unbind printer |
| Mouse | EventSystem UI module | Console buttons, lists, scroll |
| WASD / mouse look | `Movement` | Walk and aim during bind targeting |

Important details:

- `EnableDeviceInterfaceStateInput()` already enables `QuitCancel`; mouse UI works through the EventSystem's own actions.
- `EnableDeviceBindStateInput()` already enables `Movement`, `QuitCancel`, and `DeviceBind`.
- `BindDevice` and `UnbindDevice` are both bound to left mouse intentionally. The active bind mode decides which event is subscribed; never subscribe to both at once.
- The console uses the existing cursor helpers (`GameState.ShowCursor` / `HideCursor`).

Optional later additions (only if a concrete need appears):

- A `DeviceConsole` map for keyboard navigation (`Navigate`, `Submit`, `Cancel`).
- A `ToggleDeviceScreen` key.
- A `PausePrint` key.

Do not add these speculatively; mouse UI and `Q` are sufficient for the first implementation.

---

## 6. Architecture

### 6.1 Domain model

```
Device
  - ItemComputingDataHolder Computing
  - DeviceScreenDisplay Screen
  - Transform ScreenAnchor
  - List<Printer> BoundPrinters
  - Printer ActivePrinter
  - bool IsDisplayOn
  - TryBindPrinter(Printer)
  - UnbindPrinter(Printer)
  - SetActivePrinter(Printer)
  - SetScreenOn(bool)

Printer
  - PartsAssembly Assembly
  - PrinterMotionBase Motion
  - PrintBedAnchor BedAnchor
  - PrinterEnvironment Environment
  - PrinterEnclosure Enclosure (detected through Environment)
  - PrinterStateMachine StateMachine
  - PrinterJob ActiveJob / CompletedJob / PartialJob
  - PrinterTemperatureModel Temperature
  - PrinterWearIntegrator Wear
  - SpoolFilamentState ActiveSpool
  - float PrintSpeedMultiplier
  - float SafeServiceTemperatureC
  - PartLocation[] ServiceBlockedDuringPrint
  - int CalibrationGridSize
  - Device BoundDevice
  - string StatusSummary

PrinterJob
  - PrintJobRequest Request (object, settings, spool, placement)
  - float ElapsedSeconds
  - float ConsumedFilamentMeters
  - int PauseCount / float PauseQualityPenalty
  - float QualityMin / QualityMax (after pause penalty)
  - bool IsPartial / bool WasHardStopped / bool UsesApplied
  - float Progress01 / RemainingSeconds / RemainingFilamentMeters

PrinterStateMachine
  - Idle
  - Preheating
  - Printing
  - Paused        (pause/resume and safe service)
  - Cooling
  - Finished
  - Calibrating   (N × N plate touch grid)
  - Maintenance   (validation/data driven in M3)
  - Fault         (preheat timeout in M3; full failures in M5)
```

The printer is a self-contained `MonoBehaviour` that ticks every frame, regardless of whether a device is open or bound.

### 6.2 Existing code integration

#### `Items/Computing/Device.cs`

Implement `IInteractable`:

- Return exactly one `InteractionOption`.
- `Info`: `"Use device"`.
- `OnExecute`: change to `new GameStateDeviceInterface(this)`.
- Cache the option list; do not allocate a new list every hover frame.

#### `Game/Logic/Hover/ItemHoverInteract.cs`

The current simple-interactable path only checks the hit collider. Computing prefabs keep colliders/Rigidbodies on child meshes, while `Device` lives on the root.

Fix:

- Before the existing `PartsAssembly` complex-interactable path, try `hit.collider.GetComponentInParent<Device>()`.
- If found, treat it as a simple interactable and use its `GetInteractions()`.
- Do not use a generic `GetComponentInParent<IInteractable>()` here, because it could accidentally resolve the printer's `FFFInteractions` and bypass the multi-option printer interaction path.
- Cache both the hit collider and the resolved `Device` so the prompt does not flicker between child meshes.

#### `Game/StateMachine/State/GameStateDeviceInterface.cs`

Turn it into a contextual state:

```
GameStateDeviceInterface(Device device)
```

On enter:

- `GameStateManager.Instance.DeviceController.OpenConsole(device)`.
- Enable device interface input.
- Show cursor.

On exit:

- `DeviceController.CloseConsole()`.
- Disable all input.
- Hide cursor.

#### `Game/StateMachine/State/GameStateDeviceBind.cs`

Turn it into a contextual state:

```
GameStateDeviceBind(Device device, DeviceBindMode mode, GameState returnState)
```

`DeviceBindMode`:

- `Bind`
- `Unbind`

On enter:

- Hide the console.
- `DeviceController.EnterBindMode(device, mode)`.
- Enable bind input.
- Hide/lock cursor.

On exit:

- `DeviceController.ExitBindMode()`.
- Disable all input.

When a printer is selected or the player cancels, the state returns to its `returnState` (the previous console state). Do not rely on `GameStateManager.PreviousGameState` for this; store the return state explicitly.

Guard against re-entrant events: if `ExitBindMode()` raises a cancel event while the state is already transitioning after a successful selection, ignore it. A simple `isExiting` flag in the state or controller is sufficient.

#### `Game/Controller/PlayerController.cs`

Add:

```
SetDeviceInterfaceState(bool enable)
SetDeviceBindState(bool enable)
```

- Interface state: disable movement, jump, look, and free-walk hover.
- Bind state: enable movement, look, jump/crouch/walk/run as appropriate, but not free-walk item interactions.

The existing `SetFreeWalkState` is not suitable for bind targeting because it also enables item hover interactions.

#### `Game/Controller/InterfaceController.cs`

Keep this class focused on state UI visibility. If the new console canvas is parented under the existing `interface` root, add only a canvas reference and a setter. Device-specific orchestration belongs in `DeviceController`, not in `InterfaceController`.

#### `Game/StateMachine/GameStateManager.cs`

Add one field:

```
public DeviceController DeviceController;
```

This is a controller, not a game state. The global state machine remains unchanged.

#### `Input/InputReader.cs`

No new events are required. Reuse:

- `OnQuitEvent`
- `OnBindDeviceEvent`
- `OnUnbindDeviceEvent`

#### `Game/Logic/Hover/ItemHoverSell.cs`

Computing devices cannot be sold while connected to any printer. The sell hover resolves the `Device` on the item root and checks `Device.BoundPrinters`:

- bound to at least one printer → `SellingState.Wrong`, reason `Connected to a printer`;
- no bound printers → normal sell rules.

The wrong state is applied through the existing selling ghost `_selling_state` blackboard, and the reason appears in the sell hint like `Missing parts` / `Worn parts`.

#### Combined free-part / wearable items

Some parts are both a free part and a regular item (e.g. `Prusa_MK4S_printer_battery_closet_adapter_root` has `ItemFreePartDataHolder`, `ItemDataHolder`, and `ItemWearState`). These rules keep the flows consistent:

- Regular free parts live directly under their `free_parts_reset_points` location and are moved as their own item root.
- A wearable free part is installed in a `PartLocation` carrier. `ItemPlacementUtility.GetFreePartMovableTransform` resolves the carrier as the movable transform, so the location collider and the installed part travel together.
- A free part nested under a core item stays restricted to the core item's parent surface.
- A standalone item which also exposes `ItemFreePartDataHolder` has no core item above it and can be placed freely.
- Placement updates every `ISurfacePlaceable` on the item root, so both free-part and item surface states agree.
- Taking a wearable free part moves its `PartLocation` carrier; it stays installed in that carrier. Install mode is not offered while it is already installed. The carrier is deactivated while carried so its location collider cannot block placement rays.
- Taking a core item resets every mapped free part: regular parts return to their `ItemPartsToLocationsList` location, and wearable parts move their `PartLocation` carrier to it. Old shelf registrations and placement ghosts are cleared.
- `PartLocation.Remove()` and `PartLocation.Install()` reset a wearable part's carrier to its authored `ItemPartsToLocationsList` location, so the assembly socket is never left displaced.
- Entering installation hides both the regular placement ghost and the free-part placement ghost.
- Hovering a detached combined part still offers wear inspection through its `ItemDataHolder`.

### 6.3 Printer state machine

States and transitions:

| From | Event | To |
|---|---|---|
| `Idle` | valid job + start | `Preheating` |
| `Preheating` | nozzle and bed at target | `Printing` |
| `Preheating` | hard stop | `Cooling` (partial = false) |
| `Printing` | progress reaches 100% | `Cooling` |
| `Printing` | pause | `Paused` |
| `Paused` | resume, target reached | `Printing` |
| `Paused` | resume, below target | `Preheating` |
| `Printing` / `Paused` | hard stop | `Cooling` (partial = true) |
| `Preheating` | hard stop | `Cooling` (no partial) |
| `Printing` | fatal fault (M5) | `Cooling` / `Fault` |
| `Cooling` | temperatures near ambient, complete | `Finished` |
| `Cooling` | temperatures near ambient, partial | `Idle` (partial print remains) |
| `Cooling` | temperatures near ambient, preheat stop | `Idle` (clear bed) |
| `Finished` | print removed / clear bed | `Idle` |
| `Idle` | calibration requested | `Calibrating` |
| `Calibrating` | grid complete | `Idle` |
| `Calibrating` | hard stop | `Idle` |
| `Idle` / `Finished` | any part at 0% / missing critical part | `Maintenance` |
| `Maintenance` | parts repaired | `Idle` |

Rules:

- `Finished` means a completed print is still on the bed.
- `Idle` means the bed is clear.
- A hard-stopped partial print sets `HasPartialPrint = true`; the printer returns to `Idle` after cooling but rejects new jobs until the partial print is removed.
- `Paused` is used for player pause and safe part service; it does not consume print progress.
- `Calibrating` moves the nozzle to every point of an `N × N` plate grid and does not consume a job.
- `Maintenance` is data/validation-driven in M3; live automatic transitions and repair scheduling remain M5.
- `Fault` is reserved for M5 failure modes; M3 uses it only for a preheat timeout.

The printer ticks with simulated time:

```
simDelta = Mathf.Min(Time.deltaTime, 0.1f) * PrintSpeedMultiplier;
```

Clamp the per-frame delta to avoid huge jumps after a tab-out or a long frame.

### 6.4 Jobs and validation

`PrintJobValidator` returns:

```
PrintJobValidationResult
  - bool IsValid
  - List<string> Errors
  - List<string> Warnings
  - float EstimatedSeconds
  - float EstimatedFilamentMeters
```

Validation rules:

- A printer is selected and bound.
- Printer is `Idle`; `Finished` means a completed print is still on the bed and blocks a new job until the bed is cleared. `HasPartialPrint` blocks separately.
- Printer has all critical parts installed.
- No installed part is completely worn out (`0%`); any installed part at `0%` blocks.
- Every installed part at `20%` or less warns; critical parts additionally warn between `20%` and `40%`.
- All installed part conditions feed the raw-quality hardware penalty.
- The selected spool slot is occupied by a filament spool.
- Remaining filament on the selected spool is enough.
- Object fits the build volume.
- Print plate is present.
- No job is already active or queued on that printer.
- The printer is not calibrating.
- The object has a confirmed placement inside the usable plate area and within the build volume.
- Supports match the object: a model that needs supports can start with `None`, but receives a severe raw-quality penalty and a warning. `Auto` on a model that does not need it is allowed with a "not mandatory" warning and extra cost.

All part locations are scanned, not only the critical list. Critical parts are the only ones that block when missing; a completely worn non-critical part still blocks because the policy is “any part at 0% blocks”. Spools are the exception: they are consumables and are excluded from the generic scan, so only the selected spool is validated. An empty or worn spool on the unselected side does not block the job.

Critical parts are serialized as **`PartLocation` references** on the printer (`Printer.CriticalPartLocations`), not names. The print plate is `Printer.PrintPlateLocation`. References survive renames and support prefab variants; names are only used by the editor auto-assign helper. `ItemMetaSO.ItemTypes` is deliberately not used because it stores only broad paths (`FFF`, `Printer`, `Part`), not leaf types such as nozzle or print plate.

Balanced default critical set:

- extruder nozzle, heater, thermistor, fan, filament bearing,
- print bed and print bed thermistor (print plate is separate),
- X/Y belts, X/Y rotors, Z left/right rotors.

Other bearings/bushings are wear-warned and 0%-blocked by the all-parts scan but are not in the missing-critical list; add their references if the assembly should demand them.

Hard failures block `Start print`. Warnings do not block but are shown in the console. Any selected spool material is printable; material affects M3 temperatures and M5 quality rather than job validity.

### 6.5 Temperature model

Per printer:

```
PrinterTemperatureState
  - Ambient
  - Chamber
  - Nozzle
  - Bed
```

- Targets come from the selected spool's `FilamentMaterialProfileSO`; when it is null, fall back by `MaterialType` (PLA/PETG/ABS/TPU).
- Preheat uses a linear heating rate toward target.
- Printing holds targets; small deviations are allowed.
- Cooling uses an exponential decay toward ambient.
- An enclosure is detected through `PrinterEnclosure` bounds. It slows cooling, lets the chamber hold heat and improves the quality estimate. Draft, particulate and warp effects remain M5.
- Preheat completes when both nozzle and bed are within tolerance (default ±2 °C).
- In M3, a preheat timeout logs a warning and enters `Fault`; full thermal-failure gameplay is M5.
- Raw quality is capped at `90` even with an enclosure. The post-process state can polish the finished object later.

### 6.6 Wear and filament

Use the existing wear system and the correct delta types:

| Part | Delta | Notes |
|---|---|---|
| Nozzle / filament bearing | `WearDeltaFilamentTemperatureAbrasiveness` | filament length, nozzle temp, spool abrasiveness |
| Heater / thermistors / enclosure fire tube / thermometer | `WearDeltaTimeTemperature` | simulated seconds at operating temperature |
| Spool | `WearDeltaFilament` | consumed metres / spool length |
| Axes / belts / rotors / bearings | `WearDeltaDistanceTemperature` | distance approximated from simulated time until M4 paths exist |
| Extruder fan / HEPA fan | `WearDeltaTimeRPM` | simulated seconds at `FanRPM` |
| HEPA filter trap | `WearDeltaTimeFumes` | simulated seconds at `FumesDensity` |
| Battery closet adapter | `WearDeltaTime` | powered/print time |
| Print plate | `WearDeltaUses` | one use per completed/partial print |
| Print bed | `WearDeltaUsesHardness` | one use per completed/partial print, scaled by spool hardness |

Rules:

- Batch wear application about once per simulated second, not every frame.
- Use simulated process time, not wall-clock time.
- Enclosure parts are scanned from `PrinterEnclosure` as well as the printer assembly.
- The selected spool rotates by `consumedMetres / (2π × EffectiveRadiusMeters) × 360°`; its visible child is chosen by nearest `SpoolWearLevelsToGameObjectsDictionary` threshold.
- `ItemWearState.ApplyWear` calls `CalculateWearByRatio`, so the authored `WearRatio` is honored. Existing assets use `1`, so the change does not alter their current values.
- Spool remaining filament:

```
remainingMeters = wear.CurrentCondition / 100f * spoolData.FilamentLengthSpoolMeters;
```

`wear` and `spoolData` belong to the spool in the selected `SpoolSlot`.

This assumes the spool uses `ItemWearLogicFilamentSO`. Validate that in M0 and add a guard/fallback if the logic type is different.

- Add a direct condition-loss API to `ItemWearState`:

```
public void ApplyConditionLoss(float amount)
```

Refactor `ApplyWear` to use it. It must:
- clamp to `[MinCondition, MaxCondition]`
- trigger `OnItemWornOut` exactly once
- ignore further loss when already worn out

Hard-stop penalty (initial values, tune in M3):

- Nozzle: `0.2 + 0.8 × normalizedNozzleTemperature`
- Heater: `0.1 + 0.4 × normalizedNozzleTemperature`
- Bed/plate: `0.1 + 0.3 × normalizedBedTemperature`
- Axes: small optional penalty

These are one-off condition points, not `WearDelta` values.

### 6.7 Print visuals

#### Object instantiation

- Instantiate the object prefab under `PrintBedAnchor`.
- Center its bottom on the bed.
- Use the selected spool's `PrintingMaterial`.
- Disable colliders and physics during printing.
- Keep the object on the `object_to_print` layer for later hover/removal.
- Remove any ghost components from the instance.

#### Layer reveal

The object shader exposes `_reveal_height` and reveals vertices below a world-space cutoff relative to each renderer pivot:

```
worldPos.y - rendererPivot.y < _reveal_height
```

Therefore, per renderer:

```
cutoffWorldY = objectBaseWorldY + printedHeightMeters * objectScaleY;
revealHeight = cutoffWorldY - renderer.transform.position.y;
```

`ObjectsToPrint` prefabs are authored with the pivot at the lowest point of the mesh, centred in X/Z. For a renderer on the object root that makes its useful reveal range `0..height`; the formula above still subtracts the renderer pivot, so child renderers and other pivot conventions work too.

Important:

- The shader test is strict: `localY < _reveal_height`. A cutoff exactly on a vertex/face boundary clips that face.
- At the start, put the cutoff below the lowest vertex so nothing is visible.
- At completion, put the cutoff above the highest vertex or switch to the final material. Do not stop exactly at the object height, or the top face is lost.
- `PrintRevealDebug` pads its debug range for this reason; production code should pad by at least a fraction of a millimetre, or use the next layer height.
- `objectBaseWorldY` must follow the object/bed whenever the bed moves.
- Do not use a fixed world height for the whole print.
- Reset `_reveal_height` at the start of each print; the material default of `1` would show too much.
- Use a `MaterialPropertyBlock` per renderer with `_reveal_height`.
- Quantize the reveal to the layer height.
- Update the reveal at ~15 Hz, not every frame.

#### Motion

`PrinterMotionSimulator` generates cheap procedural print-space positions:

- Layer count from model height / layer height.
- Each layer follows a small Lissajous or zigzag path inside the object footprint.
- `ApplyNozzlePosition` takes X/Z in bed-local metres and Y as nozzle height above the bed.
- MK4S implements X on the extruder, Z on the bed carriage, and Y on the gantry. A dropping-bed printer implements the same roles differently.
- Add a small Z-hop on layer changes.
- Do not attempt real perimeter/infill geometry.
- Use a seeded random pattern so the motion is deterministic per job.
- Respect configured axis speeds and soft limits.
- Home the axes after finishing or hard-stopping.

The motion layer only talks to `PrinterMotionBase`; it never touches the MK4S transforms directly.

#### Spool

- Rotation arrives in M3: `degrees = consumedMetres / (2π × EffectiveRadiusMeters) × 360`, with a serialized local axis and direction.
- The rotation uses simulated consumption, so it follows `PrintSpeedMultiplier`.
- The visible filament child is chosen from `SpoolWearLevelsToGameObjectsDictionary` by nearest condition threshold.
- M4 adds shader reveal polish (`_reveal_height`) and visual verification with the printed object present.
- If the vertical reveal looks wrong, fall back to the M3 wear-level children rather than new prefabs in M5.

#### Completion and removal

- On normal completion:
  - Fully reveal the object.
  - Restore the object's final materials.
  - Re-enable colliders/surface placement.
  - Mark it as a completed print.
- On hard stop:
  - Freeze the reveal at the partial height.
  - Keep the printing material.
  - Mark it as a partial print.
- Removal:
  - Interact with the printed object or use a console action.
  - Destroy the instance and clear the printer's bed slot.
  - Partial prints are discarded, not resumed.

### 6.8 Console UI

Use one console per device, not a shared screen-space overlay.

Placement:

- The console is a world-space uGUI canvas parented to the device's `ScreenAnchor` and positioned in front of the measured display bounds.
- It stays visible on the device screen at all times, including while the player walks away.
- Logical width is `1600`; logical height follows the physical screen aspect. One uniform scale maps `1600` logical units to the display width.
- The canvas is offset in front of the front-most display point by `0.0015 m`.
- Only the focused device enables its `GraphicRaycaster`; its event camera is the active Cinemachine output camera. Interaction is impossible while unfocused.

Style:

- Follow `Assets/Art/3D/Prefabs/Interface/items_market`: the default rounded UI sprite, white `(1,1,1,0.78)` and gray `(0.52,0.52,0.52,0.78)` boxes, black text, sizes 20–25.
- No accent colours, gradients, or modern dashboard styling. Selection uses a black bar and weight, not hue.
- Key badges follow the `interactions_list` / `part_install_canvas` pattern: a white badge sized to its key (`LMB`, `Q`) plus a short label.
- Only non-obvious targets show hover text (printers in bind mode). Buttons do not change the hint line.
- Text for `Q` is short: `Quit` in the console, `Leave` in bind mode.

Layout:

- Header: device name, connection count, `Screen off`, `Quit`.
- Left column: bound printer list with active selection.
- Main area: a compact active-printer strip and a print-job card with the object list grouped by `ObjectToPrintType`, layer-height/speed/infill/supports/spool/simulation-speed selectors, estimates, raw-quality range, a scrollable validation/warnings list and `Start print`.
- Active-printer strip: state, progress, nozzle/bed temperatures and remaining filament through `Printer.StatusSummary`.
- Job-card controls: `Pause`/`Resume`, `Hard stop`/`Confirm stop`, `Calibrate` and `Clear bed`. `Clear bed` is a temporary M3 removal action; M4 replaces it with the real completed/partial print.
- `SIM SPEED` presets `1×`, `4×`, `64×`, `120×`; the selected value is stored on the active printer.
- Validation rows show every block and warning; the list scrolls independently so the selectors and `Start print` stay fixed. The job card itself is not a scroll view. The validation block has a subtle inset background and a thin top rule to separate it from the card.
- The printer list and object list are `ScrollRect` views with `RectMask2D`; mouse wheel and drag work over the full viewport, including empty space, and content grows with the number of entries.
- Footer: `Q Quit` chip, `Connect printer`, `Disconnect`. The `Q Quit` chip and the screen-off `Q Quit` chip share the same bottom-left anchor (`Margin`, `Margin`) so the badge does not jump between power states.
- Screen-off overlay: black panel with `Power on` and a `Q` hint.
- Bind mode uses the authored `device_bind_canvas` in the player's view, shown by `InterfaceController.SetDeviceBindState` together with `point_canvas`.
- Each active device keeps one cached console view. Temporary deactivation during placement/taking hides it, and the same view is revealed again on reactivation. On sale or destruction the view is removed, destroyed, and its generated mask textures are disposed.
- The console is clipped by a screen-shape mask: `DeviceScreenAnchor.ScreenMaskSprite` when set, otherwise a rounded-rectangle generated from `ScreenCornerRadius` (fraction of the logical width); `0` keeps square corners.
- The console hides whenever the display renderers are disabled, so selling, placing, or other ghost previews do not leave the UI floating over the ghost.
- `DeviceBindPanel` fills the canvas: the top `horizontal_stack` reads `Printer binding` / `Printer unbinding`, `device_bind_hover_name` shows the hovered printer, `bind_root` shows the constant `LMB Bind` key hint only while a printer is hovered, and `device_bind_reason` shows a failed-attempt reason for an exposed `ReasonDuration`.
- While bound to, the device canvas hides its header and footer and keeps the printer list.
- No toasts; success returns to the console silently.
- Every device spawns with its screen off.

Behavior:

- `DeviceConsolePresenter` owns one `DeviceConsoleView` per device and enables interaction through `Focus(device, eventCamera)` / `Unfocus()`.
- Views subscribe to their own device and refresh on events plus ~5 Hz polling; unfocused views refresh at a lower rate.
- Do not rebuild strings every frame.
- Disable buttons with reasons rather than hiding them.
- Confirm hard stop and unbind-while-printing.
- Do not show a printer as “connected” if its binding was removed.
- While a printer is busy, FreeWalk hover shows `Printer.StatusSummary` instead of take/sell prompts. The dedicated hover-status layout is an owner-authored UI demand.

### 6.9 Camera controller

`DeviceFocusController`:

- References:
  - `CinemachineCamera ConsoleCamera`
  - `CinemachineBrain Brain`
- Methods:
  - `Focus(Device device, Action onReady)`
  - `Release(Action onReady)`
- `DeviceController` enables console interaction in the `onReady` callback; the console canvas is already visible on the screen.
- Framing:
  - Get the screen anchor (`DeviceScreenAnchor` or `ItemComputingDataHolder.ScreenGameObject`).
  - Compute the screen center, extents and normal from the renderer bounds expressed in the screen anchor's frame. Do not use the world-space AABB: at arbitrary device rotations it inflates the size and pushes the console off the glass.
  - Place the camera at `center + normal * distance`.
  - Distance from screen size, FOV, and a fill factor; clamp to a safe minimum.
  - Orient toward the screen center directly; no look-at component is required.
- Priority:
  - Console camera active `100`; idle `-1`, below the player camera's implicit `0` so activation-order tie-breaking never selects it.
  - Release restores the console camera priority to `-1`.
- Blend:
  - Temporarily set the Brain's default blend to `BlendTime` (`0.55 s`).
  - Wait for the blend to start, then for it to finish, before invoking `onReady`.
  - Restore the previous default blend when the wait ends.
  - Cancel any pending focus/release coroutine when the console is closed early or the device becomes invalid.
- Event camera:
  - Exposes `Brain.OutputCamera` so the focused console can raycast with the player camera.
- Cursor:
  - Show/unlock in console.
  - Hide/lock in bind mode and free walk.

Fallback:

- If no Cinemachine camera is assigned, log an error and open the console without moving the camera. Do not silently break the flow.

### 6.10 Persistence and performance

Persistence:

- No save/load in the first implementation.
- Printer jobs are in-memory and do not survive scene reload.
- Document this clearly in the UI or design notes if it becomes relevant.

Performance budgets:

- Printer simulation: O(1) per tick.
- Reveal updates: ~15 Hz.
- Motion transform updates: ~30 Hz.
- UI refresh: ~5 Hz.
- No per-frame `new`, LINQ, or string concatenation in hot paths.
- Target: 4 simultaneously printing printers under ~0.5 ms CPU per printer on the main thread.
- Use `MaterialPropertyBlock`, cached renderers, cached transforms, and cached material property IDs.
- Avoid physics on the printed object during printing.

---

## 7. Data model changes

### `ObjectToPrintMetaSO`

Add a serializable print profile:

```
[Serializable]
public class ObjectToPrintPrintProfile
{
    public float FilamentMetersAt100PercentInfill;
    public float PrintTimeSecondsAt100PercentInfill;
    public bool SupportsRequired;
    public float SupportOverhangPercent;
}

[Serializable]
public class ObjectToPrintRequirements
{
    public InfillPattern DefaultInfill = InfillPattern.Grid;
}
```

Add both to the meta:

```
public ObjectToPrintPrintProfile PrintProfile;
public ObjectToPrintRequirements Requirements;
```

Object dimensions are **derived from the prefab bounds** by `ObjectToPrintSizing`, not authored. The global reference layer height (`0.2 mm`) and speed (`80 mm/s`) come from `LayerHeightSettings` / `PrintSpeedSettings`; only the two 100% baseline numbers are per object.

Why inline instead of a separate asset:

- The catalog is currently small.
- It keeps object metadata self-contained.
- A separate `ObjectToPrintPrintProfileSO` can be introduced later without changing job code if needed.

The current catalog has one entry (`Stanford bunny`, `Assets/Art/3D/Prefabs/ObjectsToPrint/Animal/Stanford_bunny/SOs/stanford_bunny_meta_SO.asset`). Its 100% infill baseline is calibrated against PrusaSlicer in the M2 gate.

### Infill

```
public enum InfillPattern
{
    None,
    Grid,
    Gyroid
}
```

Initial tuning table (calibrated against PrusaSlicer in the M2 gate):

| Pattern | Effective density | Time factor |
|---|---:|---:|
| `None` | 0.1892 | 0.2615 |
| `Grid` | 0.3153 | 0.3399 |
| `Gyroid` | 0.3531 | 0.3765 |

```
filament = profile.FilamentMetersAt100PercentInfill * density;
time = profile.PrintTimeSecondsAt100PercentInfill
     * infillTimeFactor
     * (LayerHeightSettings.ReferenceMillimeters / selectedLayerHeight)
     * Pow(PrintSpeedSettings.ReferenceMillimetersPerSecond / selectedSpeed, 0.8);
```

These values are intentionally simple and tunable. Infill changes time and filament; supports add a provisional multiplier; layer height and speed change time (filament stays constant at this abstraction). The reference layer height (`0.2 mm`) and speed (`80 mm/s`) are global settings.

Raw quality is projected from the same settings before post-processing:

```
raw = 82
    + layerDelta   (Draft -10, Normal 0, Fine +4)
    + speedDelta   (Slow +4, Normal 0, Fast -10)
    - hardwareWear (0..10 from the worst installed part condition)
    - supportsMark (2 when Auto supports are used)
    - supportRisk (25 when supports are required but None is selected)

range = clamp(raw, 20, 100) .. clamp(raw + 5, 20, 100)
```

M5 replaces the deltas with the full `PrintQualityModel` (warp, chamber, draft, cooling, failures). Post-processing raises the rolled raw value; supports must be removed, and sanding cannot fix warping or layer shifts.

### Filament material profile

Add a `FilamentMaterialProfileSO` (one asset per material) with:

- `NozzleTemperature`
- `BedTemperature`
- `RecommendedChamberTemperature`
- `Density`
- `WarpFactor`
- `CoolingRateMultiplier`

`ItemFFFSpoolDataHolder` gains an optional `MaterialProfile` reference. Any spool material can print any catalog object; `MaterialType` and the thermal profile are consumed in M3/M5 for temperatures, shrink, warp and quality. Assets for PLA/PETG/ABS/TPU are not created yet.

### Printer runtime state

Plain C# classes, not MonoBehaviours:

- `PrintJob`
- `PrintJobRequest` with `SpoolSlot` and `PrintPlacement`
- `PrintPlacement` (position on plate, yaw, uniform scale, object boundary corners)
- `PrintProgress`
- `PrintResult`
- `PrinterTemperatureState`
- `PrintJobValidationResult`

This keeps the FSM testable and avoids scene coupling.

---

## 8. Milestones

Each milestone has a **goal**, **deliverables**, **acceptance criteria**, and a **manual gate**. Do not start the next milestone until the manual gate is checked.

### Milestone summary

| Milestone | Goal | Exit gate |
|---|---|---|
| **M0** | Prefab wiring + calibration | Axes, bed anchor, reveal, hover, screen framing confirmed |
| **M1** | Console + binding | Open/close console, bind/unbind multiple printers, camera flow feels right |
| **M2** | Jobs + validation | Object/infill selection, believable estimates, clear rejection reasons |
| **M2.5** | Bed placement + profile tooling | Place/rotate/scale on the plate, live validity, estimates recalculated, editor profile generator |
| **M3** | FSM + temperatures + wear + enclosure | Full job cycle, pause/service/hard stop, calibration, wear/filament applied correctly |
| **M4** | Print visuals | Object reveal, motion, spool, partial print, performance |
| **M5** | Failure + advanced enclosure + polish | Failures, warp/draft/particulate effects, post-process quality, audio/VFX |

---

### M0 — Prefab wiring and calibration

**Goal:** Make the printer and device physically ready for logic. No printing gameplay yet.

**Deliverables**

- `Printer` root component with references to:
  - `PartsAssembly`
  - `PrinterMotionBase`
  - `PrintBedAnchor`
  - `PrinterEnvironment`
- `CartesianPrinterMotion` with serialized axis bindings:
  - Horizontal X → `x_axis_base/extruder_base`, local X
  - Horizontal Z → `print_bed_holder_base`, local Y (bed front/back)
  - Vertical Y → `x_axis_base`, local Z (gantry up/down)
- `PrintBedAnchor` under/at `print_bed_location`, calibrated to Y-up.
- `PrinterBindHighlight` with a translucent sphere sized to printer bounds; the same sphere is the bind hover volume.
- `Device` component on all four computing prefabs.
- Minimal `Device.GetInteractions()` returning `Use device`, plus the `ItemHoverInteract` parent-`Device` resolution. This is only the interaction entry point; in M0 the execute action can log a placeholder, and M1 wires it to the console state.
- `DeviceScreenDisplay` on all four computing prefabs.
- `DeviceScreenAnchor` at the screen center (or a documented fallback).
- `PrinterMotionDebug` / gizmos for manual axis movement.
- `PrintRevealDebug` for testing `_reveal_height`.
- Optional: placeholder `device_console_camera` in the scene.

**Acceptance criteria**

- The printer axes can be moved from a debug control without touching print logic.
- The bed anchor places a test cube flush on the plate, Y-up, centered.
- The test cube reveals correctly with `_reveal_height`.
- `F` prompt appears on all four computing device prefabs.
- The screen anchor frames the screen for all four device types.
- Bind highlight spheres fit the printer, are the bind hover volume, and do not block raycasts.

**Manual gate (must be recorded in the calibration table)**

1. Move each axis slowly; confirm direction and safe range.
2. Verify axis signs; record `Invert` values.
3. Verify the bed anchor: test cube bottom is flush with the plate, no rotation offset.
4. Verify the reveal: cutoff at 0, mid-height, and full height; no z-fighting.
5. Verify the spool: remaining-filament formula and `_reveal_height` behavior.
6. Verify device hover on laptop, PC, smartphone, and tablet.
7. Verify screen framing for each device type; record distance/offset.
8. Verify screen-off material swap and restore.
9. Verify bind highlight sphere bounds and colors.

**Do not proceed to M1 until the axis directions, bed anchor, and reveal direction are confirmed.**

---

### M1 — Device console and printer binding

**Goal:** The player can open a device console, connect and disconnect printers, and see a live printer list.

**Deliverables**

- Finalize the M0 `Device` interaction entry point if needed.
- `GameStateDeviceInterface(Device)` contextual state.
- `GameStateDeviceBind(Device, DeviceBindMode, GameState)` contextual state.
- `DeviceController`:
  - `OpenConsole`, `CloseConsole`
  - `EnterBindMode`, `ExitBindMode`
  - bind/unbind selection events
- `DeviceFocusController` with Cinemachine blend.
- `DeviceConsolePresenter` + `DeviceConsoleView` with:
  - one always-visible world-space console per device
  - one cached view per device, reused across temporary deactivation and disposed only when the device is sold or destroyed
  - printer list
  - active selection
  - connection status
  - `Connect printer` / `Disconnect`
  - `Screen off`
  - `Quit`
  - `Q Quit` chips in the console footer and on the screen-off overlay, at the same anchor
- `PrinterBindTargeting` with hover highlight.
- `PrinterBindHighlight` colors:
  - bindable: cyan
  - already bound to this device: green
  - bound to another device (unavailable): red
  - selected for unbinding in unbind mode: amber
  - hovered: slightly more opaque (`0.30` vs `0.18`), never fully opaque
- `PlayerController.SetDeviceInterfaceState` / `SetDeviceBindState`.
- Scene wiring for `DeviceController` and the console canvas. The scene already contains empty `states/device_interface` and `interface/device_interface` marker objects; use or replace them rather than adding parallel placeholders.

**Scene wiring (implemented)**

- `states`: `DeviceController` next to `GameStateManager` (single shared controller).
- `controllers/device_bind`: `PrinterBindTargeting` with the player camera, `3 m` range, all layers. It is a persistent controller, not a state object.
- `controllers/interface/device_bind/device_bind_canvas`: `DeviceBindPanel` bound to the authored title, hover name, `bind_root` and reason texts; `ReasonDuration` is exposed (currently `2 s`).
- `InterfaceController.PointInterface` and `InterfaceController.DeviceBindPanel` drive the bind UI.
- `interface/device_interface`: `DeviceConsolePresenter` using the Montserrat SDF font and the built-in rounded UI sprite. It owns one world-space `DeviceConsoleView` per device, built at runtime under that device's screen anchor; there is no screen-space console HUD.
- New root `device_console_camera`: `CinemachineCamera` (priority `-1` idle, FOV `55`, near clip `0.01`) plus `DeviceFocusController`.
- `GameStateManager.DeviceController` points at the shared controller.
- The `demonstration_1` laptop instance is active so the console has an authored device next to a paired printer.

**Acceptance criteria**

- Hovering a computing device shows `Use device` and `F`.
- `F` focuses the device screen and enables console interaction.
- The console is visible on the device screen even while the player is in free walk.
- `Q` leaves the console and returns the camera and controls.
- `Connect printer` enters bind mode, highlights bindable printers, and binds on LMB.
- `Q` cancels bind mode and returns to the console.
- Binding a printer already bound to another device fails with feedback.
- Bound printers appear in the console list.
- `Disconnect` enters unbind mode and removes the selected binding.
- Multiple printers can be bound to one device and selected in the list.
- Unbinding while printing does not stop the print.
- Screen off shows a black physical screen with an on-screen `Power on` overlay; power-on restores both.
- Hovering a printer in bind mode shows its name and the `bind_root` `LMB Bind` hint; buttons stay silent.
- No duplicate event subscriptions or dangling references.

**Manual gate**

1. Test camera framing and blend time for all four device types.
2. Test cursor show/hide/lock in console, bind mode, and free walk.
3. Test binding from different player positions and distances.
4. Test that printers behind walls cannot be bound.
5. Test UI at 16:9, 21:9, and 4:3; text must remain legible.
6. Walk away and confirm the console stays visible on the device screen but cannot be clicked from free walk.
7. Test screen-off and power-on transitions.
8. Test binding persistence while walking away and returning.
9. Test unbinding a printing printer and rebinding it.

**Do not proceed to M2 until binding and camera flow feel comfortable and do not break free walk.**

---

### M2 — Job selection and validation

**Goal:** The console can present objects and print settings, calculate estimates and a raw-quality range, and reject invalid jobs with clear reasons.

**Deliverables**

- `ObjectToPrintPrintProfile` (baseline filament/time, supports) and `ObjectToPrintRequirements` on `ObjectToPrintMetaSO`.
- `ObjectToPrintSizing` derives object dimensions from the prefab bounds.
- `InfillPattern` and `InfillSettings`.
- `SupportsMode` (`None` / `Auto`) and support cost/penalty rules.
- `FilamentMaterialProfileSO` (data type; spool reference is optional).
- `LayerHeightPreset`, `PrintSpeedPreset`, `LayerHeightSettings`, `PrintSpeedSettings`.
- `SpoolSlot` (`Left` / `Right`) and printer spool-slot references.
- `PrintJobRequest`, `PrintJobValidationResult`, `PrintJobValidator`, `PrintEstimates`, `PrintQualityEstimator`.
- Console job picker, layer-height/speed/infill/supports/spool selectors, estimate panel, raw-quality range, validation panel.
- `Start print` gating and a `Printer.PendingJob` slot for M3.
- Placeholder printer status until M3 runs jobs.
- Placement is deliberately deferred to M2.5; M2 uses the default placement for estimates and commands.

**Acceptance criteria**

- The object list is populated from `ObjectToPrintMetasListSO` and grouped into sections by the object's first `ObjectToPrintType`; new types get a new section automatically.
- Selecting an object shows name, icon, derived height, estimates, and a raw-quality range.
- Layer height, speed and infill selections update time and filament (filament depends on infill and supports only).
- Supports defaults from the meta; `Auto` adds cost, and a required object with `None` shows the severe quality penalty.
- The `SPOOL` row selects `Left` or `Right`; the console defaults to an occupied slot and warns when the selected slot is empty.
- Every installed part at `0%` blocks; every installed part at `20%` or below warns; critical parts also warn between `20%` and `40%`.
- The projected raw-quality range moves with layer height, speed and critical-part condition.
- `Start print` is disabled when:
  - no printer is selected
  - printer is busy
  - a partial print is on the bed
  - any installed part is at 0% (critical parts missing also block)
  - the selected spool slot is empty or contains no filament spool
  - remaining filament on the selected spool is insufficient
  - the object does not fit the build volume
  - the print plate is missing
- Validation messages are specific, readable, and all current blocks/warnings are listed in the scrollable validation block.
- Estimates are deterministic for the same object, settings and spool.

**Manual gate**

1. Fill in the two 100% baseline values for the current object catalog; dimensions and reference settings are derived/global.
2. Compare estimates against a slicer at Normal layer height and Normal speed; record the tuning factor. Check one Draft and one Fine print for the layer-height scaling.
3. Verify spool remaining calculation at 100%, 50%, and 10% condition, including Left/Right selection and a missing selected slot.
4. Verify build-volume rejection with an oversized test object.
6. Verify that the console cannot start a job on an unbound printer.
7. Toggle supports on a required and a non-required object; confirm the warning, cost and severe quality penalty.
8. Set a critical part to `30%` (critical warning), a non-critical part to `10%` (low warning), and any part to `0%` (block).

**Do not proceed to M3 until the estimates and validation rules are believable.**

---

### M2.5 — Interactive bed placement and profile tooling

**Goal:** The player places, rotates and uniformly scales the selected object on the print plate before starting, with live validity feedback, and object profiles can be seeded from an editor tool.

**Deliverables**

- `PrintPlacement` data on the job: local position on the plate, yaw, uniform scale, object boundary corners.
- `GameStateObjectPlace(Device, Printer, draft job, returnState)` contextual state. Replace the empty `GameStateObjectPlace` stub; no new global state.
- Plate focus camera/controller, separate from `DeviceFocusController`, using `Printer.ResolvedBedAnchor` (`PrintBedAnchor`) for the plate plane, size and front direction.
- `ObjectPlacementController` using the existing `ObjectPlace` input map:
  - hold LMB + pointer raycast for movement; bind `ObjectPlacementOnPlate` to pointer position instead of mouse delta,
  - hold `R` + mouse delta for yaw,
  - hold `S` + mouse delta for uniform scale,
  - hold `RMB` for camera orbit; wheel for zoom,
  - confirm Enter, cancel `Q`;
  - an internal `PlacementMode` enum (`Idle`, `Moving`, `Rotating`, `Scaling`, `OrbitingCamera`) is enough; no new global states.
- Ghost of type `ObjectToPrintGhostType.PrinterBedPlacement`, using `CornersList`/bounds for footprint.
- Shader feedback through `shader_object_to_print_printer_bed_placement_mode`:
  - `_printer_bed_placement_state` (`0` valid, `1` wrong),
  - `_PRINTER_BED_PLACEMENT_STATE_WRONG` keyword,
  - apply through a `MaterialPropertyBlock` on the ghost renderers.
- Validation (soft limits, no hard clamping):
  - rotated, scaled footprint inside the usable plate area (plate bounds minus margin),
  - height within `PrinterMotionBase.BuildVolume.y`,
  - scale inside the dynamic min/max described below,
  - confirm disabled while invalid; the ghost uses the wrong shader state.
- Scale limits are dynamic:
  - maximum: the largest scale that keeps the rotated footprint inside the usable plate and the scaled height inside `PrinterMotionBase.BuildVolume`;
  - minimum: the scale at which the object's largest bounding dimension reaches `2 cm` (`MinimumObjectSizeMeters`);
  - the player may exceed either limit; the placement simply becomes invalid until corrected.
- Estimates:
  - initial console estimates remain the default placement (centred, yaw `0`, scale `1`);
  - after confirm, filament and time scale by `scale³` (fixed layer height and speed), then feed validation and `PrintJobRequest`;
  - rotation changes fit only, not volume.
- `Place on bed` button in the console; `Start print` requires a confirmed placement.
- Placement is draft-only and required for every print. It is not stored on the printer or reused; leaving the console or starting a print discards it so the next job must place the object again.
- **Placement UI is authored by the project owner.** The roadmap asks for the hints/validity/confirm-cancel UI before implementation.
- Editor tool `PrintProfileGenerator`:
  - batch or selected `ObjectToPrintMetaSO` entries from the catalog,
  - load the prefab, combine renderer bounds (report only; runtime sizing uses the same data), compute mesh volume,
  - estimate overhang ratio and set `SupportsRequired` plus `SupportOverhangPercent`,
  - seed 100% infill filament/time from volume and a configurable reference flow,
  - share the reference layer height/speed and flow in one editor settings asset,
  - report what was written; slicer values still override the seeds.

**Acceptance criteria**

- The object appears at the plate centre on first entry with estimates matching the M2 defaults.
- Moving keeps the footprint on the plate; invalid positions turn the ghost to the wrong shader state.
- Rotation and scale update the ghost and validity live.
- Scale changes the displayed time/filament; rotation does not.
- Overscaling/underscaling is possible, shows the wrong shader state, and cannot be confirmed.
- Confirm stores placement and returns to the console; cancel restores the previous placement.
- Confirm and cancel blend back to the console with the same Cinemachine blend used to enter placement.
- `Start print` is disabled until a valid placement is confirmed.
- The editor tool reports dimensions and fills supports/baseline seeds for every catalog entry without scene edits.
- No overlapping objects are allowed because one job per printer and partial prints block new jobs.

**Manual gate**

1. **Done:** push the ghost to each plate corner and beyond; the wrong state appears and confirm is blocked.
2. **Done:** go below the dynamic min (`2 cm` largest dimension) and above the max (`BuildVolume`/plate fit); the wrong state appears and confirm is blocked.
3. **Done:** recalculated estimates match the cube-scaling rule at the minimum, `1.0` and the maximum scale.
4. **Done:** cancel restores the previous placement and confirm survives console close/reopen.
5. **Done:** the editor generator on the bunny matches the seeded volume/filament against the slicer.
6. **Done:** confirm and cancel return with the same blend duration as entering.
7. **Done:** the project owner authored the placement UI on `object_place_canvas`.

**M2.5 gate passed; M3 may start after the M2.5 commit.**

---

### M3 — Printer FSM, temperatures, wear, and enclosure loop

**Goal:** A job actually runs through preheat, print, cool, and finish; wear and filament are consumed; the player can pause, service, hard-stop and calibrate through the console.

**Deliverables**

- `PrinterStateMachine` with the states and transitions in section 6.3, including `Calibrating`.
- Simulated-time ticking with a per-printer `PrintSpeedMultiplier`.
- Console `SIM SPEED` preset row: `1×`, `4×`, `64×`, `120×` (default `120×`). The value is stored on the printer, not globally, and can be changed while idle or printing.
- `PrinterTemperatureModel` with ambient/chamber/nozzle/bed, linear heating, hold, exponential cooling and a preheat timeout.
- Material targets from `FilamentMaterialProfileSO`; fallback profile by `MaterialType` because current spool assets have no profile assigned.
- `PrinterWearIntegrator`:
  - nozzle: `WearDeltaFilamentTemperatureAbrasiveness`
  - spool: `WearDeltaFilament`
  - heater/thermistors/fire tube/thermometer: `WearDeltaTimeTemperature`
  - battery closet adapter: `WearDeltaTime`
  - axes/belts/rotors: `WearDeltaDistanceTemperature` (distance approximated until M4 paths)
  - fans: `WearDeltaTimeRPM`
  - HEPA filter trap: `WearDeltaTimeFumes`
  - print plate: `WearDeltaUses` once per print
  - print bed: `WearDeltaUsesHardness` once per print, scaled by spool material hardness
- `ItemWearState.ApplyConditionLoss` and `ApplyWear` routed through it.
- `SpoolFilamentState` for the selected `SpoolSlot`, including remaining metres, spool rotation and wear-level child sync.
- Rename `ItemFFFPrusaMK4SSpoolDataHolder` to `ItemFFFSpoolDataHolder`.
- Spool rotation from consumed filament:
  - `degrees = consumedMetres / (2π × EffectiveRadiusMeters) × 360`;
  - rotation follows simulated consumption, so the visual speed follows `PrintSpeedMultiplier`;
  - `SpoolWearLevelsToGameObjectsDictionary` shows the nearest authored condition level.
- Enclosure integration:
  - `PrinterEnclosure` marker and automatic containment detection;
  - enclosure slows cooling, lets the chamber hold heat, and reduces/removes the no-enclosure quality penalty;
  - raw quality is capped at `90` even with an enclosure; post-processing can polish beyond that later.
- Hard-stop flow with partial-print flag, one-off condition penalties and console confirmation.
- Console live status binding: state, progress, nozzle/bed temperatures, remaining filament.
- Console controls for `Pause`/`Resume`, `Hard stop`/`Confirm stop`, `Calibrate` and `Clear bed` (temporary M3 removal action).
- Printer continues ticking when unbound.
- Pause/resume through the console, not a new InputAction:
  - pause stops print progress, moves the nozzle to a service position and lets the nozzle/bed cool;
  - service swap is allowed only after the nozzle and bed are below `SafeServiceTemperatureC` (default `50 °C`);
  - parts may be swapped while paused, except `Printer.ServiceBlockedDuringPrint` references (default: print bed, print bed thermistor, print plate);
  - resume requires every critical part and the selected spool to be installed again; if a blocked part must change, the print can only be hard-stopped;
  - a pause leaves a seam/quality penalty and a long pause may add adhesion risk in M5.
- Calibration state:
  - console `Calibrate` button starts an `N × N` grid, with `N` exposed as `CalibrationGridSize`;
  - the nozzle visually touches every point on the plate at height `0`, then homes;
  - `Hard stop` cancels calibration.
- Interaction protection while a printer is busy:
  - sell, take and free-walk interaction are blocked for the printer and its enclosure;
  - hovering shows `Printer.StatusSummary` instead of take/sell prompts.
- **Owner UI demand:** the dedicated FreeWalk hover-status section for busy printers/enclosures is authored by the project owner; the code exposes `Printer.StatusSummary` and blocks the existing interactions.

**Acceptance criteria**

- Start job → `Preheating` → `Printing` → `Cooling` → `Finished` using the stored placement/scale.
- Preheat completes at target temperature within tolerance.
- Print progress reaches 100% at the expected simulated duration.
- Cooling returns temperatures toward ambient; enclosure cooling is visibly slower.
- Nozzle, bed/plate/heater/thermistor, enclosure parts and selected-spool wear are applied to the correct parts.
- Every existing wear delta type is exercised by at least one installed printer/enclosure part or by the completed-print use pass.
- The selected spool's condition and remaining filament decrease consistently; the unselected spool is untouched.
- The selected spool rotates proportionally to consumed filament and its visible wear-level child follows its condition.
- Hard stop at any point:
  - stops motion and heaters
  - applies the one-off penalty
  - leaves a partial print only after printing starts
  - blocks new jobs until the bed is cleared
- Unbinding the device during a print does not stop the printer.
- Rebinding shows the correct live state.
- A worn-out part blocks a new job; low parts warn.
- Pause mid-print:
  - motion and progress stop, heaters cool to the safe service temperature;
  - swapping an allowed part works and the print can resume;
  - swapping a `ServiceBlockedDuringPrint` part is refused;
  - resume applies the pause seam/quality penalty;
  - the print does not fail merely because it was paused.
- Calibration with `N` grid points touches the plate at every point and returns to idle.
- The console shows the chosen simulation multiplier and live printer status.
- A busy printer/enclosure cannot be sold, taken or freely interacted with.
- Raw quality tops out at `90`; the post-process state can improve the finished object later.

**Manual gate**

1. Run a short job and compare condition loss to the configured wear logic; tune `WearPerHour` / `WearPerFilamentLength`.
2. Record preheat, print, and cooling times at `1×`, `4×`, `64×` and `120×`; confirm the multiplier changes simulated speed only.
3. Tune the hard-stop penalty so it is noticeable but not catastrophic.
4. Profile 2–4 printers printing simultaneously; check CPU and GC.
5. Verify no NaN/Inf in temperatures, progress, or condition.
6. Verify partial print blocks the next job and `Clear bed` releases it.
7. Pause, wait for cooling, swap a nozzle/fan/belt, resume and confirm the seam penalty.
8. Attempt to swap a print-bed part while paused and confirm it is refused.
9. Run calibration with `N = 3` and `N = 5`; confirm every point is touched and the nozzle homes afterwards.
10. Verify the spool rotates in the correct direction and the visible filament level follows the wear dictionary.
11. Check enclosure and no-enclosure quality differences; confirm the raw maximum is `90`.
12. Try to sell/take/interact with a busy printer and enclosure; confirm the status-only hover and blocked actions.

**Do not proceed to M4 until the numbers and timing feel right, wear is applied to the intended parts, and pause/service/calibration are stable.**

---

### M4 — Print visualization

**Goal:** The print looks convincing: object reveal, nozzle/bed motion, support columns, and removal. Spool rotation and wear-level sync already arrive in M3.

**Deliverables**

- `PrintedObjectPresenter`.
- `PrintedSupportPresenter`: sample overhang triangles for the placed object, cluster them to a capped number of points (e.g. `48`), and spawn thin vertical support columns to the plate. Simple Y-scaled cubes or cylinders are enough; reveal them with print height and hide them after completion.
- `PrintRevealController`.
- `PrinterMotionSimulator`.
- `CartesianPrinterMotion` integration.
- Spool reveal polish; rotation and wear-level child sync already arrive in M3.
- Completion material restore and collider enable.
- Partial-print removal.
- If printing ghosts are reused, fix the existing `ObjectToPrintGhostPrintingDataHolder` bug: its `OnValidate`/`Awake` currently assign `ObjectToPrintGhostType.Quality` instead of `Printing`. Prefer instantiating the real object prefab for prints and bypassing this ghost path.

**Acceptance criteria**

- The object appears where the M2.5 placement put it: position, yaw and uniform scale.
- With `Auto` supports, visible support columns appear under the overhangs and are revealed with the print; `None` prints no supports.
- Reveal starts empty and grows bottom-up.
- Reveal follows the moving bed correctly.
- The nozzle appears to work over the printed area.
- Axis motion stays within soft limits and does not clip.
- The M3 spool rotation continues to read correctly during the full print.
- Hard stop freezes the partial object at the correct height.
- Completed object is fully visible and removable.
- No per-frame material instancing or GC spikes.

**Manual gate**

1. Compare the reveal top to the nozzle height; adjust the offset.
2. Check bed movement direction and speed.
3. Check the object from several player angles; no popping or z-fighting.
4. Check multi-mesh objects; all renderers reveal consistently.
5. Re-check the M3 spool visual at 100%, 50%, 10%, and 0% with the printed object present.
6. Check partial print height and removal.
7. Check completed object materials, colliders, and selling/placement behavior.

**Do not proceed to M5 until the print reads clearly as a print from the player's normal viewing distance.**

---

### M5 — Failure, advanced enclosure, and polish

**Goal:** Add depth and edge cases after the core loop is stable.

**Candidate deliverables**

- Advanced enclosure effects: draft, particulate filtration, warp and long-pause adhesion risk. Basic chamber temperature, cooling slowdown and enclosure quality are already in M3.
- `PrintQualityModel` expanding the M2 `PrintQualityEstimator` range, and a real quality value on `IsHaveQuality`.
- Failure modes:
  - filament runout
  - clog
  - thermal fault
  - warping
  - layer shift
- Quality-dependent sale value.
- Maintenance scheduling when critical parts enter the `20–40%` warning band.
- Optional print queue.
- Audio/VFX:
  - fan hum
  - stepper motion
  - completion chime
  - error buzzer
- Screen animations and notifications.
- Save/load if desired.
- Optional resume-from-partial-print.
- Production cleanup:
  - remove the calibration-only `PrintBedAnchorDebug` and `PrinterMotionDebug` components from shipped prefabs, or keep them disabled and stripped from the player build;
  - remove any placed `PrintRevealDebug` and temporary test cubes;
  - confirm `PrinterBindHighlight.DebugState` is `Hidden`;
  - keep editor-only debug context menus and diagnostic helpers out of the player path, using `#if UNITY_EDITOR` or an editor assembly where appropriate;
  - remove temporary debug logging/GUI that is not part of the shipped experience.

**Acceptance criteria**

- Enclosure changes thermal behavior and quality in a visible, understandable way.
- Failure modes are telegraphed and recoverable.
- Quality affects the object's value or usability.
- The game remains performant with multiple printers.
- Shipped prefabs/scenes contain no active calibration-only debug components, test cubes, or debug GUI.

---

## 9. Calibration record

Fill this table during M0 and update it during later milestones.

| Item | Recorded value | Notes |
|---|---|---|
| Horizontal X transform | `x_axis_base/extruder_base` | local X |
| Horizontal X min / max offset | `-0.127` / `0.127` | signed local units, at print-space -X / +X |
| Horizontal X speed | `0.2` | units/s |
| Horizontal Z transform | `print_bed_holder_base` | local Y (bed front/back) |
| Horizontal Z min / max offset | `-0.07` / `0.151` | signed local units, at print-space -Z / +Z |
| Horizontal Z speed | `0.2` | units/s |
| Vertical Y transform | `x_axis_base` | local Z (gantry up/down) |
| Vertical Y min / max offset | `-0.22` / `0` | signed local units, first layer / full height |
| Vertical Y speed | `0.1` | units/s |
| Build volume X / Y / Z | `0.256` / `0.221` / `0.223` | meters |
| Bed anchor local position | `(0, 0, 0)` | relative to `printer_print_plate_location` |
| Bed anchor local rotation | identity | `SurfaceOffset = 0.0016 m`, plate `0.256 × 0.223 m` |
| Test cube flush / centered | yes | `SpawnOnStart` disabled after calibration |
| Reveal cutoff formula verified | yes | strict `localY < _reveal_height`; endpoints padded |
| Reveal update rate | 15 Hz |  |
| Spool reveal mapping | same cutoff direction | remaining filament is the inverse of print progress; verify in M4 |
| Spool rotation axis |  | not implemented yet; decide in M4 |
| Spool remaining formula verified | no | deferred to M4 |
| Laptop screen anchor + distance | +Z → world +Z | oriented renderer bounds in the anchor frame (fill `0.82`, FOV `55`, padding `0.025 m`, min `0.12 m`); verify in the M1 gate |
| PC screen anchor + distance | +Z → world +Z | computed from bounds (fill `0.82`, FOV `55`, padding `0.025 m`, min `0.12 m`); prefab not yet instanced, verify in the M1 gate |
| Smartphone screen anchor + distance | +Z → world +Y | device lies flat; distance computed from bounds (same values); prefab not yet instanced, verify in the M1 gate |
| Tablet screen anchor + distance | +Z → world +Y | device lies flat; distance computed from bounds (same values); prefab not yet instanced, verify in the M1 gate |
| Camera blend in / out | `0.55 s` EaseInOut | both directions; console interaction enables after the focus blend |
| Bind highlight bounds | printer bounds + `0.08 m` padding | translucent sphere, no collider |
| Bind hover volume | the highlight sphere itself | analytic ray-sphere test; aiming at the bubble is enough |
| Bind hover opacity | `0.30` vs `0.18` idle | slightly more opaque on hover, never opaque |
| Bind distance | `3 m` | `PrinterBindTargeting.MaxDistance`; to the sphere entry point, with a line-of-sight raycast |
| Bind reason duration | `2 s` | `DeviceBindPanel.ReasonDuration` on `device_bind_canvas` |
| Console UI | world-space uGUI canvas per device | logical width `1600`, height from screen aspect, `0.0015 m` offset, Montserrat SDF, items_market style |
| Console canvas facing | forward points into the screen | UGUI is readable when the viewer looks along canvas `+Z`; verify in the M1 gate |
| Console canvas placement | display bounds centre + normal | offset from the front-most bounds point, then `0.0015 m`; verify in the M1 gate |
| Console refresh | `0.2 s` | focused view on events plus ~5 Hz polling; other views slower |
| Infill density | None `0.1892`, Grid `0.3153`, Gyroid `0.3531` | `InfillSettings.Density` |
| Infill time factor | None `0.2615`, Grid `0.3399`, Gyroid `0.3765` | `InfillSettings.TimeFactor` |
| Layer height | `0.28` / `0.20` / `0.12` mm | `LayerHeightSettings`; time scales `reference / selected` |
| Print speed | `40` / `80` / `140` mm/s | `PrintSpeedSettings`; time scales `(reference / selected)^0.8` |
| Raw quality | base `82`, range width `5`, raw cap `90` | `PrintQualityEstimator`; enclosure `+5`, no enclosure `-9`; post-process can polish beyond 90 later |
| Part wear (all installed) | block `0%`, warn `≤20%`; critical warn `20–40%` | `WornOutConditionPercent` / `LowConditionWarningPercent` / `CriticalPartWarningPercent` |
| Job catalog | `Assets/SOs/ObjectsToPrint/object_to_print_metas_list_SO.asset` | assigned to `DeviceConsolePresenter.ObjectCatalog` |
| Placement scale | soft limits: largest dimension ≥ `2 cm`; footprint/height within plate/`BuildVolume` | overscale/underscale is allowed but invalid; no clamp |
| Plate margin | `2 mm` inside the plate bounds | keeps the footprint off the plate edge |
| Placement shader | `_printer_bed_placement_state` `0/1`, `_PRINTER_BED_PLACEMENT_STATE_WRONG` | ghost validity feedback |
| Scale estimate rule | filament and time × `scale³` | fixed layer height and speed |
| Support cost (provisional) | filament × `1.06`, time × `1.12` | replace with `SupportOverhangPercent` model later |
| Support rule | required object: `None` → `-25` raw and max `55`; optional `Auto` → warning only | `PrintJobValidator` / `PrintQualityEstimator` |
| Material gating | none | any spool material prints; M3/M5 use material for temperatures and quality |
| Bunny baseline (calibrated) | `85.36 m` at 100%, `36 720 s` at 100% | PrusaSlicer, `0.2 mm` / `80 mm/s`; dimensions derived |
| Console mask | `ScreenCornerRadius` / `ScreenMaskSprite` on `DeviceScreenAnchor` | smartphone starts at `0.05`; set `0` for square corners |
| Console camera idle priority | `-1` | player camera stays at implicit `0` |
| Print speed multiplier | `1×` / `4×` / `64×` / `120×` | console `SIM SPEED`; default `120×`, stored per printer |
| Temperature tolerance | `±2 °C` | preheat completes when nozzle and bed are in tolerance |
| Preheat timeout | `900` simulated seconds | logs a warning and enters `Fault` |
| Safe service temperature | `50 °C` | pause service gate |
| Hard-stop penalty | nozzle `0.2 + 0.8 × normalized`, heater `0.1 + 0.4 × normalized`, bed/plate `0.1 + 0.3 × normalized` | tune in the M3 gate |
| Calibration grid | `5 × 5`, `0.15 s` dwell, `10 mm` padding | `CalibrationGridSize` / `CalibrationPointDwellSeconds` / `CalibrationPaddingMeters` |
| Spool rotation | `degrees = metres / (2π × 0.1 m) × 360` | `EffectiveRadiusMeters`, local axis and invert flag on `ItemFFFSpoolDataHolder` |

---

## 10. Test plan

### M0

- Axis movement test.
- Bed anchor test cube.
- Reveal slider test.
- Device hover test.
- Screen framing test.
- Screen-off material test.
- Bind highlight test.

### M1

- Open/close console from all four devices.
- Console stays visible on the device screen from free walk.
- The bind canvas shows `Printer binding`, the hovered printer name and `bind_root`; failed clicks on a printer show the reason.
- Screens spawn off; a fresh device shows the `Power on` overlay when focused.
- Bind/unbind one printer.
- Bind/unbind multiple printers.
- Bind rejection cases.
- Camera blend and cursor behavior.
- Screen-off persistence.
- UI aspect-ratio checks.

### M2

- Job list and estimates.
- Layer-height and speed changes.
- Infill changes.
- Supports default/cost/warning/penalty.
- Raw-quality range changes with settings, supports and part wear.

### M2.5

- Enter/exit placement, cursor and camera behavior, including a smooth confirm/cancel return blend.
- Move, rotate and scale; soft limits and invalid feedback.
- Shader valid/wrong state.
- Default estimates and post-placement recalculations.
- Confirm/cancel and persistence through console close/reopen.
- Editor profile generator output, including overhang percent.
- Validation for every failure reason.
- Build volume rejection.
- Filament sufficiency.

### M3

- Full job cycle.
- Simulation speeds `1×`, `4×`, `64×`, `120×`.
- Pause/resume and safe service swap.
- Hard stop at preheat, mid-print, and near-complete.
- Wear application per part, including enclosure parts and every delta type.
- Selected spool depletion, rotation and wear-level child sync.
- Enclosure vs no-enclosure thermal behavior and quality cap.
- Calibration grid at `N = 3` and `N = 5`.
- Busy printer/enclosure blocks sell/take/interact and shows status-only hover.
- Unbind/rebind during print.
- Worn-out part blocks job.
- Multiple printers simultaneously.

### M4

- Visual reveal at start/mid/end.
- Auto supports render as columns under overhangs and follow the print height.
- No supports render when `None`.
- Bed motion and nozzle path.
- M3 spool visual with the printed object present.
- Partial print freeze and removal.
- Completed object material/collider restore.
- Performance profile.

### M5

- Enclosure thermal/quality effects.
- Failure modes, including wear-triggered service prompts.
- Quality and sale value.
- Audio/VFX.
- Save/load, if implemented.
- Production cleanup: calibration/debug components removed or disabled, test cubes removed, editor-only debug helpers excluded from the player.

---

## 11. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Prefab rotations make axis mapping non-obvious | M0 debug movement + calibration table; use `AxisBinding.Invert` |
| Bed anchor orientation is wrong | Calibrate with a test cube in M0; `printer_print_plate_location` composes to an identity world rotation, but verify it |
| Reveal looks wrong because the bed moves | Compute cutoff from the object's current base, not a fixed world Y |
| Shader property block compatibility | Verify `_reveal_height` with MPB in M0; fall back to material instances if necessary |
| Camera blend fights player look | Dedicated camera + priority + explicit blend; disable look in console |
| Wear values are unbalanced | Tune in M3 with short jobs; keep values in SOs |
| UI scope creep | One generic console; defer dashboards and fancy animations to M5 |
| Multiple printers cause CPU spikes | Update budgets; no per-frame allocations; reveal at 15 Hz |
| No save/load | Document in-memory scope; add persistence later if needed |
| Partial print blocks the player with no way out | Always provide removal interaction/console action |

---

## 12. Out of scope / future

- Print queue.
- Resume from partial print.
- Real slicer/G-code.
- Multi-material printing.
- Printer networking/range.
- Full device power/battery simulation.
- Delta/CoreXY printer prefabs.
- Auto-generated print profiles.
- Multiplayer.
- Save/load.

---

## 13. Current status and next step

**Status:** M0 and M1 are committed. M1 implementation is complete and the laptop flow is approved; formal M1 close still needs the other device types and aspect ratios checked. M2 is committed and pushed, calibrated against PrusaSlicer and play-mode gated. M2.5 is committed and complete. The first M3 iteration is implemented: the printer owns the job FSM, temperature/wear simulation, pause/service/hard-stop controls, calibration state, enclosure loop and spool visuals. The M3 play-mode gate is still pending.


M1 is code-complete:

- `DeviceController` owns the console flow and bind selections.
- `DeviceFocusController` frames the screen, blends the camera, and exposes its event camera.
- `DeviceScreenFraming` shares screen measurements between framing and the on-screen console.
- `DeviceConsolePresenter` manages one always-visible `DeviceConsoleView` per device.
- `DeviceBindPanel` fills the authored `device_bind_canvas` with the hovered printer and failed-attempt reasons.
- The console lives on the device screen, follows the items_market style, and hides its header and footer while binding; `device_bind_canvas` and `point_canvas` are the bind UI.
- Only the focused console enables its raycaster, so free walk cannot click a remote screen.
- `PrinterBindTargeting` (under `controllers/device_bind`) plus `PrinterBindHighlight` provide sphere-hover targeting and feedback.
- `GameStateDeviceInterface` / `GameStateDeviceBind` are contextual; no new global states or InputActions.
- `MainScene` wires the controller, targeting, presenter, and `device_console_camera`. No MonoBehaviour lives under `states/...` except the legacy shared `GameStateManager`/`DeviceController` on the `states` root.

M1 is the highest-leverage gate after M0 because it proves the player-facing loop:

- The console focuses the screen and returns control cleanly.
- Binding is centre-screen and cannot reach through walls.
- A printer never stops because its device stopped listening.
- One console serves every device type.

Remaining M1 work (manual gate):

1. Open `MainScene`; confirm `F` focuses the laptop screen and enables console interaction.
2. Confirm the console stays visible on the screen after `Q`, from free walk, and after walking away.
3. Confirm `Q` restores the player camera and controls.
4. Confirm `device_bind_canvas` and `point_canvas` appear, the title reads `Printer binding`, the hovered printer name fills in, and failed attempts show their reason for `ReasonDuration` seconds.
5. Bind and unbind the paired MK4S and the test printer; confirm highlight colours and rejection feedback.
   - The "unbinding does not stop the print" criterion can only be exercised once M3 provides a running print.
6. Confirm screen off/on affects both the console and the physical display.
7. Check the console at 16:9, 21:9, and 4:3.
8. Instance the PC, smartphone, and tablet prefabs; verify framing, canvas fit, hover, and bind targeting.
9. Record any tuning values in §9.

M2 is implemented:

- `ObjectToPrintPrintProfile` (two baselines + supports) and `ObjectToPrintRequirements` live on `ObjectToPrintMetaSO`; object size is derived by `ObjectToPrintSizing`.
- `InfillPattern`, `LayerHeightPreset`, `PrintSpeedPreset`, `PrintEstimates`, `PrintQualityEstimator`, `PrintJobRequest`, `PrintJobValidationResult` and `PrintJobValidator` cover settings, estimates, raw quality and rules.
- `Printer` exposes `PendingJob`, `HasPendingJob`, `HasPartialPrint`, `CriticalPartLocations`, `PrintPlateLocation`, `LeftSpoolLocation` and `RightSpoolLocation` references.
- The console's print-job card has an object picker, layer-height/speed/infill/supports rows, time/filament/raw-quality summary, validation text, and a gated `Start print`.
- `DeviceConsolePresenter.ObjectCatalog` is wired in `MainScene` to the authored catalog.

Remaining M2 work (manual gate):

1. **Done:** object baseline calibrated against PrusaSlicer: `85.36 m` and `36 720 s` at 100% infill (`0.2 mm`, `80 mm/s`).
2. **Done:** Grid / Normal / Normal compared with PrusaSlicer (`28.53 m`, `3 h 53 min`) and matched by the console (`28.5 m`, `3 h 53 min`); tuning factor recorded in §9.
3. **Done:** Draft and Fine spot checks confirmed time and raw quality move in the expected direction.
4. **Done:** validation matrix verified, including the oversized-object rejection.
5. **Done:** `Start print` queues `Printer.PendingJob` only when valid; M3 consumes it on the next printer tick.
6. **Done:** `Printer.CriticalPartLocations` / `PrintPlateLocation` references confirmed.

M1's remaining device-type/aspect checks are carried forward; they do not block M2 validation.

M2.5 placement is implemented against the project-owner-authored `object_place_canvas` (move/rotate/scale hints, `object_place_reason`, confirm/cancel). The editor profile generator is implemented already: `Tools/Manufacturing/Print Profile Generator`. Follow-up fixes add console `Left`/`Right` spool selection, keep the cached console view (and its job draft) across placement/taking until the device is sold, and blend the placement camera back to the console on confirm/cancel. The manual gate passed with the authored tuning values.

The M3 first iteration is ready for its manual gate. The remaining owner-authored UI demand is the dedicated FreeWalk printer-status hover section; the code already blocks take/sell/interact and exposes `Printer.StatusSummary`.

---

## Appendix A — MK4S hierarchy reference

```
Prusa_MK4S_printer_root
├── free_parts_reset_points
├── spool_holder
│   ├── colliders
│   └── placement_points
│       ├── spool_location_1
│       └── spool_location_2
├── base
│   ├── computer_closet
│   ├── z_axis_guide_left
│   ├── print_bed_holder_base
│   │   ├── colliders
│   │   └── placement_points
│   │       ├── y_axis_bearing_* locations
│   │       └── print_bed_location
│   │           └── placement_points
│   │               ├── print_bed_thermistor_location
│   │               └── printer_print_plate_location
│   ├── z_axis_guide_right
│   ├── x_axis_base
│   │   ├── placement_points
│   │   ├── colliders
│   │   └── extruder_base
│   │       ├── colliders
│   │       └── placement_points
│   │           ├── extruder_thermistor_location
│   │           ├── extruder_filament_bearing_location
│   │           ├── extruder_bearing_* locations
│   │           ├── extruder_filament_heater_location
│   │           ├── extruder_nozzle_location
│   │           └── extruder_fan_location
│   └── colliders
└── corners
```

Notes:

- `base` has a 180° rotation about `(0, 1, 1) / √2`, so its children's local Y/Z are swapped relative to world.
- `x_axis_base` is the moving gantry. `extruder_base` is a child of it. Its local Z is world up.
- `print_bed_holder_base` is the moving bed carriage. Its local Y is world front/back.
- `print_bed_location` has a non-identity local rotation but an identity world rotation, so a child anchor with identity local rotation is Y-up.
- `print_bed_location/placement_points/printer_print_plate_location` holds the `Prusa_MK4S_printer_print_plate_meta_SO` metadata and is the **print plate** node. It was authored as `extruder_thermistor_location`; it was renamed because the metadata, not the name, defines the part. The real extruder thermistor remains under `x_axis_base/extruder_base/placement_points`.
- The print plate node sits ~5.8 mm above `print_bed_location` and also composes to an identity world rotation. Use it as the bed-surface reference.
- `spool_location_1` and `spool_location_2` are the two spool mounts.

---

## Appendix B — Proposed file map

### Scene hierarchy conventions

- MonoBehaviours belong under `controllers/...` (persistent controllers, targeting, managers) or `interface/...` (UI presenters and canvases).
- `states/...` folders are scene markers for state-scoped data or objects. Do not attach scripts under them. If a state needs behaviour, put the component under `controllers/...` and let the state call it.
- The only current exceptions are the shared `GameStateManager` and `DeviceController` on the `states` root. Treat them as legacy and do not add more scripts there.
- `PrinterBindTargeting` follows this rule: it lives under `controllers/device_bind` because it persists across states and only runs while bind mode is active.

M0 files actually added are listed in Appendix D. The tree below is the full target layout.

```
Assets/Scripts/Coursework_Master_Degree/
├── Items/Computing/
│   ├── Device.cs
│   ├── DeviceScreenDisplay.cs
│   └── DeviceScreenAnchor.cs
├── Items/FFF/Printer/
│   ├── Printer.cs
│   ├── PrinterStateMachine.cs
│   ├── PrinterJob.cs
│   ├── PrinterTemperatureModel.cs
│   ├── PrinterWearIntegrator.cs
│   ├── PrinterEnvironment.cs
│   ├── PrinterEnclosure.cs
│   ├── SpoolFilamentState.cs
│   ├── Jobs/
│   │   ├── InfillPattern.cs
│   │   ├── PrintEstimates.cs
│   │   ├── PrintJobRequest.cs
│   │   ├── PrintJobValidationResult.cs
│   │   ├── PrintJobValidator.cs
│   │   ├── SimulationSpeedPreset.cs
│   │   └── SpoolSlot.cs
│   ├── Motion/
│   │   ├── PrinterMotionBase.cs
│   │   ├── CartesianPrinterMotion.cs
│   │   ├── PrinterMotionSimulator.cs
│   │   └── AxisBinding.cs
│   ├── Visuals/
│   │   ├── PrintedObjectPresenter.cs
│   │   ├── PrintRevealController.cs
│   │   └── PrintBedAnchor.cs
│   └── Binding/
│       ├── PrinterBindTargeting.cs
│       └── PrinterBindHighlight.cs
├── Game/Controller/
│   ├── DeviceController.cs
│   ├── DeviceFocusController.cs
│   ├── ObjectPlacementController.cs
│   └── ObjectPlacementCameraController.cs
├── Game/Logic/Interface/DeviceConsole/
│   ├── DeviceConsolePresenter.cs
│   ├── DeviceConsoleView.cs
│   ├── DeviceConsoleStyle.cs
│   ├── DeviceConsoleChip.cs
│   └── DeviceJobPanel.cs
├── Game/Logic/Interface/DeviceBind/
│   └── DeviceBindPanel.cs
├── Game/StateMachine/State/
│   ├── GameStateDeviceInterface.cs   (modify)
│   └── GameStateDeviceBind.cs        (modify)
├── ScriptableObjects/
│   ├── Items/
│   │   └── FilamentMaterialProfileSO.cs
│   └── ObjectsToPrint/
│       ├── ObjectToPrintMetaSO.cs        (modify)
│       └── ObjectToPrintPrintProfile.cs
└── Editor/
    ├── PrintProfileGeneratorWindow.cs
    └── DetectPlacedItemsWindow.cs
```

---

## Appendix C — Decision log

| Date | Decision | Rationale |
|---|---|---|
| 2026-11-16 | Device → many printers; printer → one device | Realistic, future-proof, avoids refactor |
| 2026-11-16 | Printer owns job and keeps printing when unbound | Physical printer autonomy |
| 2026-11-16 | Enclosure hooks now, basic gameplay in M3, advanced effects in M5 | Thermal stability and quality matter as soon as jobs run; warp/draft/particulate stay polish |
| 2026-11-16 | `PrinterMotionBase` + `CartesianPrinterMotion` | Supports future printer constructions |
| 2026-11-16 | Dedicated Cinemachine console camera | Consistent framing, clean blend |
| 2026-11-16 | Screen-only power-off | Prints continue; no full power simulation |
| 2026-11-16 | No new input actions for M0–M4 | Existing inputs and UI EventSystem cover the flow |
| 2026-11-16 | No job queue | Keep first pass simple |
| 2026-11-16 | Partial print remains after hard stop | Approved default |
| 2026-11-16 | 120× simulated print time | Approved default |
| 2026-11-16 | MK4S axes: X on extruder, Z on bed carriage, Y on gantry | `base` is rotated 180° about `(0,1,1)/√2`; node-local axes do not match their names |
| 2026-11-17 | No MonoBehaviours under `states/...` | State folders are markers/data; controllers and targeting persist and belong under `controllers/...` |
| 2026-11-17 | Bind targeting ray starts at the player camera | The first-person camera is the eyes; a separate eye anchor would drift from the view |
| 2026-11-17 | Screen metrics measured in the anchor frame | A world AABB inflates `FrontOffset` by `0.1–0.2 m` and the extents at non-90° device rotations |
| 2026-11-17 | Key badge images are sliced | A simple image stretches the rounded corners when the badge is wider than the sprite |
| 2026-11-17 | Console clipped by a screen-shape mask | Rounded phone displays otherwise show a rectangular UI; sprite override covers notches |
| 2026-11-17 | Console follows screen renderer visibility | Selling/placing ghosts disable the screen renderers, so the UI must hide with them |
| 2026-11-18 | Critical parts are `PartLocation` references | Stable under renames; `ItemMetaSO.ItemTypes` stores broad paths only, and names are brittle |
| 2026-11-18 | `Start print` stores `Printer.PendingJob` in M2 | Keeps M2 validation-only while giving M3 a single entry point |
| 2026-11-18 | Linear infill estimates | Predictable, tunable from a slicer comparison; refine coefficients instead of per-object special cases |
| 2026-11-18 | Expose layer height and speed only | They are the two settings with clear time/quality trade-offs; nozzle diameter is hardware |
| 2026-11-18 | Raw quality as a range in M2 | Gives post-processing a target before the full M5 quality model exists |
| 2026-11-18 | Bed placement as M2.5 | Job setup, not printing; M3/M4 need stored placement before execution/visuals |
| 2026-11-18 | Uniform scale allowed in placement | Volume matters; scale changes estimates by `scale³`; bounds are dynamic |
| 2026-11-18 | Scale limits from printer/object | Max must fit `BuildVolume` and plate; min keeps the object at least `2 cm` |
| 2026-11-18 | `PrintBedAnchor` is the placement reference | One source for surface, size and front direction |
| 2026-11-18 | Placement is per-job and not persisted | Same as slicing: each print is set up fresh |
| 2026-11-18 | Scale limits are soft, not clamped | The player sees the shader error and cannot confirm; no silent snapping |
| 2026-11-18 | Supports visualised in M4 as simple columns | Full support generation is a slicer problem; columns make the abstract supports readable |
| 2026-11-18 | Placement UI authored by owner | Keeps the game-feel/UI design with the project owner |
| 2026-11-19 | Simulation speed is a per-printer console preset | The printer owns the job; presets match existing console rows and avoid slider UI work |
| 2026-11-19 | All wear delta types are mapped to printer/enclosure parts or print-use actions | Existing assets and SOs already define the intended wear kinds; no parallel wear model |
| 2026-11-19 | Raw print quality caps at 90 | Leaves room for the post-process state to improve the final object |
| 2026-11-19 | Spool rotation and wear-level child sync move to M3 | The loop already knows consumed metres; M4 only polishes reveal |
| 2026-11-19 | Calibration is a printer state, not a console overlay | Reuses motion and the existing status/controls; no new global state |
| 2026-11-19 | Busy printer/enclosure blocks sell/take/interact | Protects a running job; hover status is an owner-authored UI section |
| 2026-11-18 | Editor profile generator seeds data | Mesh bounds/volume/overhang are automatable; exact time still needs a slicer reference |
| 2026-11-18 | Supports are `None`/`Auto`, driven by meta | The model's geometry decides whether supports are needed; the player chooses the cost |
| 2026-11-18 | Profile split into baseline + requirements; size derived | Two calibratable numbers per object; dimensions come from prefab bounds |
| 2026-11-18 | Missing required supports is a penalty, not a block | Starting is allowed but raw quality drops by `25` and caps at `55` |
| 2026-11-18 | Critical wear has a warning band `20–40%` | Gives the player a maintenance warning before a hard block |
| 2026-11-18 | All installed parts checked at start | Any part at 0% blocks; all parts ≤20% warn; critical parts get an earlier 20–40% warning |
| 2026-11-18 | Pause can service non-blocked parts | Avoids losing a print to a cheap wear part; bed/plate remains a hard-stop swap |
| 2026-11-18 | Warnings use a dedicated scroll view | All reasons stay accessible without scrolling away job controls; future warnings fit |
| 2026-11-18 | Critical defaults cover function groups | Extruder, bed, belts/rotors and filament feed; bearings remain wear-only |
| 2026-11-18 | Placement uses pointer raycast + modifiers | Direct manipulation feels better than delta-only movement; camera stays on RMB |

---

## Appendix D — M0 implementation status

**Status:** scripts added; prefab and scene wiring pending (manual, by design).

**Added scripts**

| File | Purpose |
|---|---|
| `Assets/Scripts/Coursework_Master_Degree/Items/FFF/Printer/Motion/AxisBinding.cs` | One calibrated axis: node, local direction, signed travel offsets, speed |
| `.../Motion/PrinterMotionBase.cs` | Abstract print-space ↔ mechanism mapping, build volume gizmo |
| `.../Motion/CartesianPrinterMotion.cs` | MK4S mapping: extruder X, bed carriage Z, gantry Y |
| `.../Visuals/PrintBedAnchor.cs` | Y-up bed anchor with plate and up-axis gizmos |
| `.../Visuals/PrintBedAnchorDebug.cs` | Spawns/clears a test cube flush on the plate |
| `.../Visuals/PrintRevealDebug.cs` | Drives `_reveal_height` with an in-game slider and Empty/Mid/Full buttons |
| `.../Binding/PrinterBindHighlight.cs` | Runtime translucent sphere sized to printer bounds; state colours |
| `.../Debug/PrinterMotionDebug.cs` | Play-mode per-axis sliders; shows normalized value and local offset |
| `.../PrinterEnvironment.cs` | Enclosure/ambient/draft/particulate data; M3 uses enclosure cooling/quality and M5 adds advanced effects |
| `.../Printer.cs` | Printer root hardware references and configuration validation |
| `Assets/Scripts/Coursework_Master_Degree/Items/Computing/Device.cs` | `IInteractable` entry point, `Use device`, screen helpers, `UseRequested` |
| `.../Computing/DeviceScreenDisplay.cs` | Swaps display materials for screen-off, restores on power-on |
| `.../Computing/DeviceScreenAnchor.cs` | Marker for screen centre and outward normal |

**Modified script**

| File | Change |
|---|---|
| `Game/Logic/Hover/ItemHoverInteract.cs` | Resolves `Device` on the prefab root before the simple-interactable path; caches it so child colliders do not flicker |

**Compile check:** all new and modified scripts were compiled against the project's Unity 6000.6.8f1 assemblies and `Assembly-CSharp`; no errors. Only the expected duplicate-type warning from checking `Device`/`ItemHoverInteract` alongside the previously built assembly.

**Deliberate M0 behaviour**

- `Device.RequestUse()` logs a placeholder until M1 wires the console state.
- Screens spawn off: `DeviceScreenDisplay` applies the off state in `Awake`, and `ApplyStateOnAwake` has been removed.
- Runtime materials are created for the bind sphere and screen-off state only if no material is assigned.
- No scene or `InputActions` change has been made. The only deliberate prefab change is renaming the print plate node to `printer_print_plate_location`; its fileIDs and part metadata are unchanged.

**Known follow-ups**

- Record the actual MK4S `MinLocal` / `MaxLocal` offsets in §9.
- Confirm the test cube is flush, centred, and Y-up.
- Verify the reveal endpoints with the padded `PrintRevealDebug` range.
- Add a `_reveal_height` object printing shader before M4; current `ObjectsToPrint` materials are URP Lit.
- Decide in M1 how the screen anchor is authored per device type.

---

## Appendix E — M0 wiring checklist

Do this in the Unity editor. Use the prefab root `.../Prusa_MK4S_printer_root.prefab` and the four computing prefabs.

### E.1 Bed anchor

The real plate surface is the node `printer_print_plate_location` under `base/print_bed_holder_base/placement_points/print_bed_location/placement_points`. It holds the `Prusa_MK4S_printer_print_plate_meta_SO` metadata, sits ~5.8 mm above `print_bed_location`, is centred on the plate, and composes to an identity world rotation.

1. Under that print plate node, create an empty child named `print_bed_anchor`.
2. Reset its local transform. The whole chain composes to an identity world rotation, so the child should already be Y-up; verify with the gizmo.
3. Leave it at the node origin, which is on the plate surface. (If you instead parent the anchor under `print_bed_location`, set `SurfaceOffset = 0.0058` to reach the surface.)
4. Add `PrintBedAnchor`. Set `SurfaceOffset` to `0` for printing; use `0.001` only if the test cube z-fights. Set `PlateSize` to the measured plate size, e.g. `(0.25, 0.21)`.
5. Add `PrintBedAnchorDebug` to the same object for now. Cube size `0.02`, `SpawnOnStart = true`.

Caveat: parenting the anchor to the plate part means it deactivates if the plate part is uninstalled. That is physically reasonable for printing, but for M0 make sure the plate is installed before testing.

### E.2 Motion and printer root

1. Add `PrinterEnvironment` to the printer root. `HasEnclosure = false`, ambient/chamber `21`.
2. Add `CartesianPrinterMotion` to the printer root and set:
   - `BuildVolume = (0.25, 0.21, 0.21)` as a starting point; replace with the calibrated volume.
   - `HorizontalX.Node = base/x_axis_base/extruder_base`, `LocalAxis = (1,0,0)`, `Speed = 0.20`, `MinLocal = MaxLocal = 0`.
   - `HorizontalZ.Node = base/print_bed_holder_base`, `LocalAxis = (0,1,0)`, `Speed = 0.20`, `MinLocal = MaxLocal = 0`.
   - `VerticalY.Node = base/x_axis_base`, `LocalAxis = (0,0,1)`, `Speed = 0.10`, `MinLocal = MaxLocal = 0`.
   - `BedAnchor = print_bed_anchor` (the child of the print plate node).
3. Add `Printer` to the root. It auto-assigns `Assembly`, `ItemData`, `Motion`, and `Environment`. Assign `BedAnchor = print_bed_anchor`.
4. Add `PrinterBindHighlight` to the root. `BoundsRoot = root`, `Padding = 0.08`. Leave `SphereMaterial` empty unless URP shader stripping becomes a problem.
5. Add `PrinterMotionDebug` to the root. `Motion` auto-assigns. Keep `ShowGui = true`.

### E.3 Axis calibration

Enter play mode with the test cube spawned. The debug sliders apply a raw local offset from the authored position, independent of `MinLocal` / `MaxLocal`, so the range can be discovered safely.

Calibrate to the **usable print volume**, not the mechanical travel extremes. `AxisBinding` maps normalized `0..1` across `MinLocal..MaxLocal`, and `ApplyNozzlePosition` maps the build volume onto that same span. Recording a larger mechanical range stretches print-space metres and makes `BuildVolume` disagree with the machine; recording a smaller range clips the object. Use the plate edges for X/Z and the first-layer gap plus build height for Y. Never drive an axis into a hard stop to find a limit.

1. Drag the Horizontal X slider. Confirm the nozzle moves left/right. Record the offset when the nozzle is over the left plate edge and over the right plate edge.
2. Drag Horizontal Z. Confirm the **bed** moves front/back. Record the offsets when the nozzle is over the front and back plate edges.
3. Drag Vertical Y. Confirm the **gantry** moves up/down. Record the offset at the first-layer position (nozzle tip just above the plate) and at full build height. The authored nozzle sits roughly 0.257 m above the plate, so expect a large **negative** first-layer offset and a less negative full-height offset.
4. If an axis needs more than `DebugTravel = 0.25` m, raise `PrinterMotionDebug.DebugTravel` and re-enter play mode.
5. Stop play mode and enter the offsets:
   - `HorizontalX.MinLocal` = offset at print-space -X (left plate edge), `MaxLocal` = offset at +X (right plate edge).
   - `HorizontalZ.MinLocal` = offset at print-space -Z (one plate edge), `MaxLocal` = offset at +Z (the opposite edge).
   - `VerticalY.MinLocal` = first-layer offset, `MaxLocal` = full-height offset.
   - Offsets are **signed** and measured from the authored local position. `MinLocal` may be numerically greater than `MaxLocal`.
   - Use `Invert` only if it makes the recorded order more natural; do not use it to hide an unknown direction.
6. Set `BuildVolume.x` and `BuildVolume.z` to the measured plate spans, and `BuildVolume.y` to the measured full height. The calibrated span on each axis must equal its matching `BuildVolume` component, or the print is rescaled.
7. Re-enter play mode. Enable `Test calibrated range`. Move X/Z to 0.5 and confirm the nozzle sits over the bed centre. Move Y to 0 and 1 and confirm the first-layer gap and full height.
8. Confirm the test cube is flush, centred, and upright. Remove the cube or disable `SpawnOnStart` before saving.

### E.4 Reveal

1. With the test cube spawned, add `PrintRevealDebug` to the cube or assign its `MeshRenderer`.
2. Use a material that exposes `_reveal_height`. The debug logs a warning if the property is missing. As of M0, only the spool shadergraphs expose this property; the `ObjectsToPrint` shaders are URP Lit and do not. Use a spool material on the test cube for now.
3. Check `Empty` (nothing visible), `Mid` (lower half), and `Full` (whole cube). The shader uses strict `localY < _reveal_height`, so the debug pads the range by `EndpointPadding` (default 0.5 mm) plus 0.1% of the height. If `Empty` still shows the bottom face or `Full` still clips the top face, raise `EndpointPadding` and click `Reinitialize`.
4. Do not test a real `ObjectsToPrint` prefab yet. Its current material has no `_reveal_height`; that shader must be added before M4.
5. Record the spool shader's cutoff direction. It reveals fragments **below** the cutoff, which matches the object reveal convention; the spool's remaining-filament mapping is the inverse of print progress.

### E.5 Computing devices

Repeat for `laptop_root`, `PC_root`, `smartphone_root`, and `tablet_root`.

1. Add `Device` to the root. `Computing` auto-assigns; leave `Screen` and `ScreenAnchor` until the next steps.
2. Add `DeviceScreenDisplay` to the root. `Computing` auto-assigns. Leave `ScreenRenderers` empty so it collects from `ScreenGameObject`. Leave `ScreenOffMaterial` empty for a runtime black material. Screens spawn off: the component applies the off state in `Awake`, and the old `ApplyStateOnAwake` flag is gone.
3. Create an empty child named `screen_anchor` under the **screen mesh GameObject**, not the device root: `.../screen/screen_anchor`. This keeps the anchor attached to the display when a laptop lid or monitor moves. Put it at the display centre (reset its local position first) and rotate it until its local +Z (blue arrow) points **out of** the screen toward the viewer. Add `DeviceScreenAnchor`. Use `InvertNormal` if the blue arrow must point inward. A root-level anchor works only if the screen never moves relative to the root.
4. Assign `Device.Screen` and `Device.ScreenAnchor`.
5. Test hover on each child mesh: the prompt should read `Use device`. Press `F`; the console should open and the camera should focus the display (M1).
6. Enter **play mode** first, then test screen-off from the component context menu: select the device root, open the `DeviceScreenDisplay` component's ⋮ menu, and choose `Screen Off`, `Screen On`, or `Toggle Screen`. The context menu relies on the materials cached in `Awake`, so it is not a safe edit-mode test. Confirm the display goes black and `Screen On` restores the authored material. Stop play mode afterward; nothing is saved.
7. The console camera is now wired once in `MainScene` (`device_console_camera` with `CinemachineCamera` priority `-1` and `DeviceFocusController`); there is no per-prefab camera component. For each prefab, verify the anchor by selecting `screen_anchor` and aligning the Scene view to it (`Ctrl+Shift+F`): the display should be centred and the blue +Z arrow should point outward.

### E.6 Bind highlight

1. Add `PrinterBindHighlight` to the printer root if not already added. Leave it hidden by default.
2. In play mode, set the component's `DebugState` to `Bindable`, or use the `Debug/Show ...` context menu. Confirm the sphere encloses the printer, does not block the hover raycast, and is translucent.
3. Check `BoundToThisDevice`, `BoundToOtherDevice`, `SelectedForUnbind`, and `SetHovered(true)`.

### E.7 Record and stop

Fill §9 with the recorded values, then stop. Do not start M1 until the manual gate passes and the mapping is confirmed. If any axis moves on the wrong node, fix the wiring rather than adding code compensation.

---

## Appendix F — M2 configuration and calibration

This is the M2 counterpart to Appendix E: what to fill in, where, and how to tell the result is believable. Code is already wired; the steps below are data and verification.

### F.1 Catalog and object profiles

The console reads `DeviceConsolePresenter.ObjectCatalog`, which is assigned in `MainScene` to:

`Assets/SOs/ObjectsToPrint/object_to_print_metas_list_SO.asset`

Each entry is an `ObjectToPrintMetaSO`. Add objects to `ObjectToPrintMetasList` in the order they should appear in the console. The console groups entries by the first `ObjectToPrintType`; add a new enum entry to get a new section automatically. Entries with no type fall under `OTHER`.

For every meta, set `PrintProfile`:

| Field | Meaning |
|---|---|
| `FilamentMetersAt100PercentInfill` | Filament for a solid print at the global reference profile |
| `PrintTimeSecondsAt100PercentInfill` | Time for a solid print at the global reference profile |
| `SupportsRequired` | Geometry needs supports; drives the console default and validation |
| `SupportOverhangPercent` | Optional generated overhang ratio for future support-cost tuning |
| `DefaultInfill` (Requirements) | Initial console selection |

Object width/height/depth are **not authored**: `ObjectToPrintSizing` derives them from the catalog prefab bounds. The reference layer height and speed are global (`0.2 mm`, `80 mm/s`).

The estimates shown by the console are:

```
filament = FilamentMetersAt100PercentInfill * density(infill)
         * (supports Auto ? 1.06 : 1)
time     = PrintTimeSecondsAt100PercentInfill
         * infillTimeFactor
         * (0.2 / selectedLayerHeight)
         * (80 / selectedSpeed)^0.8
         * (supports Auto ? 1.12 : 1)
```

The job card exposes `LAYER`, `SPEED`, `INFILL` and `SUPPORTS` as four rows. Raw quality is projected from the same settings and shown next to the estimates.

Current coefficients live in `InfillSettings`:

| Infill | Density | Time factor |
|---|---:|---:|
| None | 0.1892 | 0.2615 |
| Grid | 0.3153 | 0.3399 |
| Gyroid | 0.3531 | 0.3765 |

Model consistency:

- Filament depends only on infill and supports. Layer height and speed do not change the material volume.
- Time is monotonic: finer layers, slower speed, denser infill and `Auto` supports all increase it.
- Raw quality depends on layer height, speed, supports and hardware condition. Infill changes strength, not the projected surface quality.
- Only `Grid` was measured directly against the slicer. `None` and `Gyroid` are proportional extrapolations of the calibrated `Grid` coefficients.

| Setting | Values | Raw-quality delta |
|---|---|---:|
| Layer height | Draft / Normal / Fine | `-10 / 0 / +4` |
| Print speed | Slow / Normal / Fast | `+4 / 0 / -10` |

Raw quality starts at `82`, subtracts up to `10` for the worst installed part condition, subtracts `2` for `Auto` supports, and subtracts `25` (cap `55`) when a model that needs supports is printed with `None`. It is shown as a `min..min+5` range.

Do not tune per object; calibrate the 100% profile and the global coefficients.

### F.2 Printer requirements

Select the printer root (`Prusa_MK4S_printer_root`) and check the `Printer` component:

- `CriticalPartLocations` — direct `PartLocation` references. Defaults:
  - extruder nozzle, heater, thermistor, fan, filament bearing
  - print bed, print bed thermistor
  - X/Y belts, X/Y rotors, Z left/right rotors
- `PrintPlateLocation` — direct reference to `printer_print_plate_location`.
- `Auto-Assign Critical Parts` (context menu) fills both from the default names; normal validation uses only the references.

Validation matches locations by name because `ItemMetaSO.ItemTypes` stores only the broad item path (`FFF`, `Printer`, `Part`), not leaf types such as print plate or nozzle. If the hierarchy is renamed, update these strings.

Every installed part is inspected:

- `0%` or worn out → block (`WornOutConditionPercent`);
- `20%` or less → warning (`LowConditionWarningPercent`);
- critical parts additionally warn between `20%` and `40%` (`CriticalPartWarningPercent`).

All part conditions feed the raw-quality hardware penalty. Non-critical wear may also affect mid-print failure in M5.

Bed assembly dependency order (MK4S, and the pattern for future printers):

| Location | Install prerequisites (`InstallPrerequisiteLocations`) | Must be removed first (`RemovalBlockerLocations`) |
|---|---|---|
| Y-axis bearing ×4 | none | print bed, bed thermistor, print plate |
| `print_bed_location` | all four Y-axis bearings | bed thermistor, print plate |
| `print_bed_thermistor_location` | `print_bed_location` | print plate |
| `printer_print_plate_location` | `print_bed_location`, `print_bed_thermistor_location` | none |

This gives the installation order: Y-axis bearings → print bed → bed thermistor → print plate. Removal order: print plate → bed thermistor → print bed → Y-axis bearings. The lists are used by `PartLocation.CanInstall` / `CanRemove`. Installation hover/validation only checks `InstallPrerequisiteLocations`; `RemovalBlockerLocations` is only a removal-order constraint and must not block installing a part.

### F.3 Spool

1. Install one or both spools into `spool_location_1` / `spool_location_2` on the printer. The optional spool group requires at least one.
2. `Printer.LeftSpoolLocation` / `RightSpoolLocation` map those locations to the console's `SPOOL: Left / Right` row. The console defaults to an occupied side; the player chooses the active side explicitly.
3. On the spool's `ItemFFFSpoolDataHolder`, set `MaterialType` (PLA/PETG/ABS/TPU) and keep `FilamentLengthSpoolMeters` at the physical spool length (default `300 m`).
4. Remaining filament is read from the selected spool's `ItemWearState.CurrentCondition`:
   `remaining = FilamentLengthSpoolMeters * CurrentCondition / 100`.
   In play mode use the `ItemWearState` context menu `Debug/Set Condition 100% / 50% / 10%` to test thresholds without editing assets.
5. Optionally assign a `FilamentMaterialProfileSO` for M3 temperatures. If it is null, M3 falls back by `MaterialType`.

If the selected slot is empty, the console reports `No filament spool installed on the Left side.` / `... Right side.` and disables `Start print`. The other side can be selected from the `SPOOL` row.

Only the selected spool participates in spool validation and wear/quality checks. A worn or empty spool on the unselected side is ignored until the player selects it.

### F.4 Start print

`Start print` is enabled only when `PrintJobValidator` returns no errors. The request stores the object, infill, layer height, speed, selected `SpoolSlot`, placement, estimates and raw-quality range in `Printer.PendingJob`. The printer consumes it on the next tick, enters `Preheating`, and the console switches to live status. Nothing is instantiated on the bed until M4.

### F.5 Validation matrix

Check each row in play mode and confirm the console message:

| Setup | Expected |
|---|---|
| No active printer | `No printer selected.` |
| Printer bound to another device | `Printer is not connected to this device.` |
| Printer printing/heating/paused/cooling/calibrating | `Printer is busy (...)` |
| Completed print still on bed | `Printer is busy (Finished).` |
| Partial print on bed | `A partial print is still on the bed.` |
| Job already queued | `A job is already queued on this printer.` |
| Selected spool slot empty | `No filament spool installed on the Left/Right side.` |
| Selected slot has non-spool item | `The Left/Right spool slot does not contain a filament spool.` |
| Selected spool condition 0 | `The Left/Right filament spool is empty.` |
| Selected spool condition ≤ 20% | `Low condition on the Left/Right spool (n%).` |
| Selected spool below estimate | `Not enough filament on the Left/Right spool: ...` |
| Critical location empty | `Missing part: ...` |
| Any installed part at 0% | `Worn out: ... (location, 0%).` |
| Any installed part at 20% or less | `Low condition: ... (location, n%).` |
| Critical part at 21–40% | `Critical part condition: ... (location, n%).` |
| Print plate location empty | `Print plate is missing.` |
| Oversized test object | `Object does not fit: ...` |
| Supports required | `Supports are required; ...` warning |

### F.6 Calibration record

Fill these in after the first slicer comparison and copy the final numbers into §9:

| Item | Value |
|---|---|
| Bunny size reported by `ObjectToPrintSizing` (not edited) | derived from prefab bounds |
| Bunny filament at 100% infill | `85.36 m` |
| Bunny time at 100% infill at `0.2 mm` / `80 mm/s` | `36 720 s` (`10 h 12 min`) |
| Supports required? | yes |
| Auto support cost compared with None | filament ×1.06, time ×1.12 |
| Required + None quality (expect `55-55`) | `55-55` |
| Slicer estimate at Grid / Normal / Normal | `28.53 m`, `3 h 53 min` |
| Console estimate at Grid / Normal / Normal | `28.5 m`, `3 h 53 min` |
| Tuning factor (slicer / console) | `1.00` filament, `1.00` time |
| Draft time vs Normal | `1 h 22 min` vs `3 h 53 min` |
| Fine time vs Normal | `12 h 29 min` vs `3 h 53 min` |
| Raw quality: Draft / Normal / Fine | `70-75 / 80-85 / 84-89` |
| Raw quality: Slow / Normal / Fast | `84-89 / 80-85 / 70-75` |
| Spool material used | PrusaSlicer default (PLA) |
| Critical locations confirmed | yes |

Expected values with the calibrated bunny seed (`85.36 m`, `36 720 s` at 100%) and `Auto` supports:

| Layer / speed / infill (supports Auto) | Filament | Simulated time | Real time at `120×` | Raw quality |
|---|---:|---:|---:|---:|
| Draft / Fast / None | `17.1 m` | `1 h 22 min` | `0.7 min` | `60–65` |
| Draft / Normal / Grid | `28.5 m` | `2 h 46 min` | `1.4 min` | `70–75` |
| Normal / Fast / Grid | `28.5 m` | `2 h 29 min` | `1.2 min` | `70–75` |
| Normal / Normal / Grid | `28.5 m` | `3 h 53 min` | `1.9 min` | `80–85` |
| Fine / Normal / Gyroid | `32.0 m` | `7 h 10 min` | `3.6 min` | `84–89` |
| Fine / Slow / Gyroid | `32.0 m` | `12 h 29 min` | `6.2 min` | `88–93` |

These are the values to compare against a slicer. Tune `InfillSettings`/`PrintSpeedSettings` globally rather than per object.

### F.7 Bed placement and scale (M2.5)

The plate is `printer_print_plate_location` under `base/print_bed_holder_base/placement_points/print_bed_location/placement_points`. The existing ghost path already has the pieces:

- `ObjectToPrintGhostType.PrinterBedPlacement`
- `ObjectToPrintGhostPrinterBedPlacementDataHolder` with `CornersList`
- per-object `*_ghost_printer_bed_placement_root.prefab`
- `shader_object_to_print_printer_bed_placement_mode.mat`

Configuration:

1. Usable plate area: `PrintBedAnchor.SurfacePosition` / `SurfaceRotation` / `PlateSize` minus `2 mm` margin. Use the plate's local axes, not a world AABB.
2. Scale is uniform, with soft validity limits; the player is not clamped:
   - the largest valid scale keeps the rotated footprint inside the usable plate and the scaled height inside `PrinterMotionBase.BuildVolume`;
   - the smallest valid scale makes the object's largest bounding dimension `2 cm` (exposed `MinimumObjectSizeMeters`);
   - exceeding either limit sets the ghost to the wrong shader state and disables confirm; no fixed ratio limit is authored.
3. Ghost feedback: apply a `MaterialPropertyBlock` to all ghost renderers:
   - `_printer_bed_placement_state = 0` for a valid placement,
   - `_printer_bed_placement_state = 1` for an invalid placement,
   - toggle `_PRINTER_BED_PLACEMENT_STATE_WRONG` with the same state.
   Verify the exact right/wrong look in play mode; the material currently ships with the right state active.
4. Camera orbit pitch is clamped to `15°–75°` above the plate; zoom distance is clamped too. `PrintBedAnchor` supplies the plate plane, size and the front direction used for the initial yaw.
5. Estimates:
   - default console estimates assume centred, yaw `0`, scale `1`;
   - after a confirmed placement, multiply filament and time by `scale³`;
   - rotation does not change volume/estimates in M2.5 (it can affect supports/quality later).
6. Store `localPosition`, `yaw` and `uniformScale` on `PrintJobRequest`; the object boundary corners are derived from the ghost bounds and used for validation.
7. Confirm is disabled while invalid; cancel returns to the console without saving. `Start print` requires a confirmed placement, and placement is required again for the next job.

**UI authoring:** the project owner creates the placement UI. It needs at least:
- a title/state strip (`Placing object`),
- movement/rotation/scale hints using the `ObjectPlace` bindings,
- a valid/invalid description line,
- confirm and cancel hints.

Do not implement the UI before asking; the controller should expose the state the UI needs.

### F.8 Editor profile generator

Yes, most numeric seeds can be generated in edit mode; exact print time still needs a slicer reference.

Editor tools:

- `Tools/Manufacturing/Print Profile Generator` — profile seeds.
- `Tools/Asset/Prefab Icon Generator` — batch icon generator for prefabs under a root folder; writes `<prefab>_icon.png` next to each prefab (existing tool in `Assets/Editor`).
- `Tools/Scene/Detect Placed Items` — lists placed objects/printers/devices/spools in the open scene with Select buttons.

`PrintProfileGenerator` merges metas from:

- the assigned catalog,
- `ObjectToPrintDataHolder` items placed in the open scene (shown as `Scene`),
- all `ObjectToPrintMetaSO` assets only when both sources are empty.

Set the optional coefficients, press `Scan`, inspect the `Scene`/`Catalog` labels, then `Apply successful scans`.

It can:

1. Load each catalog meta's prefab with `PrefabUtility.LoadPrefabContents`.
2. Combine all child `Renderer.bounds` and report the prefab-root-space size. Dimensions are runtime-derived by `ObjectToPrintSizing`; do not write them into the meta.
3. Compute mesh volume from the triangle soup (signed tetrahedra sum). For a watertight mesh this is the solid volume; for open meshes, report the result as approximate.
4. Compute the overhang ratio: triangles whose normal is below the overhang angle (`45°` default) relative to the plate up; set `SupportsRequired` above a configurable threshold and write `SupportOverhangPercent`.
5. Seed 100% infill filament from `volume / (π × (filamentDiameter/2)²)`.
6. Seed 100% infill time from `volume / referenceFlowMm3PerSecond`, with the flow value exposed in the tool settings.
7. Write through `SerializedObject`, mark the asset dirty, and report a per-object summary; support batch over the whole catalog.

Model meshes must have **Read/Write enabled** for volume/overhang. The bunny FBX now has it enabled, so the tool can report volume and overhang. Defaults: `1.75 mm` filament, `12 mm3/s` reference flow, `45°` overhang, `5%` supports threshold.

Limitations:

- Time from volume is a seed, not a slicer result. Calibrate it with one known print, then keep a single global factor.
- Automatic orientation selection is not part of the first version; the player rotates on the bed instead. A future version can try candidate yaw/face rotations and pick the lowest overhang score.
- Default infill stays authored; material is selected by the selected spool and is not inferred.
- The tool must not touch scenes; it only edits meta assets.

Recommended workflow:

1. Run the generator on the catalog.
2. Inspect bounds/supports per object.
3. Print or slice one reference object at Normal/Normal/Grid.
4. Record the slicer values in F.6 and adjust the global flow/factor, not individual objects unless the mesh is unusual.

### F.9 Supports

Supports are a job setting, not free-form geometry:

1. `ObjectToPrintPrintProfile.SupportsRequired` is the geometric truth: the editor generator sets it from the overhang ratio. `SupportOverhangPercent` records the ratio for later tuning.
2. The console shows `SUPPORTS: None / Auto`.
3. Default:
   - required → `Auto`,
   - not required → `None`.
4. Validation and quality:
   - required + `None` → allowed, device-UI warning `This object needs supports.`, raw quality `-25` and capped at `55`;
   - not required + `Auto` → warning `Supports are not mandatory here.`, normal extra cost.
5. Cost (provisional until `SupportOverhangPercent` is wired into the estimate):
   - filament × `1.06`,
   - time × `1.12`.
6. Raw quality: `Auto` subtracts `2` marks for the support contact surfaces; required + `None` applies the severe penalty above.
7. Post-processing: supports must be removed before sale/quality; leaving them caps the final quality. This is M5 work.
8. Visualization staging:
   - M2: words, estimates and validation only;
   - M4: simple generated columns (Y-scaled cubes or thin cylinders) under overhangs, revealed with the print height;
   - M5: support removal with the support cutting tool and the quality cap if left on.

Do not let the player paint supports manually; the setting is a slicer-style toggle, not a modelling tool.


### F.10 M2.5 placement implementation and manual gate

The authored UI lives on `object_place_canvas`. `ObjectPlacementPanel` sits on that canvas and owns only `object_place_reason`; the title and move/rotate/scale/camera/confirm/cancel hints stay visible for the whole state. Reasons are written one per line as `- <reason>`, and the reason block is hidden when the placement is valid. `point_canvas` is not referenced by the placement code: the preceding FreeWalk state disables it, and the console state does not re-enable it.

Controls:

| Input | Action |
|---|---|
| hold LMB + pointer | move the ghost on the plate |
| hold `R` + mouse X | rotate the object yaw |
| hold `S` + mouse Y | uniform scale |
| hold RMB + mouse | orbit the placement camera |
| wheel | zoom the placement camera |
| Enter | confirm a valid placement |
| `Q` | cancel and return to the console |

Movement raycast:

- pointer move uses a mathematical plane at `PrintBedAnchor.SurfacePosition` with normal `anchor.transform.up`; it does not hit a collider. This intentionally allows moving outside the plate so the invalid state can be shown.

Camera:

- orbit target = `PrintBedAnchor.SurfacePosition + anchor.up * (BuildVolume.y * 0.5)`;
- width/depth are centred on the bed, so the same rule applies to future printers;
- fit distance is derived from the full build-volume diagonal and camera FOV, then clamped by `MinDistance` / `MaxDistance`;
- pitch is clamped to `15°–75°`; the initial pitch is `35°`;
- default FOV `50°`, orbit sensitivity `0.3`, zoom sensitivity `0.0004`, fit padding `1.15`.
- zoom limits are volume-relative: minimum `0.55 × build-volume radius`, maximum `5 × build-volume radius`; both are serialized on the placement controller.
- entering and leaving placement use the Cinemachine Brain default blend (`0.55 s`, `EaseInOut`). On confirm/cancel the placement camera drops to inactive priority but stays alive as the blend source until `Brain.IsBlending` ends, then it is destroyed. The wait uses `DeviceFocusController.MaxBlendWait` (`3 s`) as the safety timeout.
- object defaults: yaw `1°/input`, scale sensitivity `0.01`, plate margin `2 mm`, minimum object size `2 cm`.
- all of these are serialized on the `ObjectPlacementController` component under `controllers/object_place`, so they can be tuned in the Inspector without a code change.

Validation reasons (all current reasons are listed):

- `Print bed is unavailable.`
- `Object prefab is unavailable.` / `Placement ghost is unavailable.`
- `Object is too small (minimum 2 cm).`
- `Object is taller than the build volume.`
- `Footprint does not fit on the print plate.`

Plate fit uses the 2D convex hull of the actual ghost mesh vertices, not only the authored base corners, against the intersection of `PlateSize` and `BuildVolume.x/z`, minus `2 mm` on each side. This catches overhanging geometry such as a head or arm beyond the base footprint while avoiding the over-conservative full AABB. Non-readable meshes fall back to the full renderer bounds. The player is not clamped; exceeding a limit only marks the placement wrong and blocks confirm. The ghost uses `_printer_bed_placement_state = 0/1` and toggles `_PRINTER_BED_PLACEMENT_STATE_WRONG`.

Job flow:

1. Select the object and settings in the console.
2. Press `Place on bed`; the placement camera focuses the plate and the ghost starts at the previous confirmed placement, or at the plate centre with yaw `0` and scale `1`.
3. Confirm a valid placement; the console returns with the placement stored on the draft job. Cancel keeps the previous placement.
4. `Start print` is disabled until a valid placement exists. A confirmed placement survives console close/reopen while the same draft is selected, and is cleared when the object/printer/device changes or after a job is queued.
5. Confirmed estimates multiply filament and time by `scale³`; rotation changes fit only.

Manual gate to record:

| Check | Result |
|---|---|
| Ghost starts centred on first entry | Pass |
| Move to each plate corner and beyond; wrong state and blocked confirm | Pass |
| Minimum 2 cm and maximum plate/height scales; wrong state and blocked confirm | Pass |
| Estimates match the cube rule at min / 1.0 / max scale | Pass |
| Cancel restores the previous placement; confirm survives console close/reopen | Pass |
| `object_place_reason` lists every current reason with `- ` prefix | Pass |
| Camera orbit stays locked on the volume centre; pitch/zoom limits hold | Pass |
| Confirm/cancel return blend matches the entry blend | Pass |
| `point_canvas` stays hidden during `GameStateObjectPlace` | Pass |
