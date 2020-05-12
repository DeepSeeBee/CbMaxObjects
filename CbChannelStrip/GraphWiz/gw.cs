using CbMaxClrAdapter;
using CbMaxClrAdapter.Jitter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace CbChannelStrip.GraphWiz
{
 
   internal sealed class CGwNode
   {
      internal CGwNode(string aName, double aX, double aY, double aDx, double aDy)
      {
         this.Name = aName;
         this.X = aX;
         this.Y = aY;
      }
      internal readonly string Name;      
      internal readonly double X;
      internal readonly double Y;
      internal readonly double Dx;
      internal readonly double Dy;
      internal readonly string Shape;
      internal enum COutlineShapeEnum
      {
         Triangle,
         InvTriangle,
         Circle,
      }
      private COutlineShapeEnum? ShapeEnumM;
      internal COutlineShapeEnum ShapeEnum
      {
         get
         {
            if (!(this.ShapeEnumM.HasValue))
               this.ShapeEnumM = this.CalcShapeEnum();
            return this.ShapeEnumM.Value;
         }
      }
      private COutlineShapeEnum CalcShapeEnum()
      {
         switch (this.Shape.ToLower())
         {
            case "triangle":
               return COutlineShapeEnum.Triangle;
            case "invtriangle":
               return COutlineShapeEnum.InvTriangle;
            case "Mcircle":
               return COutlineShapeEnum.Circle;
            default:
               throw new ArgumentException();
         }
      }

      internal CMatrixData NewShapeMatrix()
      {
         //var aMatrix = new CMatrixData();
         //aMatrix.ReallocateInternal(, CMatrixData.CCellTypeEnum.Float32, 1, new int[] {}
         throw new NotImplementedException();
      }

      internal CMatrixData NewTransformMatrix()
      {
         throw new NotImplementedException();
      }


      internal double CenterX
      {
         get => this.X + this.Dx / 2.0d;
      }
      internal double CenterY
      {
         get => this.Y + this.Dy / 2.0d;
      }

   }
   internal sealed class CGwGraph
   {
      internal CGwGraph(IEnumerable<CGwNode> aNodes)
      {
         this.Nodes = aNodes;
      }

      internal static CGwGraph New(string aCode)
      {
         aCode = aCode.Replace(Environment.NewLine, " ");
         var aGwNodes = new List<CGwNode>();
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
         var aDiagX = double.Parse(aToks[0]);
         var aDiagY = double.Parse(aToks[1]);
         var aDiagDx = double.Parse(aToks[2]);
         var aDiagDy = double.Parse(aToks[3]);
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

               //var aData = aParser.ReadString();
               aParser.SkipWhitespace();
               aParser.Expect("]");
               aParser.SkipWhitespace();
               aParser.Expect(";");
               aParser.SkipWhitespace();
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
               if (aNodeAttributes.ContainsKey("Pos"))
               {
                  var aPos = aNodeAttributes["Pos"];
                  var aXy = aPos.Split(',');
                  var aXText = aXy[0];
                  var aYText = aXy[1];
                  var aX = double.Parse(aXText);
                  var aY = double.Parse(aYText);
                  var aDx = double.Parse(aNodeAttributes["width"]);
                  var aDy = double.Parse(aNodeAttributes["height"]);
                  var aGwNode = new CGwNode(aNodeName, aX, aY, aDx, aDy);
                  aGwNodes.Add(aGwNode);
               }

            }
            aParser.SkipWhitespace();
         }
         aParser.SkipWhitespace();
         aParser.Expect("}");
         aParser.SkipWhitespace();
         aParser.ExpectEndOfFile();

         var aGwGraph = new CGwGraph(aGwNodes);
         return aGwGraph;

      }

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
         private bool IsEof { get => this.Pos >= this.String.Length; }
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

         internal string ReadValue()
         {
            if(this.IsNumeric)
            {
               var aSb = new StringBuilder();
               while (this.IsNumeric)
               {
                  aSb.Append(this.ReadChar());
               }
               if (this.Char == '.')
               {
                  aSb.Append(this.ReadChar());
                  while (this.IsNumeric)
                  {
                     aSb.Append(this.ReadChar());
                  }
               }
               var aDbl = double.Parse(aSb.ToString());
               return aDbl.ToString();
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
               aAttributes.Add(aAttributeName, aAttributeValue);
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
   }


   internal sealed class CGwDiagramBuilder : CRoutingVisitor
   {
      internal CGwDiagramBuilder(CSettings aSettings)
      {
         this.Settings = aSettings;
      }

      private CSettings Settings;

      private readonly List<string> CodeWithoutCoords = new List<string>();


      private int Indent;
      private void AddLine(string aCode) => this.CodeWithoutCoords.Add(new string(' ', this.Indent) + aCode);

      private void AddLines(CRouting aRouting, string aName, int aLatency, bool aActive = true, string aShape = "")
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
         var aStringBuilder = new StringBuilder();
         aStringBuilder.Append("\"" + aName + "\"");
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
         aStringBuilder.Append(";");
         this.AddLine(aStringBuilder.ToString());
      }

      public override void Visit(CRoutings aRoutings)
      {
         this.AddLine("digraph G");
         this.AddLine("{");
         ++this.Indent;

         var aWithOutputs = from aTest in aRoutings
                            where aTest.InputIdx == 0
                               || aTest.IsLinkedToSomething
                            select aTest;

         foreach (var aRouting in aWithOutputs)
         {
            if (aRouting.InputIdx == 0)
            {
               this.AddLines(aRouting, this.GetInName(aRouting), 0, aRouting.IsLinkedToOutput, "invtriangle");
               this.AddLines(aRouting, this.GetOutName(aRouting), aRouting.FinalOutputLatency, aRouting.IsLinkedToOutput, "triangle");
            }
            else
            {
               this.AddLines(aRouting, this.GetInName(aRouting), aRouting.OutputLatency, aRouting.IsLinkedToInput && aRouting.IsLinkedToOutput, "Mcircle");
            }
         }

         base.Visit(aRoutings);
         --this.Indent;
         this.AddLine("}");
      }

      private string GetName(CRouting aRouting) => "R" + aRouting.InputIdx;

      private string GetInName(CRouting aRouting) => aRouting.InputIdx == 0 ? "in" : this.GetName(aRouting);
      private string GetOutName(CRouting aRouting) => aRouting.InputIdx == 0 ? "out" : this.GetName(aRouting);

      private void VisitNonNull(CNonNullRouting aRouting)
      {
         if (aRouting.IsLinkedToSomething)
         {
            foreach (var aOutput in aRouting.Outputs)
            {
               if (aOutput.IsLinkedToSomething)
               {
                  this.AddLine(this.GetInName(aRouting) + " -> " + this.GetOutName(aOutput) + ";");
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

      internal static void Test(Action<string> aFailAction)
      {
         var aSettings = new CSettings();
         Test("", new CFlowMatrix(aSettings, 7,
                                  0, 1, 1, 0, 0, 0, 0,
                                  0, 0, 0, 0, 0, 0, 1,
                                  0, 0, 0, 1, 1, 0, 0,
                                  0, 0, 0, 0, 0, 1, 0,
                                  0, 0, 0, 0, 0, 1, 0,
                                  0, 0, 0, 0, 0, 0, 1,
                                  1, 0, 0, 0, 0, 0, 0
                                  ).Routings.GraphWizDiagram, aFailAction);
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
            var aInstallDir = this.Settings.GraphWizInstallDir;
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
