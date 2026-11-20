# Wear Coefficients — Manual Approval Gate

**Status:** Required project-owner review before M4 is marked done.
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

## Expected behaviour checklist

| Category | What should drive wear | Approval question |
|---|---|---|
| Nozzle | Filament abrasiveness + temperature/time | Does it wear at the expected rate for the chosen filament? |
| Heater / thermistors / fire-suppression tube / thermometer | Time at temperature | Do they wear at the expected rate while hot, not while idle? |
| Print bed | Uses × material hardness | Does one completed/partial print consume the expected amount? |
| Print plate | Uses | Does one completed/partial print consume the expected amount? |
| X/Y/Z belts and rotors | Distance + temperature (currently approximated from simulated time) | Is the approximation acceptable until real paths land? |
| Bearings / guide bearings | Distance + temperature | Do they wear slower than belts/rotors as authored? |
| Spool / filament | Consumed filament length | Does remaining filament match the consumed metres? |
| Enclosure HEPA/fire/thermometer | Time + chamber temperature | Do they wear only when the enclosure is active/hot? |
| Battery closet / adapter | As authored | Confirm they are not wearing unexpectedly. |
| Hard stop | One-off nozzle/heater/bed/plate penalty | Is the damage noticeable but not catastrophic? |

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
