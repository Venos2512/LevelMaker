# Level Maker Tool - Unity Editor Extension

## 📋 Tổng quan dự án

Level Maker là công cụ Editor mạnh mẽ cho Unity, được thiết kế để giúp game developers và level designers nhanh chóng tạo prototype maps bằng **hệ thống grid-based**:
- ✅ **Grid System**: Map chia thành cells, objects snap vào grid
- ✅ Đặt các khối cơ bản (Cube, Sphere, Cylinder, Capsule, Plane)
- ✅ Grid-based placement với collision detection
- ✅ Resize theo grid units (1x1x1, 2x1x1, 2x2x2, etc.)
- ✅ Boolean operations (Union, Subtract, Intersect) 
- ✅ Export/Import level layouts

## 🎯 Grid System (Version 2.0)

### Core Concept
Map được chia thành **lưới 3D** với các cells có kích thước cố định:
- **Cell Size**: Kích thước mỗi cell (VD: 1m)
- **Grid Dimensions**: Số lượng cells (VD: 20x5x20 = 20x20 map, 5 layers cao)
- **Grid Position**: Tọa độ trong grid (0,0,0), (5,2,3), etc.
- **Grid Size**: Object chiếm bao nhiêu cells (1x1x1, 2x1x1, 3x3x1, etc.)

### Why Grid-Based?
1. **Perfect Alignment**: Tất cả objects snap vào grid
2. **No Overlapping**: Collision detection tự động
3. **Modular Design**: Dễ tạo pieces tái sử dụng
4. **Easy Planning**: Design level trên giấy rồi implement chính xác
5. **Performance**: Optimization dễ dàng hơn

## 🚀 Cài đặt

### Yêu cầu hệ thống
- Unity 2021.3 hoặc mới hơn
- .NET 4.x hoặc cao hơn
- Universal Render Pipeline (optional)

### Các files đã được tạo

```
Assets/
├── Scripts/
│   ├── GridManager.cs                  # Grid system core (NEW!)
│   ├── BlockData.cs                    # Component lưu metadata
│   ├── LevelExporter.cs                # Logic export/import
│   ├── README.md                       # Tài liệu chi tiết
│   ├── QUICKSTART.md                   # Hướng dẫn cơ bản
│   ├── GRID_QUICKSTART.md              # Hướng dẫn Grid System (NEW!)
│   └── Editor/
│       ├── LevelMakerWindow.cs         # Main editor window
│       ├── GridManagerInspector.cs     # Grid manager inspector (NEW!)
│       ├── CSGOperations.cs            # Boolean operations
│       ├── LevelMakerInspector.cs      # Custom inspector
│       └── LevelExporterWindow.cs      # Export/Import UI
└── Levels/                             # Folder chứa exported levels
    └── README.md
```

## 🎯 Cách sử dụng

### 1. Mở công cụ
```
Unity MTạo Grid Manager (BẮT BUỘC!)
```
1. Trong Level Maker window, tab "Place"
2. Click "Create Grid Manager"
3. Configure settings:
   - Cell Size: 1.0 (1 meter per cell)
   - Grid Dimensions: 20x5x20
   - Show Grid: Enabled
```

### 3. Các chế độ làm việc

#### **Place Mode** (Đặt objects)
```
1. Chọn primitive type (Cube, Sphere, etc.)
2. Set Grid Size (1x1x1, 2x1x1, 3x3x1, etc.)
3. Di chuột trong Scene view
   - Preview xanh = valid placement
   - Preview đỏ = occupied/invalid
4. Left Click để đặt
5. Object auto-snap vào grid
```

**Grid Overlay Info** hiển thị:
- Grid Position: (5, 0, 3)
- Grid Size: 2x1x1
- Status: Valid/Occupied
- Cell Size: 1.0m

#### **Resize/Move Mode**
```
1. SGrid Management System (NEW!)
```csharp
- Grid cells với kích thước configurable
- Visual grid overlay trong Scene view
- Occupied cells tracking
- Collision detection tự động
- Snap to grid cho tất cả operations
- Grid-based sizing (1x1x1, 2x1x1, etc.)
```

