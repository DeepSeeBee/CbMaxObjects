using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbMaxClrAdapter.Jitter
{
   using System.Drawing;
   using System.IO;
   using System.Runtime.InteropServices;

   public sealed class CMatrixData : CSealable
   {
      public enum CCellTypeEnum
      {
         Char,
         Long,
         Float32,
         Float64,
      }

      internal CCellTypeEnum CellTypeEnum { get; private set; } // TODO: rest der matrix_info properties public machen.

      internal int ByteCount => object.ReferenceEquals(this.Buffer, null) ? 0 : this.Buffer.Length;
      private int DimensionCountM;
      internal int DimensionCount => this.DimensionCountM;
      private int[] DimensionSizesM;
      internal int[] DimensionSizes => this.DimensionSizesM;
      private int[] DimensionStridesM;
      internal int[] DimensionStrides => this.DimensionStridesM;
      private int PlaneCountM;
      internal int PlaneCount => this.PlaneCountM;

      internal byte[] Buffer;

      internal void ClearInternal()
      {
         this.ReallocateInternal(0, CCellTypeEnum.Char, 0, new int[] { }, new int[] { }, 0);
      }

      public void Clear()
      {
         this.CheckWrite();
         this.ClearInternal();
      }

      public void Reallocate(int aSize, CCellTypeEnum aCellTypeEnum, int aDimensionCount, int[] aDimensionSizes, int[] aStrides, int aPlaneCount)
      {
         this.CheckWrite();
         this.ReallocateInternal(aSize, aCellTypeEnum, aDimensionCount, aDimensionSizes, aStrides, aPlaneCount);
      }
      public void ReallocateInternal(int aSize, CCellTypeEnum aCellTypeEnum, int aDimensionCount, int[] aDimensionSizes, int[] aStrides, int aPlaneCount)
      {
         var aByteCount = aDimensionCount == 0
                        ? 0
                        : aStrides[aDimensionCount - 1] * aDimensionSizes[aDimensionCount - 1]
                        ;
         if (aSize != aByteCount)
         {
            throw new ArgumentException("Size does not match DimensionSizes/Stride");
         }
         else
         {
            if (aByteCount != this.ByteCount)
            {
               this.Buffer = new byte[aByteCount];
            }
            this.CellTypeEnum = aCellTypeEnum;
            this.DimensionCountM = aDimensionCount;
            this.DimensionSizesM = aDimensionSizes;
            this.DimensionStridesM = aStrides;
            this.PlaneCountM = aPlaneCount;
         }
      }

      internal void WriteByteInternal(ref int aPos, byte aByte)
      {
         this.Buffer[aPos] = aByte;
         ++aPos;
      }
      public void WriteByte(ref int aPos, byte aByte)
      {
         this.CheckWrite();
         this.WriteByteInternal(ref aPos, aByte);
      }

      public void SetImage(Image aImage)
      {
         this.SetImage(new Bitmap(aImage));
      }

      private const int ByteAllignment = 16;

      public void PrintMatrixInfo(CMaxObject aMaxObject, string aName = "Matrix")
      {
         aMaxObject.WriteLogInfoMessage(aName + ".ByteCount = " + this.ByteCount);
         aMaxObject.WriteLogInfoMessage(aName + ".CellType = " + this.CellTypeEnum.ToString());
         aMaxObject.WriteLogInfoMessage(aName + ".DimensionCount = " + this.DimensionCount);
         for (var aIdx = 0; aIdx < this.DimensionCount; ++aIdx)
            aMaxObject.WriteLogInfoMessage(aName + ".DimensionSizes[" + aIdx + "] = " + this.DimensionSizes[aIdx]);
         for (var aIdx = 0; aIdx < this.DimensionCount; ++aIdx)
            aMaxObject.WriteLogInfoMessage(aName + ".DimensionStrides[" + aIdx + "] = " + this.DimensionStrides[aIdx]);
         aMaxObject.WriteLogInfoMessage(aName + ".PlaneCount = " + this.PlaneCount);
      }

      public void SetImage(Bitmap aBitmap)
      {
         this.CheckWrite();
         var aDx = aBitmap.Width;
         var aDy = aBitmap.Height;
         var aDimensionCount = 2;
         var aDimensionSizes = new int[] { aDx, aDy };
         var aPlaneCount = 4;
         var aRowUsedBytes = aDx * aPlaneCount; 
         var aRest = aRowUsedBytes % ByteAllignment; 
         var aExtra = (aRest == 0) ? 0 : (ByteAllignment - aRest);
         var aRowStride = aRowUsedBytes + aExtra; 
         var aDimensionStrides = new int[] { aPlaneCount, aRowStride };
         var aByteCount = aDimensionSizes[aDimensionCount - 1] * aDimensionStrides[aDimensionCount - 1];
         this.ReallocateInternal(aByteCount, CCellTypeEnum.Char, aDimensionCount, aDimensionSizes, aDimensionStrides, aPlaneCount);
         var aBytePos = 0;
         for (int aY = 0; aY < aDy; ++aY)
         {
            int aRowBytePos = aBytePos;
            for (int aX = 0; aX < aDx; ++aX)
            {
               var aPixel = aBitmap.GetPixel(aX, aY);
               this.WriteByteInternal(ref aRowBytePos, aPixel.A);
               this.WriteByteInternal(ref aRowBytePos, aPixel.R);
               this.WriteByteInternal(ref aRowBytePos, aPixel.G);
               this.WriteByteInternal(ref aRowBytePos, aPixel.B); 
            }
            aBytePos += aDimensionStrides[1]; 
         }
      }
      public byte ReadByte(ref int aBytePos)
      {
         var aByte = this.Buffer[aBytePos]; 
         ++aBytePos;
         return aByte;
      }

      public Bitmap NewBitmap()
      { // TODO_OPT
         if (this.DimensionCount == 2
         && this.PlaneCount == 4
         && this.DimensionStrides[0] == 4)
         {
            var aBytePos = 0;
            var aDx = this.DimensionSizes[0];
            var aDy = this.DimensionSizes[1];
            var aBitmap = new Bitmap(aDx, aDy);
            for (int aY = 0; aY < aDy; ++aY)
            {
               int aRowBytePos = aBytePos;
               for (int aX = 0; aX < aDx; ++aX)
               {
                  var aA = this.ReadByte(ref aRowBytePos);
                  var aR = this.ReadByte(ref aRowBytePos);
                  var aG = this.ReadByte(ref aRowBytePos);
                  var aB = this.ReadByte(ref aRowBytePos);
                  var aPixelColor = Color.FromArgb(aA, aR, aG, aB);
                  aBitmap.SetPixel(aX, aY, aPixelColor);
               }
               aBytePos = aBytePos + this.DimensionStrides[1];
            }
            return aBitmap;
         }
         else
         {
            throw new FormatException("Matrixformat not supported.");
         }
      }
   }

   [CDataType(CMessageTypeEnum.Matrix)]
   public sealed class CMatrix : CValMessage<CMatrixData>
   {
      public CMatrix(CMatrixData aMatrixData)
      {
         this.Value = aMatrixData;
      }
      public CMatrix() : this(new CMatrixData())
      {
      }

      internal override void AddTo(CEditableListData value) => new InvalidOperationException();
      internal override void Send(CMultiTypeOutlet aMultiTypeOutlet) => aMultiTypeOutlet.MaxObject.Marshal.Send(aMultiTypeOutlet, this);
   }

   public sealed class CMatrixOutlet
   :
       CSingleTypeOutlet<CMatrix>
   {
      public CMatrixOutlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
      }

      public override void Send()
      {
         this.MaxObject.Marshal.Send(this);
      }

   }

   public sealed class CMatrixInlet
   :
       CSingleTypeInlet<CMatrix>
   {
      public CMatrixInlet(CMaxObject aMaxObject) : base(aMaxObject)
      {
         this.SupportInternal();
         this.SupportInternal(CMessageTypeEnum.List);
      }

      private const string Symbol = "jit_matrix";

      protected override void Receive(CMessage aMessage)
      {
         if (aMessage is CList)
         {
            var aList = (CList)aMessage;
            var aListData = aList.Value;
            if (aListData.Count() == 2
            && aListData.ElementAt(0).ToString() == Symbol)
            {
               var aObjectName = aListData.ElementAt(1).ToString();
               this.MaxObject.Marshal.Object_In_Matrix_Receive(this.Index, aObjectName);
            }
            else
            {
               throw this.MaxObject.NewDoesNotUnderstandExc(this, "this list");
            }
         }
         else if (aMessage is CMatrix)
         {
            base.Receive((CMatrix)(object)aMessage);
         }
         else
         {
            throw this.MaxObject.NewDoesNotUnderstandExc(this, aMessage.DataTypeEnum);
         }
      }


   }
}
