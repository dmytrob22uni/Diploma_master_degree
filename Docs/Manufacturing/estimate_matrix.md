# Print Estimate Matrix — Stanford Bunny

**Status:** M2 calibration reference, generated 2026-11-20.

**Purpose:** show every console combination for the bunny, check the numbers for
sanity, and record the coefficient decisions behind the current estimate model.

## Inputs

Source: `stanford_bunny_meta_SO.asset`, `PrintEstimates`, `PrintSettings`,
`InfillPattern`, `SupportsSettings` and `PrintQualityEstimator`.

| Item | Value |
|---|---|
| Filament at 100% infill | `85.36 m` (PrusaSlicer reference) |
| Time at 100% infill, `0.20 mm`, `80 mm/s` | `36,720 s` (`10 h 12 min`, PrusaSlicer reference) |
| Supports required | yes |
| Support overhang | `15.48308%` |
| Wall thickness | `1.2 mm` |
| Bounds (derived) | about `10.00 x 8.01 x 9.91 cm` |
| Solid volume (derived) | `216.25 cm3` |
| Surface area (derived) | `252.25 cm2` |
| Solid shell at `1.2 mm` | `14.0%` of solid volume |
| Material shell, Grid | `44.4%` of Grid material |

The 100% profile is a calibration reference, not a console choice. The console
offers `None`, `Grid` and `Gyroid`; their material fractions already include
the shell and top/bottom skins.

The editor tool has separate round seed defaults — `12 mm3/s x 0.5 duty =
6.0 mm3/s` and filament packing `1.0`. They produce about `36,042 s` and
`89.91 m` for the bunny, so they are not used for this matrix. The matrix uses
the calibrated profile values above; the exact tool calibration is
`5.88922 mm3/s` and `0.94943`.

Coefficients:

| Setting | Values |
|---|---|
| Layer height | Draft `0.28 mm`, Normal `0.20 mm`, Fine `0.12 mm` |
| Layer time factor | `reference / selected` = `0.714 / 1.000 / 1.667` |
| Speed | Slow `80`, Normal `160`, Fast `240 mm/s` |
| Speed time factor | `(80 / selected)^0.8` = `1.000 / 0.574 / 0.415` |
| Infill material | None `0.1892`, Grid `0.3153`, Gyroid `0.3531` |
| Infill time factor | None `0.2615`, Grid `0.3399`, Gyroid `0.3765` |
| Supports Auto | filament `x1.06`, time `x1.12` at `15.48308%` overhang |
| Scale rule | shell `s2`, core `s3`; `1.0` at scale 1 |

## Time matrix

Simulated time. Supports `None` is allowed but produces the required-support
warning and a raw-quality cap of `55`.

### Supports None

| Layer / speed | None | Grid | Gyroid |
|---|---:|---:|---:|
| Draft / Slow | 1 h 54 min | 2 h 29 min | 2 h 45 min |
| Draft / Normal | 1 h 06 min | 1 h 25 min | 1 h 35 min |
| Draft / Fast | 47 min | 1 h 02 min | 1 h 08 min |
| Normal / Slow | 2 h 40 min | 3 h 28 min | 3 h 50 min |
| Normal / Normal | 1 h 32 min | 1 h 59 min | 2 h 12 min |
| Normal / Fast | 1 h 06 min | 1 h 26 min | 1 h 36 min |
| Fine / Slow | 4 h 27 min | 5 h 47 min | 6 h 24 min |
| Fine / Normal | 2 h 33 min | 3 h 19 min | 3 h 41 min |
| Fine / Fast | 1 h 51 min | 2 h 24 min | 2 h 39 min |

### Supports Auto

