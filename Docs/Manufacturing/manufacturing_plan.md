# Manufacturing Plan — Device-Controlled FFF Printing

**Status:** In progress — M4 implemented; manual gate pending
**Last updated:** 2026-11-20
**Scope:** Laptop / PC / Smartphone / Tablet controlling one or more FFF printers. Prusa MK4S is the first model; rectangular XYZ printers, including CoreXY, are supported by the shared architecture.
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

> **HIGH PRIORITY — printer/enclosure features must stay printer-agnostic**
>
> Do not hardcode model names, MK4S-specific GameObject names, fixed spool sides, or per-model conditionals into shared runtime code. Use serialized `PartLocation` references, `SpoolSlotDefinition` data, role-based docking sources/targets, `PrinterMotionBase` subclasses, authored wear data, and per-prefab critical/reference lists.
>
> If a requested printer/enclosure feature cannot be expressed through those contracts, generalize the shared contract first rather than adding a model-specific branch. New rectangular XYZ models should normally be added through prefabs/data; CoreXY uses `CoreXYPrinterMotion`, independent XYZ uses `CartesianPrinterMotion`. Delta/polar volumes and multi-material printing are out of scope until explicitly approved.
>
> **Final documentation gate:** at the end of the final milestone, review all printer/enclosure and printable-object work against this rule and write the full agent-facing setup guides for adding future printers and objects. Start from [`new_printer_agent_setup_reminder.md`](new_printer_agent_setup_reminder.md) and [`new_object_print_setup_reminder.md`](new_object_print_setup_reminder.md).

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
- Delta printer prefabs and non-rectangular volumes; CoreXY motion support exists, but no CoreXY model prefab is authored yet.

---

## 3. Approved defaults

| Decision | Value |
|---|---|
| Console presentation | Screen-space overlay, one generic UI for all devices |
| Camera focus | Smooth Cinemachine blend to a dedicated screen-framing camera |
| Print time | Simulated time = real time × configurable multiplier; default `1×` |
| Hard stop result | Partial print stays on the bed until removed |
| Infill choices | `None`, `Grid`, `Gyroid`; affects time/filament only |
| Print settings | Layer height (`Draft 0.28` / `Normal 0.20` / `Fine 0.12` mm), speed (`Slow 80` / `Normal 160` / `Fast 240` mm/s), supports (`None` / `Auto`) |
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
- `Start print`, `Place on bed`, `Pause`/`Resume`, `Hard stop`/`Confirm stop`, `Calibrate`, `Connect printer`, `Disconnect`, `Screen off`, and `Quit`. Completed and partial prints are removed manually by hovering them; there is no `Clear bed` action.

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
7. When finished, the object is fully revealed on the bed. Once cooling reaches ambient + tolerance and the head is parked at home, the object can be taken off the bed and placed.

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
6. No new job can start until the partial print is removed by taking it off the bed and placing it.

### 4.8 Leaving the console

- `Q` quits the console (`Quit`).
- The console stops accepting pointer input and stays visible on the device screen; the camera blends back to the player, movement/look is restored, and the cursor is hidden/locked.
- Printers continue running while the player walks away.
- The screen-off state is preserved in the world.

### 4.9 Calibration

1. With an idle printer and a clear bed, click `Calibrate` in the console.
2. The head moves smoothly from its current position to `PrinterMotionBase.CalibrationHomePrintPosition` (global X/Z, optional calibration height), lifts by `CalibrationLiftHeightMeters` (default `3 cm`; clamped so the target does not exceed `BuildVolume.y`), then for every point of an `N × N` grid (`CalibrationGridSize`, default `5`):
   - moves over the point at the lifted height,
   - lowers to height `0` and touches the plate,
   - waits `CalibrationPointDwellSeconds`,
   - lifts again and advances.
3. The grid is inset by `CalibrationPaddingMeters`; movement speed is `CalibrationMoveSpeedMetersPerSecond`.
4. Calibration returns smoothly to the global `HomePositionNormalized` and marks the printer calibrated for the next print. `Hard stop` cancels it and also moves to the global home. Calibration travel and dwell follow the printer's `PrintSpeedMultiplier`; use `1×` when visually inspecting the grid.
5. Calibration does not consume a job and is blocked while a print is queued, partial or finished.
6. Calibration quality is evaluated when printing ends. A calibrated printer applies `1.0×`; an uncalibrated printer applies a random `0.7–1.0×` at the end. Calibration is consumed by a job that actually printed: when that job finalizes, the printer returns to uncalibrated for the following print. A hard stop during preheat does not consume it. Calibration is still optional and never blocks starting or resuming. Changing a **calibration-sensitive** part (nozzle, heater, extruder thermistor, print bed/thermistor/plate, axis belts, rotors or bearings) invalidates calibration in both idle and active states. Spool, battery closet/adapter and enclosure-accessory changes do not invalidate calibration. A job whose calibration is invalidated finishes with the random factor; the console warning stays visible until the finished/partial print is removed.
6. `HomePositionNormalized` lives on `PrinterMotionBase` and is drawn as an orange gizmo. It uses the printer-front frame: X `0` = left, `1` = right; Z `0` = back, `1` = front; the default is top-left with the build volume 3/4 forward. `CartesianPrinterMotion` converts the bed-moving Z role to print space internally, so `AxisBinding.Invert` never appears in authored or player-facing values. Enable `UseCustomHomePosition` to reveal the `0..1` X/Y/Z sliders (`HomeX`, `HomeY`, `HomeZ`). Y maps from the plate to the build-volume top. This is preferred over manually posing a head transform on the prefab because it stays valid across axis mappings, printer variants and build volumes. A child marker transform can feed the same normalized value later if visual authoring is needed.
7. Calibration has an optional height-only override: enable `UseCustomCalibrationHomeHeight` and set `CalibrationHomeY`. X/Z always follow the global home; after calibration the head returns to the full global home. The height override is drawn as a green gizmo and does not change spawn, pause, hard-stop or post-print home.

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
- `PrinterEnclosure.Doors` auto-finds child `Door` components. All doors must be closed and settled before a print starts or resumes; an animating door counts as not closed.
- Door interactions are available while idle, paused, cooling or finished. During preheating, printing or calibration they return a short interlock label (`Door (Heating)`, `Door (Printing)`, `Door (Calibrating)`) instead of opening. If a door is opened by another path during preheating/printing, the printer pauses automatically.
- `PrintJobValidator`, `Printer.TryResume` and `Printer.TryStartPendingJob` enforce the closed-door requirement. The console validation list and printer status show the warning; `TryStartPendingJob` keeps the queued job until the doors close.
- Doors implement `IPrinterBusyInteraction`, so the busy-printer hover gate does not suppress their interaction prompt. The door itself explains whether it is locked, interlocked or ready.
- `PrinterTemperatureModel` reads chamber temperature and slows cooling when enclosed.
- `PrintQualityEstimator` applies an enclosure bonus / no-enclosure penalty; raw quality caps at `90`.
- Enclosure parts join the printer wear scan, so fans, filters, thermometers and fire-suppression parts wear during a job.
- Enclosure inspection keeps the enclosure interaction list and treats the printer as one part for integrity/removal. Wear inspection includes the printer's internal parts and hides the printer root so only the wear ghosts remain visible.
- Installing a printer in the enclosure docks its accessories:
  - installed printer spools move to matching enclosure spool sockets; a printer can expose up to four selectable slots;
  - the install is blocked when the enclosure does not have enough free spool sockets;
  - installed accessories are matched by role: `Printer.DockingSources` roles find `PrinterEnclosure.DockingTargets` roles. The MK4S wires `battery_closet` and `battery_closet_adapter`;
  - free parts are matched separately by role: `Printer.FreePartDockingSources` roles find `PrinterEnclosure.FreePartDockingTargets` transforms. The MK4S wires `battery_closet_plug` and `wall_socket_plug`;
  - the printer `spool_holder` is hidden when spools are docked and restored on removal.
- Printer spool slots are `SpoolSlotDefinition` data (`Id`, `Label`, `Location`). The console exposes 0/1 spools without a selector and up to four with a dynamic selector; only one selected slot feeds a job.
- Enclosure spool sockets are `PrinterEnclosureSpoolSocket` data. A socket may name a specific printer slot id or accept any free slot.
- The printer, spool, battery closet and adapter sockets are `PartLocation`s because they are installed parts. The two plug reset points remain plain transforms registered through `ItemPartsToLocationsList`, because plugs are free parts.
- Docking updates `PrinterEnvironment` directly, so enclosure thermal/quality/wear behavior applies after a runtime install rather than relying on awake-time renderer-bounds detection.
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

Implementations:

```
CartesianPrinterMotion : PrinterMotionBase
  - AxisBinding HorizontalX
  - AxisBinding HorizontalZ
  - AxisBinding VerticalY

CoreXYPrinterMotion : PrinterMotionBase
  - Transform Carriage
  - Vector3 HorizontalXLocal / HorizontalZLocal
  - float HorizontalXMinLocal / MaxLocal
  - float HorizontalZMinLocal / MaxLocal
  - AxisBinding VerticalY
```

`CartesianPrinterMotion` covers independent-axis Cartesian printers. `CoreXYPrinterMotion` composes print-space X and Z onto one carriage; belt/motor dynamics are not simulated. Both use the same print-space job, placement, calibration and bed-anchor contracts.

`AxisBinding` contains:

- `Transform Node`
- `Vector3 LocalAxis`
- `float MinLocal`
- `float MaxLocal`
- `bool Invert`
- `float Speed`

This supports:

- bedslinger printers;
- fixed-bed Cartesian printers;
- CoreXY printers through the coupled-carriage implementation;
- future XYZ printers with a different build volume or axis wiring.

The current scope is rectangular XYZ print volumes. Delta, polar and other non-rectangular volumes are deliberately out of scope. The job/visual layer only asks for a print-space position; new motion classes map it without touching `Printer`, placement or validation.

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
- Rotation: local +Y is world up and local +Z points to the printer front. The placement camera and visual home frame use this front direction.
- Keep the anchor under the bed-moving axis (the bed carriage on a bedslinger) so `CartesianPrinterMotion` can detect a bed-moving horizontal Z role.
- The printed object is parented to this anchor, so it moves with the bed.
- At print start, compute the object's local bottom offset and center it on the anchor.

This avoids fighting the prefab's diagonal rotations and makes future printer prefabs easier to support.

**Universality audit (printer/enclosure)**

- `PrinterMotionBase` / `CartesianPrinterMotion` / `CoreXYPrinterMotion` own node wiring, axis signs and travel limits. Job, visual and enclosure code call only `ApplyNozzlePosition`, `HomePrintPosition`, `BuildVolume` and `BedAnchor`.
- `HomePositionNormalized` is exposed in the printer-front frame: X `0` = left, `1` = right; Z `0` = back, `1` = front; Y `0` = plate, `1` = top. `CartesianPrinterMotion` detects a bed-moving Z role from the bed-anchor hierarchy and converts it to print space internally. `AxisBinding.Invert` is never authored into home or placement values.
- The bed anchor's local +Z is the printer-front direction used by placement framing and the home frame. Rotate the anchor if the model front differs, and keep it under the bed-moving axis for role detection.
- `CartesianPrinterMotion` axis labels are role-agnostic; serialized tooltips may use MK4S paths as examples only. A new independent-axis rectangular printer is a prefab/calibration job, not a new runtime branch.
- `CoreXYPrinterMotion` assumes a fixed bed; a moving-bed variant overrides `HorizontalZMovesBed`.
- Enclosure behavior is driven by `PrinterEnclosure` data: doors, spool sockets, docking roles, free-part reset points and critical parts. `PrinterEnvironment` receives the docked state at runtime; shared code does not branch on model names or transforms.
- Runtime part checks use serialized `PartLocation` references and `Printer.CriticalPartLocations` / `PrinterEnclosure.CriticalPartLocations`. The only name-based binding helper is the editor-only `Auto-Assign Critical Parts` context menu (`#if UNITY_EDITOR`); remaining name switches are display labels with a generic underscore fallback and never drive validation or state.
- Shelf storage is independent of printer ownership: `Printer.FindFor` stops at the physical printer/enclosure hierarchy. Enclosure surfaces still resolve the printer; items placed on them keep their own hover/take.
- Calibration, quality and job state are printer-generic; enclosure quality is read through `Printer.HasEnclosure` / `PrinterEnvironment.HasEnclosure`, and each job latches `StartedWithEnclosure` at queue time. The `+5` / `-9` enclosure quality values are global balance constants; future enclosure tiers should expose their own values on `PrinterEnvironment`/`PrinterEnclosure` rather than add a printer-name branch.
- A new printer that needs different geometry or coupled axes should add a `PrinterMotionBase` subclass or prefab data, not a special case in shared job, console or enclosure code.

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
  - PartLocation NozzleWearLocation
  - PartLocation HeaterWearLocation
  - PartLocation PrintBedWearLocation
  - PartLocation[] CalibrationSensitiveLocations
  - List<SpoolSlotDefinition> SpoolSlots
  - List<PrinterDockingSource> DockingSources
  - List<PrinterFreePartDockingSource> FreePartDockingSources
  - SpoolFilamentState ActiveSpool
  - float PrintSpeedMultiplier
  - float SafeServiceTemperatureC
  - PartLocation[] ServiceBlockedDuringPrint
  - int CalibrationGridSize
  - bool IsCalibrated
  - bool IsPausedServiceReady
  - bool CanOpenInteractions
  - bool CanOpenEnclosureDoors
  - float HeatingProgress01 / CoolingProgress01
  - Device BoundDevice
  - string StatusSummary
  - PrintedObjectPresenter PrintedObject
  - bool TryReleasePrintedObject(GameObject)

PrinterJob
  - PrintJobRequest Request (object, settings, spool, placement)
  - float ElapsedSeconds
  - float ConsumedFilamentMeters
  - int PauseCount / float PauseQualityPenalty
  - float CalibrationQualityMultiplier / FinalQuality
  - float QualityMin / QualityMax (after pause penalty and calibration)
  - float QualityWithoutEnclosureMin / QualityWithoutEnclosureMax
  - bool StartedWithEnclosure / bool EnclosureInterrupted
  - bool IsPartial / bool WasHardStopped / bool UsesApplied
  - float Progress01 / RemainingSeconds / RemainingFilamentMeters

PrintedObjectDataHolder
  - Printer / PrinterJob
  - float Quality (0..90; -1 for partial)
  - bool IsPartial / IsCompleted / IsFinalized / IsReleased
  - bool IsOnPrintBed

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

