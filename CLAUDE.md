# CLAUDE.md - Project Sheep

## Project Overview

**Project Sheep** is a stealth/action game where you play as a wolf hunting humans in the forest while hiding from hunters in bushes. Top-down 3D camera with 2D sprites (Don't Starve style).

### Core Loop
Humans spawn → Walk to house → Wolf hunts → Hides from hunters → Repeat

### Win/Lose Conditions
- **Win**: Eat enough humans OR survive all waves
- **Lose**: N humans reach the house OR wolf killed by hunters

---

## Tech Stack

| Component | Version | Purpose |
|-----------|---------|---------|
| Unity | 6000.3.2f1 | Game engine |
| URP | 17.3.0 | Rendering (PC + Mobile pipelines) |
| Input System | 1.17.0 | Player controls |
| AI Navigation | 2.0.9 | NavMesh pathfinding |
| Cinemachine | 3.1.5 | Camera management |

### Existing Setup
- `Assets/Scripts/PlayerControls.cs` - Player movement, dash, input handling
- `Assets/Scripts/PlayerInteractions.cs` - Eating animals, bullet detection, abilities, hiding state
- `Assets/Scripts/WolfHealth.cs` - Wolf health, damage, death system
- `Assets/Scripts/AnimalSpawner.cs` - Spawns animals at intervals
- `Assets/Scripts/AnimalMovement.cs` - NavMesh-based animal pathfinding with wandering, runtime speed control
- `Assets/Scripts/BillboardSprite.cs` - Makes 2D sprites face camera in 3D world
- `Assets/Scripts/ForestArea.cs` - Trigger for player hiding in forests
- `Assets/Scripts/HunterController.cs` - Hunter AI with wandering, vision, shooting
- `Assets/Scripts/HunterBullet.cs` - Projectile that travels toward wolf
- `Assets/Scripts/ScoreManager.cs` - Tracks score, fires OnScoreChanged event
- `Assets/Scripts/WaveManager.cs` - Score-based difficulty progression, spawns hunters, changes animal speed
- `Assets/Scripts/AnimalRegistry.cs` - Singleton tracking all animals for runtime modifications
- `Assets/PlayerInputs.inputactions` - Input action definitions (Move, Dash, Action, Spell)
- PC and Mobile render pipeline assets in `Assets/Settings/`

---

## Architecture

### Hunter AI (HunterController)
Simple state machine with states: Wandering, Alert, Shooting. Hunters wander randomly within a radius, detect wolf via vision cone (range + angle + LOS), build up detection progress, then shoot. Respects isHidden flag on wolf.

### Vision Cone
Distance check via OverlapSphere, then angle calculation from forward vector, finally raycast for line-of-sight. Respects isHidden flag on wolf.

### Waypoint System
ScriptableObject-based paths. WaypointPath component holds ordered transforms. Supports loop, ping-pong, and one-way traversal.

### Forest/Hiding System
Trigger colliders on forest areas. ForestArea component sets isHidden flag on player via OnTriggerEnter/Exit. Player can walk through forests freely; animals path around them using NavMesh.

### Animal Movement
NavMeshAgent targeting the goal. Animals automatically path around NavMesh obstacles (forests). Natural wandering behavior via periodic random Z-offset to destination.

### Projectile System (Hunter Bullets)
Hunters spawn bullet prefabs that travel toward the wolf. The wolf detects bullets via PlayerInteractions OnTriggerEnter (same pattern as eating animals). No Rigidbody needed - CharacterController handles trigger detection.

### Wave System (Score-Based Difficulty)
Event-driven architecture for difficulty progression. ScoreManager fires `OnScoreChanged` event when score changes. WaveManager listens and triggers effects when thresholds are crossed.

**Components:**
- `ScoreManager` - Singleton with `OnScoreChanged` event (oldScore, newScore)
- `WaveManager` - Configurable list of thresholds with effects
- `AnimalRegistry` - Tracks all animals, applies speed changes at runtime

**Data Flow:**
```
PlayerInteractions.EatAnimal()
    → ScoreManager.AddScore()
        → OnScoreChanged event fires
            → WaveManager.HandleScoreChanged()
                → Check thresholds
                    → SpawnHunters() or SetAnimalSpeedMultiplier()
```

**Effect Types:**
- `SpawnHunters` - Spawns N hunters at designated spawn points
- `ChangeAnimalSpeed` - Multiplies all animal speeds (existing and future)

### Animal Registry Pattern
AnimalMovement registers itself with AnimalRegistry on Start(), unregisters on Destroy(). This allows WaveManager to modify all active animals at runtime. New animals automatically get the current speed multiplier applied.

---

## Collision Detection Approach

This project uses **CharacterController-based trigger detection** instead of Rigidbody physics. This is simpler and works well for this game style.

### How It Works
1. **Player/Wolf** has a CharacterController (which acts like a Rigidbody for collision purposes)
2. **Other objects** (animals, bullets) have Colliders with **Is Trigger = true**
3. **PlayerInteractions** uses `OnTriggerEnter` to detect when objects enter the player's trigger
4. Objects are identified by **Tags** (e.g., "Animal", "Bullet")

### Why This Approach
- **No Rigidbody needed** on moving objects
- **Simple movement** via `transform.position` in Update
- **Consistent pattern** - all interactions go through PlayerInteractions
- **Easy to extend** - just add new tags and handle them in OnTriggerEnter

### Adding New Interactable Objects
1. Create the object with a Collider (Is Trigger = true)
2. Add a unique Tag (e.g., "PowerUp", "Trap")
3. In PlayerInteractions.OnTriggerEnter, add a check for the new tag
4. Handle the interaction (destroy object, apply effect, etc.)

```csharp
// Example: Adding a new interactable
if (other.CompareTag("PowerUp") == true)
{
    CollectPowerUp(other.gameObject);
}
```

---

## Folder Structure

```
Assets/
├── Prefabs/
│   ├── Characters/      # Wolf, Human, Hunter
│   ├── Environment/     # Bush, House, Waypoint
│   └── UI/              # HUD, GameOver panels
├── Scenes/
│   ├── Game.unity       # Main gameplay scene
│   └── MainMenu.unity
├── ScriptableObjects/
│   ├── WaypointPaths/
│   └── WaveConfigs/
├── Scripts/
│   ├── Core/            # GameManager, WaveManager
│   ├── Player/          # WolfController, WolfHealth
│   ├── AI/              # StateMachine, HunterController, HumanController, VisionCone
│   ├── Environment/     # HidingSpot, House, WaypointPath
│   └── UI/              # HUDController, GameOverUI
├── Sprites/
│   ├── Characters/
│   └── Environment/
├── Audio/
│   ├── SFX/
│   └── Music/
└── VFX/
```

---

## Naming Conventions

### C#
- **Classes**: PascalCase (`WolfController`, `HunterStateMachine`)
- **Interfaces**: IPascalCase (`IState`, `IDamageable`)
- **Methods**: PascalCase (`TakeDamage()`)
- **Private fields**: camelCase, NO underscore prefix (`currentHealth`, `isHidden`)
- **Constants**: SCREAMING_SNAKE (`MAX_HEALTH`)
- **Events**: OnPascalCase (`OnWolfDied`)

---

## Coding Guidelines

### Readability Over Brevity
Code should be easy to read and understand, even if it means writing more lines.

### Variable Names
Use descriptive names that explain what the variable does:
```csharp
// BAD - too short, unclear
float spd = 5f;
Vector3 dir;

// GOOD - clear and descriptive
float movementSpeed = 5f;
Vector3 dashDirection;
```

### No Underscore Prefix
Do not use underscore prefix for private variables:
```csharp
// BAD
private float _speed;
private bool _isDashing;

// GOOD
private float speed;
private bool isDashing;
```

### Explicit Comparisons
Always write clear, explicit comparisons instead of using negation operators:
```csharp
// BAD - confusing negation
if (!context.performed)
if (!isAlive)

// GOOD - clear comparison
if (context.performed == false)
if (isAlive == false)
```

### No Ternary Operators
Avoid ternary operators (`? :`). Use full if/else blocks instead:
```csharp
// BAD - hard to read
Vector3 dir = hasInput ? inputDirection : forwardDirection;

// GOOD - clear logic flow
Vector3 dir;
if (hasInput)
{
    dir = inputDirection;
}
else
{
    dir = forwardDirection;
}
```

### Comments
Add comments explaining WHAT the code does and WHY:
```csharp
// Calculate movement direction based on camera orientation
// This makes W always move toward the top of the screen
Vector3 moveDirection = GetCameraRelativeDirection(input);
```

### If Statements
Write clear if statements, even if it means more lines:
```csharp
// BAD - compact but confusing
if (isDashing || cooldownTimer > 0f) return;

// GOOD - clear and readable
if (isDashing == true)
{
    return;
}

if (cooldownTimer > 0f)
{
    return;
}
```

### Assets
- **Prefabs/Scenes**: PascalCase (`Wolf.prefab`, `Game.unity`)
- **Sprites**: PascalCase_Suffix (`Wolf_Idle.png`, `Wolf_Walk_01.png`)
- **Audio**: lowercase_underscore (`wolf_growl.wav`, `footstep_01.wav`)
- **ScriptableObjects**: Name_Type (`ForestPath_WaypointPath.asset`)

### Scene Hierarchy
```
--- MANAGERS ---
GameManager
WaveManager

--- ENVIRONMENT ---
Ground
House
Bushes/
Waypoints/

--- CHARACTERS ---
Wolf
Humans/
Hunters/

--- UI ---
Canvas/

--- CAMERAS ---
Main Camera
```

---

## Key Systems

| System | Script | Purpose |
|--------|--------|---------|
| Player Controls | `PlayerControls.cs` | Movement (WASD), dash (Shift), input callbacks |
| Player Interactions | `PlayerInteractions.cs` | Eating animals, action (E), spell (Q), isHidden state |
| Animal Spawner | `AnimalSpawner.cs` | Spawns animal prefabs at intervals |
| Animal Movement | `AnimalMovement.cs` | NavMesh pathfinding with natural wandering |
| Forest Area | `ForestArea.cs` | Trigger-based hiding for player in forests |
| Billboard Sprite | `BillboardSprite.cs` | Makes 2D sprites face camera |
| Wolf Health | `WolfHealth.cs` | Health, damage, death |
| Hunter AI | `HunterController.cs` | Wanders, detects wolf (vision cone), shoots bullets |
| Hunter Bullet | `HunterBullet.cs` | Projectile that travels toward wolf |
| Score Manager | `ScoreManager.cs` | Tracks score, fires OnScoreChanged event |
| Wave Manager | `WaveManager.cs` | Score thresholds trigger effects (spawn hunters, speed up animals) |
| Animal Registry | `AnimalRegistry.cs` | Tracks all animals for runtime speed changes |
| Game Manager | `GameManager.cs` | (Planned) Game state, win/lose conditions |

---

## Development Phases

### MVP (Week 1-2)
- Game field with boundaries
- Humans spawn and walk to house via paths
- Wolf movement (WASD) and sprint (Shift)
- Wolf eats humans on contact
- UI: Eaten counter, escaped counter
- Game over when N humans escape

### Stealth Mechanics (Week 3)
- Bushes with hiding functionality
- Hunters with patrol waypoints
- Vision cone detection (ignores hidden wolf)
- Detection timer → shoot
- Wolf health system
- Wave system with increasing difficulty

### Polish (Week 4)
- Wolf leveling (XP from eating)
- Abilities between waves (fire breath, dash, invisibility)
- Sound effects and particles
- Main menu and game over screens

---

## Testing Checklist

### Wolf
- [ ] Moves in all directions
- [ ] Dash works with cooldown
- [ ] Eats humans on contact
- [ ] Takes damage from hunters
- [ ] Dies at 0 health

### Humans
- [ ] Spawn at designated points
- [ ] Navigate to house via NavMesh
- [ ] Destroyed when eaten
- [ ] Escaped count increments on reaching house

### Hunters
- [ ] Follow patrol waypoints
- [ ] Detect wolf in vision cone
- [ ] Ignore hidden wolf
- [ ] Chase when wolf spotted
- [ ] Attack when in range
- [ ] Return to patrol when wolf lost

### Hiding
- [ ] Wolf hidden when in bush
- [ ] Wolf visible when exiting bush
- [ ] Visual feedback for hidden state

### Game Flow
- [ ] Waves spawn correctly
- [ ] Game over on wolf death
- [ ] Game over when humans escape
- [ ] Restart resets all systems

---

## Quick Reference

### Layers
| Layer | Purpose |
|-------|---------|
| Player (6) | Wolf |
| Human (7) | Human targets |
| Hunter (8) | Hunter enemies |
| Obstacle (9) | Vision blockers |
| HidingSpot (10) | Bush triggers |

### Tags
- `Player` - Wolf/Player
- `Animal` - Animals (prey)
- `Hunter` - Hunters (enemies)
- `Bullet` - Hunter projectiles
- `House` - Destination/Goal
- `Forest` - Hiding spots (optional, detection uses Player tag)

### Input Actions (PlayerInputs.inputactions)
- Move (Vector2): WASD / Left Stick
- Dash (Button): Left Shift / South Button
- Action (Button): E / East Button
- Spell (Button): Q / West Button

### Performance Targets
- 60 FPS on PC
- 30 FPS on mobile

---

## Cinemachine 3 Top-Down Camera Setup

### Overview
Camera that looks down at the player (Tunic/Don't Starve style), smoothly follows movement, doesn't rotate with player.

### Step 1: Create World Up Override Object
1. **GameObject** → **Create Empty**
2. Rename to `CameraWorldUp`
3. Set **Rotation**:
   - Tunic-style (angled): `X: 50, Y: 0, Z: 0`
   - Don't Starve (more top-down): `X: 70, Y: 0, Z: 0`
4. Position: `0, 0, 0` (doesn't matter)

### Step 2: Create Cinemachine Follow Camera
1. Select your **Wolf/Player** in Hierarchy
2. **GameObject** → **Cinemachine** → **Targeted Cameras** → **Follow Camera**
3. New GameObject created with:
   - Cinemachine Camera
   - Cinemachine Follow
   - Cinemachine Rotation Composer

### Step 3: Configure Cinemachine Brain
1. Select **Main Camera** in Hierarchy
2. Find **Cinemachine Brain** component (auto-added)
3. Set **World Up Override** → drag `CameraWorldUp` object here

### Step 4: Configure Cinemachine Camera
| Setting | Value |
|---------|-------|
| Tracking Target | Wolf/Player |
| Lens → Field of View | 40-50 |

### Step 5: Configure Cinemachine Follow
| Setting | Value | Purpose |
|---------|-------|---------|
| Follow Offset | `X: 0, Y: 10, Z: -8` | Height and distance |
| Damping | `X: 0.5, Y: 0.5, Z: 0.5` | Smoothness |

### Step 6: Configure Cinemachine Rotation Composer
| Setting | Value | Purpose |
|---------|-------|---------|
| Tracked Object Offset | `X: 0, Y: 1, Z: 0` | Look above feet |
| Lookahead Time | 0 | No anticipation |
| Damping | `X: 0.5, Y: 0.5` | Smooth rotation |
| Dead Zone | `X: 0.1, Y: 0.1` | No-move area |
| Soft Zone | `X: 0.5, Y: 0.5` | Slow-move area |

### Style Presets

**Tunic Style:**
- World Up Rotation: `X: 50`
- Follow Offset: `Y: 12, Z: -10`
- Field of View: 40

**Don't Starve Style:**
- World Up Rotation: `X: 70`
- Follow Offset: `Y: 15, Z: -5`
- Field of View: 50

### Troubleshooting
| Problem | Solution |
|---------|----------|
| Camera flips weirdly | Set World Up Override on Brain |
| Camera doesn't follow | Check Tracking Target assigned |
| Camera in ground | Increase Follow Offset Y |
| Jerky movement | Increase Damping values |
| Too slow/laggy | Decrease Damping values |

---

## NavMesh Setup for Animal Pathfinding

### Overview
Animals use NavMeshAgent to automatically path around forest obstacles. Player ignores NavMesh (uses CharacterController).

### Step 1: Add NavMesh Surface to Ground
1. Select **Ground** in Hierarchy
2. **Add Component** → **NavMesh Surface**
3. Settings:
   | Setting | Value |
   |---------|-------|
   | Agent Type | Humanoid |
   | Default Area | Walkable |
   | Use Geometry | Render Meshes |
4. Click **Bake**

### Step 2: Configure Forest as NavMesh Obstacle
1. Select each **Forest** GameObject
2. **Add Component** → **Nav Mesh Obstacle**
3. Settings:
   | Setting | Value |
   |---------|-------|
   | Shape | Capsule or Box |
   | Carve | ✓ (checked) |
4. Also add **Collider** with **Is Trigger = true** for player hiding

### Step 3: Add NavMeshAgent to Animal Prefab
1. Select **Animal** prefab
2. **Add Component** → **Nav Mesh Agent**
3. Settings:
   | Setting | Value |
   |---------|-------|
   | Speed | 3 (matches script) |
   | Angular Speed | 120 |
   | Stopping Distance | 0.5 |

### Verify NavMesh
- In Scene view, blue overlay = walkable area
- Holes around forests = animals will path around

---

## Wave System Setup

### Overview
The wave system triggers game changes when the player reaches score thresholds. It uses an event-driven architecture for flexibility.

### Components on Game Manager
The Game Manager object should have these components:
- `ScoreManager` - Tracks score and fires events
- `AnimalRegistry` - Tracks all animals (set Base Animal Speed = 3)
- `WaveManager` - Listens for score changes and triggers effects

### Configuring Wave Thresholds
In WaveManager Inspector, add elements to the Wave Thresholds list:

| Score | Effect Type | Int Param | Float Param | Result |
|-------|-------------|-----------|-------------|--------|
| 5 | SpawnHunters | 2 | - | Spawns 2 hunters |
| 10 | ChangeAnimalSpeed | - | 1.5 | Animals move 50% faster |

### Hunter Spawn Points
1. Create empty GameObjects at map edges
2. Add them to WaveManager's `Hunter Spawn Points` list
3. Assign Hunter prefab to `Hunter Prefab` field

### Adding New Effect Types
1. Add to `WaveEffectType` enum in WaveManager.cs
2. Add handling in `ExecuteEffect()` method
3. Configure threshold in Inspector

### Subscribing to Score Changes (For Future Systems)
```csharp
// In your script's Start():
ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;

// Handler method:
private void HandleScoreChanged(int oldScore, int newScore)
{
    // React to score changes
}

// In OnDestroy():
ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
```

---

## Hunter Bullet Setup

### Step 1: Create Bullet Prefab
1. **GameObject** → **3D Object** → **Sphere** (or any shape)
2. Scale down to small size (e.g., `0.2, 0.2, 0.2`)
3. Remove **Mesh Renderer** if you want invisible bullet, or add material
4. **Add Component** → **Sphere Collider** (or Box Collider)
5. Set **Is Trigger = true** on the Collider
6. **Add Component** → **HunterBullet** script
7. Save as prefab in `Assets/Prefabs/`

### Step 2: Configure Bullet Tag
1. **Edit** → **Project Settings** → **Tags and Layers**
2. Add new tag: `Bullet`
3. Select bullet prefab, set **Tag = Bullet**

### Step 3: Assign to Hunter
1. Select **Hunter** prefab or GameObject
2. In **HunterController** component, assign **Bullet Prefab**
3. Optionally create empty child as **Bullet Spawn Point**

### How It Works
1. Hunter detects wolf → enters Alert state → detection timer fills
2. Hunter enters Shooting state → spawns bullet prefab
3. Bullet travels in straight line toward wolf's position at spawn time
4. Wolf's PlayerInteractions detects bullet via OnTriggerEnter
5. PlayerInteractions calls WolfHealth.TakeDamage() and destroys bullet

---

## Current Progress (What's Working)

### Implemented ✅
- [x] Player movement (WASD, camera-relative)
- [x] Player dash (Shift, burst with cooldown)
- [x] Animal spawning (random Z position, configurable interval)
- [x] Animal movement (NavMesh pathfinding, natural wandering)
- [x] Animals path around forest obstacles
- [x] Player eats animals on contact (trigger collision)
- [x] Forest hiding system (player becomes hidden in forests)
- [x] Action ability framework (E key, area-based)
- [x] Spell ability framework (Q key, directional cone)
- [x] Top-down camera (Cinemachine 3)
- [x] 2D billboard sprites (face camera in 3D world)
- [x] Hunter AI with random wandering (NavMesh)
- [x] Hunter vision cone detection (range, angle, LOS)
- [x] Hunter respects isHidden flag (ignores hidden wolf)
- [x] Hunter detection timer before shooting
- [x] Hunter bullet projectiles (spawned, travel to wolf)
- [x] Wolf detects bullets via PlayerInteractions
- [x] Wolf health system (damage, death)
- [x] Score system with UI display
- [x] Wave system with score-based thresholds
- [x] Dynamic hunter spawning at thresholds
- [x] Dynamic animal speed changes at thresholds
- [x] Animal registry for runtime modifications

### Next Steps (TODO)
- [ ] Visual feedback when player is hidden
- [ ] Visual feedback when wolf takes damage
- [ ] Game over screen on wolf death
- [ ] Player leveling system (subscribe to OnScoreChanged)
