# Manufacturing Plan — Device-Controlled FFF Printing

**Status:** Draft for implementation  
**Last updated:** 2026-11-16  
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
- Screen-space console overlay and a smooth camera focus on the device screen.
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
| Job queue | None; one active job per printer |
| Device-to-printer model | One device → many printers; one printer → one device |
| Printer ownership of job | Printer owns the job and keeps printing if the device is unbound |
| Screen-off behavior | Screen/display only; device and printer continue running |
| Enclosure | Architecture hooks now; gameplay effects in M5 |
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
8. The console overlay fades in.

### 4.2 Console

The console shows:

- Device name and power state.
- Bound printer list with status indicators.
- Active printer status card.
- Job picker from the `ObjectsToPrint` catalog.
- Infill selector.
- Estimated print time and filament use.
- Validation messages.
- `Start print`, `Hard stop`, `Add printer`, `Disconnect printer`, `Turn off screen`, and `Quit`.

### 4.3 Binding a printer

1. Player clicks `Add printer` in the console.
2. The console closes, the camera blends back to the player, and movement/look is enabled.
3. A small banner says `Select a printer (LMB), Q to cancel`.
4. Bindable printers show a translucent cyan sphere; printers already bound to this device show green; printers bound to another device show red; the hovered target brightens.
5. The player looks at a printer and left-clicks.
6. Binding succeeds, a toast confirms it, and the console reopens with the new printer selected.
7. If the printer is already bound to another device, binding fails with a clear message.

### 4.4 Disconnecting a printer

1. Player clicks `Disconnect printer` in the console.
2. If the active printer is printing, a confirmation modal warns that the print will continue.
3. The console closes and the camera returns to the player.
4. Bound printers show a translucent amber/orange sphere.
5. Player left-clicks a bound printer; that binding is removed and the console reopens.
6. The printer keeps its current job and state. The device simply stops controlling it.

### 4.5 Starting a print

1. Player selects the active printer.
2. Player selects an object and infill.
3. Console validates the job.
4. `Start print` becomes available only when the job is valid.
5. Printer transitions `Idle → Preheating → Printing → Cooling → Finished`.
6. Console shows live state, temperatures, progress, remaining time, and filament use.
7. When finished, the object is fully revealed on the bed and can be removed.

### 4.6 Hard stop

1. Player clicks `Hard stop` and confirms.
2. The printer immediately stops motion and turns heaters off.
3. A partial print remains on the bed.
4. A one-off condition penalty is applied to the nozzle/heater/bed/plate.
5. The printer cools down and returns to `Idle` with `HasPartialPrint = true`.
6. No new job can start until the partial print is removed.

### 4.7 Leaving the console

- `Q` quits the console.
- The console fades out, the camera blends back to the player, movement/look is restored, and the cursor is hidden/locked.
- Printers continue running while the player walks away.
- The screen-off state is preserved in the world.

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

**Decision: include the enclosure in the architecture now, but deliver its gameplay effects in M5.**

Why defer:

- The base printing loop, job validation, wear, and visuals must be stable first.
- The enclosure mainly changes thermal and quality parameters; it does not change the printer FSM or binding model.
- Adding enclosure gameplay too early would mix tuning with core plumbing.

Design hooks to add early:

- `PrinterEnvironment` component on the printer:
  - `bool HasEnclosure`
  - `float AmbientTemperature`
  - `float ChamberTemperature`
  - `float DraftFactor`
  - `float ParticulateFactor`
- `PrinterTemperatureModel` reads chamber temperature from this component.
- `PrintQualityModel` (M5) computes warping/quality from:
  - material shrink factor
  - chamber temperature vs material recommendation
  - cooling rate
  - draft
  - part wear
  - temperature deviation
- The console can display `Chamber: ambient` or `Chamber: enclosed` from M3 onward, even before quality effects exist.

M5 enclosure effects:

- Better temperature maintenance for ABS/ASA-like materials.
- Reduced warping risk.
- Reduced draft sensitivity.
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

- Add `device_console_camera` as a `CinemachineCamera` with a `CinemachineHardLookAt` component.
- On focus:
  - Read the device's screen anchor.
  - Place the camera in front of the screen at a distance derived from the screen bounds and FOV.
  - Set the look-at target to the screen center.
  - Raise the camera priority above the player camera.
