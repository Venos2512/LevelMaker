# Level Maker - Runtime Edition

**Build levels with top-down camera and point-and-click placement!** 🎮🏗️

A Unity tool for rapid level prototyping with intuitive top-down view, click-to-place blocks, and prefab saving.

---

## ✨ Features

- **🎮 Top-Down Camera**: Bird's eye view for easy planning
- **🖱️ Click-to-Place**: Click exactly where you want blocks
- **📷 Pan & Zoom**: WASD + mouse wheel for navigation
- **🔄 Block Replacement**: Shift+Click to replace existing blocks
- **💾 Save to Prefab**: Export your level as reusable prefab
- **📐 Grid Snapping**: Automatic alignment to grid
- **🏗️ Layer System**: Build vertically with layer switching ([ ])
- **🔗 Adjacent Rules**: Blocks above ground must connect
- **♾️ Infinite Grid**: No boundaries, build anywhere
- **👁️ Visual Preview**: Green (valid) / Red (invalid) placement indicators

---

## 🚀 Quick Start (5 Minutes)

### 1. Setup Scene

1. **Create GridManager**:
   ```
   Hierarchy → Create Empty → Name: "GridManager"
   Add Component: GridManager
   Cell Size: 1
   ✓ Show Grid
   ```

2. **Setup Camera**:
   ```
   Select Main Camera
   Add Component: LevelBuilderCamera
   Position: (0, 30, 0)
   Rotation: (60, 0, 0)
   ```

3. **Create Builder**:
   ```
   Hierarchy → Create Empty → Name: "LevelBuilder"
   Add Component: LevelBuilder
   Add Component: LevelBuilderUI
   Assign references: GridManager, LevelBuilder
   ```

### 2. Play & Build!

1. Press **Play** ▶️
2. **WASD** or **Middle Mouse** to pan camera
3. **Scroll** to zoom in/out
4. **Move mouse** over grid - see preview
5. **Left Click** to place block at mouse position
6. **Right Click** to delete block
7. Press **1-5** to change block type
8. Press **[ ]** to change layer
9. Press **P** to save as prefab!

📖 **Full Setup Guide**: See [RUNTIME_SETUP_GUIDE.md](RUNTIME_SETUP_GUIDE.md)

---

## 🎮 Controls

### Camera
| Key | Action |
|-----|--------|
| **WASD** | Pan camera |
| **Middle Mouse** | Drag to pan |
| **Scroll** | Zoom in/out |
| **Right Mouse** | Rotate view |
| **Q/E** | Rotate camera |
| **Shift** | Pan faster |

### Building
| Key | Action |
|-----|--------|
| **Left Click** | Place at cursor |
| **Right Click** | Delete at cursor |
| **Shift+Click** | Replace block |
| **1-5** | Select block type |
| **[ ]** | Layer down/up |
| **P** | Save menu |
| **Ctrl+S** | Quick save |

---

## 📦 Project Structure

```
Assets/
├── Scripts/
│   ├── LevelBuilder.cs           # Main runtime controller
│   ├── LevelBuilderCamera.cs     # Camera movement
│   ├── LevelBuilderUI.cs         # UI & save system
│   ├── GridManager.cs            # Grid system
│   ├── BlockData.cs              # Block metadata
│   └── Editor/                   # (Legacy editor tools)
├── Scenes/
│   └── SampleScene.unity         # Setup your scene here
├── Prefabs/
│   └── Levels/                   # Saved level prefabs
└── README.md
```

---

## 💡 How It Works

### Grid System
- All blocks snap to 1x1x1 grid cells
- GridManager tracks occupied positions
- Prevents overlapping placements
- Infinite grid with no boundaries

### Layer Building
- **Layer 0** (ground): Place anywhere
- **Layer 1+**: Must connect to adjacent block
- No floating structures allowed
- Press Q/E to switch layers

### Block Types
1. **Cube** - Standard block
2. **Sphere** - Rounded shape
3. **Cylinder** - Column shape
4. **Capsule** - Pill shape
5. **Plane** - Flat surface

### Save System
- Creates prefab in `Assets/Prefabs/Levels/`
- Contains all blocks as hierarchy
- Drag into any scene to reuse
- Preserves positions and types

---

## 📝 Building Rules

✅ **Allowed**:
- Place on ground (Layer 0)
- Stack blocks vertically
- Place next to existing blocks
- Replace any block with Shift+Click

❌ **Not Allowed**:
- Floating blocks (no adjacent support)
- Overlapping placements
- Diagonal-only connections

---

## 🎯 Example Workflow

1. **Press Play** ▶️
2. Build floor at Layer 0 (press 1 for cubes)
3. Press **E** to go to Layer 1
4. Build walls around floor
5. Press **E** again for Layer 2
6. Add roof or details
7. Press **P** to save as "MyLevel"
8. Prefab created at `Assets/Prefabs/Levels/MyLevel.prefab`
9. Drag prefab into other scenes!

---

## 🔧 Customization

### Grid Settings (GridManager)
```csharp
cellSize = 1f;              // Grid cell size
gridVisualSize = 20;        // Grid display size
showGrid = true;            // Show grid lines
gridColor = Color.gray;     // Grid line color
```

### Builder Settings (LevelBuilder)
```csharp
maxPlaceDistance = 100f;    // Raycast distance
validColor = Color.green;   // Valid placement color
invalidColor = Color.red;   // Invalid placement color
```

### Camera Settings (LevelBuilderCamera)
```csharp
moveSpeed = 10f;            // Movement speed
mouseSensitivity = 2f;      // Look sensitivity
invertY = false;            // Invert Y axis
```

---

## 🐛 Troubleshooting

### Preview not visible
- ✅ Camera must have "MainCamera" tag
- ✅ Must be in Play mode
- ✅ Click to lock cursor

### Can't place blocks above ground
- ✅ Need adjacent block connection
- ✅ Build from ground up
- ✅ No floating allowed

### Save not working
- ✅ Check LevelBuilderUI references
- ✅ Verify `Assets/Prefabs/Levels/` exists
- ✅ Must be in Editor (not standalone build)

---

## 📚 Documentation

- **[RUNTIME_SETUP_GUIDE.md](RUNTIME_SETUP_GUIDE.md)** - Complete setup instructions
- **[GRID_QUICKSTART.md](GRID_QUICKSTART.md)** - Legacy grid system guide
- **[LEVEL_MAKER_DOCUMENTATION.md](LEVEL_MAKER_DOCUMENTATION.md)** - Legacy editor tools

---

## 🆚 Editor vs Runtime

### Old (Editor Mode)
- ❌ Editor window required
- ❌ Scene view only
- ❌ No smooth camera
- ✔️ Resize/Boolean ops

### New (Runtime Mode)
- ✅ Works in Play mode
- ✅ FPS-style controls
- ✅ Smooth movement
- ✅ Replace blocks
- ✅ Save to prefab
- ⚠️ No resize (use replace)

---

## 🎓 Requirements

- Unity 2021.3 or newer
- URP optional (works with built-in)
- No external dependencies

---

## 🤝 Contributing

This tool is designed for rapid prototyping. Feel free to extend it with:
- Custom block materials
- More shapes
- Undo/Redo system
- Multiplayer support
- Terrain integration

---

## 📄 License

Free to use and modify for any project.

---

## 🎉 Credits

Built for rapid level design prototyping.  
Inspired by Minecraft creative mode building mechanics.

---

**Happy Building!** 🏗️✨

For detailed setup, see [RUNTIME_SETUP_GUIDE.md](RUNTIME_SETUP_GUIDE.md)
