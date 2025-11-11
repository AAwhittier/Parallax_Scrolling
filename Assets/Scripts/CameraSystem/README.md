# 2D Camera System for Unity 6

A comprehensive, modular camera system for 2D games with minimal dependencies. Easily portable to any Unity 2D project.

## 📁 File Structure

```
Assets/Scripts/CameraSystem/
├── CameraController2D.cs      # Arrow key/WASD camera control
├── CameraFollowTarget.cs      # Smooth target following
├── CameraBounds.cs            # Movement boundary constraints
├── CameraZoom.cs              # Zoom in/out functionality
├── CameraShake.cs             # Screen shake effects
├── CameraUtilities.cs         # Helper utility functions
└── README.md                  # This file
```

## 🎮 Components Overview

### CameraController2D
**Purpose:** Manual camera control with keyboard input

**Features:**
- Arrow key and WASD movement
- Optional mouse drag (right-click)
- Smooth or instant movement
- Built-in movement constraints
- Adjustable speed

**Best for:** Free camera exploration, level editors, debugging

---

### CameraFollowTarget
**Purpose:** Automatically follow a game object (player, vehicle, etc.)

**Features:**
- Smooth following with configurable damping
- Look-ahead based on target velocity
- Dead zone (camera only moves when target leaves zone)
- Axis-specific following (X-only, Y-only, or both)
- Snap to target functionality

**Best for:** Following player characters, side-scrollers, platformers

---

### CameraBounds
**Purpose:** Constrain camera within defined boundaries

**Features:**
- Manual boundary definition
- Auto-fit to colliders
- Accounts for camera viewport size
- Visual gizmos in editor
- Runtime boundary updates

**Best for:** Limiting camera to playable area, level boundaries

---

### CameraZoom
**Purpose:** Control camera zoom level

**Features:**
- Mouse scroll wheel zoom
- Keyboard zoom (+ and - keys)
- Smooth zoom transitions
- Min/max zoom limits
- Instant or smooth zoom methods

**Best for:** Strategy games, inspection modes, accessibility

---

### CameraShake
**Purpose:** Add screen shake effects for impacts and events

**Features:**
- Basic and trauma-based shake modes
- Customizable duration and intensity
- Preset shake levels (light, medium, heavy)
- Gradual decay
- Perlin noise-based movement

**Best for:** Explosions, impacts, damage feedback, juice

---

### CameraUtilities
**Purpose:** Static helper functions for camera operations

**Features:**
- Viewport bounds calculations
- Position visibility checks
- Screen/world space conversions
- Distance calculations
- And more...

**Best for:** General camera-related calculations throughout your project

---

## 🚀 Quick Start Guide

### Setup 1: Arrow Key Camera Control (Scene Exploration)

**Perfect for viewing your parallax backgrounds!**

1. Select your Main Camera
2. Add Component → `CameraController2D`
3. Configure in Inspector:
   - **Move Speed:** `5` (adjust to preference)
   - **Smooth Movement:** ✓ (checked)
   - **Use Arrow Keys:** ✓ (checked)
   - **Use WASD:** ✓ (checked - optional)

4. Press Play and use arrow keys to move around!

**Optional - Add Movement Limits:**
- Check **Enable Constraints**
- Set Min/Max X and Y to match your scene bounds

---

### Setup 2: Follow Player Character

1. Select your Main Camera
2. Add Component → `CameraFollowTarget`
3. Configure in Inspector:
   - **Target:** Drag your Player GameObject here
   - **Offset:** `(0, 0, -10)` (default for 2D)
   - **Smooth Time:** `0.3` (adjust for smoothness)
   - **Follow X:** ✓
   - **Follow Y:** ✓

**Optional - Add Look Ahead:**
- Check **Enable Look Ahead**
- **Look Ahead Distance:** `2`
- **Look Ahead Smoothing:** `0.5`

---

### Setup 3: Combine Follow + Bounds

1. Add both `CameraFollowTarget` AND `CameraBounds` to your camera
2. Configure `CameraFollowTarget` to follow your player
3. Configure `CameraBounds` to limit movement to your level
4. They work together automatically!

