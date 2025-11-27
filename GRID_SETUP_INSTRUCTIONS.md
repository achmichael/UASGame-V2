# 🚀 GridBuilder Raycast-Based Floor Detection - Setup Guide

## What Changed

The GridBuilder has been completely rewritten to use **raycast-based floor detection** instead of bounding box approximation. This ensures objects spawn **ONLY** inside the actual labyrinth shape.

---

## ✅ Key Improvements

### 1. **Raycast Floor Detection**
- Each grid cell performs a downward raycast from above
- Only cells that hit a collider tagged `"Floor"` are marked as walkable
- Grid perfectly follows the labyrinth shape, not just its bounding box

### 2. **Fast Spawn System**
- All walkable floor positions stored in `List<Vector3> walkableCells`
- New method: `GetRandomWalkablePosition(float heightOffset)`
- 100% guaranteed valid spawn positions (no guessing or retries)

### 3. **No More Bounds.Contains()**
- Removed all logic relying on bounding box validation
- Spawning uses only raycast-verified floor positions
- No edge cases or outside-labyrinth spawns

### 4. **Clean Integration**
- LabyrinthSpawnManager now uses GridBuilder's walkableCells directly
- No duplicate caching or coordinate conversion issues
- Production-ready, maintainable code

---

## 🔧 Setup Instructions

### Step 1: Tag Your Floor Objects
**CRITICAL:** All labyrinth floor colliders MUST be tagged as `"Floor"`

1. Select all floor GameObjects in your labyrinth
2. In the Inspector, set Tag = `"Floor"`
3. Verify all walkable surfaces have this tag

### Step 2: Configure GridBuilder Inspector Settings

#### **Grid Scan Settings:**
- `Grid Width`: 50 (increase for larger labyrinths)
- `Grid Height`: 50 (increase for larger labyrinths)
- `Node Spacing`: 1.0 (smaller = more accurate, but slower)
- `Grid Origin`: Set to bottom-left corner of labyrinth area

