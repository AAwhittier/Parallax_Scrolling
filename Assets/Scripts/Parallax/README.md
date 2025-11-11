# Parallax Scrolling System for Unity 6

A robust, modular parallax scrolling system designed for 2D sidescrolling games (MapleStory-style). This system allows easy addition/removal of parallax layers with full customization.

## 📁 File Structure

```
Assets/
├── Scripts/
│   └── Parallax/
│       ├── ParallaxController.cs    # Main controller - manages all layers
│       ├── ParallaxLayer.cs         # Individual layer behavior
│       ├── ParallaxLayerData.cs     # ScriptableObject for layer config
│       └── ParallaxHelper.cs        # Utility functions
└── Images/
    └── Backgrounds/                 # Place your background images here
```

## 🚀 Quick Start Guide

### Method 1: Manual Setup (Recommended for Learning)

1. **Create the Parallax System**
   - Create an empty GameObject in your scene (Right-click in Hierarchy > Create Empty)
   - Name it "Parallax System"
   - Add the `ParallaxController` component to it

2. **Setup Camera Reference**
   - The controller will auto-detect `Camera.main`
   - Or manually assign your camera in the Inspector under "Target Camera"

3. **Create Parallax Layers**
   - For each background layer:
     - Create a new GameObject as a child of "Parallax System"
     - Name it descriptively (e.g., "Sky", "Mountains", "Trees", "Ground")
     - Add a `SpriteRenderer` component
     - Add a `ParallaxLayer` component
     - Assign your background sprite to the SpriteRenderer

4. **Configure Layer Settings**
   - Adjust each layer's parallax speed:
     - **Sky/Far Background**: 0.1 - 0.2 (slowest, furthest away)
     - **Mountains/Mid Background**: 0.3 - 0.5
     - **Trees/Near Background**: 0.6 - 0.8
     - **Foreground Elements**: 0.9 - 1.0 (fastest, closest)

5. **Enable Auto-Detection**
   - On the `ParallaxController`, check "Auto Detect Layers"
   - Layers will be automatically found and added to the list

### Method 2: Using ScriptableObjects (Recommended for Production)

1. **Create Layer Data Assets**
   - Right-click in Project window
   - Create > Parallax > Layer Data
   - Name it (e.g., "Sky_Layer", "Mountains_Layer")
   - Configure settings in Inspector:
     - Assign sprite
     - Set parallax speed
     - Configure sorting layer/order

2. **Create Layers from Data**
   - Use the `ParallaxLayerData.CreateLayerGameObject()` method
   - Or manually apply data with `ApplyToLayer()`

## 🎮 Unity Inspector Settings

### ParallaxController Component

| Field | Description | Default |
|-------|-------------|---------|
| **Target Camera** | Camera to follow (usually Main Camera) | Auto-assigned |
| **Parallax Layers** | List of all layers to control | Empty |
| **Update Method** | When to update (Update/LateUpdate/FixedUpdate) | LateUpdate |
| **Auto Detect Layers** | Automatically find child ParallaxLayer components | True |

### ParallaxLayer Component

| Field | Description | Typical Values |
|-------|-------------|----------------|
| **Parallax Speed** | Movement speed multiplier | 0.1 - 1.0 |
| **Infinite Scrolling** | Enable seamless looping | True |
| **Sprite Renderer** | Reference to sprite renderer | Auto-assigned |
| **Enable Vertical Parallax** | Allow vertical parallax effect | False |
| **Vertical Parallax Speed** | Vertical movement multiplier | 0.5 |

## 📊 Recommended Layer Setup (MapleStory Style)

Here's a typical parallax setup with 5 layers:

```
Parallax System/
├── Layer_1_Sky (Speed: 0.1, Z: 0)
├── Layer_2_Clouds (Speed: 0.25, Z: 0.1)
├── Layer_3_Mountains (Speed: 0.4, Z: 0.2)
├── Layer_4_Trees (Speed: 0.65, Z: 0.3)
└── Layer_5_Ground (Speed: 0.85, Z: 0.4)
```

### Sorting Layers Setup

1. Open Unity's Tags & Layers (Edit > Project Settings > Tags and Layers)
2. Add these Sorting Layers (in order):
   - `Background` (for parallax layers)
   - `Default` (for gameplay elements)
   - `Foreground`

3. Assign layers in your sprites:
   - All parallax layers: `Background`
   - Order in Layer: -5 (furthest) to -1 (closest)

## 🎨 Working with Your Background Images

### Preparing Images

1. **Import Images**
   - Place PNG/JPG files in `Assets/Images/Backgrounds/`
   - Select the image in Unity
   - In Inspector:
     - Texture Type: `Sprite (2D and UI)`
     - Sprite Mode: `Single`
     - Pixels Per Unit: `100` (adjust for your game scale)
     - Filter Mode: `Bilinear` or `Point` (for pixel art)
     - Compression: `None` for best quality

