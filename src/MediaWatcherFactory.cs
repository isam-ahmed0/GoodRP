using System.Runtime.InteropServices;
using GoodRP.Interfaces;

namespace GoodRP;

public static class MediaWatcherFactory
{
    public static IMediaWatcher Create()
    {
#if WINDOWS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new MediaWatcher();
#endif

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new Linux.LinuxMediaWatcher();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new Macos.MacosMediaWatcher();

#if WINDOWS
        return new MediaWatcher();
#else
        return new Linux.LinuxMediaWatcher();
#endif
    }
}
