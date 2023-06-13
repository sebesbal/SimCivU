using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.Tilemaps;

[Serializable]
public struct SplineLineRendererSettings
{
    public float width;
    public Material material;
    [Range(16, 512)]
    public int subdivisions;
    public Color startColor, endColor;
}

[RequireComponent(typeof(SplineContainer))]
public class RouteRenderer : MonoBehaviour
{
    //SplineContainer m_SplineContainer;
    Spline m_Spline;
    bool m_Dirty;
    Vector3[] m_Points;

    [SerializeField]
    SplineLineRendererSettings m_LineRendererSettings = new SplineLineRendererSettings() {
        width = .5f,
        subdivisions = 64
    };

    [SerializeField]
    Vector2Int[] m_tiles = new Vector2Int[] { new Vector2Int(0, 0) };

    LineRenderer m_Line;
    Tilemap m_TileMap;

    void Awake()
    {
        //m_SplineContainer = GetComponent<SplineContainer>();
        m_Spline = new Spline();
        m_TileMap = GetComponentInParent<Tilemap>();
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
        if (spline == m_Spline)
            m_Dirty = true;
    }

    void Update()
    {
        if (m_Line == null && m_TileMap != null)
        {
            if (m_Line != null)
                DestroyImmediate(m_Line.gameObject);

            foreach (var tile in m_tiles)
            {
                //BezierKnot knot = new BezierKnot()
                // spline.add
            }

            m_Line = new GameObject().AddComponent<LineRenderer>();
            m_Line.gameObject.name = $"SplineRenderer";
            m_Line.transform.SetParent(transform, true);

            m_Dirty = true;
        }

        // It's nice to be able to see resolution changes at runtime
        if (m_Points?.Length != m_LineRendererSettings.subdivisions)
        {
            m_Dirty = true;
            m_Points = new Vector3[m_LineRendererSettings.subdivisions];
            m_Line.positionCount = m_LineRendererSettings.subdivisions;
        }

        if (!m_Dirty)
            return;

        // m_Dirty = false;
        // var trs = m_SplineContainer.transform.localToWorldMatrix;

        //     for (int i = 0; i < m_LineRendererSettings.subdivisions; i++)
        //     {
        //         m_Points[i] = math.transform(trs, m_Spline.EvaluatePosition(i / (m_LineRendererSettings.subdivisions - 1f)));
        //         // m_Points[i] = math.transform(trs, m_Spline.EvaluatePosition(i / (m_LineRendererSettings.subdivisions - 1f)));
        //         //m_Points[i] = m_TileMap.CellToWorld(new Vector3Int(m_tiles[i].x, m_tiles[i].y, 0));
        //     }

        //     m_Line.widthCurve = new AnimationCurve(new Keyframe(0f, m_LineRendererSettings.width));
        //     m_Line.startColor = m_LineRendererSettings.startColor;
        //     m_Line.endColor = m_LineRendererSettings.endColor;
        //     m_Line.material = m_LineRendererSettings.material;
        //     m_Line.useWorldSpace = true;
        //     m_Line.SetPositions(m_Points);
    }
}