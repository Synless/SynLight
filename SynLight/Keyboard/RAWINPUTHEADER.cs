using System;
using System.Runtime.InteropServices;

namespace SynLight.Model
{
	[StructLayout(LayoutKind.Sequential)]
	public class RAWINPUTHEADER
	{
		public uint dwType;
		public uint dwSize;
		public IntPtr hDevice;
		public IntPtr wParam;
	}
}