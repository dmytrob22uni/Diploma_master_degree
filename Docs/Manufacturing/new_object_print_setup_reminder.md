# New Object To Print — Setup Reminder and Checklist

**Status:** Required final-milestone documentation task.

This file is the reminder for the final documentation gate. Before the manufacturing work is considered complete, replace this reminder with a full agent-facing setup guide for adding a new printable object without per-object runtime code.

The target is a rectangular XYZ printer workflow. Object setup is data/prefab work plus a calibrated print profile; it is not a new gameplay system.

## Important calibration rule

`Tools/Manufacturing/Print Profile Generator` is a **seeder and validator**, not the final calibrator.

- Use **Scan** to inspect mesh volume, surface area, overhang and the seeded baseline.
- Do **not** click **Apply successful scans** on the Stanford bunny just to refresh it: Apply overwrites `FilamentMetersAt100PercentInfill` and `PrintTimeSecondsAt100PercentInfill` with the tool's round seed. For the bunny that is about `36,042 s` and `89.91 m`, not the calibrated `36,720 s` / `85.36 m`.
- The editor exposes `reference flow x extrusion duty` for time and filament packing for material. The round defaults are `12 mm3/s x 0.5 = 6.0 mm3/s` and `1.0`. Use the bunny's exact calibration (`5.88922 mm3/s`, `0.94943`) when exact slicer reproduction is required.
- The stored calculation baseline is `0.20 mm` layer, `80 mm/s` (Slow), `100%` infill and no supports. It is shown at the top of the editor tool and in the profile's inspector header.
- Calibrate the final baseline in a slicer at that profile: `0.2 mm` layer height, `80 mm/s`, `100%` infill, supports off.
- Use **Apply successful scans** only to seed a new object; confirm the two seeded values with a slicer run before treating the profile as calibrated.
- The new geometry and shell/infill scale model are derived at runtime, so volume and surface area do not need to be stored on the profile.

## Required guide contents

The final setup guide must cover, in order:

1. **Model prefab**
   - readable mesh (`Read/Write` enabled) for volume, surface area, supports and caps;
   - pivot at the bottom centre, centred in X/Z;
   - uniform scale `1` at the prefab root;
   - original collider and `object_to_print` layer;
   - `ObjectToPrintDataHolder` and `ObjectToPrintMetaSO` reference;
   - ghost prefabs for placement, printer-bed placement, quality and selling;
   - mutual `ObjectToPrintDataHolder` ↔ ghost references through `ItemInstancer`;
   - no per-object branch in shared runtime code.

2. **Print profile**
   - `FilamentMetersAt100PercentInfill` from a slicer at the reference profile;
   - `PrintTimeSecondsAt100PercentInfill` from the same slicer run;
   - `SupportsRequired` and `SupportOverhangPercent` from the mesh/tool;
   - `WallThicknessMillimeters` (default `1.2 mm`) used by the shell/infill scale rule;
   - `DefaultInfill`;
   - icon, name, description, `ObjectToPrintType` and catalog entry;
   - note that the editor tool’s volume/flow×duty/filament-packing values are seeds; the round defaults are approximate (about `-1.9%` time and `+5.3%` filament versus the calibrated bunny), and each new object still needs a slicer confirmation;
   - the tool reports solid shell, material shell for the default infill, and the scale-2x factor from the same rule used by `PrintEstimates`.

3. **Catalog and UI**
   - add the meta to the correct `ObjectToPrintMetasListSO`;
   - verify the console object list, icon, grouping, default settings and draft persistence;
   - verify the object is selectable without any object-specific UI code.

4. **Estimate and validation**
   - geometry is cached from the prefab by `ObjectToPrintSizing.GetGeometry`;
   - confirms the shell/infill scale rule at minimum, `1.0` and maximum scale;
   - confirms support cost scales with the object’s overhang percentage;
   - verifies build-volume/plate fit, filament sufficiency and support warnings;
   - confirms scale `1` reproduces the calibrated baseline exactly.

5. **Print visuals and removal**
   - live printing uses the selected spool’s `PrintingMaterial` with reveal/backface lighting and no cap;
   - settled pause and hard-stopped partial print use a sliced capped surface with the spool’s `FinalMaterial`;
   - resume removes the pause cap and restores the printing shader;
   - completed print has no cap; partial print keeps the capped surface/collider;
   - `Auto` supports generate separate closed columns from the object mesh; they use the printing material live, switch to the final material on settled pause/partial/completion, reveal by scale, and remain attached until M5. They are not part of the object cap/sliced collider;
   - completed/partial removal clears the bed and uses no-cancel placement;
   - selling removes the object, cap/sliced surface, supports and every registered ghost;
   - selling on the bed is blocked; partials sell without income.

6. **Verification**
   - run the M2 estimates/quality checks against `estimate_matrix.md`;
   - run the M2.5 placement and scale/support re-check;
   - run the M4 print visual/manual gate;
   - verify multi-mesh reveal, support coverage and sale-time cleanup;
   - verify no object-specific assumptions leaked into shared runtime code;
   - run the full prefab/scene audit.

## Existing references

- `Docs/Manufacturing/new_printer_agent_setup_reminder.md` — printer setup.
- `Docs/Manufacturing/wear_coefficients_manual_gate.md` — wear approval.
- `Docs/Manufacturing/manufacturing_plan.md` — M2, M2.5 and M4 gates.
- `Docs/Manufacturing/estimate_matrix.md` — full bunny console matrix and sanity review.
- `Assets/Scripts/Coursework_Master_Degree/Items/FFF/Printer/Jobs/ObjectToPrintSizing.cs`
- `Assets/Scripts/Coursework_Master_Degree/Items/FFF/Printer/Jobs/PrintEstimates.cs`
- `Assets/Scripts/Coursework_Master_Degree/Editor/PrintProfileGeneratorWindow.cs`

## Reminder

Update the roadmap when the full guide is written, record the supported object profiles and any deferred object types, and keep this file as the entry point for future agents adding printable objects.