2. **Create Sprites from Images**
   - Drag the image from Project to Scene
   - Or assign to SpriteRenderer's Sprite field

### Image Size Recommendations

- **Vertical Resolution**: Match or exceed your camera's orthographic size
- **Horizontal Width**: At least 2x screen width for seamless scrolling
- **Example**: For 1920x1080 game, use images at least 3840x1080

### Handling Infinite Scrolling

The system automatically handles infinite scrolling. Ensure:
- Image width is sufficient (2-3x camera view)
- Texture Wrap Mode is set to `Repeat` (optional, for tiling)

## 🛠️ Runtime API Usage

### Adding/Removing Layers at Runtime

```csharp
using ParallaxScrolling;

// Get controller reference
ParallaxController controller = FindObjectOfType<ParallaxController>();

// Add a new layer
ParallaxLayer newLayer = CreateNewLayer();
controller.AddLayer(newLayer);

// Remove a layer
controller.RemoveLayer(someLayer);

// Clear all layers
controller.ClearLayers();
```

### Controlling Parallax

```csharp
// Change a layer's speed
layer.SetParallaxSpeed(0.5f);

// Reset all layers to start position
controller.ResetAllLayers();

// Enable/disable parallax
controller.SetParallaxEnabled(false);
```

### Using Helper Functions

```csharp
using ParallaxScrolling;

// Auto-setup typical layer speeds
ParallaxHelper.SetupTypicalLayerDepths(controller);

// Calculate speed for a visual distance
float speed = ParallaxHelper.CalculateParallaxSpeed(0.7f);
```

## 🎯 Common Use Cases

### 1. Simple 3-Layer Background

```
Layer 1 (Sky): Speed 0.1
Layer 2 (Hills): Speed 0.4
Layer 3 (Trees): Speed 0.7
```

### 2. Complex Multi-Layer Scene

```
Layer 1 (Far Sky): Speed 0.05
Layer 2 (Clouds): Speed 0.15
Layer 3 (Mountains): Speed 0.3
Layer 4 (Mid Hills): Speed 0.5
Layer 5 (Trees): Speed 0.7
Layer 6 (Ground): Speed 0.9
```

### 3. Vertical Scrolling (Optional)

Enable vertical parallax for games with vertical camera movement:
- Enable "Enable Vertical Parallax" on layers
- Set "Vertical Parallax Speed" (usually same as horizontal)

## 🐛 Troubleshooting

### Layers not moving
- Ensure camera is assigned to ParallaxController
- Check that ParallaxLayer components are added
- Verify "Auto Detect Layers" is enabled or layers are manually added

### Layers moving too fast/slow
- Adjust parallax speed values (0.1 = very slow, 1.0 = same speed as camera)
- Use ParallaxHelper.SetupTypicalLayerDepths() for automatic setup

### Gaps in infinite scrolling
- Ensure sprite width is sufficient (2-3x camera view)
- Check sprite renderer bounds
- Verify "Infinite Scrolling" is enabled

### Sorting issues (layers in wrong order)
- Adjust Z positions (further back = lower Z)
- Set sorting order in SpriteRenderer
- Check Sorting Layer settings

## 📚 Code Architecture

### Components Overview

- **ParallaxController**: Central manager, coordinates all layers
- **ParallaxLayer**: Individual layer logic, handles movement and infinite scrolling
- **ParallaxLayerData**: ScriptableObject for reusable configurations
- **ParallaxHelper**: Utility functions for common tasks

### Update Flow

1. Camera moves in scene
2. ParallaxController.LateUpdate() called
3. Each ParallaxLayer.UpdateParallax() calculates offset
4. Layers move proportionally to parallax speed
5. Infinite scrolling repositions sprites if needed

## 🔧 Advanced Customization

### Custom Update Timing

Change when parallax updates:
- `Update`: Standard game loop
- `LateUpdate`: After all Update() calls (recommended for smooth camera following)
- `FixedUpdate`: Physics-synced updates

### Performance Optimization

- Use sprite atlases for multiple layers
- Disable layers far off-screen
- Use appropriate texture compression
- Consider object pooling for dynamic layers

## 💡 Tips & Best Practices

1. **Layer Count**: 3-6 layers is ideal for most games
2. **Speed Distribution**: Space out speeds evenly (don't use too similar values)
3. **Z-Position**: Use small increments (0.1) to avoid rendering issues
4. **Naming**: Use clear, numbered names (Layer_1_Sky, Layer_2_Mountains)
5. **Testing**: Use Unity's Scene view to verify layer depth and positioning

## 📝 License & Credits

Created for Unity 6 2D projects. Feel free to modify and extend for your project needs.
