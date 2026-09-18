# Enemy (Zombie)

How the zombie works, what you need to set up, and what not to change.

---

## Setup

He is a prefab. Every tuned value and the whole Animator setup come with him.
Drag him into the scene, then do these three things.

### 1. Tag the player `Player`

Most of the enemy scripts find the player with
`GameObject.FindGameObjectWithTag("Player")`. Without the tag the zombie stands
still and ignores you. Check this first when he does nothing.

The tag itself is already in the repo, in `ProjectSettings/TagManager.asset`,
along with the `Enemy` layer, which is already applied to the prefab. Applying
the tag to your player object is the part that is on you.

### 2. Bake the NavMesh

Per scene, does not come with the prefab. No NavMesh, no movement.

### 3. Put walls on a layer inside `Sight Blockers`

`Sight Blockers` on `EnemyAI` is the layer mask for what blocks line of sight.
`Require Line Of Sight` is on, so the mask is live.

- Walls must be in the mask, or he sees through the level
- `Player` and `Enemy` must stay out of it. Player in the mask means his own body
  blocks the raycast and the zombie never sees anyone. `Enemy` in the mask means
  zombies block each other

The zombie himself is deliberately Untagged. Only the layer `Enemy` matters for
him.

### Optional: add `PlayerGrabEffect` to the player object

Red screen flash and camera shake during the grab. Everything else about the grab
works without it, and nothing on the zombie warns you when it is missing, so it is
easy to forget. It is self contained and finds the camera on its own, so adding
the component is all it needs.

### Also needed by the player movement

`FirstPersonMovement` needs a `Player/Move` action in the project's input actions
asset. It logs an error and disables itself without one, which takes knockback and
the grab pin down with it. The `CharacterController` it drives is added
automatically by `RequireComponent`.

---

## Dealing damage to him

Until weapons exist, half of him cannot be tested. Stagger, the damage flash,
being alerted by gunfire and dying all start from a damage call, so without one
he only ever patrols, chases and attacks.

Weapons deal damage through `EnemyHealth`:

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

Both are on the same GameObject, so a hit on either one ends up at the same
`TakeDamage`.
There is no headshot multiplier. If weapons want one, check which collider the
raycast hit and apply it on the weapon side.

---

## Player scripts to replace

Three of the player scripts are placeholders I wrote so I had something to damage
and disable while testing. On the player side I only own the WASD movement and
the grab effect, so the health, the camera and the sprint and jump belong to
whoever builds them.

`PlayerHealth` is there because melee, kick and grab all need something on the
player to damage.

The other two exist because of the grab. It takes control of the player while it
runs, so the player cannot look away, walk off or sprint out of it, and hands
control back at the end. `EnemyGrabAttack` does that by toggling `.enabled` on both
scripts, and by calling `SyncFromTransform()` on the look script afterwards, so
the camera keeps the angle the grab left it at instead of snapping back to where
it was pointing before.

That gives whoever implements the camera and the sprint and jump three
requirements:

- Keep the class names `FirstPersonLook` and `FirstPersonSprintJump`
- Respect `.enabled`, meaning the script does nothing at all while disabled
- Expose `SyncFromTransform()` on the look script, which reads the current
  transform rotation back into whatever the script uses to track pitch and yaw

If those requirements cannot be met, the grab can be turned off entirely by
setting its weight to 0 in the `Options` list on `EnemyAttackState`. He then only
uses melee and kick, and nothing else changes.

Each name below is a contract. The zombie calls exactly these class names and
members, so a replacement has to keep them, or the calls have to be adapted in
the enemy script listed.

| Script | Member called | Called from | If the name changes |
|---|---|---|---|
| `PlayerHealth` | `TakeDamage(float)` | `EnemyMeleeAttack`, `EnemyGrabAttack` | compile error, easy to find |
| `FirstPersonLook` | `.enabled`, `SyncFromTransform()` | `EnemyGrabAttack` | silent, camera snaps on release |
| `FirstPersonSprintJump` | `.enabled` | `EnemyGrabAttack` | silent, sprint overrides the hold |

`PlayerHealth` is referenced by type, so replacing it will not compile until
every call site is updated.

The other two are found by class name at runtime, so the project compiles either
way and a mismatch only shows up in play mode. The class name is part of the
contract as well, so a `PlayerLook` in place of a `FirstPersonLook` is never
found. `PlayerGrabEffect` is found the same way.

