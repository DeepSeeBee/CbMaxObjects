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
using CbMaxClrAdapter.Deploy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
      [STAThread]
      static void Main(string[] args)
      {
         CPackage.Install(aPackage =>
         {
            var aExternals = new Tuple<string, bool>[]
            {
               new Tuple<string, bool>("cb_clrobject.mxe64", false),
               new Tuple<string, bool>("CbVirtualMixerMatrix.exe", false),
               new Tuple<string, bool>("CbMaxClrAdapter.dll", false),
               new Tuple<string, bool>("RGiesecke.DllExport.dll", false),
               new Tuple<string, bool>("CbVirtualMixerMatrix.mxf", true)
            }; 
            foreach(var aExternal in aExternals)
            {
               aPackage.AddExternal(aExternal.Item1, @"packages\charly_beck\CbVirtualMixerMatrix\externals", aExternal.Item2);
            }
         });
      }
   }
}
