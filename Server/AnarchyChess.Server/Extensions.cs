namespace AnarchyChess.Server;

public static class Extensions
{
    public static (int Row, int Column) CoordinatesOf<T>(this T[,] matrix, T value)
    {
        var width = matrix.GetLength(0);
        var height = matrix.GetLength(1);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (matrix[x, y]!.Equals(value))
                    return (x, y);
            }
        }

        return (-1, -1);
    }
}