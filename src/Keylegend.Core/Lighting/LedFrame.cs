namespace Keylegend.Core.Lighting;

/// <summary>
/// One rendered picture of the keyboard: a colour for every cell of the vendor LED matrix.
/// Frames are reused between renders rather than reallocated, because a frame is produced on
/// every state change and allocation churn in that path is pointless.
/// </summary>
public sealed class LedFrame
{
    private readonly RgbColor[,] _cells;

    public LedFrame(int rows, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);

        Rows = rows;
        Columns = columns;
        _cells = new RgbColor[rows, columns];
    }

    public int Rows { get; }

    public int Columns { get; }

    public RgbColor this[int row, int column]
    {
        get
        {
            ThrowIfOutside(row, column);
            return _cells[row, column];
        }
    }

    public void Set(int row, int column, RgbColor colour)
    {
        ThrowIfOutside(row, column);
        _cells[row, column] = colour;
    }

    public void Clear() => Array.Clear(_cells);

    /// <summary>Renders the frame in the packing the Chroma SDK expects.</summary>
    public int[][] ToBgrMatrix()
    {
        var matrix = new int[Rows][];

        for (var row = 0; row < Rows; row++)
        {
            var line = new int[Columns];

            for (var column = 0; column < Columns; column++)
            {
                line[column] = _cells[row, column].ToBgr();
            }

            matrix[row] = line;
        }

        return matrix;
    }

    private void ThrowIfOutside(int row, int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(row);
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
    }
}
