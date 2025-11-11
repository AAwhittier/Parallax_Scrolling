# 🎮 Unity Parallax Scrolling - Setup Guide

## Required Unity Inspector Connections

### Step-by-Step Setup Instructions

#### 1️⃣ Camera Setup
**What you need:**
- Your main 2D camera

**How to connect:**
1. Select the `Parallax System` GameObject
2. In the `ParallaxController` component:
   - **Target Camera** field → Drag your Main Camera here
   - (Or leave empty - it auto-detects Camera.main)

---

#### 2️⃣ Layer Setup (for each background layer)

**What you need:**
- Background image sprites (from Assets/Images/Backgrounds/)

**How to connect:**
1. Create child GameObject under `Parallax System`
2. Add `SpriteRenderer` component
   - **Sprite** field → Drag your background image here
   - **Sorting Layer** → Set to "Background" (or create it first)
   - **Order in Layer** → Set based on depth (-5 for furthest, -1 for closest)

3. Add `ParallaxLayer` component
   - **Sprite Renderer** → Auto-assigned (or drag the SpriteRenderer)
   - **Parallax Speed** → Set value:
     - 0.1-0.2 for far backgrounds (sky, distant mountains)
     - 0.4-0.6 for mid-range (hills, clouds)
     - 0.7-0.9 for close elements (trees, ground)
   - **Infinite Scrolling** → Check this ✓

---

## 🎯 Quick Setup Checklist

### Scene Hierarchy Structure
```
Scene
├── Main Camera (must have "MainCamera" tag)
├── Parallax System (with ParallaxController)
│   ├── Layer_1_Sky (SpriteRenderer + ParallaxLayer)
│   ├── Layer_2_Clouds (SpriteRenderer + ParallaxLayer)
│   ├── Layer_3_Mountains (SpriteRenderer + ParallaxLayer)
│   └── Layer_4_Trees (SpriteRenderer + ParallaxLayer)
└── Player (your character/camera controller)
```

### Inspector Connections Summary

| GameObject | Component | Field | Connect To |
|------------|-----------|-------|------------|
| Parallax System | ParallaxController | Target Camera | Main Camera |
| Parallax System | ParallaxController | Parallax Layers | Auto-detected or manually add |
| Layer_1_Sky | ParallaxLayer | Sprite Renderer | Auto-assigned |
| Layer_1_Sky | SpriteRenderer | Sprite | Your sky image |

---

## 🖼️ Image Import Settings

When you add images to `Assets/Images/Backgrounds/`:

1. Select the image in Project window
2. In Inspector, configure:

```
Texture Type: Sprite (2D and UI)
Sprite Mode: Single
Pixels Per Unit: 100
Filter Mode: Bilinear (or Point for pixel art)
Compression: None (for best quality)
Max Size: 2048 or 4096 (depending on image size)
```

3. Click "Apply"

---

## 🎬 Testing Your Setup

### In Editor:
1. Click Play
2. Move the camera (or player) horizontally
3. You should see layers moving at different speeds

### If nothing moves:
- Check camera is assigned to ParallaxController
- Verify ParallaxLayer components are present
- Ensure "Auto Detect Layers" is checked on controller

---

## 🔧 Common Inspector Settings

### ParallaxController Settings
```
Target Camera: [Main Camera]          ← Drag camera here
Parallax Layers: Size X               ← Auto-filled if auto-detect enabled
  ├─ Element 0: Layer_1_Sky
  ├─ Element 1: Layer_2_Clouds
  └─ Element 2: Layer_3_Mountains
Update Method: LateUpdate             ← Keep as LateUpdate
Auto Detect Layers: ✓                 ← Check this
```

### ParallaxLayer Settings (Example - Sky)
```
Parallax Speed: 0.15                  ← Adjust for depth
Infinite Scrolling: ✓                 ← Enable for seamless loop
Sprite Renderer: [Auto-assigned]      ← Should auto-fill
Enable Vertical Parallax: □           ← Usually disabled
```

---

## 🎨 Adding New Layers

### Method 1: Manual (In Scene)
1. Right-click `Parallax System` → Create Empty
2. Name it (e.g., "Layer_5_Foreground")
3. Add Component → `Sprite Renderer`
4. Add Component → `Parallax Layer`
5. Assign sprite and configure speed
6. Done! (Auto-detected if enabled)

### Method 2: ScriptableObject (Reusable)
1. Right-click in Project → Create → Parallax → Layer Data
2. Configure settings in Inspector
3. Use in runtime or editor

---

## 📦 Example Layer Configuration

### 5-Layer Setup (MapleStory Style)

| Layer Name | Sprite | Parallax Speed | Z Position | Order in Layer |
|------------|--------|----------------|------------|----------------|
| Layer_1_Sky | sky.png | 0.1 | 0 | -5 |
| Layer_2_Clouds | clouds.png | 0.25 | 0.1 | -4 |
| Layer_3_Mountains | mountains.png | 0.4 | 0.2 | -3 |
| Layer_4_Hills | hills.png | 0.6 | 0.3 | -2 |
| Layer_5_Trees | trees.png | 0.8 | 0.4 | -1 |

**Position Setup:**
- All layers: X = 0, Y = 0 (or match your scene)
- Z position increases for closer layers

---

## ⚠️ Important Notes

### Camera Requirements:
- Must be tagged as "MainCamera" (if using auto-detect)
- Should be 2D perspective
- Recommended: Orthographic projection

### Sprite Requirements:
- Import as "Sprite (2D and UI)" texture type
- Width should be 2-3x your camera view for seamless scrolling
- Height should match or exceed camera view

### Performance Tips:
- Keep layer count reasonable (3-6 layers)
- Use compressed textures for large backgrounds
- Disable layers not visible in current scene

---

## 🚀 You're Ready!

Your parallax system is now set up! The backgrounds will automatically scroll at different speeds as your camera moves, creating a depth illusion.

For advanced features and API usage, see: `Assets/Scripts/Parallax/README.md`
