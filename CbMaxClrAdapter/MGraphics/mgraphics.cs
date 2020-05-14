using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbMaxClrAdapter.MGraphics
{
   public struct CPoint
   {
      public CPoint(double aX, double aY)
      {
         this.X = aX;
         this.Y = aY;
      }
      public CPoint(Tuple<double, double> aTuple):this(aTuple.Item1, aTuple.Item2) { }
      public readonly double X;
      public readonly double Y;
      public static CPoint operator -(CPoint aLhs, CPoint aRhs) => new CPoint(aLhs.X - aRhs.X, aLhs.Y - aRhs.Y);
      public static CPoint operator +(CPoint aLhs, CPoint aRhs) => new CPoint(aLhs.X + aRhs.X, aLhs.Y + aRhs.Y);
      public static CPoint operator *(CPoint aLhs, CPoint aRhs) => new CPoint(aLhs.X * aRhs.X, aLhs.Y * aRhs.Y);
      public static CPoint operator /(CPoint aLhs, CPoint aRhs) => new CPoint(aLhs.X / aRhs.X, aLhs.Y / aRhs.Y);
      public CPoint Rotate(double aAngle)
      {
         double s = Math.Sin(aAngle);
         double c = Math.Cos(aAngle);
         double x = this.X * c - this.Y * s;
         double y = this.X * s + this.Y * c;
         return new CPoint(x, y);
      }
   }
   public struct CRectangle
   {
      public CRectangle(double aX, double aY, double aDx, double aDy)
      {
         this.X = aX;
         this.Y = aY;
         this.Dx = aDx;
         this.Dy = aDy;
      }
      public readonly double X;
      public readonly double Y;
      public readonly double Dx;
      public readonly double Dy;

      public CRectangle Center(CPoint aSize) => new CRectangle(this.X + (this.Dx - aSize.X) / 2.0d,
                                                               this.Y + (this.Dy - aSize.Y) / 2.0d,
                                                               aSize.X,
                                                               aSize.Y
                                                               );
      public CPoint Pos { get => new CPoint(this.X, this.Y); }
   }

   public class CVector2dPainter 
   {
      public CVector2dPainter(CListInlet aDumpIn, COutlet aOut)
      {
         this.Out = aOut;
         this.DumpIn = aDumpIn;
      }

      private readonly COutlet Out;
      private readonly CListInlet DumpIn;

      private void Send(params object[] aValues)=> this.Out.SendValues(aValues);
      private double ColorPart(byte aPart) => ((double)aPart / (double)255);
      private double R(Color aColor) => ColorPart(aColor.R);
      private double G(Color aColor) => ColorPart(aColor.G);
      private double B(Color aColor) => ColorPart(aColor.B);
      private double A(Color aColor) => ColorPart(aColor.A);
      public void SetColor(Color aColor)=> this.Send("set_source_rgba", R(aColor), G(aColor), B(aColor), A(aColor));
      public void Paint() => this.Send("paint");
      public void IdentityMatrix() => this.Send("identity_matrix");
      public void MoveTo(double aX, double aY) => this.Send("move_to", aX, aY);
      public void LineTo(double aX, double aY) => this.Send("line_to", aX, aY);
      public void LineTo(CPoint aPoint) => this.LineTo(aPoint.X, aPoint.Y);
      public void MoveTo(CPoint aPos) => this.MoveTo(aPos.X, aPos.Y);
      public void SetLineWidth(double aD) => this.Send("set_line_width", aD); 
      public void Clear() => this.Clear(Color.White, Color.Black);
      public void Clear(Color aBackground, Color aForeground)
      {
         this.SetColor(aBackground);         
         this.Paint();
         this.SetColor(aForeground);
         this.IdentityMatrix();
         this.MoveTo(0, 0);
      }
      public void Ellipse(double aX, double aY, double aWidth, double aHeight) => this.Send("ellipse", aX, aY, aWidth, aHeight);
      public void Fill() => this.Send("fill");
      public void Scale(double aXScale, double aYScale) => this.Send("scale", aXScale, aYScale);
      public void Scale(CPoint aScale) => this.Scale(aScale.X, aScale.Y);
      public CPoint TextMeasure(string aString)
      {
         var aIn = this.DumpIn.Message.Value;
         aIn.Clear(); // 79b3fe31-16ca-4a05-865c-85bc7e15a3c4
         this.Send("text_measure", aString);
         if(aIn.Symbol == "text_measure"
         && aIn.Count() == 3)
         {
            var aX = Convert.ToDouble(aIn.ElementAt(1));
            var aY = Convert.ToDouble(aIn.ElementAt(2));
            var aPoint = new CPoint(aX, aY); 
            return aPoint;
         }
         else
         {
            this.DumpIn.MaxObject.WriteLogErrorMessage("No result for TextMeasure");
            return new CPoint(0, 0);
         }
      }
       
      public void Text(string aText)
      {
         //this.DumpIn.MaxObject.WriteLogInfoMessage("CVector2dPainter.Text(\"" + aText  + "\")");
         this.Send("show_text", aText);
      }
      public void Text(string aText, CRectangle aRect)
      {
         var aSize = this.TextMeasure(aText);
         var aPos = aRect.Center(aSize).Pos;
         this.MoveTo(aPos);
         this.Text(aText);
      }

      public void Stroke() => this.Send("stroke");
      public void NewPath() => this.Send("new_path");
      public void ClosePath() => this.Send("close_path");
      public void Rectangle(CRectangle aRect) => this.Send(aRect.X, aRect.Y, aRect.Dx, aRect.Dy);
      
      public CPoint ImageSurfaceSize
      {
         get
         {
            var aList = this.DumpIn.Message.Value;
            aList.Clear();
            this.Send("image_surface_get_size");
            if (aList.Symbol == "image_surface_get_size"
            && aList.Count() == 3)
            {
               var aDx = Convert.ToDouble(aList.ElementAt(1));
               var aDy = Convert.ToDouble(aList.ElementAt(2));
               return new CPoint(aDx, aDy);
            }
            else
            {
               this.DumpIn.MaxObject.WriteLogErrorMessage("No result for ImageSurfaceGetSize");
               return new CPoint(0, 0);
            }
         }
      }
   }
}