- On release:
  - Lower the console camera priority.
  - Let the Cinemachine Brain blend back to the player camera.
- Default blend: `0.35–0.5 s` each way.
- The console UI fades in after the blend completes, or cross-fades during the blend.
- While the console is open, player movement/look is disabled and the cursor is visible.
- While bind targeting is active, the cursor is hidden/locked and player movement/look is enabled.

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
  - The console shows a black screen with a `Power on` button and a hint.
  - `Q` still closes the console; the screen remains off in the world.
- When the player next interacts, the console opens in the off state and can be powered on.
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
  - PrinterStateMachine StateMachine
  - PrintJob ActiveJob
  - Device BoundDevice
  - event Action<PrinterState> OnStateChanged
  - event Action<PrintProgress> OnProgress
  - event Action<PrintResult> OnJobFinished

PrintJob
  - ObjectToPrintMetaSO Object
  - InfillPattern Infill
  - float TotalPrintSeconds
  - float TotalFilamentMeters
  - float ElapsedPrintSeconds
  - float ConsumedFilamentMeters
  - float NozzleTargetTemperature
  - float BedTargetTemperature
  - bool IsPartial

PrinterStateMachine
  - Idle
  - Preheating
  - Printing
  - Paused        (reserved for later)
  - Cooling
  - Finished
  - Maintenance
  - Fault         (reserved for M5 failures)
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

### 6.3 Printer state machine

States and transitions:

| From | Event | To |
|---|---|---|
| `Idle` | valid job + start | `Preheating` |
| `Preheating` | nozzle and bed at target | `Printing` |
| `Preheating` | hard stop | `Cooling` (partial = false) |
| `Printing` | progress reaches 100% | `Cooling` |
| `Printing` | hard stop | `Cooling` (partial = true) |
| `Printing` | fatal fault (M5) | `Cooling` / `Fault` |
| `Cooling` | temperatures near ambient | `Finished` or `Idle` |
| `Finished` | print removed | `Idle` |
| `Idle` / `Finished` | worn-out/missing critical part | `Maintenance` |
| `Maintenance` | parts repaired | `Idle` |

Rules:

- `Finished` means a completed print is still on the bed.
- `Idle` means the bed is clear.
- A hard-stopped partial print sets `HasPartialPrint = true`; the printer returns to `Idle` after cooling but rejects new jobs until the partial print is removed.
- `Paused` is reserved for a later pause/resume feature.
- `Fault` is reserved for M5 failure modes.

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
- Printer is `Idle` or `Finished` with no partial print.
- Printer has all critical parts installed.
- No critical part is worn out.
- Spool is installed.
- Spool material is compatible with the object/profile.
- Remaining filament is enough.
- Object fits the build volume.
- Print plate is present.
- No job is already active on that printer.

Hard failures block `Start print`. Warnings do not block but are shown in the console.

### 6.5 Temperature model

Per printer:

```
PrinterTemperatureState
  - Ambient
  - Chamber
  - Nozzle
  - Bed
```

- Targets come from the filament material profile.
- Preheat uses a linear heating rate toward target.
- Printing holds targets; small deviations are allowed.
- Cooling uses an exponential decay toward ambient.
- Enclosure (M5) slows cooling and raises chamber temperature.
- Preheat completes when both nozzle and bed are within tolerance (default ±2 °C).
- In M3, a failure to reach target logs a warning and eventually faults; full thermal-failure gameplay is M5.

### 6.6 Wear and filament

Use the existing wear system and the correct delta types:

| Part | Delta | Notes |
|---|---|---|
| Nozzle | `WearDeltaFilamentTemperatureAbrasiveness` | filament length, nozzle temp, spool abrasiveness |
| Bed / plate / heater / thermistor | `WearDeltaTimeTemperature` | simulated seconds at operating temperature |
| Spool | `WearDeltaFilament` | consumed metres / spool length |
| Axes (optional) | `WearDeltaDistanceTemperature` | estimated path distance, ambient/chamber temp |

Rules:

