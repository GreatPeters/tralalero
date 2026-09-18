using System;
using System.Linq;
using IndianOceanAssets.ShooterSurvival;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Preserve workbook encounters' one-enemy/one-spot contract while editing.</summary>
internal static class WorkbookEnemyAssignment
{
    internal static bool IsManaged(EnemyEventController enemy) => enemy != null &&
        EncounterPlacementTables.Rows.Any(row => row.kind == "적 배치" && row.scene == enemy.gameObject.scene.name && row.id == enemy.name);

    internal static void Assign(EnemyEventActivationSpot destination, EnemyEventController target, EnemyEventActivationSpot[] spots)
    {
        if(destination.gameObject.scene != target.gameObject.scene)throw new ArgumentException("같은 씬의 적과 발동 스팟을 연결하세요.");
        var owners=spots.Where(spot=>spot!=null&&spot.Targets.Contains(target)).ToArray();
        if(owners.Length>1||destination.Targets.Length>1||owners.Any(spot=>spot.Targets.Length!=1))
            throw new InvalidOperationException("이미 중복된 연결이 있습니다. 적과 발동 스팟을 각각 하나씩 복구한 뒤 연결하세요.");
        if(owners.Length==1&&owners[0]==destination)return;
        var previous=destination.Targets.FirstOrDefault();
        if(previous!=null&&owners.Length==0)
            throw new InvalidOperationException("기존 적의 발동 스팟이 사라지지 않도록 빈 스팟을 선택하세요.");
        if(previous!=null&&spots.Count(spot=>spot!=null&&spot.Targets.Contains(previous))!=1)
            throw new InvalidOperationException("기존 적의 연결이 중복되어 교체할 수 없습니다.");
        var source=owners.FirstOrDefault();
        const string undo="Replace Workbook Enemy Connection";
        Undo.RecordObject(destination,undo);if(source!=null)Undo.RecordObject(source,undo);
        // Swapping occupied spots preserves both actors. Re-clicking an existing
        // workbook binding is a no-op rather than leaving that actor untriggered.
        destination.Targets=new[]{target};
        if(source!=null)source.Targets=previous!=null?new[]{previous}:Array.Empty<EnemyEventController>();
        foreach(var spot in new[]{destination,source}.Where(spot=>spot!=null))
        {
            EditorUtility.SetDirty(spot);PrefabUtility.RecordPrefabInstancePropertyModifications(spot);
        }
        EditorSceneManager.MarkSceneDirty(destination.gameObject.scene);
    }
}