| Layer / speed | None | Grid | Gyroid |
|---|---:|---:|---:|
| Draft / Slow | 2 h 08 min | 2 h 46 min | 3 h 04 min |
| Draft / Normal | 1 h 14 min | 1 h 36 min | 1 h 46 min |
| Draft / Fast | 53 min | 1 h 09 min | 1 h 17 min |
| Normal / Slow | 2 h 59 min | 3 h 53 min | 4 h 18 min |
| Normal / Normal | 1 h 43 min | 2 h 14 min | 2 h 28 min |
| Normal / Fast | 1 h 14 min | 1 h 37 min | 1 h 47 min |
| Fine / Slow | 4 h 59 min | 6 h 28 min | 7 h 10 min |
| Fine / Normal | 2 h 52 min | 3 h 43 min | 4 h 07 min |
| Fine / Fast | 2 h 04 min | 2 h 41 min | 2 h 59 min |

The reference comparison is **Normal layer / Slow speed / Grid / Auto**:
`3 h 53 min`, matching the PrusaSlicer reference. At `120x` simulation speed
that is about `1.95 min` of real time; at `240x` it is about `58 s`. `SIM SPEED`
is not a print setting; it only divides real elapsed time and does not change
time, filament or quality. Above `1x`, the console appends the real duration in
parentheses to the simulated estimate. The multiplier is applied to the real
frame delta before the hitch clamp, so a low frame rate does not silently
stretch the real duration away from that estimate; the completion log reports
the simulated seconds, real seconds and effective multiplier for verification.
The real clock accumulates only the printing intervals, so pauses and reheats
do not skew it.
`SPOOL` only gates filament sufficiency and does not change estimates; object
selection swaps the profile, so this matrix is bunny-specific.

## Filament matrix

Filament depends on infill and supports only; layer height and speed do not
change the material volume in this model.

| Infill | Supports None | Supports Auto |
|---|---:|---:|
| None | `16.15 m` | `17.12 m` |
| Grid | `26.91 m` | `28.53 m` |
| Gyroid | `30.14 m` | `31.95 m` |

`28.53 m` at Grid / Auto matches the PrusaSlicer reference.

## Quality matrix

Raw quality before calibration, pause penalties and post-processing. Values
below use `Auto` supports and no hardware penalty; `None` supports applies
`-25` and caps the result at `55`.

### Enclosed printer

| Layer / speed | Raw quality |
|---|---:|
| Draft / Slow | `79-84` |
| Draft / Normal | `75-80` |
| Draft / Fast | `65-70` |
| Normal / Slow | `89-90` |
| Normal / Normal | `85-90` |
| Normal / Fast | `75-80` |
| Fine / Slow | `90-90` |
| Fine / Normal | `89-90` |
| Fine / Fast | `79-84` |

### Open printer (no enclosure)

| Layer / speed | Raw quality |
|---|---:|
| Draft / Slow | `65-70` |
| Draft / Normal | `61-66` |
| Draft / Fast | `51-56` |
| Normal / Slow | `75-80` |
| Normal / Normal | `71-76` |
| Normal / Fast | `61-66` |
| Fine / Slow | `79-84` |
| Fine / Normal | `75-80` |
| Fine / Fast | `65-70` |

The matrix is the **current-state estimate** shown in the console. The final
job quality applies the enclosure contribution once at completion using the
job's one-chance outcome: start enclosed and never remove the printer → `+5`;
the first removal → permanent `-9`, even after reinstalling; start unenclosed →
`-9`, even if the printer is installed later.

## Sanity review

**Time**

- Monotonic in every dimension: finer layers, slower speed, denser infill and
  `Auto` supports all increase time.
- Span is `47 min` to `7 h 10 min`, about `9.1x`. A short draft print and a
  long fine print are both meaningful choices.
- Layer scaling is linear: Draft/Normal/Fine at Grid / Slow / Auto is
  `2 h 46 / 3 h 53 / 6 h 28`. This is simple and predictable. Real slicers are
  slightly sub-linear because fixed per-layer costs exist; the game
  abstraction is acceptable.
- Speed gains diminish: Slow to Normal is `0.574x`, Normal to Fast is `0.723x`.
  That matches the feel of acceleration and travel overhead.