- Batch wear application about once per simulated second, not every frame.
- Use simulated process time, not wall-clock time.
- Note: `ItemWearState.ApplyWear` currently calls `CalculateWear`, not `CalculateWearByRatio`, so the wear logic's private `WearRatio` is not applied. Either switch `ApplyWear` to `CalculateWearByRatio` (a global behaviour change) or tune the per-SO wear rates directly. Decide this in M3 and document the choice.
- Spool remaining filament:

```
remainingMeters = wear.CurrentCondition / 100f * spoolData.FilamentLengthSpoolMeters;
```

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
- Use the installed spool's `PrintingMaterial`.
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

- Rotate the spool proportional to consumed filament (axis calibrated in M0).
- Drive the spool shader's `_reveal_height` from the remaining filament ratio.
- The spool property is normalized `0..1`; calibrate what visually reads as full vs empty.
- If the vertical reveal looks wrong, fall back to swapping spool wear-level prefabs in M5.

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

Use one screen-space overlay prefab for all device types.

Suggested layout:

- Dark translucent scrim.
- Centered console panel, 16:9.
- Header: device name, screen power, connection status.
- Left column: bound printer list with status dots and active selection.
- Main area: active printer status, temperatures, progress, job details.
- Footer: job picker, infill segmented control, estimate, validation, action buttons.
- Toast area for bind/unbind/validation messages.
- `screen_off_root`: black panel with `Power on` and hints.

Behavior:

- `DeviceConsolePresenter.Open(device)` binds to the device and active printer.
- `Close()` unsubscribes from printer events.
- Refresh on events and at ~5 Hz; do not rebuild strings every frame.
- Disable buttons with reasons rather than hiding them.
- Confirm hard stop and unbind-while-printing.
- Show printer state colors consistently (idle, heating, printing, finished, maintenance, fault).
- Do not show a printer as “connected” if its binding was removed.

### 6.9 Camera controller

`DeviceFocusController`:

- References:
  - `CinemachineCamera ConsoleCamera`
  - `CinemachineBrain Brain`
  - `DeviceConsolePresenter Console`
- Methods:
  - `Focus(Device device, Action onReady)`
  - `Release(Action onReady)`
- Framing:
  - Get the screen anchor (`DeviceScreenAnchor` or `ItemComputingDataHolder.ScreenGameObject`).
  - Compute the screen center and normal from renderer bounds.
  - Place the camera at `center + normal * distance`.
  - Distance from screen size, FOV, and a fill factor; clamp to a safe minimum.
  - Look at the screen center with `CinemachineHardLookAt`.
- Priority:
  - Console camera high (e.g., `100`), player camera low (`0`).
  - Release restores the console camera priority to `0`.
