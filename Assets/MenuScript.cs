using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MenuScript
{
    [MenuItem("Tools/Assign Tile Material")]    // create a new menu item for the Unity Editor | Tools is the menu item ; Assign.. is the sub-menu item
    public static void AssignTileMaterial()     // this function is the behaviour of this menu item | making it static means we don't need to load the script
    {
        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile"); // get all objects with tag "Tile" and return them as GameObjects
        Material material = Resources.Load<Material>("Tile");   // go to Resources folder and look for Material with name "Tile"

        foreach (GameObject t in tiles)
        {
            t.GetComponent<Renderer>().material = material; // get the renderer for each tile and assing its material to that material we have in the Resources folder
        }
    }

    [MenuItem("Tools/Assign Tile Script")]
    public static void AssignTileScript()
    {
        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");

        foreach (GameObject t in tiles)
        {
            t.AddComponent<Tile>(); // add the Tile script to all tiles
        }
    }
}
