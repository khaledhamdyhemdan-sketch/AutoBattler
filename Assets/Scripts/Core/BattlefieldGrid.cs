using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

namespace AutoBattler.Core
{
    /// <summary>
    /// Grid-based battlefield for unit placement
    /// Uses responsive layout compatible with Unity 6.3 UI Toolkit
    /// </summary>
    public class BattlefieldGrid : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] private int rows = 4;
        [SerializeField] private int columns = 7;
        [SerializeField] private float cellSize = 1.0f;
        [SerializeField] private float cellSpacing = 0.1f;

        [Header("Visual")]
        [SerializeField] private Material cellMaterial;
        [SerializeField] private Color validCellColor = Color.green;
        [SerializeField] private Color invalidCellColor = Color.red;
        [SerializeField] private Color occupiedCellColor = Color.gray;

        // Grid state
        private GridCell[,] grid;
        private bool placementEnabled = false;
        private bool battlefieldLocked = false;

        // Unit tracking
        private Dictionary<int2, UnitController> occupiedCells = new Dictionary<int2, UnitController>();

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeGrid();
        }

        #endregion

        #region Grid Initialization

        private void InitializeGrid()
        {
            grid = new GridCell[rows, columns];

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    Vector3 worldPosition = GetWorldPosition(row, col);
                    grid[row, col] = new GridCell
                    {
                        row = row,
                        column = col,
                        worldPosition = worldPosition,
                        isOccupied = false
                    };

                    // Create visual cell (optional)
                    CreateCellVisual(row, col, worldPosition);
                }
            }

            Debug.Log($"[BattlefieldGrid] Grid initialized: {rows}x{columns}");
        }

        private void CreateCellVisual(int row, int col, Vector3 position)
        {
            GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            cellObject.name = $"Cell_{row}_{col}";
            cellObject.transform.parent = transform;
            cellObject.transform.position = position;
            cellObject.transform.rotation = Quaternion.Euler(90, 0, 0);
            cellObject.transform.localScale = Vector3.one * cellSize;

            // Apply material
            if (cellMaterial != null)
            {
                Renderer renderer = cellObject.GetComponent<Renderer>();
                renderer.material = cellMaterial;
            }

            // Add collider for raycasting
            cellObject.layer = LayerMask.NameToLayer("Default");
        }

        private Vector3 GetWorldPosition(int row, int col)
        {
            float x = col * (cellSize + cellSpacing);
            float z = row * (cellSize + cellSpacing);
            return transform.position + new Vector3(x, 0, z);
        }

        #endregion

        #region Unit Placement

        public bool PlaceUnit(UnitController unit, int row, int col)
        {
            if (battlefieldLocked)
            {
                Debug.LogWarning("[BattlefieldGrid] Cannot place unit - battlefield is locked");
                return false;
            }

            if (!IsValidCell(row, col))
            {
                Debug.LogWarning($"[BattlefieldGrid] Invalid cell: ({row}, {col})");
                return false;
            }

            if (IsCellOccupied(row, col))
            {
                Debug.LogWarning($"[BattlefieldGrid] Cell occupied: ({row}, {col})");
                return false;
            }

            // Place unit
            Vector3 worldPos = GetWorldPosition(row, col);
            unit.transform.position = worldPos;

            // Mark cell as occupied
            int2 cellKey = new int2(row, col);
            occupiedCells[cellKey] = unit;
            grid[row, col].isOccupied = true;

            // Register with combat manager
            CombatManager.Instance?.RegisterPlayerUnit(unit);

            Debug.Log($"[BattlefieldGrid] Placed unit at ({row}, {col})");

            return true;
        }

        public bool MoveUnit(UnitController unit, int fromRow, int fromCol, int toRow, int toCol)
        {
            if (battlefieldLocked) return false;

            if (!IsValidCell(toRow, toCol) || IsCellOccupied(toRow, toCol))
            {
                return false;
            }

            // Clear old cell
            int2 oldKey = new int2(fromRow, fromCol);
            occupiedCells.Remove(oldKey);
            grid[fromRow, fromCol].isOccupied = false;

            // Occupy new cell
            int2 newKey = new int2(toRow, toCol);
            occupiedCells[newKey] = unit;
            grid[toRow, toCol].isOccupied = true;

            // Move unit
            unit.transform.position = GetWorldPosition(toRow, toCol);

            return true;
        }

        public bool RemoveUnit(int row, int col)
        {
            if (battlefieldLocked) return false;

            int2 cellKey = new int2(row, col);
            if (occupiedCells.ContainsKey(cellKey))
            {
                occupiedCells.Remove(cellKey);
                grid[row, col].isOccupied = false;
                return true;
            }

            return false;
        }

        #endregion

        #region Grid Queries

        public bool IsValidCell(int row, int col)
        {
            return row >= 0 && row < rows && col >= 0 && col < columns;
        }

        public bool IsCellOccupied(int row, int col)
        {
            if (!IsValidCell(row, col)) return true;
            return grid[row, col].isOccupied;
        }

        public GridCell GetCell(int row, int col)
        {
            if (IsValidCell(row, col))
            {
                return grid[row, col];
            }
            return default;
        }

        public int2 WorldPositionToGridCoords(Vector3 worldPosition)
        {
            Vector3 localPos = worldPosition - transform.position;
            int col = Mathf.RoundToInt(localPos.x / (cellSize + cellSpacing));
            int row = Mathf.RoundToInt(localPos.z / (cellSize + cellSpacing));
            return new int2(row, col);
        }

        #endregion

        #region Control

        public void EnablePlacement(bool enabled)
        {
            placementEnabled = enabled;
            Debug.Log($"[BattlefieldGrid] Placement {(enabled ? "enabled" : "disabled")}");
        }

        public void LockBattlefield()
        {
            battlefieldLocked = true;
            placementEnabled = false;
            Debug.Log("[BattlefieldGrid] Battlefield locked for combat");
        }

        public void UnlockBattlefield()
        {
            battlefieldLocked = false;
            Debug.Log("[BattlefieldGrid] Battlefield unlocked");
        }

        public void ClearBattlefield()
        {
            occupiedCells.Clear();

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    grid[row, col].isOccupied = false;
                }
            }

            Debug.Log("[BattlefieldGrid] Battlefield cleared");
        }

        #endregion

        #region Public Properties

        public int Rows => rows;
        public int Columns => columns;
        public bool PlacementEnabled => placementEnabled;
        public bool IsLocked => battlefieldLocked;

        #endregion
    }

    /// <summary>
    /// Represents a single cell in the battlefield grid
    /// </summary>
    [System.Serializable]
    public struct GridCell
    {
        public int row;
        public int column;
        public Vector3 worldPosition;
        public bool isOccupied;
    }
}
