using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Metacraft.Puzzles
{
    public sealed class SlidingPipePuzzle2D : MonoBehaviour
    {
        [Flags]
        private enum PipeOpenings
        {
            None = 0,
            Up = 1,
            Right = 2,
            Down = 4,
            Left = 8
        }

        private enum TileKind
        {
            Pipe
        }

        private enum PipeShape
        {
            Custom,
            End,
            Straight,
            Corner,
            TJunction,
            Cross
        }

        [Header("Board")]
        [SerializeField] private int width = 5;
        [SerializeField] private int height = 4;
        [SerializeField] private float cellSize = 1.15f;
        [SerializeField] private Vector2 boardOrigin = new Vector2(-1.75f, 1.35f);
        [SerializeField] private bool shuffleOnStart;
        [SerializeField, Min(0)] private int shuffleMoves = 12;
        [SerializeField] private int shuffleSeed = 2026;
        [SerializeField] private List<TileDefinition> tileLayout = new();

        [Header("Input")]
        [SerializeField] private Camera inputCamera;

        [Header("Colors")]
        [SerializeField] private Color cellColor = new Color(0.13f, 0.12f, 0.11f, 1f);
        [SerializeField] private Color tileColor = new Color(0.48f, 0.35f, 0.18f, 1f);
        [SerializeField] private Color connectedColor = new Color(0.18f, 0.85f, 0.62f, 1f);
        [SerializeField] private Color sourceColor = new Color(0.1f, 0.5f, 1f, 1f);
        [SerializeField] private Color exitColor = new Color(1f, 0.55f, 0.12f, 1f);

        [Header("Sprites")]
        [SerializeField] private Sprite cellSprite;
        [SerializeField] private Sprite tileBackgroundSprite;
        [SerializeField, Min(0f)] private float tileBackgroundSpriteScale = 1f;
        [SerializeField] private Sprite endPipeSprite;
        [SerializeField] private Sprite straightPipeSprite;
        [SerializeField] private Sprite cornerPipeSprite;
        [SerializeField] private Sprite tJunctionPipeSprite;
        [SerializeField] private Sprite crossPipeSprite;
        [SerializeField] private Sprite customPipeSprite;
        [SerializeField, Min(0f)] private float pipeSpriteScale = 1f;
        [SerializeField, Min(0f)] private float endPipeSpriteScale = 1f;
        [SerializeField, Min(0f)] private float straightPipeSpriteScale = 1f;
        [SerializeField, Min(0f)] private float cornerPipeSpriteScale = 1f;
        [SerializeField, Min(0f)] private float tJunctionPipeSpriteScale = 1f;
        [SerializeField, Min(0f)] private float crossPipeSpriteScale = 1f;
        [SerializeField, Min(0f)] private float customPipeSpriteScale = 1f;
        [SerializeField, Range(0, 3)] private int endPipeSpriteClockwiseOffset;
        [SerializeField, Range(0, 3)] private int straightPipeSpriteClockwiseOffset;
        [SerializeField, Range(0, 3)] private int cornerPipeSpriteClockwiseOffset;
        [SerializeField, Range(0, 3)] private int tJunctionPipeSpriteClockwiseOffset;
        [SerializeField, Range(0, 3)] private int crossPipeSpriteClockwiseOffset;
        [SerializeField, Range(0, 3)] private int customPipeSpriteClockwiseOffset;
        [SerializeField] private bool tintPipeSprites = true;
        [SerializeField] private int sortingOrderOffset;

        private Tile[,] grid;
        private readonly List<Tile> tiles = new();
        private readonly List<GameObject> generatedObjects = new();

        private Vector2Int emptyCell;
        private Sprite squareSprite;
        private bool solved;
        private SpriteRenderer entranceRenderer;
        private SpriteRenderer exitRenderer;

        public event Action Solved;

        private static readonly Vector2Int[] Directions =
        {
            new(0, -1),
            new(1, 0),
            new(0, 1),
            new(-1, 0)
        };

        private static readonly PipeOpenings[] DirectionOpenings =
        {
            PipeOpenings.Up,
            PipeOpenings.Right,
            PipeOpenings.Down,
            PipeOpenings.Left
        };

        private void OnValidate()
        {
            width = 5;
            height = 4;
            tileLayout ??= new List<TileDefinition>();
        }

        private void Awake()
        {
            if (width != 5 || height != 4)
            {
                Debug.LogWarning("This prototype is authored for a 5x4 board.", this);
            }

            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }

            squareSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
            BuildBoard();

            if (shuffleOnStart)
            {
                Shuffle(shuffleMoves);
            }

            RefreshConnections();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame || inputCamera == null)
            {
                return;
            }

            Vector2 worldPosition = inputCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);
            for (int i = 0; i < hits.Length; i++)
            {
                Tile tile = FindTile(hits[i].transform);
                if (tile != null)
                {
                    TrySlide(tile);
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            if (squareSprite != null)
            {
                Destroy(squareSprite);
            }
        }

        private void BuildBoard()
        {
            ClearGeneratedObjects();
            tiles.Clear();
            grid = new Tile[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    CreateCell(x, y);
                }
            }

            emptyCell = FindEmptyCell();

            if (tileLayout.Count == 0)
            {
                Debug.LogWarning("Sliding Pipe Puzzle has no tile layout. Add pipe tiles in the Inspector.", this);
            }

            foreach (TileDefinition definition in tileLayout)
            {
                Vector2Int cell = definition.Cell;
                if (!IsInside(cell) || cell == emptyCell)
                {
                    continue;
                }

                AddTile(cell.x, cell.y, TileKind.Pipe, definition);
            }

            CreateEndpoint("Entrance", new Vector2Int(0, 0), Vector2.left, sourceColor, out entranceRenderer);
            CreateEndpoint("Exit", new Vector2Int(width - 1, height - 1), Vector2.right, exitColor, out exitRenderer);
        }

        private Vector2Int FindEmptyCell()
        {
            bool[,] occupied = new bool[width, height];

            foreach (TileDefinition definition in tileLayout)
            {
                if (IsInside(definition.Cell))
                {
                    occupied[definition.Cell.x, definition.Cell.y] = true;
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!occupied[x, y])
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }

            Debug.LogWarning("Tile Layout has no empty cell. Forcing empty cell to bottom-right.", this);
            return new Vector2Int(width - 1, height - 1);
        }

        private void AddTile(int x, int y, TileKind kind, TileDefinition definition)
        {
            PipeOpenings openings = definition.ToOpenings();
            Tile tile = new Tile(kind, openings, new Vector2Int(x, y));
            tile.Root = new GameObject($"{kind}_{x}_{y}");
            tile.Root.transform.SetParent(transform, false);
            tile.Root.transform.position = CellToWorld(tile.Cell);
            generatedObjects.Add(tile.Root);

            tile.Root.transform.localScale = Vector3.one * (cellSize * 0.9f);

            GameObject tileBackground = new GameObject("TileBackground");
            tileBackground.transform.SetParent(tile.Root.transform, false);
            tileBackground.transform.localPosition = Vector3.zero;
            tileBackground.transform.localScale = Vector3.one * tileBackgroundSpriteScale;
            generatedObjects.Add(tileBackground);

            SpriteRenderer tileRenderer = tileBackground.AddComponent<SpriteRenderer>();
            tileRenderer.sprite = tileBackgroundSprite != null ? tileBackgroundSprite : squareSprite;
            tileRenderer.color = tileColor;
            tileRenderer.sortingOrder = sortingOrderOffset + 5;
            tile.BackgroundRenderer = tileRenderer;

            BoxCollider2D collider = tile.Root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            tile.PipeParts = CreatePipeVisuals(tile, definition);
            tiles.Add(tile);
            grid[x, y] = tile;
        }

        private List<SpriteRenderer> CreatePipeVisuals(Tile tile, TileDefinition definition)
        {
            List<SpriteRenderer> parts = new List<SpriteRenderer>();
            Sprite pipeSprite = GetPipeVisual(definition, tile.Openings, out int visualClockwiseTurns);
            if (pipeSprite != null)
            {
                AddPipeSprite(
                    tile,
                    parts,
                    pipeSprite,
                    visualClockwiseTurns,
                    GetPipeVisualScale(definition, tile.Openings));
                return parts;
            }

            AddPipePart(tile, parts, "Center", Vector2.zero, new Vector2(0.28f, 0.28f));

            if (tile.Openings.HasFlag(PipeOpenings.Up))
            {
                AddPipePart(tile, parts, "Up", new Vector2(0f, 0.24f), new Vector2(0.18f, 0.48f));
            }

            if (tile.Openings.HasFlag(PipeOpenings.Right))
            {
                AddPipePart(tile, parts, "Right", new Vector2(0.24f, 0f), new Vector2(0.48f, 0.18f));
            }

            if (tile.Openings.HasFlag(PipeOpenings.Down))
            {
                AddPipePart(tile, parts, "Down", new Vector2(0f, -0.24f), new Vector2(0.18f, 0.48f));
            }

            if (tile.Openings.HasFlag(PipeOpenings.Left))
            {
                AddPipePart(tile, parts, "Left", new Vector2(-0.24f, 0f), new Vector2(0.48f, 0.18f));
            }

            return parts;
        }

        private void AddPipeSprite(Tile tile, List<SpriteRenderer> parts, Sprite sprite, int clockwiseTurns, float visualScale)
        {
            GameObject visual = new GameObject("PipeSprite");
            visual.transform.SetParent(tile.Root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, -90f * clockwiseTurns);
            visual.transform.localScale = Vector3.one * pipeSpriteScale * visualScale;
            generatedObjects.Add(visual);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrderOffset + 6;
            parts.Add(renderer);
        }

        private void AddPipePart(Tile tile, List<SpriteRenderer> parts, string name, Vector2 localPosition, Vector2 localScale)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(tile.Root.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            generatedObjects.Add(part);

            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrderOffset + 6;
            parts.Add(renderer);
        }

        private void CreateCell(int x, int y)
        {
            GameObject cell = new GameObject($"Cell_{x}_{y}");
            cell.transform.SetParent(transform, false);
            cell.transform.position = CellToWorld(new Vector2Int(x, y));
            cell.transform.localScale = Vector3.one * cellSize;
            generatedObjects.Add(cell);

            SpriteRenderer renderer = cell.AddComponent<SpriteRenderer>();
            renderer.sprite = cellSprite != null ? cellSprite : squareSprite;
            renderer.color = cellColor;
            renderer.sortingOrder = sortingOrderOffset;
        }

        private void CreateEndpoint(string name, Vector2Int adjacentCell, Vector2 direction, Color color, out SpriteRenderer endpointRenderer)
        {
            GameObject endpoint = new GameObject(name);
            endpoint.transform.SetParent(transform, false);
            endpoint.transform.position = CellToWorld(adjacentCell) + (Vector3)(direction * cellSize);
            endpoint.transform.localScale = endPipeSprite != null
                ? Vector3.one * cellSize * 0.9f * pipeSpriteScale * endPipeSpriteScale
                : Vector3.one * (cellSize * 0.45f);
            endpoint.transform.localRotation = Quaternion.Euler(
                0f,
                0f,
                -90f * (GetEndpointClockwiseTurns(direction) + endPipeSpriteClockwiseOffset));
            generatedObjects.Add(endpoint);

            endpointRenderer = endpoint.AddComponent<SpriteRenderer>();
            endpointRenderer.sprite = endPipeSprite != null ? endPipeSprite : squareSprite;
            endpointRenderer.color = color;
            endpointRenderer.sortingOrder = sortingOrderOffset + 7;
        }

        private static int GetEndpointClockwiseTurns(Vector2 direction)
        {
            if (direction == Vector2.left)
            {
                return 1;
            }

            if (direction == Vector2.right)
            {
                return 3;
            }

            if (direction == Vector2.up)
            {
                return 2;
            }

            return 0;
        }

        private void TrySlide(Tile tile)
        {
            if (tile == null)
            {
                return;
            }

            Vector2Int delta = tile.Cell - emptyCell;
            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
            {
                return;
            }

            Vector2Int previousCell = tile.Cell;
            grid[previousCell.x, previousCell.y] = null;
            grid[emptyCell.x, emptyCell.y] = tile;

            tile.Cell = emptyCell;
            emptyCell = previousCell;
            tile.Root.transform.position = CellToWorld(tile.Cell);

            RefreshConnections();
        }

        private Tile FindTile(Transform hitTransform)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                if (hitTransform == tiles[i].Root.transform || hitTransform.IsChildOf(tiles[i].Root.transform))
                {
                    return tiles[i];
                }
            }

            return null;
        }

        private void Shuffle(int moves)
        {
            System.Random random = new System.Random(shuffleSeed);
            Tile lastMoved = null;

            for (int i = 0; i < moves; i++)
            {
                List<Tile> candidates = GetSlidableTiles();
                if (lastMoved != null && candidates.Count > 1)
                {
                    candidates.Remove(lastMoved);
                }

                if (candidates.Count == 0)
                {
                    return;
                }

                Tile tile = candidates[random.Next(candidates.Count)];
                lastMoved = tile;
                TrySlide(tile);
            }
        }

        private List<Tile> GetSlidableTiles()
        {
            List<Tile> candidates = new List<Tile>();
            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int cell = emptyCell + Directions[i];
                if (IsInside(cell) && grid[cell.x, cell.y] != null)
                {
                    candidates.Add(grid[cell.x, cell.y]);
                }
            }

            return candidates;
        }

        private void RefreshConnections()
        {
            foreach (Tile tile in tiles)
            {
                tile.IsConnected = false;
            }

            Tile source = grid[0, 0];
            Tile exitCandidate = grid[width - 1, height - 1];

            if (source == null || !source.Openings.HasFlag(PipeOpenings.Left))
            {
                solved = false;
                UpdateVisuals();
                return;
            }

            Queue<Tile> queue = new Queue<Tile>();
            source.IsConnected = true;
            queue.Enqueue(source);

            while (queue.Count > 0)
            {
                Tile current = queue.Dequeue();

                for (int i = 0; i < Directions.Length; i++)
                {
                    PipeOpenings opening = DirectionOpenings[i];
                    if (!current.Openings.HasFlag(opening))
                    {
                        continue;
                    }

                    Vector2Int neighborCell = current.Cell + Directions[i];
                    if (!IsInside(neighborCell))
                    {
                        continue;
                    }

                    Tile neighbor = grid[neighborCell.x, neighborCell.y];
                    if (neighbor == null || neighbor.IsConnected)
                    {
                        continue;
                    }

                    PipeOpenings opposite = Opposite(opening);
                    if (!neighbor.Openings.HasFlag(opposite))
                    {
                        continue;
                    }

                    neighbor.IsConnected = true;
                    queue.Enqueue(neighbor);
                }
            }

            bool wasSolved = solved;
            bool reachesExitCell = exitCandidate != null &&
                exitCandidate.IsConnected &&
                exitCandidate.Openings.HasFlag(PipeOpenings.Right);

            solved = reachesExitCell;
            UpdateVisuals();

            if (solved && !wasSolved)
            {
                Debug.Log("Sliding Pipe Puzzle solved.", this);
                Solved?.Invoke();
            }
        }

        private void UpdateVisuals()
        {
            foreach (Tile tile in tiles)
            {
                Color pipeColor = tile.IsConnected ? connectedColor : Color.white;

                foreach (SpriteRenderer pipePart in tile.PipeParts)
                {
                    pipePart.color = tintPipeSprites ? pipeColor : Color.white;
                }

                if (tile.BackgroundRenderer != null)
                {
                    tile.BackgroundRenderer.color = tileColor;
                }
            }

            if (entranceRenderer != null)
            {
                entranceRenderer.color = IsEntranceConnected() ? connectedColor : sourceColor;
            }

            if (exitRenderer != null)
            {
                exitRenderer.color = solved ? connectedColor : exitColor;
            }
        }

        private bool IsEntranceConnected()
        {
            Tile source = grid[0, 0];
            return source != null && source.Openings.HasFlag(PipeOpenings.Left) && source.IsConnected;
        }

        private Vector3 CellToWorld(Vector2Int cell)
        {
            return transform.position + new Vector3(
                boardOrigin.x + (cell.x * cellSize),
                boardOrigin.y - (cell.y * cellSize),
                0f);
        }

        private bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
        }

        private static PipeOpenings Opposite(PipeOpenings opening)
        {
            return opening switch
            {
                PipeOpenings.Up => PipeOpenings.Down,
                PipeOpenings.Right => PipeOpenings.Left,
                PipeOpenings.Down => PipeOpenings.Up,
                PipeOpenings.Left => PipeOpenings.Right,
                _ => PipeOpenings.None
            };
        }

        private Sprite GetPipeSprite(PipeShape shape)
        {
            return shape switch
            {
                PipeShape.End => endPipeSprite,
                PipeShape.Straight => straightPipeSprite,
                PipeShape.Corner => cornerPipeSprite,
                PipeShape.TJunction => tJunctionPipeSprite,
                PipeShape.Cross => crossPipeSprite,
                PipeShape.Custom => customPipeSprite,
                _ => null
            };
        }

        private Sprite GetPipeVisual(TileDefinition definition, PipeOpenings openings, out int clockwiseTurns)
        {
            if (definition.SpriteOverride != null)
            {
                clockwiseTurns = definition.ClockwiseTurns + customPipeSpriteClockwiseOffset;
                return definition.SpriteOverride;
            }

            PipeShape shape = definition.Shape;
            clockwiseTurns = definition.ClockwiseTurns;

            if (shape == PipeShape.Custom && TryInferShape(openings, out PipeShape inferredShape, out int inferredTurns))
            {
                shape = inferredShape;
                clockwiseTurns = inferredTurns;
            }

            clockwiseTurns += GetPipeSpriteClockwiseOffset(shape);
            return GetPipeSprite(shape);
        }

        private int GetPipeSpriteClockwiseOffset(PipeShape shape)
        {
            return shape switch
            {
                PipeShape.End => endPipeSpriteClockwiseOffset,
                PipeShape.Straight => straightPipeSpriteClockwiseOffset,
                PipeShape.Corner => cornerPipeSpriteClockwiseOffset,
                PipeShape.TJunction => tJunctionPipeSpriteClockwiseOffset,
                PipeShape.Cross => crossPipeSpriteClockwiseOffset,
                PipeShape.Custom => customPipeSpriteClockwiseOffset,
                _ => 0
            };
        }

        private float GetPipeVisualScale(TileDefinition definition, PipeOpenings openings)
        {
            PipeShape shape = definition.Shape;
            if (definition.SpriteOverride != null)
            {
                shape = PipeShape.Custom;
            }
            else if (shape == PipeShape.Custom && TryInferShape(openings, out PipeShape inferredShape, out _))
            {
                shape = inferredShape;
            }

            return GetPipeSpriteScale(shape);
        }

        private float GetPipeSpriteScale(PipeShape shape)
        {
            return shape switch
            {
                PipeShape.End => endPipeSpriteScale,
                PipeShape.Straight => straightPipeSpriteScale,
                PipeShape.Corner => cornerPipeSpriteScale,
                PipeShape.TJunction => tJunctionPipeSpriteScale,
                PipeShape.Cross => crossPipeSpriteScale,
                PipeShape.Custom => customPipeSpriteScale,
                _ => 1f
            };
        }

        private static bool TryInferShape(PipeOpenings openings, out PipeShape shape, out int clockwiseTurns)
        {
            PipeShape[] candidates =
            {
                PipeShape.End,
                PipeShape.Straight,
                PipeShape.Corner,
                PipeShape.TJunction,
                PipeShape.Cross
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                PipeShape candidate = candidates[i];
                PipeOpenings baseOpenings = TileDefinition.ShapeToOpenings(candidate);
                for (int turns = 0; turns < 4; turns++)
                {
                    if (RotateClockwise(baseOpenings, turns) == openings)
                    {
                        shape = candidate;
                        clockwiseTurns = turns;
                        return true;
                    }
                }
            }

            shape = PipeShape.Custom;
            clockwiseTurns = 0;
            return false;
        }

        private static PipeOpenings RotateClockwise(PipeOpenings openings, int turns)
        {
            turns = ((turns % 4) + 4) % 4;
            for (int i = 0; i < turns; i++)
            {
                PipeOpenings rotated = PipeOpenings.None;

                if (openings.HasFlag(PipeOpenings.Up))
                {
                    rotated |= PipeOpenings.Right;
                }

                if (openings.HasFlag(PipeOpenings.Right))
                {
                    rotated |= PipeOpenings.Down;
                }

                if (openings.HasFlag(PipeOpenings.Down))
                {
                    rotated |= PipeOpenings.Left;
                }

                if (openings.HasFlag(PipeOpenings.Left))
                {
                    rotated |= PipeOpenings.Up;
                }

                openings = rotated;
            }

            return openings;
        }

        private void ClearGeneratedObjects()
        {
            for (int i = generatedObjects.Count - 1; i >= 0; i--)
            {
                if (generatedObjects[i] != null)
                {
                    Destroy(generatedObjects[i]);
                }
            }

            generatedObjects.Clear();
        }

        private sealed class Tile
        {
            public Tile(TileKind kind, PipeOpenings openings, Vector2Int cell)
            {
                Kind = kind;
                Openings = openings;
                Cell = cell;
            }

            public TileKind Kind { get; }
            public PipeOpenings Openings { get; }
            public Vector2Int Cell { get; set; }
            public GameObject Root { get; set; }
            public SpriteRenderer BackgroundRenderer { get; set; }
            public List<SpriteRenderer> PipeParts { get; set; }
            public bool IsConnected { get; set; }
        }

        [Serializable]
        private sealed class TileDefinition
        {
            [SerializeField] private Vector2Int cell;
            [SerializeField] private PipeShape pipeShape = PipeShape.Custom;
            [SerializeField, Range(0, 3)] private int clockwiseTurns;
            [SerializeField] private Sprite spriteOverride;
            [SerializeField] private bool up;
            [SerializeField] private bool right;
            [SerializeField] private bool down;
            [SerializeField] private bool left;

            public TileDefinition()
            {
            }

            public TileDefinition(Vector2Int cell, bool up, bool right, bool down, bool left)
            {
                this.cell = cell;
                this.up = up;
                this.right = right;
                this.down = down;
                this.left = left;
            }

            public Vector2Int Cell => cell;
            public PipeShape Shape => pipeShape;
            public int ClockwiseTurns => clockwiseTurns;
            public Sprite SpriteOverride => spriteOverride;

            public PipeOpenings ToOpenings()
            {
                if (pipeShape != PipeShape.Custom)
                {
                    return RotateClockwise(ShapeToOpenings(pipeShape), clockwiseTurns);
                }

                PipeOpenings openings = PipeOpenings.None;

                if (up)
                {
                    openings |= PipeOpenings.Up;
                }

                if (right)
                {
                    openings |= PipeOpenings.Right;
                }

                if (down)
                {
                    openings |= PipeOpenings.Down;
                }

                if (left)
                {
                    openings |= PipeOpenings.Left;
                }

                return openings;
            }

            public static PipeOpenings ShapeToOpenings(PipeShape shape)
            {
                return shape switch
                {
                    PipeShape.End => PipeOpenings.Up,
                    PipeShape.Straight => PipeOpenings.Up | PipeOpenings.Down,
                    PipeShape.Corner => PipeOpenings.Up | PipeOpenings.Right,
                    PipeShape.TJunction => PipeOpenings.Left | PipeOpenings.Up | PipeOpenings.Right,
                    PipeShape.Cross => PipeOpenings.Up | PipeOpenings.Right | PipeOpenings.Down | PipeOpenings.Left,
                    _ => PipeOpenings.None
                };
            }
        }
    }
}
