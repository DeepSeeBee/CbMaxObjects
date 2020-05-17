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
      public CPoint(double aXy) : this(aXy, aXy)
      {
      }

      public CPoint(Tuple<double, double> aTuple):this(aTuple.Item1, aTuple.Item2) { }
      public readonly double X;
      public readonly double Y;
      public double Hypothenuse { get => Math.Sqrt((this.X * this.X) + (this.Y * this.Y)); }
      public static bool operator ==(CPoint aLhs, CPoint aRhs) => aLhs.X == aRhs.X && aLhs.Y == aRhs.Y;
      public static bool operator !=(CPoint aLhs, CPoint aRhs) => !(aLhs == aRhs);
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
      public override string ToString() => this.X.ToString() + ", " + this.Y.ToString();
      public CPoint Min(CPoint aRhs) => new CPoint(Math.Min(this.X, aRhs.X), Math.Min(this.Y, aRhs.Y));
      public CPoint Max(CPoint aRhs) => new CPoint(Math.Max(this.X, aRhs.X), Math.Max(this.Y, aRhs.Y));
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
         var l = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2)); 
         var ud = new CPoint(d.X / l, d.Y / l);                                                 
         var p = new CPoint(-ud.Y, ud.X);                                           
         var n = new CPoint(p1.X - ud.Y * dist, p1.Y + ud.X * dist); 
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
         return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
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
         this.X = aX;
         this.Y = aY;
         this.Dx = aDx;
         this.Dy = aDy;
      }
      public CRectangle (CPoint aPos, CPoint aSize) : this(aPos.X, aPos.Y, aSize.X, aSize.Y) { }
      public readonly double X;
      public readonly double Y;
      public readonly double Dx;
      public readonly double Dy;
      public CPoint TopLeft { get => new CPoint(this.X, this.Y); }
      public CPoint TopRight { get => new CPoint(this.X + this.Dx, this.Y); }
      public CPoint BottomLeft { get => new CPoint(this.X, this.Y + this.Dy); }
      public CPoint BottomRight { get => new CPoint(this.X + this.Dx, this.Y + this.Dy); }
      public double Diagonale { get => new CPoint(this.Dx, this.Dy).Hypothenuse; }
      public CRectangle CenterRect(CPoint aSize) => new CRectangle(this.X + (this.Dx - aSize.X) / 2.0d,
                                                               this.Y + (this.Dy - aSize.Y) / 2.0d,
                                                               aSize.X,
                                                               aSize.Y
                                                               );
      public CPoint Pos { get => new CPoint(this.X, this.Y); }

      public bool Contains(CPoint aPoint) => this.X <= aPoint.X
                                          && this.Y <= aPoint.Y
                                          && this.X + this.Dx >= aPoint.X
                                          && this.Y + this.Dy >= aPoint.Y
                                          ;
      public CPoint CenterPoint { get => new CPoint(this.X + this.Dx / 2.0d, this.Y + this.Dy / 2.0d); }
      public override string ToString() => "CRectangle: " + this.X + ";" + this.Y + ";" + this.Dx + ";" + this.Dy;
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
      public void Translate(double aX, double aY) => this.Send("translate", aX, aY);
      public void Translate(CPoint aTransform) => this.Translate(aTransform.X, aTransform.Y);
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
