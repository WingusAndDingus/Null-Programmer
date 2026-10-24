using System;
using UnityEngine;

// Geometry is separate so every row can be measured before it is positioned.
public static class DialogueChoiceLayout
{
    public sealed class Plan
    {
        public int columns;
        public float width;
        public float fontSize;
        public float contentHeight;
        public Vector2[] positions;
        public float[] heights;
    }

    public static Plan Calculate(Vector2 viewport, int count, float canvasScale,
        Func<int, float, float, float> measureHeight)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        const float gap = 24f;
        const float padding = 26f; // Includes floating motion, entrance and shadow clearance.
        canvasScale = Mathf.Max(0.01f, canvasScale);
        float innerWidth = Mathf.Max(1f, viewport.x - 2f * padding);
        float minimumWidth = Mathf.Max(340f, 230f / canvasScale);
        int desiredColumns = count >= 4 ? 2 : Mathf.Min(count, 3);
        int columns = Mathf.Clamp(Mathf.FloorToInt((innerWidth + gap) / (minimumWidth + gap)), 1, desiredColumns);
        float width = Mathf.Min(Mathf.Max(580f, minimumWidth), (innerWidth - gap * (columns - 1)) / columns);
        float fontSize = Mathf.Max(30f, 14f / canvasScale);
        int rows = Mathf.CeilToInt((float)count / columns);
        var rowHeights = new float[rows];
        for (int i = 0; i < count; i++)
        {
            int row = i / columns;
            rowHeights[row] = Mathf.Max(rowHeights[row], measureHeight(i, width, fontSize));
        }
        float total = 2f * padding + (rows - 1) * gap;
        foreach (float height in rowHeights) total += height;
        var result = new Plan
        {
            columns = columns,
            width = width,
            fontSize = fontSize,
            contentHeight = Mathf.Max(viewport.y, total),
            positions = new Vector2[count],
            heights = new float[count]
        };
        float y = padding + (result.contentHeight - total) * 0.5f;
        for (int row = 0; row < rows; row++)
        {
            int rowCount = Mathf.Min(columns, count - row * columns);
            for (int col = 0; col < rowCount; col++)
            {
                int i = row * columns + col;
                result.positions[i] = new Vector2((col - (rowCount - 1) * 0.5f) * (width + gap), -y);
                result.heights[i] = rowHeights[row];
            }
            y += rowHeights[row] + gap;
        }
        return result;
    }
}
