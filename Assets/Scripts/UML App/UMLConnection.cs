using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UMLConnection : Graphic
{
    public enum ConnectionType
    {
        Composition,
        Dependency
    }

    [Header("Connection Targets")]
    public RectTransform startNode;
    public RectTransform endNode;
    
    [Header("Line Settings")]
    public ConnectionType connectionType = ConnectionType.Composition;
    public float lineThickness = 4f;

    [Header("Symbol Settings")]
    public float diamondSize = 16f;
    public float arrowSize = 16f;
    [Tooltip("Color of the Composition Diamond (Color field above sets the line/arrow color)")]
    public Color diamondColor = Color.white;

    [Header("Dependency Dashes")]
    public float dashLength = 10f;
    public float gapLength = 8f;

    private void Update()
    {
        if (startNode != null && endNode != null)
        {
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (startNode == null || endNode == null) return;

        Vector3 localStart = rectTransform.InverseTransformPoint(startNode.position);
        Vector3 localEnd = rectTransform.InverseTransformPoint(endNode.position);
        
        Vector3 direction = (localEnd - localStart).normalized;
        float distance = Vector3.Distance(localStart, localEnd);
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0).normalized;

        if (connectionType == ConnectionType.Composition)
        {
            DrawSolidLine(vh, localStart, localEnd, perpendicular);
            
            Vector3 diamondCenter = Vector3.Lerp(localStart, localEnd, 0.5f);
            DrawDiamond(vh, diamondCenter, direction, perpendicular);
        }
        else if (connectionType == ConnectionType.Dependency)
        {
            Vector3 lineEnd = localEnd - (direction * arrowSize);
            DrawDashedLine(vh, localStart, lineEnd, direction, perpendicular, distance - arrowSize);
            
            DrawArrow(vh, localEnd, direction, perpendicular);
        }
    }

    private void DrawSolidLine(VertexHelper vh, Vector3 start, Vector3 end, Vector3 perp)
    {
        Vector3 widthOffset = perp * (lineThickness / 2f);
        
        UIVertex[] vertices = new UIVertex[4];
        for (int i = 0; i < 4; i++) { vertices[i] = UIVertex.simpleVert; vertices[i].color = color; }

        vertices[0].position = start - widthOffset;
        vertices[1].position = start + widthOffset;
        vertices[2].position = end + widthOffset;
        vertices[3].position = end - widthOffset;

        vh.AddUIVertexQuad(vertices);
    }

    private void DrawDashedLine(VertexHelper vh, Vector3 start, Vector3 end, Vector3 dir, Vector3 perp, float totalDistance)
    {
        Vector3 widthOffset = perp * (lineThickness / 2f);
        float currentDist = 0f;
        
        while (currentDist < totalDistance)
        {
            float drawLen = Mathf.Min(dashLength, totalDistance - currentDist);
            Vector3 dashStart = start + (dir * currentDist);
            Vector3 dashEnd = dashStart + (dir * drawLen);
            
            UIVertex[] vertices = new UIVertex[4];
            for (int i = 0; i < 4; i++) { vertices[i] = UIVertex.simpleVert; vertices[i].color = color; }

            vertices[0].position = dashStart - widthOffset;
            vertices[1].position = dashStart + widthOffset;
            vertices[2].position = dashEnd + widthOffset;
            vertices[3].position = dashEnd - widthOffset;

            vh.AddUIVertexQuad(vertices);
            currentDist += dashLength + gapLength;
        }
    }

    private void DrawDiamond(VertexHelper vh, Vector3 center, Vector3 dir, Vector3 perp)
    {
        Vector3 p1 = center - (dir * diamondSize); 
        Vector3 p2 = center + (perp * diamondSize); 
        Vector3 p3 = center + (dir * diamondSize); 
        Vector3 p4 = center - (perp * diamondSize); 

        UIVertex[] vertices = new UIVertex[4];
        // Apply the new diamondColor here instead of the base graphic color
        for (int i = 0; i < 4; i++) { vertices[i] = UIVertex.simpleVert; vertices[i].color = diamondColor; }

        vertices[0].position = p1;
        vertices[1].position = p2;
        vertices[2].position = p3;
        vertices[3].position = p4;

        vh.AddUIVertexQuad(vertices);
    }

    private void DrawArrow(VertexHelper vh, Vector3 tip, Vector3 dir, Vector3 perp)
    {
        Vector3 baseCenter = tip - (dir * arrowSize);
        Vector3 p1 = tip; 
        Vector3 p2 = baseCenter + (perp * (arrowSize * 0.7f)); 
        Vector3 p3 = baseCenter; 
        Vector3 p4 = baseCenter - (perp * (arrowSize * 0.7f)); 

        UIVertex[] vertices = new UIVertex[4];
        for (int i = 0; i < 4; i++) { vertices[i] = UIVertex.simpleVert; vertices[i].color = color; }

        vertices[0].position = p1;
        vertices[1].position = p2;
        vertices[2].position = p3;
        vertices[3].position = p4;

        vh.AddUIVertexQuad(vertices);
    }
}