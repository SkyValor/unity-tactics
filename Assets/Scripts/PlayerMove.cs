using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : TacticsMove
{
    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        // drawing a line forward, so we know which direction the player is facing
        Debug.DrawRay(transform.position, transform.forward);

        if (!moving)
        {
            FindSelectableTiles();
            CheckMouse();
        }
        else
        {
            Move();
        }
    }
    
    // we want to check if the player clicked on a certain tile, and if he did, which one it was
    void CheckMouse()
    {
        if (Input.GetMouseButtonUp(0)) // returns true in the frame that the user releases the mouse button | 0 means the left click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // shooting a ray from the camera into the game scene, starting at the mouse position

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))  // if this ray collides with something...
            {
                if (hit.collider.tag == "Tile") // ... we wanna know if it's a Tile
                {
                    Tile t = hit.collider.GetComponent<Tile>();

                    if (t.selectable)
                    {
                        MoveToTile(t);
                    }
                }
            }
        }
    }
}
