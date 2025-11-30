using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace IndianOceanAssets.ShooterSurvival
{
    [CustomEditor(typeof(WaveManager))]
    public class WaveManagerEditor : Editor
    {
        private WaveManager manager;
        private List<bool> foldouts = new List<bool>();

        public override void OnInspectorGUI()
        {
            manager = (WaveManager)target;

            EditorGUILayout.Space();
            DrawSurvivalModeConfiguration();

            EditorUtility.SetDirty(manager);
        }

        private void DrawSurvivalModeConfiguration()
        {
            EditorGUILayout.LabelField("Wave Configuration", EditorStyles.boldLabel);

            // Ensure foldouts list matches wave count
            while (foldouts.Count < manager.waves.Count)
                foldouts.Add(true); // default to expanded

            while (foldouts.Count > manager.waves.Count)
                foldouts.RemoveAt(foldouts.Count - 1);

            for (int i = 0; i < manager.waves.Count; i++)
            {
                var wave = manager.waves[i];

                // Foldout for each wave
                foldouts[i] = EditorGUILayout.Foldout(foldouts[i], $"Wave {i + 1}", true, EditorStyles.foldoutHeader);

                if (foldouts[i])
                {
                    EditorGUILayout.BeginVertical("box");

                    // ENEMIES
                    EditorGUILayout.LabelField("Enemies", EditorStyles.boldLabel);
                    for (int j = 0; j < wave.enemies.Count; j++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        wave.enemies[j].enemyType = (EnemyType)EditorGUILayout.EnumPopup(wave.enemies[j].enemyType, GUILayout.Width(150));
                        wave.enemies[j].enemyCount = EditorGUILayout.IntField(wave.enemies[j].enemyCount, GUILayout.Width(100));

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            wave.enemies.RemoveAt(j);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (GUILayout.Button("Add Enemy Type"))
                    {
                        wave.enemies.Add(new EnemyWaveEntry());
                    }

                    // BARRELS
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Barrels", EditorStyles.boldLabel);
                    if (wave.barrels == null) wave.barrels = new List<BarrelWaveEntry>();
                    for (int j = 0; j < wave.barrels.Count; j++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        wave.barrels[j].barrelType = (BarrelType)EditorGUILayout.EnumPopup(wave.barrels[j].barrelType, GUILayout.Width(150));
                        wave.barrels[j].barrelCount = EditorGUILayout.IntField(wave.barrels[j].barrelCount, GUILayout.Width(100));

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            wave.barrels.RemoveAt(j);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (GUILayout.Button("Add Barrel Type"))
                    {
                        wave.barrels.Add(new BarrelWaveEntry());
                    }

                    // WALLS
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Walls", EditorStyles.boldLabel);
                    if (wave.walls == null) wave.walls = new List<WallWaveEntry>();
                    for (int j = 0; j < wave.walls.Count; j++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        wave.walls[j].wallEffectType = (WallEffectType)EditorGUILayout.EnumPopup(wave.walls[j].wallEffectType, GUILayout.Width(150));
                        wave.walls[j].wallCount = EditorGUILayout.IntField(wave.walls[j].wallCount, GUILayout.Width(100));

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            wave.walls.RemoveAt(j);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    if (GUILayout.Button("Add Wall Type"))
                    {
                        wave.walls.Add(new WallWaveEntry());
                    }

                    // WAVE TIMING
                    EditorGUILayout.Space();
                    wave.timeForNextWave = EditorGUILayout.FloatField("Time For Next Wave", wave.timeForNextWave);

                    if (GUILayout.Button("Remove Wave"))
                    {
                        manager.waves.RemoveAt(i);
                        foldouts.RemoveAt(i);
                        break;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }

            if (GUILayout.Button("Add Wave"))
            {
                manager.waves.Add(new Wave());
                foldouts.Add(true); // Add corresponding foldout
            }
        }

    }
}