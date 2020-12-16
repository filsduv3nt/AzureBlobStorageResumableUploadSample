using Args;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlobResumableUpload.Model
{
    public class InputArguments
    {
        public string ConnectionString { get; set; }

        public string ContainerName { get; set; }

        public string FileName { get; set; }

        public int BlockSize { get; set; }
    }
}
