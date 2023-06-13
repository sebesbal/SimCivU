using System;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Tilemaps;

/// <summary>
/// A simple example showing how to pass Spline data to the GPU using SplineComputeBufferScope.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer), typeof(SplineContainer))]
public class RouteRenderer2 : MonoBehaviour
{
    // Use with Shader/InterpolateSpline.compute
    [SerializeField]
    ComputeShader m_ComputeShader;
    Tilemap m_TileMap;

    [SerializeField, Range(16, 512)]
    int m_Segments = 128;
    //int m_Segments = 1024;

    Spline m_Spline;
    LineRenderer m_Line;
    bool m_Dirty = false;

    SplineComputeBufferScope<Spline> m_SplineBuffers;
    Vector3[] m_Positions;
    ComputeBuffer m_PositionsBuffer;
    int m_GetPositionsKernel;

    [SerializeField]
    Vector2Int[] m_cells = new Vector2Int[] { new Vector2Int(0, 0) };

    Vector3 CellToWorld(Vector2 cell)
    {
        return m_TileMap.CellToWorld(new Vector3Int((int)cell.x, (int)cell.y)) + new Vector3(0, 1.0f, 0);
    }

    Spline CellsToSpline(Vector2Int[] cells)
    {
        int n = cells.Length;
        var spline = new Spline(n);
        for (int i = 0; i < cells.Length; i++)
        {
            var cell = cells[i];
            var knot = new BezierKnot(CellToWorld(cell) + new Vector3(0, 1, 0), new Vector3(0, 0, -1), new Vector3(0, 0, 1));
            spline.Add(knot);
        }
        return spline;
    }

    Spline CellsToSpline2(Vector2Int[] cells)
    {
        static Quaternion angle(Vector3 v)
        {
            return Quaternion.Euler(0, Vector3.Angle(v, Vector3.forward), 0);
        };

        var offset = new Vector3(0, 3, 0);
        int n = cells.Length;
        var spline = new Spline(n + 1);
        Vector3[] pos = new Vector3[n + 1];
        Quaternion[] rot = new Quaternion[n + 1];
        var tangentIn = new Vector3(0, 0, -0.2f);
        var tangentOut = new Vector3(0, 0, 0.2f);

        for (int i = 0; i < n; i++)
        {
            pos[i] = CellToWorld(cells[i]);
        }
        pos[n] = pos[n - 1];

        Vector3 a = pos[0];
        for (int i = 1; i < n; i++)
        {
            var b = pos[i];
            pos[i] = (a + b) / 2;
            a = b;
        }

        rot[0] = angle(pos[1] - pos[0]);
        for (int i = 1; i < n; i++)
        {
            rot[i] = angle(pos[i + 1] - pos[i - 1]);
        }
        rot[n] = angle(pos[n] - pos[n - 1]);

        for (int i = 0; i < n + 1; i++)
        {
            var knot = new BezierKnot(pos[i] + offset, tangentIn, tangentOut, rot[i]);
            spline.Add(knot);
        }
        return spline;
    }

    private Spline CellsToSpline3(Vector2Int[] cellPositions)
    {
        var spline = new Spline();

        for (int i = 0; i < cellPositions.Length; i++)
        {
            Vector3 worldPosition = CellToWorld(cellPositions[i]);
            BezierKnot knot = new BezierKnot(worldPosition);

            if (i > 0)
            {
                Vector3 prevWorldPosition = CellToWorld(cellPositions[i - 1]);
                knot.TangentIn = (worldPosition - prevWorldPosition);
            }

            if (i < cellPositions.Length - 1)
            {
                Vector3 nextWorldPosition = CellToWorld(cellPositions[i + 1]);
                knot.TangentOut = (nextWorldPosition - worldPosition);
            }

            knot.TangentIn = - (knot.TangentIn + knot.TangentOut) / 4;
            knot.TangentOut = - knot.TangentIn;

            Debug.Log("Knot: " + knot.ToString());
            spline.Add(knot);
        }
        return spline;
    }
    private Spline CellsToSpline4(Vector2Int[] cellPositions)
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
        //Debug.Log("Awake called");
        m_TileMap = GetComponentInParent<Tilemap>();
        if (m_TileMap != null && m_cells.Length > 0)
        {
            m_Spline = CellsToSpline4(m_cells);
        }
        else
        {
            m_Spline = GetComponent<SplineContainer>().Spline;
        }

        // Set up the LineRenderer
        m_Line = GetComponent<LineRenderer>();
        m_Line.positionCount = m_Segments;

        m_GetPositionsKernel = m_ComputeShader.FindKernel("GetPositions");

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

        m_Dirty = true;
    }

    void OnEnable()
    {
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

    void Update()
    {
        //Debug.Log("Update called");
        if (!m_Dirty)
            return;
        m_Dirty = false;

        // Once initialized, call SplineComputeBufferScope.Upload() to update the GPU copies of spline data. This
        // is only necessary here because we're constantly updating the Spline in this example. If the Spline is
        // static, there is no need to call Upload every frame.
        m_SplineBuffers.Upload();

        m_ComputeShader.GetKernelThreadGroupSizes(m_GetPositionsKernel, out var threadSize, out _, out _);
        m_ComputeShader.Dispatch(m_GetPositionsKernel, (int)threadSize, 1, 1);
        m_PositionsBuffer.GetData(m_Positions);

        m_Line.loop = m_Spline.Closed;
        m_Line.SetPositions(m_Positions);
    }
}