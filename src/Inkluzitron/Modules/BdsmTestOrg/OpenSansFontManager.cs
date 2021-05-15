using System;
using System.Linq;
using System.Drawing.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Inkluzitron.Resources.Fonts.OpenSans
{
    public class OpenSansFontManager : IDisposable
    {
        public FontFamily Light { get; }
        public FontFamily Bold { get; }

        private readonly PrivateFontCollection _fontCollection;
        private readonly IntPtr _lightData;
        private readonly IntPtr _boldData;

        public OpenSansFontManager()
        {
            _fontCollection = new PrivateFontCollection();
            LoadFontFromResource(OpenSansResources.Light, out _lightData);
            LoadFontFromResource(OpenSansResources.Bold, out _boldData);

            Light = _fontCollection.Families.Single(f => f.Name == "Open Sans Condensed Light");
            Bold = _fontCollection.Families.Single(f => f.Name == "Open Sans Condensed");
        }

        public void Dispose()
        {
            _fontCollection.Dispose();
            Marshal.FreeCoTaskMem(_lightData);
            Marshal.FreeCoTaskMem(_boldData);
        }

        // https://stackoverflow.com/a/23658552
        private void LoadFontFromResource(byte[] fontBytes, out IntPtr memoryPointer)
        {
            var fontData = Marshal.AllocCoTaskMem(fontBytes.Length);
            Marshal.Copy(fontBytes, 0, fontData, fontBytes.Length);
            _fontCollection.AddMemoryFont(fontData, fontBytes.Length);
            memoryPointer = fontData;
        }
    }
}