### Placement System
```csharp
- Real-time preview với color coding
- Grid-based positioning
- Automatic snap to grid
- Occupied cell detection
- Grid overlay information display
- Multiple primitive types
```

### Resize/Move System
```csharp
- Position Handle với grid snapping
- Grid Size adjustment trong Inspector
- Grid Size Presets (1x1x1, 2x1x1, 2x2x2, etc.)
- Collision validation
- Visual feedback với wireframe
- Undo/Redo support
```

### Boolean Operations
```csharp
- Union: Combine multiple objects
- Subtract: Remove volume from objects (tạo lỗ)
- Intersect: Keep only overlapping volume
- Simplified CSG for qvới Grid System

```plaintext
1. Tạo Grid Manager (Cell Size = 1m, Dimensions = 20x5x20)
2. Tạo Ground: Grid Size 10x1x10 tại (0,0,0)
3. Tạo 4 tường:
   - Tường trước: 10x3x1 tại (0,0,5)
   - Tường sau: 10x3x1 tại (0,0,-5)
   - Tường trái: 1x3x10 tại (-5,0,0)
   - Tường phải: 1x3x10 tại (5,0,0)
4. Boolean Subtract để tạo cửa: 2x3x1 vào tường
5. Thêm furniture:
   - Bàn: 2x1x1
   - Ghế: 1x1x1
   - Tủ: 1x2x1
6. Group objects lại
7. Export level
```

### Workflow với Grid Units

```plaintext
Planning Phase (trên giấy):
   ┌─────────────┐
   │  10x10 Room │
   │             │
   │  ▢ Table 2x1│
   │  ▢ Chair 1x1│
   └─────────────┘

Implementation:
   Grid Size → World Scale → Perfect fit!
   10x10x3 → 10m x 10m x 3m phòng
   2x1x1 → 2m x 1m x 1m bàn
   1x1x1 → 1m x 1m x 1m ghế
```

### Modular Level Design

```plaintext
Tạo Modules:
- Floor Tile: 1x1x1
- Wall Segment: 1x3x1
- Corner: 1x3x1 (90° rotation)
- Door Frame: 2x3x1
- Window: 2x2x1

Assemble:
Grid giúp snap perfect → Build nhanh
```

### Boolean Operations
```csharp
- Union: Combine multiple objects
- SubtracManager Settings
```csharp
// Core Settings
cellSize = 1.0f;              // Kích thước mỗi cell (meters)
gridDimensions = new Vector3Int(20, 5, 20);  // 20x20 map, 5 layers

// Visual Settings
showGrid = true;              // Hiển thị grid lines
gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);  // Màu lưới
occupiedColor = new Color(1f, 0.5f, 0.5f, 0.3f); // Màu cells đã chiếm
```

### Grid Size Examples
```csharp
// Small objects
Vector3Int.one;               // 1x1x1 - Cube nhỏ, ghế

// Medium objects
new Vector3Int(2, 1, 1);     // 2x1x1 - Bàn
new Vector3Int(1, 2, 1);     // 1x2x1 - Tủ
new Vector3Int(2, 2, 2);     // 2x2x2 - Khối lớn

// Large objects
new Vector3Int(10, 1, 10);   // 10x1x10 - Sàn
new Vector3Int(10, 3, 1);    // 10x3x1 - Tường dài
new Vector3Int(5, 1, 5);     // 5x1x5 - Platform
```

### Preview Colors
```csharp
placementColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);  // Xanh lá - Valid
invalidColor = new Color(0.8f, 0.3f, 0.3f, 0.5f);    // Đỏ - Invalid/Occupi
### Tạo một căn phòng đơn giản

```plaintext
1. Tạo Ground (Plane, size 10x1x10)
2. Tạo 4 tường (Cubes với sizes phù hợp)
3. Boolean Subtract để tạo cửa/cửa sổ
4. Thêm furniture bằng các cubes nhỏ
5. Group objects lại
6. Export level
```

### Workflow nhanh

```plaintext
Place Mode → Đặt objects
   ↓
