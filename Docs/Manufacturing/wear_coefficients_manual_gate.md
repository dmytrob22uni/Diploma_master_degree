# Wear Coefficients — Manual Approval Gate

**Status:** Rates calibrated 2026-11-21; project-owner sign-off for the new rates is pending before M4 is marked done.
**Owner:** Project owner.
**Related systems:** `PrinterWearIntegrator`, `ItemWearState`, `ItemWearLogicSO` variants, `Printer.AxisTravelMetersPerSecond`, `SpoolFilamentState`.

## Why this gate exists

M4 adds real-looking print motion and manual removal, so the wear simulation now has enough context to judge whether every part wears at a believable speed. This is a tuning/approval gate, not an automated test.

The goal is not “exact real-world wear”. The goal is that all wear paths move at the pace the project owner expects during a normal print, a paused print, a hard stop and a completed print.

## Review procedure

1. Use a short, repeatable object and a known spool.
2. Record every part's condition before the job.
3. Run the job at normal simulation speed and at a high multiplier.
4. Record condition after preheat, mid-print, pause/resume, hard stop and completion.
5. Repeat once with an enclosure installed and once without.
6. Compare each category against the expected behaviour below.

## Calibration basis (2026-11-21)

The rates below are anchored to one **medium configured print** on the Stanford bunny:

- `2 h 14 min` simulated printing (`2.23 h`), `28.53 m` filament.
- Normal layer height, Normal speed, Grid infill, Auto supports.
- PLA reference: hardness `0.75`, abrasiveness `0.15`.
- Time-based hot parts use about `2.48 h` per print including preheat/cooling; distance parts use `1.20 km` of active-print travel.
- Target: most active parts reach 0 condition in **2–4 medium prints**; durable sensors, battery hardware and fire suppression intentionally last longer. A fresh part starts at 95–100 condition.

| Part | Logic | New coefficient | Wear per medium print | Prints to 0 |
|---|---|---:|---:|---:|
| Nozzle | filament × temperature × abrasiveness | `1.5 / m` | `52.4` | `1.9` |
| Extruder filament bearing | filament × temperature × abrasiveness | `0.28 / m` | `33.3` | `3.0` |
| Filament heater | time × temperature | `16 / h` | `39.7` | `2.5` |
| Extruder thermistor | time × temperature | `10 / h` | `24.8` | `4.0` |
| Print bed thermistor | time × temperature | `9 / h` | `27.9` | `3.6` |
| Battery closet | time × chamber temperature | `4 / h` | `9.9` | `10.1` |
| Battery closet adapter | time | `3.5 / h` | `8.7` | `11.5` |
| Extruder fan | time × RPM | `10 / h` | `40.7` | `2.5` |
| HEPA filter fan | time × RPM | `6.5 / h` | `29.0` | `3.4` |
| HEPA filter trap | time × fumes | `3.3 / h` | `29.4` | `3.4` |
| Fire-suppression tube | time × chamber temperature | `4 / h` | `9.9` | `10.1` |
| Enclosure thermometer | time × chamber temperature | `6.5 / h` | `16.1` | `6.2` |
| Axis belt | distance × temperature | `27 / km` | `32.5` | `3.1` |
| Belt bearing | distance × temperature | `21 / km` | `25.3` | `4.0` |
| Guide bearing | distance × temperature | `16.5 / km` | `19.9` | `5.0` |
| Rotor | distance × temperature | `14 / km` | `16.9` | `5.9` |
| Rotor bearing | distance × temperature | `16.5 / km` | `19.9` | `5.0` |
| Print bed | uses × material hardness | `16 / use` | `28.0` | `3.6` |
| Print plate | uses | `25 / use` | `25.0` | `4.0` |

Hard-stop penalties are also part of the wear loop: nozzle `3 + 7 × normalized nozzle temperature`, heater `1.5 + 3.5 × normalized nozzle temperature`, and bed/plate `0.5 + 1.5 × normalized bed temperature`. That is noticeable but below one medium print of normal wear.

The bed, plate and adapter prefabs each use one active wear state. Their secondary time-temperature assets are calibrated for future multi-state use but are not currently attached.

## Expected behaviour checklist

| Category | What should drive wear | Approval question |
|---|---|---|
| Nozzle | Filament abrasiveness + temperature/time | Does it wear at the expected rate for the chosen filament? |
| Heater / thermistors / fire-suppression tube / thermometer | Time at temperature | Do they wear at the expected rate while hot, not while idle? |
| Print bed | Uses × material hardness | Does one completed/partial print consume the expected amount? |
| Print plate | Uses | Does one completed/partial print consume the expected amount? |
| X/Y/Z belts and rotors | Distance + temperature (approximated from active print time; preheat/pause/cooling do not count) | Is the approximation acceptable until path distance is exposed? |
| Bearings / guide bearings | Distance + temperature | Do they wear slower than belts/rotors as authored? |
| Spool / filament | Consumed filament length | Does remaining filament match the consumed metres? |
| Enclosure HEPA/fire/thermometer | Time + chamber temperature | Do they wear only when the enclosure is active/hot? |
| Battery closet / adapter | As authored | Confirm they are not wearing unexpectedly. |
| Hard stop | One-off nozzle/heater/bed/plate penalty | Is the damage noticeable but not catastrophic? |

## Tuning workflow

Use two knobs for different jobs:

1. **Quick playtest offset:** `Printer.WearRateMultiplier` in the printer inspector. `1.0` is the calibrated rate; `0.75` makes printer/enclosure wear last about 33% longer and `1.5` makes it about 50% faster. It also scales completed-print use wear and hard-stop damage. Spool filament consumption is not scaled.
2. **Authored per-part calibration:** open `Tools/Manufacturing/Wear Calibration`. Assign or create a `WearCalibrationProfileSO`, set the medium-print baseline, scan, enter target prints per part, and apply the checked rows.

The editor tool only writes the coefficient field (`WearPerHour`, `WearPerKilometer`, `WearPerFilamentLength` or `WearPerUse`). It preserves reference temperatures, temperature ranges, temperature effects, abrasiveness effects, hardness effects, RPM reference/effect and wear logic type. Spool filament assets are excluded because their condition is physical filament consumption. The default profile lives at `Assets/SOs/Items/Wear/wear_calibration_profile.asset`.

For final calibration, keep `WearRateMultiplier` at `1.0`; use it only as a playtest offset so the authored target table remains meaningful.

## Sign-off

- [ ] Nozzle rate approved
- [ ] Heater/thermistor/fire/thermometer rates approved
- [ ] Bed/plate use rates approved
- [ ] Belt/rotor/bearing rates approved
- [ ] Spool/filament consumption approved
- [ ] Enclosure-part rates approved
- [ ] Hard-stop penalties approved
- [ ] Pause/home behaviour does not add unintended wear

**Approved by:** ____________________  **Date:** __________

If any value is changed after this sign-off, re-run the relevant row and update this file.
