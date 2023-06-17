using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Location
{
    public int x;
    public int y;
}

class Cell
{
    int id;
    public Location location;
    public int level;
}

/// <summary>
/// Defines transport cost on a grid
/// </summary>
public class Map : MonoBehaviour
{
    Grid m_grid;
    // Dictionary<int, Cell> m_cells = new Dictionary<int, Cell>();
    List<Cell> m_cells;

    // Start is called before the first frame update
    void Start()
    {
        m_grid = GetComponent<Grid>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
