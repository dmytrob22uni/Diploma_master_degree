using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Coursework_Master_Degree.ScriptableObjects.Items.Wear;

namespace Coursework_Master_Degree.Editor
{
    /// <summary>
    /// Authoring tool for the wear metas. Designers enter target medium-print
    /// lifetimes; the tool derives the matching coefficient and writes it back
    /// to the selected ScriptableObject. Runtime never depends on this window.
    /// </summary>
    public sealed class WearCalibrationWindow : EditorWindow
    {
        private const string ProfileFolder = "Assets/SOs/Items/Wear";
        private const string ProfilePath =
            ProfileFolder + "/wear_calibration_profile.asset";
        private const float DefaultTargetPrints = 3.0f;

        private sealed class Row
        {
            public ItemWearLogicSO Logic;
            public string Path;
            public string Name;
            public string Category;
            public string CoefficientField;
            public float CurrentCoefficient;
            public float BaseWearPerPrint;
            public float WearRatio;
            public float CurrentPrints;
            public float TargetPrints;
            public float SuggestedCoefficient;
            public bool Apply;
            public string Note;
        }

        private WearCalibrationProfileSO profile;
        private readonly List<Row> rows = new List<Row>();
        private Vector2 scroll;
        private bool printerPartsOnly = true;

        [MenuItem("Tools/Manufacturing/Wear Calibration")]
        private static void Open()
        {
            GetWindow<WearCalibrationWindow>("Wear Calibration");
        }

        private void OnEnable()
        {
            if (profile == null)
            {
                profile = FindProfile();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Wear Calibration",
                EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Set target lifetimes in medium prints. The tool derives " +
                "WearPerHour, WearPerKilometer, WearPerFilamentLength or " +
                "WearPerUse and leaves reference temperatures, ranges, " +
                "effects and RPM settings untouched.\n\n" +
                "Targets are x1 medium prints. Printer.WearRateMultiplier " +
                "is a playtest offset; Printer.WearWorkloadExponent " +
                "compresses larger jobs while keeping x1 unchanged.",
                MessageType.None);

            profile = (WearCalibrationProfileSO)EditorGUILayout.ObjectField(
                "Calibration profile",
                profile,
                typeof(WearCalibrationProfileSO),
                false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create profile"))
                {
                    CreateProfile();
                }

                GUI.enabled = profile != null;
                if (GUILayout.Button("Scan"))
                {
                    Scan();
                }

                GUI.enabled = profile != null && HasCheckedRows();
                if (GUILayout.Button("Apply checked"))
                {
                    ApplyChecked();
                }

                GUI.enabled = true;
            }

            if (profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Create or assign a WearCalibrationProfileSO to persist " +
                    "the target lifetimes.",
                    MessageType.Warning);
                return;
            }

            DrawBaseline();

            EditorGUILayout.Space();
            printerPartsOnly = EditorGUILayout.Toggle(
                new GUIContent(
                    "Printer/enclosure parts only",
                    "Filters to wear assets under a /Part/ folder. " +
                    "Spool filament wear is always excluded because it is " +
                    "physical consumption."),
                printerPartsOnly);

            if (rows.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Press Scan to find wear assets.",
                    MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (int i = 0; i < rows.Count; i++)
            {
                DrawRow(rows[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBaseline()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Medium print baseline",
                EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            profile.PrintHours = EditorGUILayout.FloatField(
                "Printing hours",
                profile.PrintHours);
            profile.PreheatCoolingHours = EditorGUILayout.FloatField(
                "Heat/cool hours",
                profile.PreheatCoolingHours);
            profile.FilamentMeters = EditorGUILayout.FloatField(
                "Filament (m)",
                profile.FilamentMeters);
            profile.MaterialHardness = EditorGUILayout.Slider(
                "Material hardness",
                profile.MaterialHardness,
                0.0f,
                1.0f);
            profile.MaterialAbrasiveness = EditorGUILayout.Slider(
                "Abrasiveness",
                profile.MaterialAbrasiveness,
                0.0f,
                1.0f);
            profile.NozzleTemperature = EditorGUILayout.FloatField(
                "Nozzle °C",
                profile.NozzleTemperature);
            profile.BedTemperature = EditorGUILayout.FloatField(
                "Bed °C",
                profile.BedTemperature);
            profile.ChamberTemperature = EditorGUILayout.FloatField(
                "Chamber °C",
                profile.ChamberTemperature);
            profile.FanRPM = EditorGUILayout.FloatField(
                "Fan RPM",
                profile.FanRPM);
            profile.FumesDensity = EditorGUILayout.FloatField(
                "Fumes density",
                profile.FumesDensity);
            profile.AxisTravelMetersPerSecond = EditorGUILayout.FloatField(
                "Axis travel (m/s)",
                profile.AxisTravelMetersPerSecond);
            profile.UseEnclosure = EditorGUILayout.Toggle(
                "Use enclosure",
                profile.UseEnclosure);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(profile);
                RecalculateRows();
            }

            EditorGUILayout.LabelField(
                "Active time basis",
                $"{profile.HotHours:0.00} h hot, " +
                $"{profile.PrintHours:0.00} h printing, " +
                $"{profile.AxisKilometers:0.00} km");
        }

        private void DrawRow(Row row)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(
                    $"{row.Name}  [{row.Category}]",
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    row.Path,
                    EditorStyles.miniLabel);

                if (!string.IsNullOrEmpty(row.Note))
                {
                    EditorGUILayout.HelpBox(row.Note, MessageType.Warning);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    row.TargetPrints = EditorGUILayout.FloatField(
                        "Target prints",
                        row.TargetPrints);

                    if (EditorGUI.EndChangeCheck())
                    {
                        row.TargetPrints = Mathf.Max(0.1f, row.TargetPrints);
                        row.Apply = true;
                        SaveTarget(row);
                        RecalculateRow(row);
                    }

                    row.Apply = EditorGUILayout.ToggleLeft(
                        "Apply",
                        row.Apply,
                        GUILayout.Width(64.0f));
                }

                EditorGUILayout.LabelField(
                    "Current",
                    $"{FormatPrints(row.CurrentPrints)} prints, " +
                    $"{row.CoefficientField} = " +
                    $"{row.CurrentCoefficient:0.####}");

                EditorGUILayout.LabelField(
                    "Suggested",
                    $"{row.SuggestedCoefficient:0.####} " +
                    $"({FormatPrints(row.TargetPrints)} prints)");
            }
        }