---

### Setup 4: Add Zoom

1. Add Component → `CameraZoom`
2. Configure:
   - **Min Zoom:** `2` (zoomed in)
   - **Max Zoom:** `10` (zoomed out)
   - **Default Zoom:** `5`
   - **Enable Scroll Wheel Zoom:** ✓

3. Use mouse wheel to zoom in/out during play

---

### Setup 5: Add Screen Shake

1. Add Component → `CameraShake`
2. In your code, call shake when needed:

```csharp
using CameraSystem;

// Get camera shake component
CameraShake shake = Camera.main.GetComponent<CameraShake>();

// Trigger shake
shake.ShakeMedium();
// or
shake.Shake(0.5f, 0.3f); // duration, intensity
```

---

## 📊 Unity Inspector Reference

### CameraController2D Settings

| Field | Description | Typical Value |
|-------|-------------|---------------|
| **Move Speed** | Units per second | 5.0 |
| **Smooth Movement** | Enable damping | ✓ |
| **Smooth Time** | Damping amount | 0.1 |
| **Use Arrow Keys** | Enable arrow keys | ✓ |
| **Use WASD** | Enable WASD | ✓ |
| **Use Mouse Drag** | Enable right-click drag | □ |
| **Enable Constraints** | Limit movement area | Optional |

### CameraFollowTarget Settings

| Field | Description | Typical Value |
|-------|-------------|---------------|
| **Target** | GameObject to follow | Your Player |
| **Offset** | Distance from target | (0, 0, -10) |
| **Smooth Time** | Follow smoothness | 0.3 |
| **Follow X** | Follow horizontal | ✓ |
| **Follow Y** | Follow vertical | ✓ |
| **Enable Look Ahead** | Predict movement | Optional |
| **Enable Dead Zone** | Only move outside zone | Optional |

### CameraBounds Settings

| Field | Description | Typical Value |
|-------|-------------|---------------|
| **Enable Bounds** | Activate constraints | ✓ |
| **Min X / Max X** | Horizontal limits | Based on level |
| **Min Y / Max Y** | Vertical limits | Based on level |
| **Auto Fit To Collider** | Use collider bounds | Optional |

### CameraZoom Settings

| Field | Description | Typical Value |
|-------|-------------|---------------|
| **Min Zoom** | Max zoom in | 2.0 |
| **Max Zoom** | Max zoom out | 10.0 |
| **Default Zoom** | Starting zoom | 5.0 |
| **Zoom Speed** | Transition speed | 5.0 |
| **Enable Scroll Wheel** | Mouse zoom | ✓ |
| **Enable Keyboard** | +/- keys zoom | ✓ |

### CameraShake Settings

| Field | Description | Typical Value |
|-------|-------------|---------------|
| **Default Duration** | Shake length | 0.5 |
| **Default Intensity** | Shake strength | 0.3 |
| **Use Trauma Shake** | Realistic mode | ✓ |
| **Trauma Decay** | Fade speed | 1.5 |

---

## 💻 Code Examples

### Example 1: Manual Camera Control
```csharp
using UnityEngine;
using CameraSystem;

public class GameManager : MonoBehaviour
{
    private CameraController2D cameraController;

    void Start()
    {
        cameraController = Camera.main.GetComponent<CameraController2D>();
    }

    void Update()
    {
        // Reset camera position
        if (Input.GetKeyDown(KeyCode.R))
        {
            cameraController.SetPosition(0, 0);
        }

        // Change speed
        if (Input.GetKeyDown(KeyCode.F))
        {
            cameraController.SetMoveSpeed(10f);
        }
    }
}
```

### Example 2: Follow Target
```csharp
using UnityEngine;
using CameraSystem;

public class PlayerController : MonoBehaviour
{
    void Start()
    {
        // Assign camera to follow this object
        CameraFollowTarget followCam = Camera.main.GetComponent<CameraFollowTarget>();
        followCam.SetTarget(transform);
    }
}
```

