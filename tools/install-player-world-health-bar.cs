// Pipeline eval_file executes a method body; keep namespace-qualified statements here.
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Preserve dirty scenes before authoring health bars.");
        string original = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        string[] names = { "Noryangjin_MapTool_Mode_SR18", "HighWay", "RestStop" };
        foreach (string name in names)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/ShooterSurvival/Scenes/Tools/" + name + ".unity");
            string backup = "tmp/backups/harbor-ui-production-2026-09-14/" + name + ".before.unity";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));
            if (!System.IO.File.Exists(backup) && !UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, backup, true))
                throw new System.IO.IOException("Could not save baseline snapshot.");
            var root = scene.GetRootGameObjects().Single(g => g.name == "Canvas");
            var player = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<IndianOceanAssets.ShooterSurvival.PlayerScript>(true)).Single();
            var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).First(c => c.enabled);
            var existing = root.transform.Find("PlayerWorldHealthBar");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var rect = new GameObject("PlayerWorldHealthBar", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
            rect.SetParent(root.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(150f, 24f);
            var track = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            track.color = new Color(.035f, .045f, .02f, .96f);
            track.raycastTarget = false;
            track.sprite = MobileUIArt.Rounded;
            track.type = UnityEngine.UI.Image.Type.Sliced;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            fill.transform.SetParent(rect, false);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(4, 4); fill.rectTransform.offsetMax = new Vector2(-4, -4);
            fill.sprite = MobileUIArt.Rounded;
            fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0;
            fill.color = new Color(.2f, .9f, .08f, 1f); fill.raycastTarget = false;
            var bar = rect.gameObject.AddComponent<PlayerWorldHealthBar>();
            bar.player = player; bar.gameplayCamera = camera; bar.fill = fill;
            bar.visibility = rect.GetComponent<CanvasGroup>();
            bar.visibility.blocksRaycasts = false; bar.visibility.interactable = false;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new System.IO.IOException("Health bar scene save failed.");
        }
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(original);
        return new { installed = names, original };