The printer is a self-contained `MonoBehaviour` that ticks every frame, regardless of whether a device is open or bound. `PrintedObjectPresenter`, `PrintRevealController`, `PrintedSupportPresenter` and `PrinterMotionSimulator` are printer-agnostic helpers; they use only the bed anchor, motion contract, placement and object prefab.

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

#### Enclosure/printer assembly inspection

When a printer is installed in a `PrinterEnclosure`:

- `PartsAssembly.FindEnclosingPrinterEnclosure` resolves the enclosure assembly from the printer's holding `PartLocation`.
- `FFFInteractions` on the printer redirects its interaction list to the enclosure, so only the enclosure's `Parts integrity`, `Parts wear` and `Parts remove` options are shown.
- `PartHoverIntegrity` resolves any internal printer part to the printer root, so the printer reads as one part.
- `PartHoverWear` and `PartsWearStateSetup` include the installed printer's internal parts as wear entries. The printer root is hidden during wear inspection so its frame does not obscure those ghosts.
- `PartHoverRemove` resolves internal printer hits to the printer root and removes the whole printer through its enclosure part location.
- Existing installation/missing/wear ghosts remain used for the parts that are actually shown.

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

Printers cannot be sold while they are running a job or bound to a device:

- busy → `Wrong`, `Printer is on its job`, with the explanation in `item_hover_sell_description`;
- bound but idle → `Wrong`, `Connected to a device`, with `Disconnect this printer from its device before selling.`;
- unbound and idle → normal sell rules.

The same printer check applies when the sell target is an enclosure containing that printer. The wrong state is applied through the existing selling ghost `_selling_state` blackboard.

#### Combined free-part / wearable items

Some parts are both a free part and a regular item (e.g. `Prusa_MK4S_printer_battery_closet_adapter_root` has `ItemFreePartDataHolder`, `ItemDataHolder`, and `ItemWearState`). These rules keep the flows consistent:

- Regular free parts live directly under their `free_parts_reset_points` location and are moved as their own item root.
- A wearable free part is installed in a `PartLocation` carrier. `ItemPlacementUtility.GetFreePartMovableTransform` resolves the carrier as the movable transform, so the location collider and the installed part travel together.
- A free part nested under a core item stays restricted to the core item's parent surface.
- A standalone item which also exposes `ItemFreePartDataHolder` has no core item above it and can be placed freely.
- Placement updates every `ISurfacePlaceable` on the item root, so both free-part and item surface states agree.
- Taking a wearable free part moves its `PartLocation` carrier; it stays installed in that carrier. Install mode is not offered while it is already installed. The carrier is deactivated while carried so its location collider cannot block placement rays.
- Taking a core item resets mapped free parts: regular parts return to their `ItemPartsToLocationsList` location, and wearable parts still installed in their carrier and still attached to the core item move the `PartLocation` carrier to it. A removed or separately placed wearable part is left where the player put it, so it cannot visually snap into an installed-looking socket while still counting as removed. Old shelf registrations and placement ghosts are cleared.
- `PartLocation.Remove()` and `PartLocation.Install()` reset a wearable part's carrier to its authored `ItemPartsToLocationsList` location, so the assembly socket is never left displaced.
- Entering installation hides both the regular placement ghost and the free-part placement ghost.
- A free part with its own `ItemDataHolder` that is installed under a `PrinterEnclosure` is a fixed installed part, not a loose take. The battery closet adapter uses this rule while the printer is docked; it becomes a free part again once removed or after the printer leaves the enclosure.
- Hovering a detached combined part still offers wear inspection through its `ItemDataHolder`.
- Printer docking matches free-part roles to enclosure reset targets. It moves those parts, registers them on the enclosure item, and clears those runtime mappings on undock before restoring the printer’s own free-part mappings.

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
| `Cooling` | temperatures near ambient + head parked, complete | `Finished` |
| `Cooling` | temperatures near ambient + head parked, partial | `Idle` (partial print remains) |
| `Cooling` | temperatures near ambient + head parked, preheat stop | `Idle` (no print) |
| `Finished` | print taken from the bed | `Idle` |
| `Idle` | calibration requested | `Calibrating` |
| `Calibrating` | grid complete | `Idle` |
| `Calibrating` | hard stop | `Idle` |
| `Idle` / `Finished` | any part at 0% / missing critical part | `Maintenance` |
| `Maintenance` | parts repaired | `Idle` |

Rules:

- `Finished` means a completed print is still on the bed and the head is parked.
- `Idle` has no completed print; a partial print may still be on the bed.
- On spawn, after finishing a print, after a hard stop and after calibration cancel/return, move to `PrinterMotionBase.HomePositionNormalized`. Calibration may start/lift from its optional height override, but always returns to global home. M4 smooths the post-print move; there is no separate service/park position.
- Completion and partial finalization happen only after cooling reaches ambient + tolerance and the head reaches home. Colliders/removal are enabled only then.
- Pause parks the head at home. Resume returns smoothly to the current print path.
- A hard-stopped partial print sets `HasPartialPrint = true`; the printer returns to `Idle` after cooling + parking but rejects new jobs until the partial print is manually removed.
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
- Printer has all critical printer parts installed.
- When the printer is installed in a `PrinterEnclosure`, the enclosure's required parts are installed as well, and all enclosure doors are closed and settled.
- No installed part is completely worn out (`0%`); any installed part at `0%` blocks.
- Every installed part at `20%` or less warns; critical parts additionally warn between `20%` and `40%`.
- All installed part conditions feed the raw-quality hardware penalty.
- The selected spool slot is occupied by a filament spool.
- Remaining filament on the selected spool is enough.
- Object fits the build volume.
- Print plate is present.
- No job is already active or queued on that printer.
- The printer is not calibrating.
- An uncalibrated printer is allowed but warns: `Printer is not calibrated; final quality may be 70–100% of the estimate.`
- The object has a confirmed placement inside the usable plate area and within the build volume.
- Supports match the object: a model that needs supports can start with `None`, but receives a severe raw-quality penalty and a warning. `Auto` on a model that does not need it is allowed with a "not mandatory" warning and extra cost.

All part locations are scanned, not only the critical list. Critical parts are the only ones that block when missing; a completely worn non-critical part still blocks because the policy is “any part at 0% blocks”. Spools are the exception: they are consumables and are excluded from the generic scan, so only the selected spool is validated. An empty or worn spool on the unselected side does not block the job.

For a docked printer, the scan covers both `Printer.Assembly` and the enclosing `PrinterEnclosure` assembly. The installed printer at `printer_location` and the selected spool are skipped as generic parts because they are the job target and the separately validated consumable. Enclosure wear also feeds the raw-quality hardware penalty.

Critical parts are serialized as **`PartLocation` references** on the printer (`Printer.CriticalPartLocations`) and on the enclosure (`PrinterEnclosure.CriticalPartLocations`), not names. The print plate is `Printer.PrintPlateLocation`. References survive renames and support prefab variants; critical-part names are only used by the editor-only auto-assign helper (`#if UNITY_EDITOR`), never for runtime validation. `ItemMetaSO.ItemTypes` is deliberately not used because it stores only broad paths (`FFF`, `Printer`, `Part`), not leaf types such as nozzle or print plate.

Balanced default printer critical set:

- extruder nozzle, heater, thermistor, fan, filament bearing,
- print bed and print bed thermistor (print plate is separate),
- X/Y belts, X/Y rotors, Z left/right rotors.

Default enclosure critical set:

- battery closet and battery closet adapter,
- three fire-suppression tubes,
- thermometer,
- left/right HEPA filter fans and HEPA filter trap.

The enclosure spool is not in this list because `CheckSpool` validates only the selected side; `printer_location` is excluded because the docked printer itself is the job target. Add or remove references from `PrinterEnclosure.CriticalPartLocations` if a specific enclosure part should be optional.

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
- Hover/console state progress uses the temperatures captured at the state transition: `Heating 0–100%` and `Cooling 0–100%` follow the actual interval; printing and paused states use job progress.
- An enclosure is detected through `PrinterEnclosure` bounds. It slows cooling, lets the chamber hold heat and improves the quality estimate. Draft, particulate and warp effects remain M5.
- Preheat completes when both nozzle and bed are within tolerance (default ±2 °C).
- In M3, a preheat timeout logs a warning and enters `Fault`; full thermal-failure gameplay is M5.
- Raw quality is capped at `90` even with an enclosure. The post-process state can polish the finished object later.
- Calibration quality is finalized when printing completes. A calibrated printer uses `1.0×`; an uncalibrated printer uses a random `0.7–1.0×`. Calibration is consumed by a job that actually printed, so the next job starts uncalibrated until the player calibrates again; a preheat-only hard stop keeps it. A calibration-sensitive part change invalidates calibration in idle or active states, so the job finishes uncalibrated. Spool/battery/adapter/enclosure-accessory changes do not. The warning remains in the console until the bed is cleared.

### 6.6 Wear and filament

Use the existing wear system and the correct delta types:

| Part | Delta | Notes |
|---|---|---|
| Nozzle / filament bearing | `WearDeltaFilamentTemperatureAbrasiveness` | filament length, nozzle temp, spool abrasiveness |
| Heater / thermistors / enclosure fire tube / thermometer | `WearDeltaTimeTemperature` | simulated seconds at the temperature source authored on the wear logic |
| Spool | `WearDeltaFilament` | consumed metres / spool length |
| Axes / belts / rotors / bearings | `WearDeltaDistanceTemperature` | distance approximated from simulated time until M4 paths exist |
| Extruder fan / HEPA fan | `WearDeltaTimeRPM` | simulated seconds at `FanRPM` |
| HEPA filter trap | `WearDeltaTimeFumes` | simulated seconds at `FumesDensity` |
| Battery closet adapter | `WearDeltaTime` | powered/print time |
| Print plate | `WearDeltaUses` | one use per completed/partial print |
| Print bed | `WearDeltaUsesHardness` | one use per completed/partial print, scaled by spool hardness |

Rules:

- `ItemWearLogicTimeTemperatureSO.TemperatureSource` selects `Nozzle`, `Bed`, `Chamber` or `EnclosureAware`; `EnclosureAware` uses chamber while the part is under a `PrinterEnclosure` and bed elsewhere. Runtime no longer infers temperature channels from GameObject names.
- Batch wear application about once per simulated second, not every frame.
- Use simulated process time, not wall-clock time.
- Enclosure parts are scanned from `PrinterEnclosure` as well as the printer assembly.
- The selected spool rotates by `consumedMetres / (2π × EffectiveRadiusMeters) × 360°`; its visible child is chosen by nearest `SpoolWearLevelsToGameObjectsDictionary` threshold.
- Hover wear panels read the live `ItemWearState.CurrentCondition` and refresh periodically while hovering; wear components are resolved on the item/part hierarchy, not only the root object.
- `ItemWearState.ApplyWear` calls `CalculateWearByRatio`, so the authored `WearRatio` is honored. Existing assets use `1`, so the change does not alter their current values.
- Spool remaining filament:

```
remainingMeters = wear.CurrentCondition / 100f * spoolData.FilamentLengthSpoolMeters;
```

`wear` and `spoolData` belong to the spool in the selected `SpoolSlotDefinition`.

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

These are one-off condition points, not `WearDelta` values. The nozzle, heater, bed and plate targets are serialized `PartLocation` references on `Printer` (`NozzleWearLocation`, `HeaterWearLocation`, `PrintBedWearLocation`, `PrintPlateLocation`), so a new printer model can rename its hierarchy without changing code.

### 6.7 Print visuals

#### Object instantiation

- Instantiate the real object prefab under `PrintBedAnchor`; do not use the legacy printing ghost.
- Place it with the stored M2.5 local position, yaw and uniform scale.
- Use the selected spool's `PrintingMaterial` while printing; it exposes `_reveal_height`.
- Add a runtime `PrintedObjectDataHolder` for quality, partial/completed state and bed ownership.
- Disable colliders and physics during printing. The object remains on the `object_to_print` layer for later hover/removal.
- Keep live printing uncapped. The spool's printing shader supplies both the reveal cutoff and backface lighting; no cap is generated per layer.
- When a pause settles (cooled and head parked), build a sliced, capped surface with the spool's regular `FinalMaterial`. On resume, remove that surface and restore the printing shader and reveal.
- On hard-stop finalization, build the same sliced, capped surface and use it as both the partial visual and partial collider. Completed prints have no cap.
- Partial prints keep the sliced, capped surface and final material after removal and placement.
- Do not use the `ObjectToPrintGhostPrintingDataHolder` path; the real prefab is the single source of visual/collider truth.

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
- Do not build a cap during live printing; the printing shader's backface lighting is the intended active-layer look.
- A settled pause or finalized partial print gets a sliced, capped surface built in object-root local space so it follows the bed.
- The sliced surface and collider are display/gameplay approximations, not a real slicer.

#### Motion

`PrinterMotionSimulator` generates cheap procedural print-space positions:

- Layer count from model height / layer height.
- Precompute conservative XZ bounds per layer from the captured mesh once at job start; empty bands inherit the nearest populated layer.
- Transform those bounds through the actual printed-object-to-bed matrix (bed-anchor inverse x object local-to-world), so position, yaw, scale and bed-anchor rotation are handled together instead of reconstructed from placement fields.
- Each layer follows a small Lissajous path inside its layer bounds, not over the whole plate or the full object box on every layer.
- Each job selects a seeded motion profile: randomized Lissajous frequencies/phases, a secondary harmonic and low-frequency Perlin drift. The path stays inside the layer bounds and is deterministic for that job.
- Use a continuous global layer parameter so X/Z do not reset phase at layer boundaries; interpolate adjacent layer bounds for a smooth silhouette transition.
- Add a continuous Z-hop bump that is zero at both layer boundaries.
- Visual pace follows `EstimatedSeconds`, which already includes the console SPEED preset (`80 / 160 / 240`); do not scale it again in the path.
- Per-frame work is O(1): no mesh reads, capping or allocations during `Tick`.
- The printer applies the multiplier to real frame delta, with a `1 s` real-time clamp for hitches. Do not clamp the real delta to `0.1 s` before multiplying: that silently caps high-speed presets when the frame rate drops.
- `ApplyNozzlePosition` takes X/Z in bed-local metres and Y as nozzle height above the bed.
- MK4S implements X on the extruder, Z on the bed carriage, and Y on the gantry. A dropping-bed printer implements the same roles differently.
- Do not attempt real perimeter/infill geometry.
- Use a seeded pattern so the motion is deterministic per job.
- Respect the rectangular build volume / soft limits.
- Axis `Invert` flags are part of the print-space calibration: the MK4S horizontal X uses `Invert = 1` because its carriage parent maps local X to world -X, and horizontal Z uses `Invert = 1` because the bed moves the opposite way from the nozzle in print space. Keep the path bounds and motion axes on the same bed-local frame.
- Pause and hard stop both move the head smoothly to `HomePositionNormalized`. The park move uses real frame time, not `PrintSpeedMultiplier`, so `120×`/`240×` do not teleport the head home; resuming mid-park hands the current position to the simulator instead of snapping to home.
- After cooling, normal completion moves the head smoothly to `HomePositionNormalized`; the print becomes removable only when both cooling and parking finish. Calibration may start/lift from `CalibrationHomePrintPosition` (X/Z global, optional height). `Home()` remains the mechanical axis-home primitive for debug/mechanical use only.