`FirstPersonMovement` is referenced both ways. `EnemyMeleeAttack` holds it by
type, so renaming it is a compile error there, while `EnemyGrabAttack` looks it up
by class name and fails quietly. Renaming it breaks the grab pin and the push
without breaking the build.

### Keep these as they are

| Script | What the enemy calls | Required |
|---|---|---|
| `FirstPersonMovement` | `AddImpulse(Vector3)` for knockback, `SpeedMultiplier = 0` to pin the player during the grab | yes |
| `PlayerGrabEffect` | a method of its own named `SetActive(bool)`, not `GameObject.SetActive` | no |

`FirstPersonMovement` requires a `CharacterController` and drives it directly. If
the player movement is ever rewritten on a Rigidbody, knockback and the grab pin
both stop working, with no error.

Tuning `FirstPersonMovement` is safe. Walk speed, acceleration and gravity are
not read by anything on the zombie. Two fields matter: `SpeedMultiplier`, which
the grab sets to 0, so anything else writing it every frame fights it, and
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
| `EnemyMeleeAttack` | Damage Delay, melee / kick | 1.03 / 0.8 | the impact frame in each clip |
| `EnemyMeleeAttack` | Recovery Time, both | 2.5 | the point where the scream blends out |
| `EnemyGrabAttack` | Grab Duration | 2.8 | grab clip at state Speed 1.8 |
| `EnemyGrabAttack` | Recovery Time | 2.5 | the point where the scream blends out |
| `EnemyHitReaction` | Stagger Duration | 1.11 | 2.0s clip ÷ Speed 1.8 |
| `EnemyAttackState` | Cooldown | 0.3 | what is left of the 2.8s scream after Recovery Time |
| `EnemyAttackState` | Max Attack Duration | 10 | the longest attack and its recovery |
| `EnemyAI` | Head Forward Offset | (0, 0, 0) | the head bone axis on this rig |
| `EnemyAI` | Eye Height | 1.5 | the player capsule height |
| Animator | Apply Root Motion | off | the NavMeshAgent driving movement |

**Speeds.** Wander Speed and Chase Speed are also the two thresholds in the
`Locomotion` blend tree, which picks the animation by reading current speed.
Change one in the Inspector and set the same number on the blend tree motion, or
he runs at the new speed playing the walk animation.

**Timings.** Nothing reads clip lengths at runtime. Every timing is a number
matched to a clip by hand, so swapping a clip means redoing them.

**Recovery.** The scream is 2.8s and starts blending out around 2.55s. Every
`recoveryTime` is 2.5 so the agent moves as the blend begins. Higher and he
stands still with the run animation playing. Lower and he walks off mid scream.

The 0.3 Cooldown on `EnemyAttackState` is the remainder, so 2.5 of body freeze
plus 0.3 of cooldown covers the full 2.8s scream. He walks for that last 0.3s but
cannot start another attack in it. `EnemyHitReaction` goes through the same gate,
so he cannot be staggered during it either.

### Animator

- Do not rename the parameters. `Speed`, `GrabAttack`, `MeleeAttack`, `Kick` and
  `HitReaction` are written as strings in code and are case sensitive. Renaming
  one breaks that animation with no error
- Grab and Reaction states run at Speed 1.8. Change it and divide the clip length
  by the new speed to get the script value
- Do not tick Can Transition To Self on the Any State transitions. That caused
  animations to freeze on their first frame
- Keep IK Pass ticked on the Base Layer or head tracking silently stops

---

## Values you can tune

Not every field on the prefab is listed here, only the ones that matter.

| Component | Field | Now | What it does |
|---|---|---|---|
| `EnemyAI` | Detection Range | 15 | how far he can see inside the cone |
| `EnemyAI` | View Angle | 110 | width of the sight cone |
| `EnemyAI` | Lose Sight Range | 19 | distance at which he gives up a chase |
| `EnemyAI` | Hearing Range | 4 | how far he hears in any direction |
| `EnemyAI` | Require Line Of Sight | on | whether walls block sight and hearing |
| `EnemyAI` | Wander Radius | 12 | size of the patrol area around his spawn |
| `EnemyAI` | Wander Timeout | 15 | seconds he keeps walking to a patrol point before dropping it |
| `EnemyAI` | Idle Duration | 2 to 6 | seconds standing still between patrol points |
| `EnemyAI` | Alert Duration | 8 | how long he has to close in after being shot |
| `EnemyAttackState` | Attack Range | 2 | how close before he attacks |
| `EnemyAttackState` | weights | 4 / 4 / 3 | how often melee, kick and grab are picked |
| `EnemyMeleeAttack` | Damage, melee / kick | 10 / 15 | damage per hit |
| `EnemyMeleeAttack` | Knockback, both | 10 | how hard the player is pushed |
| `EnemyMeleeAttack` | Damage Range | 2.5 | how far the hit reaches |
| `EnemyGrabAttack` | Damage | 45 | damage per grab |
| `EnemyGrabAttack` | Push Force | 20 | how hard the player is shoved on release |
| `EnemyHitReaction` | Stagger Chance | 0.65 | how often a hit staggers him |
| `EnemyHitReaction` | Cooldown | 1.5 | minimum gap between staggers |
| `EnemyHealth` | Max Health | 100 | how much damage he takes before dying |
| `EnemyFaceTarget` | Turn Speed | 120 | how fast he tracks you while attacking |
| NavMeshAgent | Acceleration | 20 | how fast he reaches his target speed |
| NavMeshAgent | Angular Speed | 360 | how fast the agent turns |
| NavMeshAgent | Stopping Distance | 1.5 | how close he stops to the player |