- Blend:
  - Use the Brain's default blend or a per-camera blend.
  - Wait for the blend before enabling the console, or cross-fade the UI.
  - Cancel any pending focus/release coroutine when the console is closed early or the device becomes invalid.
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
    public float ModelHeightMeters;
    public float LayerHeightMillimeters = 0.2f;
    public float FilamentMetersAt100PercentInfill;
    public float PrintTimeSecondsAt100PercentInfill;
    public bool SupportsRequired;
    public InfillPattern DefaultInfill = InfillPattern.Grid;
}
```

Add:

```
public ObjectToPrintPrintProfile PrintProfile;
```

Why inline instead of a separate asset:

- The catalog is currently small.
- It keeps object metadata self-contained.
- A separate `ObjectToPrintPrintProfileSO` can be introduced later without changing job code if needed.

### Infill

```
public enum InfillPattern
{
    None,
    Grid,
    Gyroid
}
```

Initial tuning table (placeholders, calibrate in M2):

| Pattern | Effective density | Time factor |
|---|---:|---:|
| `None` | 0.15 | 0.60 |
| `Grid` | 0.50 | 1.00 |
| `Gyroid` | 0.55 | 1.12 |

```
filament = profile.FilamentMetersAt100PercentInfill * density;
time = profile.PrintTimeSecondsAt100PercentInfill * timeFactor;
```

These values are intentionally simple and tunable. The important property is that infill affects time and filament consistently.

### Filament material profile

Add a `FilamentMaterialProfileSO` (one asset per material) with:

- `NozzleTemperature`
- `BedTemperature`
- `RecommendedChamberTemperature`
- `Density`
- `WarpFactor`
- `CoolingRateMultiplier`

The spool can reference it, or a lookup by `ItemType` can resolve it.

### Printer runtime state

Plain C# classes, not MonoBehaviours:

- `PrintJob`
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
| **M3** | FSM + temperatures + wear | Full job cycle, hard stop, wear/filament applied correctly |
| **M4** | Print visuals | Object reveal, motion, spool, partial print, performance |
| **M5** | Failure + enclosure + polish | Enclosure effects, failures, quality, audio/VFX |

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
- `PrinterBindHighlight` with a translucent sphere sized to printer bounds.
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
- Bind highlight spheres fit the printer and do not block raycasts.

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

**Goal:** The player can open a device console, add/remove printers, and see a live printer list.

**Deliverables**

- Finalize the M0 `Device` interaction entry point if needed.
- `GameStateDeviceInterface(Device)` contextual state.
- `GameStateDeviceBind(Device, DeviceBindMode, GameState)` contextual state.
- `DeviceController`:
  - `OpenConsole`, `CloseConsole`
  - `EnterBindMode`, `ExitBindMode`
  - bind/unbind selection events
- `DeviceFocusController` with Cinemachine blend.
- `DeviceConsolePresenter` with:
  - printer list
  - active selection
  - connection status
  - `Add printer` / `Disconnect printer`
  - `Turn off screen`
  - `Quit`
- `PrinterBindTargeting` with hover highlight.
- `PrinterBindHighlight` colors:
  - bindable: cyan
  - already bound to this device: green
  - bound to another device (unavailable): red
  - selected for unbinding in unbind mode: amber
  - hovered: brighter
- `PlayerController.SetDeviceInterfaceState` / `SetDeviceBindState`.
- Scene wiring for `DeviceController` and the console canvas. The scene already contains empty `states/device_interface` and `interface/device_interface` marker objects; use or replace them rather than adding parallel placeholders.

**Acceptance criteria**

- Hovering a computing device shows `Use device` and `F`.
- `F` focuses the device screen and opens the console.
- `Q` closes the console and returns the camera and controls.
- `Add printer` enters bind mode, highlights bindable printers, and binds on LMB.
- `Q` cancels bind mode and returns to the console.
- Binding a printer already bound to another device fails with feedback.
- Bound printers appear in the console list.
- `Disconnect printer` enters unbind mode and removes the selected binding.
- Multiple printers can be bound to one device and selected in the list.
- Unbinding while printing does not stop the print.
- Screen off shows a black console and black physical screen; power-on restores both.
- No duplicate event subscriptions or dangling references.

**Manual gate**

1. Test camera framing and blend time for all four device types.
2. Test cursor show/hide/lock in console, bind mode, and free walk.
3. Test binding from different player positions and distances.
4. Test that printers behind walls cannot be bound.
5. Test UI at 16:9, 21:9, and 4:3; text must remain legible.
6. Test screen-off and power-on transitions.
7. Test binding persistence while walking away and returning.
8. Test unbinding a printing printer and rebinding it.

**Do not proceed to M2 until binding and camera flow feel comfortable and do not break free walk.**

---

### M2 — Job selection and validation

**Goal:** The console can present objects and infill, calculate estimates, and reject invalid jobs with clear reasons.

**Deliverables**

- `ObjectToPrintPrintProfile` fields on `ObjectToPrintMetaSO`.
- `InfillPattern` and `InfillSettings`.
- `FilamentMaterialProfileSO` or equivalent material data.
- `PrintJobRequest`.
- `PrintJobValidator`.
- Console job picker, infill selector, estimate panel, validation panel.
- `Start print` gating.
- Placeholder printer status if M3 is not implemented yet.

**Acceptance criteria**

- The object list is populated from `ObjectToPrintMetasListSO`.
- Selecting an object shows name, icon, height, and estimates.
- Infill selection updates time and filament.
- `Start print` is disabled when:
  - no printer is selected
  - printer is busy
  - a partial print is on the bed
  - a critical part is missing or worn out
  - no spool is installed
  - remaining filament is insufficient
  - the object does not fit the build volume
  - the print plate is missing
- Validation messages are specific and readable.
- Estimates are deterministic for the same object/infill/spool.

**Manual gate**

1. Fill in print profiles for the current object catalog.
2. Compare estimates against a slicer or a known reference print; record the tuning factor.
3. Verify spool remaining calculation at 100%, 50%, and 10% condition.
4. Verify build-volume rejection with an oversized test object.
5. Verify material compatibility warnings.
6. Verify that the console cannot start a job on an unbound printer.

**Do not proceed to M3 until the estimates and validation rules are believable.**

---

### M3 — Printer FSM, temperatures, and wear

**Goal:** A job actually runs through preheat, print, cool, and finish; wear and filament are consumed.

**Deliverables**

- `PrinterStateMachine` with the states and transitions in section 6.3.
- Simulated-time ticking with `PrintSpeedMultiplier`.
- `PrinterTemperatureModel`.
- `PrinterWearIntegrator`.
- `ItemWearState.ApplyConditionLoss`.
- `SpoolFilamentState` or equivalent remaining-filament wrapper.
- Hard-stop flow with partial-print flag.
- Console live status binding.
- Printer continues ticking when unbound.

**Acceptance criteria**

- Start job → `Preheating` → `Printing` → `Cooling` → `Finished`.
- Preheat completes at target temperature within tolerance.
- Print progress reaches 100% at the expected simulated duration.
- Cooling returns temperatures toward ambient.
- Nozzle, bed/plate/heater/thermistor, and spool wear are applied to the correct parts.
- Spool condition and remaining filament decrease consistently.
- Hard stop at any point:
  - stops motion and heaters
  - applies the one-off penalty
  - leaves a partial print
  - blocks new jobs until removal
- Unbinding the device during a print does not stop the printer.
- Rebinding shows the correct live state.
- A worn-out critical part blocks a new job.

**Manual gate**

1. Run a short job and compare condition loss to the configured wear logic; tune `WearPerHour` / `WearPerFilamentLength`.
2. Record preheat, print, and cooling times at `120×`; adjust the multiplier if it feels too fast/slow.
3. Tune the hard-stop penalty so it is noticeable but not catastrophic.
4. Profile 2–4 printers printing simultaneously; check CPU and GC.
5. Verify no NaN/Inf in temperatures, progress, or condition.
6. Verify partial print blocks the next job.

**Do not proceed to M4 until the numbers and timing feel right and wear is applied to the intended parts.**

---

### M4 — Print visualization

**Goal:** The print looks convincing: object reveal, nozzle/bed motion, spool consumption, and removal.

**Deliverables**

- `PrintedObjectPresenter`.
- `PrintRevealController`.
- `PrinterMotionSimulator`.
- `CartesianPrinterMotion` integration.
- Spool rotation/reveal.
- Completion material restore and collider enable.
- Partial-print removal.
- If printing ghosts are reused, fix the existing `ObjectToPrintGhostPrintingDataHolder` bug: its `OnValidate`/`Awake` currently assign `ObjectToPrintGhostType.Quality` instead of `Printing`. Prefer instantiating the real object prefab for prints and bypassing this ghost path.

**Acceptance criteria**

- The object appears at the bed center, correct scale, correct orientation.
- Reveal starts empty and grows bottom-up.
- Reveal follows the moving bed correctly.
- The nozzle appears to work over the printed area.
- Axis motion stays within soft limits and does not clip.
- The spool visually consumes filament.
- Hard stop freezes the partial object at the correct height.
- Completed object is fully visible and removable.
- No per-frame material instancing or GC spikes.

**Manual gate**

1. Compare the reveal top to the nozzle height; adjust the offset.
2. Check bed movement direction and speed.
3. Check the object from several player angles; no popping or z-fighting.
4. Check multi-mesh objects; all renderers reveal consistently.
5. Check the spool visual at 100%, 50%, 10%, and 0%.
6. Check partial print height and removal.
7. Check completed object materials, colliders, and selling/placement behavior.

**Do not proceed to M5 until the print reads clearly as a print from the player's normal viewing distance.**

---

### M5 — Failure, enclosure, and polish

**Goal:** Add depth and edge cases after the core loop is stable.

**Candidate deliverables**

- `PrinterEnvironment` enclosure integration.
- Chamber temperature, draft, and cooling effects.
- `PrintQualityModel` and `IsHaveQuality`.
- Failure modes:
  - filament runout
  - clog
  - thermal fault
  - warping
  - layer shift
- Quality-dependent sale value.
- Pause/resume.
- Optional print queue.
- Audio/VFX:
  - fan hum
  - stepper motion
  - completion chime
  - error buzzer
- Screen animations and notifications.
- Save/load if desired.
- Optional resume-from-partial-print.

**Acceptance criteria**

- Enclosure changes thermal behavior and quality in a visible, understandable way.
- Failure modes are telegraphed and recoverable.
- Quality affects the object's value or usability.
- The game remains performant with multiple printers.

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
| Laptop screen anchor + distance | +Z → world +Z | distance deferred to M1 |
| PC screen anchor + distance | +Z → world +Z | distance deferred to M1 |
| Smartphone screen anchor + distance | +Z → world +Y | device lies flat; distance deferred to M1 |
| Tablet screen anchor + distance | +Z → world +Y | device lies flat; distance deferred to M1 |
| Camera blend in / out |  | deferred to M1 |
| Bind highlight bounds | printer bounds + `0.08 m` padding | translucent sphere, no raycast |
| Bind distance | `1 m` proposed | not enforced in M0; enforce in M1 |
| Print speed multiplier | 120× |  |
| Hard-stop penalty |  | deferred to M3 |

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
- Bind/unbind one printer.
- Bind/unbind multiple printers.
- Bind rejection cases.
- Camera blend and cursor behavior.
- Screen-off persistence.
- UI aspect-ratio checks.

### M2

- Job list and estimates.
- Infill changes.
- Validation for every failure reason.
- Build volume rejection.
- Filament sufficiency.
- Material compatibility.

### M3

- Full job cycle.
- Hard stop at preheat, mid-print, and near-complete.
- Wear application per part.
- Spool depletion.
- Unbind/rebind during print.
- Worn-out part blocks job.
- Multiple printers simultaneously.

### M4

- Visual reveal at start/mid/end.
- Bed motion and nozzle path.
- Spool visual.
- Partial print freeze and removal.
- Completed object material/collider restore.
- Performance profile.

### M5

- Enclosure thermal/quality effects.
- Failure modes.
- Quality and sale value.
- Pause/resume.
- Audio/VFX.
- Save/load, if implemented.

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

**M0 scripts are implemented. Prefab/scene wiring is next.**

M0 is the highest-leverage milestone because it de-risks every later step:

- It proves the axis mapping and signs.
- It proves the bed anchor and print orientation.
- It proves the shader reveal and cutoff formula.
- It proves the device hover and screen framing.
- It gives concrete values for the calibration table.

Remaining M0 work:

1. Wire the printer prefab per Appendix E.1–E.4.
2. Wire the four computing prefabs per Appendix E.5.
3. Test the bind highlight per Appendix E.6.
4. Record the §9 calibration values.
5. Stop and review before M1.

Do not start M1 until the manual M0 gate is recorded.

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

M0 files actually added are listed in Appendix D. The tree below is the full target layout.

```
Assets/Scripts/Coursework_Master_Degree/
├── Items/Computing/
│   ├── Device.cs
│   ├── DeviceScreenDisplay.cs
│   └── DeviceScreenAnchor.cs
├── Items/FFF/Printer/
│   ├── Printer.cs
│   ├── PrinterState.cs
│   ├── PrinterStateMachine.cs
│   ├── PrinterJob.cs
│   ├── PrinterJobValidator.cs
│   ├── PrinterTemperatureModel.cs
│   ├── PrinterWearIntegrator.cs
│   ├── PrinterEnvironment.cs
│   ├── SpoolFilamentState.cs
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
│   └── DeviceController.cs
├── Game/Logic/Camera/
│   └── DeviceFocusController.cs
├── Game/Logic/Interface/DeviceConsole/
│   ├── DeviceConsolePresenter.cs
│   └── DeviceConsoleUI.cs
├── Game/StateMachine/State/
│   ├── GameStateDeviceInterface.cs   (modify)
│   └── GameStateDeviceBind.cs        (modify)
└── ScriptableObjects/ObjectsToPrint/
    └── ObjectToPrintMetaSO.cs        (modify)
