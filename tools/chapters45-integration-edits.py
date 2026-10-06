from pathlib import Path
ROOT=Path(r'D:\Tralalero Shooter\Tralalero Shooter D')
def edit(relative,old,new):
    path=ROOT/relative;text=path.read_text(encoding='utf-8-sig')
    if new in text:return
    if old not in text:raise RuntimeError('Expected source changed: '+relative)
    path.write_text(text.replace(old,new),encoding='utf-8')
edit('Assets/Tests/Editor/ChapterPolishIntegrationTests.cs','[TestCase("RestStop", 3, "")]','[TestCase("RestStop", 3, "Jamsil")]')
edit('Assets/Tests/Editor/ChapterPolishIntegrationTests.cs','if (next.Length > 0) Assert.That(chapter.nextChapterMovie.length, Is.InRange(4.9, 5.2));','if (number < 3 && next.Length > 0) Assert.That(chapter.nextChapterMovie.length, Is.InRange(4.9, 5.2));\n            if (number == 3) Assert.That(chapter.nextChapterTitle, Does.Contain("잠실"));')
edit('Assets/ShooterSurvival/Scripts/Audio/GameAudioService.cs','scene.name=="HighWay"?traffic:','(scene.name=="HighWay"||scene.name=="Jamsil")?traffic:')
edit('tools/build-s22-android.cs','"Assets/ShooterSurvival/Scenes/Tools/RestStop.unity"};','"Assets/ShooterSurvival/Scenes/Tools/RestStop.unity","Assets/ShooterSurvival/Scenes/Tools/Jamsil.unity","Assets/ShooterSurvival/Scenes/Tools/ShoeTower.unity"};')
text=(ROOT/'tools/build-s22-android.cs').read_text(encoding='utf-8-sig').replace('S22AndroidBuild','Chapter45AndroidBuild').replace('outputs/s22-polish-2026-10-01/build-','outputs/chapters45-2026-10-02/build-').replace('TralaleroShooter-20261001-S22-','TralaleroShooter-20261002-Chapters45-')
path=ROOT/'tools/build-chapters45-android.cs'
if path.exists() and path.read_text(encoding='utf-8')!=text:raise RuntimeError('Preserve existing chapter45 build tool')
path.write_text(text,encoding='utf-8')
print('Focused test expectation, city ambience and five-scene build paths installed.')
