using CbChannelStrip.Graph;
using CbChannelStrip.GaAnimator;
using CbMaxClrAdapter;
using CbMaxClrAdapter.Jitter;
using CbMaxClrAdapter.MGraphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace CbChannelStrip.GraphWiz
{
   using CPoint = Tuple<double, double>;

   internal sealed class CGwEdge
   {
      internal CGwEdge(string aNode1Name, string aNode2Name, IEnumerable<CPoint> aSplines, Color? aColor)
      {
         this.Node1Name = aNode1Name;
         this.Node2Name = aNode2Name;
         this.Name = aNode1Name + " -> " + aNode2Name;
         this.Splines = aSplines;
         this.Color = aColor;
      }
      internal readonly string Name;
      internal readonly string Node1Name;
      internal readonly string Node2Name;
      internal readonly IEnumerable<CPoint> Splines;
      internal readonly Color? Color;

      internal IEnumerable<Tuple<CPoint, CPoint, CPoint>> NewBezierCurves()
      {
         throw new NotImplementedException();
      }

   }

   internal sealed class CGwNode
   {
      internal CGwNode(string aName, double aX, double aY, double aDx, double aDy, string aShape)
      {
         this.Name = aName;
         this.X = aX;
         this.Y = aY;
         this.Dx = aDx;
         this.Dy = aDy;
         this.ShapeEnum = (CShapeEnum)Enum.Parse(typeof(CShapeEnum), aShape, true);
      }
      internal readonly string Name;
      internal readonly double X;
      internal readonly double Y;
      internal readonly double Dx;
      internal readonly double Dy;
      internal enum CShapeEnum
      {
         Triangle,
         InvTriangle,
         MCircle,
         MSquare
      }
      internal readonly CShapeEnum ShapeEnum;
      internal Color? Color;
      internal Color? FontColor;

      internal double CenterX
      {
         get => this.X + this.Dx / 2.0d;
      }
      internal double CenterY
      {
         get => this.Y + this.Dy / 2.0d;
      }
   }

   internal sealed class CCaseInsenstiveComparer : IEqualityComparer<string>
   {
      bool IEqualityComparer<string>.Equals(string x, string y)
         => x.ToLower() == y.ToLower();
      int IEqualityComparer<string>.GetHashCode(string obj) => obj.ToLower().GetHashCode();
   }

   internal sealed class CGwGraph
   {
      internal CGwGraph(IEnumerable<CGwNode> aNodes, IEnumerable<CGwEdge> aEdges, CPoint aSize)
      {
         this.Nodes = aNodes;
         this.Edges = aEdges;
         this.Size = aSize;
      }

      internal CGwGraph() : this(new CGwNode[] { }, new CGwEdge[]{}, new CPoint(0.0d, 0.0d)) { }
      internal readonly CPoint Size;

      private static void AddColor1(Dictionary<string, Color> aDic, string aName, Color aColor)
      {
         if (!aDic.ContainsKey(aName))
            aDic.Add(aName, aColor);
      }
      private static void AddColor(Dictionary<string, Color> aDic, string aName, Color aColor)
      {
         AddColor1(aDic, aName, aColor);
         if (aName.Contains("Gray"))
            AddColor1(aDic, aName.Replace("Gray", "Grey"), aColor);
      }

      private static Dictionary<string, Color> NewColorDic()
      {
         var aDic = new Dictionary<string, Color>(0, new CCaseInsenstiveComparer());
         var aProperties1 = typeof(Color).GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
         var aProperties2 = from aField in aProperties1 where aField.PropertyType.Equals(typeof(Color)) select aField;
         var aProperties = aProperties2;
         foreach(var aProperty in aProperties2)
         {
            var aColor = (Color)aProperty.GetValue(null);
            AddColor(aDic, aProperty.Name, aColor);
         }
         return aDic;
      }

      private static Dictionary<string, Color> ColorDic = NewColorDic();

      private static Color? GetColor(Dictionary<string, string> aAttributes, string aAttributeName)
      {
         if (aAttributes.ContainsKey(aAttributeName))
         {
            var aColorName = aAttributes[aAttributeName];
            if(ColorDic.ContainsKey(aColorName))
            {
               return ColorDic[aColorName];
            }
            else
            {
               return default(Color?);
            }
         }
         else
         {
            return default(Color?);
         }
      }
      internal static CGwGraph New(string aCode)
      {
         aCode = aCode.Replace(Environment.NewLine, " ");
         var aNodes = new List<CGwNode>();
         var aEdges = new List<CGwEdge>();
         var aParser = new CParser(aCode);
         aParser.SkipWhitespace();
         aParser.Expect("digraph");
         aParser.SkipWhitespace();
         var aDiagramName = aParser.ReadIdentifier();
         aParser.SkipWhitespace();
         aParser.Expect("{");
         aParser.SkipWhitespace();
         aParser.Expect("graph");
         aParser.SkipWhitespace();
         aParser.Expect("[");
         aParser.SkipWhitespace();
         aParser.Expect("bb");
         aParser.SkipWhitespace();
         aParser.Expect("=");
         aParser.SkipWhitespace();
         var aCoords = aParser.ReadString().Trim();
         var aToks = aCoords.Split(',').ToArray();
         var aDiagX = aParser.ParseDouble(aToks[0]);
         var aDiagY = aParser.ParseDouble(aToks[1]);
         var aDiagDx = aParser.ParseDouble(aToks[2]);
         var aDiagDy = aParser.ParseDouble(aToks[3]);
         aParser.SkipWhitespace();
         aParser.Expect("]");
         aParser.SkipWhitespace();
         aParser.Expect(";");
         aParser.SkipWhitespace();
         while(aParser.Char != '}')
         {
            aParser.SkipWhitespace();
            var aNodeName1 = aParser.ReadIdentifier();
            aParser.SkipWhitespace();
            var aIsEdge = aParser.Is("->");
            if (aIsEdge)
            {
               aParser.Expect("->");
               aParser.SkipWhitespace();
               var aTargetNodeName = aParser.ReadIdentifier();
               aParser.SkipWhitespace();
               aParser.Expect("[");
               var aAttributes = aParser.ReadAttributes();
               var aPos = aAttributes["pos"]
                         .Replace("\\ ", ""); // TODO ?!
               var aPosParser = new CParser(aPos);
               aPosParser.Expect("e,");
               var aSplines = new List<CPoint>();
               while(!aPosParser.IsEof)
               {
                  var aX = aPosParser.ParseDouble(aPosParser.ReadValue());
                  aPosParser.Expect(",");
                  var aY = aDiagDy - aPosParser.ParseDouble(aPosParser.ReadValue());
                  var aPoint = new CPoint(aX, aY);
                  aSplines.Add(aPoint);
                  aPosParser.SkipWhitespace();
               }
               var aColor = GetColor(aAttributes, "color");
               aParser.SkipWhitespace();
               aParser.Expect("]");
               aParser.SkipWhitespace();
               aParser.Expect(";");
               aParser.SkipWhitespace();
               var aEdge = new CGwEdge(aNodeName1, aTargetNodeName, aSplines, aColor);
               aEdges.Add(aEdge);
            }
            else
            {
               var aNodeName = aNodeName1;
               aParser.SkipWhitespace();               
               aParser.Expect("[");
               aParser.SkipWhitespace();
               var aNodeAttributes = aParser.ReadAttributes();

               aParser.SkipWhitespace();
               aParser.Expect("]");
               aParser.SkipWhitespace();
               aParser.Expect(";");
               if (aNodeAttributes.ContainsKey("pos"))
               {
                  var aPos = aNodeAttributes["pos"];
                  var aXy = aPos.Split(',');
                  var aXText = aXy[0];
                  var aYText = aXy[1];
                  var aX = aParser.ParseDouble(aXText);
                  var aY = aDiagDy - aParser.ParseDouble(aYText);
                  var aDx = InchesToPixels(aParser.ParseDouble(aNodeAttributes["width"]));
                  var aDy = InchesToPixels(aParser.ParseDouble(aNodeAttributes["height"]));
                  var aShape = aNodeAttributes["shape"];
                  var aColor = GetColor(aNodeAttributes, "color");
                  var aFontColor = GetColor(aNodeAttributes, "fontcolor");                  
                  var aGwNode = new CGwNode(aNodeName, aX, aY, aDx, aDy, aShape);
                  aGwNode.Color = aColor;
                  aGwNode.FontColor = aFontColor;
                  aNodes.Add(aGwNode);
               }
            }
            aParser.SkipWhitespace();
         }
         aParser.SkipWhitespace();
         aParser.Expect("}");
         aParser.SkipWhitespace();
         aParser.ExpectEndOfFile();

         var aGwGraph = new CGwGraph(aNodes.ToArray(), 
                                     aEdges.ToArray(),
                                     new CPoint(aDiagDx, aDiagDy)
                                     );
         return aGwGraph;

      }

      private static double InchesToPixels(double aInches) => aInches * 72;

      private sealed class CParser
      {
         internal CParser(string aString, int aPos = 0)
         {
            this.String = aString;
            this.Pos = aPos;
         }
         internal CParser Copy() => new CParser(this.String, this.Pos);
         private int Pos;
         private readonly string String;
         internal char Char { get => this.String[this.Pos]; }
         internal bool IsEof { get => this.Pos >= this.String.Length; }
         private bool IsWhitespace { get => this.Char.ToString().Trim() == string.Empty; }
         private void SkipCharacter() => ++this.Pos;
         internal void SkipWhitespace()
         {
            while (!this.IsEof
               && this.IsWhitespace)
               this.SkipCharacter();
         }
         internal void CheckNotEndOfFile(int aNr)
         {
            if (this.Pos + aNr >= this.String.Length)
               throw this.NewException("Unexpected end of file.");
         }
         internal void Expect(string aString)
         {
            this.CheckNotEndOfFile(aString.Length);
            if(this.String.Substring(this.Pos, aString.Length) == aString)
            {
               this.Pos += aString.Length;
            }
            else
            {
               throw this.NewException("Expected '" + aString + "'.");
            }
         }
         private bool IsNumeric
         {
            get => NumberChars.Contains(this.Char);
         }
         internal bool Is(string aString)
         {
            return this.Pos + aString.Length < this.String.Length
               && this.String.Substring(this.Pos, aString.Length) == aString;
         }
         private static readonly string StartIdentifierChars1 = "_abcdefghijklmnopqrstuvwxyz";
         private static readonly string StartIdentifierChars = StartIdentifierChars1.ToUpper() + StartIdentifierChars1.ToLower();
         private static readonly string NumberChars = "1234567890";
         private static readonly string MidIdentifierTokens = StartIdentifierChars + NumberChars;
         private bool IsStartIdentifierChar
         {
            get => StartIdentifierChars.Contains(this.Char);
         }
         private bool IsMidIdentifierToken
         {
            get => MidIdentifierTokens.Contains(this.Char);
         }
         internal string ReadChar()
         {
            var aChar = this.Char.ToString();
            this.Pos++;
            return aChar;
         }
         private Exception NewException(string aMsg) => new Exception("At character " + this.Pos + ": " + aMsg);
         internal string ReadIdentifier()
         {
            var aParser = this.Copy();            
            if(aParser.IsStartIdentifierChar)
            {
               var aIdentifier = new StringBuilder();
               aIdentifier.Append(aParser.ReadChar());
               while (!aParser.IsEof
                  && aParser.IsMidIdentifierToken)
                  aIdentifier.Append(aParser.ReadChar());
               this.Pos = aParser.Pos;
               return aIdentifier.ToString();
            }
            else
            {
               throw this.NewException("Expected Identifier.");
            }
         }
         internal string ReadString()
         {
            var aSb = new StringBuilder();
            this.Expect("\"");
            while (!this.IsEof && this.Char != '\"')
               aSb.Append(this.ReadChar());
            this.Expect("\"");
            return aSb.ToString();
         }
         private static CultureInfo EnCulture = CultureInfo.GetCultureInfo("en-us");
         internal double ParseDouble(string aText) => double.Parse(aText, EnCulture);
         internal string ReadValue()
         {           
            if(this.IsNumeric
            || this.Char == '-'
            || this.Char == '+')
            {
               double aMultiplier;
               if(this.Char == '-')
               {
                  this.ReadChar();
                  aMultiplier = -1;
               }
               else if(this.Char == '+')
               {
                  this.ReadChar();
                  aMultiplier = 1;
               }
               else
               {
                  aMultiplier = 1;
               }

               var aSb = new StringBuilder();
               while (!this.IsEof && this.IsNumeric)
               {
                  aSb.Append(this.ReadChar());
               }
               if (!this.IsEof && this.Char == '.')
               {
                  this.ReadChar();
                  aSb.Append(".");
                  while (!this.IsEof && this.IsNumeric)
                  {
                     aSb.Append(this.ReadChar());
                  }
               }
               var aDbl = this.ParseDouble(aSb.ToString()) * aMultiplier;
               return aDbl.ToString(EnCulture);
            }
            else if(this.Char == '\"')
            {
               return this.ReadString();
            }
            else
            {
               return this.ReadIdentifier();
            }
         }

         internal Dictionary<string, string> ReadAttributes()
         {
            var aAttributes = new Dictionary<string, string>();
            var aParser = this;
            bool aMoreAttribs = false;
            do
            {
               aParser.SkipWhitespace();
               var aAttributeName = aParser.ReadIdentifier();
               aParser.SkipWhitespace();
               aParser.Expect("=");
               aParser.SkipWhitespace();
               var aAttributeValue = aParser.ReadValue();
               aAttributes.Add(aAttributeName, aAttributeValue.ToString(EnCulture));
               aParser.SkipWhitespace();
               aMoreAttribs = aParser.Char == ',';
               if (aMoreAttribs)
               {
                  aParser.Expect(",");
               }
            }
            while (aMoreAttribs);
            return aAttributes;
         }
         internal void ExpectEndOfFile()
         {
            if(!this.IsEof)
            {
               throw this.NewException("Expected end of file.");
            }
         }
      }      

      internal readonly IEnumerable<CGwNode> Nodes;
      internal readonly IEnumerable<CGwEdge> Edges;
   }

   internal sealed class CGwDiagramBuilder : CRoutingVisitor
   {
      private Action<string> DebugPrint;
      internal CGwDiagramBuilder(Action<string> aDebugPrint,
                                 CGwDiagramLayout aLayout)
      {
         this.DebugPrint = aDebugPrint;
         this.DiagramLayout = aLayout;
      }

      internal readonly CGwDiagramLayout DiagramLayout;

      private readonly List<string> CodeWithoutCoords = new List<string>();

      private volatile CGwGraph GwGraphM;
      internal CGwGraph GwGraph
      {
         get
         {
            if(!(this.GwGraphM is object))
            {
               var aGraph = CGwGraph.New(this.CodeWithCoords.JoinString(" "));
               this.GwGraphM = aGraph;
            }
            return this.GwGraphM;
         }
      }

      private int Indent;
      private void AddLine(string aCode) => this.CodeWithoutCoords.Add(new string(' ', this.Indent) + aCode);

      private void AddLines(string aName, int aLatency, bool aActive = true, string aShape = "")
      {
         var aAttributes = new Dictionary<string, string>();
         aAttributes.Add("label", aName + " l=" + aLatency);
         if (!aShape.IsEmpty())
            aAttributes.Add("shape", aShape);
         if (aActive)
         {
            aAttributes.Add("color", "black");
            aAttributes.Add("fontcolor", "black");
         }
         else
         {
            aAttributes.Add("color", "lightgrey");
            aAttributes.Add("fontcolor", "lightgrey");
         }
         //aAttributes.Add("fixedsize", "true");
         var aStringBuilder = new StringBuilder();
         aStringBuilder.Append("\"" + aName + "\"");
         aStringBuilder.Append(this.GetAttributesCode(aAttributes));
         aStringBuilder.Append(";");
         this.AddLine(aStringBuilder.ToString());
      }

      private string GetAttributesCode(Dictionary<string, string> aAttributes)
      {
         var aStringBuilder = new StringBuilder();         
         aStringBuilder.Append("[");
         var aAttributeOpen = false;
         foreach (var aAttribute in aAttributes)
         {
            if (aAttributeOpen)
            {
               aStringBuilder.Append(" ");
            }
            aStringBuilder.Append(aAttribute.Key + "=" + "\"" + aAttribute.Value + "\"");
         }
         aStringBuilder.Append("]");
         return aStringBuilder.ToString();
      }

      public override void Visit(CRoutings aRoutings)
      {
         this.AddLine("digraph G");
         this.AddLine("{");
         ++this.Indent;
         var aWithOutputs = from aTest in aRoutings
                            where aTest.InputIdx == 0
                               || this.DiagramLayout.GetIncludeInDiagram(aTest)
                            select aTest;

         foreach (var aRouting in aWithOutputs)
         {
            if (aRouting.InputIdx == 0)
            {
               this.AddLines(aRouting.NameForInput, 0, aRouting.IsLinkedToOutput, "invtriangle");
               this.AddLines(aRouting.NameForOutput, aRouting.FinalOutputLatency, aRouting.IsLinkedToOutput, "triangle");
            }
            else
            {
               this.AddLines(aRouting.NameForInput, aRouting.OutputLatency, aRouting.IsLinkedToInput && aRouting.IsLinkedToOutput, "Mcircle");
            }
         }

         base.Visit(aRoutings);
         --this.Indent;
         this.AddLine("}");
      }



      private void VisitNonNull(CNonNullRouting aRouting)
      {
         if (aRouting.IsLinkedToSomething)
         {
            foreach (var aOutput in aRouting.Outputs)
            {
               if (aOutput.IsLinkedToSomething)
               {
                  var aEdge = aRouting.NameForInput + " -> " + aOutput.NameForOutput;
                  var aAttribs = new Dictionary<string, string>();
                  var aIsLinked = aRouting.IsLinkedToInput && aOutput.IsLinkedToOutput;
                  aAttribs.Add("color", aIsLinked ? "black" : "lightgrey");
                  var aAttributesCode = this.GetAttributesCode(aAttribs);
                  this.AddLine(aEdge + aAttributesCode + ";");
               }
            }
         }
      }

      public override void Visit(CParalellRouting aParalellRouting)
      {
         this.VisitNonNull(aParalellRouting);
      }

      public override void Visit(CDirectRouting aDirectRouting)
      {
         this.VisitNonNull(aDirectRouting);
      }

      public override void Visit(CNullRouting aNullRouting)
      {
      }

      #region Test
      private static void Test(string aId, CGwDiagramBuilder aDiagram, Action<string> aFailAction)
      {
         aDiagram.Bitmap.Save(@"C:\Program Files\Cycling '74\Max 8\packages\max-sdk-8.0.3\source\charly_beck\CbChannelStrip\m4l\Test\graph.png");
         var aCodeWithCoords = aDiagram.CodeWithCoords.JoinString(Environment.NewLine);
         var aGwGraph = CGwGraph.New(aCodeWithCoords);
      }

      internal static void Test(Action<string> aFailAction, Action<string> aDebugPrint)
      {
         Test("1baa58c2-5e3a-49c4-b762-eceab52cf977", CFlowMatrix.NewTestFlowMatrix5(aDebugPrint).Routings.GwDiagramBuilder, aFailAction);
      }
      #endregion

      private IEnumerable<string> NewGraph(IEnumerable<string> aLines)
      {
         var aStream = new MemoryStream();
         this.NewGraph(aLines, string.Empty, aStream);
         aStream.Seek(0, SeekOrigin.Begin);
         var aReader = new StreamReader(aStream);
         var aString = aReader.ReadToEnd();
         var aResultLines1 = aString.Split(Environment.NewLine[0]);
         var aResultLines2 = from aLine in aResultLines1
                             select aLine.StartsWith(Environment.NewLine[1].ToString())
                                  ? aLine.TrimStart(Environment.NewLine[1])
                                  : aLine
                                  ;
         var aResultLines = aResultLines2.ToArray();
         return aResultLines;
      }
      private void NewGraph(IEnumerable<string> aLines,
                              string aGraphoutputType,
                              Stream aStream)
      {
         try
         {
            var aErmGraphNodeVm = this;
            var aDotProcess = new ProcessStartInfo();
            var aInstallDir = this.DiagramLayout.GraphWizInstallDir;
            var aBinDir = new DirectoryInfo(Path.Combine(aInstallDir.FullName, "bin"));
            var aDir = aBinDir;
            aDotProcess.FileName = Path.Combine(aDir.FullName, "dot.exe");
            aDotProcess.Arguments = aGraphoutputType.IsEmpty() ? string.Empty : "-T" + aGraphoutputType;
            aDotProcess.RedirectStandardInput = true;
            aDotProcess.RedirectStandardOutput = true;
            aDotProcess.RedirectStandardError = true;
            aDotProcess.UseShellExecute = false;
            aDotProcess.WindowStyle = ProcessWindowStyle.Hidden;
            aDotProcess.CreateNoWindow = true;
            var aText = aLines.JoinString("\n");
            var aCompatibleText = aText;
            var aProcess = Process.Start(aDotProcess);
            aProcess.StandardInput.Write(aCompatibleText);
            aProcess.StandardInput.Close();
            var aReader = new BinaryReader(aProcess.StandardOutput.BaseStream);
            var aBuf = new byte[1024];
            var aByteCount = 0;
            do
            {
               aByteCount = aReader.Read(aBuf, 0, aBuf.Length);
               aStream.Write(aBuf, 0, aByteCount);
            }
            while (aByteCount != 0);
            var aError1 = aProcess.StandardError.ReadToEnd();
            var aError2 = aError1 is object ? aError1.ToString() : string.Empty;
            var aError = aError2;
            if (aError != string.Empty)
            {
               var aExc = new Exception(aError);
               throw aExc; 
            }
         }
         catch (Exception aExc)
         {
            throw new Exception("Could not create graph. " + aExc.Message, aExc);
         }
      }

      private string[] CodeWithCoordsM;
      private string[] CodeWithCoords
      {
         get
         {
            if (!(this.CodeWithCoordsM is object))
            {
               this.CodeWithCoordsM = this.NewGraph(this.CodeWithoutCoords).ToArray();
            }
            return this.CodeWithCoordsM;
         }
      }

      private Bitmap NewBitmap()
      {
         var aGraphoutputType = "bmp";
         var aImageStream = new MemoryStream();
         var aLines = this.CodeWithCoords;
         this.NewGraph(aLines, aGraphoutputType, aImageStream);
         aImageStream.Seek(0, SeekOrigin.Begin);
         var aBitmap = new Bitmap(aImageStream);
         return aBitmap;
      }

      //private Bitmap NewBitmap(IEnumerable<string> aLines)
      //{
      //   var aGraphoutputType = "bmp";
      //   var aImageStream = new MemoryStream();
      //   try
      //   {
      //      this.NewGraph(aLines, aGraphoutputType, aImageStream);
      //   }
      //   catch (Exception aExc)
      //   {
      //      throw new Exception("Could not create graph. " + aExc.Message, aExc);
      //   }
      //   aImageStream.Seek(0, SeekOrigin.Begin);
      //   var aBitmap = new Bitmap(aImageStream);
      //   return aBitmap; 
      //}

      private Bitmap BitmapM;
      public Bitmap Bitmap
      {
         get
         {
            if (!(this.BitmapM is object))
            {
               this.BitmapM = this.NewBitmap();
            }
            return this.BitmapM;
         }
      }
   }


}