Acceleration and Angular Speed are worth leaving high. Too low and he runs on the
spot for a moment before moving, or cannot keep up when the player circles him.

The Cooldown on `EnemyHitReaction` is what prevents shots from chaining staggers.

Require Line Of Sight off lets him see and hear through the level, which is handy
in a test scene. With it off the `Sight Blockers` mask stops mattering entirely.

Wander Timeout is what stops him getting stuck. It drops a patrol point he cannot
reach, so a blocked path costs him 15s of walking instead of trapping him forever.

Lose Sight Range must stay a few metres above Detection Range. Too close and he
flips between chasing and patrolling when you stand on the edge.

Damage Range must stay above Attack Range for the same reason. He commits to an
attack at Attack Range and the hit is rechecked against Damage Range partway
through the animation, so if Damage Range drops below it he swings and never
connects, with no error.

Turn Speed 120 means 1.5s to turn 180°. Low values let you dodge by circling him,
high values make him hard to shake off. Design call.

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
- Hearing, a 4m sphere, works in any direction including behind him, but is
  blocked by walls in the same way sight is. Both senses end in the same line of
  sight check
- Being shot, instant, from any distance

Sneaking past means staying outside the 4m hearing range and out of the cone, or
keeping something from the `Sight Blockers` mask between you and him.

**Chase.** 3.5 m/s straight at you. Detection is only tested while he is idle or
wandering, so once the chase starts the cone, the hearing sphere and the walls
stop mattering. He knows where you are and heads straight there. Hiding does not
work, and neither does breaking line of sight. The only thing that ends a chase
is getting more than 19m away, which is `Lose Sight Range` and not the same
value as the 15m sight range.

**Being shot.** Alerts him instantly from any distance and opens an 8 second
window where the distance check is skipped entirely. He runs at you for those 8
seconds no matter how far away you are. When the window closes, the 19m rule
takes over. Inside it the chase continues with no time limit, and outside it he
gives up on the spot. Every new hit restarts the 8 seconds, so he never gives up
while you are still shooting.

**Giving up.** He does not teleport or run back. His spawn point is what anchors
his patrol area, and he never returns to it directly. He stops where he is,
stands still for a few seconds, then picks a random point within 12m of that
spawn point and walks there at 0.6 m/s. Nothing snaps him back. Not the player
breaking line of sight, not the player leaving the area, and not the zombie
himself being far from his spawn point. From across the map this takes a long
time since he patrols normally the whole way. Worth knowing as a design
limitation.

**Attacking.** Within 2m he picks one of three by weighted random, melee and kick
at 36% each and grab at 27%. The grab pins you, turns your camera to face him, then
damages and shoves you.

For melee and kick, damage lands partway through the animation and the range is
rechecked against Damage Range at that moment, so backing off mid swing dodges
it. The grab does not recheck anything. Once it starts it always connects.

**After an attack.** He plays a 2.8s scream and cannot move or attack. This is an
Animator transition that fires when any attack clip ends, not a script timer. The
scripts hold him still for 2.5s of it, so he starts moving again as the scream
blends out. See **Recovery** above.

**Stagger.** Roughly two thirds of hits stagger him, with a cooldown so gun shots
do not chain it. He never staggers mid attack, handled in code.

**Head tracking.** His head follows you on top of whatever animation is playing.

**Death.** `EnemyHealth.Die()` fires its events and destroys the object. There is
no death animation.

---

## Known limitations

