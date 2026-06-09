# Level Maker Tool for Unity

## Tổng quan
Level Maker là công cụ Editor mạnh mẽ cho Unity giúp bạn nhanh chóng tạo prototype map bằng cách đặt và chỉnh sửa các khối 3D trên **hệ thống grid-based**.

## ⭐ Grid System (NEW!)

Map được chia thành **grid cells** có kích thước cố định:
- **Cell Size**: Kích thước mỗi cell (mặc định 1m)
- **Grid Dimensions**: Số lượng cells (20x5x20)
- **Grid Size**: Objects chiếm bao nhiêu cells (1x1x1, 2x1x1, etc.)
- **Snap to Grid**: Tự động, không thể đặt giữa cells
- **Collision Detection**: Không thể đặt chồng lấp

## Tính năng chính

### 1. **Grid Manager**
- Quản lý hệ thống grid 3D
- Visualize grid trong Scene view
- Track occupied cells
- Prevent overlapping placement
- Customizable cell size và dimensions

### 2. **Placement Mode (Chế độ Đặt)**
- Đặt các primitive shapes: Cube, Sphere, Cylinder, Capsule, Plane
- Preview real-time với color coding (xanh = valid, đỏ = occupied)
- **Grid-based placement**: Objects snap vào grid cells
- **Grid Size selection**: Chọn kích thước theo grid units (1x1x1, 2x1x1, etc.)
- Grid overlay info hiển thị position và status

### 3. **Resize/Move Mode**
- Di chuyển objects với Position Handle
- Snap to grid khi di chuyển
- Resize theo grid units trong Inspector
- Grid Size Presets: 1x1x1, 2x1x1, 2x2x2, etc.
- Validation: Không thể resize/move nếu overlap

### 4. **Boolean Operations**
- **Union**: Hợp hai hoặc nhiều objects
- **Subtract**: Trừ object từ object khác (tạo lỗ)
- **Intersect**: Lấy phần giao nhau
- Simplified CSG operations cho quick prototyping

### 5. **Export/Import System**
- Xuất level layout ra file JSON
- Import lại level đã lưu
- Backup và version control cho levels

## Cách sử dụng

### Mở Tool
1. Trong Unity Editor, chọn **Tools > Level Maker**
2. Window Level Maker sẽ xuất hiện

### Tạo Grid Manager (Bắt buộc!)
1. Trong Level Maker window, tab **Place**
2. Click **"Create Grid Manager"**
3. Configure trong Inspector:
   - **Cell Size**: 1.0 (1m per cell)
   - **Grid Dimensions**: 20x5x20 (hoặc theo ý bạn)
   - **Show Grid**: Enable để xem grid

### Placement Mode
1. Chọn mode **Place**
2. Chọn primitive type (Cube, Sphere, etc.)
3. **Set Grid Size** (1x1x1, 2x1x1, 3x3x1, etc.)
4. Di chuyển chuột trong Scene view
   - Preview xanh = vị trí hợp lệ
   - Preview đỏ = bị chồng lấp
5. **Left Click** để đặt object
6. Object tự động snap vào grid

### Resize/Move Mode
1. Chọn mode **Resize/Move**
2. Select object trong scene
3. **Di chuyển**: Kéo Position Handle, snap to grid
4. **Resize**: Thay đổi Grid Size trong Inspector
5. Hoặc dùng **Grid Size Presets**: 1x1x1, 2x1x1, 2x2x2

### Boolean Operations
1. Chọn mode **Boolean**
2. Chọn operation type (Union/Subtract/Intersect)
3. Select object đầu tiên và click **Add Selected to Boolean List**
4. Select object thứ hai và click **Add Selected to Boolean List**
5. Click **Perform Boolean Operation**

### Export Level
1. Mở **Tools > Level Exporter**
2. Chọn Level Container
3. NG**: Toggle Grid visibility (select GridManager)
- **Ctrl + Z**: Undo
- **Ctrl + D**: Duplicate
- **Delete**: Delete selected
- **F**: Focus on selected

### Best Practices
1. **Luôn tạo Grid Manager trước** khi đặt objects
2. **Plan theo grid units**: Phòng 10x10, hành lang 2x10, etc.
3. **Dùng Grid Size Presets** cho các kích thước thông dụng
4. **Show Grid** khi cần alignment chính xác
5. **Rebuild Grid** nếu grid data bị sai
6. EGridManager
Core component quản lý grid system:
- Cell size và grid dimensions
- Occupied cells tracking
- Grid visualization
- Snap to grid logic
- Collision detection

