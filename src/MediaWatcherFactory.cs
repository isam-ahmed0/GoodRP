using System.Runtime.InteropServices;
using GoodRP.Interfaces;

namespace GoodRP;

public static class MediaWatcherFactory
{
    public static IMediaWatcher Create()
    {
#if WINDOWS
        return new MediaWatcher();
#else
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new Linux.LinuxMediaWatcher();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new Macos.MacosMediaWatcher();

        return new Linux.LinuxMediaWatcher();
#endif
    }
}