```

---

## Appendix C — Decision log

| Date | Decision | Rationale |
|---|---|---|
| 2026-11-16 | Device → many printers; printer → one device | Realistic, future-proof, avoids refactor |
| 2026-11-16 | Printer owns job and keeps printing when unbound | Physical printer autonomy |
| 2026-11-16 | Enclosure hooks now, gameplay in M5 | Keep core loop stable |
| 2026-11-16 | `PrinterMotionBase` + `CartesianPrinterMotion` | Supports future printer constructions |
| 2026-11-16 | Dedicated Cinemachine console camera | Consistent framing, clean blend |
| 2026-11-16 | Screen-only power-off | Prints continue; no full power simulation |
| 2026-11-16 | No new input actions for M0–M4 | Existing inputs and UI EventSystem cover the flow |
| 2026-11-16 | No job queue | Keep first pass simple |
| 2026-11-16 | Partial print remains after hard stop | Approved default |
| 2026-11-16 | 120× simulated print time | Approved default |
| 2026-11-16 | MK4S axes: X on extruder, Z on bed carriage, Y on gantry | `base` is rotated 180° about `(0,1,1)/√2`; node-local axes do not match their names |

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
| `.../PrinterEnvironment.cs` | Enclosure/ambient/draft/particulate data (M5 uses it) |
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
- `DeviceScreenDisplay.ApplyStateOnAwake` is off by default, so screens keep their authored look during calibration.
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
2. Add `DeviceScreenDisplay` to the root. `Computing` auto-assigns. Leave `ScreenRenderers` empty so it collects from `ScreenGameObject`. Leave `ScreenOffMaterial` empty for a runtime black material. Keep `ApplyStateOnAwake = false`.
3. Create an empty child named `screen_anchor` under the **screen mesh GameObject**, not the device root: `.../screen/screen_anchor`. This keeps the anchor attached to the display when a laptop lid or monitor moves. Put it at the display centre (reset its local position first) and rotate it until its local +Z (blue arrow) points **out of** the screen toward the viewer. Add `DeviceScreenAnchor`. Use `InvertNormal` if the blue arrow must point inward. A root-level anchor works only if the screen never moves relative to the root.
4. Assign `Device.Screen` and `Device.ScreenAnchor`.
5. Test hover on each child mesh: the prompt should read `Use device`. Press `F`; the console should log the M0 placeholder.
6. Enter **play mode** first, then test screen-off from the component context menu: select the device root, open the `DeviceScreenDisplay` component's ⋮ menu, and choose `Screen Off`, `Screen On`, or `Toggle Screen`. The context menu relies on the materials cached in `Awake`, so it is not a safe edit-mode test. Confirm the display goes black and `Screen On` restores the authored material. Stop play mode afterward; nothing is saved.
7. Optional, and not required for the M0 gate: add a `device_console_camera` (`CinemachineCamera`, priority 0, `CinemachineHardLookAt`) under `states`. Defer this to M1, when the console UI exists and the framing distance can be tuned. For M0, verify the anchor by selecting `screen_anchor` and aligning the Scene view to it (`Ctrl+Shift+F`): the display should be centred and the blue +Z arrow should point outward.

### E.6 Bind highlight

1. Add `PrinterBindHighlight` to the printer root if not already added. Leave it hidden by default.
2. In play mode, set the component's `DebugState` to `Bindable`, or use the `Debug/Show ...` context menu. Confirm the sphere encloses the printer, does not block the hover raycast, and is translucent.
3. Check `BoundToThisDevice`, `BoundToOtherDevice`, `SelectedForUnbind`, and `SetHovered(true)`.

### E.7 Record and stop

Fill §9 with the recorded values, then stop. Do not start M1 until the manual gate passes and the mapping is confirmed. If any axis moves on the wrong node, fix the wiring rather than adding code compensation.
