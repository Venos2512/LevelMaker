# Grid System Update - Version 2.0

## What Changed?

Level Maker tool đã được update từ **free-form placement** sang **grid-based system**.

## Sự khác biệt

### Version 1.0 (Old)
```
❌ Đặt object ở bất kỳ đâu
❌ Manual snap to grid (optional)
❌ Có thể chồng lấp
❌ Resize tự do với handles
❌ Khó align chính xác
```

### Version 2.0 (NEW - Grid-Based)
```
✅ Objects bắt buộc snap vào grid cells
✅ Map chia thành grid 3D với cell size cố định
✅ Collision detection tự động - không chồng lấp
✅ Resize theo grid units (1x1x1, 2x1x1, etc.)
✅ Perfect alignment mọi lúc
✅ Grid visualization trong Scene view
✅ Occupied cells tracking
```

## Tính năng mới

### 1. GridManager Component
- Quản lý grid system
- Track occupied cells
- Visualize grid trong editor
- Configurable cell size & dimensions

### 2. Grid-Based Placement
- Preview với color coding (xanh = valid, đỏ = occupied)
- Grid overlay info (position, size, status)
- Automatic snapping
- No overlapping allowed

### 3. Grid-Based Sizing
- Objects có Grid Size (1x1x1, 2x1x1, 2x2x2, etc.)
- Grid Size Presets trong Inspector
- Resize validation

### 4. Move with Grid Snapping
- Position Handle auto-snap to grid
- Collision check khi di chuyển
- Visual feedback

## Migration từ v1.0

### Nếu bạn có level cũ:
1. Mở scene
2. Tạo GridManager: **Tools > Level Maker > Create Grid Manager**
3. Configure Cell Size (recommend: 1.0)
4. Select GridManager object
5. Click **"Rebuild Grid from Scene"** trong Inspector
6. Tất cả BlockData objects sẽ được update

### Các thay đổi trong code:

#### BlockData Component
```csharp
// NEW fields:
public Vector3Int gridPosition;  // Vị trí trong grid
public Vector3Int gridSize;      // Kích thước theo cells
```

#### LevelMakerWindow
```csharp
// OLD:
Vector3 defaultSize;
bool snapToGrid;
float gridSize;

// NEW:
Vector3Int gridSize;  // Grid units thay vì world units
GridManager gridManager;  // Reference to grid manager
float cellSize;       // Cell size
Vector3Int gridDimensions;  // Grid bounds
```

## Documentation

### Đọc thêm:
- **GRID_QUICKSTART.md** - Hướng dẫn nhanh Grid System
- **README.md** - Updated với Grid System info
- **LEVEL_MAKER_DOCUMENTATION.md** - Full documentation

## Breaking Changes

### ⚠️ Không tương thích ngược:
1. **Grid Manager required**: Phải tạo GridManager trước khi đặt objects
2. **No free placement**: Không thể đặt giữa grid cells
3. **Grid-based sizing**: Resize theo grid units, không phải world units
4. **API changes**: Một số methods đã thay đổi signature

### ✅ Vẫn tương thích:
1. Boolean operations
2. Export/Import system
3. Level container system
4. Prefab workflow

## Why Grid System?

### Advantages:
1. **Perfect Alignment**: Mọi object align hoàn hảo
2. **No Overlapping**: Collision detection built-in
3. **Modular Design**: Dễ tạo reusable pieces
4. **Easy Planning**: Plan level trên giấy rồi implement chính xác
5. **Performance**: Tối ưu hóa dễ dàng hơn
6. **Consistent Scale**: Mọi object theo cùng một scale system

### Use Cases:
- **Grid-based games**: Puzzle, strategy, dungeon crawler
- **Modular levels**: Tái sử dụng pieces
- **Precise layouts**: Architecture, interior design
- **Quick prototyping**: Block out levels nhanh

## Examples

### Trước (v1.0):
```
Place object at (1.234, 0.567, 2.891)
Size: (1.5, 2.3, 1.8)
→ Khó align, có thể overlap
```

### Sau (v2.0):
```
Place object at Grid Position (1, 0, 2)
Grid Size: 2x2x1
→ World Position (1.0, 0.0, 2.0)
→ World Size (2.0, 2.0, 1.0)
→ Perfect alignment, no overlap
```

## FAQ

**Q: Có thể disable grid system không?**
A: Không, grid system là core của v2.0. Nếu cần free placement, sử dụng v1.0.

**Q: Cell size bao nhiêu là tốt?**
A: Phụ thuộc project:
- Cell Size 1.0: Cho hầu hết games
- Cell Size 0.5: Cho detail cao hơn
- Cell Size 2.0: Cho larger scale

**Q: Có thể có nhiều GridManager không?**
A: Có, mỗi level container có thể có GridManager riêng.

**Q: Grid dimensions tối đa?**
A: Recommended: 50x20x50 (50,000 cells)
Maximum: Limited bởi memory

**Q: Export format có thay đổi không?**
A: Có, JSON files giờ include gridPosition và gridSize.

## Support

Nếu gặp vấn đề:
1. Check **README.md** - Common Issues section
2. Read **GRID_QUICKSTART.md**
3. Try "Rebuild Grid from Scene"
4. Check Unity console for errors

---

**Version 2.0 - Grid-Based System**
*Updated: June 2026*
