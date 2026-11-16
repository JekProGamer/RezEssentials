using System;
using CounterStrikeSharp.API.Modules.Utils;

namespace hvhgg_essentials.Extensions;

public static class QAngleExtensions
{
	public static bool IsValid(this QAngle angle)
	{
		if (angle.IsFinite() && !angle.IsNaN())
		{
			return angle.IsAllowed();
		}
		return false;
	}

	private static bool IsFinite(this QAngle angle)
	{
		if (float.IsFinite(angle.X) && float.IsFinite(angle.Y))
		{
			return float.IsFinite(angle.Z);
		}
		return false;
	}

	private static bool IsNaN(this QAngle angle)
	{
		if (!float.IsNaN(angle.X) && !float.IsNaN(angle.Y))
		{
			return float.IsNaN(angle.Z);
		}
		return true;
	}

	public static void Fix(this QAngle angle)
	{
		angle.FixInfinity();
		angle.FixNaN();
		if (angle.IsReasonable())
		{
			angle.Normalize();
		}
		angle.Clamp();
	}

	private static void FixInfinity(this QAngle angle)
	{
		if (!float.IsFinite(angle.X))
		{
			angle.X = 0f;
		}
		if (!float.IsFinite(angle.Y))
		{
			angle.Y = 0f;
		}
		if (!float.IsFinite(angle.Z))
		{
			angle.Z = 0f;
		}
	}

	private static void FixNaN(this QAngle angle)
	{
		if (float.IsNaN(angle.X))
		{
			angle.X = 0f;
		}
		if (float.IsNaN(angle.Y))
		{
			angle.Y = 0f;
		}
		if (float.IsNaN(angle.Z))
		{
			angle.Z = 0f;
		}
	}

	private static void Clamp(this QAngle angle)
	{
		angle.X = Math.Clamp(angle.X, -179.9f, 179.9f);
		angle.Y = Math.Clamp(angle.Y, -180f, 180f);
	}

	private static void Normalize(this QAngle angle)
	{
		angle.X = (angle.X + 180f) % 360f - 180f;
		angle.Y = (angle.Y + 180f) % 360f - 180f;
		angle.Z = (angle.Z + 180f) % 360f - 180f;
	}

	internal static bool IsReasonable(this QAngle q)
	{
		float x = q.X;
		if (x > -360000f && x < 360000f)
		{
			x = q.Y;
			if (x > -360000f && x < 360000f)
			{
				x = q.Z;
				if (x > -360000f)
				{
					return x < 360000f;
				}
				return false;
			}
		}
		return false;
	}

	internal static bool IsAllowed(this QAngle q)
	{
		float x = q.X;
		if (x >= -179f && x <= 179f)
		{
			x = q.Y;
			if (x >= -180f && x <= 180f)
			{
				x = q.Z;
				if (x >= -180f)
				{
					return x <= 180f;
				}
				return false;
			}
		}
		return false;
	}
}