- Infill: None to Grid adds about `30%` time and `67%` filament; Grid to Gyroid
  adds about `11%` time and `12%` filament. The Grid jump is large but
  justified by the extra material. Gyroid is only a small step beyond Grid.
- Supports add `12%` time and `6%` filament at this object's `15.48%` overhang.
  That is a plausible cost for a modest overhang.

**Filament**

- The matrix now matches the slicer at the reference point. The previous scan
  seed made every filament value about `5.3%` high.
- Filament is independent of layer height and speed by design. A real slicer
  uses a little more material at finer layers; the game keeps the tradeoff
  legible and volume-based.

**Quality**

- Quality is monotonic in layer height and speed. `Auto` supports subtract `2`
  for contact marks, but a supports-required object loses `25` and caps at
  `55` with `None`; for this bunny, `Auto` is therefore the higher-quality
  choice despite the small contact-mark penalty.
- The enclosure swing is large (`+5` versus `-9`), so the same settings read
  about `14` raw points higher when enclosed. This is deliberate: the
  enclosure is a meaningful upgrade.
- The raw cap of `90` compresses the top end: enclosed Normal / Slow is
  `89-90` and Fine / Slow is `90-90`. Fine still has a higher floor, but the
  visible gain is small. This is a balance observation for M5, not a
  coefficient bug; post-processing and the final calibration multiplier are
  intended to differentiate finished objects.
- `None` infill does not lower quality by itself. That is correct here: infill
  is a strength/material choice, while supports and surface settings drive the
  projected finish.

**Choice coverage**

- `None` is the fast/cheap choice, `Grid` the balanced default, and `Gyroid`
  the strongest material choice. Until strength or sale value uses infill,
  Gyroid is mostly dominated by Grid; that is recorded as an M5 gameplay item,
  not hidden by falsifying its costs.

## Tweaks applied from this review

- Editor tool time seed is `reference flow x extrusion duty`. The round
  defaults are `12 mm3/s x 0.5 = 6.0 mm3/s`; the bunny's exact calibration is
  `5.88922 mm3/s` (`216,252.06 mm3 / 36,720 s`).
- Editor tool filament packing defaults to `1.0`; the bunny's exact
  calibration is `0.94943` (`85.36 / 89.90716`). The bunny profile itself
  stays at `85.36 m` / `36,720 s`, so the matrix above is unchanged.
- `PrintEstimates.MaterialShellShare` is now the single source for the
  scale-rule shell share. The editor tool shows:
  - solid shell `14.0%`;
  - material shell for the default infill (`44.4%` for Grid);
  - scale `2x` factor `6.22x` versus volume-only `8.00x`.
- `Apply successful scans` now skips results with unreadable meshes instead of
  writing a partial-geometry profile.
- The stale quality and estimate tables in the manufacturing plan were
  replaced with the values above.

## Remaining calibration work

- `Grid` is the only infill measured directly against the slicer. `None` and
  `Gyroid` are proportional extrapolations and should be spot-checked.
- `Normal` and `Fast` speed and `Draft`/`Fine` layer settings still need a
  multi-point slicer matrix before their coefficients can be called
  calibrated. The current values are deliberate, monotonic game coefficients.
- The quality cap and the lack of an infill strength/value effect are M5
  balance items.
- Scale (`0.5x`, `1x`, `2x`) is a placement concern, not a console setting; it
  is covered by the M2.5 scale/support re-check.

## Regeneration

The matrix is a direct evaluation of:

```text
time = 36,720
     * infillTimeFactor
     * (0.20 / layerHeight)
     * (80 / speed)^0.8
     * (Auto ? 1.12 : 1.0)
     * scaleFactor

filament = 85.36
         * infillMaterialFactor
         * (Auto ? 1.06 : 1.0)
         * scaleFactor
```

`scaleFactor` is `1.0` at scale 1. A future PrusaSlicer CLI matrix can replace
the proportional infill/speed/layer coefficients once those runs exist.
