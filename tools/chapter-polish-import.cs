if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Edit Mode required");
var enemies = ChapterMascotImporter.ImportReviewed();
var props = ChapterPropRefinement.ImportReviewed();
var ads = ChapterPresentationInstaller.ConfigureTestAds();
return new { enemies, props, ads };
