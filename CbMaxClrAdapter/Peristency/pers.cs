using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbMaxClrAdapter.Peristency
{
   public abstract class CDataBlock
   {
      public CDataBlock(CMaxObject aMaxObject, Guid aGuid, string aName)
      {
         this.Guid = aGuid;
      }
      public readonly Guid Guid;
      public Action<Stream> ReadFromStreamAction;
      public Action<Stream> WriteToStreamAction;
      internal MemoryStream MemoryStream;
      internal readonly CMaxObject MaxObject;
      internal bool SaveIsEnabled;
      internal readonly string Name;

      public void NeedsSave()
      {
         this.SaveIsEnabled = true;
      }

      internal void WriteToStream()
      {
         bool aOk = false;
         var aMemoryStream = new MemoryStream();
         try
         {
            this.ReadFromStreamAction(this.MemoryStream);
            aOk = true;
         }
         catch(Exception aExc)
         {
            this.MaxObject.WriteLogErrorMessage(new Exception("Could not save DataBlock '" + this.Name + "'. " + aExc.Message));
            aOk = false;
         }
         if(aOk)
         {
            this.MemoryStream = aMemoryStream;
         }
      }
   }

   internal sealed class CDataBlocks
   {
      internal readonly List<CDataBlock> DataBlocks = new List<CDataBlock>();
      internal void Save()
      {
         foreach(var aDataBlock in this.DataBlocks)
         {
            if(aDataBlock.SaveIsEnabled)
            {
               aDataBlock.WriteToStream();
            }
         }
         var aMemoryStream = new MemoryStream();
         var aStreamWriter = new StreamWriter(aMemoryStream);
         foreach(var aDataBlock in this.DataBlocks)
         {
            aStreamWriter.Write(aDataBlock.Guid.ToByteArray());
            aDataBlock.MemoryStream.Seek(0, SeekOrigin.Begin);
            aDataBlock.MemoryStream.CopyTo(aMemoryStream);
         }
      }
   }
}
