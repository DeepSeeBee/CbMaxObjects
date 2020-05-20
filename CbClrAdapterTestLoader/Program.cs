using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CbClrAdapterTestLoader
{
   class Program
   {

      static void Main(string[] args)
      {
         Assembly aAssembly1;
         { 
            var aDllPath = @"C:\Program Files\Cycling '74\Max 8\packages\max-sdk-8.0.3\source\charly_beck\CbClrAdapterTestLoader\mfx_fuckedup\cbmaxclradapter.dll";
            var aAssembly = Assembly.LoadFrom(aDllPath);
            var aTypes = aAssembly.GetTypes();
            var aDllExportsType = aAssembly.GetType("CbMaxClrAdapter.DllExports", true);
            var aSObjectNewType = aAssembly.GetType("CbMaxClrAdapter.DllExports+SObject_New", true);
            var aNewArgs = (dynamic)Activator.CreateInstance(aSObjectNewType);
            aNewArgs.mAssemblyName = "cbvirtualmixermatrix.dll";
            aNewArgs.mTypeName = "CbVirtualMixerMatrix.CVirtualMixerMatrix";
            aAssembly1 = aAssembly;

            var aNewMethod = aDllExportsType.GetMethod("Object_New", BindingFlags.Public | BindingFlags.Static);
            var aObj = aNewMethod.Invoke(null, new object[] { aNewArgs });
         }
         {
            var aDllPath = @"C:\Program Files\Cycling '74\Max 8\packages\max-sdk-8.0.3\source\charly_beck\CbClrAdapterTestLoader\mfx_fuckedup\cbvirtualmixermatrix.dll";
            var aAssembly = Assembly.LoadFrom(aDllPath);
            var aType = aAssembly.GetType("CbVirtualMixerMatrix.CVirtualMixerMatrix", true);
            var aObj = Activator.CreateInstance(aType);

         }

         //var aHandle = DllExports.Object_New(aNewArgs);
         //var aNewArgs = new DllExports.SObject_New();
         //aNewArgs.mAssemblyName = "cbvirtualmixermatrix.dll";
         //aNewArgs.mTypeName = "CbVirtualMixerMatrix.CVirtualMixerMatrix";
         //
         //
         //var aHandle = DllExports.Object_New(aNewArgs);
         //DllExports.Object_Init(aHandle);
      }
   }
}