**Several enemies close together get chaotic.** They do not coordinate, so two
can grab the player at once and fight over the camera, one can keep hitting him
while another has him pinned, and knockbacks stack. Nothing guards against this,
there are no invulnerability frames, no knockback resistance and no exclusivity
on the grab. For now the fix is the Inspector, so grab weight 0 on some of them
and lower damage or knockback on others. A proper fix would be a static lock on
`EnemyGrabAttack` so only one can hold the player at a time and an iFrames
mechanic for the player.

**The walk animation is a little off.** The run looks ok, but the walk does not.
The zombie looks like he is sliding or skating a bit. Each clip was made for one
footspeed, so any other speed brings the same problem back, and the blend tree
threshold only picks which clip plays, it does not fix the sliding. Fixing it
properly means moving the wander speed, the blend tree threshold and the clip
speed together, and each one affects the others. I got it close, not perfect. As
it stands right now, it is fully functional, but looks a little off.

**Hiding does not work once he is chasing.** Detection stops being tested, so
cover and corners do nothing. Distance is the only way out.

**Giving up does not teleport him back.** He carries on patrolling from wherever
he stopped, and walks back towards his spawn at 0.6 m/s, which takes a long time.
There is no reset.

**Everything was tuned on an empty plane.** In corridors, Wander Radius 12 is too
large and he ends up hugging walls, and Hearing Range 4 means he hears the player
before the player can see him.

**No headshot multiplier, no death animation and no pooling.** Both colliders
deal the same damage, and `Die()` destroys the object outright, so anything that
needs him to stay around after dying does not exist yet.

---

## The scripts

| Script | Purpose |
|---|---|
| `EnemyHealth` | HP and the damage and death events. What weapons call |
| `EnemyAI` | Idle, wander, chase, and all detection |
| `EnemyAttackState` | Picks which attack runs, stops two running at once |
| `EnemyMeleeAttack` | Melee and kick, two variations in one component |
| `EnemyGrabAttack` | The grab. Pins the player, turns the camera, shoves |
| `EnemyHitReaction` | Stagger when shot |
| `EnemyFaceTarget` | Turns his body during attacks, when the agent stops doing it |
| `EnemyHeadLook` | Head tracks the player via IK |
| `EnemyDamageFlash` | Red flash on hit |

The first five are the core. The last four can each be removed on their own, you
just lose that feature.

---

## When something does not work

### Logs an error and disables itself

| Script | Needs |
|---|---|
| `EnemyAI` | baked NavMesh, player tag (the `NavMeshAgent` is added by `RequireComponent`) |
| `EnemyAttackState`, `EnemyFaceTarget` | player tag |
| `EnemyMeleeAttack`, `EnemyGrabAttack` | `PlayerHealth` on the player or a parent |
| `EnemyHeadLook` | player tag, rig imported as Humanoid |
| `FirstPersonMovement` | `Player/Move` input action |

Humanoid matters because head IK needs Unity to know which bone is the head. A
Generic rig does not have that information.

### Fails silently

| Symptom | Probable cause |
|---|---|
| Grab plays but you can still walk away | `FirstPersonMovement` not found |
| No knockback from melee or kick | same |
| Camera does not turn during the grab | no `Camera` under the player |
| Can still look around while held | `FirstPersonLook` not found |
| Camera snaps when he lets go | `SyncFromTransform()` not reached |
| Sprint overrides the hold | `FirstPersonSprintJump` not found |
| No red filter or shake during the grab | `PlayerGrabEffect` not on the player |
| Head does not track you | IK Pass unticked on the Base Layer |
| No red flash when shot | materials without `_BaseColor`, so not URP |
| Nothing animates at all | Animator parameter renamed |
| He never attacks | `Options` list empty or all weights 0 |
| Being shot does not alert him | `EnemyHealth` not on the enemy |
| He sees through walls | walls not in the Sight Blockers mask |
| He never sees anything | player's layer is in the Sight Blockers mask |

---

## Debug scripts

I wrote two optional debug scripts for testing the enemy and its behaviour
patterns. They are not on the prefab. Add the component to the zombie when you
want them, remove it afterwards.

`EnemyDebugReadout` shows a text panel on screen. Useful for telling whether the
body is actually moving or just playing a walk animation on the spot.

`EnemyDebugRanges` draws the detection rings in the Game view, since gizmos only
show in the Scene view. **G** toggles it. It reads `EnemyAI` fields by reflection
using string names, so renaming a field makes that ring silently read 0.

Both use `OnGUI`, which is expensive and draws over the game, so remove them
when you are done.