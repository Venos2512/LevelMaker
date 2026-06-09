# Level Builder - Runtime Setup Guide

## Overview
The Level Builder system is a **runtime Play mode tool** with **top-down camera** for building levels. Click directly on the map to place blocks!

## Key Features
- ✅ **Top-Down Camera**: Bird's eye view for easy placement
- ✅ **Click to Place**: Click exactly where you want blocks
- ✅ **Smooth Camera Pan**: WASD or Middle Mouse to move camera
- ✅ **Zoom In/Out**: Mouse wheel for perfect view
- ✅ **Replace Blocks**: Shift+Click to replace existing blocks
- ✅ **Save to Prefab**: Save your level as a prefab asset
- ✅ **Adjacent Building**: Blocks above ground must connect to others
- ✅ **Infinite Grid**: No boundaries, build anywhere!

---

## Quick Setup (5 minutes)

### Step 1: Create the Scene Setup

1. **Open your scene** (e.g., SampleScene)

2. **Create GridManager**:
   - Right-click in Hierarchy → Create Empty
   - Name it "GridManager"
   - Add Component → `GridManager` script
   - Set Cell Size to `1`
   - Check "Show Grid"

3. **Setup Main Camera**:
   - Select Main Camera
   - Add Component → `LevelBuilderCamera` script
   - Set Pan Speed: `20`
   - Position camera at `(0, 30, 0)` looking down
   - Rotation: `(60, 0, 0)` for angled top-down view

4. **Create Level Builder Object**:
   - Right-click in Hierarchy → Create Empty
   - Name it "LevelBuilder"
   - Add Component → `LevelBuilder` script
   - Drag GridManager to the `Grid Manager` field
   - Add Component → `LevelBuilderUI` script
   - Drag LevelBuilder script to UI's `Level Builder` field
   - Drag GridManager to UI's `Grid Manager` field

### Step 2: Test It!

1. **Press Play** ▶️
2. **Move camera**: WASD or drag with Middle Mouse
3. **Zoom**: Mouse wheel
4. **Place blocks**: Move mouse over grid, left click to place
5. **Delete**: Right click on block
6. **Replace**: Shift + Click on block
7. **Change type**: Press 1-5 keys
8. **Change layer**: Press [ or ] keys
9. **Save level**: Press P or Ctrl+S

---

## Controls Reference

### Camera Controls
- **WASD**: Pan camera horizontally
- **Middle Mouse Drag**: Pan camera
- **Mouse Wheel**: Zoom in/out
- **Right Mouse Drag**: Rotate view (optional)
- **Q/E Keys**: Rotate camera
- **Shift**: Pan faster
- **Edge Pan**: Move mouse to screen edge (optional)

### Building Controls
- **Left Click**: Place block at mouse position
- **Right Click**: Delete block at mouse position
- **Shift + Left Click**: Replace block
- **1-5 Keys**: Select block type (Cube, Sphere, Cylinder, Capsule, Plane)
- **[ ] Keys**: Change layer down/up
- **P Key**: Open save menu
- **Ctrl+S**: Quick save

### Placement Rules
- **Layer 0 (ground)**: Place anywhere
- **Layer 1+**: Must connect to adjacent block (no floating blocks)
- Blocks snap to grid automatically
- Preview shows green (valid) or red (invalid)
- Blue plane indicator shows hover position

---

## Saving Your Level

### Method 1: Save Dialog
1. Press **P** to open save menu
2. Type level name (e.g., "Level_01")
3. Click **Save**
4. Level saved to `Assets/Prefabs/Levels/Level_01.prefab`

### Method 2: Quick Save
1. Press **Ctrl+S**
2. Saves with default name (or last used name)

**Note**: Saved prefabs contain all blocks as a hierarchy. You can drag them into any scene!

---

## Tips & Tricks

### Building Efficiently
- Start at **Layer 0** to lay foundation
- Use **[ ]** to switch layers up/down
- **Shift+Click** to quickly replace blocks
- Build from bottom up - higher blocks need connections
- Use number keys 1-5 to quickly change block types

### Camera Navigation
- Hold **Shift** while panning to move faster
- **Middle mouse drag** for smooth, precise panning
- **Mouse wheel** to zoom in for detail work
- Pan camera to **screen edges** for auto-scrolling (if enabled)
- Use **Q/E** or **Right mouse** to rotate view for different angles

