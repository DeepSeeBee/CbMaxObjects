using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbMaxClrAdapter.Jitter
{
   using System.Collections;
   using System.Drawing;
   using System.IO;
   using System.Runtime.InteropServices;
   using System.Security.Permissions;

   public abstract class CMatrixCellEnumerator  : IEnumerator
   {
      internal CMatrixCellEnumerator(CMatrixData aMatrixData)
      {
         this.MatrixData = aMatrixData;
         this.DimensionCount = aMatrixData.DimensionCount;
         this.DimensionSizes = aMatrixData.DimensionSizes;
         this.Pos = new int[this.DimensionCount];
         this.Bof = true;
      }

      internal void CheckPos()
      {
         if (this.Bof)
            throw new InvalidOperationException("No valid position.");
      }

      internal readonly CMatrixData MatrixData;
      private readonly int DimensionCount;
      private readonly int[] DimensionSizes;
      private int[] PosM;
      public int[] Pos { get { this.CheckPos(); return this.PosM; } private set { this.PosM = value; } }
      private bool Bof;
      private int PlaneM;
      public int Plane { get { return this.PlaneM; } set { this.MatrixData.CheckPlane(value); this.PlaneM = value; } }

      public object Current { get => this.GetCurrentObj(this.Plane); }
      public abstract object GetCurrentObj(int aPlane);

      public void Dispose() { }


      private static void Test(string aId, CMatrixCellEnumerator aEnumerator, int[] aPos, Action<string> aFailAction)
      {
         Test(aId, aEnumerator.Pos.SequenceEqual(aPos), aFailAction);
      }

      private static void Test(string aId, bool aOk, Action<string> aFailAction)
      {
         if (!aOk)
            aFailAction(aId);
      }

      public static void Test(Action<string> aFailAction)
      {
         var aMatrixData = new CMatrixData();
         aMatrixData.Reallocate(CMatrixData.CCellTypeEnum.Char, 3, new int[] { 2, 2, 2 }, 1);
         var aEnumerator = aMatrixData.GetCellEnumerator();
         Test("947ad689-cc81-45ab-9fe3-708b61830795", aEnumerator.MoveNext(), aFailAction);
         Test("84c3a114-e563-4dc4-a478-7d8faf219577", aEnumerator, new int[] { 0, 0, 0 }, aFailAction);
         Test("f8afda6d-0b8a-4581-abf9-6323a5ad0324", aEnumerator.MoveNext(), aFailAction);
         Test("1ab67394-0814-4d62-94f2-209e6b8cb9c7", aEnumerator, new int[] { 1, 0, 0 }, aFailAction);
         Test("154bdd87-ec83-4658-bc01-494ea7ef63fd", aEnumerator.MoveNext(), aFailAction);
         Test("ec67df3f-b547-4431-a071-e4afd2e80181", aEnumerator, new int[] { 0, 1, 0 }, aFailAction);
         Test("32083d5b-656c-421e-babf-1631dcd0f40f", aEnumerator.MoveNext(), aFailAction);
         Test("01b84778-6ed4-4f13-aa1a-3a5adf474aca", aEnumerator, new int[] { 1, 1, 0 }, aFailAction);
         Test("df8d56ea-5208-491c-8af9-258b0375f988", aEnumerator.MoveNext(), aFailAction);
         Test("88a5d93e-0937-483e-ab40-0e51de4bee8d", aEnumerator, new int[] { 0, 0, 1 }, aFailAction);
         Test("f1ae375f-f67d-4dc9-a469-8a3c58fc6c02", aEnumerator.MoveNext(), aFailAction);
         Test("124fa3a5-b598-4a48-93f8-78d0cc5578fe", aEnumerator, new int[] { 1, 0, 1 }, aFailAction);
         Test("f183cc2e-3c0a-4a5a-82e4-49e167b276e2", aEnumerator.MoveNext(), aFailAction);
         Test("636549a1-ac5d-4733-a5c7-bfd94469e358", aEnumerator, new int[] { 0, 1, 1 }, aFailAction);
         Test("f183cc2e-3c0a-4a5a-82e4-49e167b276e2", aEnumerator.MoveNext(), aFailAction);
         Test("636549a1-ac5d-4733-a5c7-bfd94469e358", aEnumerator, new int[] { 1, 1, 1 }, aFailAction);
         Test("75ec4a8d-0956-4275-bfae-5f245059c4ad", !aEnumerator.MoveNext(), aFailAction);
      }

      public bool MoveNext()
      {
         if (this.Bof)
         {
            this.Bof = false;
            return this.DimensionSizes[0] > 0;
         }
         else
         {
            for (var aIdx = 0; aIdx < this.DimensionCount; ++aIdx)
            {
               if (this.Pos[aIdx] + 1 < this.DimensionSizes[aIdx])
               {
                  this.Pos[aIdx]++;
                  for (var aIdx2 = aIdx - 1; aIdx2 >= 0; --aIdx2)
                  {
                     this.Pos[aIdx2] = 0;
                  }
                  return true;
               }
            }
            return false;
         }
      }
      public void Reset()
      {
         for(var aIdx =0; aIdx < this.DimensionCount; ++aIdx)
         {
            this.Pos[aIdx] = 0;            
         }
         this.Bof = true;
      }
   }

   public abstract class CMatrixCellEnumerator<T> : CMatrixCellEnumerator
   {
      internal CMatrixCellEnumerator(CMatrixData aMatrixData):base(aMatrixData)
      {

      }
      public new T Current { get => this.CurrentT; }
      internal T CurrentT { get => this.GetCurrent(this.Plane); }
      public abstract T GetCurrent(int aPlane);
   }

   public sealed class CMatrixCellCharEnumerator : CMatrixCellEnumerator<byte>
   {
      internal CMatrixCellCharEnumerator(CMatrixData aMatrixData):base(aMatrixData)
      {
         aMatrixData.CheckCellType(CMatrixData.CCellTypeEnum.Char);
      }
      public override byte GetCurrent(int aPlane)=> this.MatrixData.GetCellChar(this.Plane, this.Pos);
      public override object GetCurrentObj(int aPlane) => this.GetCurrent(aPlane);
   }

   public sealed class CMatrixCellLongEnumerator : CMatrixCellEnumerator<Int32>
   {
      internal CMatrixCellLongEnumerator(CMatrixData aMatrixData) : base(aMatrixData)
      {
         aMatrixData.CheckCellType(CMatrixData.CCellTypeEnum.Long);
      }
      public override Int32 GetCurrent(int aPlane) => this.MatrixData.GetCellLong(this.Plane, this.Pos);
      public override object GetCurrentObj(int aPlane) => this.GetCurrent(aPlane);
   }

   public sealed class CMatrixCellFloat64Enumerator : CMatrixCellEnumerator<double>
   {
      internal CMatrixCellFloat64Enumerator(CMatrixData aMatrixData) : base(aMatrixData)
      {
         aMatrixData.CheckCellType(CMatrixData.CCellTypeEnum.Float64);
      }
      public override double GetCurrent(int aPlane) => this.MatrixData.GetCellLong(this.Plane, this.Pos);
      public override object GetCurrentObj(int aPlane) => this.GetCurrent(aPlane);
   }
   public sealed class CMatrixCellFloat32Enumerator : CMatrixCellEnumerator<float>
   {
      internal CMatrixCellFloat32Enumerator(CMatrixData aMatrixData) : base(aMatrixData)
      {
         aMatrixData.CheckCellType(CMatrixData.CCellTypeEnum.Float32);
      }
      public override float GetCurrent(int aPlane) => this.MatrixData.GetCellLong(this.Plane, this.Pos);
      public override object GetCurrentObj(int aPlane) => this.GetCurrent(aPlane);
   }

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

      public int ByteCount => object.ReferenceEquals(this.Buffer, null) ? 0 : this.Buffer.Length;
      private int DimensionCountM;
      public int DimensionCount => this.DimensionCountM;
      private int[] DimensionSizesM;
      public int[] DimensionSizes => this.DimensionSizesM;
      private int[] DimensionStridesM;
      public int[] DimensionStrides => this.DimensionStridesM;
      private int PlaneCountM;
      public int PlaneCount => this.PlaneCountM;

      internal byte[] Buffer;

      public int CellSize { get; private set; }

      public CMatrixCellCharEnumerator GetCellCharEnumerator() =>new CMatrixCellCharEnumerator(this); 
      public CMatrixCellLongEnumerator GetCellLongEnumerator() => new CMatrixCellLongEnumerator(this);
      public CMatrixCellFloat32Enumerator GetCellFloat32Enumerator() => new CMatrixCellFloat32Enumerator(this);
      public CMatrixCellFloat64Enumerator GetCellFloat64Enumerator() => new CMatrixCellFloat64Enumerator(this);

      public CMatrixCellEnumerator GetCellEnumerator()
      {
         switch(this.CellTypeEnum)
         {
            case CCellTypeEnum.Char:
               return this.GetCellCharEnumerator();
            case CCellTypeEnum.Float32:
               return this.GetCellFloat32Enumerator();
            case CCellTypeEnum.Float64:
               return this.GetCellFloat64Enumerator();
            case CCellTypeEnum.Long:
               return this.GetCellLongEnumerator();
            default:
               throw new Exception("CellTypeEnum out of range.");
         }
      }

      internal void ClearInternal()
      {
         this.ReallocateInternal(0, CCellTypeEnum.Char, 0, new int[] { }, new int[] { }, 0);
      }

      public void Clear()
      {
         this.CheckWrite();
         this.ClearInternal();
      }

      public static int GetCellSize(CCellTypeEnum aCellTypeEnum)
      {
         switch (aCellTypeEnum)
         {
            case CCellTypeEnum.Char:
               return 1;
            case CCellTypeEnum.Float32:
               return 4;
            case CCellTypeEnum.Float64:
               return  8;
            case CCellTypeEnum.Long:
               return 4;
            default:
               throw new ArgumentException("CellTypeEnum missmatch.");
         }
      }

      public void Reallocate(CCellTypeEnum aCellTypeEnum, int aDimensionCount, int[] aDimensionSizes, int aPlaneCount)
      {
         var aStrides = new int[aDimensionCount];
         var aStride = aPlaneCount * GetCellSize(aCellTypeEnum);
         aStrides[0] = aStride;         
         for(var aIdx = 1; aIdx < aDimensionCount; ++aIdx)
         {
            aStride = aStride * aDimensionSizes[aIdx];
            aStrides[aIdx] = aStride;
         }
         var aSize = aStrides[aDimensionCount - 1] * aDimensionSizes[aDimensionCount - 1];
         this.Reallocate(aSize, aCellTypeEnum, aDimensionCount, aDimensionSizes, aStrides, aPlaneCount);
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
            var aCellSize = GetCellSize(aCellTypeEnum);
            if (aByteCount != this.ByteCount)
            {
               this.Buffer = new byte[aByteCount];
            }
            else
            {
               for(var aIdx = 0; aIdx < aByteCount; ++aIdx)
               {
                  this.Buffer[aIdx] = 0;
               }
            }
            this.CellTypeEnum = aCellTypeEnum;
            this.DimensionCountM = aDimensionCount;
            this.DimensionSizesM = aDimensionSizes;
            this.DimensionStridesM = aStrides;
            this.PlaneCountM = aPlaneCount;
            this.CellSize = aCellSize;         
         }
      }

      internal void WriteByteInternal(ref int aPos, byte aByte)
      {
         this.Buffer[aPos] = aByte;
         ++aPos;
      }

      public void CheckCompatible(CMatrixData aRhs)
      {
         if(this.DimensionCount != aRhs.DimensionCount
         || ! this.DimensionSizes.SequenceEqual(aRhs.DimensionSizes)
         || this.PlaneCount != aRhs.PlaneCount
         || this.CellTypeEnum != aRhs.CellTypeEnum)
         {
            throw new Exception("Matrix not compatible.");
         }
      }

      internal int GetBytePos(int aPlane, params int[] aPos)
      {
         int aBytePos = 0;
         for(var aIdx = 0; aIdx < this.DimensionCount; ++aIdx)
         {
            aBytePos += aPos[aIdx] * this.DimensionStrides[aIdx];
         }
         aBytePos += this.CellSize * aPlane;
         return aBytePos;
      }

      public double GetCellFloat64(int aPlane, params int[] aPos)
      {
         this.CheckCellType(CCellTypeEnum.Float64);
         var aBytePos = this.GetBytePos(aPlane, aPos);
         var aDouble = BitConverter.ToDouble(this.Buffer, aBytePos);
         return aDouble;
      }

      public float GetCellFloat32(int aPlane, params int[] aPos)
      {
         this.CheckCellType(CCellTypeEnum.Float32);
         var aBytePos = this.GetBytePos(aPlane, aPos);
         var aDouble = BitConverter.ToSingle(this.Buffer, aBytePos);
         return aDouble;
      }

      internal byte GetCellChar(int aPlane, params int[] aPos)
      {
         this.CheckCellType(CCellTypeEnum.Char);
         var aBytePos = this.GetBytePos(aPlane, aPos);
         var aChar = this.Buffer[aBytePos];
         return aChar;
      }

      internal Int32 GetCellLong(int aPlane, params int[] aPos)
      {
         this.CheckCellType(CCellTypeEnum.Long);
         var aBytePos = this.GetBytePos(aPlane, aPos);
         var aInt32 = BitConverter.ToInt32(this.Buffer, aBytePos);
         return aInt32;
      }

      public void SetCellFloat64(double aValue, int aPlane, params int[] aPos)
      {
         this.CheckCellType(CCellTypeEnum.Float64);
         var aBytePos = this.GetBytePos(aPlane, aPos);
         var aBytes = BitConverter.GetBytes(aValue);
         for(var aIdx = 0; aIdx < aBytes.Length;++aIdx)
         {
            this.Buffer[aBytePos + aIdx] = aBytes[aIdx];
         }
      }
      public void SetCellFloat32(float aValue, int aPlane, params int[] aPos)
      {
         this.CheckCellType(CCellTypeEnum.Float32);
         var aBytePos = this.GetBytePos(aPlane, aPos);
         var aBytes = BitConverter.GetBytes(aValue);
         for (var aIdx = 0; aIdx < aBytes.Length; ++aIdx)
         {
            this.Buffer[aBytePos + aIdx] = aBytes[aIdx];
         }
      }

      public void SetCellLong(Int32 aValue, int aPlane, params int[] aPos)
      {
         this.CheckCellType(CCellTypeEnum.Long);
         var aBytePos = this.GetBytePos(aPlane, aPos);
         var aBytes = BitConverter.GetBytes(aValue);
         for (var aIdx = 0; aIdx < aBytes.Length; ++aIdx)
         {
            this.Buffer[aBytePos + aIdx] = aBytes[aIdx];
         }
      }

      public void SetCellChar(byte aValue, int aPlane, params int[] aPos)
      {
         this.CheckCellType(CCellTypeEnum.Char);
         var aBytePos = this.GetBytePos(aPlane, aPos);
         this.Buffer[aBytePos] = aValue;
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
      internal byte ReadByte(ref int aBytePos)
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

      internal void CheckPlane(int aPlane)
      {
         if (aPlane < 0 || aPlane >= this.PlaneCount)
            throw new ArgumentException("Plane out of range.");
      }

      internal void CheckCellType(CCellTypeEnum aCellTypeEnum)
      {
         if (aCellTypeEnum != this.CellTypeEnum)
            throw new InvalidOperationException("CellTypeEnum missmatch.");
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
