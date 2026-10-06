#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IndianOceanAssets.ShooterSurvival;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Saved production scenes, native imported bounds and real collider queries.
// These edit-mode contracts complement ordinary-stat play/replay and visual QA;
// they do not establish fun, camera readability or device frame time.
public sealed class Chapter45SceneIntegrationTests
{
    const string SceneRoot = "Assets/ShooterSurvival/Scenes/Tools/";

    [TestCase("RestStop", 3, "Jamsil")]
    [TestCase("Jamsil", 4, "ShoeTower")]
    [TestCase("ShoeTower", 5, "")]
    public void CampaignDestinationsAreEnabledAndPresentationIsBound(string name, int chapter, string next)
    {
        WithScene(name, scene =>
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == scene.path), Is.True, name + " is build enabled");
            var progression = Components<ChapterProgression>(scene).Single();
            Assert.That(progression.chapter, Is.EqualTo(chapter));
            Assert.That(progression.nextScene, Is.EqualTo(next));
            Assert.That(progression.clearRewardText, Is.Not.Null);
            Assert.That(progression.transitionUI, Is.Not.Null);
            Assert.That(progression.transitionUI.gameObject.scene, Is.EqualTo(scene));
            Assert.That(progression.transitionUI.player, Is.Not.Null);
            Assert.That(progression.transitionUI.display, Is.Not.Null);
            Assert.That(progression.transitionUI.skipButton, Is.Not.Null);
            if (next.Length > 0)
                Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == SceneRoot + next + ".unity"), Is.True, "Reachable campaign destination");
        });
    }

    [TestCase("Jamsil", 4)]
    [TestCase("ShoeTower", 5)]
    public void AuthoredActorsAndPhysicalGoalAreRegisteredInTheirOwnScene(string name, int chapter)
    {
        WithScene(name, scene =>
        {
            var director = Components<Chapter45Director>(scene).Single();
            // Registration may be explicitly serialized or discovered by Awake.
            // Exercise the real entry point, then restore its in-memory changes;
            // a partial nonempty list or an actor outside this director still fails.
            using var initialized = new DirectorAwakeScope(director);
            Assert.That(director.enabled && director.gameObject.activeInHierarchy, Is.True);
            Assert.That(director.chapter, Is.EqualTo(chapter));
            Assert.That(director.route, Is.Not.Null);
            Assert.That(director.route.gameObject.scene, Is.EqualTo(scene));
            Assert.That(director.hud, Is.Not.Null);
            Assert.That(Components<PlayerScript>(scene), Has.Length.EqualTo(1));
            var opening = Components<OpeningStoryUI>(scene).Single();
            Assert.That(opening.gameObject.activeSelf, Is.False, "New chapters do not inherit an active frozen opening matte");
            Assert.That(opening.enabled && opening.autoPlayMovie, Is.True, "Manual story playback keeps its working component");
            var tutorial = Components<CoastalTutorialUI>(scene).Single();
            var canvas = Components<CanvasScript>(scene).Single();
            Assert.That(canvas.isActiveAndEnabled, Is.True, "The shared Canvas must remain active for real Start and game state");
            Assert.That(tutorial.enabled, Is.False, "The original chapter tutorial does not run in new chapters");
            Assert.That(tutorial.panel, Is.Not.Null);
            Assert.That(tutorial.panel.activeSelf || tutorial.panel.activeInHierarchy, Is.False, "The tutorial panel cannot cover the new chapter lobby");
            Registered(scene, director.encounters);
            Registered(scene, director.targets);
            Registered(scene, director.choices);
            Registered(scene, director.hazards);
            Registered(scene, director.lifts);
            Registered(scene, director.goals);
            Assert.That(director.encounters.Any(e => e.requiredToProceed), Is.True, "Final goal must depend on authored combat");
            foreach (var encounter in director.encounters)
            {
                Assert.That(encounter.actors, Is.Not.Empty, encounter.name);
                Assert.That(encounter.actors.All(a => a != null && a.gameObject.scene == scene), Is.True, encounter.name);
                Assert.That(encounter.stopDistance, Is.InRange(0, director.route.Length), encounter.name);
                AssertChoiceReference(director, encounter.choiceIndex, encounter.requiredChoice, encounter.name);
            }
            foreach (var target in director.targets)
            {
                Assert.That(target.hitCollider, Is.Not.Null, target.name);
                Assert.That(target.hitCollider.gameObject.scene, Is.EqualTo(scene), target.name);
                Assert.That(target.panel, Is.Not.Null, target.name);
                Assert.That(target.panel.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh != null && f.GetComponent<MeshRenderer>() != null), Is.True,
                    target.name + " has visible panel geometry");
                AssertChoiceReference(director, target.choiceIndex, target.requiredChoice, target.name);
                if (target.blocksAllLanes || target.captain)
                    Assert.That(target.requiredForGoal || target.captain, Is.True, target.name + " cannot be bypassed by the goal");
            }
            Assert.That(Components<Collider>(scene).Where(c => c.tag == "GameEndTriggerTag" || c.tag == "GameEnd"), Is.Empty,
                "Legacy win triggers bypass required encounter/target completion");

            var goal = director.goals.Single();
            Assert.That(goal.gameObject.activeInHierarchy && goal.enabled, Is.True);
            Assert.That(goal.floor, Is.EqualTo(director.route.segments.Last().floor));
            var triggers = goal.GetComponents<Collider>().Where(c => c.enabled && c.isTrigger).ToArray();
            Assert.That(triggers, Is.Not.Empty, "Completion requires a real player trigger");
            Physics.SyncTransforms();
            bool reachable = false;
            for (float distance = director.route.Length - 8; distance <= director.route.Length; distance += .5f)
            {
                director.route.Sample(distance, out var point, out _);
                point += Vector3.up * (director.footOffset + .8f);
                reachable |= triggers.Any(c => Vector3.Distance(c.ClosestPoint(point), point) < .05f);
            }
            Assert.That(reachable, Is.True, "Final route passes through the physical goal at player height");
            Assert.That(goal.offering, Is.EqualTo(chapter == 5));
            if (chapter == 5)
            {
                Assert.That(goal.floor, Is.EqualTo(7));
                Assert.That(director.useExplicitRegistrations, Is.True, "Empty registries cannot rediscover archived bosses");
                Assert.That(director.targets.Any(t => t.captain), Is.False, "The former roof captain is not a current victory gate");
                var managers = director.encounters.Where(e => e.floor == 7).ToArray();
                Assert.That(managers, Has.Length.GreaterThanOrEqualTo(3));
                Assert.That(managers.All(e => e.requiredToProceed && e.choiceIndex < 0), Is.True, "Every final manager applies to both branches");
                Assert.That(managers.All(e => e.stopDistance < director.route.Length - 8), Is.True, "Managers gate the approach to the physical shoe");
                Assert.That(goal.collectionVisual, Is.Not.Null);
                Assert.That(goal.collectionVisual.GetComponentsInChildren<MeshRenderer>(true), Is.Not.Empty);
                Assert.That(goal.collectionVisual.gameObject.scene, Is.EqualTo(scene));
            }
        });
    }

    [Test]
    public void TowerHasActualSevenFloorsBasementAndTwoSelectedPathsToPhysicalShoe()
    {
        WithScene("ShoeTower", scene =>
        {
            var director = Components<Chapter45Director>(scene).Single();
            using var initialized = new DirectorAwakeScope(director);
            var route = director.route;
            Assert.That(director.useExplicitRegistrations, Is.True);
            Assert.That(route.segments.Select(s => s.floor).Distinct(), Is.EquivalentTo(new[] { 1,2,3,4,5,6,7,8 }));
            Assert.That(route.segments.Where(s => s.floor == 8).All(s => s.label.StartsWith("B1") && s.start.y < 0), Is.True,
                "Basement uses a nonnegative unique deck ID, not the common-deck sentinel");
            foreach (int floor in Enumerable.Range(1,7))
                Assert.That(route.segments.Where(s => s.floor == floor).Select(s => s.start.y).Distinct().Count(), Is.EqualTo(1));
            for (int i=0;i<route.segments.Length;i++)
            {
                var segment=route.segments[i];
                Assert.That(Finite(segment.start)&&Finite(segment.end),Is.True);
                Assert.That(segment.Length,Is.EqualTo(Vector3.Distance(segment.start,segment.end)).Within(.01f),"Authored metres remain physical metres");
                Assert.That(segment.start.y,Is.EqualTo(segment.end.y).Within(.001f));
                if(i+1<route.segments.Length && route.segments[i+1].floor==segment.floor)
                    Assert.That(Vector3.Distance(segment.end,route.segments[i+1].start),Is.LessThan(.01f));
            }
            var fork=director.choices.Single(c=>c.kind==Chapter45Choice.ChoiceKind.FloorRoute);
            Assert.That(fork.floor,Is.EqualTo(4));
            int forkIndex=Array.IndexOf(director.choices,fork);
            var actualLinks=director.lifts.Select(l=>route.segments[l.afterSegment].floor+">"+route.segments[l.Destination(l.afterSegment)].floor).ToArray();
            Assert.That(actualLinks,Is.EquivalentTo(new[]{"1>2","2>3","3>4","4>8","4>5","8>7","5>6","6>7"}));
            foreach(var lift in director.lifts)
            {
                int from=route.segments[lift.afterSegment].floor,to=route.segments[lift.Destination(lift.afterSegment)].floor;
                Assert.That(lift.exactDuration,Is.True);Assert.That(lift.duration,Is.InRange(1,15));
                if(from==4||from==8||from==5||from==6){Assert.That(lift.choiceIndex,Is.EqualTo(forkIndex));Assert.That(lift.requiredChoice,Is.EqualTo(from==8||to==8?0:1));}
                if(from==1){Assert.That(lift.minimumFloorSeconds,Is.EqualTo(30));Assert.That(lift.requireFloorClear,Is.False);Assert.That(lift.escalator,Is.True);}
                else Assert.That(lift.requireFloorClear,Is.True);
                Assert.That(lift.GetComponentsInChildren<MeshRenderer>(true),Is.Not.Empty,"Connections have visible geometry");
                var supports=lift.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger).ToArray();
                AssertSupported(supports,route.segments[lift.afterSegment].end,lift.name+" physical boarding support");
            }
            var survival=Components<RestStopHoldout>(scene).Single(h=>h.chapter45Owner==director);
            Assert.That(survival.requiredFloor,Is.EqualTo(6));Assert.That(survival.choiceIndex,Is.EqualTo(forkIndex));Assert.That(survival.requiredChoice,Is.EqualTo(1));
            Assert.That(survival.police.Length,Is.GreaterThanOrEqualTo(12));Assert.That(survival.entrances,Has.Length.EqualTo(4));
            Assert.That(director.lifts.Single(l=>route.segments[l.afterSegment].floor==6).requiredHoldout,Is.SameAs(survival));
            Assert.That(director.encounters.Where(e=>e.floor==8).All(e=>e.choiceIndex==forkIndex&&e.requiredChoice==0),Is.True);
            Assert.That(director.encounters.Where(e=>e.floor==5||e.floor==6).All(e=>e.choiceIndex==forkIndex&&e.requiredChoice==1),Is.True);
            var archive=director.transform.Find("Preserved_PreDetailed_58F_118F_20261003");Assert.That(archive,Is.Not.Null);Assert.That(archive.gameObject.activeSelf,Is.False);
            Assert.That(archive.GetComponentsInChildren<Chapter45Target>(true).Any(t=>t.captain),Is.True,"Former captain retained for recovery");
            Assert.That(director.targets.Any(t=>t.transform.IsChildOf(archive)),Is.False);
        });
    }

    [TestCase("Jamsil")]
    [TestCase("ShoeTower")]
    public void ActualRoadCollidersSupportTheRouteAndBothForkDodgeCorridors(string name)
    {
        WithScene(name, scene =>
        {
            var director = Components<Chapter45Director>(scene).Single();
            var route = director.route;
            Physics.SyncTransforms();
            var supports = director.GetComponentsInChildren<Collider>(true).Where(c => c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger
                && c.GetComponentInParent<Chapter45Target>() == null && c.GetComponentInParent<EnemyScript_space>() == null
                && c.GetComponentInParent<Chapter45Hazard>() == null).ToArray();
            Assert.That(supports, Is.Not.Empty);
            var distances = new SortedSet<float> { 0, route.Length };
            for (float d = 4; d < route.Length; d += 4) distances.Add(d);
            for (int i = 0; i < route.segments.Length; i++)
            {
                distances.Add(route.SegmentStart(i) + .01f);
                distances.Add(route.SegmentEnd(i) - .01f);
            }
            foreach (float distance in distances)
            {
                route.Sample(distance, out var center, out var forward);
                var right = Vector3.Cross(Vector3.up, forward);
                foreach (float lane in new[] { -2.4f, 0, 2.4f })
                    AssertSupported(supports, center + right * lane, name + " route " + distance.ToString("F2") + " lane " + lane);
            }
            var forks = director.choices.Where(c => c.kind == Chapter45Choice.ChoiceKind.RouteFork).ToArray();
            if (name == "Jamsil") Assert.That(forks, Is.Not.Empty, "Street keeps a physical lane fork");
            else Assert.That(director.choices.Any(c => c.kind == Chapter45Choice.ChoiceKind.FloorRoute), Is.True, "Mall selects actual destination floors");
            foreach (var fork in forks)
            {
                Assert.That(fork.branchHalfWidth, Is.GreaterThanOrEqualTo(2.8f), "Room to dodge while committed");
                var samples = new SortedSet<float> { fork.distance, fork.endDistance };
                for (float d = fork.distance; d <= fork.endDistance; d += 4) samples.Add(d);
                foreach (float distance in samples)
                {
                    route.Sample(distance, out var center, out var forward);
                    var right = Vector3.Cross(Vector3.up, forward);
                    for (int selection = 0; selection <= 1; selection++)
                        foreach (float lane in new[] { -2.4f, 0, 2.4f })
                            AssertSupported(supports, center + right * (fork.Offset(distance, selection) + lane),
                                name + " " + fork.name + " option " + selection + " at " + distance.ToString("F2") + " lane " + lane);
                }
            }
        });
    }

    [TestCase("Jamsil", "GeneratedCrownAnchor", 105f, false)]
    [TestCase("ShoeTower", "CrownShell", 91f, true)]
    [TestCase("ShoeTower", "WhiteShoeOffering", 2.7f, false)]
    public void NativeCrownMeshesHaveThreeBoundedLodsAndUprightImportedAxes(string name, string anchorName, float expectedLength, bool cutaway)
    {
        WithScene(name, scene =>
        {
            // Presentation polish retains a disabled source crown for recovery.
            // Exactly one crown may submit visible geometry; do not select an
            // arbitrary matching name or accidentally validate the hidden source.
            // The old rooftop shoe is retained as an archive asset. The current
            // 7F physical goal is verified above and does not depend on this shell.
            var visibleAnchors = Components<Transform>(scene).Where(t => t.name == anchorName
                && t.GetComponentsInChildren<Renderer>(true).Any(r => (name == "ShoeTower" ? ActiveInsideArchive(r.transform) : r.gameObject.activeInHierarchy) && r.enabled && !r.forceRenderingOff)).ToArray();
            Assert.That(visibleAnchors, Has.Length.EqualTo(1), anchorName + " has exactly one accepted scene or preserved archive instance");
            var anchor = visibleAnchors[0];
            var group = anchor.GetComponent<LODGroup>();
            Assert.That(group, Is.Not.Null);
            Assert.That(group.enabled, Is.True);
            var lods = group.GetLODs();
            Assert.That(lods, Has.Length.EqualTo(3));
            int[] budgets = { 12000, 6500, 2200 };
            long previous = long.MaxValue;
            var seen = new HashSet<Renderer>();
            for (int i = 0; i < lods.Length; i++)
            {
                var renderers = lods[i].renderers;
                Assert.That(renderers, Is.Not.Empty);
                Assert.That(renderers.All(r => r != null && r.transform.IsChildOf(anchor) && seen.Add(r)), Is.True,
                    "Every LOD owns distinct, local renderers");
                long triangles = 0;
                foreach (var renderer in renderers)
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    Assert.That(filter, Is.Not.Null);
                    Assert.That(filter.sharedMesh, Is.Not.Null);
                    var mesh = filter.sharedMesh;
                    for (int s = 0; s < mesh.subMeshCount; s++) triangles += mesh.GetIndexCount(s) / 3;
                    Assert.That(renderer.sharedMaterials.All(m => m != null && m.shader != null && m.shader.isSupported), Is.True);
                }
                Assert.That(triangles, Is.InRange(1L, (long)budgets[i]), anchorName + " LOD" + i + " triangle budget");
                Assert.That(triangles, Is.LessThan(previous), "Each far LOD reduces actual submitted geometry");
                previous = triangles;
                if (i > 0) Assert.That(lods[i].screenRelativeTransitionHeight, Is.LessThan(lods[i - 1].screenRelativeTransitionHeight));
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                Assert.That(Finite(bounds.center) && Finite(bounds.size), Is.True);
                float length = name == "Jamsil" ? bounds.size.x : bounds.size.z;
                float width = name == "Jamsil" ? bounds.size.z : bounds.size.x;
                Assert.That(length, Is.EqualTo(expectedLength).Within(expectedLength * .03f), anchorName + " length in native world axes");
                Assert.That(bounds.size.y, Is.LessThan(length * .5f), "The sneaker length cannot point vertically");
                if (cutaway)
                {
                    // The accepted playable interior narrows only its transverse
                    // section; the source/exterior/offering keep their full shape.
                    Assert.That(width, Is.EqualTo(13f).Within(.35f), "Interior shell preserves its approved 13m cross-section");
                    // This inactive asset retains the former 235m roof deck.
                    // Its shell begins at 232.7m, so total mesh height includes
                    // the 2.3m below the deck and is not the wall rise.
                    const float preservedRoofDeckHeight = 235f;
                    Assert.That(bounds.max.y - preservedRoofDeckHeight, Is.InRange(2f, 12.2f), "Preserved cutaway retains its original above-deck height limit");
                }
                else
                {
                    Assert.That(length / Mathf.Max(width, .0001f), Is.InRange(2.7f, 3.4f), "Volumetric sneaker width survives native import");
                    Assert.That(bounds.size.y / Mathf.Max(width, .0001f), Is.InRange(1.1f, 1.5f), "Import axis conversion must not roll the shoe onto its side");
                }
                Assert.That(anchor.GetComponentsInChildren<MeshCollider>(true), Is.Empty, "Decorative crown uses authored support floors, not detailed mesh collision");
            }
        });
    }

    [TestCase("Jamsil")]
    [TestCase("ShoeTower")]
    public void EncounterVisualsProvideTheAnimationStatesUsedByTheirController(string name)
    {
        WithScene(name, scene =>
        {
            // Include inactive authored actors: their controller still needs to
            // work when an encounter, branch or later floor activates them.
            var actors = Components<Chapter45Encounter>(scene).SelectMany(e => e.actors).Distinct().ToArray();
            Assert.That(actors, Is.Not.Empty);
            string[] required = { ForwardEnemyAnimationContract.Idle, ForwardEnemyAnimationContract.Walk,
                ForwardEnemyAnimationContract.Run, ForwardEnemyAnimationContract.AttackLoop,
                ForwardEnemyAnimationContract.AttackOnce, ForwardEnemyAnimationContract.Die };
            foreach (var actor in actors)
            {
                Assert.That(actor, Is.Not.Null, name + " encounter actor reference");
                string label = name + "/" + actor.transform.parent.name + "/" + actor.name;
                Assert.That(actor.GetComponent<EnemyEventController>(), Is.Not.Null, label + " uses the actual combat animation controller");
                var animators = actor.GetComponentsInChildren<Animator>(true);
                Assert.That(animators, Has.Length.EqualTo(1), label + " resolves one visual rig");
                var animator = animators[0];
                Assert.That(animator.avatar, Is.Not.Null, label + " keeps its imported rig avatar");
                Assert.That(animator.avatar.isValid, Is.True, label + " has a valid avatar");
                var controller = animator.runtimeAnimatorController as AnimatorController;
                Assert.That(controller, Is.Not.Null, label + " uses an inspectable imported AnimatorController");
                Assert.That(controller.layers, Is.Not.Empty, label + " has the layer zero addressed by EnemyEventController");
                var states = AnimationStates(controller.layers[0].stateMachine).ToArray();
                foreach (string requiredState in required)
                {
                    var matching = states.Where(s => s.name == requiredState).ToArray();
                    Assert.That(matching, Has.Length.EqualTo(1), label + ": missing or ambiguous state " + requiredState);
                    Assert.That(matching[0].motion, Is.Not.Null, label + ": " + requiredState + " must play an assigned motion");
                    Assert.That(HasAnimationClip(matching[0].motion), Is.True, label + ": " + requiredState + " must contain a nonempty imported animation clip");
                }
            }
        });
    }

    static IEnumerable<AnimatorState> AnimationStates(AnimatorStateMachine machine)
    {
        foreach (var child in machine.states) yield return child.state;
        foreach (var child in machine.stateMachines)
            foreach (var state in AnimationStates(child.stateMachine)) yield return state;
    }

    static bool HasAnimationClip(Motion motion)
    {
        if (motion is AnimationClip clip) return clip.length > 0 && !clip.empty;
        if (motion is BlendTree blend) return blend.children.Length > 0 && blend.children.All(c => c.motion != null && HasAnimationClip(c.motion));
        return false;
    }
    static void AssertChoiceReference(Chapter45Director director, int index, int selection, string label)
    {
        if (index < 0) return;
        Assert.That(index, Is.LessThan(director.choices.Length), label + " choice reference");
        Assert.That(selection, Is.InRange(0, 1), label + " required option");
    }

    static void Registered<T>(Scene scene, T[] registered) where T : Component
    {
        Assert.That(registered, Is.Not.Null, typeof(T).Name);
        Assert.That(registered.All(c => c != null && c.gameObject.scene == scene), Is.True, typeof(T).Name + " references are local");
        var current = Components<T>(scene).Where(c => !IsArchived(c.transform)).ToArray();
        Assert.That(registered, Is.EquivalentTo(current), typeof(T).Name + " registration includes every current component exactly once and excludes the recovery archive");
    }

    static bool IsArchived(Transform t)
    {
        for (; t != null; t=t.parent) if (t.name.StartsWith("Preserved_PreDetailed_")) return true;
        return false;
    }
    static bool ActiveInsideArchive(Transform t)
    {
        for (; t != null; t=t.parent)
        {
            if (t.name.StartsWith("Preserved_PreDetailed_")) return true;
            if (!t.gameObject.activeSelf) return false;
        }
        return false;
    }

    // Awake only binds fields/static Active; it does not begin the run. Snapshot
    // those fields so reused editor scenes and a prior Active owner remain intact.
    // Do not call BeginRun here: it resets actors, hazards and player progression.
    sealed class DirectorAwakeScope : IDisposable
    {
        readonly Chapter45Director director;
        readonly FieldInfo[] fields;
        readonly object[] values;
        readonly FieldInfo active;
        readonly object activeBefore;
        readonly bool enabledBefore;

        public DirectorAwakeScope(Chapter45Director owner)
        {
            director = owner;
            enabledBefore = owner.enabled;
            var type = typeof(Chapter45Director);
            fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(f => !f.IsInitOnly).ToArray();
            values = fields.Select(f => f.GetValue(owner)).ToArray();
            active = type.GetField("<Active>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(active, Is.Not.Null, "Preserve the existing static director owner");
            activeBefore = active.GetValue(null);
            var awake = type.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null, "Exercise the real runtime registration entry point");
            try { awake.Invoke(owner, null); }
            catch { Dispose(); throw; }
        }

        public void Dispose()
        {
            for (int i = 0; i < fields.Length; i++) fields[i].SetValue(director, values[i]);
            active.SetValue(null, activeBefore);
            director.enabled = enabledBefore;
        }
    }

    static void AssertSupported(Collider[] colliders, Vector3 point, string label)
    {
        var ray = new Ray(point + Vector3.up, Vector3.down);
        foreach (var collider in colliders)
        {
            var bounds = collider.bounds;
            if (point.x < bounds.min.x || point.x > bounds.max.x || point.z < bounds.min.z || point.z > bounds.max.z) continue;
            if (collider.Raycast(ray, out var hit, 2) && Mathf.Abs(hit.point.y - point.y) <= .12f && hit.normal.y >= .8f) return;
        }
        Assert.Fail(label + ": no walkable collider at " + point.ToString("F3"));
    }

    static bool Finite(Vector3 p) => Finite(p.x) && Finite(p.y) && Finite(p.z);
    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    static T[] Components<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

    static void WithScene(string name, Action<Scene> check)
    {
        Assert.That(EditorApplication.isPlayingOrWillChangePlaymode, Is.False, "Run these checks in idle Edit Mode");
        string path = SceneRoot + name + ".unity";
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try { check(scene); }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
}
#endif