### Performance
- Preview blocks have no colliders (performance friendly)
- Only placed blocks have physics
- GridManager tracks all positions efficiently
- Hover indicator is a simple plane (lightweight)

---

## Troubleshooting

### "Preview not showing"
- Make sure Main Camera has Main Camera tag
- Check that you're in Play mode
- Move mouse over the grid area

### "Can't place blocks above ground"
- Blocks on Layer 1+ need adjacent connections
- Place at least one block touching the ground first
- Build continuously, no floating islands
- Use **[ ]** to switch to correct layer

### "Camera not moving"
- Check that LevelBuilderCamera is on Main Camera
- Try WASD keys or middle mouse drag
- Make sure you're in Play mode

### "Mouse click not placing"
- Move mouse over grid - preview should appear
- Green preview = valid, red = invalid
- Check if you're on correct layer with **[ ]**
- Right-click deletes, left-click places

### "Save button doesn't work"
- LevelBuilderUI script must be in the scene
- Check that GridManager reference is set
- Ensure Assets/Prefabs/Levels/ folder exists

### "Blocks snap incorrectly"
- Check GridManager Cell Size (default: 1)
- All blocks should have BlockData component
- GridManager must be in scene

### "Can't see where I'm placing"
- Zoom in with mouse wheel
- Blue plane indicator shows exact position
- Green/red preview shows valid/invalid

---

## Advanced Configuration

### Grid Settings (GridManager)
- **Cell Size**: Size of each grid cell (default: 1)
- **Grid Visual Size**: How many cells to show in gizmos
- **Grid Color**: Color of grid lines
- **Occupied Color**: Color showing used cells

### Builder Settings (LevelBuilder)
- **Max Place Distance**: How far raycast goes (default: 200)
- **Valid Color**: Preview color for valid placement (green)
- **Invalid Color**: Preview color for invalid placement (red)
- **Hover Color**: Ground indicator color (cyan/blue)

### Camera Settings (LevelBuilderCamera)
- **Pan Speed**: Base pan speed (default: 20)
- **Fast Pan Speed**: Speed when holding Shift (default: 40)
- **Zoom Speed**: How fast zoom changes (default: 10)
- **Min/Max Zoom**: Height limits (5 to 50)
- **Rotation Speed**: Rotation sensitivity (default: 100)
- **Allow Rotation**: Enable/disable camera rotation
- **Edge Pan Speed**: Speed when mouse at screen edge
- **Edge Pan Border**: Pixel distance from edge to trigger
- **Constrain To Bounds**: Limit camera movement area

---

## What Changed from Editor Mode?

### Removed ❌
- Editor Window (no more Tools > Level Maker)
- Resize/Move mode (use delete + replace instead)
- Boolean operations (may add later)
- Gizmo-based placement
- FPS camera controls

### Added ✅
- **Top-down camera** with pan and zoom
- **Click-to-place** at mouse cursor position
- **Hover indicators** (blue plane + preview)
- **Edge panning** for smooth navigation
- **Camera rotation** with right mouse or Q/E
- Replace functionality (Shift+Click)
- Better visual feedback
- Prefab save system
- On-screen HUD and instructions

### Kept ✔️
- Grid snapping system
- Layer-based building (now [ ] instead of Q/E)
- Adjacent connection rules
- BlockData component
- GridManager tracking

---

## Example Workflow

1. **Press Play** ▶️
2. **Position camera**: Pan with WASD, zoom with scroll
3. **Lay foundation**: Click on ground (Layer 0) to place cubes
4. **Build walls**: Press **]** for Layer 1, click next to floor blocks
5. **Add details**: Press **2-5** to use different shapes, click to place
6. **Fix mistakes**: Shift+Click to replace, Right-Click to delete
7. **Save**: Press **P**, name it, click Save
8. **Use prefab**: Drag from `Assets/Prefabs/Levels/` into any scene!

---

## Next Steps

- Build your first test level with top-down view
- Experiment with camera zoom and pan
- Try building multi-layer structures
- Use different block types (1-5 keys)
- Save and reuse prefabs in other scenes

Happy building! 🎮🏗️
