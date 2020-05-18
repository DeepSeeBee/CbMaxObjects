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
         this.x = aX;
         this.y = aY;
      }
      public CPoint(double aXy) : this(aXy, aXy)
      {
      }


      public CPoint(Tuple<double, double> aTuple):this(aTuple.Item1, aTuple.Item2) { }
      public readonly double x;
      public readonly double y;
      public double Hypothenuse { get => Math.Sqrt((this.x * this.x) + (this.y * this.y)); }
      public static bool operator ==(CPoint aLhs, CPoint aRhs) => aLhs.x == aRhs.x && aLhs.y == aRhs.y;
      public static bool operator !=(CPoint aLhs, CPoint aRhs) => !(aLhs == aRhs);
      public static CPoint operator -(CPoint aLhs, CPoint aRhs) => new CPoint(aLhs.x - aRhs.x, aLhs.y - aRhs.y);
      public static CPoint operator +(CPoint aLhs, CPoint aRhs) => new CPoint(aLhs.x + aRhs.x, aLhs.y + aRhs.y);
      public static CPoint operator *(CPoint aLhs, CPoint aRhs) => new CPoint(aLhs.x * aRhs.x, aLhs.y * aRhs.y);
      public static CPoint operator /(CPoint aLhs, CPoint aRhs) => new CPoint(aLhs.x / aRhs.x, aLhs.y / aRhs.y);
      public CPoint Rotate(double aAngle)
      {
         double s = Math.Sin(aAngle);
         double c = Math.Cos(aAngle);
         double x = this.x * c - this.y * s;
         double y = this.x * s + this.y * c;
         return new CPoint(x, y);
      }
      public override string ToString() => this.x.ToString() + ", " + this.y.ToString();
      public CPoint Min(CPoint aRhs) => new CPoint(Math.Min(this.x, aRhs.x), Math.Min(this.y, aRhs.y));
      public CPoint Max(CPoint aRhs) => new CPoint(Math.Max(this.x, aRhs.x), Math.Max(this.y, aRhs.y));
      public static CPoint GetSizeOfPreservedRatioScale(CPoint aImageSize, CPoint aScreenSize)
      { 
         var wi = aScreenSize.x;
         var hi = aScreenSize.y;
         var ri = wi / hi;
         var ws = aImageSize.x;
         var hs = aImageSize.y;
         var rs = ws / hs;
         var aSize = rs > ri
           ? new CPoint(wi * hs / hi, hs)
           : new CPoint(ws, hi * ws / wi);
         return aSize;
      }

   }

   public struct CLine
   {
      public CLine(CPoint p1, CPoint p2)
      {
         this.P1 = p1;
         this.P2 = p2;
      }
      public readonly CPoint P1;
      public readonly CPoint P2;

      public CLine Paralell(double dist)
      {
         var p1 = this.P1;
         var p2 = this.P2;                           
         var d = p2 - p1;                           
         var l = Math.Sqrt(Math.Pow(p2.x - p1.x, 2) + Math.Pow(p2.y - p1.y, 2)); 
         var ud = new CPoint(d.x / l, d.y / l);                                                 
         var p = new CPoint(-ud.y, ud.x);                                           
         var n = new CPoint(p1.x - ud.y * dist, p1.y + ud.x * dist); 
         var s = n + d;
         return new CLine(n, s);

      }

   }
   public struct CTriangle
   {
      public CTriangle(CPoint p1, CPoint p2, CPoint p3)
      {
         this.P1 = p1;
         this.P2 = p2;
         this.P3 = p3;
      }

      public readonly CPoint P1;
      public readonly CPoint P2;
      public readonly CPoint P3;

      private double sign(CPoint p1, CPoint p2, CPoint p3)
      {
         return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
      }

      public bool Contains(CPoint pt)
      {
         var d1 = sign(pt, this.P1, this.P2);
         var d2 = sign(pt, this.P2, this.P3);
         var d3 = sign(pt, this.P3, this.P1);
         var n = (d1 < 0) || (d2 < 0) || (d3 < 0);
         var p = (d1 > 0) || (d2 > 0) || (d3 > 0);
         var aContains = !(n && p);
         return aContains;
      }
   }

   public struct CRectangle
   {
      public CRectangle(double aX, double aY, double aDx, double aDy)
      {
         this.x = aX;
         this.y = aY;
         this.Dx = aDx;
         this.Dy = aDy;
      }
      public CRectangle (CPoint aPos, CPoint aSize) : this(aPos.x, aPos.y, aSize.x, aSize.y) { }
      public readonly double x;
      public readonly double y;
      public readonly double Dx;
      public readonly double Dy;
      public CPoint TopLeft { get => new CPoint(this.x, this.y); }
      public CPoint TopRight { get => new CPoint(this.x + this.Dx, this.y); }
      public CPoint BottomLeft { get => new CPoint(this.x, this.y + this.Dy); }
      public CPoint BottomRight { get => new CPoint(this.x + this.Dx, this.y + this.Dy); }
      public double Diagonale { get => new CPoint(this.Dx, this.Dy).Hypothenuse; }
      public CRectangle CenterRect(CPoint aSize) => new CRectangle(this.x + (this.Dx - aSize.x) / 2.0d,
                                                               this.y + (this.Dy - aSize.y) / 2.0d,
                                                               aSize.x,
                                                               aSize.y
                                                               );
      public CPoint Pos { get => new CPoint(this.x, this.y); }

      public bool Contains(CPoint aPoint) => this.x <= aPoint.x
                                          && this.y <= aPoint.y
                                          && this.x + this.Dx >= aPoint.x
                                          && this.y + this.Dy >= aPoint.y
                                          ;
      public CPoint CenterPoint { get => new CPoint(this.x + this.Dx / 2.0d, this.y + this.Dy / 2.0d); }
      public override string ToString() => "CRectangle: " + this.x + ";" + this.y + ";" + this.Dx + ";" + this.Dy;


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

      private void Send(params object[] aValues)=> this.Out.SendValuesO(aValues);
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
      public void LineTo(CPoint aPoint) => this.LineTo(aPoint.x, aPoint.y);
      public void MoveTo(CPoint aPos) => this.MoveTo(aPos.x, aPos.y);
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
      public void Scale(CPoint aScale) => this.Scale(aScale.x, aScale.y);
      public void Translate(double aX, double aY) => this.Send("translate", aX, aY);
      public void Translate(CPoint aTransform) => this.Translate(aTransform.x, aTransform.y);
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
         this.Send("show_text", aText);
      }
      public void Text(string aText, CRectangle aRect)
      {
         var aSize = this.TextMeasure(aText);
         var aPos = aRect.CenterRect(aSize).Pos;
         this.MoveTo(aPos);
         this.Text(aText);
      }

      public void Stroke() => this.Send("stroke");
      public void NewPath() => this.Send("new_path");
      public void ClosePath() => this.Send("close_path");
      public void Rectangle(CRectangle aRect) => this.Send(aRect.x, aRect.y, aRect.Dx, aRect.Dy);
      
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