### Example 3: Dynamic Bounds
```csharp
using UnityEngine;
using CameraSystem;

public class LevelManager : MonoBehaviour
{
    void Start()
    {
        CameraBounds bounds = Camera.main.GetComponent<CameraBounds>();

        // Set bounds based on level
        bounds.SetBounds(-20f, 20f, -10f, 10f);
    }
}
```

### Example 4: Zoom Control
```csharp
using UnityEngine;
using CameraSystem;

public class ZoomController : MonoBehaviour
{
    private CameraZoom cameraZoom;

    void Start()
    {
        cameraZoom = Camera.main.GetComponent<CameraZoom>();
    }

    public void ZoomToPlayer()
    {
        cameraZoom.SetZoomSmooth(3f); // Zoom in
    }

    public void ZoomOut()
    {
        cameraZoom.ResetZoom(); // Return to default
    }
}
```

### Example 5: Shake Effects
```csharp
using UnityEngine;
using CameraSystem;

public class Combat : MonoBehaviour
{
    private CameraShake cameraShake;

    void Start()
    {
        cameraShake = Camera.main.GetComponent<CameraShake>();
    }

    void OnPlayerHit()
    {
        cameraShake.ShakeLight(); // Small shake
    }

    void OnExplosion()
    {
        cameraShake.ShakeHeavy(); // Big shake
    }

    void OnBossAttack()
    {
        cameraShake.Shake(1.0f, 0.8f); // Custom shake
    }
}
```

### Example 6: Using Utilities
```csharp
using UnityEngine;
using CameraSystem;

public class UtilityExample : MonoBehaviour
{
    void Update()
    {
        Camera cam = Camera.main;

        // Get mouse position in world
        Vector2 mouseWorld = CameraUtilities.GetMouseWorldPosition2D(cam);

        // Check if position is visible
        bool visible = CameraUtilities.IsPositionVisible(cam, transform.position);

        // Get camera viewport bounds
        Bounds viewBounds = CameraUtilities.GetCameraViewBounds(cam);
    }
}
```

---

## 🎯 Common Use Cases

### Use Case 1: Side-Scroller (MapleStory-style)
```
Components needed:
- CameraFollowTarget (follow player)
- CameraBounds (constrain to level)
- CameraShake (optional, for impacts)

Setup:
1. Add CameraFollowTarget, set Target to player
2. Set Offset to (0, 1, -10) - slightly above player
3. Enable Follow X, disable Follow Y (or enable for vertical levels)
4. Add CameraBounds, set to level dimensions
```

### Use Case 2: Top-Down Game
```
Components needed:
- CameraFollowTarget
- CameraZoom (for strategic view)
- CameraBounds

Setup:
1. Position camera above scene (e.g., rotation X: 90)
2. Add CameraFollowTarget, follow player
3. Add CameraZoom for zoom control
4. Add CameraBounds to keep camera in playable area
```

### Use Case 3: Free Exploration/Level Editor
```
Components needed:
- CameraController2D
- CameraZoom

Setup:
1. Add CameraController2D
2. Enable arrow keys and mouse drag
3. Add CameraZoom for inspection
4. Optionally add CameraBounds to prevent flying too far
```

### Use Case 4: Cutscenes
```
Use code to control camera:
- Disable CameraController2D during cutscene
- Use transform.position for scripted movement
- Re-enable CameraController2D after cutscene
```

---

## 🔧 Advanced Tips

### Combining Multiple Components
You can mix and match components:
- **Follow + Bounds** = Player camera with limits
- **Follow + Zoom** = Dynamic camera distance
- **Controller + Zoom** = Manual exploration with zoom
- **Any combination + Shake** = Add juice to your game

### Performance Notes
- All components use LateUpdate() for smooth camera movement
- Minimal overhead, suitable for mobile
- No allocations in Update loops

### Portability
To use in a new project:
1. Copy the entire `CameraSystem` folder
2. Drag onto camera GameObject
3. Configure settings
4. Done! No other dependencies required

### Debugging
- All components show gizmos in Scene view when selected
- Check Console for warnings (null cameras, etc.)
- Use `SetEnabled(false)` to temporarily disable components