**Path diversity model**

- The job seed mixes the object id, printer hash and a random per-job salt, so the same object does not repeat the exact same path on every print.
- Primary X/Z Lissajous frequencies are randomized in `1.3–2.7` and `2.0–3.8` cycles per layer, with random phases.
- A secondary harmonic at `35–65%` of the primary frequency is mixed at `0.10–0.32` weight to break up the simple figure-eight.
- Low-frequency Perlin drift (`0.29–0.37` per layer) shifts phase and position gradually across the object.
- All terms are continuous in the global layer parameter; the result is clamped to the current layer bounds. Pause/resume keeps the same seed and path.
- This is cosmetic path variety, not a real slicer perimeter/infill path.

The motion layer only talks to `PrinterMotionBase`; it never touches the MK4S transforms directly.

#### Home, service and park

- `Home()` is the mechanical axis-home primitive: `AxisBinding.RestoreHome()` returns each node to its authored local position. It is not a configurable upper-center point and is not the gameplay start position.
- `HomePositionNormalized` is the global gameplay home used for spawn, pause/service, post-print parking and hard-stop recovery.
- `BeginHomeMove` / `TickHomeMove` run on real time at `CalibrationMoveSpeedMetersPerSecond`; the simulation preset never shortens the visible park. Cancelling calibration uses the same smooth park before returning to `Idle`.
- Resuming after a reheat reuses the existing printed object, reveal state and layer bounds; only the motion simulator's return-to-path is re-armed. `BeginJob` runs only when the presenter has no printed object.
- Calibration uses the optional `CalibrationHomePositionNormalized` only for its start/lift height; X/Z and the return position always use the global home.
- `UseCustomHomePosition` is off by default; the default is the visual top-left / 3/4-forward home. When on, `HomeX/Y/Z` use the same printer-front frame (X `0` = left, Z `0` = back). `CartesianPrinterMotion` detects whether the horizontal Z role moves the bed and applies the print-space conversion internally; do not compensate for `AxisBinding.Invert` in the home values.
- X/Z are normalized across the build volume and Y is normalized height. On a bed-slinger the moving bed is one of those axes, so the same sliders define the bed home; fixed beds ignore that axis.
- Do not add separate service/park/bed-home positions unless a future printer proves it needs an independent pose.

#### Spool

- Rotation arrives in M3: rotate the **spool root**, not the mesh child:
  `degrees = consumedMetres / (2π × EffectiveRadiusMeters) × 360`.
- The MK4S root axis is local `-Y`; the default is `RotationAxis = Vector3.up` with `InvertRotation = true`.
- Both `spool_location_1` and `spool_location_2` share the same authored holder rotation, so the same root axis/direction applies to both.
- The rotation uses simulated consumption, so it follows `PrintSpeedMultiplier`.
- The visible filament child uses the upper authored threshold: `91..100 → 100`, `81..90 → 90`, … `1..10 → 10`, and only `0` shows the empty child. This keeps the spool visually fuller for longer.
- `SpoolFilamentState` reads the installed spool's live `ItemWearState.CurrentCondition`; the visible child and remaining metres are derived from that same value. If a spool has unexpected wear logic, consumption falls back to direct condition loss so the wear state still moves.
- M4 applies the spool-bound printing shader (`_reveal_height`, backface lighting) during live printing.
- A settled pause shows a sliced capped surface with the spool's regular `FinalMaterial`; resuming removes it and restores the printing shader/reveal.
- Hard-stopped partial prints show the same capped final-material surface after cooling and parking.
- Completed prints have no cap and use the regular final material.
- Partial prints keep the capped final-material surface after removal and placement.
- If the vertical reveal looks wrong, verify the renderer-pivot/cutoff formula before changing artwork.

#### Completion and removal

- On normal completion:
  - Fully reveal the object.
  - Restore the spool's regular `FinalMaterial`.
  - Keep generated supports attached; M5 adds support removal.
  - Wait for cooling and head parking at `PrinterMotionBase.HomePositionNormalized`.
  - After both complete, enable the original colliders/surface placement and mark it as a completed print.
- On hard stop:
  - Freeze the reveal and supports at the partial height.
  - Keep the printing material during cooling.
  - Wait for cooling and head parking, then build the sliced/capped partial visual and collider.
  - Mark it as a partial print. Partial prints are removable, placed and sellable, but give no income.
- Removal:
  - Hover a completed or partial print and enter `GameStateItemPlace` directly, with no `Cancel` option.
  - Release the object from the printer before entering placement; it becomes a normal item.
  - Removing the object clears the printer's bed slot. The head is already at `HomePositionNormalized`, so removal needs no extra head motion.
  - There is no `Clear bed` action in M4.
  - Do not allow selling while the print is on the bed. Take it off and place it first.
  - Supports stay attached to the finished object until M5 adds a removal interaction.
  - Partial prints are not resumed.

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
- Job-card controls: `Pause`/`Resume`, `Hard stop`/`Confirm stop` and `Calibrate`. Completed and partial prints are removed by hovering the object; there is no `Clear bed` button.
- `SIM SPEED` presets `1×`, `4×`, `64×`, `120×`, `240×`; the selected value is stored on the active printer. When the multiplier is above `1×`, the estimate line also shows the real elapsed time in parentheses, e.g. `3 h 53 min (real 58 s)`.
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
- While a printer is busy, FreeWalk hover shows the printer name in `item_hover_info_text` and fills the separate `free_walk_canvas/vertical_stack` fields:
  - `printer_state_progress_text`
  - `printer_nozzle_temperature_text`
  - `printer_bed_temperature_text`
  - `printer_spool_meters_text`
  - `printer_pause_penalty_text`
- The five fields are only active for a busy printer or its enclosure. Empty spool/pause fields are hidden; normal take/inspect prompts return when the printer is idle.
- `printer_state_progress_text` uses `Heating 0–100%` / `Cooling 0–100%` from the interval start temperatures. Printing and paused states use job progress.
- `ItemHoverInfo` refreshes the five fields while the same printer collider stays hovered.
- `Printer.FindFor` resolves only the physical printer/enclosure hierarchy. Surface placement is storage, so items placed on an enclosure shelf keep their own hover/take even while the printer is busy; hovering the enclosure surface itself still resolves the printer and shows its status.
- While the printer is running or calibrating, `ItemHoverInteract` suppresses its prompt so it does not duplicate the status; door interactables are exempt so they can show their interlock reason.
- Once the printer is `Paused`, cooled below `SafeServiceTemperatureC` and the head is parked, `Printer.IsPausedServiceReady` / `CanOpenInteractions` become true: the name and five status fields remain visible, and `interact_root` also appears with `Parts wear`, `Parts integrity` and `Parts remove`. Individual parts can still be refused by `ServiceBlockedDuringPrint`.
- Resuming or hard-stopping the job closes the interaction list again.
- In `ItemsSell`, a busy or bound printer/enclosure is `Wrong`, uses the selling ghost, cannot be sold, and puts the concise reason in `item_hover_sell_description`. Busy takes priority over connected.
- Enclosure detection uses the same `Printer.FindFor` path: `PrinterEnclosure` binds the printer whose `PrinterEnvironment` is inside it.

#### Console job drafts

- Each printer owns a console draft: selected object, selected spool, simulation speed and placement.
- Each object inside that printer owns its own settings: layer height, speed, infill, supports and placement.
- Switching printers restores that printer's selected object, spool and settings.
- Switching objects restores that object's settings and placement instead of resetting to defaults.
- New object drafts are seeded once from `ObjectToPrintMetaSO`: infill from `Requirements.DefaultInfill`, supports from `PrintProfile.SupportsRequired`, layer/speed `Normal`.
- New printer drafts choose the first occupied spool; later manual spool choices are preserved across switches.
- Placement is cleared only when the job is queued, the object is removed, or the device draft is discarded.

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
    public float WallThicknessMillimeters = 1.2f;
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

Object dimensions, solid volume and surface area are **derived from the catalog prefab** by `ObjectToPrintSizing`, not authored. The global reference layer height (`0.2 mm`) and speed (`80 mm/s`, now the Slow preset) come from `LayerHeightSettings` / `PrintSpeedSettings`; only the two 100% baseline numbers and the wall thickness are per object.

Why inline instead of a separate asset:

- The catalog is currently small.
- It keeps object metadata self-contained.
- A separate `ObjectToPrintPrintProfileSO` can be introduced later without changing job code if needed.

The current catalog has one entry (`Stanford bunny`, `Assets/Art/3D/Prefabs/ObjectsToPrint/Animal/Stanford_bunny/SOs/stanford_bunny_meta_SO.asset`). Its 100% infill baseline is calibrated against PrusaSlicer in the M2 gate.

The stored calculation baseline is `0.20 mm` layer, `80 mm/s` (Slow), `100%` infill and no supports. It is displayed in `Tools/Manufacturing/Print Profile Generator` and in the profile's inspector header; time and filament are stored at that baseline, and the console multipliers scale from it.

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

These values are intentionally simple and tunable. Infill changes time and filament; supports use a per-object overhang multiplier; layer height and speed change time (filament stays constant at this abstraction); confirmed scale uses the shell/infill two-term factor. The reference layer height (`0.2 mm`) and speed (`80 mm/s`, Slow) are global settings.

The profile baseline is the only time coefficient that can absorb a single slicer measurement: at `0.2 mm` / `80 mm/s` / `100%` infill / no supports every multiplier is `1.0`, so `time = PrintTimeSecondsAt100PercentInfill`. The editor tool exposes `reference flow x extrusion duty`: its round defaults are `12 mm3/s x 0.5 = 6.0 mm3/s`, while the bunny's exact calibration is `5.88922 mm3/s`. The profile baseline remains authoritative; do not add a second global time correction on top of it. The layer/speed/infill/support multipliers need a multi-point slicer matrix if they are to be refitted.

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
- `PrintJobRequest` with `SpoolSlotId` and `PrintPlacement`
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
| **M3** | FSM + temperatures + wear + enclosure | Full job cycle, pause/service/hard stop, calibration, wear/filament applied correctly; console drafts persist per printer/object |
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
- Enclosure-installed printers use `EnclosureRadiusMultiplier` (default `1.5×`) for a larger bind/hover sphere.
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
- `ObjectToPrintSizing` derives dimensions and mesh geometry from the prefab.
- `InfillPattern` and `InfillSettings`.
- `SupportsMode` (`None` / `Auto`) and support cost/penalty rules.
- `FilamentMaterialProfileSO` (data type; spool reference is optional).
- `LayerHeightPreset`, `PrintSpeedPreset`, `LayerHeightSettings`, `PrintSpeedSettings`.
- `SpoolSlotDefinition` data with `Id`, `Label` and `PartLocation`; up to four UI slots.
- `PrinterEnclosureSpoolSocket` mapping for docked external spools.
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
- The `SPOOL` row is hidden for zero/one slot and dynamically shows up to four slot labels. The console defaults to an occupied slot and warns when the selected slot is empty.
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
2. Compare estimates against a slicer at Normal layer height and Slow speed (`80 mm/s`); record the tuning factor. Check one Draft and one Fine print for the layer-height scaling, then spot-check Normal/Fast speed presets.
3. Verify spool remaining calculation at 100%, 50%, and 10% condition, including 0/1/up-to-four slot selection and a missing selected slot.
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
  - after confirm, filament and time use the object-derived shell/infill scale rule (fixed layer height and speed), then feed validation and `PrintJobRequest`;
  - rotation changes fit only, not volume.
- `Place on bed` button in the console; `Start print` requires a confirmed placement.
- Placement is draft-only and required for every print. It is not stored on the printer or reused; leaving the console or starting a print discards it so the next job must place the object again.
- **Placement UI is authored by the project owner.** The roadmap asks for the hints/validity/confirm-cancel UI before implementation.
- Editor tool `PrintProfileGenerator`:
  - batch or selected `ObjectToPrintMetaSO` entries from the catalog,
  - load the prefab, combine renderer bounds (report only; runtime sizing uses the same data), compute mesh volume and surface area,
  - estimate overhang ratio and set `SupportsRequired` plus `SupportOverhangPercent`,
  - seed 100% infill filament/time from volume, a configurable reference flow x extrusion duty and a configurable filament packing factor,
  - write the configurable wall thickness used by the shell/infill scale rule,
  - report solid shell, material shell for the default infill, and the scale-2x factor using the same rule as runtime,
  - share the reference layer height/speed and seed coefficients in one editor settings asset,
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
3. **Re-check required after the estimate refinement:** recalculated estimates match the shell/infill scale rule at the minimum, `1.0` and the maximum scale, and support cost follows each object's overhang percentage.
4. **Done:** cancel restores the previous placement and confirm survives console close/reopen.
5. **Done:** the editor generator reproduces the bunny `36 720 s` / `85.36 m` with the exact calibration (`5.88922 mm3/s`, `0.94943`); the round defaults (`6.0 mm3/s`, `1.0`) are approximate seeds. The full matrix review is in [`estimate_matrix.md`](estimate_matrix.md).
6. **Done:** confirm and cancel return with the same blend duration as entering.
7. **Done:** the project owner authored the placement UI on `object_place_canvas`.

**M2.5 gate passed; M3 may start after the M2.5 commit. The estimate refinement recheck is a post-M2.5 follow-up.**

---

### M3 — Printer FSM, temperatures, wear, and enclosure loop

**Goal:** A job actually runs through preheat, print, cool, and finish; wear and filament are consumed; the player can pause, service, hard-stop and calibrate through the console.

**Status:** Done (2026-11-20). The MK4S manual gate passed, including the calibration-sensitive part policy. CoreXY play-mode validation and 3/4-slot console presentation are deferred until authored printer models exist.

**Deliverables**

