﻿using CbVirtualMixerMatrix.Graph;
using CbVirtualMixerMatrix.GraphViz;
using CbChannelStripTest;
using CbMaxClrAdapter;
using CbMaxClrAdapter.Jitter;
using CbMaxClrAdapter.MGraphics;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Documents;
using System.Windows.Media.Converters;
using System.Windows.Threading;

namespace CbVirtualMixerMatrix.GaAnimator
{
   internal enum CDropEffectEnum
   {
      Remove,
      Add,
      Focus,
      None
   }

   internal abstract class CGaShape
   {
      internal static readonly Color DefaultColor = Color.Black;
      internal static readonly System.Drawing.Color AnnouncingColor = System.Drawing.Color.Magenta;
      internal static readonly System.Drawing.Color FocusedColor = System.Drawing.Color.Green;
      internal static readonly System.Drawing.Color HoveringColorAdd = System.Drawing.Color.LimeGreen;
      internal static readonly System.Drawing.Color HoveringColorRemove = System.Drawing.Color.Red;
      internal static readonly System.Drawing.Color HoveringColorFocus = System.Drawing.Color.LimeGreen;

      internal readonly CGaGraph GaGraph;
      internal readonly CGaAnimator GaAnimator;
      internal CGaShape(CGaGraph aGaGraph)
      {
         this.GaGraph = aGaGraph;
         this.GaAnimator = aGaGraph.GaAnimator;
      }

      private volatile object OpacityM = (double)0.0d;
      internal double Opacity { get => (double)this.OpacityM; set => this.OpacityM = value; }
      internal abstract string Name { get; }
      internal abstract void Paint(CVector2dPainter aOut);

      internal double LineWidth = 2.0;
      internal bool IsFocused { get => this.GaAnimator.State.GetIsFocused(this); }
      internal virtual void AnimateAppear(double aPercent)
      {
         this.Opacity = 1.0d - aPercent;
         this.Announcing = true;
      }

      internal virtual void AnimateDisappear(double aPercent)
      {
         this.Opacity = aPercent;
         this.Announcing = true;
      }
      internal abstract CGaMorph NewMorph(CGaTransition aGaTransition, CGaNode aNewNode);
      internal abstract CGaMorph NewMorph(CGaTransition aGaTransition, CGaEdge aNewEdge);
      internal virtual bool HasMorph { get => true; }
      internal abstract CGaMorph AcceptNewMorph(CGaTransition aGaTransition, CGaShape aOldShape);
      internal virtual void Init() { }
      internal bool Announcing;
      internal CDropEffectEnum? DropEffectEnum;
      internal virtual System.Drawing.Color HoveringColor { get => DefaultColor; }
      internal abstract bool HitTestEnabled { get; }
      internal System.Drawing.Color? ResolveColor(System.Drawing.Color? aColor)
      {
         if (this.Announcing)
            return AnnouncingColor;
         else if (this.DropEffectEnum.HasValue)
            switch(this.DropEffectEnum.Value)
            {
               case CDropEffectEnum.Add:
                  return HoveringColorAdd;
               case CDropEffectEnum.Remove:
                  return HoveringColorRemove;
               case CDropEffectEnum.Focus:
                  return HoveringColorFocus;
               case CDropEffectEnum.None:
                  break;
               default:
                  throw new ArgumentException("DropEffectEnumOutOfRange: " + this.DropEffectEnum.Value.ToString());               
            }
         if (this.IsFocused)
            return FocusedColor;
         return aColor;
      }
      internal virtual void Animate(CGaAnnounceAnimation aAnnounceAnimation)
      {
         if (aAnnounceAnimation.IsRunning)
         {
            this.Announcing = true;
         }
      }

      internal abstract CRectangle Rect { get; }
      internal abstract bool ContainsPointExact(CPoint aPoint);
      internal bool ContainsPoint(CPoint aPoint) => this.Rect.Contains(aPoint) && this.ContainsPointExact(aPoint);
   }

   internal abstract class CGaEdgeBase : CGaShape
   {
      internal CGaEdgeBase(CGaGraph aGraph):base(aGraph)
      {
      }

      private volatile CPoint[] SplinesM;
      internal virtual CPoint[] Splines 
      {
         get
         {
            if (!(this.SplinesM is object))
            {
               this.SplinesM = new CPoint[] { new CPoint(), new CPoint() };
            }
            return this.SplinesM;
         }
         set => this.SplinesM = value; 
      }
      internal virtual CPoint P0 { get => this.Splines.First(); }
      internal virtual CPoint P1 { get => this.Splines.Skip(1).First(); }      
      internal virtual CPoint P2 { get => this.Splines.Last(); }
      private object ColorM = default(Color?);
      internal Color? Color { get => (Color?)this.ColorM; set => this.ColorM = value; }
      internal virtual bool P2TipIsVisible { get => true; }
      internal virtual bool IsVisible { get => true; }
      internal override void Paint(CVector2dPainter aOut)
      {
         if (this.IsVisible)
         {
            var aBezier = this.Splines;
            var aSplines = aBezier.Skip(1);
            var aBaseColor1 = this.Color;
            var aBaseColor2 = this.ResolveColor(aBaseColor1);
            var aBaseColor = aBaseColor2.GetValueOrDefault(DefaultColor);
            var aOpacity = this.Opacity;
            var aAlpha = 1.0d - aOpacity;
            var aColor = System.Drawing.Color.FromArgb((int)(aAlpha * 255.0d), aBaseColor);
            aOut.NewPath();
            aOut.SetColor(aColor);
            var aFirst = true;
            foreach (var aPoint in aSplines)
            {
               if (aFirst)
                  aOut.MoveTo(aPoint);
               else
                  aOut.LineTo(aPoint);
               aFirst = false;
            }
            aOut.Stroke();

            if (this.P2TipIsVisible)
            { // DrawArrowTip
               var aTip = this.P2Tip;
               aOut.NewPath();
               aOut.MoveTo(aTip.P1);
               aOut.LineTo(aTip.P2);
               aOut.LineTo(aTip.P3);
               aOut.ClosePath();
               aOut.Fill();
            }
         }
      }
      internal CTriangle P2Tip
      {
         get
         {
            //var aBezier = this.Splines;
            var aTip = this.P0;
            var aP1 = this.P2;
            var aP2 = aTip - aP1;
            var a90 = Math.PI / 2.0d;
            var aLen = new CPoint(0.75d, 0.75d);
            var aC1 = aP2.Rotate(a90) * aLen + aP1;
            var aC2 = aP2.Rotate(-a90) * aLen + aP1;
            var aTriangle = new CTriangle(aTip, aC1, aC2);
            return aTriangle;
         }
      }

      internal double HitTestWidth = 16;

      internal override bool ContainsPointExact(CPoint aPoint)
      {
         if(this.IsVisible)
         {     
            bool aContains = false;
            var aSplines = this.Splines;
            var aP2TipIsVisible = this.P2TipIsVisible;          
            if (aP2TipIsVisible
            && this.P2Tip.Contains(aPoint))
            {
               return true;
            }
            else
            {
               var aWidth2 = this.HitTestWidth / 2.0d;
               for (var aIdx = 1; aIdx < aSplines.Length - 1 && !aContains; ++aIdx)
               {
                  var p1 = aSplines[aIdx];
                  var p2 = aSplines[aIdx + 1];
                  var aLine0 = new CLine(p1, p2);
                  var aLine1 = aLine0.Paralell(aWidth2);
                  var aLine2 = aLine0.Paralell(-aWidth2);
                  var aP1 = aLine1.P1;
                  var aP2 = aLine1.P2;
                  var aP3 = aLine2.P2;
                  var aP4 = aLine2.P1;
                  var aT1 = new CTriangle(aP1, aP2, aP4);
                  var aT2 = new CTriangle(aP2, aP3, aP4);
                  aContains = aContains
                           || aT1.Contains(aPoint)
                           || aT2.Contains(aPoint)
                           ;
               }
               return aContains;
            }
         }
         else
         {
            return false;
         }
      }
      internal override CRectangle Rect
      {
         get
         {
            var aPoint1 = this.P1;
            var aPoint2 = this.P2;
            var aTopLeft = aPoint1.Min(aPoint2);
            var aBottomRight = aPoint2.Max(aPoint2);
            var aWidth = this.LineWidth;
            var aP1 = new CPoint(aTopLeft.x - aWidth / 2.0f, aTopLeft.y);
            var aP2 = new CPoint(aBottomRight.x + aWidth / 2.0f, aBottomRight.y);
            var aRect = new CRectangle(aP1, aP2 - aP1);
            return aRect;
         }
      }
   }

