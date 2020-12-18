using Args;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlobResumableUpload.Model
{
    [Args.ArgsModel(StringComparison = StringComparison.InvariantCultureIgnoreCase, SwitchDelimiter = "/")]
    public class InputArguments
    {
        [Args.ArgsMemberSwitchAttribute("connectionstring", "cs")]
        public string ConnectionString { get; set; }

        [Args.ArgsMemberSwitchAttribute("containername", "cn")]
        public string ContainerName { get; set; }

        [Args.ArgsMemberSwitchAttribute("filename", "fn")]
        public string FileName { get; set; }

        [Args.ArgsMemberSwitchAttribute("blocksize", "bs")]
        public int BlockSize { get; set; }
    }
}
