The conversion formulas are straightforward:

  TileID → X,Y:
  x = tileId % mapWidth
  y = tileId / mapWidth  (integer division)

  X,Y → TileID:
  tileId = (y * mapWidth) + x

  These are from the game source in Reference/Source/Base/Game/GameCore/:
  - Tile.cs:10764-10765 for ID→coordinates
  - Game.cs:7624 for coordinates→ID

  Map widths by size:
  ┌──────────┬────────────┬───────┐
  │ Map Size │ Dimensions │ Width │
  ├──────────┼────────────┼───────┤
  │ Smallest │ 45×45      │ 45    │
  ├──────────┼────────────┼───────┤
  │ Tiny     │ 58×58      │ 58    │
  ├──────────┼────────────┼───────┤
  │ Small    │ 66×66      │ 66    │
  ├──────────┼────────────┼───────┤
  │ Medium   │ 74×74      │ 74    │
  ├──────────┼────────────┼───────┤
  │ Large    │ 82×82      │ 82    │
  ├──────────┼────────────┼───────┤
  │ Huge     │ 90×90      │ 90    │
  └──────────┴────────────┴───────┘
  Note: The game uses a hexagonal grid where even rows (y % 2 == 0) are
  offset by 0.5 tiles horizontally for rendering, but the tileId math
  treats it as a simple 2D array.

