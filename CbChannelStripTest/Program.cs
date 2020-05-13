using CbChannelStrip;
using CbMaxClrAdapter;
using CbMaxClrAdapter.Jitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CbChannelStripTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var aFailAction = new Action<string>(delegate(string aTestCaseId) { throw new Exception("Test failed: " + aTestCaseId); });
            CListData.Test(aFailAction);
            CChannelStrip.Test(aFailAction);
            CMatrixCellEnumerator.Test(aFailAction);
        }
    }
}
