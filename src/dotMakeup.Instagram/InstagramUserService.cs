using System.Diagnostics;
using System.Runtime.InteropServices;
using dotMakeup.Instagram.Models;
using Python.Runtime;

namespace dotMakeup.Instagram;

public class InstagramUserService
{

    public InstagramUserService()
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
    public async Task<InstagramUser> GetUserAsync(string username)
    {
        string bio = null;
        string name = null;
        string profilePic = null;
        using (Py.GIL())
        {
            dynamic np = Py.Import("instaloader");

            dynamic insta = np.Instaloader();

            dynamic profile = np.Profile.from_username(insta.context, username);

            bio = profile.biography;
            name = profile.full_name;
            profilePic = profile.profile_pic_url;


        }

        return new InstagramUser();
    }
}