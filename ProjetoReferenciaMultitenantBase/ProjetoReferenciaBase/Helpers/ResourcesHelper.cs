﻿using System;
using System.IO;
using System.Reflection;

namespace ProjetoReferenciaMultitenantBase.Helpers
{
    public static class ResourcesHelper
    {
        public static void ExtractSaveResource(string filename, string location)
        {
            var a = Assembly.GetExecutingAssembly();
            using (var resFilestream = a.GetManifestResourceStream(filename))
            {
                if (resFilestream != null)
                {
                    var full = Path.Combine(location, filename);

                    using (var stream = File.Create(full))
                    {
                        resFilestream.CopyTo(stream);
                    }

                }
            }
        }
    }
}
