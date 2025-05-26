using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace user
{
    public partial class Data
    {
        public class DataPart
        {
            public byte[] dataHash;
            public byte[] dataPart;
            public byte[] partHash;
        }
        static public DataPart[] SplitData(byte[] data, int maxLength)
        {
            throw new NotImplementedException();
        }
    }
}
