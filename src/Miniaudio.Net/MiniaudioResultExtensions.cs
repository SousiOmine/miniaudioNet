using Miniaudio.Net.Interop;

namespace Miniaudio.Net;

internal static class MiniaudioResultExtensions
{
    public static void EnsureSuccess(this int result, string api)
    {
        if (result == 0)
        {
            return;
        }

        var description = NativeMethods.DescribeResult(result);
        throw new MiniaudioException(result, api, description);
    }
}
