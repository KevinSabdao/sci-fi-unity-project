# Enemy (Zombie)

How the zombie works, what you need to set up, and what not to change.

---

## Setup

He is a prefab. Every tuned value and the whole Animator setup come with him.
Drag him into the scene, then do these four things.

### 1. Tag the player `Player`

Seven scripts find the player with `GameObject.FindGameObjectWithTag("Player")`.
Without the tag the zombie stands still and ignores you. Check this first when he
does nothing.

The tag itself is already in the repo, in `ProjectSettings/TagManager.asset`.
Applying it to your player object is the part that is on you.

### 2. Bake the NavMesh

Per scene, does not come with the prefab. No NavMesh, no movement.

### 3. Add `PlayerGrabEffect` to the player object

Red screen flash and camera shake during the bite. Nothing about it lives on the
zombie, so nothing warns you when it is missing. It is self contained and finds
the camera on its own, so adding the component is all it needs.

### 4. Put walls on a layer inside `Sight Blockers`

`Sight Blockers` on `EnemyAI` is the layer mask for what blocks line of sight.
`Require Line Of Sight` is on, so the mask is live.

- Walls must be in the mask, or he sees through the level
- `Player` and `Enemy` must stay out of it. Player in the mask means his own body
  blocks the raycast and the zombie never sees anyone. `Enemy` in the mask means
  zombies block each other

The zombie himself is deliberately Untagged. Only the layer `Enemy` matters for
him.

---

## Dealing damage to him

Until weapons exist, half of him cannot be tested. Stagger, the damage flash,
being alerted by gunfire and dying all start from a damage call, so without one
he only ever patrols, chases and attacks.

Weapons deal damage through `EnemyHealth`, which is the real thing, not a
placeholder:

```csharp
GetComponent<EnemyHealth>().TakeDamage(amount);
```

That call is the contract. `EnemyHealth` sits on the root object, so a raycast
hitting either collider can find it with `GetComponent` on the hit object.
Anything that damages him has to go through it, or the enemy side needs adapting
to whatever the weapons use instead.

It fires `OnDamaged` and `OnDied` UnityEvents for hooking up sounds, scrap drops
or score in the Inspector, plus static `AnyDamaged` / `AnyDied` for HUD work.
Unsubscribe from the static ones in `OnDestroy` or they leak between play
sessions.

Two colliders, both on the root object:

| Collider | Covers | Size |
|---|---|---|
| Capsule | body | centre (0, 0.8, 0), radius 0.45, height 1.8 |
| Sphere | head | centre (0.04, 1.7, 0.18), radius 0.2 |

Both are on the same GameObject, so either one calls the same `TakeDamage`.
There is no headshot multiplier. If weapons want one, check which collider the
raycast hit and apply it on the weapon side.

---

## Player scripts to replace

Three of the player scripts are placeholders I wrote so I had something to damage
and disable while testing. I only own the WASD movement, so the health, the
camera and the sprint and jump belong to whoever builds them.

`PlayerHealth` is the straightforward one. Melee, kick and bite all need
something on the player to damage.

The other two exist because of the bite. It takes control of the player while it
runs, so you cannot look away, walk off or sprint out of it, and hands control
back at the end. `EnemyGrabAttack` does that by toggling `.enabled` on both
scripts, and by calling `SyncFromTransform()` on the look script afterwards, so
the camera keeps the angle the grab left it at instead of snapping back to where
it was pointing before.

That gives whoever implements the camera and the sprint and jump three
requirements:

- Keep the class names `FirstPersonLook` and `FirstPersonSprintJump`
- Respect `.enabled`, meaning the script does nothing at all while disabled
- Expose `SyncFromTransform()` on the look script, which reads the current
  transform rotation back into whatever the script uses to track pitch and yaw

If those requirements cannot be met, the bite can be turned off entirely: set its
weight to 0 in the `Options` list on `EnemyAttackState`. He then only uses melee
and kick, and nothing else changes.

Each name below is a contract. The zombie calls exactly these class names and
members, so a replacement has to keep them, or the calls have to be adapted in
the enemy script listed.

| Script | Member called | Called from | If the name changes |
|---|---|---|---|
| `PlayerHealth` | `TakeDamage(float)` | `EnemyMeleeAttack`, `EnemyGrabAttack` | compile error, easy to find |
| `FirstPersonLook` | `.enabled`, `SyncFromTransform()` | `EnemyGrabAttack` | silent, camera snaps on release |
| `FirstPersonSprintJump` | `.enabled` | `EnemyGrabAttack` | silent, sprint overrides the hold |

