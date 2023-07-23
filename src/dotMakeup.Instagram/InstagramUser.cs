using System.Diagnostics;
using Python.Runtime;

namespace dotMakeup.Instagram;

public class InstagramUser
{
    public async Task<string> GetUserAsync(string username)
    {
        Runtime.PythonDLL = @"/opt/homebrew/opt/python@3.11/Frameworks/Python.framework/Versions/3.11/lib/libpython3.11.dylib";
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();
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