---

## 🎨 Integration with Parallax System

The camera system works seamlessly with the parallax scrolling system:

1. Add `CameraController2D` or `CameraFollowTarget` to your camera
2. The `ParallaxController` automatically uses `Camera.main`
3. As the camera moves, parallax layers update automatically
4. No additional setup required!

**Example Scene Hierarchy:**
```
Main Camera
├─ CameraController2D (or CameraFollowTarget)
├─ CameraBounds
└─ CameraZoom

Parallax System
├─ Layer_1_Sky
├─ Layer_2_Mountains
└─ Layer_3_Trees

Player (if using follow)
```

---

## 📝 API Reference

### CameraController2D
- `SetPosition(Vector3)` - Teleport camera
- `SetPosition(float x, float y)` - Teleport camera (2D)
- `MoveBy(Vector3)` - Move by offset
- `SetMoveSpeed(float)` - Change speed
- `SetEnabled(bool)` - Enable/disable
- `SetConstraints(...)` - Set boundaries
- `DisableConstraints()` - Remove boundaries

### CameraFollowTarget
- `SetTarget(Transform)` - Change target
- `SetOffset(Vector3)` - Change offset
- `SetSmoothTime(float)` - Change smoothness
- `SnapToTarget()` - Instant position update
- `SetEnabled(bool)` - Enable/disable

### CameraBounds
- `SetBounds(float, float, float, float)` - Set boundaries
- `SetBounds(Rect)` - Set from rect
- `SetBounds(Vector2, Vector2)` - Set from corners
- `UpdateCameraSize()` - Recalculate viewport
- `IsPositionInBounds(Vector3)` - Check if in bounds
- `GetBounds()` - Get bounds as Rect
- `GetBoundsCenter()` - Get center point

### CameraZoom
- `SetZoom(float)` - Instant zoom
- `SetZoomSmooth(float)` - Smooth zoom
- `ResetZoom()` - Return to default
- `ZoomIn(float)` - Zoom in by amount
- `ZoomOut(float)` - Zoom out by amount
- `SetZoomLimits(float, float)` - Change limits
- `GetCurrentZoom()` - Get current value
- `GetTargetZoom()` - Get target value

### CameraShake
- `Shake()` - Default shake
- `Shake(float intensity)` - Custom intensity
- `Shake(float duration, float intensity)` - Full custom
- `ShakeLight()` - Preset light shake
- `ShakeMedium()` - Preset medium shake
- `ShakeHeavy()` - Preset heavy shake
- `StopShake()` - Stop immediately
- `AddTrauma(float)` - Add trauma (trauma mode)
- `IsShaking()` - Check if shaking
- `GetTrauma()` - Get trauma level

### CameraUtilities (Static)
See `CameraUtilities.cs` for full list of helper functions.

---

## ⚠️ Troubleshooting

**Camera not moving:**
- Check that component is enabled
- Verify input settings (arrow keys, WASD enabled)
- Check that camera has Camera component

**Follow not working:**
- Ensure Target is assigned
- Check that target isn't null
- Verify Follow X/Y are enabled

**Bounds not constraining:**
- Ensure Enable Bounds is checked
- Verify min/max values are correct
- Call UpdateCameraSize() if camera size changed

**Shake not working:**
- Ensure camera has CameraShake component
- Check that Shake() is being called
- Verify intensity > 0

**Zoom not working:**
- Only works with orthographic cameras
- Check min/max zoom values
- Verify zoom controls are enabled

---

## 📚 Additional Resources

- Unity Manual: [Cameras](https://docs.unity3d.com/Manual/Cameras.html)
- Unity Manual: [2D Game Development](https://docs.unity3d.com/Manual/Unity2D.html)
- Unity Manual: [Orthographic Camera](https://docs.unity3d.com/Manual/class-Camera.html)

---

## 🎉 You're All Set!

This camera system provides everything you need for professional 2D camera control. Mix and match components to fit your game's needs, and enjoy clean, reusable code that works across projects!

For questions or issues, refer to the inline code comments in each script.