`PlayerHealth` is referenced by type, so replacing it will not compile until
every call site is updated. The compiler errors show you exactly which ones.

The other two are found by class name at runtime, so the project compiles either
way and a mismatch only shows up in play mode. The class name is part of the
contract as well: a `PlayerLook` instead of a `FirstPersonLook` is never found.

### Keep these as they are

| Script | What the enemy calls | Required |
|---|---|---|
| `FirstPersonMovement` | `AddImpulse(Vector3)` for knockback, `SpeedMultiplier = 0` to pin the player during the bite | yes |
| `PlayerGrabEffect` | `SetActive(bool)` | no |

`FirstPersonMovement` requires a `CharacterController` and drives it directly. If
the player movement is ever rewritten on a Rigidbody, knockback and the bite pin
both stop working, with no error.

Tuning `FirstPersonMovement` is safe. Walk speed, acceleration and gravity are
not read by anything on the zombie. Two fields matter: `SpeedMultiplier`, which
the bite sets to 0, so anything else writing it every frame fights the grab, and
`impulseDamping`, which controls how fast knockback fades.

---

## Values you must not change alone

Each of these is tied to something else. Change one side without the other and it
breaks, usually with no error.

| Component | Field | Value | Tied to |
|---|---|---|---|
| `EnemyAI` | Wander Speed | 0.6 | blend tree threshold |
| `EnemyAI` | Chase Speed | 3.5 | blend tree threshold |
| `EnemyMeleeAttack` | Duration, melee / kick | 2.633 / 3.367 | clip lengths |
| `EnemyMeleeAttack` | Damage Delay, melee / kick | 1.03 / 0.8 | impact frame ÷ 30fps |
| `EnemyMeleeAttack` | Recovery Time, both | 2.5 | scream length |
| `EnemyGrabAttack` | Grab Duration | 2.8 | bite clip at state Speed 1.8 |
| `EnemyGrabAttack` | Recovery Time | 2.5 | scream length |
| `EnemyHitReaction` | Stagger Duration | 1.11 | 2.0s clip ÷ Speed 1.8 |
| `EnemyAttackState` | Cooldown | 0.3 | the scream is the real cooldown |
| `EnemyAttackState` | Max Attack Duration | 10 | worst case, kick plus recovery |
| `EnemyAI` | Head Forward Offset | (0, 0, 0) | already calibrated for this rig |
| Animator | Apply Root Motion | off | on, it fights the NavMeshAgent |

**Speeds.** Wander Speed and Chase Speed are also the two thresholds in the
`Locomotion` blend tree, which picks the animation by reading current speed.
Change one in the Inspector and set the same number on the blend tree motion, or
he runs at the new speed playing the walk animation.

**Timings.** Nothing reads clip lengths at runtime. Every timing is a number
matched to a clip by hand, so swapping a clip means redoing them.

**Recovery.** The scream is 2.8s and starts blending out around 2.55s. Every
`recoveryTime` is 2.5 so the agent moves as the blend begins. Higher and he
stands still with the run animation playing. Lower and he walks off mid scream.

### Animator

- Do not rename the parameters. `Speed`, `GrabAttack`, `MeleeAttack`, `Kick` and
  `HitReaction` are written as strings in code and are case sensitive. Renaming
  one breaks that animation with no error
- Bite and Reaction states run at Speed 1.8. Change it and divide the clip length
  by the new speed to get the script value
- Do not tick Can Transition To Self on the Any State transitions. That caused
  animations to freeze on their first frame
- Keep IK Pass ticked on the Base Layer or head tracking silently stops

---

## Values you can tune

| Component | Field | Now | What it does |
|---|---|---|---|
| `EnemyAI` | Detection Range | 15 | sight range, red ring |
| `EnemyAI` | View Angle | 110 | cone width |
| `EnemyAI` | Lose Sight Range | 19 | when he gives up, yellow ring |
| `EnemyAI` | Hearing Range | 4 | any direction, magenta ring |
| `EnemyAI` | Wander Radius | 12 | patrol area, cyan ring |
| `EnemyAI` | Alert Duration | 8 | how long he has to close in after being shot |
| `EnemyAttackState` | Attack Range | 2 | how close before he attacks |
| `EnemyAttackState` | weights | 4 / 4 / 3 | melee / kick / bite |
| `EnemyMeleeAttack` | Damage, melee / kick | 10 / 15 | |
| `EnemyMeleeAttack` | Knockback, both | 10 | |
| `EnemyMeleeAttack` | Damage Range | 2.5 | |
| `EnemyGrabAttack` | Damage | 45 | |
| `EnemyGrabAttack` | Push Force | 20 | |
| `EnemyHitReaction` | Stagger Chance | 0.65 | |
| `EnemyHitReaction` | Cooldown | 1.5 | stops shotgun pellets chaining staggers |
| `EnemyHealth` | Max Health | 100 | |
| `EnemyFaceTarget` | Turn Speed | 120 | how fast he tracks you while attacking |
| NavMeshAgent | Acceleration | 20 | too low and he runs on the spot briefly |
| NavMeshAgent | Angular Speed | 360 | too low and he cannot follow you circling |
| NavMeshAgent | Stopping Distance | 1.5 | |