- `PrinterStateMachine` with the states and transitions in section 6.3, including `Calibrating`.
- Simulated-time ticking with a per-printer `PrintSpeedMultiplier`.
- Console `SIM SPEED` preset row: `1×`, `4×`, `64×`, `120×`, `240×` (default `1×`). The value is stored on the printer, not globally, and can be changed while idle or printing. Above `1×`, the estimate line adds `(real …)` next to the simulated duration.
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
- `SpoolFilamentState` for the selected spool slot id, including remaining metres, spool rotation and wear-level child sync.
- `SpoolSlotDefinition` data with string ids and up to four console slots; requests and drafts persist the slot id.
- `CoreXYPrinterMotion` for coupled-XY rectangular XYZ printers.
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
- Console controls for `Pause`/`Resume`, `Hard stop`/`Confirm stop` and `Calibrate`. There is no `Clear bed`; finalized completed and partial prints are removed manually in M4.
- Console job drafts are keyed by printer, with per-object settings and placement, so switching printers/objects restores the prior draft.
- Printer continues ticking when unbound.
- Pause/resume through the console, not a new InputAction:
  - pause stops print progress, moves the nozzle to `HomePositionNormalized` and lets the nozzle/bed cool;
  - service swap is allowed only after the nozzle and bed are below `SafeServiceTemperatureC` (default `50 °C`);
  - parts may be swapped while paused, except `Printer.ServiceBlockedDuringPrint` references (default: print bed, print bed thermistor, print plate);
  - resume requires every critical part and the selected spool to be installed again; if a blocked part must change, the print can only be hard-stopped;
  - a pause leaves a seam/quality penalty and a long pause may add adhesion risk in M5.
- Calibration state:
  - console `Calibrate` button starts an `N × N` grid, with `N` exposed as `CalibrationGridSize`;
  - the head moves smoothly from its current position to `PrinterMotionBase.CalibrationHomePrintPosition` (global X/Z, optional calibration height), lifts, visits every point, lowers to height `0`, dwells, lifts, and returns to the global home;
  - `CalibrationLiftHeightMeters`, `CalibrationMoveSpeedMetersPerSecond` and `CalibrationPointDwellSeconds` are exposed;
  - calibration travel and dwell use the active `PrintSpeedMultiplier`; use `1×` to inspect the grid visually;
  - successful calibration marks the printer calibrated for the next job; uncalibrated jobs get a random `0.7–1.0×` quality factor at the end of printing and a console warning. A job that actually printed consumes the calibration when it finalizes, so the following job needs a fresh calibration; a hard stop during preheat keeps it;
  - changing a calibration-sensitive part sets `IsCalibrated = false` whether the printer is idle or on a job; the warning stays visible until the finished/partial print is removed. Spool and enclosure-accessory changes do not invalidate calibration;
  - `Hard stop` cancels calibration and moves to the global home.
- `PrinterMotionBase.HomePositionNormalized` is the global start position for spawn, pause, hard stop and post-print parking. It is drawn as an orange gizmo; `UseCustomHomePosition` exposes the normalized X/Y/Z sliders, and the same normalized X/Z also defines the moving bed home on bed-slinger printers.
- Calibration has a height-only override via `UseCustomCalibrationHomeHeight` + `CalibrationHomeY` (green gizmo). X/Z and the return position always use the global home.
- Interaction protection while a printer is busy:
  - sell and take are blocked for the printer and its enclosure;
  - free-walk interaction is blocked while the printer is running/calibrating, but is allowed once it is `Paused`, cooled below `SafeServiceTemperatureC` and the head is parked (`Printer.IsPausedServiceReady` / `CanOpenInteractions`);
  - when the printer is installed in an enclosure, its interaction list redirects to the enclosure; integrity resolves the printer as one part, wear includes the printer's internal parts while hiding the printer root, and remove resolves to the printer root;
  - part installation is blocked, but the MMB install hint remains visible for an eligible target; pressing MMB is refused in `GameStateItemPlace` and `item_hover_place_text` shows the reason (`Printer is on its job`, `Printer is calibrating` or `Wait for the printer to cool`). The game does not enter `GameStatePartInstall`;
  - `PartHoverInstall` also shows the same reason in `part_install_name` if that state is reached another way;
  - hovering fills the separate FreeWalk status fields instead of a single prompt.
- FreeWalk hover status is implemented as the five separate `free_walk_canvas` text fields above. The layout/visual styling remains owner-authored; the code exposes the metrics and blocks the existing interactions.

**Acceptance criteria**

- Start job → `Preheating` → `Printing` → `Cooling` → `Finished` using the stored placement/scale.
- Preheat completes at target temperature within tolerance.
- Print progress reaches 100% at the expected simulated duration.
- Cooling returns temperatures toward ambient; enclosure cooling is visibly slower.
- Heating and cooling percentages track the actual temperature interval and update in the console and FreeWalk hover.
- Items placed on enclosure shelves or surfaces keep their own name and take prompt while the printer is busy; the enclosure surface itself still shows the busy printer status.
- All enclosure doors must be closed and settled before start/resume. Open doors show a console warning, remain interactable when idle/paused/cooling/finished, and interlock during preheating/printing/calibration.
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
- Calibration with `N` grid points moves smoothly at the selected `SIM SPEED`, touches the plate at every point, and returns to the global home.
- An uncalibrated printer starts with a warning but is not blocked; its final quality uses a random `0.7–1.0×` factor at print completion. A successful calibration applies to the next job that actually prints and is consumed when that job finalizes; a preheat-only cancellation keeps it. Changing a calibration-sensitive part invalidates calibration in idle or active states and keeps the warning visible until the bed is cleared; spool/enclosure-accessory changes do not.
- The console shows the chosen simulation multiplier and live printer status. The validation card also shows an `Enclosure:` row: `installed (+5 quality bonus)`, `not installed (no +5 bonus; -9 quality)`, or the job-specific wording when the printer was installed/removed after the job was queued or started.
- A printer can expose zero, one, two, three or four selectable spool slots; the console hides the selector for zero/one and shows the rest dynamically.
- A CoreXY prefab using `CoreXYPrinterMotion` reaches the same print-space positions as the Cartesian implementation; belt/motor dynamics are not simulated.
- Switching printers preserves each printer's selected object, spool, settings and placement; switching objects preserves their settings/placement.
- A busy printer/enclosure cannot be sold or taken. Free-walk interaction is blocked while running/calibrating, but becomes available after a pause has cooled and parked; all three interaction options are shown, while `ServiceBlockedDuringPrint` still refuses specific parts.
- An installed printer is inspected only through its enclosure: enclosure interaction list, printer-as-one integrity/remove, and printer internals available in wear mode.
- A docked printer validates the enclosure's required parts as well; missing battery closet, adapter, HEPA, fire-suppression or thermometer parts block `Start print` with a specific error, and enclosure wear contributes to the quality estimate.
- Installing a printer moves its installed spools to available enclosure sockets, up to the number of free sockets, hides `spool_holder` when mapped, and restores everything on removal. Overflow blocks the install with a reason. The console supports zero, one or up to four selectable slots.
- Detached enclosure plugs reset to the enclosure plug locations when the enclosure is taken.
- Raw quality tops out at `90`; the post-process state can improve the finished object later.
- `PrintJobRequest.StartedWithEnclosure` captures the enclosure state used for the quality estimate. Removing or reinstalling the printer updates the console row but does not re-apply or remove that job's enclosure contribution.

**Manual gate**

1. Run a short job and compare condition loss to the configured wear logic; tune `WearPerHour` / `WearPerFilamentLength`.
2. Record preheat, print, and cooling times at `1×`, `4×`, `64×`, `120×` and `240×`; confirm the multiplier changes simulated speed only and the estimate line shows the matching real time.
3. Tune the hard-stop penalty so it is noticeable but not catastrophic.
4. Profile 2–4 printers printing simultaneously; check CPU and GC.
5. Verify no NaN/Inf in temperatures, progress, or condition.
6. Verify a partial print blocks the next job and can be taken off the bed and placed.
7. Pause, wait for cooling, swap a nozzle/fan/belt, resume and confirm the seam penalty.
8. Attempt to swap a print-bed part while paused and confirm it is refused. After calibration, change a spool or enclosure accessory in idle and during a paused job and confirm calibration remains; change a nozzle/plate/axis part in both states and confirm it invalidates.
9. Run calibration with `N = 3` and `N = 5`; confirm the head moves smoothly from its current position, lifts, touches every point, returns to `HomePositionNormalized`, and `Hard stop` cancels/homes. Repeat at more than one `SIM SPEED` value; toggle the calibration height override and confirm it affects only the calibration start/lift.
10. Verify the spool rotates in the correct direction and the visible filament level follows the wear dictionary.
11. Check enclosure and no-enclosure quality differences; confirm the raw maximum is `90`.
12. Hover a running printer and its enclosure in FreeWalk; confirm the printer name stays in `item_hover_info_text`, the five status fields update, and take/interact are hidden.
    - Pause the job and wait for cooling and homing. Confirm the name/stats remain and `interact_root` appears on the printer/enclosure; open it and confirm `Parts wear`, `Parts integrity` and `Parts remove` are available. Confirm a `ServiceBlockedDuringPrint` part is still refused. Resume and confirm the interaction closes again.
13. In `ItemsSell`, confirm the busy printer/enclosure shows the reason in `item_hover_sell_description`, uses the wrong selling ghost, and cannot be sold. Repeat for an idle but device-bound printer: `Connected to a device`.
14. While carrying a part, hover a busy/calibrating/hot printer; confirm the MMB install hint is still visible. Press MMB and confirm the game stays in `ItemPlace`, `item_hover_place_text` shows the reason, and no part-install state/ghost is entered.
15. Install a printer in an enclosure, then inspect the enclosure: interaction list must be the enclosure's; integrity must show the printer as one part; wear must show enclosure parts and the printer's internal parts with the printer root hidden; remove must treat the printer as one part with its ghost.
16. Install printers with zero, one, two and four spools. Confirm the console hides the selector for zero/one, derives its selection from an occupied slot, and shows the correct labels for up to four. Confirm the enclosure accepts only as many spools as it has free sockets and blocks overflow with a reason. While docked, confirm the battery closet adapter is not offered as a free take; remove it through the parts flow instead. After removal it behaves as a free part again. Detach a plug, take the enclosure, and confirm it resets to the enclosure plug location. Remove the printer and confirm accessories return and `spool_holder` is visible again.
17. With the printer docked, remove the battery closet, adapter, one HEPA fan, one fire-suppression tube, and the thermometer one at a time. Confirm `Start print` is disabled with a specific missing-part error for each, and the quality estimate includes installed enclosure-part wear. Reinstall them and confirm the errors disappear.
18. Start a print on an uncalibrated printer; confirm the warning appears and final quality is in the `0.7–1.0×` range. Run calibration, then start again and confirm the warning disappears and quality is `1.0×`. Finish that print and start another without recalibrating: the warning must return and the second job must use the random factor. Recalibrate and confirm `1.0×` again. Cancel a job during preheat and confirm calibration is still active; hard-stop after printing has started and confirm it is consumed.
19. With multiple printers connected, change object/settings/placement on printer A, switch to printer B, change them there, then switch back; confirm A's draft is restored. Switch objects on one printer and confirm each object's settings and placement return.
20. Instantiate a rectangular CoreXY prefab with `CoreXYPrinterMotion`; run the motion debug and calibration checks. Confirm X/Z compose onto the shared carriage, the bed/gantry Y role maps correctly, placement/validation still use the rectangular build volume, and no `Printer`/job code changes are needed.
21. Hover printer and enclosure through preheat, printing, cooling and resume-heating; confirm the state percentage moves from 0 to 100 and matches the temperature interval rather than job progress.
22. Place a spool, part or device on an enclosure shelf while the printer is printing; confirm hovering it shows the item's own name and take/use prompt, not the busy printer status, and that it can be taken. Hovering the enclosure surface itself should still show the printer status.
23. Start with one door open; confirm `Start print` is blocked with the console warning. Close both doors and confirm the job starts. Pause, open both doors, confirm they open and `Resume` is blocked with the warning; close and resume.
24. Finish a print and wait for cooling and homing. Confirm both doors open, the finished object can be removed, and the enclosure/printer service interactions remain blocked until the object is removed. Print the same object twice and confirm the head paths differ while staying inside the layer bounds; test rotated and scaled placements.

**Sign-off (2026-11-20):** The MK4S M3 manual gate passed, including the calibration-sensitive part check. The 2026-11-21 thermal/door/head-path checks were added afterward and remain pending. Deferred until the relevant assets exist: CoreXY play-mode validation (no CoreXY model/prefab) and 3/4-slot console presentation (no authored printer with that configuration).

**M3 gate passed; M4 may proceed. The deferred variant checks remain acceptance items for when those printer configurations are authored.**

---

### M4 — Print visualization

**Goal:** The print looks convincing: object reveal, nozzle/bed motion, support columns, and manual removal. Spool rotation and wear-level child sync already arrive in M3.

**Status:** Implementation is in place; the manual gate and the wear-coefficient approval in §10 are pending. M4 is not marked done yet.

**Deliverables**

- `PrintedObjectPresenter`: instantiate the real object prefab under `PrintBedAnchor`, apply the M2.5 placement, own the printed-object lifecycle and release it to `GameStateItemPlace`.
- `PrintedObjectDataHolder`: runtime quality, partial/completed state, printer ownership and `IsOnPrintBed`.
- `PrintRevealController`: per-renderer `_reveal_height` through `MaterialPropertyBlock`, quantized to layer height and updated at ~15 Hz.
- `PrintedSupportPresenter`: sample overhang triangles for the placed object, cluster them to a capped number of points (`48`), and spawn thin vertical support columns. Columns reveal with print height and remain attached to the finished object; M5 adds support removal.
- `PrinterMotionSimulator`: seeded procedural path inside precomputed per-layer bed-local bounds, with a continuous layer-to-layer path and a continuous Z-hop. Bounds are built once at job start; it never moves outside the rectangular build volume.
- `CartesianPrinterMotion` integration through `PrinterMotionBase`; CoreXY uses the same contract when its model prefab exists.
- Spool integration: use the selected spool's `PrintingMaterial` (the shader with `_reveal_height` and backface lighting) during live printing; completed prints apply the spool's regular `FinalMaterial`.
- No live layer cap: printing keeps the shader reveal and backface lighting.
- Paused-settled surface: after cooling and parking, build a sliced, capped surface with the spool's final material; on resume remove it, restore the printing shader/reveal and return to the print path.
- Hard-stopped partial surface: after cooling and parking, build the same sliced, capped surface and use it as the partial visual and collider.
- Partial-print surface: keep the sliced, capped final-material surface after removal and placement. Completed prints have no cap and use the original collider.
- Completion material restore and collider enable only after cooling and head parking.
- Apply `PrinterJob.FinalQuality` to `PrintedObjectDataHolder.Quality`; completed prints are sellable, partial prints are sellable with no income.
- Post-print parking: after cooling, move the head smoothly to `PrinterMotionBase.HomePositionNormalized`. Pause and hard stop also park the head at home.
- Manual removal of completed **and** partial prints. The player hovers the print and enters `GameStateItemPlace` directly, with no `Cancel` option. Removing it clears the printer bed and lets the object be placed like a normal item.
- No `Clear bed` action. Selling a print while it is still on the printer bed is blocked with a clear reason; take it off and place it first.
- Real object prefab, not the legacy printing ghost. The `ObjectToPrintGhostPrintingDataHolder` type bug is fixed, but the real prefab path is preferred.
- `Printer` owns the presenter through `PrintedObject` and auto-adds it for printer prefabs that do not author one, so new models do not need model-specific code.