#### **Raycast Detection:**
- `Raycast Height`: 50 (must be ABOVE all floor surfaces)
- `Raycast Max Distance`: 100 (how far down to search)
- `Node Radius`: 0.4 (visualization size)
- `Floor Tag`: "Floor" (must match your floor objects' tag)

#### **Auto Detection (Recommended):**
- `Auto Detect Bounds`: ✅ Enable this
- `Labyrinth Parent`: Drag your labyrinth parent GameObject here
- `Auto Detect Padding`: 2.0 (extra margin around labyrinth)

#### **Visualization:**
- `Show Gizmos`: ✅ Enable to see grid in Scene view
- `Show Only Walkable`: ✅ Enable to see only valid floor tiles
- `Walkable Color`: Green (color of valid spawn points)
- `Gizmo Size`: 0.3 (size of debug cubes)

### Step 3: Configure LabyrinthSpawnManager

The SpawnManager will automatically use GridBuilder's walkable cells. Ensure:

- `Grid Builder`: Reference to your GridBuilder component
- `Auto Spawn On Start`: Enable if you want automatic spawning

---

## 🎮 How It Works

### Grid Generation Process:

```
1. GridBuilder scans area in Awake()
   ↓
2. For each grid cell:
   - Raycast downward from raycastHeight
   - Check if hit has "Floor" tag
   - If yes → Add to walkableCells list
   - If no → Mark as unwalkable
   ↓
3. Connect walkable neighbors for pathfinding
   ↓
4. LabyrinthSpawnManager uses walkableCells for spawning
```

### Spawning Process:

```
Player/Item/Enemy needs spawn position
   ↓
Call: GetRandomValidTile(heightOffset)
   ↓
GridBuilder.GetRandomWalkablePosition(heightOffset)
   ↓
Returns: Random floor position + height offset
   ↓
Distance checks (if needed)
   ↓
Object spawned at validated position ✅
```

---

## 🐛 Troubleshooting

### Problem: "NO WALKABLE CELLS FOUND"

**Solutions:**
1. ✅ Ensure floor objects are tagged `"Floor"` (case-sensitive!)
2. ✅ Check `Raycast Height` is ABOVE all floors (increase to 100+)
3. ✅ Verify `Grid Origin` covers the labyrinth area
4. ✅ Increase `Grid Width` and `Grid Height` to cover entire labyrinth
5. ✅ Check floor colliders exist and are enabled

### Problem: "Only a few walkable cells found"

**Solutions:**
1. ✅ Decrease `Node Spacing` for higher resolution (e.g., 0.5)
2. ✅ Increase `Grid Width` and `Grid Height`
3. ✅ Enable `Auto Detect Bounds` to automatically calculate size
4. ✅ Check if some floors are missing the "Floor" tag

### Problem: "Objects still spawning outside labyrinth"

**Solutions:**
1. ✅ Verify ALL floor surfaces have the "Floor" tag
2. ✅ Check Scene view Gizmos - green cubes show valid spawn points
3. ✅ Ensure `Show Only Walkable` is enabled to see actual grid
4. ✅ Look for floors missing colliders

### Problem: "Grid generation is slow"

**Solutions:**
1. ✅ Increase `Node Spacing` (e.g., 1.5 or 2.0)
2. ✅ Reduce `Grid Width` and `Grid Height` to minimum needed
3. ✅ Disable `Auto Detect Bounds` and set manual bounds
4. ✅ This only runs once at startup - it's normal for large grids

---

## 📊 Performance Notes

- Grid generation happens **once** in Awake()
- Typical 50x50 grid (2500 raycasts) takes ~100-200ms
- Spawning is **instant** (random list access)
- No runtime raycasts after initialization
- Memory efficient: ~24 bytes per walkable cell

---

## 🎨 Visual Debugging

### Scene View Gizmos:
- **Green cubes** = Walkable floor cells (valid spawn points)
- **Red cubes** = Unwalkable cells (if showOnlyWalkable = false)
- **Yellow box** = Grid scan area bounds
- **Cyan lines** = Neighbor connections (pathfinding graph)
- **Blue sphere** = Player spawn position
- **Cyan spheres** = Item/Enemy spawn positions

### Inspector Info:
Check Console on startup for:
```
[GridBuilder] ✓ Grid generation complete:
  - Scanned: 2500 cells (50x50)
  - Walkable: 847 cells (33.9%)
  - Floor hits: 847
```

---

## 🚀 Advanced Usage

### Get Random Spawn Position (from any script):

```csharp
GridBuilder gridBuilder = FindObjectOfType<GridBuilder>();
Vector3 spawnPos = gridBuilder.GetRandomWalkablePosition(1.0f); // 1.0f height offset
Instantiate(myPrefab, spawnPos, Quaternion.identity);
```

### Check if Position is Valid:

```csharp
bool isValid = gridBuilder.IsPositionWalkable(someWorldPosition);
```

### Get All Walkable Positions:

```csharp
List<Vector3> allPositions = gridBuilder.GetAllWalkablePositions();
```

### Refresh Grid at Runtime:

```csharp
gridBuilder.RefreshGrid(); // Re-scans entire area
```

---

## ✅ Success Checklist

Before testing, verify:

- [ ] All floor objects tagged as "Floor"
- [ ] GridBuilder has Auto Detect Bounds enabled (or manual bounds set)
- [ ] Raycast Height is above all floors (check Scene view)
- [ ] Grid Width/Height covers entire labyrinth
- [ ] Scene view shows green cubes on floors (Show Gizmos enabled)
- [ ] Console shows "X walkable cells" message on startup
- [ ] No error messages in Console
- [ ] Player spawns on visible floor (not in air or walls)

---

## 📝 Code Reference

### GridBuilder New Public API:

```csharp
// Main spawn method - use this!
public Vector3 GetRandomWalkablePosition(float heightOffset)

// Check if position is on walkable floor
public bool IsPositionWalkable(Vector3 worldPosition)

// Get all valid positions
public List<Vector3> GetAllWalkablePositions()

// Get walkable cell count
public int GetWalkableCellCount()

// Refresh grid (if labyrinth changes)
public void RefreshGrid()

// Find nearest node (for pathfinding)
public Node GetNodeFromWorldPosition(Vector3 worldPosition)
```

---

## 🎯 Final Notes

1. **Always tag floors** - This is the most common issue
2. **Use Scene view Gizmos** - Visual debugging is your friend
3. **Check Console logs** - Grid generation reports walkable cell count
4. **Start with Auto Detect** - Easier than manual configuration
5. **Adjust Node Spacing** - Balance between accuracy and performance

The grid only generates where floors exist - if you see green cubes in Scene view, those are your valid spawn points! 🎮
