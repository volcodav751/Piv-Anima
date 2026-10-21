using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public class EnemyPathfinder2D : MonoBehaviour
    {
        private const float CellSize = 0.25f;
        private const float SearchPadding = 4f;
        private const float RepathInterval = 0.25f;
        private const float WaypointReachDistance = 0.2f;
        private const float NavigationSkin = 0.02f;

        private const int StraightMoveCost = 10;
        private const int DiagonalMoveCost = 14;

        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),

            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };

        private readonly List<Vector2> path = new();

        private Collider2D bodyCollider;

        private float agentRadius;
        private float nextRepathTime;

        private int currentWaypointIndex;
        private int playerLayer;
        private int enemyLayer;

        private void Awake()
        {
            bodyCollider = GetComponent<Collider2D>();

            agentRadius = Mathf.Max(
                NavigationSkin,
                Mathf.Max(
                    bodyCollider.bounds.extents.x,
                    bodyCollider.bounds.extents.y
                ) + NavigationSkin
            );

            playerLayer = LayerMask.NameToLayer("Player");
            enemyLayer = LayerMask.NameToLayer("Enemy");
        }

        public Vector2 GetDirection(
            Vector2 currentPosition,
            Vector2 targetPosition)
        {
            if (HasClearPath(currentPosition, targetPosition))
            {
                path.Clear();
                currentWaypointIndex = 0;

                return (targetPosition - currentPosition).normalized;
            }

            if (Time.time >= nextRepathTime ||
                path.Count == 0 ||
                currentWaypointIndex >= path.Count)
            {
                BuildPath(
                    currentPosition,
                    targetPosition
                );

                nextRepathTime =
                    Time.time + RepathInterval;
            }

            AdvanceWaypoint(currentPosition);

            if (currentWaypointIndex >= path.Count)
            {
                return GetEscapeDirection(currentPosition);
            }

            return (
                path[currentWaypointIndex] -
                currentPosition
            ).normalized;
        }

        private void AdvanceWaypoint(Vector2 currentPosition)
        {
            while (currentWaypointIndex < path.Count)
            {
                float distance = Vector2.Distance(
                    currentPosition,
                    path[currentWaypointIndex]
                );

                if (distance > WaypointReachDistance)
                {
                    break;
                }

                currentWaypointIndex++;
            }
        }

        private void BuildPath(
            Vector2 startPosition,
            Vector2 targetPosition)
        {
            path.Clear();
            currentWaypointIndex = 0;

            Vector2 padding =
                Vector2.one * SearchPadding;

            Vector2 gridOrigin =
                Vector2.Min(
                    startPosition,
                    targetPosition
                ) - padding;

            Vector2 gridMax =
                Vector2.Max(
                    startPosition,
                    targetPosition
                ) + padding;

            int width =
                Mathf.CeilToInt(
                    (gridMax.x - gridOrigin.x) /
                    CellSize
                ) + 1;

            int height =
                Mathf.CeilToInt(
                    (gridMax.y - gridOrigin.y) /
                    CellSize
                ) + 1;

            Node[,] nodes =
                new Node[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int coordinates =
                        new Vector2Int(x, y);

                    Vector2 worldPosition =
                        CellToWorld(
                            coordinates,
                            gridOrigin
                        );

                    nodes[x, y] =
                        new Node(
                            coordinates,
                            !IsBlocked(worldPosition)
                        );
                }
            }

            Node startNode =
                FindClosestWalkableNode(
                    nodes,
                    startPosition,
                    gridOrigin
                );

            Node targetNode =
                FindClosestWalkableNode(
                    nodes,
                    targetPosition,
                    gridOrigin
                );

            if (startNode == null ||
                targetNode == null)
            {
                return;
            }

            Vector2Int targetCoordinates =
                targetNode.coordinates;

            startNode.gCost = 0;

            startNode.hCost =
                CalculateDistanceCost(
                    startNode.coordinates,
                    targetCoordinates
                );

            List<Node> openNodes =
                new List<Node>
                {
                    startNode
                };

            while (openNodes.Count > 0)
            {
                Node currentNode =
                    GetLowestCostNode(openNodes);

                if (currentNode == targetNode)
                {
                    CreatePath(
                        startNode,
                        targetNode,
                        gridOrigin,
                        targetPosition
                    );

                    Vector2 safeStartPosition =
                        CellToWorld(
                            startNode.coordinates,
                            gridOrigin
                        );

                    if (Vector2.Distance(
                            startPosition,
                            safeStartPosition) >
                        WaypointReachDistance)
                    {
                        path.Insert(
                            0,
                            safeStartPosition
                        );
                    }

                    return;
                }

                openNodes.Remove(currentNode);
                currentNode.closed = true;

                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int neighbourCoordinates =
                        currentNode.coordinates +
                        direction;

                    if (!IsInsideGrid(
                            neighbourCoordinates,
                            width,
                            height))
                    {
                        continue;
                    }

                    Node neighbour =
                        nodes[
                            neighbourCoordinates.x,
                            neighbourCoordinates.y
                        ];

                    if (!neighbour.walkable ||
                        neighbour.closed)
                    {
                        continue;
                    }

                    bool diagonal =
                        direction.x != 0 &&
                        direction.y != 0;

                    if (diagonal &&
                        !CanMoveDiagonally(
                            currentNode.coordinates,
                            direction,
                            nodes,
                            width,
                            height))
                    {
                        continue;
                    }

                    int moveCost =
                        diagonal
                            ? DiagonalMoveCost
                            : StraightMoveCost;

                    int newCost =
                        currentNode.gCost +
                        moveCost;

                    if (newCost >= neighbour.gCost)
                    {
                        continue;
                    }

                    neighbour.parent =
                        currentNode;

                    neighbour.gCost =
                        newCost;

                    neighbour.hCost =
                        CalculateDistanceCost(
                            neighbour.coordinates,
                            targetCoordinates
                        );

                    if (!openNodes.Contains(neighbour))
                    {
                        openNodes.Add(neighbour);
                    }
                }
            }
        }

        private void CreatePath(
            Node startNode,
            Node targetNode,
            Vector2 gridOrigin,
            Vector2 targetPosition)
        {
            List<Vector2> newPath =
                new List<Vector2>();

            Node currentNode =
                targetNode;

            while (currentNode != null &&
                   currentNode != startNode)
            {
                newPath.Add(
                    CellToWorld(
                        currentNode.coordinates,
                        gridOrigin
                    )
                );

                currentNode =
                    currentNode.parent;
            }

            newPath.Reverse();

            path.AddRange(newPath);

            if (path.Count == 0 ||
                HasClearPath(
                    path[path.Count - 1],
                    targetPosition))
            {
                path.Add(targetPosition);
            }
        }

        private bool CanMoveDiagonally(
            Vector2Int current,
            Vector2Int direction,
            Node[,] nodes,
            int width,
            int height)
        {
            Vector2Int horizontal =
                current +
                new Vector2Int(
                    direction.x,
                    0
                );

            Vector2Int vertical =
                current +
                new Vector2Int(
                    0,
                    direction.y
                );

            if (!IsInsideGrid(
                    horizontal,
                    width,
                    height) ||
                !IsInsideGrid(
                    vertical,
                    width,
                    height))
            {
                return false;
            }

            return
                nodes[
                    horizontal.x,
                    horizontal.y
                ].walkable &&
                nodes[
                    vertical.x,
                    vertical.y
                ].walkable;
        }

        private bool HasClearPath(
            Vector2 start,
            Vector2 end)
        {
            Vector2 direction =
                end - start;

            float distance =
                direction.magnitude;

            if (distance <= 0f)
            {
                return true;
            }

            RaycastHit2D[] hits =
                Physics2D.CircleCastAll(
                    start,
                    agentRadius,
                    direction.normalized,
                    distance
                );

            foreach (RaycastHit2D hit in hits)
            {
                if (IsNavigationObstacle(
                    hit.collider))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsBlocked(Vector2 position)
        {
            Collider2D[] overlaps =
                Physics2D.OverlapCircleAll(
                    position,
                    agentRadius
                );

            foreach (Collider2D collider in overlaps)
            {
                if (IsNavigationObstacle(collider))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsNavigationObstacle(
            Collider2D collider)
        {
            if (collider == null ||
                collider.isTrigger)
            {
                return false;
            }

            if (collider.transform == transform ||
                collider.transform.IsChildOf(transform))
            {
                return false;
            }

            int layer =
                collider.gameObject.layer;

            if (layer == playerLayer ||
                layer == enemyLayer)
            {
                return false;
            }

            if (Physics2D.GetIgnoreLayerCollision(
                gameObject.layer,
                layer))
            {
                return false;
            }

            return true;
        }

        private Vector2 GetEscapeDirection(
            Vector2 currentPosition)
        {
            Collider2D[] overlaps =
                Physics2D.OverlapCircleAll(
                    currentPosition,
                    agentRadius
                );

            Vector2 escapeDirection =
                Vector2.zero;

            foreach (Collider2D collider in overlaps)
            {
                if (!IsNavigationObstacle(collider))
                {
                    continue;
                }

                Vector2 closestPoint =
                    collider.ClosestPoint(
                        currentPosition
                    );

                Vector2 awayFromObstacle =
                    currentPosition -
                    closestPoint;

                if (awayFromObstacle.sqrMagnitude <
                    0.0001f)
                {
                    awayFromObstacle =
                        currentPosition -
                        (Vector2)collider.bounds.center;
                }

                escapeDirection +=
                    awayFromObstacle.normalized;
            }

            return escapeDirection.normalized;
        }

        private static Node FindClosestWalkableNode(
            Node[,] nodes,
            Vector2 worldPosition,
            Vector2 gridOrigin)
        {
            Node closestNode = null;
            float closestDistance =
                float.MaxValue;

            int width =
                nodes.GetLength(0);

            int height =
                nodes.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Node node =
                        nodes[x, y];

                    if (!node.walkable)
                    {
                        continue;
                    }

                    Vector2 nodePosition =
                        CellToWorld(
                            node.coordinates,
                            gridOrigin
                        );

                    float distance =
                        (nodePosition - worldPosition)
                        .sqrMagnitude;

                    if (distance >= closestDistance)
                    {
                        continue;
                    }

                    closestDistance =
                        distance;

                    closestNode =
                        node;
                }
            }

            return closestNode;
        }

        private static Node GetLowestCostNode(
            List<Node> nodes)
        {
            Node bestNode =
                nodes[0];

            for (int i = 1; i < nodes.Count; i++)
            {
                Node node =
                    nodes[i];

                if (node.FCost < bestNode.FCost ||
                    node.FCost == bestNode.FCost &&
                    node.hCost < bestNode.hCost)
                {
                    bestNode =
                        node;
                }
            }

            return bestNode;
        }

        private static int CalculateDistanceCost(
            Vector2Int from,
            Vector2Int to)
        {
            int xDistance =
                Mathf.Abs(from.x - to.x);

            int yDistance =
                Mathf.Abs(from.y - to.y);

            int diagonalDistance =
                Mathf.Min(
                    xDistance,
                    yDistance
                );

            int straightDistance =
                Mathf.Abs(
                    xDistance -
                    yDistance
                );

            return
                diagonalDistance *
                DiagonalMoveCost +
                straightDistance *
                StraightMoveCost;
        }

        private static bool IsInsideGrid(
            Vector2Int coordinates,
            int width,
            int height)
        {
            return
                coordinates.x >= 0 &&
                coordinates.y >= 0 &&
                coordinates.x < width &&
                coordinates.y < height;
        }

        private static Vector2 CellToWorld(
            Vector2Int coordinates,
            Vector2 gridOrigin)
        {
            return
                gridOrigin +
                new Vector2(
                    coordinates.x * CellSize,
                    coordinates.y * CellSize
                );
        }

        private sealed class Node
        {
            public Vector2Int coordinates;
            public bool walkable;

            public int gCost =
                int.MaxValue;

            public int hCost;

            public bool closed;

            public Node parent;

            public int FCost =>
                gCost == int.MaxValue
                    ? int.MaxValue
                    : gCost + hCost;

            public Node(
                Vector2Int coordinates,
                bool walkable)
            {
                this.coordinates =
                    coordinates;

                this.walkable =
                    walkable;
            }
        }
    }
}