**Acceptance criteria**

- The object appears where the M2.5 placement put it: position, yaw and uniform scale.
- With `Auto` supports, visible support columns appear under the overhangs and reveal with the print; `None` prints no supports. Supports remain attached after completion.
- Reveal starts empty and grows bottom-up; live printing has no cap.
- Reveal follows the moving bed correctly.
- The nozzle moves inside the per-layer object bounds, not over the whole plate or the full object box on every layer.
- The path follows the actual placed object after rotation, scale and position changes.
- Repeating the same object produces a different seeded path while staying inside the same layer bounds.
- The nozzle appears to work over the printed area; axes stay within soft limits and do not clip.
- Layer transitions are continuous; the head does not teleport between layers.
- The selected spool's printing shader drives the reveal; completion switches the object to the spool's regular material.
- Pause and hard stop move the head smoothly to `HomePositionNormalized`.
- Pause settled: after cooling and parking, the print shows a sliced capped surface with final material; resume removes it and restores the printing shader/reveal.
- Hard stop freezes the partial shape; after cooling and parking, it shows the sliced capped surface with final material.
- Completed prints have no cap and use the original collider.
- Partial prints keep the sliced/capped surface and collider.
- Completed and partial objects are fully removable only after cooling and the head is settled at home.
- Selling a printed object removes the object, cap/sliced surface and every registered object-to-print ghost; mutual references are cleared.
- The object's quality value comes from `CompletedJob.FinalQuality`; partial prints expose no quality value and give no income.
- Hovering a finalized print transitions directly to placement with no cancel path, and the printer bed is released.
- Selling is blocked while the object is on the printer bed.
- No per-frame material instancing or GC spikes.

**Manual gate**

1. Confirm live printing has no cap; compare the reveal top to the nozzle height and adjust the offset if needed.
2. Check bed movement direction and speed.
3. Check the object from several player angles; no popping, holes or z-fighting. Confirm the cap appears only for a settled pause or finalized partial print.
4. Use a temporary multi-mesh test prefab (only Stanford bunny exists in the catalog); confirm all renderers reveal consistently and the paused/hard-stopped cap covers the cut. Remove test assets in M5.
5. Re-check the M3 spool visual at 100%, 50%, 10%, and 0% with the printed object present.
6. Check `Auto` vs `None` supports; columns follow the print height and stay attached after completion.
7. Pause and let the printer cool; confirm the capped final-material surface appears. Resume and confirm the cap disappears, the printing shader returns and the reveal continues.
8. Hard stop after a few layers; after cooling and parking confirm the capped final-material surface and sliced collider. Remove and place the partial print.
9. Check completed object materials, original collider, no cap, placement and sale.
10. Confirm the head parks at `HomePositionNormalized` after cooling and before completed/partial removal.
11. Confirm pause and hard stop park the head at home; resume returns to the print path.
12. Confirm a print on the bed cannot be sold, and that a placed partial print sells with no income while a placed completed print sells normally.
13. Confirm no-cancel removal and bed release.
14. Sell a placed print and confirm no printed object, cap/sliced surface, or object-to-print ghost remains; mutual references are cleared.
15. Profile reveal updates, partial-surface generation and multiple printers; verify no per-frame material instancing or GC spikes.
16. **Wear-coefficient approval:** complete [`wear_coefficients_manual_gate.md`](wear_coefficients_manual_gate.md) and have the project owner approve every wear path's speed.

**Do not proceed to M5 until the print reads clearly as a print from the player's normal viewing distance and the wear-coefficient gate is approved.**

---

### M5 — Failure, advanced enclosure, and polish

**Goal:** Add depth and edge cases after the core loop is stable.

**Candidate deliverables**

- Advanced enclosure effects: draft, particulate filtration, warp and long-pause adhesion risk. Basic chamber temperature, cooling slowdown and enclosure quality are already in M3.
- Support removal from completed/partial prints; M4 keeps generated supports attached.
- `PrintQualityModel` expanding the M2 `PrintQualityEstimator` range, and a real quality value on `IsHaveQuality`. M4 stores the numeric value on `PrintedObjectDataHolder`; M5 turns it into sale value and post-processing.
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
- Job-level enclosure continuity snapshot: whether the job started enclosed, whether it has been interrupted, and the quality range without the enclosure contribution.
- A capped or diminishing pause-quality penalty so repeated pause/service cycles cannot drive quality to zero.
- **Final documentation gate:** replace [`new_printer_agent_setup_reminder.md`](new_printer_agent_setup_reminder.md) with the full agent-facing setup guide for adding new FFF printer models, and replace [`new_object_print_setup_reminder.md`](new_object_print_setup_reminder.md) with the full guide for adding printable-object prefabs, profiles, ghosts and calibration. Both must cover their prefab/data contracts, validation and verification.
- Production cleanup:
  - remove the calibration-only `PrintBedAnchorDebug` and `PrinterMotionDebug` components from shipped prefabs, or keep them disabled and stripped from the player build;
  - remove any placed `PrintRevealDebug` and temporary test cubes;
  - confirm `PrinterBindHighlight.DebugState` is `Hidden`;
  - keep editor-only debug context menus and diagnostic helpers out of the player path, using `#if UNITY_EDITOR` or an editor assembly where appropriate;
  - remove temporary debug logging/GUI that is not part of the shipped experience.

#### Enclosure continuity during a job

The enclosure quality contribution is tied to the whole print, not just the
state at validation. A job keeps its original time, filament, speed, layer,
supports and placement values; only the enclosure contribution and resume
validation can change.

**Quality policy**

- The enclosure quality bonus is only earned if the printer is enclosed for
  every **printing** interval. Pausing inside the enclosure does not break it.
- Removing the printer while paused and restoring it before the next resume
  does not break continuity; the job never printed outside.
- If the job resumes outside the enclosure, set a one-way
  `EnclosureInterrupted` flag for that job. Reinstalling the enclosure later
  does not restore the bonus.
- A job that starts unenclosed can never earn the continuous-enclosure bonus,
  even if the enclosure is installed before a later resume.
- Continuous enclosure: keep the existing `+5` enclosure contribution.
- Interrupted or never enclosed: use the existing no-enclosure `-9`
  contribution, not an additional per-toggle penalty.
- Store the quality range **before** the enclosure contribution on the job.
  This avoids applying `+5` or `-9` twice when the state changes.
- The enclosure penalty is latched once. Repeated removal/installation must
  not stack enclosure penalties.

**Pause penalty**

- The existing per-resume pause penalty still applies.
- M5 must cap or diminish it so repeated pauses cannot zero quality. Suggested
  starting rule: `min(pauseCount * PauseQualityPenalty, 20)` quality points.
- Enclosure toggles while paused do not add extra enclosure penalties; the
  normal pause penalty is the cost of another resume.
- Resuming outside the enclosure adds the no-enclosure quality state, not a
  second stack of pause penalties.

**Resume validation and warnings**

- If the enclosure is still present at resume, validate its critical parts as
  today; missing required parts block resume with the existing specific errors.
- If the printer is outside the enclosure, enclosure critical parts are not
  required for resume, but the job is marked enclosure-interrupted and uses
  the no-enclosure quality contribution.
- Show one persistent job warning on the first interruption:
  `Print resumed without enclosure. Final quality will use the no-enclosure penalty.`
- If the enclosure is reinstalled later:
  `Enclosure restored. This job is already marked enclosure-interrupted; the no-enclosure penalty remains.`
- If an unenclosed job gets an enclosure before resume:
  `Enclosure installed after printing started. This job cannot earn the continuous-enclosure bonus.`
- Removing or reinstalling while paused before any outside resume should not
  change quality; optionally show a transient hint that only the pause penalty
  applies.
- Repeated toggles must update the warning state once, not spam a new warning
  per cycle.
- Calibration is unaffected by enclosure movement. Time, filament, path and
  print progress remain frozen for the active job.
- Enclosure-part wear follows the current docked state: removed enclosure parts
  stop accumulating wear; reinstalling resumes their wear targets. No
  retroactive wear is applied for the paused interval.

**Acceptance criteria**

- Enclosure changes thermal behavior and quality in a visible, understandable way.
- A job that resumes outside the enclosure loses the continuous-enclosure quality bonus exactly once; reinstalling does not restore it. Repeated toggles do not stack enclosure penalties.
- A job that starts unenclosed cannot gain the enclosure bonus by installing an enclosure before a later resume.
- Removing and reinstalling the enclosure while paused, before any outside resume, preserves the bonus and only incurs the normal pause penalty.
- Removing or reinstalling the enclosure never changes the active job's estimated time, filament, speed, layer height, supports, placement or progress; only the enclosure quality contribution and resume validation change.
- Pause quality penalties are capped or diminishing and repeated pauses cannot make the job quality estimate collapse to zero.
- The enclosure continuity warning is persistent once interrupted and is not duplicated on every toggle.
- Failure modes are telegraphed and recoverable.
- Quality affects the object's value or usability.
- The game remains performant with multiple printers.
- Shipped prefabs/scenes contain no active calibration-only debug components, test cubes, or debug GUI.

**Manual gate**

1. Start enclosed, pause, remove and reinstall the enclosure before resuming: the enclosure bonus remains; only the normal pause penalty applies.
2. Start enclosed, pause, remove the printer, resume outside: the persistent no-enclosure warning appears; final quality uses the no-enclosure contribution; time, filament and progress do not change.
3. Reinstall the enclosure and resume again: the job stays enclosure-interrupted; the no-enclosure penalty is not restored and no additional enclosure penalty stacks.
4. Start unenclosed, install the enclosure before resume: the job does not gain the bonus and shows the “started without enclosure” warning.
5. Perform several pause/resume cycles: confirm the pause penalty follows the M5 cap/diminishing rule and cannot zero quality by itself.
6. With the enclosure still present, remove a critical enclosure part and resume: resume is blocked with the specific missing-part error. With the printer outside the enclosure, resume is allowed but uses the no-enclosure quality state.
7. Confirm enclosure wear targets follow the current docked state and calibration is unchanged by enclosure movement.

---

## 9. Calibration record

Fill this table during M0 and update it during later milestones.

