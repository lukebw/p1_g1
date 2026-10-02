using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public SpriteRenderer background;
    public Tree[] trees;
    public BoxCollider2D boundary;

    Dictionary<Direction, Chunk> neighbors = new Dictionary<Direction, Chunk>();
    float width;
    float height;

    // The 8 directions bordering this Chunk, in English reading order
    public enum Direction
    {
        NW, N, NE,
        W, E,
        SW, S, SE,
        UNDEFINED
    }

    void Awake()
    {
        boundary.enabled = true;

        // Initialize the neighbor dictionary with null values
        foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
        {
            neighbors[direction] = null;
        }
    }

    void Start()
    {
        // Optionally, you can generate neighbors at the start if needed
        // GenerateNeighbors();
        width = boundary.size.x;
        height = boundary.size.y;
    }

    void Update()
    {

    }

    // TODO: generate neighbors when player enters this chunk, and unload them when player leaves this chunk
    void GenerateNeighbors()
    {
        // For each direction, check if a neighbor exists; if not, generate one
        foreach (Direction direction in System.Enum.GetValues(typeof(Direction)))
        {
            if (direction != Direction.UNDEFINED && neighbors[direction] == null)
            {
                // Generate a new chunk in the specified direction
                Vector2 offset = GetOffsetForDirection(direction);
                Vector3 newPosition = transform.position + new Vector3(offset.x, offset.y, 0);
                GameObject newChunkObj = Instantiate(gameObject, newPosition, Quaternion.identity);
                Chunk newChunk = newChunkObj.GetComponent<Chunk>();

                // Set the neighbor relationship
                neighbors[direction] = newChunk;
                newChunk.neighbors[GetOppositeDirection(direction)] = this;
            }
        }
    }

    Vector2 GetOffsetForDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.NW: return new Vector2(-width, height);
            case Direction.N: return new Vector2(0, height);
            case Direction.NE: return new Vector2(width, height);
            case Direction.W: return new Vector2(-width, 0);
            case Direction.E: return new Vector2(width, 0);
            case Direction.SW: return new Vector2(-width, -height);
            case Direction.S: return new Vector2(0, -height);
            case Direction.SE: return new Vector2(width, -height);
            default: return Vector2.zero;
        }
    }

    Direction GetOppositeDirection(Direction inputDirection)
    {
        switch (inputDirection)
        {
            case Direction.NW: return Direction.SE;
            case Direction.N: return Direction.S;
            case Direction.NE: return Direction.SW;
            case Direction.W: return Direction.E;
            case Direction.E: return Direction.W;
            case Direction.SW: return Direction.NE;
            case Direction.S: return Direction.N;
            case Direction.SE: return Direction.NW;
            default: return Direction.UNDEFINED;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered");
            // GenerateNeighbors();
        }
    }
}
