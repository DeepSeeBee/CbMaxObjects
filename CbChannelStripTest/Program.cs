﻿using CbVirtualMixerMatrix;
using CbVirtualMixerMatrix.GaAnimator;
using CbVirtualMixerMatrix.Graph;
using CbMaxClrAdapter;
using CbMaxClrAdapter.Jitter;
using CbMaxClrAdapter.MGraphics;
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
         var aFailAction = new Action<string>(delegate (string aTestCaseId) { throw new Exception("Test failed: " + aTestCaseId); });
         var aDebugPrint = new Action<string>(delegate (string aMsg) { Console.WriteLine(aMsg); });        
         CListData.Test(aFailAction);
         CVirtualMixerMatrix.Test(aFailAction, aDebugPrint);
         CMatrixCellEnumerator.Test(aFailAction);
         CGaAnimator.Test(aFailAction, aDebugPrint);
         System.Console.ReadKey();
      }
}
}
