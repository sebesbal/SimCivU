using System;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

/// <summary>
/// Renders a route spline on the map
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer), typeof(SplineContainer))]
public class RouteRenderer : MonoBehaviour
{
    // Use with Shader/InterpolateSpline.compute
    [SerializeField]
    ComputeShader m_ComputeShader;
    [SerializeField, Range(16, 512)]
    int m_Segments = 128;
    [SerializeField]
    Vector2Int[] m_cells = new Vector2Int[] { new Vector2Int(0, 0) };
    Vector2Int[] m_lastCells = null;

    bool m_Dirty = false;
    int m_GetPositionsKernel;
    Tilemap m_TileMap;
    Spline m_Spline;
    LineRenderer m_Line;
    SplineComputeBufferScope<Spline> m_SplineBuffers;
    Vector3[] m_Positions;
    ComputeBuffer m_PositionsBuffer;

    void OnValidate()
    {
        if (HasArrayChanged(m_cells, m_lastCells))
        {
            m_Dirty = true;
            m_lastCells = (Vector2Int[])m_cells.Clone();
        }
    }

    bool HasArrayChanged(Vector2Int[] a, Vector2Int[] b)
    {
        if (b == null || a.Length != b.Length)
        {
            return true;
        }

        for (int i = 0; i < a.Length; ++i)
        {
            if (a[i] != b[i])
            {
                return true;
            }
        }

        return false;
    }

    Vector3 CellToWorld(Vector2 cell)
    {
        return m_TileMap.CellToWorld(new Vector3Int((int)cell.x, (int)cell.y)) + new Vector3(0, 1.0f, 0);
    }

    private Spline CellsToSpline(Vector2Int[] cellPositions)
    {
        var spline = new Spline();
        if (cellPositions.Length < 2)
        {
            return spline;
        }
        float q = -0.25f;
        Vector3 a = CellToWorld(cellPositions[0]);
        Vector3 b = CellToWorld(cellPositions[1]);
        BezierKnot knot = new BezierKnot(a);
        knot.TangentIn = q * (b - a);
        knot.TangentOut = -knot.TangentIn;
        spline.Add(knot);

        for (int i = 0; i < cellPositions.Length - 1; i++)
        {
            a = CellToWorld(cellPositions[i]);
            b = CellToWorld(cellPositions[i + 1]);
            Vector3 c = (a + b) / 2;
            knot = new BezierKnot(c);
            knot.TangentIn = q * (b - a);
            knot.TangentOut = -knot.TangentIn;
            spline.Add(knot);
        }

        a = CellToWorld(cellPositions[cellPositions.Length - 2]);
        b = CellToWorld(cellPositions[cellPositions.Length - 1]);
        knot = new BezierKnot(b);
        knot.TangentIn = q * (b - a);
        knot.TangentOut = -knot.TangentIn;
        spline.Add(knot);

        return spline;
    }

    void Awake()
    {
        Debug.Log("Start");
        m_TileMap = GetComponentInParent<Tilemap>();
        m_Line = GetComponent<LineRenderer>();
        m_Line.positionCount = m_Segments;
        m_GetPositionsKernel = m_ComputeShader.FindKernel("GetPositions");
        m_Dirty = true;
    }

    void OnEnable()
    {
        m_lastCells = (Vector2Int[])m_cells.Clone();
        Spline.Changed += OnSplineChanged;
    }

    void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
    }

    void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
    {
        if (m_Spline == spline)
            m_Dirty = true;
    }

    void OnDestroy()
    {
        m_PositionsBuffer?.Dispose();
        m_SplineBuffers.Dispose();
    }

    void updateSplineFromCells()
    {
        m_Dirty = false;
        m_Spline = CellsToSpline(m_cells);

        // Set up the spline evaluation compute shader. We'll use SplineComputeBufferScope to simplify the process.
        // Note that SplineComputeBufferScope is optional, you can manage the Curve, Lengths, and Info properties
        // yourself if preferred.
        m_SplineBuffers = new SplineComputeBufferScope<Spline>(m_Spline);
        m_SplineBuffers.Bind(m_ComputeShader, m_GetPositionsKernel, "info", "curves", "curveLengths");

        // Set the compute shader properties necessary for accessing spline information. Most Spline functions in
        // Spline.cginc require the info, curves, and curve length properties. This is equivalent to:
        //     m_ComputeShader.SetVector("info", m_SplineBuffers.Info);
        //     m_ComputeShader.SetBuffer(m_GetPositionsKernel, "curves", m_SplineBuffers.Curves);
        //     m_ComputeShader.SetBuffer(m_GetPositionsKernel, "curveLengths", m_SplineBuffers.CurveLengths);
        m_SplineBuffers.Upload();

        // m_Positions will be used to read back evaluated positions from the GPU
        m_Positions = new Vector3[m_Segments];

        // Set up our input and readback buffers. In this example we'll evaluate a set of positions along the spline
        m_PositionsBuffer = new ComputeBuffer(m_Segments, sizeof(float) * 3);
        m_PositionsBuffer.SetData(m_Positions);
        m_ComputeShader.SetBuffer(m_GetPositionsKernel, "positions", m_PositionsBuffer);
        m_ComputeShader.SetFloat("positionsCount", m_Segments);

        m_ComputeShader.GetKernelThreadGroupSizes(m_GetPositionsKernel, out var threadSize, out _, out _);
        m_ComputeShader.Dispatch(m_GetPositionsKernel, (int)threadSize, 1, 1);
        m_PositionsBuffer.GetData(m_Positions);

        m_Line.loop = m_Spline.Closed;
        m_Line.SetPositions(m_Positions);
    }

    void Update()
    {
        if (m_Dirty)
        {
            m_Dirty = false;
            updateSplineFromCells();
        }
    }
}