Resize Mode → Adjust sizes
   ↓
Boolean Mode → Cut holes/combine
   ↓
Export → Save layout
```

## ⚙️ Cấu hình

### Grid Settings
```csharp
snapToGrid = true;
gridSize = 0.5f;  // Adjust cho độ chính xác
```

### Default Sizes
```csharp
defaultSize = new Vector3(1, 1, 1);  // Customize per primitive
```

### Preview Colors
```csharp
placementColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);  // Green
invalidColor = new Color(0.8f, 0.3f, 0.3f, 0.5f);    // Red
```

## 🔧 Advanced Usage

### Extending với Custom Primitives
Thêm custom shapes vào `PrimitiveType` enum:
```csharp
private enum PrimitiveType
{
    Cube,
    Sphere,
    // Add custom types here
    CustomShape1,
    CustomShape2
}
```

### Integration với ProBuilder
Để có CSG operations mạnh hơn:
```csharp
// Install ProBuilder package
// Replace CSGOperations với ProBuilder API
```

### Material System
Thêm material palette:
```csharp
// Create material array
// Apply on placement
```

## 📊 Architecture

### Component Diagram
```
LevelMakerWindow (EditorWindow)
    ├── Placement System
    │   ├── Preview Object
    │   ├── Raycast Detection
    │   └── Grid Snapping
    ├── Resize System
    │   └── Scale Handles
    └── Boolean System
        └── CSG Operations

BlockData (MonoBehaviour)
    └── Metadata Storage

LevelExporter
    ├── Serialization
    └── Deserialization
```

### Data Flow
```
User Input → EditorWindow → Scene Manipulation → BlockData → Export
```

## 🐛 Troubleshooting

### Preview không hiển thị
- Kiểm tra Scene view đang focus
- Đảm bảo ở Place mode
- Có objects với colliders trong scene

### Boolean không hoạt động
- Objects cần có MeshFilter
- Đảm bảo objects có bounds overlap
- Check console cho errors

### Export fails
- Kiểm tra write permissions
- Đảm bảo export path tồn tại
- Verify level container có BlockData components

## 📈 Performance

### Optimization Tips
- Sử dụng static batching cho placed objects
- Combine meshes khi hoàn thành layout
- Disable unnecessary gizmos
- Use occlusion culling

### Limitations
- Boolean operations là simplified version
- Large mesh operations có thể chậm
- Preview chỉ hoạt động trong Scene view

## 🔮 Future Enhancements

### Planned Features
- [ ] Material palette system
- [ ] Custom shape prefabs
- [ ] Undo/Redo history panel
- [ ] Multi-selection editing
- [ ] Terrain integration
- [ ] Lighting presets
- [ ] Snap to vertices
- [ ] Mesh decimation tools

### Integration Ideas
- ProBuilder for advanced CSG
- Polybrush for painting
- Terrain Tools
- Cinemachine cameras

## 📚 Documentation

- **README.md** - Tài liệu chi tiết đầy đủ
- **QUICKSTART.md** - Hướng dẫn bắt đầu nhanh
- **Code Comments** - Inline documentation trong scripts

## 🤝 Contributing

### Code Style
- Sử dụng namespace `LevelMaker` và `LevelMaker.Editor`
- Comments bằng XML documentation
- Follow Unity C# coding conventions

### Testing
- Test tất cả modes trong Scene view
- Verify export/import functionality
- Check Undo/Redo works correctly

## 📄 License
Free to use and modify for personal and commercial projects.

## 👤 Author
Created for Unity Level Design & Prototyping

## 📧 Support
- Check README.md và QUICKSTART.md first
- Review code comments
- Unity Forum / Discord communities

---

## 🎮 Get Started Now!

1. Open Unity Project
2. Go to **Tools > Level Maker**
3. Read **QUICKSTART.md**
4. Start building!

**Happy Level Making!** 🏗️✨

---

*Version 1.0 - June 2026*
