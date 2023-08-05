using System.Diagnostics;
using System.Runtime.InteropServices;
using Python.Runtime;

namespace dotMakeup.Instagram;

public class InstagramUser
{

    public InstagramUser()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Runtime.PythonDLL = @"/opt/homebrew/opt/python@3.11/Frameworks/Python.framework/Versions/3.11/lib/libpython3.11.dylib";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Runtime.PythonDLL = @"/lib64/libpython3.11.so";
        }
        
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();
    }
    public async Task<string> GetUserAsync(string username)
    {
        string bio = null;
        using (Py.GIL())
        {
            dynamic np = Py.Import("instaloader");

            dynamic insta = np.Instaloader();

            dynamic profile = np.Profile.from_username(insta.context, username);

            bio = profile.biography;
            
        }

        return bio;
    }
}