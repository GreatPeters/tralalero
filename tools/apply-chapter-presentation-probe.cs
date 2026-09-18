try { return ChapterPresentationInstaller.ApplyOpenScene(); }
catch (System.Exception error)
{
    System.IO.File.WriteAllText("map-concepts/chapters-polish-2026-09-12/presentation-error.txt", error.ToString());
    throw;
}
