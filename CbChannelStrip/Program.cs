/*
 * Bugs:
 * - Seit 202005180700: Max beendet sich nicht mehr.
 * 
 * 
 * 
 * 
 * 
 * 
 */
using CbMaxClrAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CbChannelStripTest
{
   internal static class CLazyLoad
   {
      internal static T Get<T>(ref T aVar, Func<T> aLoad) where T : class
      {
         if(!(aVar is object))
            aVar = aLoad();
         return aVar;
      }
      internal static T Get<T>(ref T? aVar, Func<T> aLoad) where T : struct
      {
         if (!(aVar.HasValue))
            aVar = aLoad();
         return aVar.Value;
      }
   }

   class Program
   {
      static void Main(string[] args)
      {
         //Assembly.LoadFrom(@"C:\Program Files\Cycling '74\Max 8\packages\max-sdk-8.0.3\source\charly_beck\CbChannelStrip\bin\x64\Debug\CbChannelStrip.exe");


         var aFiles = new string[]
         {
            "cb_clrobject.mxe64",
            "CbVirtualMixerMatrix.exe",
            "CbMaxClrAdapter.dll",
            "RGiesecke.DllExport.dll"
         };

         //var aSubDir = ""

      }
   }
}
