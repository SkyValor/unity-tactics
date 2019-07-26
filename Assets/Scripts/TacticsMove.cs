using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticsMove : MonoBehaviour
{
    List<Tile> selectableTiles = new List<Tile>();
    GameObject[] tiles;

    Stack<Tile> path = new Stack<Tile>();
    Tile currentTile;

    public bool moving = false;
    public int move = 5;
    public float jumpHeight = 2;
    public float moveSpeed = 2;
    public float jumpVelocity = 4.5f;

    Vector3 velocity = new Vector3();
    Vector3 heading = new Vector3();

    float halfHeight = 0;

    bool fallingDown = false;
    bool jumpingUp = false;
    bool movingToEdge = false;
    Vector3 jumpTarget;

    public void Init()
    {
        tiles = GameObject.FindGameObjectsWithTag("Tile");

        // the halfHeight of the player comes from its collider, which has a height of 2. Half of it would be 1, but it's multiplied by its scale (0.9) which ends being 0.9
        halfHeight = GetComponent<Collider>().bounds.extents.y;
    }

    // gets the Tile sitting underneath this unit
    public void GetCurrentTile()
    {
        currentTile = GetTargetTile(gameObject); // gets the target tile for this unit
        currentTile.current = true;
    }

    // gets the Tile sitting underneath the specified GameObject
    public Tile GetTargetTile(GameObject target)
    {
        RaycastHit hit;
        Tile tile = null;

        // shoot a Raycast downwards with a distance of 1
        if (Physics.Raycast(target.transform.position, -Vector3.up, out hit, 1))
        {
            // hitting a collider (of the Tile), cache that Tile
            tile = hit.collider.GetComponent<Tile>();
        }

        // return the target tile or null (hardly will return null)
        return tile;
    }

    public void ComputeAdjacencyLists()
    {
        // if map is able to morph (change its structure) , we need to cache the tiles each time we want the adjacency list to be updated
        //tiles = GameObject.FindGameObjectsWithTag("Tile");

        foreach (GameObject tile in tiles)
        {
            Tile t = tile.GetComponent<Tile>();
            t.FindNeighbors(jumpHeight);
        }
    }

    public void FindSelectableTiles()
    {
        ComputeAdjacencyLists();
        GetCurrentTile();

        Queue<Tile> process = new Queue<Tile>();

        process.Enqueue(currentTile);
        currentTile.visited = true;
        //currentTile.parent = ??   leave as null

        while (process.Count > 0)
        {
            Tile t = process.Dequeue(); // pops out the item in the front

            selectableTiles.Add(t);
            t.selectable = true;
  
            if (t.distance < move)  // we only want to define this tile if it's lower than the unit's move property
            {
                foreach (Tile tile in t.adjacencyList)
                {
                    if (!tile.visited)
                    {
                        tile.parent = t;                    // every tile adjacent to this one becomes a child
                        tile.visited = true;                // this tile is visited , so it is not seached again by BFS
                        tile.distance = 1 + t.distance;     // the distance of this tile is 1 plus the distance of its parent
                        process.Enqueue(tile);              // we add this child tile so that it's processed later
                    }
                }
            }
        }
    }

    public void MoveToTile(Tile tile)
    {
        path.Clear();   // if there is a path from a previous run, we want to clear it. We are now going to have a new path
        tile.target = true;
        moving = true;  // after selecting the target tile, we want to move there

        Tile next = tile; // this is the target tile, so this is our end location
        while (next != null)
        {
            path.Push(next);    // push the tile to create the path
            next = next.parent; // next is equal to the parent, so we are always walking from the parent. We start at the target and follow the parents until the initial position
        }
    }

    // we want the unit to move from tile to tile, while there are tiles to move onto
    public void Move()
    {
        if (path.Count > 0) // while there are tiles in this path, the moving will happen
        {
            Tile t = path.Peek();   // we peek at the tile in the Stack. We don't remove it until we're moving, we simply peek at it
            Vector3 target = t.transform.position;  // this is where we're moving to

            // but we're not moving TO the tile. The player sits ON the tile. We would be burried in the tile if we did that
            // so we have to sum the halfHeight of the player and the extents of the target's collider to the target position
            target.y += halfHeight + t.GetComponent<Collider>().bounds.extents.y;

            // how do you know when you reached the desired position? When the distance difference is really small
            if (Vector3.Distance(transform.position, target) >= 0.05f)
            {
                // we need to know if jumping is required
                bool jump = transform.position.y != target.y;

                if (jump)
                {
                    Jump(target);
                }
                else
                {
                    CalculateHeading(target);
                    SetHorizontalVelocity();
                }

                //Locomotion
                transform.forward = heading;                        // that's gonna face the direction we wanna go
                transform.position += velocity * Time.deltaTime;    // we update our position | this makes the movement happen little by little
            }
            else
            {
                // Tile center reached
                transform.position = target;
                path.Pop(); // we can pop that tile from the Stack. We don't need it because we've reached it
            }
        }
        else  // as soon as no more tiles exist in this path, we want some things to happen
        {
            RemoveSelectableTiles();    // since they're no longer active
            moving = false;             // since we are no longer moving; after reaching the target
        }
    }

    protected void RemoveSelectableTiles()
    {
        if (currentTile != null)    // if there is a current tile...
        {
            currentTile.current = false;    // ... we set the current to false...
            currentTile = null;             // ... and we clear it by setting it to null
        }

        foreach (Tile tile in selectableTiles)
        {
            tile.Reset();
        }

        selectableTiles.Clear();
    }

    void CalculateHeading(Vector3 target)
    {
        heading = target - transform.position;  // that's the direction we need to travel into
        heading.Normalize(); // heading should be normalized | we want this to represent a direction, not the velocity itself
    }

    // this is going to make us move forward
    void SetHorizontalVelocity()
    {
        velocity = heading * moveSpeed;
    }

    void Jump(Vector3 target)
    {
        // jumping is going to need a State Machine. We're gonna be using a very simple State Machine
        // because there's different parts to jumping

        // are we jumping up or down (falling) ?

        // jumping up means the start is at the center of the initial tile and the unit moves to the edge of the target tile

        // jumping down meang the unit moves to the edge of the initial tile and jumps down to the center of the target tile

        if (fallingDown)
        {
            FallDownward(target);
        }
        else if (jumpingUp)
        {
            JumpUpward(target);
        }
        else if (movingToEdge)
        {
            MoveToEdge();
        }
        else
        {
            PrepareToJump(target);
        }
    }

    void PrepareToJump(Vector3 target)
    {
        // from what we've seen before, the character tilts when moving upwards or downwards
        // we don't want this, so this cached Y value will prevent that heading from changing, even though the velocity is going to move our character upwards or downwards
        float targetY = target.y;

        target.y = transform.position.y;

        CalculateHeading(target);

        // now, we need to find out if we are jumping up or down
        if (transform.position.y > targetY)
        {
            // here, we are falling down
            // but before we can move down, we have to move to the edge

            // let's set the flags correctly now
            fallingDown = false;
            jumpingUp = false;
            movingToEdge = true;

            // where exactly is the edge of the tile?
            jumpTarget = transform.position + (target - transform.position) / 2.0f; // this represents the line between two tiles, so it's the exact edge of the one we're currently on
        }
        else
        {
            fallingDown = false;
            jumpingUp = true;
            movingToEdge = false;

            velocity = heading * moveSpeed / 3.0f;  // when jumping, we want to slow-down, otherwise the jumps would be too fast

            // how far are we jumping upward for?
            float difference = targetY - transform.position.y;

            // to actually make the jump happen
            velocity.y = jumpVelocity * (0.5f + difference / 2.0f);
        }
    }

    void FallDownward(Vector3 target)
    {
        // falling down from a jump or from an edge is simply moving down
        // therefore, we use Physics for the effect
        velocity += Physics.gravity * Time.deltaTime;

        // eventually, we will fall under our target position, because of gravity
        if (transform.position.y <= target.y)
        {
            // first, we stop the falling by setting this property to false
            fallingDown = false;
            jumpingUp = false;
            movingToEdge = false;

            // then, we set the position of the unit to our target position
            Vector3 p = transform.position;
            p.y = target.y;
            transform.position = p;

            // at this point, we're done moving
            velocity = new Vector3();
        }
    }

    void JumpUpward(Vector3 target)
    {
        // jumping is more than just going up; it also comes down.. because gravity.
        // we're using the Physics.gravity for that effect
        velocity += Physics.gravity * Time.deltaTime;

        // have we completed jumping over the edge of the target?
        if (transform.position.y > target.y)
        {
            // if we did, we just stop jumping up and start falling down
            jumpingUp = false;
            fallingDown = true;
        }
    }

    // moves the unit to the edge of a tile, before the unit can fall down
    void MoveToEdge()
    {
        // how do we know when we are on the edge of the tile?
        if (Vector3.Distance(transform.position, jumpTarget) >= 0.05f)
        {
            // at this point, the heading has a value; we know at what direction we're going
            // so we only need to set the horizontal velocity, to make the walking happen
            SetHorizontalVelocity();
        }
        // once we've reached the edge (or 0.05f of the edge)
        else
        {
            // we no longer want to keep moving and we can start falling down
            movingToEdge = false;
            fallingDown = true;

            // just like jumping upward, we want to slow down the movement here
            // otherwise, the unit might pass the target position and have to come back to be in the exact spot, which looks weird
            velocity /= 5.0f;
            // characters don't just drop from the tile, they jump from it
            // so we're going to give that little boost upward
            velocity.y = 1.5f;
        }
    }
}
