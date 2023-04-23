using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SynLight.Model
{
	[StructLayout(LayoutKind.Sequential)]
	public struct RAWINPUTDEVICE
	{
		ushort usUsagePage;
		ushort usUsage;
		uint dwFlags;
		IntPtr hwndTarget;

		public RAWINPUTDEVICE(ushort usUsagePage, ushort usUsage, uint dwFlags, IntPtr hwndTarget)
		{
			this.usUsagePage = usUsagePage;
			this.usUsage = usUsage;
			this.dwFlags = dwFlags;
			this.hwndTarget = hwndTarget;
		}

		public RAWINPUTDEVICE(ushort usUsagePage, ushort usUsage, uint dwFlags, IWin32Window target)
		{
			this.usUsagePage = usUsagePage;
			this.usUsage = usUsage;
			this.dwFlags = dwFlags;
			this.hwndTarget = target.Handle;
		}
	}
}