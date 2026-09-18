#if UNITY_EDITOR
using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Sr18PresentationSceneSetup
{
    public static object ApplyOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "Noryangjin_MapTool_Mode_SR18") throw new InvalidOperationException("SR18 required");
        var enemiesRoot = scene.GetRootGameObjects().Single(g => g.name == "Noryangjin_MapTool").transform.Find("Enemies");
        ReplacePair(enemiesRoot, "SR18_L_E02_", "Assets/JH/Model/Prefab/Enemy_YllowMan_Sword.prefab");
        ReplacePair(enemiesRoot, "SR18_L_E06_", "Assets/JH/Model/Prefab/Enemy_Woman.prefab");
        var enemies = enemiesRoot.GetComponentsInChildren<EnemyEventController>(true);
        foreach (var enemy in enemies)
        {
            // Absolute authored baseline: rerunning this operation never shrinks twice.
            string sourcePrefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(enemy.gameObject);
            float authoredScale=sourcePrefab.EndsWith("Enemy_Woman.prefab",StringComparison.Ordinal)?2.5f:2.25f;
            enemy.transform.localScale = Vector3.one * (authoredScale * .75f);
            var animator = enemy.GetComponentInChildren<Animator>(true);
            animator.applyRootMotion = false;
            var grounding = enemy.GetComponent<EnemyGroundedPose>() ?? enemy.gameObject.AddComponent<EnemyGroundedPose>();
            grounding.model = animator;
            var body = enemy.GetComponent<EnemyScript_space>();
            var serialized = new SerializedObject(body);
            var projectile = enemy.GetComponentInChildren<SimpleProjectile>(true);
            if (projectile != null)
            {
                serialized.FindProperty("heldProjectile").objectReferenceValue = projectile.transform;
                serialized.FindProperty("throwPoint").objectReferenceValue = projectile.transform;
                if (enemy.name.Contains("Guard"))
                {
                    var gun = enemy.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r => r.name == "input");
                    if (gun != null)
                    {
                        var aim = enemy.GetComponent<EnemyGunAim>() ?? enemy.gameObject.AddComponent<EnemyGunAim>();
                        aim.gun = gun.transform;
                        var muzzle = gun.transform.Find("Muzzle");
                        if (muzzle == null) { muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(gun.transform, false); }
                        muzzle.localPosition = new Vector3(gun.localBounds.max.x, gun.localBounds.center.y, gun.localBounds.center.z);
                        aim.muzzle = muzzle;
                        serialized.FindProperty("throwPoint").objectReferenceValue = muzzle;
                    }
                }
                foreach (var trail in projectile.GetComponentsInChildren<TrailRenderer>(true)) { trail.emitting = false; trail.Clear(); }
                foreach (var collider in projectile.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        }
        // Three metres between centres leaves visible space without growing road width.
        foreach (string prefix in new[] { "SR18_L_E02_", "SR18_L_E06_" })
        {
            var pair = enemies.Where(e => e.name.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            Vector3 center = (pair[0].transform.position + pair[1].transform.position) * .5f;
            foreach (var enemy in pair)
            {
                float side = enemy.name.EndsWith("_Right", StringComparison.Ordinal) ? 1f : -1f;
                enemy.transform.position = center + enemy.transform.right * (side * 1.5f);
                // Contact includes the hand-held equipment; avoid a false walk-through gap.
                var collider = enemy.GetComponent<CapsuleCollider>();
                if (collider != null) collider.radius = .67f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
                if (collider != null) PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            }
        }
        int lamps = 0;
        foreach (var obstacle in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ObstacleStats>(true)))
        {
            if (!obstacle.gameObject.activeInHierarchy) continue;
            if (obstacle.obstaclePattern == ObstaclePattern.Bucket)
            {
                obstacle.bucketHeadOffset = new Vector3(0f, 1.3f, 1.25f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(obstacle);
            }
            if (obstacle.obstaclePattern != ObstaclePattern.Light) continue;
            obstacle.enabled = true; obstacle.canBeShotDown = true; obstacle.value = 50f;
            obstacle.gameObject.tag = "Obstacle";
            var collider = obstacle.GetComponent<BoxCollider>();
            if (collider == null) collider = obstacle.gameObject.AddComponent<BoxCollider>();
            collider.enabled = true; collider.isTrigger = true;
            var renderer = obstacle.GetComponent<Renderer>();
            Vector3 scale = obstacle.transform.lossyScale;
            Vector3 center = collider.center;
            if (renderer != null) center.y = renderer.localBounds.min.y + 1.2f / Mathf.Abs(scale.y);
            collider.center = center;
            collider.size = new Vector3(.85f / Mathf.Abs(scale.x), 2.4f / Mathf.Abs(scale.y), .85f / Mathf.Abs(scale.z));
            if (obstacle.GetComponent<LampImpactFeedback>() == null) obstacle.gameObject.AddComponent<LampImpactFeedback>();
            PrefabUtility.RecordPrefabInstancePropertyModifications(obstacle);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obstacle.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
            lamps++;
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        return new { enemies = enemies.Length, scale = 1.6875f, activeContactLamps = lamps, pairSpacing = 3f };
    }

    private static void ReplacePair(Transform root, string prefix, string prefabPath)
    {
        var old = root.GetComponentsInChildren<EnemyEventController>(true).Single(e => e.name.StartsWith(prefix, StringComparison.Ordinal) && e.name.EndsWith("_Right", StringComparison.Ordinal));
        if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old.gameObject) == prefabPath) return;
        var replacement = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), root);
        replacement.name = old.name;
        replacement.transform.SetPositionAndRotation(old.transform.position, old.transform.rotation);
        replacement.transform.localScale = old.transform.localScale;
        var controller = replacement.GetComponent<EnemyEventController>() ?? replacement.AddComponent<EnemyEventController>();
        controller.EventMode = old.EventMode; controller.HideWhileWaiting = false;
        var clearance = old.GetComponent<EnemyCornerClearance>();
        if (clearance != null)
        {
            var copy = replacement.GetComponent<EnemyCornerClearance>() ?? replacement.AddComponent<EnemyCornerClearance>();
            copy.Configure(clearance.Corner, clearance.Direction);
        }
        foreach (var gate in UnityEngine.Object.FindObjectsByType<EnemyEventActivationSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!gate.Targets.Contains(old)) continue;
            gate.Targets = gate.Targets.Select(e => e == old ? controller : e).ToArray();
            EditorUtility.SetDirty(gate); PrefabUtility.RecordPrefabInstancePropertyModifications(gate);
        }
        UnityEngine.Object.DestroyImmediate(old.gameObject);
    }
}
#endif