| Item | Recorded value | Notes |
|---|---|---|
| Horizontal X transform | `x_axis_base/extruder_base` | local X |
| Horizontal X min / max offset | `-0.127` / `0.127` | signed local units; `Invert = 1` maps print-space -X to local +0.127 because the carriage parent maps local X to world -X |
| Horizontal X speed | `0.2` | units/s |
| Horizontal Z transform | `print_bed_holder_base` | local Y (bed front/back) |
| Horizontal Z min / max offset | `-0.07` / `0.151` | signed local units; `Invert = 1` because moving the bed toward +Z moves the nozzle toward bed-local -Z; `MinLocal` maps to print-space +Z and `MaxLocal` to print-space -Z |
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
| Enclosure bind radius | `1.5×` | `PrinterBindHighlight.EnclosureRadiusMultiplier`; applies while the printer is installed in a `PrinterEnclosure` and refreshes on install/remove |
| Bind hover volume | the highlight sphere itself | analytic ray-sphere test; aiming at the bubble is enough |
| Bind hover opacity | `0.30` vs `0.18` idle | slightly more opaque on hover, never opaque |
| Bind distance | `3 m` | `PrinterBindTargeting.MaxDistance`; to the sphere entry point, with a line-of-sight raycast |
| Bind occlusion | target printer + its installed enclosure assembly ignored | other printers/walls still block; enclosure colliders are not treated as walls for the printer installed inside them |
| Bind reason duration | `2 s` | `DeviceBindPanel.ReasonDuration` on `device_bind_canvas` |
| Console UI | world-space uGUI canvas per device | logical width `1600`, height from screen aspect, `0.0015 m` offset, Montserrat SDF, items_market style |
| Console canvas facing | forward points into the screen | UGUI is readable when the viewer looks along canvas `+Z`; verify in the M1 gate |
| Console canvas placement | display bounds centre + normal | offset from the front-most bounds point, then `0.0015 m`; verify in the M1 gate |
| Console refresh | `0.2 s` | focused view on events plus ~5 Hz polling; other views slower |
| Infill density | None `0.1892`, Grid `0.3153`, Gyroid `0.3531` | `InfillSettings.Density` |
| Infill time factor | None `0.2615`, Grid `0.3399`, Gyroid `0.3765` | `InfillSettings.TimeFactor` |
| Layer height | `0.28` / `0.20` / `0.12` mm | `LayerHeightSettings`; time scales `reference / selected` |
| Print speed | `80` / `160` / `240` mm/s | `PrintSpeedSettings`; `80 mm/s` is the Slow reference; time scales `(reference / selected)^0.8` |
| Raw quality | base `82`, range width `5`, raw cap `90` | `PrintQualityEstimator`; enclosure `+5`, no enclosure `-9`; post-process can polish beyond 90 later |
| Part wear (all installed) | block `0%`, warn `≤20%`; critical warn `20–40%` | `WornOutConditionPercent` / `LowConditionWarningPercent` / `CriticalPartWarningPercent` |
| Job catalog | `Assets/SOs/ObjectsToPrint/object_to_print_metas_list_SO.asset` | assigned to `DeviceConsolePresenter.ObjectCatalog` |
| Placement scale | soft limits: largest dimension ≥ `2 cm`; footprint/height within plate/`BuildVolume` | overscale/underscale is allowed but invalid; no clamp |
| Plate margin | `2 mm` inside the plate bounds | keeps the footprint off the plate edge |
| Placement shader | `_printer_bed_placement_state` `0/1`, `_PRINTER_BED_PLACEMENT_STATE_WRONG` | ghost validity feedback |
| Scale estimate rule | two-term shell/infill factor; at `scale = 1` it is exactly `1` | shell `s²`, infill `s³`; uses profile `WallThicknessMillimeters` |
| Support cost | filament × `1 + 0.06 × overhang / 15.48308`, time × `1 + 0.12 × overhang / 15.48308` | bunny reference overhang is `15.48308%`; other objects use their own `SupportOverhangPercent` |
| Support rule | required object: `None` → `-25` raw and max `55`; optional `Auto` → warning only | `PrintJobValidator` / `PrintQualityEstimator` |
| Material gating | none | any spool material prints; M3/M5 use material for temperatures and quality |
| Bunny baseline (current profile) | `85.36 m` at 100%, `36 720 s` at 100% | Both values restored to the PrusaSlicer reference; exact editor calibration is `5.88922 / 0.94943`, round defaults are `6.0 / 1.0` |
| Console mask | `ScreenCornerRadius` / `ScreenMaskSprite` on `DeviceScreenAnchor` | smartphone starts at `0.05`; set `0` for square corners |
| Console camera idle priority | `-1` | player camera stays at implicit `0` |
| Print speed multiplier | `1×` / `4×` / `64×` / `120×` / `240×` | console `SIM SPEED`; default `1×`, stored per printer; estimates show `(real …)` above `1×` |
| Temperature tolerance | `±2 °C` | preheat completes when nozzle and bed are in tolerance |
| Preheat timeout | `900` simulated seconds | logs a warning and enters `Fault` |
| Safe service temperature | `50 °C` | pause service gate |
| Hard-stop penalty | nozzle `0.2 + 0.8 × normalized`, heater `0.1 + 0.4 × normalized`, bed/plate `0.1 + 0.3 × normalized` | tune in the M3 gate |
| Calibration grid | `5 × 5`, `0.15 s` dwell, `10 mm` padding | `CalibrationGridSize` / `CalibrationPointDwellSeconds` / `CalibrationPaddingMeters` |
| Calibration motion | `3 cm` lift, `0.25 m/s` travel scaled by `SIM SPEED` | `CalibrationLiftHeightMeters` / `CalibrationMoveSpeedMetersPerSecond` |
| Calibration quality | calibrated `1.0×`; uncalibrated random `0.7–1.0×` at print completion | final value stored on `PrinterJob.FinalQuality`; a job that actually printed consumes the calibration at finalization; warning persists until bed clear |
| Global home / start | default `(0, 1, 0.75)` visual | top-left head, build volume/bed about 3/4 forward; X `0` = left, Z `0` = back; `UseCustomHomePosition` + `HomeX/Y/Z`; orange gizmo |
| Calibration height | global home height by default | `UseCustomCalibrationHomeHeight` + `CalibrationHomeY`; green gizmo; calibration returns to global home |
| Spool rotation | root local `-Y`, `degrees = metres / (2π × 0.1 m) × 360` | `EffectiveRadiusMeters`, `RotationAxis`, `InvertRotation` on `ItemFFFSpoolDataHolder` |

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
- Simulation speeds `1×`, `4×`, `64×`, `120×`, `240×`; above `1×` the console also shows the real duration in parentheses.
- Pause/resume and safe service swap.
- Hard stop at preheat, mid-print, and near-complete.
- Wear application per part, including enclosure parts and every delta type.
- Selected spool depletion, rotation and wear-level child sync.
- Enclosure vs no-enclosure thermal behavior and quality cap.
- Smooth calibration grid at `N = 3` and `N = 5`, return to `HomePositionNormalized`.
- Calibrated vs uncalibrated quality multiplier and console warning.
- Per-printer and per-object console draft restoration.
- Busy printer/enclosure blocks sell/take and part install; FreeWalk keeps the five status fields. Paused-safe interaction opens all three parts options after cooling and homing.
- Installed printer is inspected through the enclosure: one interaction list, printer-as-one integrity/remove, printer-internal wear entries available.
- Unbind/rebind during print.
- Worn-out part blocks job.
- Multiple printers simultaneously.

**Status (2026-11-20):** Passed for the MK4S configuration. CoreXY motion and 3/4-slot layout checks are deferred until those prefabs/configurations exist.

### M4

- Exact placement match: position, yaw and scale.
- Visual reveal at start/mid/end; no cap during live printing.
- Pause settled shows the capped final-material surface; resume removes it and restores the printing shader.
- Hard-stop partial shows the capped final-material surface after cooling/parking.
- Spool printing material during the print; regular final material for completed and partial surfaces.
- Auto supports render as columns under overhangs, follow print height, and remain attached.
- No supports render when `None`.
- Nozzle path stays inside each layer's bounds at `0.5x` / `1x` / `2x` scale and with rotation; Slow/Normal/Fast visibly change pace; layer transitions are continuous.
- Repeat the same object on the same printer and confirm the seeded path differs without leaving the layers.
- Bed motion and nozzle path.
- M3 spool visual with the printed object present.
- Pause and hard stop park at `HomePositionNormalized`.
- Post-print parking at `HomePositionNormalized`; finalize only after cooling and parking.
- Partial print freeze, cap, sliced collider and manual removal.
- Completed object material/collider restore.
- No selling while the object is on the bed.
- No `Clear bed`; completed/partial removal uses hover placement with no cancel.
- Sale destroys the printed object, cap/sliced surface and all registered ghosts with mutual references cleared.
- Performance profile.
- Wear-coefficient approval in `wear_coefficients_manual_gate.md`.

### M5

- Enclosure thermal/quality effects.
- Enclosure continuity: pause/remove/reinstall before resume preserves the bonus; resume outside latches the no-enclosure penalty; reinstalling does not restore it; toggles do not stack.
- Unenclosed start cannot gain an enclosure bonus later.
- Enclosure critical parts are revalidated when the enclosure is still present; outside enclosure resumes use the no-enclosure quality contribution with a persistent warning.
- Pause penalty cap/diminishing rule.
- Failure modes, including wear-triggered service prompts.
- Quality and sale value.
- Infill gameplay effect (strength, durability or sale value) so `None`/`Grid`/`Gyroid` are not only cost tiers; `Gyroid` is currently dominated by `Grid`.
- Audio/VFX.
- Save/load, if implemented.
- Production cleanup: calibration/debug components removed or disabled, test cubes removed, editor-only debug helpers excluded from the player.
- Final documentation gate is complete: the new-printer and new-object agent setup guides cover the universal contracts, current supported profiles and deferred cases.

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
| Partial print blocks the player with no way out | Completed and partial prints share the hover-to-place removal flow; no `Clear bed` action |
| Partial sliced-surface build cost | Build it once on pause-settled or hard-stop finalization, not per layer; fall back to a box collider if slicing fails |
| Ghosts left after selling a print | `ObjectToPrintDataHolder.DestroyGhostInstances` clears mutual references and destroys every registered ghost; destroying the object removes the cap/sliced surface |
| Object removal races the printer busy rule | `PrintedObjectDataHolder` owns `IsOnPrintBed`; `Printer.TryReleasePrintedObject` clears the bed before placement |
| Selling a print while it is on the bed | `ItemHoverSell` returns a wrong-state reason until the object is released and placed |
| Enclosure toggling during pause exploits quality | Latch `EnclosureInterrupted` once; reinstallation does not restore the bonus; pause penalties are capped/diminished |
| Enclosure penalty applied twice | Store the quality range without the enclosure contribution and apply `+5` / `-9` once at finalization |

---

## 12. Out of scope / future

- Print queue.
- Resume from partial print.
- Real slicer/G-code.
- Multi-material printing.
- Printer networking/range.
- Full device power/battery simulation.
- Delta printer prefabs and non-rectangular volumes (CoreXY motion class exists; model prefab/art still required).
- Auto-generated print profiles.
- Multiplayer.
- Save/load.

---

## 13. Current status and next step

**Status:** M0–M3 are complete. M1's device-type/aspect checks remain carried forward. M2 and M2.5 are committed, calibrated and play-mode gated; the post-M2.5 estimate refinement still needs its scale/support re-check, and the new 80/160/240 speed presets need a fresh M2 time spot-check. The bunny profile is restored to the PrusaSlicer reference (`85.36 m`, `36 720 s`); the editor exposes the exact seed calibration (`5.88922 mm3/s`, `0.94943`), while its round defaults (`6.0 mm3/s`, `1.0`) are approximate seeds. M3 is marked done on 2026-11-20. M4's implementation is now in place: the printer owns a printed-object presenter, spool-bound live reveal material, pause-settled and hard-stopped capped partial surfaces, sliced partial colliders, support columns, per-layer object-following nozzle motion, parking/finalization, and manual completed/partial removal. M4 is **not marked done**: its manual gate and the wear-coefficient approval remain pending. M5 has not started.


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

- `ObjectToPrintPrintProfile` (two baselines + supports + wall thickness) and `ObjectToPrintRequirements` live on `ObjectToPrintMetaSO`; size/geometry is derived by `ObjectToPrintSizing`.
- `InfillPattern`, `LayerHeightPreset`, `PrintSpeedPreset`, `PrintEstimates`, `PrintQualityEstimator`, `PrintJobRequest`, `PrintJobValidationResult` and `PrintJobValidator` cover settings, estimates, raw quality and rules.
- `Printer` exposes `PendingJob`, `HasPendingJob`, `HasPartialPrint`, `CriticalPartLocations`, `PrintPlateLocation` and `SpoolSlotDefinitions` references.
- The console's print-job card has an object picker, layer-height/speed/infill/supports rows, time/filament/raw-quality summary, validation text, and a gated `Start print`.
- `DeviceConsolePresenter.ObjectCatalog` is wired in `MainScene` to the authored catalog.

Remaining M2 work (manual gate):

1. **Calibration:** `85.36 m` and `36 720 s` at 100% infill (`0.2 mm`, `80 mm/s`; now Slow). Both values are restored to the PrusaSlicer reference; the exact editor seed calibration is `5.88922 mm3/s` / `0.94943`, while the tool defaults are round seeds.
2. **Original check:** Grid / Normal / Normal compared with PrusaSlicer (`28.53 m`, `3 h 53 min`) and matched by the console (`28.5 m`, `3 h 53 min`). With the new presets, old Normal is Slow; the reference point now matches in both time and filament. Normal/Fast presets and scale/support still need a fresh spot-check.
3. **Done:** Draft and Fine spot checks confirmed time and raw quality move in the expected direction.
4. **Done:** validation matrix verified, including the oversized-object rejection.
5. **Done:** `Start print` queues `Printer.PendingJob` only when valid; M3 consumes it on the next printer tick.
6. **Done:** `Printer.CriticalPartLocations` / `PrintPlateLocation` references confirmed.

M1's remaining device-type/aspect checks are carried forward; they do not block M2 validation.

M2.5 placement is implemented against the project-owner-authored `object_place_canvas` (move/rotate/scale hints, `object_place_reason`, confirm/cancel). The editor profile generator is implemented already: `Tools/Manufacturing/Print Profile Generator`. Follow-up fixes add data-driven up-to-four spool selection, keep the cached console view (and its job draft) across placement/taking until the device is sold, and blend the placement camera back to the console on confirm/cancel. The original M2.5 manual gate passed with the authored tuning values. The post-M2.5 estimate refinement (shell/infill scale and overhang-based supports) still needs its scale/support re-check.