        private void Scan()
        {
            rows.Clear();

            if (profile == null)
            {
                return;
            }

            EnsureTargets();
            string[] guids = AssetDatabase.FindAssets("t:ItemWearLogicSO");

            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("t:ScriptableObject");
            }

            List<string> paths = new List<string>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (!string.IsNullOrEmpty(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.Ordinal);

            for (int i = 0; i < paths.Count; i++)
            {
                ItemWearLogicSO logic =
                    AssetDatabase.LoadAssetAtPath<ItemWearLogicSO>(paths[i]);

                if (logic == null || logic is ItemWearLogicFilamentSO)
                {
                    continue;
                }

                bool isPart = paths[i].IndexOf(
                    "/Part/",
                    StringComparison.OrdinalIgnoreCase) >= 0;

                if (printerPartsOnly && !isPart)
                {
                    continue;
                }

                Row row = BuildRow(logic, paths[i], isPart);

                if (row != null)
                {
                    rows.Add(row);
                }
            }

            AssetDatabase.SaveAssets();
            Repaint();
        }

        private Row BuildRow(
            ItemWearLogicSO logic,
            string path,
            bool isPart)
        {
            if (!TryGetCoefficient(
                    logic,
                    out string field,
                    out float coefficient))
            {
                return new Row
                {
                    Logic = logic,
                    Path = path,
                    Name = logic.name,
                    Category = CategoryOf(path, isPart),
                    Note = "No calibratable wear coefficient on this logic.",
                };
            }

            Row row = new Row
            {
                Logic = logic,
                Path = path,
                Name = logic.name,
                Category = CategoryOf(path, isPart),
                CoefficientField = field,
                CurrentCoefficient = coefficient,
                WearRatio = IsUsesLogic(logic)
                    ? 1.0f
                    : ReadWearRatio(logic),
                BaseWearPerPrint = BaseWearPerPrint(logic),
            };

            row.TargetPrints = DefaultTargetPrints;
            RecalculateRow(row);
            row.TargetPrints = GetTargetPrints(logic, row.CurrentPrints);
            RecalculateRow(row);
            return row;
        }

