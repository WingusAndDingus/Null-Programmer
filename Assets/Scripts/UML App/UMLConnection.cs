using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UMLConnection : Graphic
{
    [Header("Connection Targets")]
    public RectTransform startNode;
    public RectTransform endNode;
    
    [Header("Line Settings")]
    public float lineThickness = 4f;

    private void Update()
    {
        // Force the UI mesh to redraw every frame so the line follows the nodes
        if (startNode != null && endNode != null)
        {
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (startNode == null || endNode == null) return;

        // Get the exact positions relative to this object's unrotated transform
        Vector3 localStart = rectTransform.InverseTransformPoint(startNode.position);
        Vector3 localEnd = rectTransform.InverseTransformPoint(endNode.position);

        // Calculate the direction and the 90-degree perpendicular angle for thickness
        Vector3 direction = (localEnd - localStart).normalized;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * (lineThickness / 2f);

        // Build the 4 corners of the quad
        UIVertex[] vertices = new UIVertex[4];
        for (int i = 0; i < 4; i++)
        {
            vertices[i] = UIVertex.simpleVert;
            vertices[i].color = color; // Uses the built-in color property from the Graphic class
        }

        vertices[0].position = localStart - perpendicular;
        vertices[1].position = localStart + perpendicular;
        vertices[2].position = localEnd + perpendicular;
        vertices[3].position = localEnd - perpendicular;

        // Draw the shape
        vh.AddUIVertexQuad(vertices);
    }
}