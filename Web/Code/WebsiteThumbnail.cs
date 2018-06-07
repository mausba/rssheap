using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

public class CutyCaptWrapper
{
    /// <summary>
    /// 1 - small
    /// 2 - medium
    /// 3 - large
    /// </summary>
    public string CutyCaptPath { get; set; }
    public string CutyCaptDefaultArguments { get; set; }
    private int ThumbWidth { get; set; }
    private int ThumbHeight { get; set; }

    private string FilePathFull { get; set; }
    private string FilePath { get; set; }

    public CutyCaptWrapper(string filePath, int thumbWidth, int thumbHeight)
    {
        ThumbHeight = thumbHeight;
        ThumbWidth = thumbWidth;
        FilePath = filePath;

        string ext = Path.GetExtension(FilePath);
        FilePathFull = FilePath.Replace(ext, ".original" + ext);

        CutyCaptPath = HttpContext.Current.Server.MapPath("~/App_Data/CutyCapt.exe"); // must be within the web root
        CutyCaptDefaultArguments = " --max-wait=10000 --out-format=png --javascript=off --java=off --plugins=off --js-can-open-windows=off --js-can-access-clipboard=off --private-browsing=on";
    }
    /// <summary>
    /// Checks if there is a cached screenshot of the website and returns url path to thumbnail of the website in order to use ase html image element source
    /// Usage example: &lt;img src=&quot;&lt;%=CutyCaptWrapper().GetScreenShot(&quot;http://google.com&quot;)%&gt;&quot; alt=&quot;&quot;&gt;
    /// </summary>
    public void SaveScreenShot(string url)
    {
        if (IsURLValid(url))
        {
            //set thumbnail sizes
            string RunArguments = " --url=" + url + " --out=" + FilePathFull + CutyCaptDefaultArguments;

            ProcessStartInfo info = new ProcessStartInfo(CutyCaptPath, RunArguments);
            info.UseShellExecute = false;
            info.RedirectStandardInput = true;
            info.RedirectStandardError = true;
            info.RedirectStandardOutput = true;
            info.CreateNoWindow = true;
            //info.WorkingDirectory = CutyCaptWorkingDirectory;
            using (Process scr = Process.Start(info))
            {
                //string output = scr.StandardOutput.ReadToEnd();
                scr.WaitForExit(10000);
                ThumbnailCreate(FilePathFull, ThumbWidth, ThumbHeight);
                //return output;
            }
        }
    }
    private void ThumbnailCreate(string sourceFilePath, int NewWidth, int MaxHeight)
    {
        if (!File.Exists(sourceFilePath)) return;
        using (Image FullsizeImage = Image.FromFile(sourceFilePath))
        {
            int NewHeight = MaxHeight;

            ThumbHeight = FullsizeImage.Height * ThumbWidth / FullsizeImage.Width;
            if (NewHeight > MaxHeight)
            {
                NewWidth = FullsizeImage.Width * MaxHeight / FullsizeImage.Height;
                NewHeight = MaxHeight;
            }

            using (Image NewImage = FullsizeImage.GetThumbnailImage(NewWidth, NewHeight, null, IntPtr.Zero))
            {
                NewImage.Save(FilePath, ImageFormat.Jpeg);
            }
        }
    }
    private string GetScreenShotFileName(string url)
    {
        Uri uri = new Uri(url);
        return uri.Host.Replace(".", "_") + uri.LocalPath.Replace("/", "_") + ".png";
    }
    private string GetScreenShotThumbnailFileName(string sourceFilename, int width, int height)
    {
        FileInfo sourceFile = new FileInfo(sourceFilename);
        string shortFilename = sourceFile.Name;
        string ext = Path.GetExtension(shortFilename);
        string replacementEnding = String.Format("{0}x{1}", width, height) + ext;
        return shortFilename.Replace(ext, replacementEnding);
    }

    private bool IsURLValid(string url)
    {
        string strRegex = "^(https?://)"
         + "?(([0-9a-z_!~*'().&=+$%-]+: )?[0-9a-z_!~*'().&=+$%-]+@)?" //user@ 
         + @"(([0-9]{1,3}\.){3}[0-9]{1,3}" // IP- 199.194.52.184 
         + "|" // allows either IP or domain 
         + @"([0-9a-z_!~*'()-]+\.)*" // tertiary domain(s)- www. 
         + @"([0-9a-z][0-9a-z-]{0,61})?[0-9a-z]\." // second level domain 
         + "[a-z]{2,6})" // first level domain- .com or .museum 
         + "(:[0-9]{1,4})?" // port number- :80 
         + "((/?)|" // a slash isn't required if there is no file name 
         + "(/[0-9a-z_!~*'().;?:@&=+$,%#-]+)+/?)$";
        Regex re = new Regex(strRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        if (re.IsMatch(url))
            return (true);
        else
            return (false);
    }
}