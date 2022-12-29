namespace AnarchyChess.Server;

public static class Extensions
{
    public static (int Column, int Row) CoordinatesOf<T>(this T[,] matrix, T value) where T : notnull
    {
        var width = matrix.GetLength(0);
        var height = matrix.GetLength(1);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (matrix[x, y].Equals(value))
                    return (x, y);
            }
        }

        return (-1, -1);
    }

    public static void Fill<T>(this T[,] matrix, T value)
    {
        var width = matrix.GetLength(0);
        var height = matrix.GetLength(1);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                matrix[x, y] = value;
            }
        }
    }
}