Lose Sight Range must stay a few metres above Detection Range. Too close and he
flips between chasing and patrolling when you stand on the edge.

Turn Speed 120 means 1.5s to turn 180°. Low values let you dodge by circling him,
high values make him hard to shake off. Design call, not a calculated value.

These were tuned on an empty plane. In corridors, Wander Radius 12 is too large
and he ends up hugging walls, and Hearing Range 4 means he hears you before you
can see him.

---

## How he behaves

**Patrol.** Stands still 2 to 6s, picks a random point within 12m of his spawn
point, walks there at 0.6 m/s, stands still again. The wander area is anchored to
where he spawned rather than to where he currently is, so he drifts back towards
his spawn over time.

**Detection.** Three separate ways, any one is enough:

- Sight, a 110° cone out to 15m, blocked by walls. The cone follows his head
  bone, so the head sweep in his idle animation changes what he can see
- Hearing, a 4m sphere, works in any direction including behind him
- Being shot, instant, from any distance

Sneaking past means staying outside the 4m hearing range and out of the cone.

**Chase.** 3.5 m/s straight at you. Detection is only tested while he is idle or
wandering, so once the chase starts the cone, the hearing sphere and the walls
stop mattering. He knows where you are and heads straight there. Hiding does not
work, and neither does breaking line of sight. The only thing that ends a chase
is getting more than 19m away, which is `Lose Sight Range` and not the same
value as the 15m sight range.

**Being shot.** Alerts him instantly from any distance and opens an 8 second
window where the distance check is skipped entirely. He runs at you for those 8
seconds no matter how far away you are. When the window closes, the 19m rule
takes over: inside it, the chase continues with no time limit, outside it, he
gives up on the spot. Every new hit restarts the 8 seconds, so he never gives up
while you are still shooting.

Since he moves at 3.5 m/s and the player is faster, shooting from far away and
running is a reliable way to lose him.

**Giving up.** He does not teleport or run back. His spawn point is what anchors
his patrol area, and he never returns to it directly: he stops where he is,
stands still for a few seconds, then picks a random point within 12m of that
spawn point and walks there at 0.6 m/s. Nothing snaps him back. Not the player
breaking line of sight, not the player leaving the area, and not the zombie
himself being far from his spawn point. From across the map this takes a long
time since he patrols normally the whole way. Worth knowing as a design
limitation.

**Attacking.** Within 2m he picks one of three by weighted random: melee and kick
at 36% each, bite at 27%. The bite pins you, turns your camera to face him, then
damages and shoves you.

Damage lands partway through the animation and the range is rechecked at that
moment, so backing off mid swing dodges it.

**Cooldown.** After every attack he plays a 2.8s scream and cannot move or
attack. This is an Animator transition that fires when any attack clip ends, not
a script timer. The scripts hold him still for the same duration.

**Stagger.** Roughly two thirds of hits stagger him, with a cooldown so shotgun
pellets do not chain it. He never staggers mid attack, handled in code.

**Head tracking.** His head follows you on top of whatever animation is playing.

**Death.** `EnemyHealth.Die()` fires its events and destroys the object. There is
no death animation.

---

## Known limitations

**Several enemies close together get chaotic.** They do not coordinate, so two
can grab the player at once and fight over the camera, one can keep hitting him
while another has him pinned, and knockbacks stack. Nothing guards against this,
there are no invulnerability frames, no knockback resistance and no exclusivity
on the grab. For now the fix is the Inspector: grab weight 0 on some of them,
lower damage or knockback on others. A proper fix would be a static lock on
`EnemyGrabAttack` so only one can hold the player at a time and an iframes
mechanic for the player.

