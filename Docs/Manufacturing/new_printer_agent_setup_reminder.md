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

2. **Motion**
   - choose `CartesianPrinterMotion` for independent axes;
   - choose `CoreXYPrinterMotion` for coupled X/Z carriage motion;
   - create a new `PrinterMotionBase` subclass for a different XYZ mapping;
   - wire `BuildVolume`, axis directions/ranges, `BedAnchor`, `HomePositionNormalized`, calibration height and gizmos;
   - verify print-space X/Z/Y maps correctly before wiring the FSM.

3. **Parts and service**
   - author `PartLocation`s and metadata;
   - wire `CriticalPartLocations`, `PrintPlateLocation`, wear targets and `CalibrationSensitiveLocations`;
   - author `ServiceBlockedDuringPrint`;
   - verify missing-part validation, wear, integrity and remove/install flows.

4. **Spools**
   - author one to four `SpoolSlotDefinition` entries with stable ids, labels and locations;
   - verify 0/1-slot selector hiding and 2–4-slot dynamic selection;
   - wire enclosure `SpoolSockets`, including optional slot ids and overflow validation;
   - verify only the selected spool feeds a job. Multi-material is not part of this setup.

5. **Enclosure docking**
   - wire installed-accessory roles;
   - wire free-part roles and reset transforms;
   - verify dock/undock, part occupancy and free-part reset behavior;
   - do not hardcode battery-closet, adapter, spool or plug categories outside the role data.

6. **Metadata and catalog**
   - add the printer/model metadata and any required `ItemType` entry;
   - wire the prefab into the scene/catalog where needed;
   - verify device binding, console selection and save/draft behavior if applicable.

7. **Verification**
   - compile and scene audit;
   - motion debug and rectangular build-volume checks;
   - calibration, home and calibration-sensitive part changes;
   - missing/worn/critical part validation;
   - 0/1/2–4 spool layouts;
   - enclosure socket overflow and role-based docking;
   - start, pause, resume, hard stop and clear-bed flows;
   - no model-specific assumptions leaked into shared runtime code.

## Reminder

Update the roadmap when the guide is written, record the printer models and deferred cases it covers, and keep this file as the entry point for future coding agents adding printer models.
