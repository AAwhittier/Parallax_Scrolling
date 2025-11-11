# 🎮 Camera System - Quick Setup Guide

## Arrow Key Camera Control (Simple Scene Exploration)

**Perfect for viewing your parallax scrolling backgrounds!**

### 5-Minute Setup

#### 1. Select Your Camera
- In Hierarchy, click on `Main Camera`

#### 2. Add Camera Controller
- In Inspector, click `Add Component`
- Search for: `CameraController2D`
- Click to add

#### 3. Configure Settings (Inspector)

```
CameraController2D Component:
─────────────────────────────────
Movement Settings:
  Move Speed: 5                    ← Adjust to your preference
  Smooth Movement: ✓               ← Checked (for smooth movement)
  Smooth Time: 0.1                 ← Lower = smoother

Input Settings:
  Use Arrow Keys: ✓                ← Checked
  Use WASD: ✓                      ← Checked (optional)
  Use Mouse Drag: □                ← Unchecked (optional feature)

Constraints:
  Enable Constraints: □            ← Optional (see below)
```

#### 4. Press Play!
- Use **Arrow Keys** or **WASD** to move the camera
- Camera moves smoothly around your scene
- Perfect for viewing your parallax backgrounds!

---

## 🎯 Optional: Add Movement Boundaries

If you want to prevent the camera from moving too far:

1. On `CameraController2D` component, check **Enable Constraints**
2. Set the boundaries:
   ```
   Enable Constraints: ✓
   Min X: -20          ← Left boundary
   Max X: 20           ← Right boundary
   Min Y: -10          ← Bottom boundary
   Max Y: 10           ← Top boundary
   ```
3. Yellow box appears in Scene view showing boundaries

---

## 🎮 Controls Reference

### Default Controls

| Input | Action |
|-------|--------|
| ↑ Arrow Up / W | Move camera up |
| ↓ Arrow Down / S | Move camera down |
| ← Arrow Left / A | Move camera left |
| → Arrow Right / D | Move camera right |

### Optional Controls (if enabled)

| Input | Action |
|-------|--------|
| Right Mouse + Drag | Pan camera |
| Mouse Scroll Wheel | Zoom in/out (requires CameraZoom) |
| + / - Keys | Zoom in/out (requires CameraZoom) |

---

## 🚀 Advanced Setup: Follow Player

If you want the camera to follow a player character:

### Setup Steps

1. **Select Main Camera**
2. **Remove or disable** `CameraController2D` (you don't need manual control)
3. **Add Component** → `CameraFollowTarget`
4. **Configure:**
   ```
   Target: [Drag your Player here]  ← Your player GameObject
   Offset: (0, 0, -10)              ← Default for 2D
   Smooth Time: 0.3                 ← Adjust smoothness
   Follow X: ✓                      ← Follow horizontal
   Follow Y: ✓                      ← Follow vertical
   ```

5. **Press Play** - Camera follows your player!

---

## 🔒 Add Zoom Feature

Want to zoom in and out?

1. **Select Main Camera**
2. **Add Component** → `CameraZoom`
3. **Configure:**
   ```
   Min Zoom: 2              ← Zoomed in (closer)
   Max Zoom: 10             ← Zoomed out (farther)
   Default Zoom: 5          ← Starting zoom
   Enable Scroll Wheel: ✓   ← Mouse wheel zoom
   Enable Keyboard: ✓       ← +/- keys zoom
   ```

4. **Press Play** - Use mouse wheel or +/- keys to zoom!

---

## 🎬 Add Screen Shake (For Effects)

Want camera shake for impacts?

1. **Select Main Camera**
2. **Add Component** → `CameraShake`
3. **In your code**, trigger shake:

```csharp
using CameraSystem;

void OnPlayerHit()
{
    Camera.main.GetComponent<CameraShake>().ShakeMedium();
}
```

**Preset Shakes:**
- `ShakeLight()` - Small shake
- `ShakeMedium()` - Medium shake
- `ShakeHeavy()` - Big shake

---

## 📐 Add Camera Boundaries

Prevent camera from showing outside your level:

1. **Select Main Camera**
2. **Add Component** → `CameraBounds`
3. **Configure:**
   ```
   Enable Bounds: ✓
   Min X: -20      ← Based on your level
   Max X: 20       ← Based on your level
   Min Y: -10      ← Based on your level
   Max Y: 10       ← Based on your level
   ```

4. **Gizmos show boundaries** in Scene view (yellow box)

### Auto-Fit to Collider (Advanced)

If you have a BoxCollider2D defining your level:

1. Check **Auto Fit To Collider**
2. Drag your level's BoxCollider2D to **Bounds Collider**
3. Boundaries automatically match your collider!

---

## 🎨 Recommended Setups

### For Parallax Background Viewing
```
Main Camera
├─ CameraController2D ✓
└─ CameraZoom ✓ (optional)
```

### For Side-Scroller Game (MapleStory-style)
```
Main Camera
├─ CameraFollowTarget ✓ (follow player)
├─ CameraBounds ✓ (limit to level)
└─ CameraShake ✓ (optional, for effects)
```

### For Top-Down Game
```
Main Camera
├─ CameraFollowTarget ✓
├─ CameraZoom ✓
└─ CameraBounds ✓
```

### For Free Exploration
```
Main Camera
├─ CameraController2D ✓
├─ CameraZoom ✓
└─ CameraBounds ✓ (optional)
```

---

## 🎯 Quick Test Checklist

After setup, test these:

- [ ] Camera moves with arrow keys/WASD
- [ ] Movement is smooth (if enabled)
- [ ] Camera stays within boundaries (if enabled)
- [ ] Zoom works with mouse wheel (if enabled)
- [ ] Camera follows player (if using follow mode)

---

## ⚡ Integration with Parallax System

The camera works automatically with parallax:

1. **Setup camera** (using any method above)
2. **Setup parallax system** (see PARALLAX_SETUP_GUIDE.md)
3. **Press Play** - They work together!

As you move the camera (manually or following player), parallax layers automatically scroll at different speeds.

**Example Scene:**
```
Hierarchy:
├─ Main Camera (CameraController2D)
├─ Parallax System
│  ├─ Sky Layer
│  ├─ Mountain Layer
│  └─ Tree Layer
└─ Player (optional)
```

---

## 🐛 Troubleshooting

**Camera not moving:**
- Check that the component is enabled (checkbox in Inspector)
- Verify Use Arrow Keys or Use WASD is checked
- Make sure you're in Play mode

**Camera movement is jerky:**
- Enable "Smooth Movement"
- Increase "Smooth Time" value (try 0.2-0.3)

**Camera goes too far:**
- Enable "Enable Constraints"
- Set appropriate Min/Max values
- Or add CameraBounds component

**Zoom not working:**
- Ensure camera is Orthographic (not Perspective)
- Check Min/Max zoom values aren't the same
- Verify scroll wheel or keyboard zoom is enabled

---

## 📚 More Info

For detailed documentation, API reference, and code examples:
- See: `Assets/Scripts/CameraSystem/README.md`

For parallax scrolling setup:
- See: `PARALLAX_SETUP_GUIDE.md`

---

## ✅ You're Ready!

Your camera is now set up for exploration. Move around your scene and enjoy your parallax scrolling backgrounds!

**Next Steps:**
1. Import your background images to `Assets/Images/Backgrounds/`
2. Set up parallax layers (see PARALLAX_SETUP_GUIDE.md)
3. Use camera controls to explore your beautiful parallax scene!