### BlockData
Component tự động được thêm vào mỗi block, lưu trữ:
- Block type
- Original size
- **Grid position** (NEW!)
- **Grid size** (NEW!)
- Creation time
- Boolean operation info

### Custom Inspector Actions
Khi select một block, Inspector hiển thị:
- **Grid Position**: Vị trí trong grid
- **GridGridManager.cs            # Grid system management
│   ├── BlockData.cs              # Component lưu metadata của blocks
│   ├── LevelExporter.cs          # Export/Import logic
│   ├── README.md                 # Tài liệu đầy đủ
│   ├── QUICKSTART.md             # Hướng dẫn cơ bản
│   ├── GRID_QUICKSTART.md        # Hướng dẫn Grid System (NEW!)
│   └── Editor/
│       ├── LevelMakerWindow.cs       # Main editor window
│       ├── GridManagerInspector.cs   # Grid manager inspector (NEW!)
│       ├── CSGOperations.cs          # Boolean operations
│       ├── LevelMakerInspector.cs    # Custom inspector
│       └── LevelExporterWindow.cs   
### GridManager Inspector
Select G Grid System
- **Grid Manager là bắt buộc**: Phải tạo GridManager trước khi đặt objects
- **Grid-based placement**: Tất cả objects snap vào grid, không thể đặt giữa cells
- **Collision prevention**: Không thể đặt objects chồng lên nhau
- **Grid units**: Resize theo grid units (1x, 2x, 3x), không có fractional sizes
- **Occupied tracking**: GridManager tự động track cells đã chiếm
- **Visualization**: Grid lines hiển thị trong Scene view (có thể toggle)

## Common Issues

### Issue: Không thể đặt object
**Cause**: Cells đã bị chiếm bởi object khác
**Solution**: Di chuyển tới vị trí khác hoặc xóa objects cũ

### Issue: Preview không hiện / "No Grid Manager found"
**Cause**: Chưa tạo GridManager
**Solution**: Click "Create Grid Manager" button trong Place mode

### Issue: Grid không hiển thị
**Cause**: Show Grid disabled
**Solution**: Select GridManager object > Enable "Show Grid"

### Issue: Object size không đúng
**Cause**: Grid Size không khớp với mong muốn
**Solution**: Adjust Grid Size trong Inspector hoặc dùng Grid Size Presets

### Issue: Occupied cells data sai
**Cause**: Grid data bị corrupt (có thể do delete objects manually)
**Solution**: Select GridManager > "Rebuild Grid from Scene"
- **Ctrl + Y**: Redo

### Best Practices
1. Sử dụng **Create Level Container** để organize các blocks
2. Enable **Snap to Grid** cho alignment chính xác
3. Sử dụng **Group Selected** để nhóm nhiều objects
4. Export level thường xuyên để backup

## Components

### BlockData
Component tự động được thêm vào mỗi block, lưu trữ:
- Block type
- Original size
- Creation time
- Boolean operation info

### Custom Inspector Actions
Khi select một block, Inspector hiển thị:
- **Reset Size**: Khôi phục kích thước gốc
- **Duplicate**: Tạo bản sao
- **Align to Ground**: Căn xuống mặt đất
- **Center Pivot**: Căn chỉnh pivot về trung tâm
- **Convert to Mesh**: Lưu mesh ra asset file

## Cấu trúc Files

```
Assets/
├── Scripts/
│   ├── BlockData.cs              # Component lưu metadata của blocks
│   ├── LevelExporter.cs          # Export/Import logic
│   └── Editor/
│       ├── LevelMakerWindow.cs   # Main editor window
│       ├── CSGOperations.cs      # Boolean operations
│       ├── LevelMakerInspector.cs # Custom inspector
│       └── LevelExporterWindow.cs # Export/Import UI
```

## Yêu cầu
- Unity 2021.3 hoặc mới hơn
- Universal Render Pipeline (URP) - optional

## Lưu ý
- Boolean operations là simplified version cho quick prototyping
- Để có CSG operations phức tạp hơn, có thể tích hợp với ProBuilder
- Preview material sử dụng transparency để hiển thị placement location

## Phát triển thêm
Có thể mở rộng với:
- Custom shapes từ prefabs
- Material palette
- Advanced CSG với mesh boolean libraries
- Terrain integration
- Multi-layer system
- Prefab variants support

## License
Free to use and modify for your projects.

---
Created for Unity Level Design & Prototyping