   internal sealed class CGaEdge : CGaEdgeBase
   {
      internal CGaEdge(CGaGraph aGaGraph, CGwEdge aGwEdge, CGaNode aGaNode1, CGaNode aGaNode2) :base(aGaGraph)
      {
         this.GwEdge = aGwEdge;
         this.Splines = (from aSpline in aGwEdge.Splines select new CPoint(aSpline.x, aSpline.y)).ToArray();
         this.GaNode1 = aGaNode1;
         this.GaNode2 = aGaNode2;
         this.Color = aGwEdge.Color;
      }
      internal readonly CGwEdge GwEdge; 
      internal override string Name { get => this.GwEdge.Name; }
      internal CGaEdge CopyEdge() => new CGaEdge(this.GaGraph, this.GwEdge, this.GaNode1, this.GaNode2);

      internal readonly CGaNode GaNode1;
      internal readonly CGaNode GaNode2;
      internal override bool HitTestEnabled => false; // TODO-Lines hading to the bottom left are hittest false-negative.
      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaEdge aNewEdge) => new CGaEdgeMorph(aGaTransition, this, aNewEdge);
      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaNode aNewNode) => throw new InvalidOperationException();
      internal override CGaMorph AcceptNewMorph(CGaTransition aGaTransition, CGaShape aOldShape) => aOldShape.NewMorph(aGaTransition, this);
      internal override Color HoveringColor => HoveringColorRemove;
      //internal volatile CPoint[] Splines;
      internal override void Animate(CGaAnnounceAnimation aAnnounceAnimation)
      {
         base.Animate(aAnnounceAnimation);

         this.Color = aAnnounceAnimation.ColorWobble.MorphColor(this.GwEdge.Color, AnnouncingColor);
      }

   }

   internal sealed class CGaNode : CGaShape
   {
      internal CGaNode(CGaGraph aGraph, CGwNode aGwNode):base(aGraph)
      {
         this.GwNode = aGwNode;
         this.CenterPos = new CPoint(aGwNode.CenterX, aGwNode.CenterY);
         this.Color = aGwNode.Color;
         this.FontColor = aGwNode.FontColor;
      }
      
      internal CGaNode CopyNode() => new CGaNode(this.GaGraph, this.GwNode);

      internal readonly CGwNode GwNode;
      internal override string Name => this.GwNode.Name;
       
      public CPoint CenterPos { get; set; }

      private volatile object DisappearScaleM = (double)1.0d;
      internal double DisappearScale { get => (double)this.DisappearScaleM; set => this.DisappearScaleM = value; }

      private volatile object AppearScaleM = (double)1.0d;
      internal double AppearScale { get => (double)this.AppearScaleM; set => this.AppearScaleM = value; }

      private volatile object WorkingScaleM = (double)1.0d;
      internal double WorkingScale { get => (double)this.WorkingScaleM; set => this.WorkingScaleM = value; }

      private volatile object AnnounceScaleM = (double)1.0d;
      internal double AnnounceScale { get => (double)this.AnnounceScaleM; set => this.AnnounceScaleM = value; }
      internal double Scale { get { return this.WorkingScale * this.AnnounceScale * this.AppearScale * this.DisappearScale; } }

      private object ColorM = default(Color?);
      internal Color? Color { get => (Color?)this.ColorM; set => this.ColorM = value; }
      private object FontColorM = default(Color?);
      internal Color? FontColor { get => (Color?)this.FontColorM; set => this.FontColorM = value; }
      internal CPoint Size { get => new CPoint(this.GwNode.Dx, this.GwNode.Dy); }
      internal CPoint TopLeftPos { get => new CPoint(this.CenterPos.x - this.Size.x / 2.0d, this.CenterPos.y - this.Size.y / 2.0d); }
      internal CPoint BottomRightPos { get=> new CPoint(this.CenterPos.x + this.Size.x / 2.0d, this.CenterPos.y + this.Size.y / 2.0d); }
      internal override CRectangle Rect { get { var aTl = this.TopLeftPos; var aBr = this.BottomRightPos; return new CRectangle(aTl.x, aTl.y, aBr.x - aTl.x, aBr.y - aTl.y); } }
      internal CTriangle Triangle { get { var r = this.Rect; return new CTriangle(r.BottomLeft, r.BottomRight, new CPoint(r.x + r.Dx / 2.0d, r.y)); } }
      internal CTriangle InvTriangle { get { var r = this.Rect; return new CTriangle(r.TopLeft, r.TopRight, new CPoint(r.x + r.Dx / 2.0d, r.y + r.Dy)); } }
      internal override bool HitTestEnabled => true;
      internal override Color HoveringColor => HoveringColorAdd;
      internal override bool ContainsPointExact(CPoint aPoint)
      {
         switch(this.GwNode.ShapeEnum)
         {
            case CGwNode.CShapeEnum.InvTriangle:
               return this.InvTriangle.Contains(aPoint);
            case CGwNode.CShapeEnum.MCircle:
               return (aPoint - this.CenterPos).Hypothenuse < (this.Rect.Diagonale / 2.0d);
            case CGwNode.CShapeEnum.MSquare:
               return this.Rect.Contains(aPoint);
            case CGwNode.CShapeEnum.Triangle:
               return this.Triangle.Contains(aPoint);
            default:
               return false;
         }
      }
      internal override void AnimateAppear(double aPercent)
      {
         base.AnimateAppear(aPercent);
         this.AppearScale = aPercent;
      }

      internal override void AnimateDisappear(double aPercent)
      {
         base.AnimateDisappear(aPercent);
         this.DisappearScale = 1.0d - aPercent;
      }

      internal override void Paint(CVector2dPainter aOut)
      {
         var aScale = this.Scale;
         var aDx = this.GwNode.Dx * aScale;
         var aDy = this.GwNode.Dy * aScale;
         var aX = this.CenterPos.x  - aDx / 2.0d;
         var aY = this.CenterPos.y  - aDy / 2.0d;
         var aRect = new CRectangle(aX, aY, aDx, aDy);
         var aText = this.Name;
         var aOpacity = this.Opacity;
         var aAlpha = 1.0d - aOpacity;
         var aDefaultColor = DefaultColor;
         var aBaseColor1 = this.Color;
         var aBaseColor2 = this.ResolveColor(aBaseColor1);
         var aBaseColor = aBaseColor2.GetValueOrDefault(aDefaultColor);
         var aColor = System.Drawing.Color.FromArgb((int)(aAlpha * 255.0d), aBaseColor);
         //aOut.SetLineWidth(this.LineWidth);
         aOut.SetColor(aColor);
         switch(this.GwNode.ShapeEnum)
         {
            case CGwNode.CShapeEnum.InvTriangle:
               aOut.NewPath();
               aOut.MoveTo(aX, aY);
               aOut.LineTo(aX + aDx, aY);
               aOut.LineTo(aX + aDx / 2.0d, aY + aDy);
               aOut.ClosePath();
               aOut.Stroke();
               break;

            case CGwNode.CShapeEnum.Triangle:
               aOut.NewPath();
               aOut.MoveTo(aX + aDx / 2.0d, aY); 
               aOut.LineTo(aX + aDx, aY + aDy);
               aOut.LineTo(aX, aY + aDy);
               aOut.ClosePath();
               aOut.Stroke();
               break;

            case CGwNode.CShapeEnum.MCircle:
               aOut.Ellipse(aX, aY, aDx, aDy);
               aOut.Stroke();
               aOut.MoveTo(aX, aY);
               break;

            case CGwNode.CShapeEnum.MSquare:
               aOut.Rectangle(aRect);
               break;
         }
         if (aScale >= 1.0d)
         {
            var aFontBaseColor1 = this.FontColor;
            var aFontBaseColor2 = this.ResolveColor(aFontBaseColor1);
            var aFontBaseColor = aFontBaseColor2.GetValueOrDefault(aDefaultColor);
            var aFontColor = System.Drawing.Color.FromArgb((int)(aAlpha * 255.0d), aFontBaseColor);
            aOut.SetColor(aFontColor);
            aOut.Text(aText, aRect);
         }
      }

      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaEdge aNewEdge) => throw new InvalidOperationException();
      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaNode aNewNode) => new CGaNodeMorph(aGaTransition, this, aNewNode);
      internal override CGaMorph AcceptNewMorph(CGaTransition aGaTransition, CGaShape aOldShape) => aOldShape.NewMorph(aGaTransition, this);

      internal override void Animate(CGaAnnounceAnimation aAnnounceAnimation)
      {
         base.Animate(aAnnounceAnimation);
         this.AnnounceScale = aAnnounceAnimation.ScaleWobble;
         this.Color = aAnnounceAnimation.ColorWobble.MorphColor(this.GwNode.Color, AnnouncingColor);  
      }
   }

   internal sealed class CGaDragEdge  :CGaEdgeBase
   {
      internal CGaDragEdge(CGaGraph aGaGraph) : base(aGaGraph)
      {
         this.Color = FocusedColor;
      }

      internal override CPoint P0 { get => this.GaAnimator.DragEdgeP1.GetValueOrDefault(new CPoint()); }
      internal override CPoint P1 { get => this.GaAnimator.DragEdgeP1.GetValueOrDefault(new CPoint()); }
      internal override CPoint P2 { get => this.GaAnimator.DragEdgeP2.GetValueOrDefault(new CPoint()); }
      internal override CPoint[] Splines { get => new CPoint[] { this.P0, this.P0, this.P2 }; set => base.Splines = value; }
      internal override bool IsVisible { get => this.GaAnimator.DragEdgeVisible; }
      internal override bool P2TipIsVisible => false;
      internal override bool HasMorph => false;
      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaEdge aNewEdge) => throw new InvalidOperationException();
      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaNode aNewNode) => throw new InvalidOperationException();
      internal override CGaMorph AcceptNewMorph(CGaTransition aGaTransition, CGaShape aOldShape) => throw new InvalidOperationException();
      internal override string Name => "DragEdge";
      internal override bool HitTestEnabled => false;
      internal override void Paint(CVector2dPainter aOut)
      {
         base.Paint(aOut);
      }
   }
   internal sealed class CGaCursor : CGaShape
   {
      internal CGaCursor(CGaGraph aGaGraph):base(aGaGraph)
      {
         this.LineWidth = 1.0d;
      }
      internal CRectangle CursorRect { get => new CRectangle(this.CursorPos.x - this.CursorSize.x / 2.0d, this.CursorPos.y - this.CursorSize.y / 2.0d, this.CursorSize.x, this.CursorSize.y); }
      internal Color? Color = System.Drawing.Color.Black;
      internal CPoint CursorPos { get => this.GaAnimator.CursorPos; }
      internal override CRectangle Rect => this.CursorRect;
      internal override bool HasMorph => false;
      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaEdge aNewEdge) => throw new InvalidOperationException();
      internal override CGaMorph NewMorph(CGaTransition aGaTransition, CGaNode aNewNode) => throw new InvalidOperationException();
      internal override CGaMorph AcceptNewMorph(CGaTransition aGaTransition, CGaShape aOldShape) => throw new InvalidOperationException();
      internal override string Name => "Cursor";
      internal override bool ContainsPointExact(CPoint aPoint) => this.CursorRect.Contains(aPoint);
      internal override bool HitTestEnabled => false;
      internal bool IsVisible { get => true; }
      internal override void Paint(CVector2dPainter aOut)
      {
         if(this.IsVisible)
         {

            var aRect = this.Rect;
            var aCenter = aRect.CenterPoint;
            var aDx2 = aRect.Dx / 2.0d;
            var aDy2 = aRect.Dy / 2.0d;
            var aP1 = new CPoint(aCenter.x, aCenter.y - aDy2);
            var aP2 = new CPoint(aCenter.x + aDx2, aCenter.y);
            var aP3 = new CPoint(aCenter.x, aCenter.y + aDy2);
            var aP4 = new CPoint(aCenter.x - aDx2, aCenter.y);
            var aColor = this.Color;
            if (aColor.HasValue)
            {
               aOut.SetColor(aColor.Value);
               aOut.SetLineWidth(this.LineWidth);
               aOut.MoveTo(aP4);
               aOut.LineTo(aP2);
               aOut.Stroke();
               aOut.MoveTo(aP1);
               aOut.LineTo(aP3);
               aOut.Stroke();
            }
         }
      }
      internal readonly CPoint CursorSize = new CPoint(10, 10);

   }

   internal sealed class CGaGraph : IEnumerable<CGaShape> 
   {
      internal CGaGraph(CGaAnimator aGaAnimator, CPoint aSize, IEnumerable<CGaShape> aShapes)
      {
         this.Size = aSize;
         this.GaAnimator = aGaAnimator;
         this.Cursor = new CGaCursor(this);
         this.DragEdge = new CGaDragEdge(this);
         foreach (var aShape in aShapes)
         {
            this.ShapesDic.Add(aShape.Name, aShape);
         }
         this.ShapesDic.Add(this.DragEdge.Name, this.DragEdge);
         this.ShapesDic.Add(this.Cursor.Name, this.Cursor);         
      }

      internal readonly CGaCursor Cursor;
      internal readonly CGaDragEdge DragEdge;

      internal CGaGraph(CGaAnimator aGaAnimator, CGwGraph aGwGraph)
      {
         this.Size = aGwGraph.Size;
         this.GaAnimator = aGaAnimator;
         this.Cursor = new CGaCursor(this);

         foreach (var aGwNode in aGwGraph.Nodes)
         {
            var aGaNode = new CGaNode(this, aGwNode);
            this.ShapesDic.Add(aGaNode.Name, aGaNode);
         }
         foreach (var aGwEdge in aGwGraph.Edges)
         {
            var aGaEdge = new CGaEdge(this, 
                                      aGwEdge, 
                                      (CGaNode)this.ShapesDic[aGwEdge.Node1Name], 
                                      (CGaNode)this.ShapesDic[aGwEdge.Node2Name]);
            this.ShapesDic.Add(aGaEdge.Name, aGaEdge);
         }
         this.ShapesDic.Add(this.Cursor.Name, this.Cursor);
         foreach (var aShape in this.ShapesDic.Values)
            aShape.Init();
      }

      internal readonly CPoint Size;

      internal readonly CGaAnimator GaAnimator;
      internal CGaGraph(CGaAnimator aGaAnimator) :this(aGaAnimator, new CGwGraph())
      {
      }

      internal Dictionary<string, CGaShape> ShapesDic = new Dictionary<string, CGaShape>();
      public IEnumerator<CGaShape> GetEnumerator() => this.ShapesDic.Values.GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

      internal IEnumerable<CGaShape> GetShapes(CPoint aPoint)=>from aShape in this
                                                               where aShape.HitTestEnabled
                                                               where aShape.ContainsPoint(aPoint) select aShape;
   }

   internal struct CProgress
   {
      internal CProgress (double aPercent)
      {
         this.Percent = aPercent;
      }
      private readonly double Percent;

      internal double MorphDouble(double aOld, double aNew) => aOld + (aNew - aOld) * this.Percent;
      internal int MorphInt(int aOld, int aNew) => (int)this.MorphDouble(aOld, aNew);
      internal CPoint MorphPoint(CPoint aOld, CPoint aNew) => new CPoint(this.MorphDouble(aOld.x, aNew.x), this.MorphDouble(aOld.y, aNew.y));

      internal CPoint[] MorphPoints(CPoint[] aOld, CPoint[] aNew)
      {
         if (aOld.Length == aNew.Length)
         {
            var aThis = this;
            return (from aIdx in Enumerable.Range(0, aOld.Length) select aThis.MorphPoint(aOld[aIdx], aNew[aIdx])).ToArray();
         }
         else
         {
            throw new ArgumentException("CProgress.MorpPoints: Points.Length missmatch.");
         }
      }
      internal Color? MorphColor(Color? aOld, Color? aNew)
      {
         if (!aOld.HasValue
         || !aNew.HasValue)
            return default(Color?);
         else
            return Color.FromArgb(this.MorphInt(aOld.Value.A, aNew.Value.A),
                                  this.MorphInt(aOld.Value.R, aNew.Value.R),
                                  this.MorphInt(aOld.Value.G, aNew.Value.G),
                                  this.MorphInt(aOld.Value.B, aNew.Value.B)
                                  );
      }
   }

   internal abstract class CGaMorph
   {
      internal CGaMorph(CGaTransition aGaTransition)
      {
         this.GaTransition = aGaTransition;
         var aOldSize = this.GaTransition.GaState.OldGraph.Size;
         var aNewSize = this.GaTransition.GaState.NewGraph.Size;
         var aMaxSize = new CPoint(Math.Max(aOldSize.x, aNewSize.x),
                                   Math.Max(aOldSize.y, aNewSize.y));
         var aDiag = Math.Sqrt(Math.Pow(aMaxSize.x, 2) + Math.Pow(aMaxSize.y, 2));
         this.MaxDistance = aDiag;
      }
      internal readonly CGaTransition GaTransition;
      internal readonly double MaxDistance;

      private object MorphPercentM = (double)0.0d;
      internal double MorphPercent { get => (double)this.MorphPercentM; set => this.MorphPercentM = value; }
      internal CProgress MorphProgress { get => new CProgress(this.MorphPercent); }

      internal TimeSpan GetMoveDuration(CPoint aOldPoint, CPoint aNewPoint)
      {
         var aDelta = CPointUtil.GetDelta(aOldPoint, aNewPoint);
         var aDistance = Math.Sqrt(Math.Pow(aDelta.x, 2) + Math.Pow(aDelta.y, 2));
         var aMaxDistance = this.MaxDistance;
         var aMaxDuration = this.MaxDuration.TotalMilliseconds;
         var aDuration = aDistance / aMaxDistance * aMaxDuration;
         return new TimeSpan(0, 0, 0, 0, (int) aDuration);
      }

      internal TimeSpan GetMoveDuration(CPoint[] aOldPoints, CPoint[] aNewPoints)
      {         
         if(aOldPoints.Length != aNewPoints.Length)
         {
            throw new ArgumentException("Points.Length missmatch.");
         }
         else if(aOldPoints.IsEmpty())
         {
            throw new ArgumentException("Points.IsEmpty.");
         }
         else
         {
            return (from aIdx in Enumerable.Range(0, aOldPoints.Length) select this.GetMoveDuration(aOldPoints[aIdx], aNewPoints[aIdx])).Max();
         }
      }

      private readonly TimeSpan MaxDuration = new TimeSpan(0, 0, 0,0, 1500);
      internal abstract bool CalcAnnounce();
      internal abstract void Morph();      
      internal abstract CGaShape MorphedShape { get; }
      internal abstract TimeSpan Duration { get; }
      internal abstract CGaShape OldShape { get; }
      internal abstract CGaShape NewShape { get; }
      internal virtual void Animate(CGaAnnounceAnimation aAnnounceAnimation)
      {
         this.MorphedShape.Animate(aAnnounceAnimation);         
      }
      internal bool IsAppear;      
   }

   internal static class CPointUtil
   {
      internal static CPoint GetDelta(CPoint a1, CPoint a2) => 
                           new CPoint(Math.Max(a1.x, a2.x) - Math.Min(a1.x, a2.x),
                                      Math.Max(a1.y, a2.y) - Math.Min(a1.y, a2.y));

   }


   internal sealed class CGaNodeMorph : CGaMorph
   {
      internal CGaNodeMorph(CGaTransition aGaTransition, CGaNode aOldNode, CGaNode aNewNode):base(aGaTransition)
      {
         this.OldNode = aOldNode;
         this.NewNode = aNewNode;
         this.MorphedNode = aOldNode.CopyNode();
      }
      internal readonly CGaNode OldNode;
      internal readonly CGaNode NewNode;
      internal readonly CGaNode MorphedNode;
      internal override CGaShape OldShape => this.OldNode;
      internal override CGaShape NewShape => this.NewNode;
      internal override CGaShape MorphedShape => this.MorphedNode;
      internal override void Morph()
      {
         this.MorphedNode.CenterPos = this.MorphProgress.MorphPoint(this.OldNode.CenterPos, this.NewNode.CenterPos);
         this.MorphedNode.Color = this.MorphProgress.MorphColor(this.OldNode.Color, this.NewNode.Color);
         this.MorphedNode.FontColor = this.MorphProgress.MorphColor(this.OldNode.FontColor, this.NewNode.FontColor);
      }
      internal override TimeSpan Duration => this.GetMoveDuration(this.OldNode.CenterPos, this.NewNode.CenterPos);
      internal override bool CalcAnnounce() => this.GaTransition.GaState.GetAnnounce(this); //=> this.OldNode.Pos != this.NewNode.Pos;
      internal override void Animate(CGaAnnounceAnimation aAnnounceAnimation)
      {
         base.Animate(aAnnounceAnimation);
         this.MorphedNode.Animate(aAnnounceAnimation);         
      }
   }
   internal sealed class CGaEdgeMorph : CGaMorph
   {
      internal CGaEdgeMorph(CGaTransition aGaTransition, CGaEdge aOldEdge, CGaEdge aNewEdge) : base(aGaTransition)
      {
         this.OldEdge = aOldEdge;
         this.NewEdge = aNewEdge;
         this.MorphedEdge = aOldEdge.CopyEdge();
         var aOld1 = this.OldEdge.Splines;
         var aNew1 = this.NewEdge.Splines;
         CPoint[] aOld2;
         CPoint[] aNew2;
         if (aOld1.Length > aNew1.Length)
         {
            if(aNew1.Length == 0)
            {
               throw new NotImplementedException();
            }
            else
            {
               var aDiff = aOld1.Length - aNew1.Length;
               var aAdd = from aIdx in Enumerable.Range(0, aDiff) select aNew1.Last();
               aOld2 = aOld1;
               aNew2 = aNew1.Concat(aAdd).ToArray();
            }
         }
         else if (aNew1.Length > aOld1.Length)
         {
            var aDiff = aNew1.Length - aOld1.Length;
            var aAdd = from aIdx in Enumerable.Range(0, aDiff) select aOld1.Last();
            aOld2 = aOld1.Concat(aAdd).ToArray();
            aNew2 = aNew1;
         }
         else
         {
            aOld2 = aOld1;
            aNew2 = aNew1;
         }
         this.OldPoints = aOld2;
         this.NewPoints = aNew2;
      }
      internal readonly CGaEdge OldEdge;
      internal readonly CPoint[] OldPoints;
      internal readonly CGaEdge NewEdge;
      internal readonly CPoint[] NewPoints;
      internal readonly CGaEdge MorphedEdge;
      internal override CGaShape OldShape => this.OldEdge;
      internal override CGaShape NewShape => this.NewEdge;
      internal override CGaShape MorphedShape => this.MorphedEdge;
      internal override void Morph()
      {
         this.MorphedEdge.Splines = this.MorphProgress.MorphPoints(this.OldPoints, this.NewPoints);
         this.MorphedEdge.Color = this.MorphProgress.MorphColor(this.OldEdge.Color, this.NewEdge.Color);
      }
      internal override TimeSpan Duration => this.GetMoveDuration(this.OldPoints, this.NewPoints);
      internal override bool CalcAnnounce() => this.GaTransition.GaState.GetAnnounce(this); //  !this.OldPoints.SequenceEqual(this.NewPoints);
   }

   internal sealed class CGaTransition
   {
      internal CGaTransition(CGaState aGaState)
      {
         this.GaState = aGaState;
         var aSize = aGaState.Size;
         var aOldGraph = aGaState.OldGraph;
         var aNewGraph = aGaState.NewGraph;
         var aOldGraphMorphKeys = from aKvp in aOldGraph.ShapesDic
                                  where aKvp.Value.HasMorph
                                  select aKvp.Key;
         var aNewGraphMorphKeys = from aKvp in aNewGraph.ShapesDic
                                  where aKvp.Value.HasMorph
                                  select aKvp.Key;
         var aKeys = aOldGraphMorphKeys.Concat(aNewGraphMorphKeys); // aOldGraph.ShapesDic.Keys.Concat(aNewGraph.ShapesDic.Keys);
         var aMorphingKeys = from aKey in aKeys
                           where aOldGraph.ShapesDic.ContainsKey(aKey)
                           where aNewGraph.ShapesDic.ContainsKey(aKey)
                           select aKey;
         var aDisappearingKeys = from aKey in aKeys
                                 where aOldGraph.ShapesDic.ContainsKey(aKey)
                                 where !aNewGraph.ShapesDic.ContainsKey(aKey)
                                 select aKey
                                 ;
         var aAppearingKeys = from aKey in aKeys
                              where !aOldGraph.ShapesDic.ContainsKey(aKey)
                              where aNewGraph.ShapesDic.ContainsKey(aKey)
                              select aKey
                                 ;
         var aMorphings = new Dictionary<string, CGaMorph>();
         foreach (var aKey in aMorphingKeys)
         {
            if(!aMorphings.ContainsKey(aKey))
            {
               var aOldShape = aOldGraph.ShapesDic[aKey];
               var aNewShape = aNewGraph.ShapesDic[aKey];
               var aMorph = aNewShape.AcceptNewMorph(this, aOldShape);
               aMorphings[aKey] = aMorph;
            }
         }
         var aMorphDuration = aMorphings.IsEmpty() ? default(TimeSpan) : (from aMorph in aMorphings.Values select aMorph.Duration).Max();


         var aDisappearings = new List<CGaShape>();
         foreach (var aKey in aDisappearingKeys)
         {
            aDisappearings.Add(aOldGraph.ShapesDic[aKey]);
         }
         var aAppearings = new List<CGaShape>();
         foreach (var aKey in aAppearingKeys)
         {
            aAppearings.Add(aNewGraph.ShapesDic[aKey]);
         }
         foreach(var aMorph in aMorphings.Values)
         {
            aMorph.IsAppear = aAppearings.Contains(aMorph.NewShape);
         }
         var aMorphShapes1 = (from aMorph in aMorphings.Values select aMorph.MorphedShape);
         var aMorphShapes2 = (aDisappearings.Concat(aAppearings).Concat(aMorphShapes1));
         var aMorpShapes = (from aGroup in aMorphShapes2.GroupBy(aShape => aShape) select aGroup.Key).ToArray();
         var aMorphGraph = new CGaGraph(this.GaState.GaAnimator, aSize, aMorpShapes);
         foreach (var aAppearing in aAppearings)
         {
            aAppearing.AnimateAppear(0.0d);
         }
         foreach(var aDisappearing in aDisappearings)
         {
            aDisappearing.AnimateDisappear(0.0d);
         }
         this.OldGraph = aOldGraph;
         this.NewGraph = aNewGraph;
         this.MorphGraph = aMorphGraph;
         this.Morphings = aMorphings;
         this.Disappearings = aDisappearings; 
         this.Appearings = aAppearings;
         this.MorphDuration = aMorphDuration;

         // Nachdem alle daten verfügbar sind:
         var aAnnouncers = (from aMorph in aMorphings.Values
                            where aMorph.CalcAnnounce()
                            select aMorph).ToArray();
         this.Announcers = aAnnouncers;
      }

      internal readonly CGaState GaState;
      internal readonly CGaGraph OldGraph; 
      internal readonly CGaGraph NewGraph;
      internal readonly CGaGraph MorphGraph;
      internal readonly Dictionary<string, CGaMorph> Morphings;
      internal readonly List<CGaShape> Disappearings;
      internal readonly List<CGaShape> Appearings;
      internal readonly TimeSpan MorphDuration;
      internal readonly CGaMorph[] Announcers;

      public IEnumerable<CGaShape> AllShapes 
      { 
         get
         {
            return this.OldGraph.ShapesDic.Values.Concat(this.NewGraph.ShapesDic.Values).Concat(from aMorph in this.Morphings.Values select aMorph.MorphedShape);
         }
      }
   }

   internal abstract class CGaWorkerResult
   {
      internal CGaWorkerResult(BackgroundWorker aBackgroundWorker)
      {
         this.BackgroundWorker = aBackgroundWorker;
      }
      internal readonly BackgroundWorker BackgroundWorker;
      internal abstract void ReceiveResult();
   }

   internal abstract class CGaNewStateWorkerResult : CGaWorkerResult
   {
      internal CGaNewStateWorkerResult(BackgroundWorker aBackgroundWorker, CGaState aNewState):base(aBackgroundWorker)
      {
         this.NewState = aNewState;
      }
      internal readonly CGaState NewState;
      internal override void ReceiveResult()
      {
         this.NewState.GaAnimator.State = this.NewState;
         this.NewState.GaAnimator.GraphException = default;
      }
   }

   internal abstract class CGaWorkerArgs
   {
      internal CGaWorkerArgs(CGaState aOldState)
      {
         this.OldState = aOldState;
      }

      internal readonly CGaState OldState;  
      internal abstract CGaWorkerResult NewWorkerResult(BackgroundWorker aWorker);
      internal virtual CGaWorkerResult NewWorkerResult(BackgroundWorker aWorker, CGaAnimator aGaAnimator, Exception aExc) => new CGaExceptionWorkerResult(aWorker, aGaAnimator, aExc);
   }

   internal sealed class CGaDefaultWorkerResult : CGaNewStateWorkerResult
   {
      internal CGaDefaultWorkerResult(BackgroundWorker aBackgroundWorker, CGaState aNewState):base(aBackgroundWorker, aNewState)
      {
      }
   }

   internal sealed class CGaExceptionWorkerResult : CGaWorkerResult
   {
      internal CGaExceptionWorkerResult(BackgroundWorker aWorker, CGaAnimator aAnimator, Exception aExc) : base(aWorker)
      {
         this.GaAnimator = aAnimator;
         this.Exception = aExc;
      }
      internal readonly CGaAnimator GaAnimator;
      internal readonly Exception Exception;
      internal override void ReceiveResult()
      {
         this.GaAnimator.GraphException = this.Exception;
      }
   }

   public sealed class CGaAnimator
   {
      internal CGaAnimator(Action<Exception> aOnExc,
                             Action aNotifyResult,
                             Action aNotifyPaint,
                             Func<CGaAnimator, CGaState> aNewState,
                             Action<string> aDebugPrint)
      {
         this.ExternDebugPrint = aDebugPrint;
         this.OnExc = aOnExc;
         this.State = aNewState(this); // new CGaState(this, new CGaGraph(this)); 
         this.AnimationThread = new System.Threading.Thread(RunAnimationThread);         
         this.NotifyResult = aNotifyResult;
         this.NotifyPaint = aNotifyPaint;

         this.AnimationThread.Start();
         this.AnimationThreadStartedEvent.WaitOne();
      }

      #region Test
      internal sealed class CTestState :  CGaState
      {
         internal CTestState(CGaAnimator aAnimator) : base(aAnimator)
         {
            this.GwGraphM = new Tuple<Exception, CGwGraph>(default, new CGwGraph());
            this.Init();
         }

         internal CTestState(CGaAnimator aAnimator, CTestState aOldState, CGwGraph aGwGraph) : base(aAnimator, aOldState)
         {
            this.GwGraphM = new Tuple<Exception, CGwGraph>(default, aGwGraph);
            this.Init();

           
         }

         private Tuple<Exception, CGwGraph> GwGraphM;
         internal override Tuple<Exception, CGwGraph> GwGraph { get => this.GwGraphM; }
      }
      internal sealed class CGaTestWorkerArgs : CGaWorkerArgs
      {
         internal CGaTestWorkerArgs(int aTestCaseNr, Action<string> aDebugPrint, CTestState aOldState):base(aOldState)
         {
            this.OldTestState = aOldState;            
            this.TestCaseNr = aTestCaseNr;
            this.DebugPrint = aDebugPrint;
            this.NewTestState = new CTestState(aOldState.GaAnimator, aOldState, this.NewGwGraph());
         }

         private readonly CTestState OldTestState;
         private readonly CTestState NewTestState;
         private readonly int TestCaseNr;
         private readonly Action<string> DebugPrint;
         private CGwGraph NewGwGraph()
         {
            switch (this.TestCaseNr % 5)
            {
               case 0:
                  return CFlowMatrix.NewTestFlowMatrix1(this.DebugPrint).Channels.GwDiagramBuilder.GwGraph.Item2;
               case 1:
                  return CFlowMatrix.NewTestFlowMatrix2(this.DebugPrint).Channels.GwDiagramBuilder.GwGraph.Item2;
               case 2:
                  return CFlowMatrix.NewTestFlowMatrix3(this.DebugPrint).Channels.GwDiagramBuilder.GwGraph.Item2;
               case 3:
                  return CFlowMatrix.NewTestFlowMatrix4(this.DebugPrint).Channels.GwDiagramBuilder.GwGraph.Item2;
               default:
                  return CFlowMatrix.NewTestFlowMatrix5(this.DebugPrint).Channels.GwDiagramBuilder.GwGraph.Item2;
            }
         }
         internal override CGaWorkerResult NewWorkerResult(BackgroundWorker aBackgroundWorker) => new CGaDefaultWorkerResult(aBackgroundWorker, this.NewTestState);
      }


      public static void Test(Action<string> aFailAction, Action<string> aDebugPrint)
      {
         var aDispatcherFrame = default(DispatcherFrame);
         var aDispatcher = default(Dispatcher);
         var aBackgroundWorker = new BackgroundWorker();
         var aBackgroundWorkerReady = new AutoResetEvent(false);
         aBackgroundWorker.DoWork += new DoWorkEventHandler(delegate (object aSender, DoWorkEventArgs aArgs)
         {
            aDispatcher = Dispatcher.CurrentDispatcher;
            aDispatcherFrame = new DispatcherFrame();
            aBackgroundWorkerReady.Set();
            Dispatcher.PushFrame(aDispatcherFrame);
         });
         aBackgroundWorker.RunWorkerAsync();
         aBackgroundWorkerReady.WaitOne();
         var aOnExc = new Action<Exception>(delegate (Exception aExc) { aDebugPrint(aExc.ToString()); });
         var aGaAnimator = default(CGaAnimator);
         var aNotifyResult = new Action(delegate () { aDispatcher.BeginInvoke(new Action(delegate () 
         {
            aGaAnimator.ProcessNewGraph();
            var aShapes = aGaAnimator.State.GaTransition.MorphGraph.GetShapes(new CPoint(90, 41));
            if(!aShapes.IsEmpty())
            {

            }
         }));});
         var aNotifyPaint = new Action(delegate () { aDispatcher.BeginInvoke(new Action(delegate () { aGaAnimator.OnPaintDone(); })); });
         var aTestState = default(CTestState);
         aGaAnimator = new CGaAnimator(aOnExc, 
                                       aNotifyResult, 
                                       aNotifyPaint, 
                                       aAnimator=> { aTestState = new CTestState(aAnimator); return aTestState; }, 
                                       aDebugPrint);
         var aDone = false;
         
         do
         {
            System.Console.WriteLine("CGaAnimatorTest: press nr or anything else to exit.");
            var aKey = System.Console.ReadKey();
            var aKeyText = aKey.KeyChar.ToString();
            int aTestCaseNr = 0;
            if (int.TryParse(aKeyText, out aTestCaseNr))
            {
               aGaAnimator.NextGraph(new CGaTestWorkerArgs(aTestCaseNr, aDebugPrint, aTestState));
            }
            else
            {
               aDone = true;
            }
         }
         while (!aDone);
         aGaAnimator.Shutdown();
         aDispatcherFrame.Continue = false;
      }
      #endregion

      internal CPoint? DragEdgeP1;
      internal CPoint? DragEdgeP2;
      internal volatile bool DragEdgeVisible;
      internal CPoint CursorPos;

      internal Action NotifyEndAnimation = new Action(delegate () { });

      private Action<string> ExternDebugPrint;
      internal void DebugPrint(string aMsg) => this.ExternDebugPrint(aMsg); // System.Diagnostics.Debug.Print(aMsg);
      internal void DebugPrint(IEnumerable<CPoint> aPoints) => this.DebugPrint((from aPoint in aPoints select aPoint.x.ToString() + ", " + aPoint.y.ToString()).JoinString(" "));
      private Action NotifyResult;
      private Action NotifyPaint;
      private Action<Exception> OnExc;
      private BackgroundWorker WorkerNullable;
      internal volatile bool PaintIsPending;      
      internal void OnPaintDone()
      {
         this.PaintIsPending = false;
         this.RunAnimationStep();
      }

      internal volatile CGaState State;

      private void CancelWorkerOnDemand()
      {
         var aWorker = this.WorkerNullable;
         if(aWorker is object)
         {
            this.WorkerNullable = default;
            this.RemoveWorkerCallbacks(aWorker);
         }
      }
      private bool IsCurrentWorker(BackgroundWorker aWorker)
      {
         return object.ReferenceEquals(this.WorkerNullable, aWorker);
      }

      private void StartWorker(CGaWorkerArgs aGaWorkerArgs)
      {
         this.CancelWorkerOnDemand();
         var aWorker = new BackgroundWorker();
         this.WorkerNullable = aWorker;
         this.AddWorkerCallbacks(aWorker);         
         this.State.WorkingAnimation.Start();
         aWorker.RunWorkerAsync(aGaWorkerArgs);
      }

      private void AddWorkerCallbacks(BackgroundWorker aWorker)
      {
         aWorker.DoWork += this.BackgroundWorkerDoWork;
         aWorker.RunWorkerCompleted += this.BackgroundWorkerRunWorkerCompleted;
      }
      private void RemoveWorkerCallbacks(BackgroundWorker aWorker)
      {
         try
         {
            aWorker.DoWork -= this.BackgroundWorkerDoWork;
            aWorker.RunWorkerCompleted -= this.BackgroundWorkerRunWorkerCompleted;
         }
         catch(Exception aExc)
         {
            this.OnExc(aExc);
         }
      }

      internal void NextGraph(CGaWorkerArgs aGaWorkerArgs)
      {
         this.StartWorker(aGaWorkerArgs);
      }

      private void BackgroundWorkerDoWork(object aSender, DoWorkEventArgs aArgs)
      {
         System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Normal;
         //System.Threading.Thread.Sleep(3000);
         var aBackgroundWorker = (BackgroundWorker)aSender;
         var aGaWorkerArgs = (CGaWorkerArgs)aArgs.Argument;
         CGaWorkerResult aWorkerResult;
         try
         {
            aWorkerResult = aGaWorkerArgs.NewWorkerResult(aBackgroundWorker);
         }
         catch(Exception aExc)
         {
            aWorkerResult = aGaWorkerArgs.NewWorkerResult(aBackgroundWorker, this, aExc);
         }
         aArgs.Result = aWorkerResult;
      }

      private void BackgroundWorkerRunWorkerCompleted(object aSender, RunWorkerCompletedEventArgs aArgs)
      {
         CGaWorkerResult aGaWorkerResult;
         var aWorker = (BackgroundWorker)aSender;
         if(aArgs.Error is object)
         {
            this.OnExc(new Exception("Error calculating GraphMorph.NewGraph. " + aArgs.Error.Message, aArgs.Error));
            aGaWorkerResult = default;
         }
         else if(aArgs.Cancelled)
         {
            // nix.
            aGaWorkerResult = default;
         }
         else if(aArgs.Result is object)
         {
            aGaWorkerResult = (CGaWorkerResult)aArgs.Result;               
         }
         else
         {
            //this.OnExc(new Exception("No result when calculating GraphMorph.NewGraph."));
            aGaWorkerResult = default;
         }
         if(aGaWorkerResult is object)
         {
            this.AddWorkerResult(aGaWorkerResult);
         }
      }
      private void AddWorkerResult(CGaWorkerResult aWorkerResult)
      {
         lock (this.WorkerResults)
         {
            this.WorkerResults.Add(aWorkerResult);
            this.NotifyResult();
         }
      }


      private readonly List<CGaWorkerResult> WorkerResults = new List<CGaWorkerResult>();
      private CGaWorkerResult PeekWorkerResultNullable()
      {
         lock (this.WorkerResults)
         {
            if(!this.WorkerResults.IsEmpty())
            {
               var aWorkerResult = this.WorkerResults.Last();
               this.WorkerResults.Clear();
               return aWorkerResult;
            }
         }
         return default;
      }
      internal void ProcessNewGraph()
      {
         var aResult = this.PeekWorkerResultNullable();
         if(aResult is object
         && this.IsCurrentWorker(aResult.BackgroundWorker))
         {
            this.WorkerNullable = default;
            this.RemoveWorkerCallbacks(aResult.BackgroundWorker);
            aResult.ReceiveResult();
         }
      }

      private readonly System.Threading.Thread AnimationThread;

      internal void Shutdown()
      {
         this.CancelWorkerOnDemand();
         this.AnimationThreadDispatcherFrame.Continue = false;
         this.AnimationThread.Join();
      }

      internal void OnAnimationStarted()
      {
         this.RunAnimationStep();
      }

      internal CPoint Size => this.State.Size;

      public bool NextGraphIsPending { get => this.WorkerNullable is object; }

      private Dispatcher AnimationThreadDispatcher;
      private AutoResetEvent AnimationThreadStartedEvent = new AutoResetEvent(false);
      private Stopwatch AnimationThreadStopWatch = new Stopwatch();

      private void InvokeInAnimationThread(Action aAction)
      {
         this.AnimationThreadDispatcher.BeginInvoke(new Action(delegate ()
         {
            try
            {
               aAction();
            }
            catch(Exception aExc)
            {
               this.OnExc(aExc); // new Exception("Exception in AnimationThread. " + aExc.ToString(), aExc));
            }
         }));
      }

      private void RunAnimationStep()
      {
         this.InvokeInAnimationThread(delegate ()
         {
            var aStopWatch = this.AnimationThreadStopWatch;
            var aElapsed = aStopWatch.ElapsedMilliseconds;            
            aStopWatch.Start();
            if (this.Animate(aElapsed))
            {               
               this.Paint();
            }
            else
            {
               aStopWatch.Reset();
            }
         });
      }

      private DispatcherFrame AnimationThreadDispatcherFrame;
      internal Exception GraphException;

      private void RunAnimationThread(object aObj)
      {
         System.Threading.Thread.CurrentThread.Priority = ThreadPriority.Normal;
         this.AnimationThreadDispatcher = Dispatcher.CurrentDispatcher;
         this.AnimationThreadDispatcherFrame = new DispatcherFrame();
         this.AnimationThreadStartedEvent.Set();         
         Dispatcher.PushFrame(this.AnimationThreadDispatcherFrame);        
      }
       
      private bool Animate(long aElapsedMilliseconds)
      {
         var aBusy = false;
         var aState = this.State;
         foreach(var aAnimation in aState.RunningAnimations)
         {            
            if(aAnimation.Animate(aElapsedMilliseconds))
            {
               aBusy = true;
            }           
         }
         return aBusy;
      }

      internal void Paint()
      {
         this.PaintIsPending = true;
         this.NotifyPaint();
      }

      private void Paint(CVector2dPainter aOut , Exception aExc)
      {
         aOut.SetColor(Color.Red);
         var aRect = new CRectangle(new CPoint(), this.Size);
         aOut.Text(aExc.Message, aRect);
      }

      internal void Paint(CVector2dPainter aOut)
      {
         var aExc = this.GraphException;
         if (aExc is object)
         {
            // actually this wont be visible since we have no size.
            // shall not occur atm.
            this.Paint(aOut, aExc);
         }
         else if(this.State.NewGraphWithExc.Item1 is object)
         {
            this.Paint(aOut, this.State.NewGraphWithExc.Item1);
         }
         else
         {
            aOut.SetLineWidth(2.0d);
            foreach (var aShape in this.State.GaTransition.MorphGraph)
            {
               aShape.Paint(aOut);
            }
         }
      }
   }

   internal abstract class CGaAnimation
   {
      internal CGaAnimation(CGaState aState)
      {
         this.State = aState;
      }
      internal readonly CGaState State;

      internal bool IsRunning { get; private set; }

      internal virtual void OnStart()
      {
      }

      internal void Start()
      {
         //this.State.GaAnimator.DebugPrint(this.GetType().Name + ".Start");
         this.FrameLen = 0;
         this.Stopwatch.Start();
         this.IsRunning = true;
         this.OnStart();
         this.State.GaAnimator.OnAnimationStarted();
      }

      internal void Stop()
      {
         this.IsRunning = false;
      }

      internal void Finish()
      {
         //  this.Animate(0);
         this.Stop();
         this.OnFinish();
      }

      internal virtual void OnFinish()
      {
      }

      internal bool RepaintIsPending { get => this.State.GaAnimator.PaintIsPending; }
      private Stopwatch Stopwatch = new Stopwatch();

      internal void Paint()
      {
         this.State.GaAnimator.Paint();
      }

      private long FrameLen;

      internal virtual long? MaxDuration { get => default(long?); }
      internal const long MaxDurationDefault = 1000;
      internal virtual void OnAnimate(long aFrameLen)
      {
      }

      internal virtual long TotalElapsed
      {
         get
         {
            var aMaxDuration = this.MaxDuration;
            var aElapsed = this.Stopwatch.ElapsedMilliseconds;
            return aElapsed;
         }
      }
      internal double PercentLin { get => this.MaxDuration.Value == 0 ? 1.0d : ((double)Math.Min(this.MaxDuration.Value, this.TotalElapsed)) / ((double)this.MaxDuration.Value); }
      internal double PercentExp { get => 1.0d - Math.Pow((1.0d - this.PercentLin) * 10, 2) / 100.0d; }
      internal double Percent { get => this.PercentExp; }
      internal CProgress Progress { get => new CProgress(this.Percent); }

      internal bool Animate(long aElapsed)
      {
         var aPaint = false;
         this.FrameLen += aElapsed;
         if (!this.RepaintIsPending)
         {
            var aMaxDuration = this.MaxDuration;
            var aTotalElapsed = this.TotalElapsed;
            bool aDone;
            long aFrameLen2;
            if (aMaxDuration.HasValue
            && aTotalElapsed > aMaxDuration)
            {
               aFrameLen2 = aTotalElapsed - aMaxDuration.Value;
               aDone = true;
            }
            else
            {
               aFrameLen2 = this.FrameLen;
               aDone = false;
            }
            this.OnAnimate(aFrameLen2);
            this.FrameLen = 0;
            aPaint = true;

            if (aDone)
            {
               this.Finish();
               aPaint = true;
            }
         }
         return aPaint;
      }
   }

   internal sealed class CGaWorkingAnimation : CGaAnimation
   {
      internal CGaWorkingAnimation(CGaState aState) : base(aState)
      {
      }

      private readonly long Intervall = 250;

      internal override void OnAnimate(long aFrameLen)
      {
         base.OnAnimate(aFrameLen);
         //this.Wobble(0);
         if (this.TotalElapsed >= this.Intervall
         && !object.ReferenceEquals(this.State, this.State.GaAnimator.State))
         {
            this.Finish();
            this.State.GaAnimator.State.AnnounceAnimation.Start();
         }
      }
   }

   internal sealed class CGaAnnounceAnimation : CGaAnimation
   {
      internal CGaAnnounceAnimation(CGaState aState) : base(aState)
      {
      }

      private double CalcWobble()
      {
         var aTime = this.TotalElapsed;
         var aIntervall = this.Intervall;
         var aCycle = (aTime % aIntervall) / 1000.0d;
         var a1 = (aCycle * Math.PI * 2) + Math.PI / 2.0d;
         var aWobble1 = Math.Sin(a1);
         return aWobble1;

      }

      private double CalcWobble(double aRange)
      {
         var aWobble1 = this.CalcWobble();
         var aWobble2 = (aWobble1 * aRange) + 1.0d;
         return aWobble2;
      }

      private readonly long Intervall = 250;

      internal double ScaleWobble { get => this.IsRunning ? this.CalcWobble(0.075) : 1.0d; }
      internal CProgress ColorWobble { get => new CProgress(this.IsRunning ? this.CalcWobble() : 0.0d); }

      internal override void OnAnimate(long aFrameLen)
      {
         base.OnAnimate(aFrameLen);
         foreach (var aMorph in this.State.GaTransition.Announcers)
         {
            aMorph.Animate(this);
         }
         foreach (var aDisappearing in this.State.GaTransition.Disappearings)
         {
            aDisappearing.Animate(this);
         }
         if (this.TotalElapsed >= this.Intervall)
         {
            this.Finish();
            this.State.GaAnimator.State.DisappearAnimation.Start();
         }
      }

      internal override void OnFinish()
      {        
         base.OnFinish();
         foreach (var aMorph in this.State.GaTransition.Announcers)
         {
            aMorph.Animate(this);
         }
      }
   }

   internal sealed class CGaDisappearAnimation : CGaAnimation
   {
      internal CGaDisappearAnimation(CGaState aState) : base(aState)
      {
      }

      internal override long? MaxDuration => 333;
      internal override void OnAnimate(long aFrameLen)
      {
         var aShapes = this.State.GaTransition.Disappearings;
         if (aShapes.IsEmpty())
         {
            this.Finish();
         }
         else
         {
            var aPercent = this.Percent;
            foreach (var aShape in aShapes)
            {
               aShape.AnimateDisappear(aPercent);
            }
         }
      }
      internal override void OnFinish()
      {
         base.OnFinish();

         this.State.MoveAnimation.Start();
      }
   }

   internal sealed class CGaMoveAnimation : CGaAnimation
   {
      internal CGaMoveAnimation(CGaState aState) : base(aState)
      {
      }


      internal override long? MaxDuration => (long)this.State.GaTransition.MorphDuration.TotalMilliseconds;
      internal override void OnAnimate(long aElapsedMilliseconds)
      {
         var aProgress = this.Percent;
         foreach (var aMatrixMorph in this.State.GaTransition.Morphings.Values)
         {
            aMatrixMorph.MorphPercent = aProgress;
            aMatrixMorph.Morph();
         }
      }
      internal override void OnFinish()
      {
         base.OnFinish();

         this.State.AppearAnimation.Start();
      }
   }

   internal sealed class CGaAppearAnimation : CGaAnimation
   {
      internal CGaAppearAnimation(CGaState aState) : base(aState)
      {
      }

      internal override long? MaxDuration => 333;

      internal override void OnAnimate(long aFrameLen)
      {
         var aShapes = this.State.GaTransition.Appearings;
         if (aShapes.IsEmpty())
         {
            this.Finish();
         }
         else
         {
            var aPercent = this.Percent;
            foreach (var aShape in aShapes)
            {
               aShape.AnimateAppear(aPercent);
            }
         }
      }

      internal override void OnFinish()
      {
         base.OnFinish();
         foreach (var aShape in this.State.GaTransition.AllShapes)
         {
            aShape.Announcing = false;
         }
         this.State.GaAnimator.NotifyEndAnimation();
      }
   }
   internal abstract class CGaState
   {
      internal CGaState(CGaAnimator aGaAnimator)
      {
         this.GaAnimator = aGaAnimator;         
         this.WorkingAnimation = new CGaWorkingAnimation(this);
         this.AnnounceAnimation = new CGaAnnounceAnimation(this);
         this.DisappearAnimation = new CGaDisappearAnimation(this);
         this.MoveAnimation = new CGaMoveAnimation(this);
         this.AppearAnimation = new CGaAppearAnimation(this);
      }

      internal CGaState(CGaAnimator aGaAnimator, CGaState aOldState)
      {
         this.OldStateNullable = aOldState;
         this.GaAnimator = aGaAnimator;
         this.OldGraph = aOldState.NewGraph;
         this.WorkingAnimation = new CGaWorkingAnimation(this);
         this.AnnounceAnimation = new CGaAnnounceAnimation(this);
         this.DisappearAnimation = new CGaDisappearAnimation(this);
         this.MoveAnimation = new CGaMoveAnimation(this);
         this.AppearAnimation = new CGaAppearAnimation(this);         
      }

      protected virtual void Init()
      {
         this.GaTransition = new CGaTransition(this);
      }

      internal readonly CGaState OldStateNullable;
      internal readonly CGaAnimator GaAnimator;
      private CGaGraph OldGraphM;
      internal CGaGraph OldGraph 
      { 
         get => CLazyLoad.Get(ref this.OldGraphM, () => this.NewGraph);
         private set
         {
            if (this.OldGraphM is object)
               throw new InvalidOperationException();
            this.OldGraphM = value;
         }
      }


      internal CGaTransition GaTransition;
      internal readonly CGaWorkingAnimation WorkingAnimation;
      internal readonly CGaAnnounceAnimation AnnounceAnimation;
      internal readonly CGaDisappearAnimation DisappearAnimation;
      internal readonly CGaMoveAnimation MoveAnimation;
      internal readonly CGaAppearAnimation AppearAnimation;
      internal virtual bool GetAnnounce(CGaNodeMorph aNodeMorph)
      {
         var aNode = aNodeMorph.MorphedNode;
         var aNewNode = aNodeMorph.NewNode;
         var aAppearingEdges = aNodeMorph.GaTransition.Appearings.OfType<CGaEdge>();
         var aRelatedAppearingEdges = from aEdge in aAppearingEdges
                                      where object.ReferenceEquals(aEdge.GaNode1, aNewNode)
                                         || object.ReferenceEquals(aEdge.GaNode2, aNewNode)
                                      select aEdge
                                      ;
         var aOldNode = aNodeMorph.OldNode;
         var aDisappearingEdges = aNodeMorph.GaTransition.Disappearings.OfType<CGaEdge>();
         var aRelatedDisappearingEdges = from aEdge in aDisappearingEdges
                                         where object.ReferenceEquals(aEdge.GaNode1, aOldNode)
                                            || object.ReferenceEquals(aEdge.GaNode2, aOldNode)
                                         select aEdge
                                         ;
         var aIsAnnounce = !aRelatedAppearingEdges.IsEmpty()
                        || !aRelatedDisappearingEdges.IsEmpty()
                        ;
         return aIsAnnounce;
      }

      internal virtual bool GetAnnounce(CGaEdgeMorph aEdgeMorph)
      {
         var aOldEdge = aEdgeMorph.OldEdge;
         var aDisappearingEdges = aEdgeMorph.GaTransition.Disappearings.OfType<CGaEdge>();
         var aIsAnnounce = aDisappearingEdges.Contains(aOldEdge);
         return aIsAnnounce;
      }

      internal virtual bool GetIsFocused(CGaShape aShape) => false;

      private Tuple<Exception, CGaGraph> NewGaGraph()
      {
         try
         {
            var aGwGraphWithExc = this.GwGraph;
            var aGwGraph = aGwGraphWithExc.Item2;
            var aGaGraph = new CGaGraph(this.GaAnimator, aGwGraph);
            var aResult = new Tuple<Exception, CGaGraph>(aGwGraphWithExc.Item1, aGaGraph);
            return aResult;
         }
         catch(Exception aExc)
         {
            var aGaGraph = new CGaGraph(this.GaAnimator);
            var aResult = new Tuple<Exception, CGaGraph>(aExc, aGaGraph);
            return aResult;
         }
      }
      internal abstract Tuple<Exception, CGwGraph> GwGraph { get; }
      private Tuple<Exception, CGaGraph> NewGraphWithExcM;
      internal Tuple<Exception, CGaGraph> NewGraphWithExc { get => CLazyLoad.Get(ref this.NewGraphWithExcM, () => this.NewGaGraph()); }
      internal CGaGraph NewGraph { get => this.NewGraphWithExc.Item2; }
      internal IEnumerable<CGaAnimation> RunningAnimations
      {
         get
         {
            if (this.OldStateNullable is object
            && this.OldStateNullable.WorkingAnimation.IsRunning)
               yield return this.OldStateNullable.WorkingAnimation;
            else if (this.WorkingAnimation.IsRunning)
               yield return this.WorkingAnimation;
            if (this.AnnounceAnimation.IsRunning)
               yield return this.AnnounceAnimation;
            if (this.DisappearAnimation.IsRunning)
               yield return this.DisappearAnimation;
            if (this.MoveAnimation.IsRunning)
               yield return this.MoveAnimation;
            if (this.AppearAnimation.IsRunning)
               yield return this.AppearAnimation;
         }
      }

      internal CPoint Size
      {
         get => new CPoint(Math.Max(this.OldGraph.Size.x, this.NewGraph.Size.x),
                           Math.Max(this.OldGraph.Size.y, this.NewGraph.Size.y));
      }
   }
}
