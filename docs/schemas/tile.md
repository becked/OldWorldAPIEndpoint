# Tile Schema

Tiles represent map hexes with terrain, resources, and ownership.

## Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /map` | Map metadata (tile count) |
| `GET /tiles?offset=0&limit=100` | Paginated tiles |
| `GET /tile/{id}` | Single tile by ID |
| `GET /tile/{x}/{y}` | Single tile by coordinates |

## Tile Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | integer | Unique tile ID |
| `x` | integer | X coordinate |
| `y` | integer | Y coordinate |
| `terrain` | string | Terrain type (e.g., `TERRAIN_TEMPERATE`) |
| `height` | string | Height type (e.g., `HEIGHT_MOUNTAIN`, `HEIGHT_HILL`) |
| `vegetation` | string? | Vegetation type if present |
| `resource` | string? | Resource type if present |
| `improvement` | string? | Improvement type if built |
| `isPillaged` | boolean | Whether improvement is pillaged |
| `ownerId` | integer? | Owning player index |
| `cityId` | integer? | City ID if tile has city |
| `cityTerritoryId` | integer? | City territory ID |

## Paginated Response

The `/tiles` endpoint returns a paginated response:

```json
{
  "tiles": [...],
  "pagination": {
    "offset": 0,
    "limit": 100,
    "total": 5476,
    "hasMore": true
  }
}
```

### Pagination Parameters

| Parameter | Default | Max | Description |
|-----------|---------|-----|-------------|
| `offset` | 0 | - | Starting index |
| `limit` | 100 | 1000 | Maximum tiles to return |

## Map Metadata

The `/map` endpoint returns:

```json
{
  "numTiles": 5476
}
```

## Example Tile

```json
{
  "id": 123,
  "x": 15,
  "y": 8,
  "terrain": "TERRAIN_TEMPERATE",
  "height": "HEIGHT_FLAT",
  "vegetation": "VEGETATION_FOREST",
  "resource": "RESOURCE_IRON",
  "improvement": "IMPROVEMENT_MINE",
  "isPillaged": false,
  "ownerId": 0,
  "cityId": null,
  "cityTerritoryId": 5
}
```

## API Limitations

Some tile fields are not available due to game API restrictions:

- `hasRoad` - Road status not accessible
- `unitIds` - Units on tile not accessible (use `/units` and filter by `tileId`)