**His speed is not a single number.** Wander Speed and Chase Speed are also the
two thresholds in the locomotion blend tree, and the clips themselves were made
for a certain footspeed. Raising Chase Speed means updating the threshold as
well, and even then the feet slide, because Apply Root Motion is off and nothing
corrects for it. Making him meaningfully faster or slower is Animator work, not
an Inspector change. If the pacing feels wrong, tuning the player's walk speed
against him is usually the easier side to move.

**The walk animation is a little off.** The run looks right, but the walk does
not. The zombie looks like he is sliding or skating a bit. I spent a lot of time
on this and got it close, not perfect. Fixing it properly means moving several
values at once, the wander speed, the blend tree threshold and the clip speed,
and each one affects the others, so it is way harder than it looks to fix. As it
stands it is fully functional, it just looks a little off.

**Attack speed has the same problem.** Every duration, damage delay and recovery
time is a number matched to a clip length by hand. Swapping or retiming a clip
means redoing all of them for that attack.

**Once he is chasing, he cannot be lost by hiding.** Detection stops being
tested, so cover and corners do nothing. Distance is the only way out.

**He never returns to his spawn.** After giving up he patrols from wherever he
stopped, walking back over time at 0.6 m/s.

**No death animation and no pooling.** `Die()` destroys the object outright.
Anything that needs him to stay around after dying, like a ragdoll or a corpse,
does not exist yet.

**No headshot multiplier.** Both colliders deal the same damage. A multiplier has
to be built on the weapon side.

**Everything was tuned on an empty plane.** In corridors, Wander Radius 12 is too
large and he ends up hugging walls, and Hearing Range 4 means he hears the player
before the player can see him.

---

## The scripts

| Script | Purpose |
|---|---|
| `EnemyHealth` | HP and the damage and death events. What weapons call |
| `EnemyAI` | Idle, wander, chase, and all detection |
| `EnemyAttackState` | Picks which attack runs, stops two running at once |
| `EnemyMeleeAttack` | Melee and kick, two variations in one component |
| `EnemyGrabAttack` | The bite. Pins the player, turns the camera, shoves |
| `EnemyHitReaction` | Stagger when shot |
| `EnemyFaceTarget` | Turns his body during attacks, when the agent stops doing it |
| `EnemyHeadLook` | Head tracks the player via IK |
| `EnemyDamageFlash` | Red flash on hit |

The first three are the core. The bottom four can each be removed on their own,
you just lose that feature.

---

## When something does not work

### Logs an error and disables itself

| Script | Needs |
|---|---|
| `EnemyAI` | `NavMeshAgent`, baked NavMesh, player tag |
| `EnemyAttackState`, `EnemyFaceTarget` | player tag |
| `EnemyMeleeAttack`, `EnemyGrabAttack` | `PlayerHealth` on the player or a parent |
| `EnemyHeadLook` | player tag, rig imported as Humanoid |
| `FirstPersonMovement` | `CharacterController`, `Player/Move` input action |

Humanoid matters because head IK needs Unity to know which bone is the head. A
Generic rig does not have that information.

### Fails silently

| Symptom | Cause |
|---|---|
| Bite plays but you can still walk away | `FirstPersonMovement` not found |
| No knockback from melee or kick | same |
| Camera does not turn during the bite | no `Camera` under the player |
| Can still look around while held | `FirstPersonLook` not found |
| Camera snaps when he lets go | `SyncFromTransform()` not reached |
| Sprint overrides the hold | `FirstPersonSprintJump` not found |
| No red filter or shake during the bite | `PlayerGrabEffect` not on the player |
| Head does not track you | IK Pass unticked on the Base Layer |
| No red flash when shot | materials without `_BaseColor`, so not URP |
| Nothing animates at all | Animator parameter renamed |
| He never attacks | `Options` list empty or all weights 0 |
| Being shot does not alert him | `EnemyHealth` not on the enemy |
| He sees through walls | walls not in the Sight Blockers mask |
| He never sees anything | player's layer is in the Sight Blockers mask |

---

## Debug scripts

Two optional scripts, not on the prefab. Add the component to the zombie when you
want them, remove it afterwards.

`EnemyDebugReadout` shows a text panel on screen. Useful for telling whether the
body is actually moving or just playing a walk animation on the spot.

`EnemyDebugRanges` draws the detection rings in the Game view, since gizmos only
show in the Scene view. **G** toggles it. It reads `EnemyAI` fields by reflection
using string names, so renaming a field makes that ring silently read 0.

Both use `OnGUI`, which is expensive and draws over the game, so remove them
when you are done.