The M3 first iteration is implemented and its earlier manual checks passed. The follow-up spool, docking, calibration-sensitive and CoreXY work extended the same scope. M3 is complete and marked done on 2026-11-20; CoreXY play-mode validation and 3/4-slot presentation remain deferred until the corresponding assets exist.

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
│   │   └── SpoolSlotDefinition.cs
│   ├── Motion/
│   │   ├── PrinterMotionBase.cs
│   │   ├── CartesianPrinterMotion.cs
│   │   ├── CoreXYPrinterMotion.cs
│   │   └── AxisBinding.cs
│   ├── Visuals/
│   │   ├── PrintedObjectPresenter.cs
│   │   ├── PrinterMotionSimulator.cs
│   │   ├── PrintLayerBounds.cs
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
│   ├── DeviceJobPanel.cs
│   └── PrinterJobDraftStore.cs
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
| 2026-11-16 | 120× fastest simulation preset; 1× default | Real-time default with an optional fast-forward |
| 2026-11-21 | 240× preset added | Extends the console to 1×/4×/64×/120×/240×; estimates show real duration above 1× |
| 2026-11-21 | Simulation multiplier applies before the hitch clamp | Real frame delta is capped at `1 s` then multiplied; the old `0.1 s` pre-multiplier clamp silently reduced high presets at low frame rates |
| 2026-11-21 | Print-space X calibration uses the carriage sign | MK4S horizontal X uses `Invert = 1` so the head path follows the same bed-local X as the placed object |
| 2026-11-21 | Print-space Z calibration uses the bed-motion sign | MK4S horizontal Z uses `Invert = 1`; moving the bed toward +Z moves the nozzle toward bed-local -Z, so the bed axis must run opposite the print-space axis |
| 2026-11-21 | Home/park moves use real time | A visible park must not be multiplied by `120×`/`240×`; resuming mid-park returns smoothly from the current position instead of snapping home |
| 2026-11-21 | Console enclosure row follows the job start state | The row explains the `+5` / `-9` quality contribution and does not imply that removing/reinstalling re-applies a queued or running job's bonus |
| 2026-11-21 | Door interlock prompts use short state labels | Interaction prompts need to fit the hover label; the console keeps the longer door warning |
| 2026-11-21 | Resume reuses the printed object | Reheating after a pause must not rebuild the mesh, reset the reveal or snap the head; only the motion return is re-armed |
| 2026-11-21 | Home coefficients use the printer-front frame | `HomeX`/`HomeZ` are user-facing visual coordinates (0 = left/back, 1 = right/front); the bed-moving Z role is converted to print space inside the motion implementation, so `AxisBinding.Invert` never leaks into authored or player values |
| 2026-11-21 | Calibration is consumed by a printed job | A successful calibration applies to the next job that actually prints; finalization returns the printer to uncalibrated for the following job. The warning and random `0.7–1.0×` factor are unchanged, and a preheat-only cancellation keeps calibration |
| 2026-11-21 | Printer/enclosure audit keeps shared runtime model-agnostic | Axis signs, limits and bed-moving detection stay in motion data; enclosure doors, spools, docking and critical parts are serialized; the only name-based helper is editor-only |
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
| 2026-11-18 | Uniform scale allowed in placement | Volume matters; estimates use a shell/infill two-term scale rule; bounds are dynamic |
| 2026-11-20 | Object geometry drives the scale estimate | `ObjectToPrintSizing` caches solid volume and surface area; the profile wall thickness yields the shell share, and `scale = 1` preserves the calibrated profile |
| 2026-11-20 | Print speed presets are 80 / 160 / 240 mm/s | The 80 mm/s profile baseline is now the Slow preset; Normal and Fast scale time from it, and the M2 speed spot-check is pending |
| 2026-11-20 | Support cost follows the object overhang | The bunny’s `15.48308%` reference reproduces its `1.06`/`1.12` cost; other objects use their own `SupportOverhangPercent` |
| 2026-11-18 | Scale limits from printer/object | Max must fit `BuildVolume` and plate; min keeps the object at least `2 cm` |
| 2026-11-18 | `PrintBedAnchor` is the placement reference | One source for surface, size and front direction |
| 2026-11-18 | Placement is per-job and not persisted | Same as slicing: each print is set up fresh |
| 2026-11-18 | Scale limits are soft, not clamped | The player sees the shader error and cannot confirm; no silent snapping |
| 2026-11-18 | Supports visualised in M4 as simple columns | Full support generation is a slicer problem; columns make the abstract supports readable and stay attached until M5 removal |
| 2026-11-18 | Placement UI authored by owner | Keeps the game-feel/UI design with the project owner |
| 2026-11-19 | Simulation speed is a per-printer console preset | The printer owns the job; presets match existing console rows and avoid slider UI work |
| 2026-11-19 | All wear delta types are mapped to printer/enclosure parts or print-use actions | Existing assets and SOs already define the intended wear kinds; no parallel wear model |
| 2026-11-19 | Raw print quality caps at 90 | Leaves room for the post-process state to improve the final object |
| 2026-11-19 | Spool rotation and wear-level child sync move to M3 | The loop already knows consumed metres; M4 only polishes reveal |
| 2026-11-19 | Calibration is a printer state, not a console overlay | Reuses motion and the existing status/controls; no new global state |
| 2026-11-19 | Busy printer/enclosure blocks sell/take; paused-safe interaction is allowed | Protects a running job while allowing Wear/Integrity/Remove after a safe pause; hover status remains visible |
| 2026-11-19 | Spool rotation uses the root with local `-Y` | The mesh child has a different authored axis; the root matches the installed spool holder |
| 2026-11-19 | Head start position lives on `PrinterMotionBase` in print space | Axis-safe across printer variants; a posed prefab transform would be brittle |
| 2026-11-19 | Calibration moves smoothly with lift/dwell | Matches real printer calibration and makes the grid readable |
| 2026-11-19 | Finished prints are removed by hover-to-place | Player-friendly; no separate removal state or cancel branch |
| 2026-11-19 | `Home()` remains mechanical; `HomePositionNormalized` is global gameplay home | Avoid overloading axis-home semantics; calibration can override independently |
| 2026-11-19 | Use normalized X/Y/Z home and no separate bed home | The moving bed is one of the print-space axes on bed-slingers; fixed beds ignore that axis |
| 2026-11-19 | Use the same home after spawn, pause, calibration and finished prints | One position keeps placement, camera and aesthetics consistent |
| 2026-11-19 | Default home is implicit; custom home is opt-in | `UseCustomHomePosition` keeps the default top-left/3/4-forward pose while exposing sliders only when needed |
| 2026-11-19 | Calibration home is a height-only optional override | Calibration can start/lift higher without redefining X/Z or the global return position |
| 2026-11-19 | FreeWalk printer stats use five separate texts | Keeps the name/status stack readable and lets the owner style each metric independently |
| 2026-11-19 | ItemHoverInteract stays silent for a busy printer unless paused-safe | Prevents duplicate status prompts while running; `CanOpenInteractions` enables service interactions after cooling and homing |
| 2026-11-19 | Bound printers cannot be sold | Prevents dangling device bindings and matches the device-side sell rule |
| 2026-11-19 | Calibration follows the printer simulation multiplier | One speed control for all simulated motion; use 1× for visual inspection |
| 2026-11-19 | One normalized home for head, bed and all state returns | Removes competing start positions and keeps placement/aesthetics consistent |
| 2026-11-19 | Part-install mode is blocked before state entry from ItemPlace | Keeps the player in `ItemPlace`, keeps the MMB hint visible, shows `item_hover_place_text`, and avoids a dead MMB transition |
| 2026-11-19 | Console job drafts are per printer and per object | Preserves placement/settings across printer and object switches without leaking them between drafts |
| 2026-11-19 | Installed printers are inspected through the enclosure | One interaction surface; integrity/remove use the printer root, wear includes printer internals |
| 2026-11-19 | Enclosure-installed printers get a larger bind sphere | `EnclosureRadiusMultiplier` (default `1.5×`) keeps the enclosure printer easy to target |
| 2026-11-19 | Bind occlusion ignores the installed enclosure assembly | Resolved through `PartsAssembly.FindEnclosingPrinterEnclosure`, so runtime install/uninstall does not rely on stale environment state; enclosure colliders still block other printers |
| 2026-11-20 | Printer/enclosure features must stay printer-agnostic | Shared runtime uses serialized references, spool definitions, role-based docking and `PrinterMotionBase`; model-specific data belongs in prefabs/assets, never runtime name/type branches |
| 2026-11-20 | Critical-list placeholders are ignored at runtime | Empty `CriticalPartLocations` entries do not block jobs or print "unassigned reference"; the editor configuration check reports genuinely unassigned entries |
| 2026-11-20 | Enclosure docking uses the existing `PartLocation` sockets | Printer, spool, battery closet and adapter are installed parts; plug reset points stay free-part transforms because plugs are not installed parts |
| 2026-11-20 | Docking relocates printer accessories and hides `spool_holder` | Keeps the installed printer visually consistent with the enclosure and restores the standalone layout on removal |
| 2026-11-20 | Installed and free-part docking are role-based | `DockingSources`/`DockingTargets` handle installed accessories; `FreePartDockingSources`/`FreePartDockingTargets` handle plugs and other loose parts without hardcoded roles |
| 2026-11-20 | Removed or separately placed wearable free parts are not reset by core-item moves | Prevents the adapter from snapping into an installed-looking socket while its printer socket is empty; regular free parts still reset |
| 2026-11-20 | The adapter is fixed while installed under an enclosure | A combined free/wearable part with `ItemDataHolder` installed under a `PrinterEnclosure` is handled as an installed part, not a free take; removal restores free behavior |
| 2026-11-20 | Enclosure parts have their own critical list | The printer's list remains printer-internal; the enclosure list covers battery closet, adapter, HEPA, fire-suppression tubes and thermometer, while the spool stays selected-slot validation |
| 2026-11-20 | Hard-stop wear uses serialized part references | `Printer.NozzleWearLocation`, `HeaterWearLocation`, `PrintBedWearLocation` and `PrintPlateLocation` replace GameObject-name lookups, so future printer hierarchies remain compatible |
| 2026-11-20 | Time-temperature wear uses an authored temperature source | `ItemWearLogicTimeTemperatureSO.TemperatureSource` replaces name heuristics; `EnclosureAware` preserves docking-dependent chamber/bed behavior |
| 2026-11-20 | Spool slots are data-driven with a typed runtime key | `SpoolSlotDefinition` ids/labels/locations and a `SpoolSlotId` value key decouple requests/drafts/UI from Left/Right; only one selected slot feeds a job. The stable string remains the persistence key. |
| 2026-11-20 | Enclosure spool sockets map to printer slots | `PrinterEnclosureSpoolSocket` supports explicit slot ids or any free slot and blocks when the printer has more installed spools than free sockets |
| 2026-11-20 | CoreXY uses a coupled horizontal carriage | `CoreXYPrinterMotion` composes X/Z onto one carriage; rectangular XYZ volumes remain shared with `CartesianPrinterMotion` and no belt/motor simulation is added |
| 2026-11-20 | M3 is complete with variant checks deferred | MK4S manual gate passed; CoreXY play-mode validation and 3/4-slot presentation wait for authored models, with no shared-contract changes |
| 2026-11-20 | M4 uses the real object prefab | The spool-bound printing material, runtime data and original collider all attach to the real prefab; the legacy printing ghost is not used |
| 2026-11-20 | Printed objects use a spool-bound reveal and final material | Printing uses the spool's `_reveal_height` shader; completion restores its regular `FinalMaterial` |
| 2026-11-20 | Caps appear only on settled pauses and finalized partials | Live printing uses uncapped reveal/backface lighting; pause-settled and hard-stopped surfaces use a sliced final-material cap; completed prints have no cap |
| 2026-11-20 | Printed-object sale destroys all ghosts and generated surfaces | `DestroyGhostInstances` clears mutual references; the object hierarchy owns the sliced cap mesh and collider |
| 2026-11-20 | Enclosure continuity is a one-way job flag | Continuous enclosure keeps `+5`; resuming outside applies `-9` once; reinstalling does not restore the bonus and toggles never stack |
| 2026-11-20 | Time calibration lives in the profile baseline, not a global factor | Exact editor calibration `5.88922 mm3/s` reproduces the bunny `36 720 s`; runtime layer/speed/infill/support multipliers are `1.0` at the reference |
| 2026-11-21 | Editor seed defaults are round | Reference flow `12 x 0.5 = 6.0 mm3/s` and packing `1.0`; exact bunny calibration `5.88922 / 0.94943` stays available |
| 2026-11-21 | Nozzle path uses per-layer bounds and a continuous layer parameter | Bounds precomputed once from the mesh through the actual object-to-bed matrix; X/Z phase and Z-hop stay continuous across layers; SPEED stays duration-driven and is not scaled again |
| 2026-11-21 | Head path varies per job | Object/printer hash plus a random salt seeds Lissajous frequencies, a secondary harmonic and low-frequency Perlin drift; layer bounds remain authoritative |
| 2026-11-21 | Enclosure doors are a shared interlock | `PrinterEnclosure.Doors` auto-finds generic `Door` components; closed doors are required for start/resume and opening during active printing pauses the job |
| 2026-11-21 | Thermal percentages are interval-based | Heating/cooling progress is measured from the temperatures captured at the state transition, not from job progress |
| 2026-11-21 | Shelf storage is independent of the printer | `Printer.FindFor` stops at the physical printer/enclosure hierarchy; items placed on a surface keep their own hover/take even when the printer is busy, while the enclosure surface itself still shows the busy status |
| 2026-11-20 | Filament seed is calibrated like the time seed | Editor filament packing `0.94943` (`85.36 / 89.90716`) restores the bunny `85.36 m`; Grid/Auto matches the slicer `28.53 m` |
| 2026-11-20 | Shell diagnostics share the runtime formula | `PrintEstimates.MaterialShellShare` drives both the scale rule and the editor tool; the bunny reports `14.0%` solid shell, `44.4%` Grid material shell and `6.22x` at scale 2 |
| 2026-11-20 | Partial prints are manually removable, not discarded | They use the same hover-to-place flow as completed prints; they expose no quality and sell with no income |
| 2026-11-20 | No `Clear bed`; selling on the bed is blocked | The player must take the object off the printer and place it before selling |
| 2026-11-20 | Supports stay attached until M5 | Generated columns reveal with the print and remain on the finished/partial object |
| 2026-11-20 | Finalize only after cooling and parking | Completed and partial objects become removable only when temperatures are back to ambient and the head is at home |
| 2026-11-20 | Presentation is auto-wired per printer | `Printer` gets/creates its `PrintedObjectPresenter`, so new printer prefabs need no model-specific visuals code |
| 2026-11-20 | Wear coefficients are a manual approval gate | M4 is not done until the project owner signs off `wear_coefficients_manual_gate.md` for every wear path |
| 2026-11-19 | Spool wear visuals use the upper condition threshold | Avoids showing the empty child while usable filament remains |
| 2026-11-19 | Calibration is recommended, not required | Uncalibrated prints warn and use a random `0.7–1.0×` quality factor at completion; calibrated prints use `1.0×` |
| 2026-11-19 | Only calibration-sensitive part changes invalidate calibration | Nozzle/heater/thermistors, bed/plate and axis mechanics invalidate in idle and active states; spool and enclosure accessories do not |
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
- M4 uses the spool `PrintingMaterial` (which exposes `_reveal_height`) rather than a separate object printing shader.
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
   - `HorizontalX.MinLocal` / `MaxLocal` are the two plate-edge offsets. Set `Invert` so `SetAxisNormalized(0, 0)` puts the nozzle at print-space -X. The MK4S carriage local X maps world -X, so it uses `MinLocal = -0.127`, `MaxLocal = 0.127`, `Invert = 1`.
   - `HorizontalZ.MinLocal` / `MaxLocal` are the two bed-edge offsets. A bed axis moves the nozzle the opposite way in print space; set `Invert = 1` (or reverse the recorded order) so positive print-space Z follows bed-local +Z. The MK4S bed uses `MinLocal = -0.07`, `MaxLocal = 0.151`, `Invert = 1`.
   - `VerticalY.MinLocal` = first-layer offset, `MaxLocal` = full-height offset.
   - Offsets are **signed** and measured from the authored local position. `MinLocal` may be numerically greater than `MaxLocal`.
   - Use `Invert` to make positive print-space coordinates follow positive bed-local coordinates; do not use it to hide an unknown direction.
6. Set `BuildVolume.x` and `BuildVolume.z` to the measured plate spans, and `BuildVolume.y` to the measured full height. The calibrated span on each axis must equal its matching `BuildVolume` component, or the print is rescaled.
7. Re-enter play mode. Enable `Test calibrated range`. Move X/Z to 0.5 and confirm the nozzle sits over the bed centre. Move Y to 0 and 1 and confirm the first-layer gap and full height. Return to the home pose and confirm the physical top-left / bed-3/4-forward corner; adjust `HomeX` / `HomeZ` under `UseCustomHomePosition` if the desired visual pose differs.
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
3. Repeat the bind hover check for `Bindable`, `BoundToThisDevice`, and `BoundToOtherDevice`; in unbind mode, check `SelectedForUnbind`. All states use the same targeting path.
4. With the printer standalone, confirm printer colliders do not block its own sphere. Install it in an enclosure and confirm the sphere uses `EnclosureRadiusMultiplier` (default `1.5×`) and that the enclosure's colliders do not block bind targeting for that printer. Remove and reinstall it while the highlight is visible; confirm the sphere refreshes its bounds, position and radius on every change. Other printers and walls must still block.

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
| `SupportOverhangPercent` | Generated overhang ratio; drives per-object support cost and the required-support warning |
| `WallThicknessMillimeters` | Fixed shell thickness for the scale rule; defaults to `1.2 mm` |
| `DefaultInfill` (Requirements) | Initial console selection |

