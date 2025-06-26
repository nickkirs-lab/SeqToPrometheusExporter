using System.Globalization;

namespace Seq.App.Prometheus;

internal class ReusableStringWriter : StringWriter
{
    [ThreadStatic]
    static ReusableStringWriter? _pooledWriter;

    /// <summary>
    /// Gets already created StringWriter if there is one available or creates a new one.
    /// </summary>
    public static StringWriter GetOrCreate()
    {
        var writer = _pooledWriter;
        _pooledWriter = null;
        if (writer == null)
        {
            writer = new ReusableStringWriter(CultureInfo.InvariantCulture);
        }

        return writer;
    }

    private ReusableStringWriter(IFormatProvider? formatProvider) : base(formatProvider)
    {
    }

    /// <summary>
    /// Clear this instance and prepare it for reuse in the future.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        // We don't call base.Dispose because all it does is mark the writer as closed so it can't be
        // written to and we want to keep it open as reusable writer.
        GetStringBuilder().Clear();
        _pooledWriter = this;
    }
}