        private void RecalculateRows()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                RecalculateRow(rows[i]);
            }
        }

        private void RecalculateRow(Row row)
        {
            float effective = row.BaseWearPerPrint * row.WearRatio;

            row.CurrentPrints = effective * row.CurrentCoefficient > 0.000001f
                ? 100.0f / (effective * row.CurrentCoefficient)
                : float.PositiveInfinity;

            row.SuggestedCoefficient = effective > 0.000001f
                ? 100.0f / Mathf.Max(0.1f, row.TargetPrints) / effective
                : 0.0f;
        }

        private void ApplyChecked()
        {
            if (profile == null)
            {
                return;
            }

            int applied = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                Row row = rows[i];

                if (!row.Apply || row.Logic == null ||
                    string.IsNullOrEmpty(row.CoefficientField))
                {
                    continue;
                }

                Undo.RecordObject(row.Logic, "Calibrate wear");
                SetCoefficient(
                    row.Logic,
                    row.CoefficientField,
                    row.SuggestedCoefficient);
                EditorUtility.SetDirty(row.Logic);
                SaveTarget(row);
                applied++;
            }

            if (applied == 0)
            {
                return;
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[WearCalibration] Applied {applied} wear coefficient " +
                $"changes from target print lifetimes.");
            Scan();
        }

        private float GetTargetPrints(
            ItemWearLogicSO logic,
            float currentPrints)
        {
            if (profile == null)
            {
                return RoundTarget(currentPrints);
            }

            WearCalibrationTarget target = FindTarget(logic);

            if (target == null)
            {
                target = new WearCalibrationTarget
                {
                    Logic = logic,
                    TargetPrints = RoundTarget(currentPrints),
                };
                profile.Targets.Add(target);
                EditorUtility.SetDirty(profile);
            }

            return Mathf.Max(0.1f, target.TargetPrints);
        }

        private void SaveTarget(Row row)
        {
            if (profile == null || row.Logic == null)
            {
                return;
            }

            WearCalibrationTarget target = FindTarget(row.Logic);

            if (target == null)
            {
                target = new WearCalibrationTarget { Logic = row.Logic };
                profile.Targets.Add(target);
            }

            target.TargetPrints = Mathf.Max(0.1f, row.TargetPrints);
            EditorUtility.SetDirty(profile);
        }

        private void EnsureTargets()
        {
            if (profile != null && profile.Targets == null)
            {
                profile.Targets = new List<WearCalibrationTarget>();
                EditorUtility.SetDirty(profile);
            }
        }

        private WearCalibrationTarget FindTarget(ItemWearLogicSO logic)
        {
            EnsureTargets();

            if (profile == null || profile.Targets == null)
            {
                return null;
            }

            for (int i = 0; i < profile.Targets.Count; i++)
            {
                WearCalibrationTarget target = profile.Targets[i];

                if (target != null && target.Logic == logic)
                {
                    return target;
                }
            }

            return null;
        }

        private void CreateProfile()
        {
            if (!AssetDatabase.IsValidFolder(ProfileFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/SOs/Items"))
                {
                    AssetDatabase.CreateFolder("Assets/SOs", "Items");
                }

                AssetDatabase.CreateFolder("Assets/SOs/Items", "Wear");
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(ProfilePath);
            WearCalibrationProfileSO created =
                CreateInstance<WearCalibrationProfileSO>();
            AssetDatabase.CreateAsset(created, path);
            AssetDatabase.SaveAssets();
            profile = created;
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
        }

        private static WearCalibrationProfileSO FindProfile()
        {
            WearCalibrationProfileSO direct =
                AssetDatabase.LoadAssetAtPath<WearCalibrationProfileSO>(
                    ProfilePath);

            if (direct != null)
            {
                return direct;
            }

            string[] guids = AssetDatabase.FindAssets(
                "t:WearCalibrationProfileSO");

            for (int i = 0; i < guids.Length; i++)
            {
                WearCalibrationProfileSO found =
                    AssetDatabase.LoadAssetAtPath<WearCalibrationProfileSO>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private float BaseWearPerPrint(ItemWearLogicSO logic)
        {
            switch (logic)
            {
                case ItemWearLogicTimeTemperatureSO timeTemperature:
                    return profile.HotHours *
                        TemperatureMultiplier(
                            TemperatureFor(
                                timeTemperature.TemperatureSource),
                            timeTemperature.ReferenceTemperature,
                            timeTemperature.TemperatureRange,
                            timeTemperature.TemperatureEffect);

                case ItemWearLogicDistanceTemperatureSO distance:
                    return profile.AxisKilometers *
                        TemperatureMultiplier(
                            profile.ChamberTemperature,
                            distance.ReferenceTemperature,
                            distance.TemperatureRange,
                            distance.TemperatureEffect);

                case ItemWearLogicTimeRpmSO rpm:
                    return profile.PrintHours *
                        (1.0f + profile.FanRPM /
                            Mathf.Max(1.0f, rpm.ReferenceRPM) *
                            rpm.RPMEffect);

                case ItemWearLogicTimeFumesSO fumes:
                    return profile.PrintHours *
                        (1.0f + profile.FumesDensity * fumes.FumesEffect);

                case ItemWearLogicFilamentTemperatureAbrasivenessSO filament:
                    return profile.FilamentMeters *
                        (1.0f + profile.MaterialAbrasiveness *
                            filament.AbrasivenessEffect) *
                        TemperatureMultiplier(
                            profile.NozzleTemperature,
                            filament.ReferenceTemperature,
                            filament.TemperatureRange,
                            filament.TemperatureEffect);

                case ItemWearLogicUsesHardnessSO usesHardness:
                    return 1.0f + profile.MaterialHardness *
                        usesHardness.HardnessEffect;

                case ItemWearLogicUsesSO _:
                    return 1.0f;

                case ItemWearLogicTimeSO _:
                    return profile.HotHours;

                default:
                    return 0.0f;
            }
        }

        private float TemperatureFor(WearTemperatureSource source)
        {
            switch (source)
            {
                case WearTemperatureSource.Nozzle:
                    return profile.NozzleTemperature;
                case WearTemperatureSource.Bed:
                    return profile.BedTemperature;
                case WearTemperatureSource.Chamber:
                    return profile.ChamberTemperature;
                default:
                    return profile.UseEnclosure
                        ? profile.ChamberTemperature
                        : profile.BedTemperature;
            }
        }

        private static float TemperatureMultiplier(
            float temperature,
            float reference,
            float range,
            float effect)
        {
            if (temperature <= reference)
            {
                return 1.0f;
            }

            float normalized = (temperature - reference) /
                Mathf.Max(1.0f, range);
            return 1.0f + normalized * effect;
        }

        private static bool IsUsesLogic(ItemWearLogicSO logic) =>
            logic is ItemWearLogicUsesSO ||
            logic is ItemWearLogicUsesHardnessSO;

        private static bool TryGetCoefficient(
            ItemWearLogicSO logic,
            out string field,
            out float coefficient)
        {
            switch (logic)
            {
                case ItemWearLogicTimeTemperatureSO timeTemperature:
                    field = "WearPerHour";
                    coefficient = timeTemperature.WearPerHour;
                    return true;
                case ItemWearLogicDistanceTemperatureSO distance:
                    field = "WearPerKilometer";
                    coefficient = distance.WearPerKilometer;
                    return true;
                case ItemWearLogicTimeRpmSO rpm:
                    field = "WearPerHour";
                    coefficient = rpm.WearPerHour;
                    return true;
                case ItemWearLogicTimeFumesSO fumes:
                    field = "WearPerHour";
                    coefficient = fumes.WearPerHour;
                    return true;
                case ItemWearLogicFilamentTemperatureAbrasivenessSO filament:
                    field = "WearPerFilamentLength";
                    coefficient = filament.WearPerFilamentLength;
                    return true;
                case ItemWearLogicUsesHardnessSO usesHardness:
                    field = "WearPerUse";
                    coefficient = usesHardness.WearPerUse;
                    return true;
                case ItemWearLogicUsesSO uses:
                    field = "WearPerUse";
                    coefficient = uses.WearPerUse;
                    return true;
                case ItemWearLogicTimeSO time:
                    field = "WearPerHour";
                    coefficient = time.WearPerHour;
                    return true;
                default:
                    field = null;
                    coefficient = 0.0f;
                    return false;
            }
        }

        private static void SetCoefficient(
            ItemWearLogicSO logic,
            string field,
            float value)
        {
            value = Mathf.Max(0.0f, value);

            switch (logic)
            {
                case ItemWearLogicTimeTemperatureSO timeTemperature:
                    timeTemperature.WearPerHour = value;
                    break;
                case ItemWearLogicDistanceTemperatureSO distance:
                    distance.WearPerKilometer = value;
                    break;
                case ItemWearLogicTimeRpmSO rpm:
                    rpm.WearPerHour = value;
                    break;
                case ItemWearLogicTimeFumesSO fumes:
                    fumes.WearPerHour = value;
                    break;
                case ItemWearLogicFilamentTemperatureAbrasivenessSO filament:
                    filament.WearPerFilamentLength = value;
                    break;
                case ItemWearLogicUsesHardnessSO usesHardness:
                    usesHardness.WearPerUse = value;
                    break;
                case ItemWearLogicUsesSO uses:
                    uses.WearPerUse = value;
                    break;
                case ItemWearLogicTimeSO time:
                    time.WearPerHour = value;
                    break;
            }
        }

        private static float ReadWearRatio(ItemWearLogicSO logic)
        {
            SerializedObject serialized = new SerializedObject(logic);
            SerializedProperty property = serialized.FindProperty("WearRatio");
            return property != null
                ? Mathf.Max(0.0f, property.floatValue)
                : 1.0f;
        }

        private static string CategoryOf(string path, bool isPart)
        {
            if (path.IndexOf(
                    "/Part/Enclosure/",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Enclosure";
            }

            if (isPart)
            {
                return "Printer";
            }

            return "Other";
        }

        private static float RoundTarget(float prints)
        {
            if (float.IsInfinity(prints) || float.IsNaN(prints))
            {
                return DefaultTargetPrints;
            }

            return Mathf.Clamp(
                Mathf.Round(prints * 10.0f) / 10.0f,
                0.1f,
                100.0f);
        }

        private static string FormatPrints(float prints)
        {
            return float.IsInfinity(prints)
                ? "∞"
                : prints.ToString("0.0");
        }

        private bool HasCheckedRows()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].Apply)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
