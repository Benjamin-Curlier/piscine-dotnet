using System.Runtime.InteropServices;

internal static partial class NativeApi
{
    [LibraryImport("asteria_native", EntryPoint = "read_sensor",
        StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    internal static partial int ReadSensor(string name, out double value);
}
