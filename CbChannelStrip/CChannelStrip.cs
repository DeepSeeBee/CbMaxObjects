using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CbChannelStrip
{
    using System.Drawing;
    using CbMaxClrAdapter;
    using CbMaxClrAdapter.Jitter;

    public sealed class CChannelStrip : CMaxObject
    {
        public CChannelStrip()
        {
            this.IntInlet = new CIntInlet(this)
            {
                Action = this.OnIntInlet
            };
            this.IntOutlet = new CIntOutlet(this);

            this.ListInlet = new CListInlet(this)
            {
                Action = this.OnListInlet
            };
            this.ListOutlet = new CListOutlet(this);

            this.MatrixInlet = new CMatrixInlet(this)
            {
                Action = this.OnMatrixInlet
            };
            this.MatrixOutlet = new CMatrixOutlet(this);

            this.LeftInlet.Support(CMessageTypeEnum.Symbol);
            this.LeftInlet.SetSymbolAction("load_image", this.OnLoadImage);

        }

        internal readonly CIntInlet IntInlet;
        internal readonly CIntOutlet IntOutlet;

        internal readonly CListInlet ListInlet;
        internal readonly CListOutlet ListOutlet;

        internal readonly CMatrixInlet MatrixInlet;
        internal readonly CMatrixOutlet MatrixOutlet;

        private void OnLoadImage(CInlet aInlet, CSymbol aSymbol)
        {
            this.MatrixOutlet.Message.Value.SetImage(Image.FromFile(@"C:\Program Files\Cycling '74\Max 8\packages\max-sdk-8.0.3\source\charly_beck\CbChannelStrip\m4l\Test\hade.jpg"));
            this.MatrixOutlet.Send();
        }

        private void OnIntInlet(CInlet aInlet, CInt aInt)
        {
            this.IntOutlet.Message.Value = aInt.Value;
            this.IntOutlet.Send();
        }

        private void OnListInlet(CInlet aInlet, CList aList)
        {
            this.ListOutlet.Message.Value = aList.Value;
            this.ListOutlet.Send();
        }

        private void OnMatrixInlet(CInlet aInlet, CMatrix aMatrix)
        {
            this.MatrixOutlet.Message.Value = aMatrix.Value;
            this.MatrixOutlet.Send();
        }
    }
}
