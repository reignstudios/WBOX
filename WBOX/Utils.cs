using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WBOX
{
	static class Utils
	{
		public static string GetNewestSubPath(string rootPath)
		{
            string result = string.Empty;
			if (Directory.Exists(rootPath))
            {
                var date = DateTime.MinValue;
                foreach (string path in Directory.GetDirectories(rootPath))
                {
                    var folderData = Directory.GetCreationTime(path);
                    if (folderData >= date)
                    {
                        date = folderData;
                        result = path;
                    }
                }
            }
            return result;
		}
	}
}
