using System;
using System.Runtime.CompilerServices;
using CounterStrikeSharp.API.Modules.Utils;

namespace hvhgg_essentials;

public class CUserCmd
{
	private nint Handle { get; set; }

	public CUserCmd(nint pointer)
	{
		Handle = pointer;
	}

	public unsafe QAngle? GetViewAngles()
	{
		if (Handle == IntPtr.Zero)
		{
			return null;
		}
		nint num = Unsafe.Read<nint>((void*)(Handle + 64));
		if (num == IntPtr.Zero)
		{
			return null;
		}
		nint num2 = Unsafe.Read<nint>((void*)(num + 64));
		if (num2 == IntPtr.Zero)
		{
			return null;
		}
		QAngle qAngle = new QAngle(num2 + 24);
		if (qAngle.Handle != IntPtr.Zero)
		{
			return qAngle;
		}
		return null;
	}
}