Object width/height/depth, solid volume and surface area are **not authored**: `ObjectToPrintSizing.GetGeometry` derives them once from the catalog prefab. The reference layer height and speed are global (`0.2 mm`, `80 mm/s`).

The estimates shown by the console are:

```
shellShare = clamp01(min(objectVolume, objectSurfaceArea * wallThickness)
                     / (objectVolume * density(infill)))

scaleFactor = shellShare * scale² + (1 - shellShare) * scale³

supportFilament = 1 + 0.06 * overhang / 15.48308
supportTime     = 1 + 0.12 * overhang / 15.48308

filament = FilamentMetersAt100PercentInfill * density(infill)
         * (supports Auto ? supportFilament : 1)
         * scaleFactor

time     = PrintTimeSecondsAt100PercentInfill
         * infillTimeFactor
         * (0.2 / selectedLayerHeight)
         * (80 / selectedSpeed)^0.8
         * (supports Auto ? supportTime : 1)
         * scaleFactor
```

`objectVolume` and `objectSurfaceArea` are derived once from the catalog
prefab by `ObjectToPrintSizing.GetGeometry`; `wallThickness` comes from the
profile. At `scale = 1`, `scaleFactor` is exactly `1`, preserving the
calibrated profile. A missing/invalid mesh or wall thickness falls back to
the old `scale³` rule.

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
- `NozzleWearLocation`, `HeaterWearLocation`, `PrintBedWearLocation` — direct references used by the one-off hard-stop penalty.
- `Auto-Assign Critical Parts` (context menu) fills the critical and plate references from the MK4S default names; normal validation and wear use only the serialized references.

The editor auto-assign helper is MK4S-specific by design. Runtime validation, hard-stop wear and time-temperature wear do not depend on names. For a new printer model, wire the same reference fields in its prefab and author the wear-logic temperature source.

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

1. Add one or more `SpoolSlotDefinition` entries to `Printer.spoolSlots`. Each entry has a stable `Id`, a console `Label` and a `PartLocation` socket. The console supports up to four entries. Runtime requests/events use the typed `SpoolSlotId`; drafts store its stable string value.
2. The console hides the `SPOOL` row for zero/one slot. For two to four slots it shows the authored labels, defaults to the first occupied slot, and stores the selected id in the job draft and request.
3. An enclosure exposes `PrinterEnclosureSpoolSocket` entries. A socket can name a specific printer slot id or accept any free slot. Docking maps installed spools first and maps remaining empty slots to remaining sockets; installation blocks when there are not enough free sockets.
4. On the spool's `ItemFFFSpoolDataHolder`, set `MaterialType` (PLA/PETG/ABS/TPU) and keep `FilamentLengthSpoolMeters` at the physical spool length (default `300 m`).
5. Remaining filament is read from the selected spool's `ItemWearState.CurrentCondition`:
   `remaining = FilamentLengthSpoolMeters * CurrentCondition / 100`.
   In play mode use the `ItemWearState` context menu `Debug/Set Condition 100% / 50% / 10%` to test thresholds without editing assets.
6. Optionally assign a `FilamentMaterialProfileSO` for M3 temperatures. If it is null, M3 falls back by `MaterialType`.

If the selected slot is empty, the console reports `No filament spool installed on the <label> side.` and disables `Start print`. Another configured slot can be selected from the dynamic `SPOOL` row.

Only the selected spool participates in spool validation and wear/quality checks. A worn or empty spool on another slot is ignored until the player selects it.

### F.4 Start print

`Start print` is enabled only when `PrintJobValidator` returns no errors. The request stores the object, infill, layer height, speed, selected `SpoolSlotId`, placement, estimates and raw-quality range in `Printer.PendingJob`. The printer consumes it on the next tick, enters `Preheating`, and the console switches to live status. Nothing is instantiated on the bed until M4.

### F.5 Validation matrix

Check each row in play mode and confirm the console message:

| Setup | Expected |
|---|---|
| No active printer | `No printer selected.` |
| Printer bound to another device | `Printer is not connected to this device.` |
| Printer not calibrated | warning: `Printer is not calibrated; final quality may be 70–100% of the estimate.` |
| Printer printing/heating/paused/cooling/calibrating | `Printer is busy (...)` |
| Completed print still on bed | `Printer is busy (Finished).` |
| Partial print on bed | `A partial print is still on the bed.` |
| Job already queued | `A job is already queued on this printer.` |
| Selected spool slot empty | `No filament spool installed on the <label> side.` |
| Selected slot has non-spool item | `The <label> spool slot does not contain a filament spool.` |
| Selected spool condition 0 | `The <label> filament spool is empty.` |
| Selected spool condition ≤ 20% | `Low condition on the <label> spool (n%).` |
| Selected spool below estimate | `Not enough filament on the <label> spool: ...` |
| Critical location empty | `Missing part: ...` |
| Any installed part at 0% | `Worn out: ... (location, 0%).` |
| Any installed part at 20% or less | `Low condition: ... (location, n%).` |
| Critical part at 21–40% | `Critical part condition: ... (location, n%).` |
| Print plate location empty | `Print plate is missing.` |
| Oversized test object | `Object does not fit: ...` |
| Supports required | `Supports are required; ...` warning |

### F.6 Calibration record

Fill these in after the first slicer comparison and copy the final numbers into §9.

See [`estimate_matrix.md`](estimate_matrix.md) for the full LAYER x SPEED x INFILL x SUPPORTS matrix and the sanity review.

| Item | Value |
|---|---|
| Bunny size reported by `ObjectToPrintSizing` (not edited) | derived from prefab bounds |
| Bunny filament at 100% infill | `85.36 m` (original slicer reference) |
| Bunny time at 100% infill at `0.2 mm` / `80 mm/s` (Slow) | `36 720 s` (`10 h 12 min`, original slicer reference) |
| Supports required? | yes |
| Auto support cost at bunny overhang | filament ×1.06, time ×1.12 at `15.48308%`; formula scales by each object's overhang |
| Required + None quality (expect `55-55`) | `55-55` |
| Slicer estimate at Grid / Normal / Slow (original reference) | `28.53 m`, `3 h 53 min` |
| Console estimate at Grid / Normal / Slow (original reference) | `28.5 m`, `3 h 53 min` |
| Tuning factor (slicer / console, original reference) | `1.00` filament, `1.00` time |
| Draft layer vs Normal layer (Grid, Slow, Auto) | `2 h 46 min` vs `3 h 53 min` |
| Fine layer vs Normal layer (Grid, Slow, Auto) | `6 h 28 min` vs `3 h 53 min` |
| Raw quality: Draft / Normal / Fine (Normal speed, Auto) | enclosed `75-80 / 85-90 / 89-90`; open `61-66 / 71-76 / 75-80` |
| Raw quality: Slow / Normal / Fast (Normal layer, Auto) | enclosed `89-90 / 85-90 / 75-80`; open `75-80 / 71-76 / 61-66` |
| Current profile | `85.36 m` filament, `36 720 s` (`10 h 12 min`) at 100% (slicer reference) |
| Current profile sources | Filament and time: PrusaSlicer reference |
| Editor seeds | Exact `5.88922 mm3/s` / `0.94943`; round defaults `6.0 mm3/s` (`12 x 0.5`) / `1.0` |
| Console estimate at Grid / Normal / Slow (current profile) | `28.53 m`, `3 h 53 min` |
| Current tuning factor (slicer / console) | filament `1.00`, time `1.00` |
| Current calibration status | Reference point matched; full console matrix reviewed in `estimate_matrix.md`; speed-preset and scale/support spot-checks pending |
| Spool material used | PrusaSlicer default (PLA) |
| Critical locations confirmed | yes |

Expected values below are the slicer-calibrated reference with `Auto` supports. Time, filament and the full console matrix are in [`estimate_matrix.md`](estimate_matrix.md). Quality values are for an enclosed printer; open-printer values are about `14` raw points lower (see the matrix). Re-run this table after the speed-preset spot-check:

| Layer / speed / infill (supports Auto) | Filament | Simulated time | Real time at `120×` | Raw quality (enclosed) |
|---|---:|---:|---:|---:|
| Draft / Fast / None | `17.1 m` | `0 h 53 min` | `0.4 min` | `65–70` |
| Draft / Normal / Grid | `28.5 m` | `1 h 36 min` | `0.8 min` | `75–80` |
| Normal / Fast / Grid | `28.5 m` | `1 h 37 min` | `0.8 min` | `75–80` |
| Normal / Normal / Grid | `28.5 m` | `2 h 14 min` | `1.1 min` | `85–90` |
| Fine / Normal / Gyroid | `32.0 m` | `4 h 07 min` | `2.1 min` | `89–90` |
| Fine / Slow / Gyroid | `32.0 m` | `7 h 10 min` | `3.6 min` | `90–90` |

These are the values to compare against a slicer. Tune `InfillSettings`/`PrintSpeedSettings` globally rather than per object. The fastest preset is now `240×`; halve the real-time column for that setting.

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
   - after a confirmed placement, apply the shell/infill two-term scale factor;
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
3. Compute mesh volume and surface area from the triangle soup (signed tetrahedra sum and triangle areas). For a watertight mesh this is the solid volume; for open meshes, report the result as approximate.
4. Compute the overhang ratio: triangles whose normal is below the overhang angle (`45°` default) relative to the plate up; set `SupportsRequired` above a configurable threshold and write `SupportOverhangPercent`.
5. Seed 100% infill filament from `volume / (π × (filamentDiameter/2)²) × filamentPackingFactor`, with the packing factor exposed in the tool settings.
6. Seed 100% infill time from `volume / (referenceFlowMm3PerSecond x extrusionDuty)`; expose both values and show the derived effective flow.
7. Write `WallThicknessMillimeters` for the shell/infill scale rule; report solid shell, material shell for the default infill, and the scale-2x factor.
8. Write through `SerializedObject`, mark the asset dirty, and report a per-object summary; support batch over the whole catalog.

Model meshes must have **Read/Write enabled** for volume/overhang. The bunny FBX now has it enabled, so the tool can report volume and overhang. Defaults: `1.75 mm` filament, `1.0` filament packing, `12 mm3/s` reference flow, `0.5` extrusion duty (`6.0 mm3/s` effective), `1.2 mm` wall thickness, `45°` overhang, `5%` supports threshold.

The tool separates the time seed into `reference flow x extrusion duty`. Reference flow is the hotend/slicer volumetric limit; extrusion duty is the time-weighted fraction actually extruding, with travel, acceleration, cooling and layer changes taking the rest. The round defaults are `12 mm3/s x 0.5 = 6.0 mm3/s`. The bunny's exact calibration is `5.88922 mm3/s` (`12 x 0.490768`), derived from `216252.06 mm3 / 36720 s` at `0.2 mm` / `80 mm/s` / `100%` infill, supports off. Enter the exact duty when exact slicer reproduction is needed. The runtime model's layer/speed/infill/support multipliers are neutral at that reference and still need a multi-point slicer matrix to refit; one slicer point cannot identify them.

The filament packing factor is the matching lumped coefficient for material: `85.36 / 89.90716 = 0.94943`. It absorbs the mesh-to-slicer material-volume difference. The tool default is `1.0`, which is a round seed and about `5.3%` above the slicer reference; use `0.94943` when exact reproduction is needed. The bunny profile stores `85.36 m`, so the runtime Grid/Auto matrix still matches the slicer. Like the effective flow, the packing factor should be recalibrated if the mesh import or reference slicer profile changes.

Limitations:

- Time and filament from volume are seeds, not a per-object slicer result. The exact bunny calibration values are recorded; the round defaults are approximate. Confirm each new object with one slicer run.
- Automatic orientation selection is not part of the first version; the player rotates on the bed instead. A future version can try candidate yaw/face rotations and pick the lowest overhang score.
- Default infill stays authored; material is selected by the selected spool and is not inferred.
- The tool must not touch scenes; it only edits meta assets.

Recommended workflow:

1. Run the generator on the catalog.
2. Inspect bounds/supports per object.
3. Slice one reference object at `100%` infill, `0.20 mm` layer, `80 mm/s`, supports off, to calibrate the profile seeds.
4. Record those values in F.6; separately compare the Grid / Normal / Slow / Auto console estimate against a Grid slicer run.
5. Adjust the global seed coefficients, not individual objects unless the mesh is unusual.

### F.9 Supports

Supports are a job setting, not free-form geometry:

1. `ObjectToPrintPrintProfile.SupportsRequired` is the geometric truth: the editor generator sets it from the overhang ratio. `SupportOverhangPercent` drives the per-object support cost and the required-support warning.
2. The console shows `SUPPORTS: None / Auto`.
3. Default:
   - required → `Auto`,
   - not required → `None`.
4. Validation and quality:
   - required + `None` → allowed, device-UI warning `This object needs supports.`, raw quality `-25` and capped at `55`;
   - not required + `Auto` → warning `Supports are not mandatory here.`, normal extra cost.
5. Cost is derived from the generated overhang ratio:
   - `supportFilament = 1 + 0.06 × overhang / 15.48308`,
   - `supportTime = 1 + 0.12 × overhang / 15.48308`.
   The bunny's `15.48308%` reference reproduces the previous `1.06` / `1.12`; objects with less overhang pay less and objects with more pay more. If supports are required but the overhang is unavailable, the reference overhang is used as a conservative fallback.
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
5. Confirmed estimates use the shell/infill two-term scale factor; rotation changes fit only.

Manual gate to record:

| Check | Result |
|---|---|
| Ghost starts centred on first entry | Pass |
| Move to each plate corner and beyond; wrong state and blocked confirm | Pass |
| Minimum 2 cm and maximum plate/height scales; wrong state and blocked confirm | Pass |
| Estimates match the shell/infill rule at min / 1.0 / max scale | Re-check |
| Support cost follows each object's overhang | Re-check |
| Cancel restores the previous placement; confirm survives console close/reopen | Pass |
| `object_place_reason` lists every current reason with `- ` prefix | Pass |
| Camera orbit stays locked on the volume centre; pitch/zoom limits hold | Pass |
| Confirm/cancel return blend matches the entry blend | Pass |
| `point_canvas` stays hidden during `GameStateObjectPlace` | Pass |
