# New Printer Agent Setup — Reminder and Checklist

**Status:** Required final-milestone documentation task.

This file is the reminder for the final documentation gate. Before the manufacturing work is considered complete, replace this reminder with a full agent-facing setup guide for adding a new FFF printer without breaking the shared architecture.

The target scope is **rectangular XYZ FFF printers**, including independent-axis Cartesian and coupled-XY/CoreXY designs. Multi-material printing, delta and polar printers are out of scope unless explicitly approved.

## Non-negotiable architecture rule

Printer and enclosure features must stay **printer-agnostic**. Do not add model names, MK4S-specific GameObject names, fixed Left/Right spool assumptions, or per-model runtime conditionals to shared code.

Use the existing contracts instead:

- serialized `PartLocation` references;
- `SpoolSlotDefinition` data and `SpoolSlotId` runtime keys;
- `PrinterEnclosureSpoolSocket` entries;
- role-based `DockingSources` / `DockingTargets`;
- role-based `FreePartDockingSources` / `FreePartDockingTargets`;
- `PrinterMotionBase`, with `CartesianPrinterMotion` or `CoreXYPrinterMotion` where appropriate;
- `PrintedObjectPresenter`, which the `Printer` auto-adds; it uses only `PrintBedAnchor`, `PrinterMotionBase`, `PrintPlacement` and the object prefab;
- authored wear logic, temperature sources and hard-stop targets;
- per-prefab critical, calibration-sensitive and service-blocked lists.

If a requested feature cannot be expressed through these contracts, generalize the shared contract first. Do not add a model-specific branch to `Printer`, `PrinterEnclosure`, the console, or the validator.

## Required guide contents

The final setup guide must cover, in order:

1. **Prefab root**
   - `Printer`
   - `ItemDataHolder`
   - `PartsAssembly`
   - `PrinterEnvironment`
   - `PrinterBindHighlight`
   - `PrinterMotionBase` implementation
   - `PrintBedAnchor`
   - `PrintedObjectPresenter` is auto-added by `Printer`; no per-model wiring is required, but verify it exists at runtime.

2. **Motion**
   - choose `CartesianPrinterMotion` for independent axes;
   - choose `CoreXYPrinterMotion` for coupled X/Z carriage motion;
   - create a new `PrinterMotionBase` subclass for a different XYZ mapping;
   - wire `BuildVolume`, axis directions/ranges, `BedAnchor`, `HomePositionNormalized`, calibration height and gizmos;
   - set each `AxisBinding.Invert` so positive print-space axes match the bed axes; the MK4S horizontal X uses `Invert = 1` because its carriage local X maps world -X, and horizontal Z uses `Invert = 1` because moving the bed in +Z moves the nozzle toward bed-local -Z;
   - author `HomeX/Y/Z` in the printer-front frame: X `0` = left / `1` = right, Y `0` = plate / `1` = top, Z `0` = back / `1` = front. Do not compensate the home values for `AxisBinding.Invert`;
   - point the `PrintBedAnchor` local +Z toward the printer front (the placement camera and visual home frame use it) and parent it under the bed carriage so `CartesianPrinterMotion` can detect a bed-moving horizontal Z role;
   - the motion may auto-find the bed anchor under the printer root, but explicit wiring is preferred; axis labels are role-agnostic, so the MK4S node paths in tooltips are examples;
   - runtime critical-part checks use serialized `PartLocation` references; the editor `Auto-Assign Critical Parts` menu uses MK4S names only as an authoring convenience, so assign references manually for a different model;
   - verify print-space X/Z/Y maps correctly before wiring the FSM.

3. **Printed-object visuals**
   - verify the object prefab has a readable mesh, a pivot at the bottom centre and an original collider;
   - confirm the selected spool has a `PrintingMaterial` with `_reveal_height` and a regular `FinalMaterial`;
   - verify live printing is uncapped and uses the spool printing shader;
   - verify a settled pause and hard-stopped partial get a sliced capped final-material surface;
   - verify resume removes the cap and restores the printing shader/reveal;
   - verify completed prints have no cap and partial prints keep the capped surface/collider;
   - verify supports use only the object mesh and stay attached until M5;
   - verify selling removes the object, cap/sliced surface and every object-to-print ghost;
   - verify completed and partial prints are taken off the bed manually; there is no `Clear bed`;
   - verify selling is blocked on the bed and partial prints give no income.

4. **Parts and service**
   - author `PartLocation`s and metadata;
   - wire `CriticalPartLocations`, `PrintPlateLocation`, wear targets and `CalibrationSensitiveLocations`;
   - author `ServiceBlockedDuringPrint`;
   - add `FanStateVisual` to each real fan-part prefab root and assign its calm/active children plus the local rotation axis/speed; do not use model-name checks;
   - verify missing-part validation, wear, integrity and remove/install flows.

5. **Spools**
   - author one to four `SpoolSlotDefinition` entries with stable ids, labels and locations;
   - verify 0/1-slot selector hiding and 2–4-slot dynamic selection;
   - wire enclosure `SpoolSockets`, including optional slot ids and overflow validation;
   - verify only the selected spool feeds a job. Multi-material is not part of this setup.

6. **Enclosure docking and doors**
   - wire installed-accessory roles;
   - wire free-part roles and reset transforms;
   - verify dock/undock, part occupancy and free-part reset behavior;
   - add a generic `Door` component to each enclosure door; `PrinterEnclosure` auto-finds child doors, so no printer-model branch is needed;
   - verify start/resume is blocked until all doors are closed and settled; opening is available when idle, paused, cooling or finished only at/below `SafeServiceTemperatureC` (`50 °C`), closing an already-open door is always allowed, and active heating/printing/calibration remain interlocked;
   - verify opening a door during active heating/printing pauses the printer and shows the console warning;
   - do not hardcode battery-closet, adapter, spool or plug categories outside the role data.

7. **Metadata and catalog**
   - add the printer/model metadata and any required `ItemType` entry;
   - wire the prefab into the scene/catalog where needed;
   - verify device binding, console selection and save/draft behavior if applicable.

8. **Verification**
   - compile and scene audit;
   - motion debug and rectangular build-volume checks;
   - calibration, home and calibration-sensitive part changes;
   - missing/worn/critical part validation;
   - 0/1/2–4 spool layouts;
   - enclosure socket overflow and role-based docking;
   - start, pause, resume, hard stop, paused-safe Wear/Integrity/Remove interactions, manual completed/partial removal and sale blocking;
   - heating/cooling percentages during preheat, cooling and resume-heating; occupancy status from items on enclosure shelves;
   - closed-door start/resume, open-door console warning, door opening while idle/paused/cooling/finished only at/below `SafeServiceTemperatureC`, closing always allowed, and auto-pause when a door opens during active printing;
   - wear-coefficient approval in `wear_coefficients_manual_gate.md`; use `Printer.WearRateMultiplier = 1` for final calibration, keep the profile target table at x1, and verify `WearWorkloadExponent` at regular and maximum-volume scales (a 3-print part should land around 2.1–2.3 maximum-volume prints); use `Tools/Manufacturing/Wear Calibration` for per-part tuning;
   - installed extruder and enclosure fans switch calm/active with printer state, removed/spare/ghost/worn-out fans stay calm, and the active child rotates around its authored axis;
   - no model-specific assumptions leaked into shared runtime code.

## Reminder

Update the roadmap when the guide is written, record the printer models and deferred cases it covers, and keep this file as the entry point for future coding agents adding printer models. Printable-object setup is covered separately by [`new_object_print_setup_reminder.md`](new_object_print_setup_reminder.md).
